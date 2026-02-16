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

    private void LookSensitivityChanged(object sender, EventArgs e)
    {
        Rpc_HandleSensitivity(GameManager.Instance.GameSettingManager.CurrentLookSensitivity);
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
    }

    public void HandleCameraAimInput()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        bool hasInput = input.LookDirection.sqrMagnitude >= _threshold;

        // Only check for target if aiming and not strongly moving the camera
        if (movement.Attacking)
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

        if (movement.Attacking)
            playerObj.rotation = Quaternion.Euler(0f, _cinemachineTargetYaw, 0f);
        //target.transform.rotation = targetRotation;

        // Direction from yaw/pitch (this is the important part)
        Vector3 aimDir = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0f) * Vector3.forward;
        // Origin the server knows (player position + height)
        Vector3 cameraorigin = transform.position + Vector3.up * targetHeight;

        aimTF.position = cameraorigin + aimDir * targetDistance;
        aimTF.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw + CameraAngleOverride, 0.0f);

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
