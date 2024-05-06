using MyBox;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class ControllerSetting : MonoBehaviour
{
    private event EventHandler SelectedUIRTChange;
    public event EventHandler OnSelectedUIRTChange
    {
        add
        {
            if (SelectedUIRTChange == null || !SelectedUIRTChange.GetInvocationList().Contains(value))
                SelectedUIRTChange += value;
        }
        remove { SelectedUIRTChange -= value; }
    }
    public RectTransform SelectedUIRT
    {
        get => selectedUIRT;
        set
        {
            selectedUIRT = value;
            SelectedUIRTChange?.Invoke(this, EventArgs.Empty);
        }
    }
    public Image SelectedUIImg
    {
        get => selectedUIImg;
        set => selectedUIImg = value;
    }

    //  ==============================

    [SerializeField] private UserData userData; 

    [Header("SIZE")]
    [SerializeField] private float minXSize;
    [SerializeField] private float maxXSize;
    [SerializeField] private float minYSize;
    [SerializeField] private float maxYSize;
    [SerializeField] private Slider sizeSlider;

    [Header("OPACITY")]
    [SerializeField] private float minOpacity;
    [SerializeField] private float maxOpacity;
    [SerializeField] private Slider opacitySlider;

    [Header("SAVE")]
    [SerializeField] private List<ControllerSettingDataRetriever> controllerDataRetriever;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] private RectTransform selectedUIRT;
    [ReadOnly][SerializeField] private Image selectedUIImg;
    [ReadOnly][SerializeField] private float initialWidth;
    [ReadOnly][SerializeField] private float initialHeight;
    [ReadOnly][SerializeField] private float initialOpacity;

    private void Awake()
    {
        OnSelectedUIRTChange += UIChange;
    }

    private void OnDisable()
    {
        OnSelectedUIRTChange -= UIChange;
    }

    private void UIChange(object sender, EventArgs e)
    {
        CheckUIValues();
        CheckSliders();
    }

    private void CheckSliders()
    {
        if (selectedUIImg == null) opacitySlider.enabled = false;
        else opacitySlider.enabled = true;

        if (selectedUIRT == null) sizeSlider.enabled = false;
        else sizeSlider.enabled = true;
    }

    private void CheckUIValues()
    {
        initialWidth = SelectedUIRT.sizeDelta.x;
        initialHeight = SelectedUIRT.sizeDelta.y;
        initialOpacity = SelectedUIImg.color.a;

        sizeSlider.value = (initialHeight - minYSize) / (maxYSize - minYSize) * (1f - 0f) + 0f;
        opacitySlider.value = (initialOpacity - minOpacity) / (maxOpacity - minOpacity) * (1f - 0f) + 0f;
    }

    public void OnSizeSliderChange()
    {
        float newSize = Mathf.Lerp(minXSize, maxXSize, sizeSlider.value);
        float ratio = initialWidth / initialHeight;
        selectedUIRT.sizeDelta = new Vector2(newSize * ratio, newSize); // Resize the UI element maintaining aspect ratio
    }

    public void OnOpacitySliderChange()
    {
        float newOpacity = Mathf.Lerp(minOpacity, maxOpacity, opacitySlider.value);
        Color elementColor = SelectedUIImg.color;
        elementColor.a = (newOpacity - minOpacity) / (maxOpacity - minOpacity) * (1f - 0f) + 0f;
        SelectedUIImg.color = elementColor;
    }

    public void ClearUIValues()
    {
        selectedUIRT = null;
        selectedUIImg = null;
        initialWidth = 0f;
        initialHeight = 0f;
        initialOpacity = 0f;
    }

    public void SetDefaultLayout()
    {
        GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to return to default layout?", () =>
        {
            GameManager.Instance.NoBGLoading.SetActive(true);
            userData.DefaultControllerLayout();
            StartCoroutine(DefaultLayout());
        }, null);
    }

    IEnumerator DefaultLayout()
    {
        foreach (var item in controllerDataRetriever)
        {
            item.SetUILayout();
            yield return null;
        }
        GameManager.Instance.NoBGLoading.SetActive(false);
    }

    public void SaveControllerSettings()
    {
        GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to save this controller layout?", () =>
        {
            GameManager.Instance.NoBGLoading.SetActive(true);
            StartCoroutine(SaveData());
        }, null);
    }

    IEnumerator SaveData()
    {
        Dictionary<string, ControllerSettingData> tempSettings = new Dictionary<string, ControllerSettingData>();

        foreach (var item in controllerDataRetriever)
        {
            tempSettings.Add(item.gameObject.name, new ControllerSettingData { sizeDeltaX = item.UISizeDelta(false), sizeDeltaY = item.UISizeDelta(true), localPositionX = item.UILocalPosition(false), localPositionY = item.UILocalPosition(true), opacity = item.UIOpacity() });
            yield return null;
        }

        userData.ControlSetting = tempSettings;

        PlayerPrefs.SetString("ControlSetting", JsonConvert.SerializeObject(tempSettings));

        GameManager.Instance.NoBGLoading.SetActive(false);
    }
}
