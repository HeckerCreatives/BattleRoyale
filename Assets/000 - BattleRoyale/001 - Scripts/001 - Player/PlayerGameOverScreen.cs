using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static Fusion.NetworkBehaviour;

public class PlayerGameOverScreen : NetworkBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private KillCountCounterController killCountCounterController;
    [SerializeField] private PlayerHealth playerHealth;

    [Space]
    [SerializeField] private GameObject controllerObj;
    [SerializeField] private GameObject pauseObj;
    [SerializeField] private GameObject gameOverPanelObj;
    [SerializeField] private GameObject winMessageObj;
    [SerializeField] private GameObject loseMessageObj;

    [Space]
    [SerializeField] private TextMeshProUGUI usernameResultTMP;
    [SerializeField] private TextMeshProUGUI playerCountResultTMP;
    [SerializeField] private TextMeshProUGUI rankResultTMP;
    [SerializeField] public TextMeshProUGUI killCountResultTMP;

    [Space]
    [SerializeField] private TextMeshProUGUI resultPointsTMP;
    [SerializeField] private TextMeshProUGUI killPointsTMP;
    [SerializeField] private TextMeshProUGUI rankPointResultTMP;
    [SerializeField] private TextMeshProUGUI hitPointResultTMP;

    [Space]
    [SerializeField] private TextMeshProUGUI expPointsTMP;
    [SerializeField] private TextMeshProUGUI lvlPointsExpTMP;
    [SerializeField] private TextMeshProUGUI rankPointsExpTMP;
    [SerializeField] private TextMeshProUGUI killPointsExpTMP;

    [field: Header("DEBUGGER")]
    [field: SerializeField][Networked] public float HitPoints { get; set; }
    [field: SerializeField][Networked] public int PlayerPlacement { get; set; }
    [field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }
    [SerializeField] private bool IsDoneShowingGameOver;

    //  ======================

    private ChangeDetector _changeDetector;

    //  ======================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(PlayerPlacement):

                    if (!HasInputAuthority) return;

                    if (IsDoneShowingGameOver) return;

                    //  THIS IS FOR DEATH

                    IsDoneShowingGameOver = true;

                    Debug.Log($"Player lose, player placement: {PlayerPlacement}");
                    usernameResultTMP.text = userData.Username;
                    playerCountResultTMP.text = $"<color=yellow><size=\"55\">#{PlayerPlacement}</size></color> <size=\"50\"> / {ServerManager.RemainingPlayers.Capacity - 2}</size>";
                    rankResultTMP.text = PlayerPlacement.ToString();
                    killCountResultTMP.text = killCountCounterController.KillCount.ToString();

                    //  Leaderboard

                    float rankpointLB = ((100 - PlayerPlacement + 1) / 100f) * 20;
                    rankpointLB = (float)Math.Truncate(rankpointLB);
                    float killpointLB = killCountCounterController.KillCount * 100f;
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
                    float killxpPoints = killCountCounterController.KillCount * ((userData.GameDetails.level / 4) + 1);
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
                        { "kill", killCountCounterController.KillCount },
                        { "death", PlayerPlacement != 1 ? 1 : 0 },
                        { "rank", PlayerPlacement }
                    }, true, (response) =>
                    {
                        StartCoroutine(GameManager.Instance.PostRequest("/leaderboard/updateuserleaderboard", "", new Dictionary<string, object>
                        {
                            { "amount", finalresultpointLB }
                        }, true, (tempresponse) => GameManager.Instance.NoBGLoading.SetActive(false), () => GameManager.Instance.NoBGLoading.SetActive(false)));
                    }, () => GameManager.Instance.NoBGLoading.SetActive(false)));

                    break;
            }
        }
    }

    private async void OnEnable()
    {
        while (!Runner)
        {
            Debug.Log("waiting runner on kill count counter");
            await Task.Yield();
        }

        if (HasInputAuthority)
        {
            while (!ServerManager)
            {
                Debug.Log("waiting on server manager kill count counter");
                await Task.Yield();
            }

            ServerManager.OnPlayerCountChange += PlayerCountChange;
            ServerManager.OnCurrentStateChange += GameStateChange;
            Debug.Log("done initialize server manager player change event on kill count counter");
        }
    }

    private void OnDisable()
    {
        if (HasInputAuthority)
        {
            if (ServerManager != null)
            {
                ServerManager.OnPlayerCountChange -= PlayerCountChange;
                ServerManager.OnCurrentStateChange -= GameStateChange;
            }
        }
    }

    private void GameStateChange(object sender, EventArgs e)
    {
        ShowWinner();
    }

    private void PlayerCountChange(object sender, EventArgs e)
    {
        ShowWinnerOnAllPlayerQuit();
    }

    private async void ShowWinner()
    {
        if (!HasInputAuthority) return;

        if (playerHealth.CurrentHealth <= 0) return;

        await Task.Delay(1500);

        if (winMessageObj.activeInHierarchy) return;

        if (IsDoneShowingGameOver) return;

        if (ServerManager.CurrentGameState == GameState.DONE)
        {
            IsDoneShowingGameOver = true;

            usernameResultTMP.text = userData.Username;
            playerCountResultTMP.text = $"<color=yellow><size=\"55\">#1</size></color> <size=\"50\"> / {ServerManager.RemainingPlayers.Capacity - 2}</size>";
            rankResultTMP.text = "1";
            killCountResultTMP.text = killCountCounterController.KillCount.ToString();

            //  Leaderboard

            float rankpointLB = ((100 - PlayerPlacement + 1) / 100) * 20;
            float killpointLB = killCountCounterController.KillCount * 100;
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
            float killxpPoints = killCountCounterController.KillCount * ((userData.GameDetails.level / 4) + 1);
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
                { "kill", killCountCounterController.KillCount },
                { "death", PlayerPlacement != 1 ? 1 : 0 },
                { "rank", PlayerPlacement }
            }, true, (response) =>
            {
                StartCoroutine(GameManager.Instance.PostRequest("/leaderboard/updateuserleaderboard", "", new Dictionary<string, object>
                {
                    { "amount", finalresultpointLB }
                }, true, (tempresponse) => GameManager.Instance.NoBGLoading.SetActive(false), () => GameManager.Instance.NoBGLoading.SetActive(false)));
            }, () => GameManager.Instance.NoBGLoading.SetActive(false)));
        }
    }

    private async void ShowWinnerOnAllPlayerQuit()
    {
        if (!HasInputAuthority) return;

        if (ServerManager.CurrentGameState != GameState.ARENA) return;

        if (ServerManager.RemainingPlayers.Count > 1) return;

        await Task.Delay(1500);

        if (winMessageObj.activeInHierarchy) return;

        if (IsDoneShowingGameOver) return;

        IsDoneShowingGameOver = true;

        usernameResultTMP.text = userData.Username;
        playerCountResultTMP.text = $"<color=yellow><size=\"55\">#1</size></color> <size=\"50\"> / {ServerManager.RemainingPlayers.Capacity - 2}</size>";
        rankResultTMP.text = "1";
        killCountResultTMP.text = killCountCounterController.KillCount.ToString();

        //  Leaderboard

        float rankpointLB = ((100 - PlayerPlacement + 1) / 100) * 20;
        float killpointLB = killCountCounterController.KillCount * 100;
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
        float killxpPoints = killCountCounterController.KillCount * ((userData.GameDetails.level / 4) + 1);
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
            { "kill", killCountCounterController.KillCount },
            { "death", PlayerPlacement != 1 ? 1 : 0 },
            { "rank", PlayerPlacement }
        }, true, (response) =>
        {
            StartCoroutine(GameManager.Instance.PostRequest("/leaderboard/updateuserleaderboard", "", new Dictionary<string, object>
            {
                { "amount", finalresultpointLB  }
            }, true, (tempresponse) => GameManager.Instance.NoBGLoading.SetActive(false), () => GameManager.Instance.NoBGLoading.SetActive(false)));
        }, () => GameManager.Instance.NoBGLoading.SetActive(false)));
    }
}
