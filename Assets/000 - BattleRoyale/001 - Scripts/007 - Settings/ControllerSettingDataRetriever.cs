using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ControllerSettingDataRetriever : MonoBehaviour
{
    [SerializeField] private PlayerNetworkLoader playerNetworkLoader;
    [SerializeField] private bool isNetworked;
    [SerializeField] private UserData userData;
    [SerializeField] private RectTransform uiRT;
    [SerializeField] private CanvasGroup uiImg;

    [Space]
    [SerializeField] private Image uiImage;
    [SerializeField] private Color downPressColor;
    [SerializeField] private Color upPress;

    private async void Awake()
    {
        if (isNetworked)
        {
            while (!playerNetworkLoader.Runner) await Task.Delay(100);

            if (!playerNetworkLoader.HasInputAuthority) return;
        }

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

    public void DownPress() => uiImage.color = downPressColor;

    public void UpPress() => uiImage.color = upPress;
}
