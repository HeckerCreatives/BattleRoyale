using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public enum InputButton
{
    Jump,
    Aim,
    SwitchHands,
    SwitchPrimary,
    SwitchSecondary,
    SwitchTrap,
    ActiveTouch,
    ArmorRepair,
    Heal,
    Reload
}

public enum HoldInputButtons
{
    Shoot,
    Sprint
}

public struct MyInput : INetworkInput
{
    public NetworkButtons Buttons;
    public NetworkButtons HoldInputButtons;
    public Vector2 MovementDirection;
    public Vector2 LookDirection;
    public Vector3 CameraHitOrigin;
    public Vector3 CameraHitDirection;
}

public class GameplayController : SimulationBehaviour, INetworkRunnerCallbacks, IBeforeUpdate
{

    //  =========================

    [SerializeField] private LayerMask cameraLayerMask;

    [Header("DEBUGGER GLOBAL")]
    [SerializeField] public Vector2 MovementDirection;
    [SerializeField] public Vector2 LookDirection;
    [SerializeField] public bool Jump;
    [SerializeField] public bool Aim;
    [SerializeField] public bool Shoot;

    [SerializeField] public bool SwitchHands;
    [SerializeField] public bool SwitchPrimary;
    [SerializeField] public bool SwitchSecondary;
    [SerializeField] public bool SwitchTrap;
    [SerializeField] public bool ArmorRepair;
    [SerializeField] public bool Heal;
    [SerializeField] public bool Reload;
    [SerializeField] public int ActiveTouch;
    [SerializeField] private bool resetInput;
    [SerializeField] private bool doneInitialize;
    [SerializeField] private Vector2 firstTouch;
    [SerializeField] private Vector3 ScreenCenterPoint;
    [SerializeField] private Vector3 CameraHitOrigin;
    [SerializeField] private Vector3 CameraHitDirection;

    //  =========================

    private GameplayInputs gameplayInputs;

    public MyInput myInput;

    Ray cameraRay;

    //  =========================

    public IEnumerator InitializeControllers()
    {
        Debug.Log("Starting To Initialize Controlls");

        while (Runner == null) yield return null;

        if (Runner != null)
        {
            myInput = new MyInput();

            gameplayInputs = new GameplayInputs();
            gameplayInputs.Enable();
            EnhancedTouchSupport.Enable();

            gameplayInputs.Gameplay.Jump.started += _ => JumpStart();
            gameplayInputs.Gameplay.Jump.canceled += _ => JumpTurnOff();
            gameplayInputs.Gameplay.Aim.started += _ => AimStart();
            gameplayInputs.Gameplay.Aim.canceled += _ => AimStop();
            gameplayInputs.Gameplay.Shoot.performed += _ => ShootStart();
            gameplayInputs.Gameplay.Shoot.canceled += _ => ShootStop();
            gameplayInputs.Gameplay.SwitchHands.started += _ => SwitchHandsStart();
            gameplayInputs.Gameplay.SwitchHands.canceled += _ => SwitchHandsStop();
            gameplayInputs.Gameplay.SwitchPrimary.started += _ => SwitchPrimaryStart();
            gameplayInputs.Gameplay.SwitchPrimary.canceled += _ => SwitchPrimaryStop();
            gameplayInputs.Gameplay.SwitchSecondary.started += _ => SwitchSecondaryStart();
            gameplayInputs.Gameplay.SwitchSecondary.canceled += _ => SwitchSecondaryStop();
            gameplayInputs.Gameplay.SwitchTrap.started += _ => SwitchTrapStart();
            gameplayInputs.Gameplay.SwitchTrap.canceled += _ => SwitchTrapStop();
            gameplayInputs.Gameplay.Heal.started += _ => HealStart();
            gameplayInputs.Gameplay.Heal.canceled += _ => HealStop();
            gameplayInputs.Gameplay.ArmorRepair.started += _ => ArmorRepairStart();
            gameplayInputs.Gameplay.ArmorRepair.canceled += _ => ArmorRepairStop();
            gameplayInputs.Gameplay.Reload.started += _ => ReloadStart();
            gameplayInputs.Gameplay.Reload.canceled += _ => ReloadStop();

            Runner.AddCallbacks(this);
        }

        doneInitialize = true;
    }

    private void OnDisable()
    {
        if (doneInitialize && Runner != null)
        {
            gameplayInputs.Gameplay.Jump.started -= _ => JumpStart();
            gameplayInputs.Gameplay.Jump.canceled -= _ => JumpTurnOff();
            gameplayInputs.Gameplay.Aim.started -= _ => AimStart();
            gameplayInputs.Gameplay.Aim.canceled -= _ => AimStop();
            gameplayInputs.Gameplay.Shoot.performed -= _ => ShootStart();
            gameplayInputs.Gameplay.Shoot.canceled -= _ => ShootStop();
            gameplayInputs.Gameplay.SwitchHands.started -= _ => SwitchHandsStart();
            gameplayInputs.Gameplay.SwitchHands.canceled -= _ => SwitchHandsStop();
            gameplayInputs.Gameplay.SwitchPrimary.started -= _ => SwitchPrimaryStart();
            gameplayInputs.Gameplay.SwitchPrimary.canceled -= _ => SwitchPrimaryStop();
            gameplayInputs.Gameplay.SwitchSecondary.started -= _ => SwitchSecondaryStart();
            gameplayInputs.Gameplay.SwitchSecondary.canceled -= _ => SwitchSecondaryStop();
            gameplayInputs.Gameplay.SwitchTrap.started -= _ => SwitchTrapStart();
            gameplayInputs.Gameplay.SwitchTrap.canceled -= _ => SwitchTrapStop();
            gameplayInputs.Gameplay.Heal.started -= _ => HealStart();
            gameplayInputs.Gameplay.Heal.canceled -= _ => HealStop();
            gameplayInputs.Gameplay.ArmorRepair.started -= _ => ArmorRepairStart();
            gameplayInputs.Gameplay.ArmorRepair.canceled -= _ => ArmorRepairStop();
            gameplayInputs.Gameplay.Reload.started -= _ => ReloadStart();
            gameplayInputs.Gameplay.Reload.canceled -= _ => ReloadStop();

            gameplayInputs.Disable();
            EnhancedTouchSupport.Disable();
            Runner.RemoveCallbacks(this);
        }
    }

    public override void FixedUpdateNetwork()
    {
        Vector2 ScreenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        cameraRay = Camera.main.ScreenPointToRay(ScreenCenter);
        CameraHitOrigin = cameraRay.origin;
        CameraHitDirection = cameraRay.direction;
    }

    #region LOCAL INPUTS

    private void JumpStart()
    {
        Jump = true;
    }

    public void JumpTurnOff()
    {
        Jump = false;
    }

    private void AimStart()
    {
        Aim = true;
    }

    private void AimStop()
    {
        Aim = false;
    }

    private void ShootStart()
    {
        Shoot = true;
    }

    public void ShootStop()
    {
        Shoot = false;
    }

    public void SwitchTrapStop()
    {
        SwitchTrap = false;
    }

    private void SwitchTrapStart()
    {
        SwitchTrap = true;
    }

    public void SwitchSecondaryStop()
    {
        SwitchSecondary = false;
    }

    private void SwitchSecondaryStart()
    {
        SwitchSecondary = true;
    }

    public void SwitchPrimaryStop()
    {
        SwitchPrimary = false;
    }

    private void SwitchPrimaryStart()
    {
        SwitchPrimary = true;
    }

    public void SwitchHandsStop()
    {
        SwitchHands = false;
    }

    private void SwitchHandsStart()
    {
        SwitchHands = true;
    }

    private void HealStart()
    {
        Heal = true;
    }

    private void HealStop()
    {
        Heal = false;
    }

    private void ArmorRepairStart()
    {
        ArmorRepair = true;
    }

    private void ArmorRepairStop()
    {
        ArmorRepair = false;
    }

    private void ReloadStart()
    {
        Reload = true;
    }

    private void ReloadStop()
    {
        Reload = false;
    }

    #endregion

    #region NETWORK

    public void BeforeUpdate()
    {
        if (resetInput)
        {
            resetInput = false;
            myInput.MovementDirection = default;
            myInput.LookDirection = default;
            myInput.Buttons = default;
        }
        // Iterate over all touches

        myInput.MovementDirection.Set(MovementDirection.x, MovementDirection.y);
        myInput.LookDirection.Set(LookDirection.x, LookDirection.y);
        myInput.CameraHitOrigin.Set(CameraHitOrigin.x, CameraHitOrigin.y, CameraHitOrigin.z);
        myInput.CameraHitDirection.Set(CameraHitDirection.x, CameraHitDirection.y, CameraHitDirection.z);
        myInput.Buttons.Set(InputButton.Jump, Jump);
        myInput.Buttons.Set(InputButton.Aim, Aim);
        myInput.Buttons.Set(InputButton.SwitchHands, SwitchHands);
        myInput.Buttons.Set(InputButton.SwitchPrimary, SwitchPrimary);
        myInput.Buttons.Set(InputButton.SwitchSecondary, SwitchSecondary);
        myInput.Buttons.Set(InputButton.SwitchTrap, SwitchTrap);
        myInput.Buttons.Set(InputButton.Heal, Heal);
        myInput.Buttons.Set(InputButton.ArmorRepair, ArmorRepair);
        myInput.Buttons.Set(InputButton.Reload, Reload);
        myInput.HoldInputButtons.Set(HoldInputButtons.Shoot, Shoot);
        myInput.HoldInputButtons.Set(HoldInputButtons.Shoot, Shoot);
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        input.Set(myInput);

        resetInput = true;
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    #endregion
}
