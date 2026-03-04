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
    [SerializeField] private PlayerController controller;

    [Space]
    [SerializeField] private AudioClip buttonClick;

    [field: Header("DEBUGGER")]
    [MyBox.ReadOnly][SerializeField] private bool isSpawned; 

    private async void OnEnable()
    {
        while (!Runner) await Task.Delay(100);

        Debug.Log("Server Manager Detected Spawn Location Controller");
        MultiplayerServerManager.Instance.OnCurrentStateChange += StateChange;

        isSpawned = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (hasState && isSpawned)
            MultiplayerServerManager.Instance.OnCurrentStateChange -= StateChange;
    }

    private void StateChange(object sender, EventArgs e)
    {
        if (MultiplayerServerManager.Instance.CurrentGameState == GameState.ARENA)
        {
            if (HasInputAuthority)
            {
                //GameManager.Instance.SceneController.SpawnArenaLoading = true;
                GameManager.Instance.SceneController.AddActionLoadinList(ResetControllerMovement());
                GameManager.Instance.SceneController.AddActionLoadinList(DropWeaponOnMoveBattleField());
                GameManager.Instance.SceneController.AddActionLoadinList(ReadyForBattle());
                GameManager.Instance.SceneController.ActionPass = true;
            }
        }
    }

    IEnumerator DropWeaponOnMoveBattleField()
    {
        if (inventory.PrimaryWeapon != null) inventory.PrimaryWeapon.RPC_DropPrimaryWeaponOnMoveBattle();

        if (inventory.SecondaryWeapon != null) inventory.SecondaryWeapon.RPC_DropSecondaryWeaponOnMoveBattle();

        if (inventory.Shield != null) inventory.Shield.RPC_DropShieldOnMoveBattle();

        inventory.RPC_DropHealRepair();

        yield return null;
    }

    IEnumerator ResetControllerMovement()
    {
        controller.RPC_ResetMovementOnMoveBattle();

        yield return null;
    }

    IEnumerator ReadyForBattle()
    {
        while (!MultiplayerServerManager.Instance.DonePlayerBattlePositions || inventory.PrimaryWeapon != null || inventory.SecondaryWeapon != null || inventory.Shield != null) yield return null;
    }

    public void PlayBtnSound() => GameManager.Instance.AudioController.PlaySFX(buttonClick);
}