using Fusion;
using Fusion.Sockets;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UIElements;

public enum InputButton
{
    Jump,
    Aim,
    Melee,
    Crouch,
    Prone,
    SwitchHands,
    SwitchPrimary,
    SwitchSecondary,
    SwitchTrap
}

public enum HoldInputButtons
{
    Shoot
}

public struct MyInput : INetworkInput
{
    public NetworkButtons Buttons;
    public NetworkButtons HoldInputButtons;
    public Vector2 MovementDirection;
    public Vector2 LookDirection;
    public Vector2 PointerPosition;
}

public class GameplayController : SimulationBehaviour, INetworkRunnerCallbacks, IBeforeUpdate
{

    //  =========================

    [Header("DEBUGGER")]
    [MyBox.ReadOnly][SerializeField] private bool cursorLocked = true;

    [Header("DEBUGGER GLOBAL")]
    [MyBox.ReadOnly][SerializeField] public Vector2 MovementDirection;
    [MyBox.ReadOnly][SerializeField] public Vector2 LookDirection;
    [MyBox.ReadOnly][SerializeField] public bool Jump;
    [MyBox.ReadOnly][SerializeField] public bool Aim;
    [MyBox.ReadOnly][SerializeField] public bool Shoot;
    [MyBox.ReadOnly][SerializeField] public bool Crouch;
    [MyBox.ReadOnly][SerializeField] public bool Prone;
    [MyBox.ReadOnly][SerializeField] public bool SwitchHands;
    [MyBox.ReadOnly][SerializeField] public bool SwitchPrimary;
    [MyBox.ReadOnly][SerializeField] public bool SwitchSecondary;
    [MyBox.ReadOnly][SerializeField] public bool SwitchTrap;
    [MyBox.ReadOnly][SerializeField] private bool resetInput;
    [MyBox.ReadOnly][SerializeField] private bool doneInitialize;



    //  =========================

    private GameplayInputs gameplayInputs;
    private Dictionary<int, bool> touchOverUI = new Dictionary<int, bool>();

    MyInput myInput;

    //  =========================

    public IEnumerator InitializeControllers()
    {
        Debug.Log("Starting To Initialize Controlls");

        while (Runner == null) yield return null;

        Debug.Log($"Is Runner null: {Runner == null}  Is Clien: {Runner.IsClient}");

        if (Runner != null)
        {
            myInput = new MyInput();

            Debug.Log($"Starting init controls");
            gameplayInputs = new GameplayInputs();
            gameplayInputs.Enable();

            gameplayInputs.Gameplay.Movement.performed += _ => MovementStart();
            gameplayInputs.Gameplay.Movement.canceled += _ => MovementStop();
            gameplayInputs.Gameplay.Jump.started += _ => JumpStart();
            gameplayInputs.Gameplay.Jump.canceled += _ => JumpTurnOff();
            gameplayInputs.Gameplay.Aim.started += _ => AimStart();
            gameplayInputs.Gameplay.Aim.canceled += _ => AimStop();
            gameplayInputs.Gameplay.Shoot.started += _ => ShootStart();
            gameplayInputs.Gameplay.Shoot.canceled += _ => ShootStop();
            gameplayInputs.Gameplay.Crouch.started += _ => CrouchStart();
            gameplayInputs.Gameplay.Crouch.canceled += _ => CrouchStop();
            gameplayInputs.Gameplay.Prone.started += _ => ProneStart();
            gameplayInputs.Gameplay.Prone.canceled += _ => ProneStop();
            gameplayInputs.Gameplay.SwitchHands.started += _ => SwitchHandsStart();
            gameplayInputs.Gameplay.SwitchHands.canceled += _ => SwitchHandsStop();
            gameplayInputs.Gameplay.SwitchPrimary.started += _ => SwitchPrimaryStart();
            gameplayInputs.Gameplay.SwitchPrimary.canceled += _ => SwitchPrimaryStop();
            gameplayInputs.Gameplay.SwitchSecondary.started += _ => SwitchSecondaryStart();
            gameplayInputs.Gameplay.SwitchSecondary.canceled += _ => SwitchSecondaryStop();
            gameplayInputs.Gameplay.SwitchTrap.started += _ => SwitchTrapStart();
            gameplayInputs.Gameplay.SwitchTrap.canceled += _ => SwitchTrapStop();

#if !UNITY_ANDROID
            gameplayInputs.Gameplay.Look.performed += _ => LookStart();
            gameplayInputs.Gameplay.Look.canceled += _ => LookStop();
#endif

            Runner.AddCallbacks(this);
            Debug.Log($"Done init controls");
        }

        doneInitialize = true;
    }

    private void OnDisable()
    {
        if (doneInitialize && Runner != null)
        {
            gameplayInputs.Gameplay.Movement.performed -= _ => MovementStart();
            gameplayInputs.Gameplay.Movement.canceled -= _ => MovementStop();
            gameplayInputs.Gameplay.Jump.started -= _ => JumpStart();
            gameplayInputs.Gameplay.Jump.canceled -= _ => JumpTurnOff();
            gameplayInputs.Gameplay.Aim.started -= _ => AimStart();
            gameplayInputs.Gameplay.Aim.canceled -= _ => AimStop();
            gameplayInputs.Gameplay.Shoot.started -= _ => ShootStart();
            gameplayInputs.Gameplay.Shoot.canceled -= _ => ShootStop();
            gameplayInputs.Gameplay.Crouch.started -= _ => CrouchStart();
            gameplayInputs.Gameplay.Crouch.canceled -= _ => CrouchStop();
            gameplayInputs.Gameplay.Prone.started += _ => ProneStart();
            gameplayInputs.Gameplay.Prone.canceled += _ => ProneStop();
            gameplayInputs.Gameplay.SwitchHands.started -= _ => SwitchHandsStart();
            gameplayInputs.Gameplay.SwitchHands.canceled -= _ => SwitchHandsStop();
            gameplayInputs.Gameplay.SwitchPrimary.started -= _ => SwitchPrimaryStart();
            gameplayInputs.Gameplay.SwitchPrimary.canceled -= _ => SwitchPrimaryStop();
            gameplayInputs.Gameplay.SwitchSecondary.started -= _ => SwitchSecondaryStart();
            gameplayInputs.Gameplay.SwitchSecondary.canceled -= _ => SwitchSecondaryStop();
            gameplayInputs.Gameplay.SwitchTrap.started -= _ => SwitchTrapStart();
            gameplayInputs.Gameplay.SwitchTrap.canceled -= _ => SwitchTrapStop();

#if !UNITY_ANDROID
            gameplayInputs.Gameplay.Look.performed -= _ => LookStart();
            gameplayInputs.Gameplay.Look.canceled -= _ => LookStop();
#endif

            gameplayInputs.Disable();
            Runner.RemoveCallbacks(this);
        }
    }

    #region LOCAL INPUTS

#if !UNITY_ANDROID

    private void SetCursorState(bool newState)
    {
        UnityEngine.Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }

#endif

    private void LookStart()
    {
        LookDirection = gameplayInputs.Gameplay.Look.ReadValue<Vector2>();
    }

    private void LookStop()
    {
        LookDirection = Vector2.zero;
    }

    private void MovementStart()
    {
        MovementDirection = gameplayInputs.Gameplay.Movement.ReadValue<Vector2>();
    }

    private void MovementStop()
    {
        MovementDirection = Vector2.zero;
    }

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
        myInput.HoldInputButtons = default;
    }

    private void CrouchStart()
    {
        Crouch = true;
    }

    public void CrouchStop()
    {
        Crouch = false;
    }

    private void ProneStart()
    {
        Prone = true;
    }

    public void ProneStop()
    {
        Prone = false;
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

    #endregion

    #region NETWORK

    public void BeforeUpdate()
    {
        if (resetInput)
        {
            resetInput = false;
            myInput.MovementDirection = default;
            myInput.Buttons = default;
        }

#if !UNITY_ANDROID
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
        {

        }
#endif

#if UNITY_ANDROID
        foreach (var touch in Touchscreen.current.touches)
        {
            int touchId = touch.touchId.ReadValue();

            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                touchOverUI[touchId] = EventSystem.current.IsPointerOverGameObject(touchId);

            if (touchOverUI.ContainsKey(touchId) && touchOverUI[touchId])
                continue;

            myInput.LookDirection += touch.delta.ReadValue();
        }
#endif

        myInput.MovementDirection.Set(MovementDirection.x, MovementDirection.y);

#if !UNITY_ANDROID

        myInput.LookDirection += LookDirection;
#endif

        myInput.Buttons.Set(InputButton.Jump, Jump);
        myInput.Buttons.Set(InputButton.Aim, Aim);
        myInput.Buttons.Set(InputButton.Melee, Shoot);
        myInput.Buttons.Set(InputButton.Crouch, Crouch);
        myInput.Buttons.Set(InputButton.Prone, Prone);
        myInput.Buttons.Set(InputButton.SwitchHands, SwitchHands);
        myInput.Buttons.Set(InputButton.SwitchPrimary, SwitchPrimary);
        myInput.Buttons.Set(InputButton.SwitchSecondary, SwitchSecondary);
        myInput.Buttons.Set(InputButton.SwitchTrap, SwitchTrap);
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
#if !UNITY_ANDROID
        if (player == runner.LocalPlayer)
        {
            //SetCursorState(true);
        }
#endif
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        input.Set(myInput);

        resetInput = true;
        //myInput.MovementDirection = default;
        myInput.LookDirection = default;
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
#if !UNITY_ANDROID
        //SetCursorState(false);
#endif
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
