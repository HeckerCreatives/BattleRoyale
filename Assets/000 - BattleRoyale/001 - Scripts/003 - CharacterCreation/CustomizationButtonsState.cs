using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.CullingGroup;

public class CustomizationButtonsState : MonoBehaviour
{
    [SerializeField] private CustomizationState state;
    [SerializeField] private CharacterCreationController characterCreationController;

    [Space]
    [SerializeField] private float xPosSelected;
    [SerializeField] private float xPosUnselected;
    [SerializeField] private RectTransform rectTransform;

    [Space]
    [SerializeField] private Image buttonImg;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color unselectedColor;

    private void OnEnable()
    {
        StateCheck();
        characterCreationController.OnCustomizationStateChange += StateChange;
    }

    private void OnDisable()
    {
        characterCreationController.OnCustomizationStateChange -= StateChange;
    }

    private void StateChange(object sender, EventArgs e)
    {
        StateCheck();
    }

    private void StateCheck()
    {
        if (characterCreationController.CurrentCustomizationState == state)
        {
            buttonImg.color = selectedColor;
            rectTransform.localPosition = new Vector2(xPosSelected, rectTransform.localPosition.y);
        }
        else
        {
            buttonImg.color = unselectedColor;
            rectTransform.localPosition = new Vector2(xPosUnselected, rectTransform.localPosition.y);
        }
    }

    public void ChangeState()
    {
        if (characterCreationController.CurrentCustomizationState == state)
            return;

        characterCreationController.CurrentCustomizationState = state;
    }
}
