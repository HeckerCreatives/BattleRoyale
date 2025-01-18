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
    [SerializeField] public TextMeshProUGUI pingTMP;

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int KillCount { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField] public float pingChange;

    public override void Render()
    {
        if (!HasInputAuthority) return;

        if (ServerManager == null) return;

        int tempCapacity = ServerManager.RemainingPlayers.Capacity;

        PlayerCount.text = $"{ServerManager.RemainingPlayers.Count:n0} / {(tempCapacity - 2):n0}";
        killCountTMP.text = $"{KillCount:n0}";
    }

    private void LateUpdate()
    {
        if (pingChange < Time.time)
        {
            pingChange = Time.time + 1;
            pingTMP.text = $"Ping: {(Runner.GetPlayerRtt(Object.InputAuthority) * 1000):n0} (ms)";
        }
    }
}
