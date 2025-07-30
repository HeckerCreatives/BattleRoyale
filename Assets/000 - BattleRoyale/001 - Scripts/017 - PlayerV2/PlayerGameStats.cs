using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static Fusion.NetworkBehaviour;

public class PlayerGameStats : NetworkBehaviour
{
    [SerializeField] private UserData userData;

    [Header("GAME STATUS")]
    [SerializeField] public TextMeshProUGUI pingTMP;

    [Header("WAITING AREA")]
    [SerializeField] public GameObject Timer;
    [SerializeField] public GameObject TimerGetReady;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI timerGetReadyTMP;

    [Header("KILL COUNT")]
    [SerializeField] public TextMeshProUGUI PlayerCount;
    [SerializeField] public TextMeshProUGUI killCountTMP;

    [Header("GAME OVER SCREEN")]
    [SerializeField] private GameObject controllerObj;
    [SerializeField] private GameObject pauseObj;
    [SerializeField] private GameObject gameOverPanelObj;
    [SerializeField] private GameObject winMessageObj;
    [SerializeField] private GameObject loseMessageObj;
    [SerializeField] private TextMeshProUGUI gameStatusTimer;

    [Header("GAME OVER STATS 1")]
    [SerializeField] private TextMeshProUGUI usernameResultTMP;
    [SerializeField] private TextMeshProUGUI playerCountResultTMP;
    [SerializeField] private TextMeshProUGUI rankResultTMP;
    [SerializeField] public TextMeshProUGUI killCountResultTMP;

    [Header("GAME OVER STATS 2")]
    [SerializeField] private TextMeshProUGUI resultPointsTMP;
    [SerializeField] private TextMeshProUGUI killPointsTMP;
    [SerializeField] private TextMeshProUGUI rankPointResultTMP;
    [SerializeField] private TextMeshProUGUI hitPointResultTMP;

    [Header("GAME OVER STATS 3")]
    [SerializeField] private TextMeshProUGUI expPointsTMP;
    [SerializeField] private TextMeshProUGUI lvlPointsExpTMP;
    [SerializeField] private TextMeshProUGUI rankPointsExpTMP;
    [SerializeField] private TextMeshProUGUI killPointsExpTMP;

    [Header("DEBUGGER")]
    [SerializeField] private bool IsDoneShowingGameOver;
    [SerializeField] private float quitCountdown;
    [SerializeField] private bool canQuit;
    [SerializeField] private float pingChange;

    [field: Header("NETWORK DEBUGGER")]
    [Networked][field: SerializeField] public DedicatedServerManager ServerManager { get; set; }
    [field: SerializeField][Networked] public int KillCount { get; set; }
    [field: SerializeField][Networked] public float HitPoints { get; set; }
    [field: SerializeField][Networked] public int PlayerPlacement { get; set; }

    //  ======================

    private ChangeDetector _changeDetector;

    //  ======================

    private void OnEnable()
    {
        quitCountdown = 10f;
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        RegisterServerEvents();
    }

    public override void Render()
    {
        if (!HasInputAuthority) return;

        if (ServerManager == null) return;

        GameStats();
        WaitingAreaTimer();
        DeathGameOver();
    }

    private void Update()
    {
        QuitOnShowStatusCountdown();
    }

    private void LateUpdate()
    {
        if (pingChange < Time.time)
        {
            pingChange = Time.time + 1;
            pingTMP.text = $"Ping: {(Runner.GetPlayerRtt(Object.InputAuthority) * 1000):n0} (ms)";
        }
    }

    private async void RegisterServerEvents()
    {
        while (ServerManager == null) await Task.Yield();

        ServerManager.OnPlayerCountChange += PlayerCountChange;
        ServerManager.OnCurrentStateChange += GameStateChange;
    }

    private void GameStats()
    {
        int tempCapacity = ServerManager.RemainingPlayers.Capacity;

        PlayerCount.text = $"{(ServerManager.RemainingPlayers.Count + ServerManager.Bots.Count):n0} / {(tempCapacity - 2):n0}";
        killCountTMP.text = $"{KillCount:n0}";
    }

    private void WaitingAreaTimer()
    {
        if (ServerManager.CurrentGameState != GameState.WAITINGAREA)
        {
            Timer.gameObject.SetActive(false);
            TimerGetReady.SetActive(false);
            return;
        }

        if (ServerManager.CurrentWaitingAreaTimerState == WaitingAreaTimerState.WAITING)
        {
            Timer.gameObject.SetActive(true);
            TimerGetReady.SetActive(false);

            timerTMP.text = $"{GameManager.Instance.GetMinuteSecondsTime(ServerManager.WaitingAreaTimer)}";
        }
        else
        {
            TimerGetReady.SetActive(true);
            Timer.gameObject.SetActive(false);

            timerGetReadyTMP.text = $"{GameManager.Instance.GetMinuteSecondsTime(ServerManager.WaitingAreaTimer)}";
        }
    }

    #region GAME OVER

    private void DeathGameOver()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(PlayerPlacement):

                    if (!HasInputAuthority) return;

                    IsDoneShowingGameOver = true;

                    usernameResultTMP.text = userData.Username;
                    int botsCount = ServerManager.Bots.Count;
                    Debug.Log(botsCount);
                    playerCountResultTMP.text = $"<color=yellow><size=\"55\">#{(PlayerPlacement + botsCount)}</size></color> <size=\"50\"> / {ServerManager.RemainingPlayers.Capacity - 2}</size>";
                    rankResultTMP.text = (PlayerPlacement + botsCount).ToString();
                    killCountResultTMP.text = KillCount.ToString();

                    //  Leaderboard

                    float rankpointLB = ((100 - (PlayerPlacement + botsCount) + 1) / 100f) * 20;
                    rankpointLB = (float)Math.Truncate(rankpointLB);
                    float killpointLB = KillCount * 100f;
                    killpointLB = (float)Math.Truncate(killpointLB);
                    float finalresultpointLB = rankpointLB + killpointLB + HitPoints;
                    finalresultpointLB = (float)Math.Truncate(finalresultpointLB);

                    rankPointResultTMP.text = rankpointLB.ToString("n0");
                    killPointsTMP.text = killpointLB.ToString("n0");
                    hitPointResultTMP.text = HitPoints.ToString("n0");
                    resultPointsTMP.text = finalresultpointLB.ToString("n0");

                    //  EXP
                    float userlevelPoints = (userData.GameDetails.level / 2) * 3;
                    userlevelPoints = (float)Math.Truncate(userlevelPoints);
                    float rankxpPoints = ((100 - PlayerPlacement + 1) / 100) * 20;
                    rankxpPoints = (float)Math.Truncate(rankxpPoints);
                    float killxpPoints = KillCount * ((userData.GameDetails.level / 4) + 1);
                    killxpPoints = (float)Math.Truncate(killxpPoints);
                    float finalxp = userlevelPoints + rankxpPoints + killxpPoints;
                    finalxp = (float)Math.Truncate(finalxp);

                    lvlPointsExpTMP.text = userlevelPoints.ToString("n0");
                    rankPointsExpTMP.text = rankxpPoints.ToString("n0");
                    killPointsExpTMP.text = killxpPoints.ToString("n0");
                    expPointsTMP.text = finalxp.ToString("n0");

                    controllerObj.SetActive(false);
                    pauseObj.SetActive(false);

                    winMessageObj.SetActive(false);
                    loseMessageObj.SetActive(true);
                    gameOverPanelObj.SetActive(true);

                    GameManager.Instance.NoBGLoading.SetActive(true);
                    StartCoroutine(GameManager.Instance.PostRequest("/usergamedetail/updateusergamedetails", "", new Dictionary<string, object>
                    {
                        { "kill", KillCount },
                        { "death", PlayerPlacement != 1 ? 1 : 0 },
                        { "rank", PlayerPlacement }
                    }, true, (response) =>
                    {
                        StartCoroutine(GameManager.Instance.PostRequest("/leaderboard/updateuserleaderboard", "", new Dictionary<string, object>
                        {
                            { "amount", finalresultpointLB }
                        }, true, (tempresponse) =>
                        {
                            GameManager.Instance.NoBGLoading.SetActive(false);
                            canQuit = true;
                        }, () => GameManager.Instance.NoBGLoading.SetActive(false)));
                    }, () => GameManager.Instance.NoBGLoading.SetActive(false)));

                    break;
            }
        }
    }

    private void PlayerCountChange(object sender, EventArgs e)
    {
        ShowWinnerOnAllPlayerQuit();
    }

    private void GameStateChange(object sender, EventArgs e)
    {
        ShowWinner();
    }

    private void ShowWinner()
    {
        if (!HasInputAuthority) return;

        if (winMessageObj.activeInHierarchy) return;

        if (IsDoneShowingGameOver) return;

        if (ServerManager.CurrentGameState == GameState.DONE)
        {
            IsDoneShowingGameOver = true;

            usernameResultTMP.text = userData.Username;
            playerCountResultTMP.text = $"<color=yellow><size=\"55\">#1</size></color> <size=\"50\"> / {ServerManager.RemainingPlayers.Capacity - 2}</size>";
            rankResultTMP.text = "1";
            killCountResultTMP.text = KillCount.ToString();

            //  Leaderboard

            float rankpointLB = ((100 - (PlayerPlacement + ServerManager.Bots.Count) + 1) / 100) * 20;
            float killpointLB = KillCount * 100;
            float finalresultpointLB = rankpointLB + killpointLB + HitPoints;
            finalresultpointLB = (float)Math.Truncate(finalresultpointLB);

            rankPointResultTMP.text = rankpointLB.ToString("n0");
            killPointsTMP.text = killpointLB.ToString("n0");
            hitPointResultTMP.text = HitPoints.ToString("n0");
            resultPointsTMP.text = finalresultpointLB.ToString("n0");

            //  EXP
            float userlevelPoints = (userData.GameDetails.level / 2) * 3;
            userlevelPoints = (float)Math.Truncate(userlevelPoints);
            float rankxpPoints = ((100 - PlayerPlacement + 1) / 100) * 20;
            rankxpPoints = (float)Math.Truncate(rankxpPoints);
            float killxpPoints = KillCount * ((userData.GameDetails.level / 4) + 1);
            killxpPoints = (float)Math.Truncate(killxpPoints);
            float finalxp = userlevelPoints + rankxpPoints + killxpPoints;
            finalxp = (float)Math.Truncate(finalxp);

            lvlPointsExpTMP.text = userlevelPoints.ToString("n0");
            rankPointsExpTMP.text = rankxpPoints.ToString("n0");
            killPointsExpTMP.text = killxpPoints.ToString("n0");
            expPointsTMP.text = finalxp.ToString("n0");

            controllerObj.SetActive(false);
            pauseObj.SetActive(false);

            winMessageObj.SetActive(true);
            loseMessageObj.SetActive(false);
            gameOverPanelObj.SetActive(true);


            GameManager.Instance.NoBGLoading.SetActive(true);
            StartCoroutine(GameManager.Instance.PostRequest("/usergamedetail/updateusergamedetails", "", new Dictionary<string, object>
            {
                { "kill", KillCount },
                { "death", PlayerPlacement != 1 ? 1 : 0 },
                { "rank", PlayerPlacement }
            }, true, (response) =>
            {
                StartCoroutine(GameManager.Instance.PostRequest("/leaderboard/updateuserleaderboard", "", new Dictionary<string, object>
                {
                    { "amount", finalresultpointLB }
                }, true, (tempresponse) => {
                    GameManager.Instance.NoBGLoading.SetActive(false);
                    canQuit = true;
                }, () => GameManager.Instance.NoBGLoading.SetActive(false)));
            }, () => GameManager.Instance.NoBGLoading.SetActive(false)));
        }
    }

    private void ShowWinnerOnAllPlayerQuit()
    {
        if (!HasInputAuthority) return;

        if (ServerManager.CurrentGameState != GameState.ARENA) return;

        if (ServerManager.RemainingPlayers.Count > 1 || ServerManager.Bots.Count > 0) return;

        if (winMessageObj.activeInHierarchy) return;

        if (IsDoneShowingGameOver) return;

        IsDoneShowingGameOver = true;

        usernameResultTMP.text = userData.Username;
        playerCountResultTMP.text = $"<color=yellow><size=\"55\">#1</size></color> <size=\"50\"> / {ServerManager.RemainingPlayers.Capacity - 2}</size>";
        rankResultTMP.text = "1";
        killCountResultTMP.text = KillCount.ToString();

        //  Leaderboard

        float rankpointLB = ((100 - (PlayerPlacement + ServerManager.Bots.Count) + 1) / 100) * 20;
        float killpointLB = KillCount * 100;
        float finalresultpointLB = rankpointLB + killpointLB + HitPoints;
        finalresultpointLB = (float)Math.Truncate(finalresultpointLB);

        rankPointResultTMP.text = rankpointLB.ToString("n0");
        killPointsTMP.text = killpointLB.ToString("n0");
        hitPointResultTMP.text = HitPoints.ToString("n0");
        resultPointsTMP.text = finalresultpointLB.ToString("n0");

        //  EXP
        float userlevelPoints = (userData.GameDetails.level / 2) * 3;
        userlevelPoints = (float)Math.Truncate(userlevelPoints);
        float rankxpPoints = ((100 - PlayerPlacement + 1) / 100) * 20;
        rankxpPoints = (float)Math.Truncate(rankxpPoints);
        float killxpPoints = KillCount * ((userData.GameDetails.level / 4) + 1);
        killxpPoints = (float)Math.Truncate(killxpPoints);
        float finalxp = userlevelPoints + rankxpPoints + killxpPoints;
        finalxp = (float)Math.Truncate(finalxp);

        lvlPointsExpTMP.text = userlevelPoints.ToString("n0");
        rankPointsExpTMP.text = rankxpPoints.ToString("n0");
        killPointsExpTMP.text = killxpPoints.ToString("n0");
        expPointsTMP.text = finalxp.ToString("n0");

        controllerObj.SetActive(false);
        pauseObj.SetActive(false);

        winMessageObj.SetActive(true);
        loseMessageObj.SetActive(false);
        gameOverPanelObj.SetActive(true);


        GameManager.Instance.NoBGLoading.SetActive(true);
        StartCoroutine(GameManager.Instance.PostRequest("/usergamedetail/updateusergamedetails", "", new Dictionary<string, object>
        {
            { "kill", KillCount },
            { "death", PlayerPlacement != 1 ? 1 : 0 },
            { "rank", PlayerPlacement }
        }, true, (response) =>
        {
            StartCoroutine(GameManager.Instance.PostRequest("/leaderboard/updateuserleaderboard", "", new Dictionary<string, object>
            {
                { "amount", finalresultpointLB  }
            }, true, (tempresponse) => {
                GameManager.Instance.NoBGLoading.SetActive(false);
                canQuit = true;
            }, () => GameManager.Instance.NoBGLoading.SetActive(false)));
        }, () => GameManager.Instance.NoBGLoading.SetActive(false)));
    }

    private void QuitOnShowStatusCountdown()
    {
        if (!canQuit) return;

        if (quitCountdown > 0)
        {
            quitCountdown -= Time.deltaTime;

            gameStatusTimer.text = $"Returning to lobby in {quitCountdown:n0}..";
        }
        else
        {
            canQuit = false;

            gameStatusTimer.text = $"Returning to lobby in 0..";

            Runner.Shutdown();
            GameManager.Instance.SceneController.CurrentScene = "Lobby";
        }
    }

    #endregion
}
