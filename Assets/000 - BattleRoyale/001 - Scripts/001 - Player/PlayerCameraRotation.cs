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

    [Header("Parameters")]
    [SerializeField] private float Sensitivity = 1f;
    [SerializeField] private float TopClamp = 70.0f;
    [SerializeField] private float BottomClamp = -30.0f;
    [SerializeField] private float CameraAngleOverride = 0.0f;

    [Header("DEBUGGER")]
    [MyBox.ReadOnly][SerializeField] private float _threshold = 0.01f;
    [MyBox.ReadOnly][SerializeField] private float _cinemachineTargetYaw;
    [MyBox.ReadOnly][SerializeField] private float _cinemachineTargetPitch;

    //  =============================

    private Dictionary<int, bool> touchOverUI = new Dictionary<int, bool>();

    [Networked] private NetworkButtons PreviousButtons { get; set; }

    //  =============================

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
        if (!HasInputAuthority) return;

#if UNITY_EDITOR
        if (GetInput<MyInput>(out var input) == false) return;

        if (input.LookDirection.sqrMagnitude >= _threshold)
        {
            _cinemachineTargetYaw += input.LookDirection.x * Time.deltaTime * Sensitivity;
            _cinemachineTargetPitch += -input.LookDirection.y * Time.deltaTime * Sensitivity;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        playerObj.SetLookRotation(_cinemachineTargetPitch, _cinemachineTargetYaw);
        //playerObj.transform.rotation = Quaternion.Euler(0.0f, _cinemachineTargetYaw, 0.0f);
        //playerObj.Transform.rotation = Quaternion.Euler(0.0f, _cinemachineTargetYaw, 0.0f);
        target.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);

        PreviousButtons = input.Buttons;
#else
        foreach (var touch in Touchscreen.current.touches)
        {
            int touchId = touch.touchId.ReadValue();
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                touchOverUI[touchId] = IsTouchOverUI(touch.position);

            if (touchOverUI.ContainsKey(touchId) && touchOverUI[touchId])
                continue;

            Vector2 touchDelta = touch.delta.ReadValue();

            if (touchDelta.sqrMagnitude >= _threshold)
            {
                _cinemachineTargetYaw += touchDelta.x * Time.deltaTime * Sensitivity;
                _cinemachineTargetPitch += -touchDelta.y * Time.deltaTime * Sensitivity;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            target.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
            playerObj.transform.rotation = Quaternion.Euler(0.0f, _cinemachineTargetYaw, 0.0f);
        }
#endif
    }

    private void CameraHeight()
    {
        if (playerController.IsProne) target.transform.localPosition = new Vector3(target.transform.localPosition.x, proneCamHeight, target.transform.localPosition.z);
        else if (playerController.IsCrouch) target.transform.localPosition = new Vector3(target.transform.localPosition.x, crouchCamHeight, target.transform.localPosition.z);
        else target.transform.localPosition = new Vector3(target.transform.localPosition.x, standCamHeight, target.transform.localPosition.z);
    }

    private bool IsTouchOverUI(Vector2Control touchPosition)
    {
        Vector2 touchPos = touchPosition.ReadValue();

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = touchPos;

        return EventSystem.current.IsPointerOverGameObject(eventData.pointerId);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

}
