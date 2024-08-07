using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

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

    [Header("AUDIOS")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider ambientVolumeSlider;

    [Header("AUDIO VOLUME TEXT")]
    [SerializeField] private TextMeshProUGUI masterVolumeTMP;
    [SerializeField] private TextMeshProUGUI musicVolumeTMP;
    [SerializeField] private TextMeshProUGUI sfxVolumeTMP;
    [SerializeField] private TextMeshProUGUI ambientVolumeTMP;

    [Header("AUDIO VOLUME BUTTON")]
    [SerializeField] private Button masterVolumeReduce;
    [SerializeField] private Button masterVolumeAdd;
    [SerializeField] private Button musicVolumeVolumeReduce;
    [SerializeField] private Button musicVolumeVolumeAdd;
    [SerializeField] private Button sfxVolumeVolumeReduce;
    [SerializeField] private Button sfxVolumeVolumeAdd;
    [SerializeField] private Button ambientVolumeVolumeReduce;
    [SerializeField] private Button ambientVolumeVolumeAdd;

    [Header("GRAPHICS")]
    [SerializeField] private Volume postProcesing;
    [SerializeField] private float maxBrightness;
    [SerializeField] private TMP_Dropdown graphicsDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown frameRateDropdown;
    [SerializeField] private TMP_Dropdown shadowDropdown;
    [SerializeField] private TMP_Dropdown antiAliasingDropdown;
    [SerializeField] private Slider brightnessSlider;

    [Header("GRAPHICS TEXT")]
    [SerializeField] private TextMeshProUGUI brightnessTMP;

    [Header("GRAPHICS BUTTON")]
    [SerializeField] private Button brightnessReduce;
    [SerializeField] private Button brightnessAdd;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] private SettingsState currentState;
    [ReadOnly][SerializeField] private SettingsState lastCurrentState;

    //  ============================

    ColorAdjustments colorAdjustments;

    //  ============================

    public IEnumerator SetVolumeSlidersOnStart()
    {
        masterVolumeSlider.value = GameManager.Instance.AudioController.CurrentVolume;
        masterVolumeTMP.text = $"{masterVolumeSlider.value * 100:n0}";
        CheckMasterVolumeButtons();

        musicVolumeSlider.value = GameManager.Instance.AudioController.CurrentMusicVolume;
        musicVolumeTMP.text = $"{musicVolumeSlider.value * 100:n0}";
        CheckMusicVolumeButtons();

        sfxVolumeSlider.value = GameManager.Instance.AudioController.CurrentSFXVolume;
        sfxVolumeTMP.text = $"{sfxVolumeSlider.value * 100:n0}";
        CheckSFXVolumeButtons();

        ambientVolumeSlider.value = GameManager.Instance.AudioController.CurrentAmbientVolume;
        ambientVolumeTMP.text = $"{ambientVolumeSlider.value * 100:n0}";
        CheckAmbientVolumeButtons();

        yield return null;
    }

    public IEnumerator SetGraphicsOnStart()
    {
        graphicsDropdown.value = GameManager.Instance.GraphicsManager.CurrentGraphicsQualityIndex;
        graphicsDropdown.RefreshShownValue();

        resolutionDropdown.value = GameManager.Instance.GraphicsManager.CurrentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        frameRateDropdown.value = GameManager.Instance.GraphicsManager.CurrentFrameRateIndex;
        frameRateDropdown.RefreshShownValue();

        shadowDropdown.value = GameManager.Instance.GraphicsManager.CurrentShadowIndex;
        shadowDropdown.RefreshShownValue();

        antiAliasingDropdown.value = GameManager.Instance.GraphicsManager.CurrentAntiAliasingIndex;
        antiAliasingDropdown.RefreshShownValue();

        brightnessSlider.value = GameManager.Instance.GraphicsManager.CurrentBrightness;
        if (postProcesing.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            colorAdjustments.postExposure.value = maxBrightness * GameManager.Instance.GraphicsManager.CurrentBrightness;
        }
        brightnessTMP.text = $"{brightnessSlider.value * 100:n0}";

        yield return null;
    }

    #region AUDIO

    public void ChangeMasterVolumeSlider()
    {
        GameManager.Instance.AudioController.CurrentVolume = masterVolumeSlider.value;

        masterVolumeTMP.text = $"{masterVolumeSlider.value * 100:n0}";
        CheckMasterVolumeButtons();
    }

    public void ChangeMusicVolumeSlider()
    {
        GameManager.Instance.AudioController.CurrentMusicVolume = musicVolumeSlider.value;

        musicVolumeTMP.text = $"{musicVolumeSlider.value * 100:n0}";
        CheckMusicVolumeButtons();
    }

    public void ChangeSFXVolumeSlider()
    {
        GameManager.Instance.AudioController.CurrentSFXVolume = sfxVolumeSlider.value;
        sfxVolumeTMP.text = $"{sfxVolumeSlider.value * 100:n0}";
        CheckSFXVolumeButtons();
    }

    public void SetAmbientVolumeSlider()
    {
        GameManager.Instance.AudioController.CurrentAmbientVolume = ambientVolumeSlider.value;
        ambientVolumeTMP.text = $"{ambientVolumeSlider.value * 100:n0}";
        CheckAmbientVolumeButtons();
    }

    #region BUTTON

    public void MasterVolumeAddReduce(bool isAdd)
    {
        if (isAdd)
            GameManager.Instance.AudioController.CurrentVolume += 0.01f;
        else
            GameManager.Instance.AudioController.CurrentVolume -= 0.01f;

        masterVolumeSlider.value = GameManager.Instance.AudioController.CurrentVolume;
        masterVolumeTMP.text = $"{masterVolumeSlider.value * 100:n0}";

        CheckMasterVolumeButtons();
    }

    private void CheckMasterVolumeButtons()
    {
        if (masterVolumeSlider.value <= 0)
        {
            masterVolumeReduce.interactable = false;
            masterVolumeAdd.interactable = true;
        }
        else if (masterVolumeSlider.value > 0 && masterVolumeSlider.value < 1)
        {
            masterVolumeReduce.interactable = true;
            masterVolumeAdd.interactable = true;
        }
        else if (masterVolumeSlider.value >= 1)
        {
            masterVolumeReduce.interactable = true;
            masterVolumeAdd.interactable = false;
        }
    }

    public void MusicVolumeAddReduce(bool isAdd)
    {
        if (isAdd)
            GameManager.Instance.AudioController.CurrentMusicVolume += 0.01f;
        else
            GameManager.Instance.AudioController.CurrentMusicVolume -= 0.01f;

        musicVolumeSlider.value = GameManager.Instance.AudioController.CurrentMusicVolume;
        musicVolumeTMP.text = $"{musicVolumeSlider.value * 100:n0}";

        CheckMusicVolumeButtons();
    }

    private void CheckMusicVolumeButtons()
    {
        if (musicVolumeSlider.value <= 0)
        {
            musicVolumeVolumeReduce.interactable = false;
            musicVolumeVolumeAdd.interactable = true;
        }
        else if (musicVolumeSlider.value > 0 && musicVolumeSlider.value < 1)
        {
            musicVolumeVolumeReduce.interactable = true;
            musicVolumeVolumeAdd.interactable = true;
        }
        else if (musicVolumeSlider.value >= 1)
        {
            musicVolumeVolumeReduce.interactable = true;
            musicVolumeVolumeAdd.interactable = false;
        }
    }

    public void SFXVolumeAddReduec(bool isAdd)
    {
        if (isAdd)
            GameManager.Instance.AudioController.CurrentSFXVolume += 0.01f;
        else
            GameManager.Instance.AudioController.CurrentSFXVolume -= 0.01f;

        sfxVolumeSlider.value = GameManager.Instance.AudioController.CurrentSFXVolume;
        sfxVolumeTMP.text = $"{sfxVolumeSlider.value * 100:n0}";

        CheckSFXVolumeButtons();
    }

    private void CheckSFXVolumeButtons()
    {
        if (sfxVolumeSlider.value <= 0)
        {
            sfxVolumeVolumeReduce.interactable = false;
            sfxVolumeVolumeAdd.interactable = true;
        }
        else if (sfxVolumeSlider.value > 0 && sfxVolumeSlider.value < 1)
        {
            sfxVolumeVolumeReduce.interactable = true;
            sfxVolumeVolumeAdd.interactable = true;
        }
        else if (sfxVolumeSlider.value >= 1)
        {
            sfxVolumeVolumeReduce.interactable = true;
            sfxVolumeVolumeAdd.interactable = false;
        }
    }

    public void AmbientVolumeAddReduec(bool isAdd)
    {
        if (isAdd)
            GameManager.Instance.AudioController.CurrentAmbientVolume += 0.01f;
        else
            GameManager.Instance.AudioController.CurrentAmbientVolume -= 0.01f;

        ambientVolumeSlider.value = GameManager.Instance.AudioController.CurrentAmbientVolume;
        ambientVolumeTMP.text = $"{sfxVolumeSlider.value * 100:n0}";

        CheckAmbientVolumeButtons();
    }

    private void CheckAmbientVolumeButtons()
    {
        if (ambientVolumeSlider.value <= 0)
        {
            ambientVolumeVolumeReduce.interactable = false;
            ambientVolumeVolumeAdd.interactable = true;
        }
        else if (ambientVolumeSlider.value > 0 && ambientVolumeSlider.value < 1)
        {
            ambientVolumeVolumeReduce.interactable = true;
            ambientVolumeVolumeAdd.interactable = true;
        }
        else if (ambientVolumeSlider.value >= 1)
        {
            ambientVolumeVolumeReduce.interactable = true;
            ambientVolumeVolumeAdd.interactable = false;
        }
    }

    #endregion

    #endregion

    #region GRAPHICS

    public void SetBrightnessSlider()
    {
        GameManager.Instance.GraphicsManager.CurrentBrightness = brightnessSlider.value;
        if (postProcesing.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            colorAdjustments.postExposure.value = maxBrightness * GameManager.Instance.GraphicsManager.CurrentBrightness;
        }

        brightnessTMP.text = $"{brightnessSlider.value * 100:n0}";
        CheckBrightnessButtons();
    }

    public void ChangeGraphicsDropdown(int index)
    {
        GameManager.Instance.GraphicsManager.CurrentGraphicsQualityIndex = index;

        if (graphicsDropdown.value == 0)
        {
            resolutionDropdown.value = 0;
            resolutionDropdown.RefreshShownValue();

            frameRateDropdown.value = 1;
            frameRateDropdown.RefreshShownValue();

            shadowDropdown.value = 0;
            shadowDropdown.RefreshShownValue();

            antiAliasingDropdown.value = 0;
            antiAliasingDropdown.RefreshShownValue();
        }
        else if (graphicsDropdown.value == 1)
        {
            resolutionDropdown.value = 1;
            resolutionDropdown.RefreshShownValue();

            frameRateDropdown.value = 1;
            frameRateDropdown.RefreshShownValue();

            shadowDropdown.value = 2;
            shadowDropdown.RefreshShownValue();

            antiAliasingDropdown.value = 1;
            antiAliasingDropdown.RefreshShownValue();
        }
        else if (graphicsDropdown.value == 2)
        {
            resolutionDropdown.value = 2;
            resolutionDropdown.RefreshShownValue();

            frameRateDropdown.value = 2;
            frameRateDropdown.RefreshShownValue();

            shadowDropdown.value = 3;
            shadowDropdown.RefreshShownValue();

            antiAliasingDropdown.value = 3;
            antiAliasingDropdown.RefreshShownValue();
        }
    }

    public void ChangeResolutionDropdown(int index)
    {
        GameManager.Instance.GraphicsManager.CurrentResolutionIndex = index;
    }

    public void ChangeFrameRateDropdown(int index)
    {
        GameManager.Instance.GraphicsManager.CurrentFrameRateIndex = index;
    }

    public void ChangeShadowDropdown(int index)
    {
        GameManager.Instance.GraphicsManager.CurrentShadowIndex = index;
    }

    public void ChangeAntiAliasingDropdown(int index)
    {
        GameManager.Instance.GraphicsManager.CurrentAntiAliasingIndex = index;
    }

    #region BUTTON

    public void BrightnessAddReduce(bool isAdd)
    {
        if (isAdd)
            GameManager.Instance.GraphicsManager.CurrentBrightness += 0.01f;
        else
            GameManager.Instance.GraphicsManager.CurrentBrightness -= 0.01f;

        brightnessSlider.value = GameManager.Instance.GraphicsManager.CurrentBrightness;
        brightnessTMP.text = $"{brightnessSlider.value * 100:n0}";

        CheckBrightnessButtons();
    }

    private void CheckBrightnessButtons()
    {
        if (brightnessSlider.value <= 0)
        {
            brightnessReduce.interactable = false;
            brightnessAdd.interactable = true;
        }
        else if (brightnessSlider.value > 0 && brightnessSlider.value < 1)
        {
            brightnessReduce.interactable = true;
            brightnessAdd.interactable = true;
        }
        else if (brightnessSlider.value >= 1)
        {
            brightnessReduce.interactable = true;
            brightnessAdd.interactable = false;
        }
    }

    #endregion

    #endregion

    public void ChangeStatePanel(int index)
    {
        CurrentState = (SettingsState)index;

        settingPanels[(int)LastCurrentState].SetActive(false);

        settingPanels[index].SetActive(true);
    }
}
