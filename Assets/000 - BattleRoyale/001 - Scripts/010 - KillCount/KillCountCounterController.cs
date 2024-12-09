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

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int KillCount { get; set; }

    public override void Render()
    {
        if (!HasInputAuthority) return;

        if (ServerManager == null) return;

        PlayerCount.text = $"{ServerManager.Players.Count:n0} / {ServerManager.Players.Capacity:n0}";
        killCountTMP.text = $"{KillCount:n0}";
    }

    private async void OnEnable()
    {
        if (HasInputAuthority)
        {
            while (!ServerManager) await Task.Delay(100);

            ServerManager.OnPlayerCountChange += PlayerCountChange;
        }
    }

    private void OnDisable()
    {
        if (HasInputAuthority)
        {
            if (ServerManager != null)
                ServerManager.OnPlayerCountChange -= PlayerCountChange;
        }
    }

    private async void PlayerCountChange(object sender, EventArgs e)
    {
        if (ServerManager.StartBattleRoyale)
        {
            if (ServerManager.RemainingPlayers.Count <= 1)
            {
                await Task.Delay(1500);

                usernameResultTMP.text = userData.Username;
                playerCountResultTMP.text = $"<color=yellow><size=\"55\">#1</size></color> <size=\"50\"> / {ServerManager.RemainingPlayers.Count}</size>";
                rankResultTMP.text = "1";

                controllerObj.SetActive(false);
                pauseObj.SetActive(false);

                winMessageObj.SetActive(true);
                loseMessageObj.SetActive(false);
                gameOverPanelObj.SetActive(true);
            }
        }
    }
}
