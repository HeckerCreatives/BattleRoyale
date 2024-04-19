using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayController : MonoBehaviour
{
    public Vector2 MovementDirection { get; private set; }
    [field: SerializeField] public bool Jump { get; private set; }
    [field: SerializeField] public bool JumpStop { get; private set; }

    //  =========================

    [Header("REMEMBER TO REMOVE THIS")]
    public Camera mainCamera;

    [Header("SETTINGS")]
    [SerializeField] private float jumpHoldTime;

    [Header("DEBUGGER")]
    [SerializeField] private float currentJumpTime;

    //  =========================

    private GameplayInputs gameplayInputs;

    //  =========================

    private void Awake()
    {
        Physics.gravity = new Vector3(Physics.gravity.x, Physics.gravity.y * 20f, Physics.gravity.z);
        gameplayInputs = new GameplayInputs();
    }

    private void OnEnable()
    {
        gameplayInputs.Enable();

        gameplayInputs.Gameplay.Movement.performed += _ => MovementStart();
        gameplayInputs.Gameplay.Movement.canceled += _ => MovementStop();
        gameplayInputs.Gameplay.Jump.started += _ => JumpStart();
        gameplayInputs.Gameplay.Jump.canceled += _ => JumpCancel();
    }

    private void OnDisable()
    {
        gameplayInputs.Gameplay.Movement.performed -= _ => MovementStart();
        gameplayInputs.Gameplay.Movement.canceled -= _ => MovementStop();
        gameplayInputs.Gameplay.Jump.started -= _ => JumpStart();
        gameplayInputs.Gameplay.Jump.canceled -= _ => JumpCancel();

        gameplayInputs.Disable();
    }

    private void Update()
    {
        JumpInputHoldTime();
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
        JumpStop = false;
        currentJumpTime = Time.time;
    }

    private void JumpCancel()
    {
        Jump = false;
        JumpStop = true;
    }

    public void JumpInputHoldTime()
    {
        if (Time.time >= currentJumpTime + jumpHoldTime)
        {
            Jump = false;
            currentJumpTime = 0f;
        }
    }

    public void JumpTurnOff()
    {
        Jump = false;
    }
}
