using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameplaySettingsController : MonoBehaviour
{
    private event EventHandler LookSensitivityChanged;
    public event EventHandler OnLookSensitivityChanged
    {
        add
        {
            if (LookSensitivityChanged == null || !LookSensitivityChanged.GetInvocationList().Contains(value))
                LookSensitivityChanged += value;
        }
        remove { LookSensitivityChanged -= value; }
    }
    public float CurrentLookSensitivity
    {
        get => currentLookSensitivity;
        set
        {
            currentLookSensitivity = value;
            LookSensitivityChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private event EventHandler LookAdsSensitivityChanged;
    public event EventHandler OnLookAdsSensitivityChanged
    {
        add
        {
            if (LookAdsSensitivityChanged == null || !LookAdsSensitivityChanged.GetInvocationList().Contains(value))
                LookAdsSensitivityChanged += value;
        }
        remove { LookAdsSensitivityChanged -= value; }
    }
    public float CurrentLookAdsSensitivity
    {
        get => currentAdsLookSensitivity;
        set
        {
            currentAdsLookSensitivity = value;
            LookAdsSensitivityChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] private float currentLookSensitivity;
    [ReadOnly][SerializeField] private float currentAdsLookSensitivity;

    private void Awake()
    {
        CheckOnStart();

        OnLookSensitivityChanged += LookSensitivityChange;
        OnLookAdsSensitivityChanged += LookAdsSensitivityChange;
    }

    private void LookAdsSensitivityChange(object sender, EventArgs e)
    {
        PlayerPrefs.SetFloat("LookSensitivity", CurrentLookSensitivity);
    }

    private void LookSensitivityChange(object sender, EventArgs e)
    {
        PlayerPrefs.SetFloat("LookAdsSensitivity", CurrentLookAdsSensitivity);
    }

    private void CheckOnStart()
    {
        if (PlayerPrefs.HasKey("LookSensitivity"))
        {
            CurrentLookSensitivity = PlayerPrefs.GetFloat("LookSensitivity");
        }
        else
        {
            CurrentLookSensitivity = 0.5f;
        }

        if (PlayerPrefs.HasKey("LookAdsSensitivity"))
        {
            CurrentLookAdsSensitivity = PlayerPrefs.GetFloat("LookAdsSensitivity");
        }
        else
        {
            CurrentLookAdsSensitivity = 0.5f;
        }
    }
}
