using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerOnScreenStick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private PlayerController _playerController;

    [Space]
    public RectTransform joystickArea; // The touch area for the joystick
    public RectTransform joystickHandle; // The handle of the joystick

    [Header("DEBUGGER")]

    [SerializeField] private Vector2 startPosition;
    [SerializeField] private Vector3 inputVector;

    public virtual void OnDrag(PointerEventData ped)
    {
        Vector2 pos;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickArea, ped.position, ped.pressEventCamera, out pos))
        {
            pos.x = (pos.x / joystickArea.sizeDelta.x);
            pos.y = (pos.y / joystickArea.sizeDelta.y);

            inputVector = new Vector2((pos.x - 0.5f) * 2f, (pos.y - 0.5f) * 2f);
            inputVector = Vector2.ClampMagnitude(inputVector, 1.0f);

            joystickHandle.anchoredPosition = new Vector2(
                inputVector.x * (joystickArea.sizeDelta.x / 3),
                inputVector.y * (joystickArea.sizeDelta.y / 3)
            );

            _playerController.gameplayController.MovementDirection = inputVector;
        }
    }

    public virtual void OnPointerDown(PointerEventData ped)
    {
        OnDrag(ped);
    }

    public virtual void OnPointerUp(PointerEventData ped)
    {
        inputVector = Vector3.zero;
        _playerController.gameplayController.MovementDirection = inputVector;
        joystickHandle.anchoredPosition = Vector3.zero;
    }
}
