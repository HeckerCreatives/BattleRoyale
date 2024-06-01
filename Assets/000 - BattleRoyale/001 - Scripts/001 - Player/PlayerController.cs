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
    public bool IsProne
    {
        get => isProne;
    }

    public bool IsCrouch
    {
        get => isCrouch;
    }

    //  =========================

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
    [MyBox.ReadOnly][SerializeField] private bool isAttacking;
    [MyBox.ReadOnly][SerializeField] private bool isProne;
    [MyBox.ReadOnly][SerializeField] private bool isCrouch;
    [MyBox.ReadOnly][SerializeField] private float currentCrouchToProneHoldTime;

    [Header("DEBUGGER ENVIRONMENT")]
    [MyBox.ReadOnly][SerializeField] private Vector3 spherePosition;
    [MyBox.ReadOnly][SerializeField] private float _jumpTimeoutDelta;
    [MyBox.ReadOnly][SerializeField] private float _fallTimeoutDelta;

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

    private void InputControlls()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        controllerInput = input;

        //JumpAndGravity();
        Move();
        Attack();
        Crouch();
        Prone();
        PlayerRotateAnimation();

        PreviousButtons = input.Buttons;
    }

    private void JumpAndGravity()
    {
        if (!HasInputAuthority) return;

        if (characterController.IsGrounded)
        {
            // update animator if using character
            animator.SetBool("jump", false);
            animator.SetBool("freefall", false);

            _fallTimeoutDelta = fallTimeout;


            if (_verticalVelocity < 0.0f)
                _verticalVelocity = -2f;

            // Jump
            if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Jump) && _jumpTimeoutDelta <= 0.0f)
            {
                if (isProne || isCrouch)
                {
                    currentCrouchToProneHoldTime = 0;
                    if (isCrouch || isProne)
                    {
                        animator.SetBool("crouch", false);
                        animator.SetBool("prone", false);
                        isCrouch = false;
                        isProne = false;
                    }

                    return;
                }
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animator.SetBool("jump", true);
            }

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = jumpTimeout;

            if (_fallTimeoutDelta >= 0.0f)
                _fallTimeoutDelta -= Time.deltaTime;
            else
            {
                animator.SetBool("freefall", true);
            }
        }

        if (_verticalVelocity < _terminalVelocity)
            _verticalVelocity += gravity * Time.deltaTime;
    }

    private void Move()
    {
        //if (!characterController.IsGrounded) return;

        Vector2 inputDirection = new Vector2(controllerInput.MovementDirection.x, controllerInput.MovementDirection.y);

        // Calculate the movement direction in the XZ plane based on camera orientation
        Vector3 moveDirection = characterController.TransformRotation * new Vector3(inputDirection.x, 0f, inputDirection.y);

        // Make sure the movement vector is normalized
        moveDirection.Normalize();

        Debug.Log($"Network Input Move Direction: {moveDirection}");

        // Apply the movement
        characterController.Move(new Vector3(moveDirection.x, 0f, moveDirection.z) * moveSpeed * Time.deltaTime);
        animator.SetFloat("x", inputDirection.x);
        animator.SetFloat("y", inputDirection.y);
    }

    private void Attack()
    {
        if (!controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Shoot)) return;

        if (isAttacking) return;

        isAttacking = true;

        animator.SetTrigger("meleeattack");
    }

    public void ResetAttack()
    {
        isAttacking = false;
        animator.ResetTrigger("meleeattack");
    }

    private void Crouch()
    {
        // Ensure we're on the ground
        //if (!characterController.IsGrounded)
        //{
        //    animator.SetBool("crouch", false);
        //    return;
        //}

        // Check if the crouch/prone button is pressed or held
        if (!controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Crouch))
        {
            // If the button is not pressed or held, reset the hold time and states
            return;
        }

        if (!isCrouch)
        {
            isCrouch = true;
            isProne = false;
            animator.SetBool("prone", false);
            animator.SetBool("crouch", true);
        }
        else
        {
            isCrouch = false;
            animator.SetBool("crouch", false);
        }
    }

    private void Prone()
    {
        // Ensure we're on the ground
        //if (!characterController.IsGrounded)
        //{
        //    animator.SetBool("crouch", false);
        //    return;
        //}

        // Check if the crouch/prone button is pressed or held
        if (!controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Prone))
        {
            // If the button is not pressed or held, reset the hold time and states
            return;
        }

        if (!isProne)
        {
            isProne = true;
            isCrouch = false;
            animator.SetBool("crouch", false);
            animator.SetBool("prone", true);
            hipsRig.weight = 0;
        }
        else
        {
            isProne = false;
            animator.SetBool("prone", false);
            hipsRig.weight = 1;
        }
    }

    private void PlayerRotateAnimation()
    {
        //if (!characterController.IsGrounded)
        //{
        //    animator.SetBool("turning", false);
        //    return;
        //}

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
