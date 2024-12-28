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

    [field: Header("DEBUGGER")]
    [field: SerializeField] public float RankPoints { get; set; }
    [field: SerializeField] public float HitPoints { get; set; }
    [field: SerializeField][Networked] public int PlayerPlacement { get; set; }
    [field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }

    //  ======================

    private ChangeDetector _changeDetector;

    //  ======================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override async void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(PlayerPlacement):

                    if (!HasInputAuthority) return;

                    //  THIS IS FOR DEATH

                    Debug.Log($"Player lose, player placement: {PlayerPlacement}");
                    usernameResultTMP.text = userData.Username;
                    playerCountResultTMP.text = $"<color=yellow><size=\"55\">#{PlayerPlacement}</size></color> <size=\"50\"> / {ServerManager.RemainingPlayers.Capacity}</size>";
                    rankResultTMP.text = PlayerPlacement.ToString();
                    killCountResultTMP.text = killCountCounterController.KillCount.ToString();
                    rankPointResultTMP.text = RankPoints.ToString("n0");
                    killPointsTMP.text = (killCountCounterController.KillCount * 100).ToString("n0");
                    hitPointResultTMP.text = HitPoints.ToString("n0");
                    resultPointsTMP.text = (RankPoints + (killCountCounterController.KillCount * 100) + HitPoints).ToString("n0");

                    controllerObj.SetActive(false);
                    pauseObj.SetActive(false);

                    winMessageObj.SetActive(false);
                    loseMessageObj.SetActive(true);
                    gameOverPanelObj.SetActive(true);

                    break;
            }
        }
    }

    private async void OnEnable()
    {
        while (!Runner)
        {
            Debug.Log("waiting runner on kill count counter");
            await Task.Delay(100);
        }

        if (HasInputAuthority)
        {
            while (!ServerManager)
            {
                Debug.Log("waiting on server manager kill count counter");
                await Task.Delay(100);
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
                ServerManager.OnPlayerCountChange -= PlayerCountChange;
            ServerManager.OnCurrentStateChange -= GameStateChange;
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
        if (playerHealth.CurrentHealth <= 0) return;

        if (winMessageObj.activeInHierarchy) return;

        if (ServerManager.CurrentGameState == GameState.DONE)
        {
            await Task.Delay(1500);

            usernameResultTMP.text = userData.Username;
            playerCountResultTMP.text = $"<color=yellow><size=\"55\">#1</size></color> <size=\"50\"> / {ServerManager.RemainingPlayers.Capacity - 1}</size>";
            rankResultTMP.text = "1";
            killCountResultTMP.text = killCountCounterController.KillCount.ToString();
            rankPointResultTMP.text = RankPoints.ToString("n0");
            killPointsTMP.text = (killCountCounterController.KillCount * 100).ToString("n0");
            hitPointResultTMP.text = HitPoints.ToString("n0");
            resultPointsTMP.text = (RankPoints + (killCountCounterController.KillCount * 100) + HitPoints).ToString("n0");

            controllerObj.SetActive(false);
            pauseObj.SetActive(false);

            winMessageObj.SetActive(true);
            loseMessageObj.SetActive(false);
            gameOverPanelObj.SetActive(true);
        }
    }

    private async void ShowWinnerOnAllPlayerQuit()
    {
        if (ServerManager.CurrentGameState != GameState.ARENA) return;

        if (ServerManager.RemainingPlayers.Count > 1) return;

        if (winMessageObj.activeInHierarchy) return;

        await Task.Delay(1500);

        usernameResultTMP.text = userData.Username;
        playerCountResultTMP.text = $"<color=yellow><size=\"55\">#1</size></color> <size=\"50\"> / {ServerManager.RemainingPlayers.Capacity - 1}</size>";
        rankResultTMP.text = "1";
        killCountResultTMP.text = killCountCounterController.KillCount.ToString();
        rankPointResultTMP.text = RankPoints.ToString("n0");
        killPointsTMP.text = (killCountCounterController.KillCount * 100).ToString("n0");
        hitPointResultTMP.text = HitPoints.ToString("n0");
        resultPointsTMP.text = (RankPoints + (killCountCounterController.KillCount * 100) + HitPoints).ToString("n0");

        controllerObj.SetActive(false);
        pauseObj.SetActive(false);

        winMessageObj.SetActive(true);
        loseMessageObj.SetActive(false);
        gameOverPanelObj.SetActive(true);
    }
}
