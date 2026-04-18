using Cinemachine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UIElements;

public class PlayerCameraRotation : NetworkBehaviour
{
    public Transform Target
    {
        get => target.transform;
    }

    //  ==================

    [SerializeField] private Transform playerObj;
    [SerializeField] private Transform impactPoint;

    [Header("References")]
    [SerializeField] private PlayerMovementV2 movement;
    [SerializeField] private GameObject target;
    [SerializeField] private float targetDistance = 10f;
    [SerializeField] private float targetHeight = 1.568005f; // your head height offset
    
    [Header("3rd Person Follow Proxy (Deadzone/Softzone Emulation)")]
    [Tooltip("Enable custom deadzone/softzone while still using Cinemachine3rdPersonFollow.")]
    [SerializeField] private bool useFollowProxyDeadzone = false;
    [Tooltip("Set this as your vcam Follow target. This proxy gets smoothed toward the real target.")]
    [SerializeField] private Transform followProxy;
    [Tooltip("Position deadzone in target local space (X=horizontal, Y=vertical, Z=depth).")]
    [SerializeField] private Vector3 followDeadzone = new(0.25f, 0.15f, 0.10f);
    [Tooltip("Softzone edge in target local space. Must be >= deadzone per axis.")]
    [SerializeField] private Vector3 followSoftzone = new(1.20f, 0.80f, 0.50f);
    [Tooltip("How quickly proxy catches up once target is outside deadzone.")]
    [SerializeField] private float followProxyCatchup = 8f;

    [Header("Deadzone Guides (Screen)")]
    [SerializeField] private bool drawFollowProxyGuides = true;
    [SerializeField] private Color deadzoneFill = new(0f, 1f, 0f, 0.08f);
    [SerializeField] private Color softzoneFill = new(0.2f, 0.6f, 1f, 0.06f);
    [SerializeField] private Color deadzoneOutline = new(0.2f, 1f, 0.2f, 0.9f);
    [SerializeField] private Color softzoneOutline = new(0.4f, 0.8f, 1f, 0.9f);
    [SerializeField] private float guideLineThickness = 2f;

    [Header("CAMERA HEIGHT")]
    [SerializeField] private float standCamHeight;
    [SerializeField] private float crouchCamHeight;
    [SerializeField] private float proneCamHeight;

    [Header("CAMERA LOOK TARGET")]
    [SerializeField] private Transform aimTF;
    [SerializeField] private LayerMask aimLayerMask;
    [SerializeField] private LayerMask aimBotEnemyMask;
    [SerializeField] private float AimDistance;
    [SerializeField] private float CameraAngleOverride;
    [SerializeField] private float aimAssistAngleRadius = 5f;
    [SerializeField] private float aimAssistStrength = 5f;       // Pull speed
    [SerializeField] private float magnetismDuration = 0.5f;     // Seconds aim assist lasts
    [SerializeField] private float impactDistance;
    [SerializeField] private Vector3 originalImpactOffset;

    [field: Header("Parameters")]
    [field: SerializeField][Networked] public float Sensitivity { get; private set; }
    [field: SerializeField][Networked] public float TopClamp { get; private set; }
    [field: SerializeField][Networked] public float BottomClamp { get; private set; }

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float _threshold { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float _cinemachineTargetYaw { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float _cinemachineTargetPitch { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float CurrentSensitivity { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float CurrentAdsSensitivity { get; private set; }

    private float magnetismTimer = 0f;
    private Transform magnetismTarget;
    private bool followProxyInitialized = false;

    public override void Spawned()
    {
        if (!HasInputAuthority) return;

        _threshold = 0.01f;
        //target.transform.parent = null;
        GameManager.Instance.GameSettingManager.OnLookSensitivityChanged += LookSensitivityChanged;
        GameManager.Instance.GameSettingManager.OnLookAdsSensitivityChanged += LookAdsSensitivityChanged;
    }

    private void OnEnable()
    {
        impactDistance = Vector3.Distance(
            playerObj.position,
            impactPoint.position
        );

        originalImpactOffset = impactPoint.position - playerObj.position;

        InitializeFollowProxy();
    }

    private void OnDisable()
    {
        if (!Runner)
        {
            if (GameManager.Instance == null) return;

            GameManager.Instance.GameSettingManager.OnLookSensitivityChanged -= LookSensitivityChanged;
            GameManager.Instance.GameSettingManager.OnLookAdsSensitivityChanged -= LookAdsSensitivityChanged;

            return;
        }

        if (!HasInputAuthority) return;

        GameManager.Instance.GameSettingManager.OnLookSensitivityChanged -= LookSensitivityChanged;
        GameManager.Instance.GameSettingManager.OnLookAdsSensitivityChanged -= LookAdsSensitivityChanged;
    }

    private void LateUpdate()
    {
        if (!Runner) return;

        if (!HasInputAuthority) return;

        UpdateFollowProxy();
    }

    private void LookSensitivityChanged(object sender, EventArgs e)
    {
        Rpc_HandleSensitivity(GameManager.Instance.GameSettingManager.CurrentLookSensitivity / 0.3f);
    }

    private void LookAdsSensitivityChanged(object sender, EventArgs e)
    {
        Rpc_HandleAdsSensitivity(GameManager.Instance.GameSettingManager.CurrentLookAdsSensitivity);
    }

    public void InitializeCameraRotationSensitivity()
    {
        Rpc_HandleSensitivity(GameManager.Instance.GameSettingManager.CurrentLookSensitivity);
        Rpc_HandleAdsSensitivity(GameManager.Instance.GameSettingManager.CurrentLookAdsSensitivity);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_HandleSensitivity(float lookSensitivity)
    {
        CurrentSensitivity = Sensitivity * lookSensitivity;
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_HandleAdsSensitivity(float adsSensitivity)
    {
        CurrentAdsSensitivity = Sensitivity * adsSensitivity;
    }

    public void HandleCameraNoAim()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        if (input.LookDirection.sqrMagnitude >= _threshold)
        {
            _cinemachineTargetYaw += input.LookDirection.x * Runner.DeltaTime * CurrentSensitivity;
            _cinemachineTargetPitch += -input.LookDirection.y * Runner.DeltaTime * CurrentSensitivity;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        target.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);
        UpdateFollowProxy();
    }

    public void HandleCameraAimInput()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        bool hasInput = input.LookDirection.sqrMagnitude >= _threshold;

        // Only check for target if aiming and not strongly moving the camera
        if (movement.CurrentlyAttacking)
        {
            if (magnetismTimer <= 0f) // Only refresh target if no active assist
            {
                var targetEnemy = GetCameraRaycastTarget(input);
                if (targetEnemy != null)
                {
                    magnetismTarget = targetEnemy;
                    magnetismTimer = magnetismDuration;
                }
            }
        }
        else
            magnetismTarget = null;

        // Apply look input
        _cinemachineTargetYaw += input.LookDirection.x * Runner.DeltaTime * (movement.Aiming ? CurrentAdsSensitivity : CurrentSensitivity);
        _cinemachineTargetPitch += -input.LookDirection.y * Runner.DeltaTime * (movement.Aiming ? CurrentAdsSensitivity : CurrentSensitivity);

        // Apply magnetism pull
        if (magnetismTarget != null && magnetismTimer > 0f)
        {
            Vector3 dirToTarget = (magnetismTarget.position - Camera.main.transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(dirToTarget, Vector3.up);
            Vector3 targetAngles = targetRot.eulerAngles;

            _cinemachineTargetYaw = Mathf.LerpAngle(_cinemachineTargetYaw, targetAngles.y, aimAssistStrength * Runner.DeltaTime);
            _cinemachineTargetPitch = Mathf.LerpAngle(_cinemachineTargetPitch, targetAngles.x, aimAssistStrength * Runner.DeltaTime);

            magnetismTimer -= Runner.DeltaTime;
            if (magnetismTimer <= 0f) magnetismTarget = null;
        }

        // Clamp angles
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        Quaternion targetRotation = Quaternion.Euler(
            _cinemachineTargetPitch,
            _cinemachineTargetYaw,
            0f
        );

        if (movement.CurrentlyAttacking)
            playerObj.rotation = Quaternion.Euler(0f, _cinemachineTargetYaw, 0f);

        target.transform.rotation = targetRotation;

        // Direction from yaw/pitch (this is the important part)
        Vector3 aimDir = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0f) * Vector3.forward;
        // Origin the server knows (player position + height)
        Vector3 cameraorigin = transform.position + Vector3.up * targetHeight;

        aimTF.position = cameraorigin + aimDir * targetDistance;
        aimTF.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw + CameraAngleOverride, 0.0f);

    }

    private void InitializeFollowProxy()
    {
        if (!useFollowProxyDeadzone || followProxy == null || target == null)
            return;

        followProxy.position = target.transform.position;
        followProxy.rotation = target.transform.rotation;
        followProxyInitialized = true;
    }

    private void UpdateFollowProxy()
    {
        if (!useFollowProxyDeadzone || followProxy == null || target == null)
            return;

        if (!followProxyInitialized)
            InitializeFollowProxy();

        Transform src = target.transform;

        // Keep rotational behavior identical to your original target pivot.
        followProxy.rotation = src.rotation;

        // Error measured in source-local space to mimic screen-like framing behavior.
        Vector3 worldError = src.position - followProxy.position;
        Vector3 localError = Quaternion.Inverse(src.rotation) * worldError;

        Vector3 localCorrection = new(
            ComputeDeadSoftCorrection(localError.x, followDeadzone.x, followSoftzone.x),
            ComputeDeadSoftCorrection(localError.y, followDeadzone.y, followSoftzone.y),
            ComputeDeadSoftCorrection(localError.z, followDeadzone.z, followSoftzone.z)
        );

        Vector3 worldCorrection = src.rotation * localCorrection;
        followProxy.position += worldCorrection * followProxyCatchup * Runner.DeltaTime;
    }

    private static float ComputeDeadSoftCorrection(float error, float deadzone, float softzone)
    {
        float absError = Mathf.Abs(error);
        float dz = Mathf.Max(0f, deadzone);
        float sz = Mathf.Max(dz, softzone);

        if (absError <= dz)
            return 0f;

        float signedExcess = Mathf.Sign(error) * (absError - dz);
        if (sz <= dz + 0.0001f)
            return signedExcess;

        float t = Mathf.Clamp01((absError - dz) / (sz - dz));
        return signedExcess * t;
    }

    private static Texture2D _whiteTex;

    private void OnGUI()
    {
        if (!useFollowProxyDeadzone || !drawFollowProxyGuides)
            return;

        // Only draw for the local controller to avoid multiple overlays in splitscreen/debugging.
        if (!HasInputAuthority)
            return;

        if (target == null)
            return;

        var cam = Camera.main;
        if (cam == null)
            return;

        if (_whiteTex == null)
        {
            _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }

        // Approximate how your local-space X/Y extents map to screen space using camera projection.
        Vector3 targetPos = target.transform.position;
        Vector3 toTarget = targetPos - cam.transform.position;
        float depth = Vector3.Dot(toTarget, cam.transform.forward);
        depth = Mathf.Max(0.01f, depth);

        float verticalWorldHalf;
        float horizontalWorldHalf;

        if (cam.orthographic)
        {
            verticalWorldHalf = cam.orthographicSize;
            horizontalWorldHalf = cam.orthographicSize * cam.aspect;
        }
        else
        {
            float vFovRad = cam.fieldOfView * Mathf.Deg2Rad;
            verticalWorldHalf = depth * Mathf.Tan(vFovRad * 0.5f);
            horizontalWorldHalf = verticalWorldHalf * cam.aspect;
        }

        float softHalfX = Mathf.Abs(followSoftzone.x);
        float softHalfY = Mathf.Abs(followSoftzone.y);
        float deadHalfX = Mathf.Abs(followDeadzone.x);
        float deadHalfY = Mathf.Abs(followDeadzone.y);

        float softHalfXN = horizontalWorldHalf > 0.0001f ? softHalfX / horizontalWorldHalf : 0f;
        float softHalfYN = verticalWorldHalf > 0.0001f ? softHalfY / verticalWorldHalf : 0f;
        float deadHalfXN = horizontalWorldHalf > 0.0001f ? deadHalfX / horizontalWorldHalf : 0f;
        float deadHalfYN = verticalWorldHalf > 0.0001f ? deadHalfY / verticalWorldHalf : 0f;

        softHalfXN = Mathf.Clamp(softHalfXN, 0f, 10f);
        softHalfYN = Mathf.Clamp(softHalfYN, 0f, 10f);
        deadHalfXN = Mathf.Clamp(deadHalfXN, 0f, 10f);
        deadHalfYN = Mathf.Clamp(deadHalfYN, 0f, 10f);

        Rect screenCenter = new Rect(Screen.width * 0.5f, Screen.height * 0.5f, 0f, 0f);

        Rect softRect = new Rect(
            screenCenter.center.x - softHalfXN * Screen.width,
            screenCenter.center.y - softHalfYN * Screen.height,
            softHalfXN * 2f * Screen.width,
            softHalfYN * 2f * Screen.height);

        Rect deadRect = new Rect(
            screenCenter.center.x - deadHalfXN * Screen.width,
            screenCenter.center.y - deadHalfYN * Screen.height,
            deadHalfXN * 2f * Screen.width,
            deadHalfYN * 2f * Screen.height);

        // Outer softzone
        GUI.color = softzoneFill;
        GUI.DrawTexture(softRect, _whiteTex);
        DrawRectOutline(softRect, softzoneOutline);

        // Inner deadzone
        GUI.color = deadzoneFill;
        GUI.DrawTexture(deadRect, _whiteTex);
        DrawRectOutline(deadRect, deadzoneOutline);
    }

    private void DrawRectOutline(Rect rect, Color outlineColor)
    {
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        float t = Mathf.Max(1f, guideLineThickness);

        GUI.color = outlineColor;
        // Top
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, t), _whiteTex);
        // Bottom
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - t, rect.width, t), _whiteTex);
        // Left
        GUI.DrawTexture(new Rect(rect.x, rect.y, t, rect.height), _whiteTex);
        // Right
        GUI.DrawTexture(new Rect(rect.xMax - t, rect.y, t, rect.height), _whiteTex);
    }

    public void SetMuzzlePosition()
    {
        Quaternion camRotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0f);
        // Rotate original offset
        Vector3 rotatedOffset = camRotation * originalImpactOffset;

        // Apply
        impactPoint.position = playerObj.position + rotatedOffset;
        impactPoint.rotation = camRotation;
    }


    private Transform GetCameraRaycastTarget(MyInput input)
    {
        Ray ray = new Ray(input.CameraHitOrigin, input.CameraHitDirection);

        if (Physics.SphereCast(ray, aimAssistAngleRadius, out RaycastHit hit, AimDistance, aimBotEnemyMask))
        {
            Debug.Log(hit.collider.gameObject.name);
            if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Bot"))
            {
                if (hit.collider.gameObject == gameObject) // Ignore self
                    return null;

                return hit.collider.transform;
            }
        }
        return null;
    }

    //private void CameraHeight()
    //{
    //    if (playerController.IsProne) target.transform.localPosition = new Vector3(target.transform.localPosition.x, proneCamHeight, target.transform.localPosition.z);
    //    else if (playerController.IsCrouch) target.transform.localPosition = new Vector3(target.transform.localPosition.x, crouchCamHeight, target.transform.localPosition.z);
    //    else target.transform.localPosition = new Vector3(target.transform.localPosition.x, standCamHeight, target.transform.localPosition.z);
    //}

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        // If you want to see gizmos in Play Mode, ensure you pass the current input
        if (GetInput<MyInput>(out var input) == false) return;

        // SphereCast parameters
        Vector3 origin = input.CameraHitOrigin;
        Vector3 direction = input.CameraHitDirection.normalized;

        Gizmos.color = Color.yellow;

        // Draw the cast line
        Gizmos.DrawLine(origin, origin + direction * AimDistance);

        // Draw start sphere
        Gizmos.DrawWireSphere(origin, aimAssistAngleRadius);

        // Draw end sphere
        Gizmos.DrawWireSphere(origin + direction * AimDistance, aimAssistAngleRadius);
    }
}
