using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ControllerSettingDataRetriever : MonoBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private RectTransform uiRT;
    [SerializeField] private CanvasGroup uiImg;

    private void Awake()
    {
        SetUILayout();
    }

    public void SetUILayout()
    {
        uiRT.anchoredPosition = new Vector2(userData.ControlSetting[gameObject.name].localPositionX, userData.ControlSetting[gameObject.name].localPositionY);
        uiRT.sizeDelta = new Vector2(userData.ControlSetting[gameObject.name].sizeDeltaX, userData.ControlSetting[gameObject.name].sizeDeltaY);
        uiImg.alpha = userData.ControlSetting[gameObject.name].opacity;
    }

    public float UILocalPosition(bool isY)
    {
        if (!isY) return uiRT.anchoredPosition.x;
        else return uiRT.anchoredPosition.y;
    }

    public float UISizeDelta(bool isY)
    {
        if (!isY) return uiRT.sizeDelta.x;
        else return uiRT.sizeDelta.y;
    }

    public float UIOpacity() => uiImg.alpha;
}
