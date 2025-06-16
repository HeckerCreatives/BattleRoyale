using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
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
    [field: SerializeField][Networked] public bool IsSprint { get; set; }
    [field: SerializeField][Networked] public bool IsRoll { get; set; }

    //  =======================

    ChangeDetector _changeDetector;

    private Dictionary<int, string> touchRoles = new Dictionary<int, string>();
    private Dictionary<int, Vector2> lastTouchPositions = new Dictionary<int, Vector2>();
    private Dictionary<int, float> stationaryTimers = new Dictionary<int, float>();

    private const float StationaryThreshold = 2f; // Pixel distance to consider as stationary
    private const float StationaryTimeThreshold = 0.1f; // Time in seconds to detect stationary

    MyInput controllerInput;
    NetworkButtons PreviousButtons;

    //  =======================

    private void OnEnable()
    {
        PlayerMovementInitialize();
    }

    public async void PlayerMovementInitialize()
    {
        characterController.SetGravity(Physics.gravity.y * 3f);
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

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
    }

    private void LateUpdate()
    {
        if (!HasInputAuthority) return;

        foreach (var finger in UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers)
        {
            if (lastTouchPositions.TryGetValue(finger.index, out Vector2 lastPosition))
            {
                Vector2 currentPosition = finger.screenPosition;
                Vector2 delta = currentPosition - lastPosition;

                if (delta.magnitude < StationaryThreshold)
                    gameplayController.LookDirection = Vector2.zero; // Reset camera movement
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        InputControlls();
    }

    #region TOUCH INPUTS

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
        if (AnalogStickDetector(finger.screenPosition) && !touchRoles.ContainsKey(finger.index))
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

    private void HandleJoystickMovement(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
    {
        Vector2 localTouchPosition;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickArea,
                finger.currentTouch.screenPosition,
                GameManager.Instance.UICamera, // ? Use the correct UI Camera
                out localTouchPosition))
        {
            // Normalize the position based on joystick area
            Vector2 normalizedPosition = new Vector2(
                (localTouchPosition.x / (joystickArea.sizeDelta.x * 0.5f)), // Adjust for pivot
                (localTouchPosition.y / (joystickArea.sizeDelta.y * 0.5f))
            );

            // Clamp joystick movement
            inputVector = Vector2.ClampMagnitude(normalizedPosition, 1.0f);

            // Move the joystick handle
            joystickHandle.anchoredPosition = inputVector * (joystickArea.sizeDelta.x * 0.3f);

            // Set movement direction
            gameplayController.MovementDirection = inputVector.normalized;
        }
    }

    private void ResetJoystick()
    {
        inputVector = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
    }

    #endregion

    #region MOVEMENTS

    private void InputControlls()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        controllerInput = input;

        Move();
        Sprint();
        Roll();
        //Shoot();
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

        if (MoveDir.sqrMagnitude > 0.01f && !IsRoll)
        {
            // Normalize to prevent sprinting from affecting rotation
            MoveDir.Normalize();

            // Optional: Smooth rotation
            Quaternion targetRotation = Quaternion.LookRotation(MoveDir);
            characterController.SetLookRotation(Quaternion.Slerp(characterController.TransformRotation, targetRotation, Runner.DeltaTime * 10f));
        }

        MoveDir = PlayerLookDirection();

        float moveSpeedValue = IsSprint ? SprintSpeed : MoveSpeed;
        MoveDirection = MoveDir * moveSpeedValue * Runner.DeltaTime;

        if (!IsRoll)
            characterController.Move(MoveDirection, 0f);
        else
            characterController.Move(characterController.TransformDirection * 300f, 0f);
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

    private void Roll()
    {
        if (stamina.Stamina < 50)
            return;

        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Roll))
            IsRoll = true;
    }

    #endregion
}
