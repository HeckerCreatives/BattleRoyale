using Fusion;
using Fusion.Sockets;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public enum InputButton
{
    Jump,
    Aim,
    Shoot,
    Crouch,
    Prone,
    SwitchHands,
    SwitchPrimary,
    SwitchSecondary,
    SwitchTrap
}

public struct MyInput : INetworkInput
{
    public NetworkButtons Buttons;
    public Vector2 MovementDirection;
    public Vector2 LookDirection;
}

public class GameplayController : SimulationBehaviour, INetworkRunnerCallbacks, IBeforeUpdate
{

    //  =========================

    [Header("SETTINGS")]
    [SerializeField] private float jumpHoldTime;

    [Header("DEBUGGER")]
    [SerializeField] private float currentJumpTime;
    [SerializeField] private bool cursorLocked = true;

    [field: Header("DEBUGGER GLOBAL")]
    [field: MyBox.ReadOnly][field: SerializeField] public Vector2 MovementDirection { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField] public Vector2 LookDirection { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField] public bool Jump { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField] public bool Aim { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField] public bool Shoot { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField] public bool Crouch { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField] public bool Prone { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField] public bool SwitchHands { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField] public bool SwitchPrimary { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField] public bool SwitchSecondary { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField] public bool SwitchTrap { get; private set; }


    //  =========================

    private GameplayInputs gameplayInputs;

    //  =========================

    private async void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SceneController.ActionPass = true;

        Debug.Log("Starting To Initialize Controlls");

        while (Runner == null) await Task.Delay(100);

        Debug.Log($"Is Runner null: {Runner == null}  Is Clien: {Runner.IsClient}");

        if (Runner != null)
        {
            Debug.Log($"Starting init controls");
            gameplayInputs = new GameplayInputs();
            gameplayInputs.Enable();

            gameplayInputs.Gameplay.Movement.performed += _ => MovementStart();
            gameplayInputs.Gameplay.Movement.canceled += _ => MovementStop();
            gameplayInputs.Gameplay.Jump.started += _ => JumpStart();
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

#if UNITY_EDITOR
            gameplayInputs.Gameplay.Look.performed += _ => LookStart();
            gameplayInputs.Gameplay.Look.canceled += _ => LookStop();
#endif

            Runner.AddCallbacks(this);
            Debug.Log($"Done init controls");
        }
    }

    private void OnDisable()
    {
        if (Runner != null)
        {
            gameplayInputs.Gameplay.Movement.performed -= _ => MovementStart();
            gameplayInputs.Gameplay.Movement.canceled -= _ => MovementStop();
            gameplayInputs.Gameplay.Jump.started -= _ => JumpStart();
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

#if UNITY_EDITOR
            gameplayInputs.Gameplay.Look.performed -= _ => LookStart();
            gameplayInputs.Gameplay.Look.canceled -= _ => LookStop();
#endif

            gameplayInputs.Disable();
            Runner.RemoveCallbacks(this);
        }
    }

    #region LOCAL INPUTS

#if !UNITY_IOS || !UNITY_ANDROID

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
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
        currentJumpTime = Time.time;
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

    private void ShootStop()
    {
        Shoot = false;
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
        var myInput = new MyInput();

        myInput.MovementDirection.Set(MovementDirection.x, MovementDirection.y);
        myInput.LookDirection.Set(LookDirection.x, LookDirection.y);
        myInput.Buttons.Set(InputButton.Jump, Jump);
        myInput.Buttons.Set(InputButton.Aim, Aim);
        myInput.Buttons.Set(InputButton.Shoot, Shoot);
        myInput.Buttons.Set(InputButton.Crouch, Crouch);
        myInput.Buttons.Set(InputButton.Prone, Prone);
        myInput.Buttons.Set(InputButton.SwitchHands, SwitchHands);
        myInput.Buttons.Set(InputButton.SwitchPrimary, SwitchPrimary);
        myInput.Buttons.Set(InputButton.SwitchSecondary, SwitchSecondary);
        myInput.Buttons.Set(InputButton.SwitchTrap, SwitchTrap);

        input.Set(myInput);

        myInput.LookDirection = default;
        myInput.MovementDirection = default;
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
