using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayController : MonoBehaviour
{
    public Vector2 MovementDirection { get; private set; }

    //  =========================

    [Header("REMEMBER TO REMOVE THIS")]
    public Camera mainCamera;

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
    }

    private void OnDisable()
    {
        gameplayInputs.Gameplay.Movement.performed -= _ => MovementStart();
        gameplayInputs.Gameplay.Movement.canceled -= _ => MovementStop();

        gameplayInputs.Disable();
    }

    private void MovementStart()
    {
        MovementDirection = gameplayInputs.Gameplay.Movement.ReadValue<Vector2>();
    }

    private void MovementStop()
    {
        MovementDirection = Vector2.zero;
    }

}
