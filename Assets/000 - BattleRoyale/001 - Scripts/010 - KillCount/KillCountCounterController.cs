using Fusion;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KillCountCounterController : NetworkBehaviour
{
    [SerializeField] private PlayerNetworkCore core;

    [Header("DEBUGGER")]
    [MyBox.ReadOnly][SerializeField] private bool isDoneInitialize;
    [field: MyBox.ReadOnly][field: SerializeField] public TextMeshProUGUI PlayerCount { get; set; } 

    public IEnumerator InitializeKillCount()
    {
        while (core.ServerManager == null || PlayerCount == null) yield return null;

        PlayerCount.text = $"{core.ServerManager.GetComponent<DedicatedServerManager>().Players.Count} / 10";

        core.ServerManager.GetComponent<DedicatedServerManager>().OnPlayerCountChange += PlayerCountChange;
    }

    private void OnDisable()
    {
        DeinitializeEvents();
    }

    private void DeinitializeEvents()
    {
        if (core.ServerManager == null) return;

        core.ServerManager.GetComponent<DedicatedServerManager>().OnPlayerCountChange -= PlayerCountChange;
    }

    private void PlayerCountChange(object sender, PlayerCountEvent e)
    {
        PlayerCount.text = $"{e.PlayerCount.ToString("n2")} / 10";
    }
}
