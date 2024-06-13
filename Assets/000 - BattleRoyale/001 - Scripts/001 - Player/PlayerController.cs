using Fusion;
using Fusion.Addons.KCC;
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
    [SerializeField] private SimpleKCC characterController;

    [Header("PLAYER")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rig hipsRig;

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

    [Header("ITEMS")]
    [SerializeField] private GameObject pickupItemList;

    [Header("DEBUGGER PLAYER")]
    [MyBox.ReadOnly][SerializeField] private float _verticalVelocity;
    [MyBox.ReadOnly][SerializeField] private float _terminalVelocity = 53.0f;
    [MyBox.ReadOnly][SerializeField] private float currentCrouchToProneHoldTime;
    [MyBox.ReadOnly][SerializeField] private float jumpDelay;
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public Vector3 MoveDirection { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float JumpImpulse { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool IsAttacking { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool IsProne { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool IsCrouch { get; private set; }

    [Header("DEBUGGER ENVIRONMENT")]
    [MyBox.ReadOnly][SerializeField] private Vector3 spherePosition;
    [MyBox.ReadOnly][SerializeField] private float _jumpTimeoutDelta;
    [MyBox.ReadOnly][SerializeField] private float _fallTimeoutDelta;
    [MyBox.ReadOnly][SerializeField] private bool isJumping;
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool Grounded { get; private set; }

    //  ======================

    [Networked] public NetworkButtons PreviousButtons { get; set; }

    MyInput controllerInput;

    //  ======================

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "ItemPickup")
        {
            pickupItemList.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "ItemPickup")
        {
            pickupItemList.SetActive(false);
        }
    }

    public override void Spawned()
    {
        // Set custom gravity.
        characterController.SetGravity(Physics.gravity.y * 4.0f);
    }

    public override void FixedUpdateNetwork()
    {
        InputControlls();
    }

    private void Update()
    {
        GroundCheck();
        SetToFreeFall();
    }

    private void InputControlls()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        controllerInput = input;

        Move();
        Attack();
        Crouch();
        Prone();
        PlayerRotateAnimation();

        PreviousButtons = input.Buttons;
    }

    private void GroundCheck()
    {
        Grounded = Physics.CheckSphere(transform.position, groundedRadius, groundLayers);
    }

    public void AnimationJump()
    {
    }

    public void ResetAnimationJump()
    {
        isJumping = false;
        animator.SetBool("jump", false);
    }

    private void SetToFreeFall()
    {
        if (!Grounded)
            animator.SetBool("freefall", true);
        else
            animator.SetBool("freefall", false);
    }

    private void Move()
    {
        MoveDirection = characterController.TransformRotation * new Vector3(controllerInput.MovementDirection.x, 0f, controllerInput.MovementDirection.y) * (IsCrouch ? crouchMoveSpeed : IsProne ? proneMoveSpeed : isJumping ? 250 : moveSpeed) * Runner.DeltaTime;

        JumpImpulse = 0f;

        MoveDirection.Normalize();

        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Jump) && Grounded && !isJumping)
        {
            if (IsCrouch || IsProne)
            {
                IsCrouch = false;
                IsProne = false;
                return;
            }
            JumpImpulse = jumpHeight;
            isJumping = true;
            animator.SetBool("jump", true);
        }


        // Apply the movement
        characterController.Move(MoveDirection, JumpImpulse);
        animator.SetFloat("x", controllerInput.MovementDirection.x);
        animator.SetFloat("y", controllerInput.MovementDirection.y);
    }

    private void Attack()
    {
        if (!controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Shoot)) return;

        if (IsAttacking) return;

        IsAttacking = true;

        animator.SetTrigger("meleeattack");
    }

    public void ResetAttack()
    {
        IsAttacking = false;
        animator.ResetTrigger("meleeattack");
    }

    private void Crouch()
    {
        // Ensure we're on the ground
        if (!Grounded)
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
            animator.SetBool("prone", false);
            animator.SetBool("crouch", true);
        }
        else
        {
            IsCrouch = false;
            animator.SetBool("crouch", false);
        }
    }

    private void Prone()
    {
        // Ensure we're on the ground
        if (!Grounded)
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
            animator.SetBool("crouch", false);
            animator.SetBool("prone", true);
            hipsRig.weight = 0;
        }
        else
        {
            IsProne = false;
            animator.SetBool("prone", false);
            hipsRig.weight = 1;
        }
    }

    private void PlayerRotateAnimation()
    {
        if (!Grounded)
        {
            animator.SetBool("turning", false);
            return;
        }

        if (controllerInput.MovementDirection != Vector2.zero)
        {
            animator.SetBool("turning", false);
            return;
        }

        if (controllerInput.LookDirection.x == 0)
        {
            animator.SetBool("turning", false);
            return;
        }

        animator.SetBool("turning", true);
        animator.SetFloat("rightleftturn", controllerInput.LookDirection.x);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, groundedRadius);
    }
}
