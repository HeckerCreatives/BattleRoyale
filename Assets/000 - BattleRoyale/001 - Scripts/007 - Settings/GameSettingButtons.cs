using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameSettingButtons : MonoBehaviour
{
    [SerializeField] private SettingsState state;
    [SerializeField] private GameSettingController controller;
    [SerializeField] private Sprite selected;
    [SerializeField] private Sprite unselected;
    [SerializeField] private Image buttonImg;

    private void OnEnable()
    {
        CheckButtons();
        controller.OnSettingStateChange += SettingStateChange;
    }

    private void OnDisable()
    {
        controller.OnSettingStateChange -= SettingStateChange;
    }

    private void SettingStateChange(object sender, EventArgs e)
    {
        CheckButtons();
    }

    private void CheckButtons()
    {
        if (state == controller.CurrentState) buttonImg.sprite = selected;
        else buttonImg.sprite = unselected;
    }
}
