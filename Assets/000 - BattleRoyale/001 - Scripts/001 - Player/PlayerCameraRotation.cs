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
using UnityEngine.UI;

public class PlayerCameraRotation : NetworkBehaviour
{
    public Transform Target
    {
        get => target.transform;
    }

    // Deterministic, networked-safe shot origin. Same pivot the aim math uses,
    // derived purely from the player's networked transform — server and clients
    // agree exactly, unlike the camera (local-only) or the animated muzzle.
    public Vector3 ShooterOrigin => transform.position + Vector3.up * targetHeight;

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

    [Header("Collision Smoothing")]
    [Tooltip("The virtual camera that has the CinemachineCollider extension. Assign to eliminate collision wobble.")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [Tooltip("How long (seconds) the CinemachineCollider blends when pushing/releasing the camera. 0.2-0.3 eliminates wobble.")]
    [SerializeField] private float collisionSmoothingTime = 0.25f;
    [Tooltip("Layers the collision probe tests against. Match this to the CinemachineCollider's Collide Against.")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [Tooltip("Radius of the spherecast that anticipates camera collision (≈ collider camera radius).")]
    [SerializeField] private float collisionProbeRadius = 0.2f;
    [Tooltip("Extra distance added to the boom length when probing, so the deadzone yields slightly before contact.")]
    [SerializeField] private float collisionProbeExtraDistance = 0.3f;
    [Tooltip("Seconds to suppress the deadzone once an obstruction is detected (fast).")]
    [SerializeField] private float blendAttack = 0.05f;
    [Tooltip("Seconds to restore the deadzone after the obstruction clears (slow — avoids pop).")]
    [SerializeField] private float blendRelease = 0.25f;

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
    [SerializeField] public float AimDistance;
    [SerializeField] private float CameraAngleOverride;
    [SerializeField] private float aimAssistRadius = 8f;           // cone half-angle in degrees
    [Range(0f, 0.95f)][SerializeField] private float slowdownStrength = 0.5f;   // input reduction when on target
    [SerializeField] private float magnetStrength = 10f;           // degrees/sec pull toward enemy
    [SerializeField] private float bodyHeight = 1.8f;              // player capsule height for body-segment detection
    [SerializeField][Range(0f, 1f)] private float magnetBodyFraction = 0.65f;  // 0=feet 1=head, 0.65≈chest
    [SerializeField] private float impactDistance;
    [SerializeField] private Vector3 originalImpactOffset;

    [Space]
    [SerializeField] private Image crosshairImg;

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

    private bool followProxyInitialized = false;
    private float _collisionBlend = 0f;
    private Cinemachine3rdPersonFollow _tpf;

    // Gizmo state — populated every tick by GetCameraRaycastTarget
    private Vector3 _gizmoCastOrigin;
    private Vector3 _gizmoCastDir;
    private bool _gizmoHadTarget;
    private Vector3 _gizmoTargetPos;

    // Last enemy locked by aim assist — used by FireArrow to correct 3rd-person parallax
    public Transform AimAssistTarget { get; private set; }

    private Transform _prevAimAssistTarget;
    private Vector3   _prevAimAssistEnemyPos;

    private bool _crosshairEnemyActive;
    private bool _crosshairDamageFlashing;

    // Reusable aim-assist candidate buffer — struct elements + retained
    // capacity, so FindEnemyInCone allocates nothing per tick.
    private struct ConeCandidate { public Transform t; public Transform root; public float cos; public Vector3 losPoint; }
    private readonly List<ConeCandidate> _coneCandidates = new();

    public Vector3 GetAimAssistChestPosition()
    {
        if (AimAssistTarget == null) return Vector3.zero;
        return AimAssistTarget.position + Vector3.up * (bodyHeight * magnetBodyFraction);
    }

    private void UpdateCrosshairColor(bool hasEnemy)
    {
        if (crosshairImg == null) return;
        if (_crosshairDamageFlashing)
        {
            _crosshairEnemyActive = hasEnemy;
            return;
        }
        if (hasEnemy == _crosshairEnemyActive) return;
        _crosshairEnemyActive = hasEnemy;

        LeanTween.cancel(crosshairImg.gameObject);
        Color target = hasEnemy ? Color.green : Color.white;
        LeanTween.value(crosshairImg.gameObject,
            (Color c) => { if (crosshairImg) crosshairImg.color = c; },
            crosshairImg.color, target, 0.2f);
    }

    public void FlashDamageCrosshair()
    {
        if (crosshairImg == null) return;
        LeanTween.cancel(crosshairImg.gameObject);
        _crosshairDamageFlashing = true;

        LeanTween.value(crosshairImg.gameObject,
            (Color c) => { if (crosshairImg) crosshairImg.color = c; },
            crosshairImg.color, Color.red, 0.1f)
            .setOnComplete(() =>
            {
                Color revertTo = _crosshairEnemyActive ? Color.green : Color.white;
                LeanTween.value(crosshairImg.gameObject,
                    (Color c) => { if (crosshairImg) crosshairImg.color = c; },
                    Color.red, revertTo, 0.25f)
                    .setOnComplete(() => { _crosshairDamageFlashing = false; });
            });
    }

    public void ExitBowAimCrosshair()
    {
        _crosshairEnemyActive = false;
        if (_crosshairDamageFlashing) return;
        if (crosshairImg == null) return;
        LeanTween.cancel(crosshairImg.gameObject);
        LeanTween.value(crosshairImg.gameObject,
            (Color c) => { if (crosshairImg) crosshairImg.color = c; },
            crosshairImg.color, Color.white, 0.2f);
    }

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
        ApplyCollisionSmoothing();
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
        // Proxy is advanced once per render frame in LateUpdate (single clock).
    }

    public void HandleCameraAimInput()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        float sensitivity = CurrentAdsSensitivity;

        _cinemachineTargetYaw   += input.LookDirection.x  * Runner.DeltaTime * sensitivity;
        _cinemachineTargetPitch += -input.LookDirection.y * Runner.DeltaTime * sensitivity;

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

        // Render-rate clock: the proxy is purely visual smoothing, so it must
        // advance on Time.deltaTime, not the network tick. (Driving it from two
        // clocks was part of the old wobble.)
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Transform src = target.transform;
        followProxy.rotation = src.rotation;

        // Independent collision anticipation — does NOT read virtualCamera.State,
        // so there is no feedback loop with the CinemachineCollider. Spherecast
        // the boom and ramp a 0..1 weight: fast attack, slow release.
        float targetBlend = ProbeCameraObstruction(src) ? 1f : 0f;
        float blendTime = targetBlend > _collisionBlend ? blendAttack : blendRelease;
        _collisionBlend = blendTime > 0.0001f
            ? Mathf.MoveTowards(_collisionBlend, targetBlend, dt / blendTime)
            : targetBlend;

        // Deadzone/softzone-smoothed candidate position.
        Vector3 worldError = src.position - followProxy.position;
        Vector3 localError = Quaternion.Inverse(src.rotation) * worldError;

        Vector3 localCorrection = new(
            ComputeDeadSoftCorrection(localError.x, followDeadzone.x, followSoftzone.x),
            ComputeDeadSoftCorrection(localError.y, followDeadzone.y, followSoftzone.y),
            ComputeDeadSoftCorrection(localError.z, followDeadzone.z, followSoftzone.z)
        );

        Vector3 worldCorrection = src.rotation * localCorrection;
        Vector3 smoothed = followProxy.position + worldCorrection * followProxyCatchup * dt;

        // Continuous blend between the deadzone result and a rigid lock to the
        // real target. Near a wall the proxy tightens so the collider has a
        // stable, non-lagging pivot; in the open it's full deadzone. No snap,
        // so no pop on engage/disengage.
        followProxy.position = Vector3.Lerp(smoothed, src.position, _collisionBlend);
    }

    // Anticipates a camera-boom obstruction independently of the collider's
    // output. Approximates the 3rd Person Follow rig: pivot + shoulder/arm
    // offset, cast back along the view boom.
    private bool ProbeCameraObstruction(Transform src)
    {
        if (collisionMask.value == 0) return false;

        if (_tpf == null && virtualCamera != null)
            _tpf = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();

        float boom = (_tpf != null ? _tpf.CameraDistance : targetDistance) + collisionProbeExtraDistance;

        Vector3 pivot = src.position;
        if (_tpf != null)
        {
            Vector3 so = _tpf.ShoulderOffset;
            pivot += src.rotation * new Vector3(so.x, so.y + _tpf.VerticalArmLength, so.z);
        }

        Vector3 dir = -(src.rotation * Vector3.forward);

        return Physics.SphereCast(pivot, collisionProbeRadius, dir, out _, boom,
                                  collisionMask, QueryTriggerInteraction.Ignore);
    }

    private void ApplyCollisionSmoothing()
    {
        if (virtualCamera == null) return;
        var col = virtualCamera.GetComponent<CinemachineCollider>();
        if (col == null) return;

        col.m_SmoothingTime = collisionSmoothingTime;
        col.m_DampingWhenOccluded = collisionSmoothingTime;
        col.m_Damping = collisionSmoothingTime;
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

#if UNITY_EDITOR
    // Debug-only deadzone/softzone visualization. OnGUI is invoked several
    // times per frame; stripping it from builds removes that cost entirely
    // (the in-method flag guard doesn't avoid the per-frame invocation).
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
#endif

    public void SetMuzzlePosition()
    {
        Quaternion camRotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0f);
        // Rotate original offset
        Vector3 rotatedOffset = camRotation * originalImpactOffset;

        // Apply
        impactPoint.position = playerObj.position + rotatedOffset;
        impactPoint.rotation = camRotation;
    }


    public void HandleCameraAimInputBow()
    {
        if (!GetInput<MyInput>(out var input)) return;

        Vector3 origin = movement.CameraHitOrigin;
        Vector3 dir    = movement.CameraHitDirection;

        if (dir.sqrMagnitude < 0.001f)
        {
            origin = transform.position + Vector3.up * targetHeight;
            dir    = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0f) * Vector3.forward;
        }
        dir = dir.normalized;

        float sensitivity = CurrentAdsSensitivity;

        Transform enemy = FindEnemyInCone(origin, dir);

        // Crosshair is local-only HUD. FindEnemyInCone still runs on the State
        // Authority for the authoritative aim-assist magnet / chest snap, but
        // the colour change must only touch the local player's reticle.
        if (HasInputAuthority)
            UpdateCrosshairColor(enemy != null);

        float speedMul = (enemy != null && (input.LookDirection.x != 0f || input.LookDirection.y != 0f))
            ? 1f - slowdownStrength
            : 1f;

        _cinemachineTargetYaw   += input.LookDirection.x  * Runner.DeltaTime * sensitivity * speedMul;
        _cinemachineTargetPitch += -input.LookDirection.y * Runner.DeltaTime * sensitivity * speedMul;

        if (enemy != null)
        {
            Vector3 currentDir   = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0f) * Vector3.forward;
            Vector3 camRight     = Vector3.Cross(Vector3.up, currentDir).normalized;
            Vector3 camUp        = Vector3.Cross(currentDir, camRight).normalized;
            Vector3 magnetTarget = enemy.position + Vector3.up * (bodyHeight * magnetBodyFraction);
            Vector3 toTarget     = (magnetTarget - origin).normalized;
            float   step         = magnetStrength * Runner.DeltaTime;

            // Horizontal: fixed chest-point pull. magnetTarget is a stable world position
            // so yaw corrections converge monotonically with no feedback loop.
            // The player can still aim anywhere on the body vertically — this only
            // compensates for the enemy strafing left/right.
            float angleX = Mathf.Asin(Mathf.Clamp(Vector3.Dot(toTarget, camRight), -1f, 1f)) * Mathf.Rad2Deg;
            _cinemachineTargetYaw += Mathf.Clamp(angleX, -step, step);

            // Vertical: velocity-based using enemy root position.
            // Only fires when the enemy actually moves up/down (jumping, knockback).
            // No correction on the first tick of locking (_prevAimAssistTarget guard)
            // prevents a spike from comparing against an uninitialized position.
            if (_prevAimAssistTarget == enemy)
            {
                float enemyDeltaY = enemy.position.y - _prevAimAssistEnemyPos.y;
                if (Mathf.Abs(enemyDeltaY) > 0.001f)
                {
                    float distToEnemy = Mathf.Max(0.01f, Vector3.Distance(origin, magnetTarget));
                    float angleY      = -Mathf.Atan2(enemyDeltaY, distToEnemy) * Mathf.Rad2Deg;
                    _cinemachineTargetPitch += Mathf.Clamp(angleY, -step, step);
                }
            }

            _prevAimAssistTarget  = enemy;
            _prevAimAssistEnemyPos = enemy.position;
        }
        else
        {
            _prevAimAssistTarget = null;
        }

        _cinemachineTargetYaw   = ClampAngle(_cinemachineTargetYaw,   float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp,    TopClamp);

        // Always face aim direction — this function is only called from bow aim upper body states
        playerObj.rotation = Quaternion.Euler(0f, _cinemachineTargetYaw, 0f);

        target.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0f);

        Vector3 aimDir    = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0f) * Vector3.forward;
        Vector3 camOrigin = transform.position + Vector3.up * targetHeight;
        aimTF.position    = camOrigin + aimDir * targetDistance;
        aimTF.rotation    = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw + CameraAngleOverride, 0f);
    }

    private Transform FindEnemyInCone(Vector3 origin, Vector3 dir)
    {
        _gizmoCastOrigin = origin;
        _gizmoCastDir    = dir;
        _gizmoHadTarget  = false;
        _gizmoTargetPos  = Vector3.zero;

        float cosThreshold = Mathf.Cos(aimAssistRadius * Mathf.Deg2Rad);
        float rangeSq      = AimDistance * AimDistance;

        // Pass 1: collect everything in range + cone. No raycasts here.
        // All detection gates (CanReadNetworkedState→IsValid, IsDead, self-skip)
        // are unchanged, so this finds exactly the same candidates as before.
        _coneCandidates.Clear();

        foreach (PlayerHealthV2 health in PlayerHealthV2.All)
        {
            if (!CanReadNetworkedState(health)) continue;
            if (health.IsDead) continue;
            if (health.Object == null || health.Object == Object) continue;

            Vector3 feet    = health.transform.position;
            Vector3 head    = feet + Vector3.up * bodyHeight;
            Vector3 closest = ClosestPointOnSegmentToRay(origin, dir, feet, head);

            if ((closest - origin).sqrMagnitude > rangeSq) continue;

            float cosAngle = Vector3.Dot(dir, (closest - origin).normalized);
            if (cosAngle <= cosThreshold) continue;

            _coneCandidates.Add(new ConeCandidate {
                t = health.transform, root = health.transform.root, cos = cosAngle,
                losPoint = feet + Vector3.up * (bodyHeight * magnetBodyFraction) });
        }

        foreach (Botdata bot in Botdata.All)
        {
            if (!CanReadNetworkedState(bot)) continue;
            if (bot.IsDead) continue;

            Vector3 feet    = bot.transform.position;
            Vector3 head    = feet + Vector3.up * bodyHeight;
            Vector3 closest = ClosestPointOnSegmentToRay(origin, dir, feet, head);

            if ((closest - origin).sqrMagnitude > rangeSq) continue;

            float cosAngle = Vector3.Dot(dir, (closest - origin).normalized);
            if (cosAngle <= cosThreshold) continue;

            _coneCandidates.Add(new ConeCandidate {
                t = bot.transform, root = bot.transform.root, cos = cosAngle,
                losPoint = feet + Vector3.up * (bodyHeight * magnetBodyFraction) });
        }

        // Pass 2: LOS-check only the angularly-best candidate; fall back to the
        // next-best only if occluded. Same result as the old per-candidate loop
        // (argmax cone angle among LOS-clear) but ~1 raycast in the common case.
        // Chest-point LOS + IsOccluded root-check are kept exactly as-is, so
        // detection — and therefore the crosshair colour — is unchanged.
        Transform bestTarget = null;
        while (_coneCandidates.Count > 0)
        {
            int bestIdx = 0;
            for (int i = 1; i < _coneCandidates.Count; i++)
                if (_coneCandidates[i].cos > _coneCandidates[bestIdx].cos)
                    bestIdx = i;

            ConeCandidate c = _coneCandidates[bestIdx];
            if (!IsOccluded(origin, c.losPoint - origin, c.root))
            {
                bestTarget = c.t;
                break;
            }

            int last = _coneCandidates.Count - 1;
            _coneCandidates[bestIdx] = _coneCandidates[last];
            _coneCandidates.RemoveAt(last);
        }

        AimAssistTarget = bestTarget;

        if (bestTarget != null)
        {
            _gizmoHadTarget = true;
            _gizmoTargetPos = bestTarget.position + Vector3.up * (bodyHeight * 0.5f);
        }

        return bestTarget;
    }

    // Occluded only if a real blocker (root != the candidate) is in the way.
    // Hitting the candidate's own colliders is not occlusion, so this works
    // regardless of which layers players/bots sit on (no mask self-exclusion).
    private bool IsOccluded(Vector3 origin, Vector3 toEnemy, Transform candidateRoot)
    {
        float dist = toEnemy.magnitude;
        if (dist <= 0.01f) return false;

        if (Physics.Raycast(origin, toEnemy / dist, out RaycastHit losHit, dist,
                            aimLayerMask, QueryTriggerInteraction.Ignore))
            return losHit.collider.transform.root != candidateRoot;

        return false;
    }

    private static bool CanReadNetworkedState(NetworkBehaviour behaviour)
    {
        if (behaviour == null) return false;
        if (behaviour.Object == null) return false;
        // IsValid only — NOT IsInSimulation. On a client, bots/other players
        // are proxies (interpolated, not simulated), so requiring
        // IsInSimulation rejected every target and the local crosshair/aim
        // assist never saw anyone. Reading a valid proxy's transform and
        // networked IsDead is safe.
        return behaviour.Object.IsValid;
    }

    private static Vector3 ClosestPointOnSegmentToRay(Vector3 rayOrigin, Vector3 rayDir, Vector3 segA, Vector3 segB)
    {
        Vector3 d = segB - segA;
        Vector3 w = segA - rayOrigin;

        float a = Vector3.Dot(d, d);
        if (a < 0.0001f) return segA;

        float b     = Vector3.Dot(d, rayDir);
        float c     = Vector3.Dot(d, w);
        float e     = Vector3.Dot(rayDir, w);
        float denom = a - b * b;

        float t = Mathf.Abs(denom) < 0.0001f ? 0f : Mathf.Clamp01((b * e - c) / denom);
        return segA + d * t;
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

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)     return;
        if (_gizmoCastDir == Vector3.zero) return;

        Vector3 origin   = _gizmoCastOrigin;
        Vector3 dir      = _gizmoCastDir;
        Vector3 endPoint = origin + dir * AimDistance;

        if (_gizmoHadTarget)
        {
            // Green: aim cone line to target
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, _gizmoTargetPos);
            Gizmos.DrawWireSphere(_gizmoTargetPos, 0.25f);
        }
        else
        {
            // Yellow: full cone length, no target
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, endPoint);
        }

        // Draw aim cone edges (8 rays)
        Gizmos.color = _gizmoHadTarget ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 1f, 0f, 0.3f);
        Vector3 perp = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) < 0.99f
            ? Vector3.Cross(dir, Vector3.up).normalized
            : Vector3.Cross(dir, Vector3.right).normalized;

        for (int i = 0; i < 8; i++)
        {
            Quaternion rot     = Quaternion.AngleAxis(i * 45f, dir);
            Vector3    edge    = Quaternion.AngleAxis(aimAssistRadius, rot * perp) * dir;
            Gizmos.DrawLine(origin, origin + edge * AimDistance);
        }
    }
}
