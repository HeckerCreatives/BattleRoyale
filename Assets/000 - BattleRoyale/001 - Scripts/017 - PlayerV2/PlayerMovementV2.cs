using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;

public class PlayerMovementV2 : NetworkBehaviour
{
    public float SprintSpeed
    {
        get => sprintSpeed;
    }

    public float MoveSpeed
    {
        get => moveSpeed;
    }

    //  ====================

    [SerializeField] private SimpleKCC characterController;
    [SerializeField] private PlayerCameraRotation cameraRotation;
    [SerializeField] private PlayerStamina stamina;
    
    [Space]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float jumpHeight;

    [Space]
    public RectTransform joystickArea;
    public RectTransform joystickHandle;

    [Header("DEBUGGER")]
    [SerializeField] private GameplayController gameplayController;
    [SerializeField] private Vector3 inputVector;

    [field: Header("DEBUGGER NETWORK")]
    [field: SerializeField][Networked] public Vector3 MoveDirection { get; set; }
    [field: SerializeField][Networked] public Vector3 MoveDir { get; set; }
    [field: SerializeField][Networked] public float XMovement { get; set; }
    [field: SerializeField][Networked] public float YMovement { get; set; }
    [field: SerializeField][Networked] public NetworkBool IsSprint { get; set; }
    [field: SerializeField][Networked] public NetworkBool IsRoll { get; set; }
    [field: SerializeField][Networked] public NetworkBool Attacking { get; set; }
    [field: SerializeField][Networked] public float JumpImpulse { get; set; }
    [field: SerializeField][Networked] public NetworkBool IsJumping { get; set; }
    [field: SerializeField][Networked] public NetworkBool IsBlocking { get; set; }
    [field: SerializeField][Networked] public Vector2 LastTouchPos { get; set; }
    [field: SerializeField][Networked] public bool IsTouching { get; set; }

    //  =======================

    //ChangeDetector _changeDetector;

    private Dictionary<int, string> touchRoles = new Dictionary<int, string>();
    private Dictionary<int, Vector2> lastTouchPositions = new Dictionary<int, Vector2>();
    private Dictionary<int, float> stationaryTimers = new Dictionary<int, float>();
    private HashSet<int> activeTouchIds = new();

    private const float StationaryThreshold = 2f; // Pixel distance to consider as stationary
    private const float StationaryTimeThreshold = 0.1f; // Time in seconds to detect stationary

    MyInput controllerInput;
    NetworkButtons PreviousButtons;

    //  =======================

    public override void Spawned()
    {
        characterController.SetGravity(Physics.gravity.y * 3f);
    }

    public void PlayerMovementInitialize()
    {
        characterController.SetGravity(Physics.gravity.y * 3f);
        //_changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (!HasInputAuthority) return;

        gameplayController = Runner.GetComponent<GameplayController>();

        //UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown += TouchFingerDown;
        //UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove += TouchFingerMove;
        //UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp += TouchFingeUp;
    }

    private void OnDisable()
    {
        //UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown -= TouchFingerDown;
        //UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove -= TouchFingerMove;
        //UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp -= TouchFingeUp;
    }

    private void LateUpdate()
    {
        if (!HasInputAuthority) return;

        //foreach (var touch in Touchscreen.current.touches)
        //{
        //    int id = touch.touchId.ReadValue();
        //    Vector2 pos = touch.position.ReadValue();
        //    var phase = touch.phase.ReadValue();

        //    Debug.Log($"id {id}  phase {phase}");

        //    switch (phase)
        //    {
        //        case UnityEngine.InputSystem.TouchPhase.Began:
        //            TouchFingerDown(id, pos);
        //            break;

        //        case UnityEngine.InputSystem.TouchPhase.Moved:
        //            TouchFingerMove(id, pos);
        //            break;

        //        case UnityEngine.InputSystem.TouchPhase.Ended:
        //        case UnityEngine.InputSystem.TouchPhase.Canceled:
        //            TouchFingeUp(id);
        //            break;
        //    }
        //}


        //foreach (var touch in Touchscreen.current.touches)
        //{
        //    int touchId = touch.touchId.ReadValue();
        //    Vector2 currentPosition = touch.position.ReadValue();

        //    if (lastTouchPositions.TryGetValue(touchId, out Vector2 lastPosition))
        //    {
        //        Vector2 delta = currentPosition - lastPosition;

        //        if (delta.magnitude < StationaryThreshold)
        //        {
        //            // Reset camera movement if touch is nearly stationary
        //            gameplayController.LookDirection = Vector2.zero;
        //        }
        //    }
        //}

        //CameraPan();

        //foreach (var finger in UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers)
        //{
        //    if (lastTouchPositions.TryGetValue(finger.index, out Vector2 lastPosition))
        //    {
        //        Vector2 currentPosition = finger.screenPosition;
        //        Vector2 delta = currentPosition - lastPosition;

        //        if (delta.magnitude < StationaryThreshold)
        //            gameplayController.LookDirection = Vector2.zero; // Reset camera movement
        //    }
        //}
    }

    public override void FixedUpdateNetwork()
    {
        FallingAfterJump();
        InputControlls();
    }

    #region TOUCH INPUTS

    //private void TouchDown(int touchId, Vector2 pos)
    //{
    //    if (AnalogStickDetector(pos))
    //    {
    //        touchRoles[touchId] = "joystick";
    //    }
    //    else if (IsTouchOverUI(pos))
    //    {
    //        return;
    //    }
    //    else
    //    {
    //        touchRoles[touchId] = "camera";
    //        lastTouchPositions[touchId] = pos;
    //        stationaryTimers[touchId] = 0f;
    //    }

    //    if (touchRoles.TryGetValue(touchId, out var role))
    //    {
    //        if (role == "camera")
    //            gameplayController.LookDirection = Vector2.zero;
    //        else if (role == "joystick")
    //            HandleJoystickMovement(touchId, pos);
    //    }
    //}

    //private void TouchMove(int touchId, Vector2 pos)
    //{
    //    if (!touchRoles.TryGetValue(touchId, out var role)) return;

    //    if (role == "camera")
    //    {
    //        if (lastTouchPositions.TryGetValue(touchId, out Vector2 lastPos))
    //        {
    //            Vector2 delta = pos - lastPos;
    //            gameplayController.LookDirection += delta;
    //            lastTouchPositions[touchId] = pos;
    //            stationaryTimers[touchId] = 0f;
    //        }
    //        else
    //        {
    //            lastTouchPositions[touchId] = pos;
    //        }
    //    }
    //    else if (role == "joystick")
    //    {
    //        HandleJoystickMovement(touchId, pos);
    //    }
    //}

    //private void TouchUp(int touchId)
    //{
    //    if (!touchRoles.TryGetValue(touchId, out var role)) return;

    //    if (role == "camera")
    //    {
    //        gameplayController.LookDirection = Vector2.zero;
    //    }
    //    else if (role == "joystick")
    //    {
    //        gameplayController.MovementDirection = Vector2.zero;
    //        ResetJoystick();
    //    }

    //    touchRoles.Remove(touchId);
    //    lastTouchPositions.Remove(touchId);
    //    stationaryTimers.Remove(touchId);
    //}

    //private bool IsTouchOverUI(Vector2 touchPosition)
    //{
    //    PointerEventData pointerEventData = new(EventSystem.current) { position = touchPosition };
    //    List<RaycastResult> results = new();
    //    EventSystem.current.RaycastAll(pointerEventData, results);

    //    foreach (var result in results)
    //    {
    //        if (result.gameObject.CompareTag("NonBlockingUI")) continue;
    //        return true;
    //    }

    //    return false;
    //}

    //private bool AnalogStickDetector(Vector2 touchPosition)
    //{
    //    PointerEventData pointerEventData = new(EventSystem.current) { position = touchPosition };
    //    List<RaycastResult> results = new();
    //    EventSystem.current.RaycastAll(pointerEventData, results);

    //    foreach (var result in results)
    //    {
    //        if (result.gameObject.CompareTag("AnalogStick"))
    //            return true;
    //    }

    //    return false;
    //}

    //private void HandleJoystickMovement(int touchId, Vector2 screenPos)
    //{
    //    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
    //        joystickArea,
    //        screenPos,
    //        GameManager.Instance.UICamera,
    //        out Vector2 localTouchPosition))
    //    {
    //        Vector2 normalized = new Vector2(
    //            localTouchPosition.x / (joystickArea.sizeDelta.x * 0.5f),
    //            localTouchPosition.y / (joystickArea.sizeDelta.y * 0.5f)
    //        );

    //        inputVector = Vector2.ClampMagnitude(normalized, 1.0f);
    //        joystickHandle.anchoredPosition = inputVector * (joystickArea.sizeDelta.x * 0.3f);
    //        gameplayController.MovementDirection = inputVector.normalized;
    //    }
    //}

    private void TouchMovements()
    {
        foreach (var touch in Touchscreen.current.touches)
        {
            int id = touch.touchId.ReadValue();
            Vector2 pos = touch.position.ReadValue();
            var phase = touch.phase.ReadValue();

            // Fallback if somehow not Began
            if (!activeTouchIds.Contains(id) && phase != UnityEngine.InputSystem.TouchPhase.Ended)
            {
                activeTouchIds.Add(id);
                TouchFingerDown(id, pos);
            }

            switch (phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    activeTouchIds.Add(id);
                    TouchFingerDown(id, pos);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                    if (lastTouchPositions.TryGetValue(id, out var lastPos))
                    {
                        float delta = Vector2.Distance(pos, lastPos);
                        if (delta < StationaryThreshold)
                            gameplayController.LookDirection = Vector2.zero; // Optional: handle stationary
                        else
                            TouchFingerMove(id, pos);
                    }
                    else
                        TouchFingerMove(id, pos);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    TouchFingeUp(id);
                    activeTouchIds.Remove(id);
                    lastTouchPositions.Remove(id);
                    break;
            }

            // Update last known position
            lastTouchPositions[id] = pos;
        }
    }

    private void ResetJoystick()
    {
        inputVector = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
    }

    private void TouchFingeUp(int touchId)
    {
        if (touchRoles.TryGetValue(touchId, out string role))
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
            touchRoles.Remove(touchId);
            lastTouchPositions.Remove(touchId); // Remove the finger's position data
        }
    }

    private void TouchFingerMove(int touchId, Vector2 pos)
    {
        if (touchRoles.TryGetValue(touchId, out string role))
        {
            if (role == "camera")
            {
                Vector2 currentTouchPosition = pos;

                if (lastTouchPositions.TryGetValue(touchId, out Vector2 lastPosition))
                {
                    Vector2 delta = currentTouchPosition - lastPosition;

                    gameplayController.LookDirection += delta;

                    lastTouchPositions[touchId] = currentTouchPosition; // Update the last position
                    stationaryTimers[touchId] = 0f; // Reset timer
                }
                else
                {
                    // Initialize the last position if not present
                    lastTouchPositions[touchId] = currentTouchPosition;
                }
            }
            else if (role == "joystick")
            {
                HandleJoystickMovement(pos);
            }
        }
    }

    private void TouchFingerDown(int touchId, Vector2 pos)
    {
        // 🟢 First: Check if the finger touched the joystick UI
        if (AnalogStickDetector(pos))
        {
            touchRoles[touchId] = "joystick";
        }
        // 🔒 Then: If it touched any other UI (like attack/jump), block it
        else if (EventSystem.current.IsPointerOverGameObject(touchId))
        {
            return; // Ignore this touch completely — it's over attack/jump/etc
        }
        // ✅ Else: Treat it as a camera drag
        else
        {
            touchRoles[touchId] = "camera";
            lastTouchPositions[touchId] = pos;
            stationaryTimers[touchId] = 0f;
        }

        // Optional: Initialize values immediately
        if (touchRoles.TryGetValue(touchId, out string role))
        {
            if (role == "camera")
                gameplayController.LookDirection = Vector2.zero;
            else if (role == "joystick")
                HandleJoystickMovement(pos);
        }
    }

    private bool IsTouchOverUI(Vector2 touchPosition)
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = touchPosition
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            // Ignore minimap or decorative UI
            if (result.gameObject.CompareTag("NonBlockingUI")) continue;

            return true;
        }

        return false;
    }

    private bool AnalogStickDetector(Vector2 touchPosition)
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = touchPosition
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            if (result.gameObject.CompareTag("AnalogStick"))
            {
                return true; // Return true if an object with the tag "AnalogStick" is found
            }
        }

        return false; // No UI element with the tag "AnalogStick" was found
    }

    private void HandleJoystickMovement(Vector2 screenPos)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickArea,
            screenPos,
            GameManager.Instance.UICamera,
            out Vector2 localTouchPosition))
        {
            // Normalize local touch relative to pivot (pivot must be 0.5, 0.5)
            Vector2 halfSize = joystickArea.rect.size * 0.5f;
            Vector2 normalized = new Vector2(
                localTouchPosition.x / halfSize.x,
                localTouchPosition.y / halfSize.y
            );

            // Clamp movement to unit circle
            inputVector = Vector2.ClampMagnitude(normalized, 1.0f);

            // Update handle position relative to base
            float handleRange = halfSize.x * 0.5f; // 50% of radius; tweak as needed
            joystickHandle.anchoredPosition = inputVector * handleRange;

            // Send to controller
            gameplayController.MovementDirection = inputVector.normalized;
        }
    }

    #endregion

    #region MOVEMENTS

    private void InputControlls()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        controllerInput = input;

        TouchMovements();
        Move();
        Sprint();
        Jump();
        Roll();
        Block();
        Shoot();
        //Crouch();
        //Prone();
        //PlayerRotateAnimation();

        PreviousButtons = input.Buttons;
    }

    private void Move()
    {
        XMovement = controllerInput.MovementDirection.x;
        YMovement = controllerInput.MovementDirection.y;

        //if (playerHealth.CurrentHealth <= 0) return;
    }

    public void MoveCharacter()
    {
        MoveDir = PlayerLookDirection();

        if (MoveDir.sqrMagnitude > 0.01f && !IsRoll)
        {
            // Normalize to prevent sprinting from affecting rotation
            MoveDir.Normalize();

            // Optional: Smooth rotation
            Quaternion targetRotation = Quaternion.LookRotation(MoveDir);
            characterController.SetLookRotation(Quaternion.Slerp(characterController.TransformRotation, targetRotation, Runner.DeltaTime * 10f));
        }


        float moveSpeedValue = IsSprint ? SprintSpeed : MoveSpeed;
        MoveDirection = MoveDir * moveSpeedValue * Runner.DeltaTime;

        characterController.Move(MoveDirection, JumpImpulse);
    }

    public void RotatePlayer()
    {
        MoveDir = PlayerLookDirection();

        if (MoveDir.sqrMagnitude > 0.01f && !IsRoll)
        {
            // Normalize to prevent sprinting from affecting rotation
            MoveDir.Normalize();

            // Optional: Smooth rotation
            Quaternion targetRotation = Quaternion.LookRotation(MoveDir);
            characterController.SetLookRotation(targetRotation);
        }

    }

    public Vector3 PlayerLookDirection()
    {
        float yawRad = cameraRotation._cinemachineTargetYaw * Mathf.Deg2Rad;
        Vector3 camForward = new Vector3(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
        Vector3 camRight = new Vector3(camForward.z, 0f, -camForward.x); // rotate forward 90°

        Vector3 worldDirection = camForward * YMovement + camRight * XMovement;

        return worldDirection.normalized;
    }

    private void Sprint()
    {
        //if (playerHealth.CurrentHealth <= 0) return;

        if (stamina.Stamina <= 0f)
        {
            IsSprint = false;
            return;
        }

        if (!controllerInput.HoldInputButtons.WasPressed(PreviousButtons, HoldInputButtons.Sprint))
        {
            IsSprint = false;
            return;
        }

        IsSprint = true;
    }

    private void Jump()
    {
        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Jump) && characterController.IsGrounded && !IsJumping)
        {
            IsJumping = true;
            JumpImpulse = jumpHeight;
        }
    }

    public void FallingAfterJump()
    {
        if (!characterController.IsGrounded)
        {
            JumpImpulse -= Mathf.Abs(Physics.gravity.y) * 3f * Runner.DeltaTime;

            if (JumpImpulse <= 0f)
            {
                JumpImpulse = 0f;
                IsJumping = false;
            }
        }
    }

    private void Roll()
    {
        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Roll))
            IsRoll = true;
        else
            IsRoll = false;
    }

    private void Block()
    {
        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Block))
            IsBlocking = true;
        else
            IsBlocking = false;
    }

    private void Shoot()
    {
        if (controllerInput.HoldInputButtons.WasPressed(PreviousButtons, HoldInputButtons.Shoot))
        {
            if (Attacking) return;

            Attacking = true;
        }
        else
        {
            if (!Attacking) return;

            Attacking = false;
        }
    }

    #endregion
}
