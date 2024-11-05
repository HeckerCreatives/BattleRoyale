
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GraphicsController : MonoBehaviour
{
    private event EventHandler GraphicsQualityChanged;
    public event EventHandler OnGraphicsQualityChange
    {
        add
        {
            if (GraphicsQualityChanged == null || !GraphicsQualityChanged.GetInvocationList().Contains(value))
                    GraphicsQualityChanged += value;
        }
        remove { GraphicsQualityChanged -= value; }
    }
    public int CurrentGraphicsQualityIndex
    {
        get => currentGraphicsQualityIndex;
        set
        {
            currentGraphicsQualityIndex = value;
            GraphicsQualityChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private event EventHandler ResolutionChanged;
    public event EventHandler OnResolutionChange
    {
        add
        {
            if (ResolutionChanged == null || !ResolutionChanged.GetInvocationList().Contains(value))
                ResolutionChanged += value;
        }
        remove { ResolutionChanged -= value; }
    }
    public int CurrentResolutionIndex
    {
        get => currentResolutionIndex;
        set
        {
            currentResolutionIndex = value;
            ResolutionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private event EventHandler FrameRateChanged;
    public event EventHandler OnFrameRateChange
    {
        add
        {
            if (FrameRateChanged == null || !FrameRateChanged.GetInvocationList().Contains(value))
                FrameRateChanged += value;
        }
        remove { FrameRateChanged -= value; }
    }
    public int CurrentFrameRateIndex
    {
        get => currentFrameRateIndex;
        set
        {
            currentFrameRateIndex = value;
            FrameRateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private event EventHandler ShadowChanged;
    public event EventHandler OnShadowChange
    {
        add
        {
            if (ShadowChanged == null || !ShadowChanged.GetInvocationList().Contains(value))
                ShadowChanged += value;
        }
        remove { ShadowChanged -= value; }
    }
    public int CurrentShadowIndex
    {
        get => currentShadowIndex;
        set
        {
            currentShadowIndex = value;
            ShadowChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private event EventHandler AntiAliasingChanged;
    public event EventHandler OnAntiAliasingChange
    {
        add
        {
            if (AntiAliasingChanged == null || !AntiAliasingChanged.GetInvocationList().Contains(value))
                AntiAliasingChanged += value;
        }
        remove { AntiAliasingChanged -= value; }
    }
    public int CurrentAntiAliasingIndex
    {
        get => currentAntiAliasingIndex;
        set
        {
            currentAntiAliasingIndex = value;
            AntiAliasingChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private event EventHandler BrightnessChanged;
    public event EventHandler OnBrightnessChange
    {
        add
        {
            if (BrightnessChanged == null || !BrightnessChanged.GetInvocationList().Contains(value))
                BrightnessChanged += value;
        }
        remove { BrightnessChanged -= value; }
    }
    public float CurrentBrightness
    {
        get => currentBrightness;
        set
        {
            currentBrightness = value;
            BrightnessChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [Header("RENDER PIPELINE")]
    [SerializeField] private UniversalRenderPipelineAsset lowPipeline;
    [SerializeField] private UniversalRenderPipelineAsset mediumPipeline;
    [SerializeField] private UniversalRenderPipelineAsset highPipeline;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] private int currentGraphicsQualityIndex;
    [ReadOnly][SerializeField] private int currentResolutionIndex;
    [ReadOnly][SerializeField] private int currentFrameRateIndex;
    [ReadOnly][SerializeField] private int currentShadowIndex;
    [ReadOnly][SerializeField] private int currentAntiAliasingIndex;
    [ReadOnly][SerializeField] private float currentBrightness;

    private void Awake()
    {
        SetDataOnAwake();
        OnGraphicsQualityChange += GraphicsChange;
        OnResolutionChange += ResolutionChange;
        OnFrameRateChange += FrameRateChange;
        OnShadowChange += ShadowChange;
        OnAntiAliasingChange += AntiAliasingChange;
        OnBrightnessChange += BrightnessChange;
    }

    private void OnDisable()
    {
        OnGraphicsQualityChange -= GraphicsChange;
        OnResolutionChange -= ResolutionChange;
        OnFrameRateChange -= FrameRateChange;
        OnShadowChange -= ShadowChange;
        OnAntiAliasingChange -= AntiAliasingChange;
        OnBrightnessChange -= BrightnessChange;
    }

    private void BrightnessChange(object sender, EventArgs e)
    {
        PlayerPrefs.SetFloat("Brightness", CurrentBrightness);
    }

    private void AntiAliasingChange(object sender, EventArgs e)
    {
        ChangeAntiAliasing();
    }

    private void ShadowChange(object sender, EventArgs e)
    {
        ChangeShadow();
    }

    private void FrameRateChange(object sender, EventArgs e)
    {
        ChangeFrameRate();
    }

    private void ResolutionChange(object sender, EventArgs e)
    {
        ChangeResolution();
    }

    private void GraphicsChange(object sender, EventArgs e)
    {
        ChangeGraphicsQuality();
    }

    private void SetDataOnAwake()
    {
        if (PlayerPrefs.HasKey("GraphicsQuality"))
        {
            CurrentGraphicsQualityIndex = PlayerPrefs.GetInt("GraphicsQuality");
            QualitySettings.SetQualityLevel(CurrentGraphicsQualityIndex, false);
        }
        else
        {
            CurrentGraphicsQualityIndex = 0;
            PlayerPrefs.SetInt("GraphicsQuality", 0);
            QualitySettings.SetQualityLevel(CurrentGraphicsQualityIndex, false);
        }

        if (PlayerPrefs.HasKey("Resolution"))
        {
            CurrentResolutionIndex = PlayerPrefs.GetInt("Resolution");
            ChangeResolution();
        }
        else
        {
            CurrentResolutionIndex = 0;
            ChangeResolution();
        }

        if (PlayerPrefs.HasKey("FrameRate"))
        {
            CurrentFrameRateIndex = PlayerPrefs.GetInt("FrameRate");
            ChangeFrameRate();
        }
        else
        {
            CurrentFrameRateIndex = 1;
            ChangeFrameRate();
        }

        if (PlayerPrefs.HasKey("Shadow"))
        {
            CurrentShadowIndex = PlayerPrefs.GetInt("Shadow");
            ChangeShadow();
        }
        else
        {
            CurrentShadowIndex = 1;
            ChangeShadow();
        }

        if (PlayerPrefs.HasKey("AntiAliasing"))
        {
            CurrentAntiAliasingIndex = PlayerPrefs.GetInt("AntiAliasing");
            ChangeAntiAliasing();
        }
        else
        {
            CurrentAntiAliasingIndex = 0;
            ChangeAntiAliasing();
        }

        if (PlayerPrefs.HasKey("Brightness"))
        {
            CurrentBrightness = PlayerPrefs.GetFloat("Brightness");
        }
        else
        {
            CurrentBrightness = 0.5f;
            PlayerPrefs.SetFloat("Brightness", 0.5f);
        }
    }

    private void ChangeGraphicsQuality()
    {
        PlayerPrefs.SetInt("GraphicsQuality", CurrentGraphicsQualityIndex);
        QualitySettings.SetQualityLevel(CurrentGraphicsQualityIndex, false);
    }

    private void ChangeResolution()
    {
        PlayerPrefs.SetInt("Resolution", CurrentResolutionIndex);

        float renderScale = CurrentResolutionIndex switch
        {
            0 => 0.5f,
            1 => 1f,
            2 => 2f,
            _ => 1f,
        };

        switch (CurrentGraphicsQualityIndex)
        {
            case 0:
                lowPipeline.renderScale = renderScale;
                break;
            case 1:
                mediumPipeline.renderScale = renderScale;
                break;
            case 2:
                highPipeline.renderScale = renderScale;
                break;
            default:
                lowPipeline.renderScale = renderScale;
                break;
        }
    }

    private void ChangeFrameRate()
    {
        PlayerPrefs.SetInt("FrameRate", CurrentFrameRateIndex);

        switch (CurrentFrameRateIndex)
        {
            case 0:
                Application.targetFrameRate = 20;
                break;
            case 1:
                Application.targetFrameRate = 30;
                break;
            case 2:
                Application.targetFrameRate = 60;
                break;
            default:
                break;
        }
    }

    private void ChangeShadow()
    {
        PlayerPrefs.SetInt("Shadow", CurrentShadowIndex);

        float shadowAvailability = CurrentShadowIndex switch
        {
            0 => 0,
            1 => 50,
            2 => 100,
            3 => 150,
            _ => 0,
        };

        int shadowCascade = CurrentShadowIndex switch
        {
            0 => 1,
            1 => 1,
            2 => 2,
            3 => 4,
            _ => 1,
        };

        switch (CurrentGraphicsQualityIndex)
        {
            case 0:
                lowPipeline.shadowDistance = shadowAvailability;
                lowPipeline.shadowCascadeCount = shadowCascade;
                break;
            case 1:
                mediumPipeline.shadowDistance = shadowAvailability;
                mediumPipeline.shadowCascadeCount = shadowCascade;
                break;
            case 2:
                highPipeline.shadowDistance = shadowAvailability;
                highPipeline.shadowCascadeCount = shadowCascade;
                break;
            default:
                lowPipeline.shadowDistance = shadowAvailability;
                lowPipeline.shadowCascadeCount = shadowCascade;
                break;
        }
    }

    private void ChangeAntiAliasing()
    {
        PlayerPrefs.SetInt("AntiAliasing", CurrentAntiAliasingIndex);

        int antiAliasingValue = CurrentShadowIndex switch
        {
            0 => 0,
            1 => 2,
            2 => 4,
            3 => 8,
            _ => 0,
        };

        QualitySettings.antiAliasing = antiAliasingValue;
    }
}
