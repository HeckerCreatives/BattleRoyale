using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsControlGameplayController : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private RectTransform uiElementRectTransform;
    [SerializeField] private CanvasGroup uiImg;
    [SerializeField] private ControllerSetting controllerSetting;

    private Vector2 pointerOffset;
    private bool isDragging = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        // Calculate the offset between the pointer position and the UI element's position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(uiElementRectTransform, eventData.position, null, out pointerOffset);
        controllerSetting.SelectedUIImg = uiImg;
        controllerSetting.SelectedUIRT = uiElementRectTransform;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // Update the UI element's position based on the pointer position and offset with sensitivity
        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(uiElementRectTransform.parent as RectTransform, eventData.position, null, out localPointerPosition))
        {
            uiElementRectTransform.localPosition = (localPointerPosition - pointerOffset);
        }
    }
}
