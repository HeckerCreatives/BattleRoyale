using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingController : MonoBehaviour
{
    [SerializeField] private Volume postProcessing;

    [SerializeField] private float maxBrightness;

    //  ============================

    ColorAdjustments colorAdjustments;

    //  ============================

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            ChangeBrightness();
            GameManager.Instance.GraphicsManager.OnBrightnessChange += BrightnessChange;
        }   
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            ChangeBrightness();
            GameManager.Instance.GraphicsManager.OnBrightnessChange -= BrightnessChange;
        }
    }

    private void BrightnessChange(object sender, EventArgs e)
    {
        ChangeBrightness();
    }

    private void ChangeBrightness()
    {
        if (postProcessing.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            colorAdjustments.postExposure.value = maxBrightness * GameManager.Instance.GraphicsManager.CurrentBrightness;
        }
    }
}
