using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ControllerSettingDataRetriever : MonoBehaviour
{
    [SerializeField] private PlayerStamina stamina;
    [SerializeField] private float checkStamina;
    [SerializeField] private bool shouldCheckStamina;

    [Space]
    [SerializeField] private UserData userData;
    [SerializeField] private RectTransform uiRT;
    [SerializeField] private CanvasGroup uiImg;

    [Space]
    [SerializeField] private Image uiImage;
    [SerializeField] private Color downPressColor;
    [SerializeField] private Color upPress;
    [SerializeField] private Color disabledColor;

    [Header("DEBUGGER")]
    [SerializeField] private bool isDownPress;

    private void OnEnable()
    {
        SetUILayout();
    }

    private void Update()
    {
        if (shouldCheckStamina)
        {
            if (isDownPress)
            {
                uiImage.color = downPressColor;
                return;
            }

            if (stamina.Stamina >= checkStamina)
                uiImage.color = upPress;
            else
                uiImage.color = downPressColor;
        }
    }

    public void SetUILayout()
    {
        if (GameManager.Instance == null)
        {
            gameObject.SetActive(false);
            return;
        }

        float aspectRatio = (float)Screen.width / Screen.height;
        float scaleFactor = Mathf.Clamp(aspectRatio / (19.5f / 9f), 0.5f, 1f);

        // scale position too
        uiRT.anchoredPosition = new Vector2(
            userData.ControlSetting[gameObject.name].localPositionX * scaleFactor,
            userData.ControlSetting[gameObject.name].localPositionY * scaleFactor
        );

        // scale size
        uiRT.localScale = new Vector2(
            userData.ControlSetting[gameObject.name].sizeDeltaX * scaleFactor,
            userData.ControlSetting[gameObject.name].sizeDeltaY * scaleFactor
        );

        uiImg.alpha = userData.ControlSetting[gameObject.name].opacity;
    }

    public float UILocalPosition(bool isY)
    {
        if (!isY) return uiRT.anchoredPosition.x;
        else return uiRT.anchoredPosition.y;
    }

    public float UISizeDelta(bool isY)
    {
        if (!isY) return uiRT.localScale.x;
        else return uiRT.localScale.y;
    }

    public float UIOpacity() => uiImg.alpha;

    public void DownPress()
    {
        isDownPress = true;
        uiImage.color = downPressColor;
    }

    public void UpPress()
    {
        isDownPress = false;

        if (shouldCheckStamina)
        {
            if (stamina.Stamina >= checkStamina)
                uiImage.color = disabledColor;

            return;
        }

        uiImage.color = upPress;
    }
}
