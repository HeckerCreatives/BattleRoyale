using Fusion;
using Fusion.Addons.SimpleKCC;
using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private JumpMovement jumpMovement;
    [SerializeField] private SimpleKCC characterController;

    [Header("RIGS")]
    [SerializeField] private Rig hipsRig;
    [SerializeField] private Rig headRig;

    [Header("PLAYER MOVEMENTS")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float crouchMoveSpeed;
    [SerializeField] private float proneMoveSpeed;
    [SerializeField] private float jumpHeight;
    [SerializeField] private float gravity;
    [SerializeField] private float jumpTimeout;
    [SerializeField] private float fallTimeout;
    [SerializeField] private float speedChangeRate = 10.0f;
    [SerializeField] private float crouchToProneHoldTime = 0.5f;

    [Header("ENVIRONMENT")]
    [SerializeField] private float groundedOffset;
    [SerializeField] private float groundedRadius;
    [SerializeField] private LayerMask groundLayers;

    [Header("DEBUGGER PLAYER")]
    [MyBox.ReadOnly][SerializeField] private float _verticalVelocity;
    [MyBox.ReadOnly][SerializeField] private float _terminalVelocity = 53.0f;
    [MyBox.ReadOnly][SerializeField] private float currentCrouchToProneHoldTime;
    [MyBox.ReadOnly][SerializeField] private float jumpDelay;

    [Header("DEBUGGER ENVIRONMENT")]
    [MyBox.ReadOnly][SerializeField] private Vector3 spherePosition;
    [MyBox.ReadOnly][SerializeField] private float _jumpTimeoutDelta;
    [MyBox.ReadOnly][SerializeField] private float _fallTimeoutDelta;

    [field: Header("DEBUGGER PLAYER NETWORK")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public Vector3 MoveDirection { get;  set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool IsShooting { get;  set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool IsProne { get;  set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool IsCrouch { get;  set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float XMovement { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float YMovement { get; set; }

    //  ======================

    [Networked] public NetworkButtons PreviousButtons { get; set; }

    MyInput controllerInput;

    //  ======================

    #region NETWORK
    
    public override void Spawned()
    {
        // Set custom gravity.
        characterController.SetGravity(Physics.gravity.y * 3f);
    }

    public override void FixedUpdateNetwork()
    {
        InputControlls();
    }

    #endregion

    public override void Render()
    {
        LayerAndWeight();
    }

    #region CONTROLLERS

    private void InputControlls()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        controllerInput = input;

        Move();
        Shoot();
        Crouch();
        Prone();
        PlayerRotateAnimation();

        PreviousButtons = input.Buttons;
    }

    private void Move()
    {
        XMovement = controllerInput.MovementDirection.x;
        YMovement = controllerInput.MovementDirection.y;

        if (playerHealth.CurrentHealth <= 0) return;

        MoveDirection = characterController.TransformRotation * new Vector3(controllerInput.MovementDirection.x, 0f, controllerInput.MovementDirection.y) * (IsCrouch ? crouchMoveSpeed : IsProne ? proneMoveSpeed : moveSpeed) * Runner.DeltaTime;

        MoveDirection.Normalize();

        // Apply the movement
        characterController.Move(MoveDirection, jumpMovement.JumpImpulse);
    }

    private void Crouch()
    {
        if (playerHealth.CurrentHealth <= 0) return;

        // Ensure we're on the ground
        if (!characterController.IsGrounded)
        {
            controllerInput.Buttons.Set(3, false);
            return;
        }

        if (!controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Crouch))
            return;

        if (!IsCrouch)
        {
            IsCrouch = true;
            IsProne = false;
        }
        else
        {
            IsCrouch = false;
        }
    }

    private void Prone()
    {
        if (playerHealth.CurrentHealth <= 0) return;

        // Ensure we're on the ground
        if (!characterController.IsGrounded)
        {
            controllerInput.Buttons.Set(4, false);
            return;
        }

        if (!controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Prone))
            return;

        if (!IsProne)
        {
            IsProne = true;
            IsCrouch = false;
            hipsRig.weight = 0;
            headRig.weight = 0;
        }
        else
        {
            IsProne = false;
            headRig.weight = 1f;
        }
    }

    //private void Attack()
    //{
    //    if (playerHealth.CurrentHealth <= 0) return;

    //    if (inventory.WeaponIndex != 1 && inventory.WeaponIndex != 2 && inventory.WeaponIndex != 3 && inventory.WeaponIndex != 4)
    //    {
    //        ResetAttack();
    //        return;
    //    }

    //    if (!controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Melee)) return;

    //    if (IsAttacking) return;

    //    IsAttacking = true;
    //}

    private void Shoot()
    {
        if (inventory.WeaponIndex != 5)
        {
            IsShooting = false;
            return;
        }

        if (controllerInput.HoldInputButtons.WasPressed(PreviousButtons, HoldInputButtons.Shoot))
        {
            if (IsShooting) return;

            IsShooting = true;
        }
        else
        {
            if (!IsShooting) return;

            IsShooting = false;
        }

        if (IsShooting) return;
    }

    private void PlayerRotateAnimation()
    {
        if (!characterController.IsGrounded)
        {
            return;
        }

        if (controllerInput.MovementDirection != Vector2.zero)
        {
            return;
        }

        if (controllerInput.LookDirection.x == 0)
        {
            return;
        }
    }

    private void LayerAndWeight()
    {
        if (IsProne)
        {
            headRig.weight = 0f;
        }
        else
        {
            headRig.weight = 1f;
        }
    }

    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, groundedRadius);
    }
}
