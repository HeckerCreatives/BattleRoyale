using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnLocationController : NetworkBehaviour
{
    [SerializeField] private PlayerNetworkCore networkCore;

    [Header("DEBUGGER")]
    [MyBox.ReadOnly][SerializeField] private bool isInitialized;
    [MyBox.ReadOnly][SerializeField] private bool doneWaitingArea;
    [MyBox.ReadOnly][SerializeField] private bool localDoneWaitingArea;
    [MyBox.ReadOnly][SerializeField] private float tempTime;
    [MyBox.ReadOnly][SerializeField] public PlayerInventory inventory;
    [field: MyBox.ReadOnly][field: SerializeField] public DedicatedServerManager ServerManager { get; set; }

    public IEnumerator InitializeSpawnLocation()
    {
        while (ServerManager == null || inventory == null) yield return null;
        tempTime = 0.5f;
        isInitialized = true;
    }

    public override void FixedUpdateNetwork()
    {
    }

    private void Update()
    {
        if (HasInputAuthority)
        {
            if (isInitialized)
            {
                if (!localDoneWaitingArea && ServerManager.CurrentGameState == GameState.ARENA)
                {
                    GameManager.Instance.SceneController.SpawnArenaLoading = true;
                    GameManager.Instance.SceneController.AddActionLoadinList(Inventory());
                    GameManager.Instance.SceneController.AddActionLoadinList(ReadyForBattle());
                    GameManager.Instance.SceneController.ActionPass = true;

                    inventory.Rpc_DropWeaponsAfterTeleportBattlefield();
                    localDoneWaitingArea = true;
                }
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