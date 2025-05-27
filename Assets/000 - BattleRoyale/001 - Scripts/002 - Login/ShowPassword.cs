using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShowPassword : MonoBehaviour
{
    [SerializeField] private TMP_InputField passwordTMP;
    [SerializeField] private Image passwordImg;
    [SerializeField] private Sprite offShow;
    [SerializeField] private Sprite onShow;

    [Header("DEBUGGER")]
    [SerializeField] private bool isShowing;

    private void OnEnable()
    {
        isShowing = false;
        Show();
    }

    public void Show(bool change = false)
    {
        if (change)
            isShowing = !isShowing;

        passwordTMP.contentType = isShowing ? TMP_InputField.ContentType.Alphanumeric : TMP_InputField.ContentType.Password;

        passwordTMP.ForceLabelUpdate();

        passwordImg.sprite = isShowing ? onShow : offShow;
    }
}
