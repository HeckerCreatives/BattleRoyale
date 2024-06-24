using Cinemachine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UIElements;

public class PlayerCameraRotation : NetworkBehaviour
{
    //  ==================

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject target;
    [SerializeField] private SimpleKCC playerObj;

    [Header("CAMERA HEIGHT")]
    [SerializeField] private float standCamHeight;
    [SerializeField] private float crouchCamHeight;
    [SerializeField] private float proneCamHeight;

    [Header("CAMERA LOOK TARGET")]
    [SerializeField] private Transform aimTF;
    [SerializeField] private LayerMask aimLayerMask;

    [Header("Parameters")]
    [field: SerializeField][Networked] public float AimDistance { get; private set; }
    [field: SerializeField][Networked] public float Sensitivity { get; private set; }
    [field: SerializeField][Networked] public float TopClamp { get; private set; }
    [field: SerializeField][Networked] public float BottomClamp { get; private set; }
    [field: SerializeField][Networked] public float CameraAngleOverride { get; private set; }

    [Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float _threshold { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float _cinemachineTargetYaw { get; private set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float _cinemachineTargetPitch { get; private set; }

    private void Start()
    {
        _threshold = 0.01f;
    }

    public override void FixedUpdateNetwork()
    {
        HandleMobileCameraInput();
    }

    private void LateUpdate()
    {
        CameraHeight();
    }

    private void HandleMobileCameraInput()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        if (input.LookDirection.sqrMagnitude >= _threshold)
        {
            _cinemachineTargetYaw += input.LookDirection.x * Runner.DeltaTime * Sensitivity;
            _cinemachineTargetPitch += -input.LookDirection.y * Runner.DeltaTime * Sensitivity;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        Vector3 Direction = new Vector3(
            Mathf.Cos(_cinemachineTargetPitch * Mathf.Deg2Rad) * Mathf.Sin(_cinemachineTargetYaw * Mathf.Deg2Rad),
            Mathf.Sin(-_cinemachineTargetPitch * Mathf.Deg2Rad),
            Mathf.Cos(_cinemachineTargetPitch * Mathf.Deg2Rad) * Mathf.Cos(_cinemachineTargetYaw * Mathf.Deg2Rad)
        );

        playerObj.SetLookRotation(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw);
        target.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        aimTF.position = transform.position + Direction * AimDistance;
    }

    private void CameraHeight()
    {
        if (playerController.IsProne) target.transform.localPosition = new Vector3(target.transform.localPosition.x, proneCamHeight, target.transform.localPosition.z);
        else if (playerController.IsCrouch) target.transform.localPosition = new Vector3(target.transform.localPosition.x, crouchCamHeight, target.transform.localPosition.z);
        else target.transform.localPosition = new Vector3(target.transform.localPosition.x, standCamHeight, target.transform.localPosition.z);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}
