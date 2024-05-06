using Cinemachine;
using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerCameraRotation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameplayController controller;
    [SerializeField] private GameObject target;
    [SerializeField] private GameObject playerObj;

    [Header("Parameters")]
    [SerializeField] private float Sensitivity = 1f;
    [SerializeField] private float TopClamp = 70.0f;
    [SerializeField] private float BottomClamp = -30.0f;
    [SerializeField] private float CameraAngleOverride = 0.0f;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] private float _threshold = 0.01f;
    [ReadOnly][SerializeField] private float _cinemachineTargetYaw;
    [ReadOnly][SerializeField] private float _cinemachineTargetPitch;

    //  =============================

    private Dictionary<int, bool> touchOverUI = new Dictionary<int, bool>();

    //  =============================

    private void LateUpdate()
    {
        HandleMobileCameraInput();
    }

    private void HandleMobileCameraInput()
    {
#if UNITY_EDITOR
        if (controller.LookDirection.sqrMagnitude >= _threshold)
        {
            _cinemachineTargetYaw += controller.LookDirection.x * Time.deltaTime * Sensitivity;
            _cinemachineTargetPitch += -controller.LookDirection.y * Time.deltaTime * Sensitivity;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        target.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        playerObj.transform.rotation = Quaternion.Euler(0.0f, _cinemachineTargetYaw, 0.0f);
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
                _cinemachineTargetPitch += touchDelta.y * Time.deltaTime * Sensitivity;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            target.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
            playerObj.transform.rotation = Quaternion.Euler(0.0f, _cinemachineTargetYaw, 0.0f);
        }
#endif
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
