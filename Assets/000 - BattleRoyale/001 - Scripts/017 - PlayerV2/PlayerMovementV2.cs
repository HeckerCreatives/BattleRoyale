using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] private PlayerInventoryV2 inventory;
    [SerializeField] private PlayerHealthV2 heatlh;
    [SerializeField] private PlayerPlayables playerplayables;

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
    [SerializeField] private float sprintCooldown;

    [field: Header("DEBUGGER NETWORK")]
    [field: SerializeField][Networked] public Vector3 MoveDirection { get; set; }
    [field: SerializeField][Networked] public Vector3 MoveDir { get; set; }
    [field: SerializeField][Networked] public float XMovement { get; set; }
    [field: SerializeField][Networked] public float YMovement { get; set; }
    [field: SerializeField][Networked] public Vector3 CameraHitOrigin { get; set; }
    [field: SerializeField][Networked] public Vector3 CameraHitDirection { get; set; }
    [field: SerializeField][Networked] public bool IsSprint { get; set; }
    [field: SerializeField][Networked] public bool IsRoll { get; set; }
    [field: SerializeField][Networked] public bool Attacking { get; set; }
    [field: SerializeField][Networked] public float JumpImpulse { get; set; }
    [field: SerializeField][Networked] public bool IsJumping { get; set; }
    [field: SerializeField][Networked] public bool IsBlocking { get; set; }
    [field: SerializeField][Networked] public bool IsHealing { get; set; }
    [field: SerializeField][Networked] public bool IsRepairing { get; set; }
    [field: SerializeField][Networked] public bool IsTrapping { get; set; }
    [field: SerializeField][Networked] public Vector2 LastTouchPos { get; set; }
    [field: SerializeField][Networked] public bool IsTouching { get; set; }
    [field: SerializeField][Networked] public bool SwitchingHands { get; set; }
    [field: SerializeField][Networked] public bool SwitchingPrimary { get; set; }
    [field: SerializeField][Networked] public bool SwitchingSecondary { get; set; }
    [field: SerializeField][Networked] public bool Aiming { get; set; }
    [field: SerializeField][Networked] public bool Reloading { get; set; }

    //  =======================

    //ChangeDetector _changeDetector;

    private Dictionary<int, string> touchRoles = new Dictionary<int, string>();
    private Dictionary<int, Vector2> lastTouchPositions = new Dictionary<int, Vector2>();
    private Dictionary<int, float> stationaryTimers = new Dictionary<int, float>();
    private HashSet<int> activeTouchIds = new();
    private HashSet<int> ignoredTouchIds = new HashSet<int>();

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

        if (!HasInputAuthority) return;

        gameplayController = Runner.GetComponent<GameplayController>();
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        TouchMovements();
    }

    public override void FixedUpdateNetwork()
    {
        FallingAfterJump();
        InputControlls();
    }

    #region TOUCH INPUTS

    private void TouchMovements()
    {
        foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
        {
            int id = touch.touchId;
            Vector2 pos = touch.screenPosition;
            var phase = touch.phase;

            // --- Handle Ended/Canceled early and skip any further handling ---
            if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                TouchFingeUp(id);
                continue; // Skip rest of the loop
            }

            // --- Handle touch phase states ---
            switch (phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    // Already handled in the block above, but safe to re-call
                    activeTouchIds.Add(id);
                    TouchFingerDown(id, pos);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                    if (!activeTouchIds.Contains(id))
                    {
                        activeTouchIds.Add(id);
                        TouchFingerDown(id, pos);
                    }
                    if (lastTouchPositions.TryGetValue(id, out var lastPos))
                    {
                        float delta = Vector2.Distance(pos, lastPos);

                        if (delta < StationaryThreshold)
                        {
                            gameplayController.LookDirection = Vector2.zero;
                        }
                        else
                        {
                            TouchFingerMove(id, pos);
                        }
                    }
                    else
                    {
                        TouchFingerMove(id, pos);
                    }
                    break;
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    if (touchRoles.TryGetValue(id, out string role))
                    {
                        if (role == "camera")
                        {
                            gameplayController.LookDirection = Vector2.zero;
                            activeTouchIds.Remove(id);
                            touchRoles.Remove(id);
                            lastTouchPositions.Remove(id); // Remove the finger's position data
                        }
                    }
                    break;
            }

            // --- Store last position ---
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
            activeTouchIds.Remove(touchId);
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

                    gameplayController.LookDirection = delta;

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
        else if (IsTouchOverUI(pos))
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

        CameraHitOrigin = controllerInput.CameraHitOrigin;
        CameraHitDirection = controllerInput.CameraHitDirection;

        Move();
        Sprint();
        Jump();
        Roll();
        Block();
        Shoot();
        Healing();
        Repairing();
        Trapping();
        SwitchToHand();
        SwitchToPrimary();
        SwitchToSecondary();
        Reload();

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
        playerplayables.CheckGround();
    }

    public void MoveWithAim()
    {
        XMovement = controllerInput.MovementDirection.x;
        YMovement = controllerInput.MovementDirection.y;

        MoveDirection = characterController.TransformRotation * new Vector3(controllerInput.MovementDirection.x, 0f, controllerInput.MovementDirection.y) * (IsSprint ? SprintSpeed : MoveSpeed) * Runner.DeltaTime;

        MoveDirection.Normalize();


        // Apply the movement
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
            sprintCooldown = 0f;
            return;
        }

        if (!controllerInput.HoldInputButtons.WasPressed(PreviousButtons, HoldInputButtons.Sprint))
        {
            IsSprint = false;
            sprintCooldown = 0f;
            return;
        }

        if (stamina.Stamina >= 10f)
        {
            if (sprintCooldown >= 0.3f)
                IsSprint = true;
            else
            {
                sprintCooldown += Time.deltaTime;
                IsSprint = false;
            }
        }
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

    private void Healing()
    {
        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Heal))
        {
            if (inventory.HealCount <= 0) return;

            if (IsRepairing) return;

            if (heatlh.CurrentHealth >= 100f) return;

            IsHealing = true;
        }
        else
            IsHealing = false;
    }

    private void Repairing()
    {
        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.ArmorRepair))
        {
            if (inventory.ArmorRepairCount <= 0) return;

            if (IsHealing) return;

            if (inventory.Armor == null) return;

            if (inventory.Armor.Supplies >= 100f) return;

            IsRepairing = true;
        }
        else
            IsRepairing = false;
    }

    private void Trapping()
    {
        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.SwitchTrap))
        {
            if (inventory.TrapCount <= 0) return;

            IsTrapping = true;
        }
        else
            IsTrapping = false;
    }

    private void SwitchToHand()
    {
        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.SwitchHands))
            SwitchingHands = true;
        else
            SwitchingHands = false;
    }
    
    private void SwitchToPrimary()
    {
        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.SwitchPrimary))
            SwitchingPrimary = true;
        else
            SwitchingPrimary = false;
    }

    private void SwitchToSecondary()
    {
        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.SwitchSecondary))
            SwitchingSecondary = true;
        else
            SwitchingSecondary = false;
    }

    private void Reload()
    {
        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Reload))
            Reloading = true;
        else
            Reloading = false;
    }

    public void WeaponSwitcher()
    {
        if (SwitchingHands)
            inventory.SwitchToHands();
        else if (SwitchingPrimary)
            inventory.SwitchToPrimary();
        else if (SwitchingSecondary)
            inventory.SwitchToSecondary();
    }

    #endregion
}
