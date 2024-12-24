using Fusion;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class KillCountCounterController : NetworkBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private PlayerHealth playerHealth;

    [Space]
    [SerializeField] public TextMeshProUGUI PlayerCount;
    [SerializeField] public TextMeshProUGUI killCountTMP;

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

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int KillCount { get; set; }

    public override void Render()
    {
        if (!HasInputAuthority) return;

        if (ServerManager == null) return;

        PlayerCount.text = $"{ServerManager.Players.Count:n0} / {ServerManager.Players.Capacity - 1:n0}";
        killCountTMP.text = $"{KillCount:n0}";
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
            killCountResultTMP.text = KillCount.ToString();

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
        killCountResultTMP.text = KillCount.ToString();

        controllerObj.SetActive(false);
        pauseObj.SetActive(false);

        winMessageObj.SetActive(true);
        loseMessageObj.SetActive(false);
        gameOverPanelObj.SetActive(true);
    }
}
