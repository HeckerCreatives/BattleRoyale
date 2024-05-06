using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameplayController : MonoBehaviour
{

    //  =========================

    [Header("SETTINGS")]
    [SerializeField] private float jumpHoldTime;

    [Header("DEBUGGER")]
    [SerializeField] private float currentJumpTime;
    [SerializeField] private bool cursorLocked = true;

    [field: Header("DEBUGGER GLOBAL")]
    [field: ReadOnly][field: SerializeField] public Vector2 MovementDirection { get; private set; }
    [field: ReadOnly][field: SerializeField] public Vector2 LookDirection { get; private set; }
    [field: ReadOnly][field: SerializeField] public bool Jump { get; private set; }
    [field: ReadOnly][field: SerializeField] public bool Aim { get; private set; }
    [field: ReadOnly][field: SerializeField] public bool Shoot { get; private set; }


    //  =========================

    private GameplayInputs gameplayInputs;

    //  =========================

    private void Awake()
    {
        gameplayInputs = new GameplayInputs();

        GameManager.Instance.SceneController.ActionPass = true;
    }

    private void OnEnable()
    {
        gameplayInputs.Enable();

        gameplayInputs.Gameplay.Movement.performed += _ => MovementStart();
        gameplayInputs.Gameplay.Movement.canceled += _ => MovementStop();
        gameplayInputs.Gameplay.Jump.started += _ => JumpStart();
        gameplayInputs.Gameplay.Aim.started += _ => AimStart();
        gameplayInputs.Gameplay.Aim.canceled += _ => AimStop();
        gameplayInputs.Gameplay.Shoot.started += _ => ShootStart();
        gameplayInputs.Gameplay.Shoot.canceled += _ => ShootStop();

#if UNITY_EDITOR
        gameplayInputs.Gameplay.Look.performed += _ => LookStart();
        gameplayInputs.Gameplay.Look.canceled += _ => LookStop();
#endif
    }

    private void OnDisable()
    {
        gameplayInputs.Gameplay.Movement.performed -= _ => MovementStart();
        gameplayInputs.Gameplay.Movement.canceled -= _ => MovementStop();
        gameplayInputs.Gameplay.Jump.started -= _ => JumpStart();
        gameplayInputs.Gameplay.Aim.started -= _ => AimStart();
        gameplayInputs.Gameplay.Aim.canceled -= _ => AimStop();
        gameplayInputs.Gameplay.Shoot.started -= _ => ShootStart();
        gameplayInputs.Gameplay.Shoot.canceled -= _ => ShootStop();

#if UNITY_EDITOR
        gameplayInputs.Gameplay.Look.performed -= _ => LookStart();
        gameplayInputs.Gameplay.Look.canceled -= _ => LookStop();
#endif

        gameplayInputs.Disable();
    }

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
}
