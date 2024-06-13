using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SettingsState
{
    AUDIO,
    GRAPHICS,
    CONTROLS
}

public class GameSettingController : MonoBehaviour
{
    private event EventHandler SettingStateChange;
    public event EventHandler OnSettingStateChange
    {
        add
        {
            if (SettingStateChange == null || !SettingStateChange.GetInvocationList().Contains(value))
                SettingStateChange += value;
        }
        remove { SettingStateChange -= value; }
    }
    public SettingsState CurrentState
    {
        get => currentState;
        set
        {
            if (currentState != lastCurrentState) lastCurrentState = currentState;
            currentState = value;
            SettingStateChange?.Invoke(this, EventArgs.Empty);
        }
    }
    public SettingsState LastCurrentState
    {
        get => lastCurrentState;
        set => lastCurrentState = value;
    }

    [SerializeField] private List<GameObject> settingPanels;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] private SettingsState currentState;
    [ReadOnly][SerializeField] private SettingsState lastCurrentState;


    public void ChangeStatePanel(int index)
    {
        CurrentState = (SettingsState)index;

        settingPanels[(int)LastCurrentState].SetActive(false);

        settingPanels[index].SetActive(true);
    }
}
