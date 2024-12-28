using Fusion;
using Fusion.Addons.SimpleKCC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerSpawnLocationController : NetworkBehaviour
{
    [SerializeField] public PlayerInventory inventory;

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField] [Networked] public DedicatedServerManager ServerManager { get; set; }
    [MyBox.ReadOnly][SerializeField] private bool isSpawned; 

    private async void OnEnable()
    {
        while (!Runner) await Task.Delay(100);

        while (ServerManager == null)
        {
            Debug.Log("Waiting for server manager PlayerSpawnLoactaionController");
            await Task.Delay(100);
        }

        Debug.Log("Server Manager Detected Spawn Location Controller");
        ServerManager.OnCurrentStateChange += StateChange;

        isSpawned = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (hasState && isSpawned)
            ServerManager.OnCurrentStateChange -= StateChange;
    }

    private void StateChange(object sender, EventArgs e)
    {
        if (ServerManager.CurrentGameState == GameState.ARENA)
        {
            if (HasInputAuthority)
            {
                GameManager.Instance.SceneController.SpawnArenaLoading = true;
                GameManager.Instance.SceneController.AddActionLoadinList(ReadyForBattle());
                GameManager.Instance.SceneController.ActionPass = true;
            }
        }
    }

    IEnumerator ReadyForBattle()
    {
        while (!ServerManager.DonePlayerBattlePositions) yield return null;
    }
}