using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerCameraRotation : MonoBehaviour
{
    [Header("References")]
    public CinemachineFreeLook freeLookCam;

    [Header("Parameters")]
    public float rotationSpeedX = 1f;
    public float rotationSpeedY = 0.1f; // Adjust this value to control sensitivity on the Y-axis

    // Dictionary to track touch IDs and their UI interaction status
    private Dictionary<int, bool> touchOverUI = new Dictionary<int, bool>();

    private void Update()
    {
        HandleMobileCameraInput();
    }

    private void HandleMobileCameraInput()
    {
        // Iterate through all active touches
        foreach (var touch in Touchscreen.current.touches)
        {
            int touchId = touch.touchId.ReadValue();
            // Check if the touch just started
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
            {
                // Set the UI state for this touch based on whether it started over a UI element
                touchOverUI[touchId] = IsTouchOverUI(touch.position);
            }

            // Check if the touch started over a UI element
            if (touchOverUI.ContainsKey(touchId) && touchOverUI[touchId])
            {
                // Ignore camera rotation for this touch
                continue;
            }

            // Adjust the camera rotation based on the touch input
            // Adjust the camera rotation based on the touch input
            Vector2 touchDelta = touch.delta.ReadValue();
            freeLookCam.m_XAxis.Value -= touchDelta.x * rotationSpeedX;
            freeLookCam.m_YAxis.Value += touchDelta.y * rotationSpeedY;
        }
    }

    // Check if a touch is over any UI element
    private bool IsTouchOverUI(Vector2Control touchPosition)
    {
        // Convert Vector2Control to Vector2
        Vector2 touchPos = touchPosition.ReadValue();

        // Set up the event data
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = touchPos;

        // Check if there is any UI element under the touch
        return EventSystem.current.IsPointerOverGameObject(eventData.pointerId);
    }
}
