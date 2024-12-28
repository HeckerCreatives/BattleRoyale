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

}
