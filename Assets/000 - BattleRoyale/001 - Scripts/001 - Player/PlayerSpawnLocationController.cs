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

    [Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField] [Networked] public DedicatedServerManager ServerManager { get; set; }

    public async override void Spawned()
    {
        while (!Runner) await Task.Delay(100);
        while (ServerManager == null) await Task.Delay(100);
        ServerManager.OnCurrentStateChange += StateChange;
    }

    private void OnDisable()
    {
        if (ServerManager != null)
            ServerManager.OnCurrentStateChange -= StateChange;
    }

    private void StateChange(object sender, EventArgs e)
    {
        if (ServerManager.CurrentGameState == GameState.ARENA)
        {
            if (HasStateAuthority)
            {
                Debug.Log("Start dropping items");
                inventory.Rpc_DropWeaponsAfterTeleportBattlefield();
            }

            if (HasInputAuthority)
            {
                GameManager.Instance.SceneController.SpawnArenaLoading = true;
                GameManager.Instance.SceneController.AddActionLoadinList(ReadyForBattle());
                GameManager.Instance.SceneController.AddActionLoadinList(Inventory());
                GameManager.Instance.SceneController.ActionPass = true;
            }
        }
    }

    IEnumerator ReadyForBattle()
    {
        while (!ServerManager.DonePlayerBattlePositions) yield return null;
    }

    IEnumerator Inventory()
    {
        while (inventory.PrimaryWeapon != null || inventory.SecondaryWeapon != null || inventory.AmmoObject != null || inventory.WeaponIndex != 1 || inventory.BowAmmo != 0 || inventory.RifleAmmo != 0) yield return null;
    }
}