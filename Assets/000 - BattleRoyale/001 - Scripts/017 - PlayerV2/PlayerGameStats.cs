using Fusion;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class PlayerGameStats : NetworkBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private PlayerHealthV2 playerhealth;
    [SerializeField] private PlayerOwnObjectEnabler ownObjectEnabler;

    [Header("GAME STATUS")]
    [SerializeField] public TextMeshProUGUI pingTMP;
    [SerializeField] public TextMeshProUGUI playtimeTMP;

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
    [field: SerializeField][Networked] public int KillCount { get; set; }
    [field: SerializeField][Networked] public float HitPoints { get; set; }
    [field: SerializeField][Networked] public int PlayerPlacement { get; set; }
    [field: SerializeField][Networked] public float playtime { get; set; }

    // ✅ NEW: server-side send-once guards (Networked so server is source of truth)
    [field: SerializeField][Networked] public bool ResultsSent { get; set; }
    [field: SerializeField][Networked] public bool ResultsSending { get; set; }

    private ChangeDetector _changeDetector;

    // local-only cache so we don't re-run UI when detector is called elsewhere
    private int _lastPlacementUI = -1;

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

        GameStats();
        DeathGameOver_UIOnly();  // ✅ UI-only: no server requests here
    }

    public override void FixedUpdateNetwork()
    {
        // ✅ server-side logic lives here
        ServerSendScoreOnDeath_Once();

        if (HasStateAuthority && !playerhealth.IsDead)
            playtime += Runner.DeltaTime;
    }

    private void Update()
    {
        QuitOnShowStatusCountdown();
    }

    private void LateUpdate()
    {
        if (!HasInputAuthority) return;
        if (Runner == null) return;

        if (pingChange < Time.time)
        {
            pingChange = Time.time + 1;
            pingTMP.text = $"Ping: {(Runner.GetPlayerRtt(Object.InputAuthority) * 1000):n0} (ms)";
        }
    }

    private async void RegisterServerEvents()
    {
        // You only need these once
        PlayerJoinedController.Instance.OnPlayerCountChange += PlayerCountChange;
        MultiplayerServerManager.Instance.OnCurrentStateChange += GameStateChange;
    }

    private void OnDisable()
    {
        // ✅ avoid event leak if object gets disabled/despawned
        if (Runner)
        {
            PlayerJoinedController.Instance.OnPlayerCountChange -= PlayerCountChange;
            MultiplayerServerManager.Instance.OnCurrentStateChange -= GameStateChange;
        }
    }

    private void GameStats()
    {
        int tempCapacity = PlayerJoinedController.Instance.RemainingPlayers.Capacity;

        PlayerCount.text = $"{(PlayerJoinedController.Instance.RemainingPlayers.Count + BotSpawnerController.Instance.Bots.Count):n0} / {(tempCapacity - 2):n0}";
        killCountTMP.text = $"{KillCount:n0}";
        playtimeTMP.text = $"Playtime: {GameManager.Instance.GetMinuteSecondsTime(playtime)}";
    }

    #region GAME OVER UI (InputAuthority only)

    // ✅ UI-only placement change detection (no HTTP here)
    private void DeathGameOver_UIOnly()
    {
        if (PlayerPlacement <= 0) return;
        if (IsDoneShowingGameOver) return;

        // avoid repeated UI work
        if (_lastPlacementUI == PlayerPlacement) return;
        _lastPlacementUI = PlayerPlacement;

        IsDoneShowingGameOver = true;

        usernameResultTMP.text = userData.Username;
        int botsCount = BotSpawnerController.Instance.Bots.Count;
        int rank = PlayerPlacement + botsCount;

        playerCountResultTMP.text =
            $"<color=yellow><size=\"55\">#{rank}</size></color> <size=\"50\"> / {PlayerJoinedController.Instance.RemainingPlayers.Capacity - 2}</size>";

        rankResultTMP.text = rank.ToString();
        killCountResultTMP.text = KillCount.ToString();

        // leaderboard points
        float rankpointLB = ((100 - rank + 1) / 100f) * 20f;
        rankpointLB = (float)Math.Truncate(rankpointLB);

        float killpointLB = KillCount * 100f;
        killpointLB = (float)Math.Truncate(killpointLB);

        float finalresultpointLB = rankpointLB + killpointLB + HitPoints;
        finalresultpointLB = (float)Math.Truncate(finalresultpointLB);

        rankPointResultTMP.text = rankpointLB.ToString("n0");
        killPointsTMP.text = killpointLB.ToString("n0");
        hitPointResultTMP.text = HitPoints.ToString("n0");
        resultPointsTMP.text = finalresultpointLB.ToString("n0");

        // EXP
        float userlevelPoints = (userData.GameDetails.level / 2f) * 3f;
        userlevelPoints = (float)Math.Truncate(userlevelPoints);

        float rankxpPoints = ((100 - PlayerPlacement + 1) / 100f) * 20f;
        rankxpPoints = (float)Math.Truncate(rankxpPoints);

        float killxpPoints = KillCount * ((userData.GameDetails.level / 4f) + 1f);
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

        canQuit = true;
    }

    #endregion

    #region SERVER SEND RESULTS (StateAuthority only, send-once)

    // ✅ Single entry point for sending results (guarded)
    private void TrySendResultsOnce(string reason, bool isWinner, int rankOverride = -1)
    {
        if (!HasStateAuthority) return;

        if (ResultsSent || ResultsSending) return; // ✅ hard guard
        ResultsSending = true;

        int botsCount = BotSpawnerController.Instance.Bots.Count;
        int rank = (rankOverride != -1)
            ? rankOverride
            : (PlayerPlacement + botsCount);

        float rankpointLB = ((100 - rank + 1) / 100f) * 20f;
        rankpointLB = (float)Math.Truncate(rankpointLB);

        float killpointLB = KillCount * 100f;
        killpointLB = (float)Math.Truncate(killpointLB);

        float finalresultpointLB = rankpointLB + killpointLB + HitPoints;
        finalresultpointLB = (float)Math.Truncate(finalresultpointLB);

        Debug.Log($"✅ SENDING RESULTS ONCE ({reason}) for {ownObjectEnabler.Username} rank={rank}");

        StartCoroutine(MultiplayerServerManager.Instance.PostRequest("/usergamedetail/updatebyserverusergamedetails", "", new Dictionary<string, object>
        {
            { "kill", KillCount },
            { "death", isWinner ? 0 : 1 },
            { "rank", rank },
            { "win", isWinner ? 1 : 0 },
            { "loss", isWinner ? 0 : 1 }, // ✅ FIXED (winner shouldn't be loss=1)
            { "playtime", playtime },
            { "username", ownObjectEnabler.Username },
            { "id", ownObjectEnabler.UserID }
        }, true, (response) =>
        {
            StartCoroutine(MultiplayerServerManager.Instance.PostRequest("/leaderboard/updateuserleaderboard", "", new Dictionary<string, object>
            {
                { "amount", finalresultpointLB },
                { "username", ownObjectEnabler.Username },
                { "id", ownObjectEnabler.UserID }
            }, true, (tempresponse) =>
            {
                ResultsSent = true;
                ResultsSending = false;

                SocketServerController.Instance.Socket.Emit("serverremovereconnect", JsonConvert.SerializeObject(
                    new Dictionary<string, string>() { { "username", ownObjectEnabler.Username.Value } }
                ));
            },
            // error callback
            () =>
            {
                Debug.LogError($"❌ leaderboard update failed ({reason})");
                ResultsSending = false; // allow retry if you want
            }));
        },
        // error callback
        () =>
        {
            Debug.LogError($"❌ usergamedetail update failed ({reason})");
            ResultsSending = false; // allow retry if you want
        }));
    }

    // ✅ Death path (placement assigned). This runs in FixedUpdateNetwork only.
    private void ServerSendScoreOnDeath_Once()
    {
        if (!HasStateAuthority) return;

        // Only attempt when placement is set AND player is dead
        if (PlayerPlacement <= 0) return;
        if (!playerhealth.IsDead) return;

        // If placement is 1 but dead, still treat as loss (your game logic)
        TrySendResultsOnce("PlayerPlacement set (death)", isWinner: false);
    }

    private void WinnerSendPointsByServer_Once()
    {
        if (!HasStateAuthority) return;

        if (MultiplayerServerManager.Instance.CurrentGameState != GameState.DONE) return;
        if (playerhealth.IsDead) return;

        TrySendResultsOnce("GameState DONE (winner)", isWinner: true, rankOverride: 1);
    }

    private void AllQuitWinnerSend_Once()
    {
        if (!HasStateAuthority) return;

        if (MultiplayerServerManager.Instance.CurrentGameState != GameState.ARENA) return;
        if (playerhealth.IsDead) return;

        if (PlayerJoinedController.Instance.RemainingPlayers.Count > 1 || BotSpawnerController.Instance.Bots.Count > 0) return;

        TrySendResultsOnce("All players quit (server winner)", isWinner: true, rankOverride: 1);
    }

    #endregion

    #region EVENTS

    private void PlayerCountChange(object sender, EventArgs e)
    {
        // UI
        ShowWinnerOnAllPlayerQuit_UI();

        // Server
        AllQuitWinnerSend_Once();
    }

    private void GameStateChange(object sender, EventArgs e)
    {
        // UI
        ShowWinner_UI();

        // Server
        WinnerSendPointsByServer_Once();
    }

    private void ShowWinner_UI()
    {
        if (!HasInputAuthority) return;
        if (winMessageObj.activeInHierarchy) return;
        if (IsDoneShowingGameOver) return;

        if (MultiplayerServerManager.Instance.CurrentGameState == GameState.DONE)
        {
            IsDoneShowingGameOver = true;

            usernameResultTMP.text = userData.Username;
            playerCountResultTMP.text = $"<color=yellow><size=\"55\">#1</size></color> <size=\"50\"> / {PlayerJoinedController.Instance.RemainingPlayers.Capacity - 2}</size>";
            rankResultTMP.text = "1";
            killCountResultTMP.text = KillCount.ToString();

            float rankpointLB = ((100 - (PlayerPlacement + BotSpawnerController.Instance.Bots.Count) + 1) / 100f) * 20f;
            rankpointLB = (float)Math.Truncate(rankpointLB);

            float killpointLB = KillCount * 100f;
            killpointLB = (float)Math.Truncate(killpointLB);

            float finalresultpointLB = rankpointLB + killpointLB + HitPoints;
            finalresultpointLB = (float)Math.Truncate(finalresultpointLB);

            rankPointResultTMP.text = rankpointLB.ToString("n0");
            killPointsTMP.text = killpointLB.ToString("n0");
            hitPointResultTMP.text = HitPoints.ToString("n0");
            resultPointsTMP.text = finalresultpointLB.ToString("n0");

            float userlevelPoints = (userData.GameDetails.level / 2f) * 3f;
            userlevelPoints = (float)Math.Truncate(userlevelPoints);

            float rankxpPoints = ((100 - PlayerPlacement + 1) / 100f) * 20f;
            rankxpPoints = (float)Math.Truncate(rankxpPoints);

            float killxpPoints = KillCount * ((userData.GameDetails.level / 4f) + 1f);
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

            canQuit = true;
        }
    }

    private void ShowWinnerOnAllPlayerQuit_UI()
    {
        if (!HasInputAuthority) return;
        if (MultiplayerServerManager.Instance.CurrentGameState != GameState.ARENA) return;
        if (PlayerJoinedController.Instance.RemainingPlayers.Count > 1 || BotSpawnerController.Instance.Bots.Count > 0) return;
        if (winMessageObj.activeInHierarchy) return;
        if (IsDoneShowingGameOver) return;

        IsDoneShowingGameOver = true;

        usernameResultTMP.text = userData.Username;
        playerCountResultTMP.text = $"<color=yellow><size=\"55\">#1</size></color> <size=\"50\"> / {PlayerJoinedController.Instance.RemainingPlayers.Capacity - 2}</size>";
        rankResultTMP.text = "1";
        killCountResultTMP.text = KillCount.ToString();

        float rankpointLB = ((100 - (PlayerPlacement + BotSpawnerController.Instance.Bots.Count) + 1) / 100f) * 20f;
        rankpointLB = (float)Math.Truncate(rankpointLB);

        float killpointLB = KillCount * 100f;
        killpointLB = (float)Math.Truncate(killpointLB);

        float finalresultpointLB = rankpointLB + killpointLB + HitPoints;
        finalresultpointLB = (float)Math.Truncate(finalresultpointLB);

        rankPointResultTMP.text = rankpointLB.ToString("n0");
        killPointsTMP.text = killpointLB.ToString("n0");
        hitPointResultTMP.text = HitPoints.ToString("n0");
        resultPointsTMP.text = finalresultpointLB.ToString("n0");

        float userlevelPoints = (userData.GameDetails.level / 2f) * 3f;
        userlevelPoints = (float)Math.Truncate(userlevelPoints);

        float rankxpPoints = ((100 - PlayerPlacement + 1) / 100f) * 20f;
        rankxpPoints = (float)Math.Truncate(rankxpPoints);

        float killxpPoints = KillCount * ((userData.GameDetails.level / 4f) + 1f);
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

        canQuit = true;
    }

    #endregion

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
}