using Fusion;
using Fusion.Addons.SimpleKCC;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private JumpMovement jumpMovement;
    [SerializeField] private SimpleKCC characterController;
    [SerializeField] private HealPlayables healPlayables;
    [SerializeField] private RepairArmorPlayables repairArmorPlayables;

    [Space]
    [SerializeField] private TextMeshProUGUI warningTMP;

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

    [Space]
    public RectTransform joystickArea; // The touch area for the joystick
    public RectTransform joystickHandle; // The handle of the joystick

    [Header("DEBUGGER")]

    [SerializeField] private Vector2 startPosition;
    [SerializeField] private Vector3 inputVector;

    [Header("DEBUGGER PLAYER")]
    [MyBox.ReadOnly][SerializeField] private float _verticalVelocity;
    [MyBox.ReadOnly][SerializeField] private float _terminalVelocity = 53.0f;
    [MyBox.ReadOnly][SerializeField] private float currentCrouchToProneHoldTime;
    [MyBox.ReadOnly][SerializeField] private float jumpDelay;

    [Header("DEBUGGER ENVIRONMENT")]
    [MyBox.ReadOnly][SerializeField] private Vector3 spherePosition;
    [MyBox.ReadOnly][SerializeField] private float _jumpTimeoutDelta;
    [MyBox.ReadOnly][SerializeField] private float _fallTimeoutDelta;
    [SerializeField] private Vector3 groundNormal;

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

    public GameplayController gameplayController;

    private Dictionary<int, string> touchRoles = new Dictionary<int, string>();
    private Dictionary<int, Vector2> lastTouchPositions = new Dictionary<int, Vector2>();
    private Dictionary<int, float> stationaryTimers = new Dictionary<int, float>();
    private const float StationaryThreshold = 2f; // Pixel distance to consider as stationary
    private const float StationaryTimeThreshold = 0.1f; // Time in seconds to detect stationary

    //  ======================

    #region NETWORK

    public async override void Spawned()
    {
        EnhancedTouchSupport.Enable(); // Enable Enhanced Touch
        // Set custom gravity.
        characterController.SetGravity(Physics.gravity.y * 3f);

        while (!Runner) await Task.Yield();

        if (!HasInputAuthority) return;

        gameplayController = Runner.GetComponent<GameplayController>();
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown += TouchFingerDown;
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove += TouchFingerMove;
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp += TouchFingeUp;
    }

    private void OnDisable()
    {
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown -= TouchFingerDown;
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove -= TouchFingerMove;
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp -= TouchFingeUp;
        EnhancedTouchSupport.Disable(); // Enable Enhanced Touch
    }

    private void TouchFingeUp(Finger finger)
    {
        if (touchRoles.TryGetValue(finger.index, out string role))
        {
            if (role == "camera")
            {
                gameplayController.LookDirection = Vector2.zero;
            }
            else if (role == "joystick")
            {
                gameplayController.MovementDirection = Vector2.zero;
                ResetJoystick();
            }
            touchRoles.Remove(finger.index);
            lastTouchPositions.Remove(finger.index); // Remove the finger's position data
        }
    }

    private void TouchFingerMove(Finger finger)
    {
        if (touchRoles.TryGetValue(finger.index, out string role))
        {
            if (role == "camera")
            {
                Vector2 currentTouchPosition = finger.screenPosition;

                if (lastTouchPositions.TryGetValue(finger.index, out Vector2 lastPosition))
                {
                    Vector2 delta = currentTouchPosition - lastPosition;

                    gameplayController.LookDirection += delta;

                    lastTouchPositions[finger.index] = currentTouchPosition; // Update the last position
                    stationaryTimers[finger.index] = 0f; // Reset timer
                }
                else
                {
                    // Initialize the last position if not present
                    lastTouchPositions[finger.index] = currentTouchPosition;
                }
            }
            else if (role == "joystick")
            {
                HandleJoystickMovement(finger);
            }
        }
    }

    private void TouchFingerDown(Finger finger)
    {
        if (IsTouchInJoystickArea(finger.screenPosition) && !touchRoles.ContainsKey(finger.index))
        {
            touchRoles[finger.index] = "joystick";
        }
        else if (!IsTouchOverUI(finger.screenPosition) && !touchRoles.ContainsKey(finger.index))
        {
            touchRoles[finger.index] = "camera";
            lastTouchPositions[finger.index] = finger.currentTouch.screenPosition; // Initialize last position
            stationaryTimers[finger.index] = 0f;
        }

        if (touchRoles.TryGetValue(finger.index, out string role))
        {
            if (role == "camera")
            {
                gameplayController.LookDirection = Vector2.zero;
            }
            else if (role == "joystick")
            {
                HandleJoystickMovement(finger);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        InputControlls();
    }

    #endregion

    private void LateUpdate()
    {
        if (!HasInputAuthority) return;

        //AnalogStick();

        foreach (var finger in UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers)
        {
            if (lastTouchPositions.TryGetValue(finger.index, out Vector2 lastPosition))
            {
                Vector2 currentPosition = finger.screenPosition;
                Vector2 delta = currentPosition - lastPosition;

                if (delta.magnitude < StationaryThreshold)
                {
                    gameplayController.LookDirection = Vector2.zero; // Reset camera movement
                }
            }
        }
    }

    #region CONTROLLERS

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ResetMovementOnMoveBattle()
    {
        IsShooting = false;
        IsProne = false;
        IsCrouch = false;
    }

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

    private bool IsTouchOverUI(Vector2 touchPosition)
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = touchPosition
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        return raycastResults.Count > 0;  // Return true if UI was hit
    }

    //private void AnalogStick()
    //{
    //    foreach (var finger in UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers)
    //    {
    //        switch (finger.currentTouch.phase)
    //        {
    //            case UnityEngine.InputSystem.TouchPhase.Began:
    //                if (IsTouchInJoystickArea(finger.currentTouch.screenPosition) && !touchRoles.ContainsKey(finger.index))
    //                {
    //                    touchRoles[finger.index] = "joystick";
    //                }
    //                else if (!IsTouchOverUI(finger.currentTouch.screenPosition) && !touchRoles.ContainsKey(finger.index))
    //                {
    //                    touchRoles[finger.index] = "camera";
    //                }
    //                break;

    //            case UnityEngine.InputSystem.TouchPhase.Moved:
    //                if (touchRoles.TryGetValue(finger.index, out string role))
    //                {
    //                    if (role == "camera")
    //                    {
    //                        gameplayController.LookDirection = finger.currentTouch.delta;
    //                    }
    //                    else if (role == "joystick")
    //                    {
    //                        HandleJoystickMovement(finger);
    //                    }
    //                }
    //                break;

    //            case UnityEngine.InputSystem.TouchPhase.Stationary:
    //                Debug.Log($"stationary");
    //                if (touchRoles.TryGetValue(finger.index, out string stationaryrole))
    //                {
    //                    Debug.Log($"role: {stationaryrole}");
    //                    if (stationaryrole == "camera")
    //                    {
    //                        gameplayController.LookDirection = Vector2.zero;
    //                    }
    //                }
    //                break;

    //            case UnityEngine.InputSystem.TouchPhase.Ended:
    //            case UnityEngine.InputSystem.TouchPhase.Canceled:
    //                if (touchRoles.TryGetValue(finger.index, out string endedrole))
    //                {

    //                    if (endedrole == "camera")
    //                    {
    //                        gameplayController.LookDirection = Vector2.zero;
    //                    }
    //                    else if (endedrole == "joystick")
    //                    {
    //                        gameplayController.MovementDirection = Vector2.zero;
    //                        ResetJoystick();
    //                    }
    //                    touchRoles.Remove(finger.index);
    //                }
    //                break;
    //            default:
    //                if (touchRoles.TryGetValue(finger.index, out string defaultrole))
    //                {
    //                    Debug.Log($"role: {defaultrole}   phase: {finger.currentTouch.phase}");
    //                }
    //                break;
    //        }
    //    }
    //}

    private void HandleJoystickMovement(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
    {
        Vector2 localTouchPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickArea, finger.currentTouch.screenPosition, null, out localTouchPosition))
        {
            localTouchPosition.x = (localTouchPosition.x / joystickArea.sizeDelta.x);
            localTouchPosition.y = (localTouchPosition.y / joystickArea.sizeDelta.y);

            inputVector = new Vector2((localTouchPosition.x - 0.5f) * 2f, (localTouchPosition.y - 0.5f) * 2f);
            inputVector = Vector2.ClampMagnitude(inputVector, 1.0f);

            joystickHandle.anchoredPosition = new Vector2(
                inputVector.x * (joystickArea.sizeDelta.x / 3),
                inputVector.y * (joystickArea.sizeDelta.y / 3)
            );

            gameplayController.MovementDirection = inputVector.normalized;
        }
    }

    private bool IsTouchInJoystickArea(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickArea, screenPosition, null, out var localPoint);
        return joystickArea.rect.Contains(localPoint);
    }

    private void ResetJoystick()
    {
        inputVector = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
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

    private void TurnOffWarning()
    {
        warningTMP.gameObject.SetActive(false);
        warningTMP.text = "";
    }

    private void Prone()
    {
        if (healPlayables.Healing || repairArmorPlayables.Repairing)
        {
            warningTMP.text = healPlayables.Healing ? "Can't prone while healing" : "Can't prone while repairing armor";
            warningTMP.gameObject.SetActive(true);
            Invoke(nameof(TurnOffWarning), 3f);

            return;
        }

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

    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, groundedRadius);
    }
}
