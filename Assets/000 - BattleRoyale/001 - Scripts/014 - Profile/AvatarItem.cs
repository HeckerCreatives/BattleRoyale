using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarItem : MonoBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private AvatarController controller;
    [SerializeField] private string id;

    [Space]
    [SerializeField] private Image avatarImg;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color enabledColor;
    [SerializeField] private Color unselectedColor;

    private void OnEnable()
    {
        CheckAvatarStatus();
        controller.OnSelectedAvatarChange += SelectedChange;
    }

    public void OnDisable()
    {
        controller.OnSelectedAvatarChange -= SelectedChange;
    }

    private void SelectedChange(object sender, EventArgs e)
    {
        CheckAvatarStatus();
    }

    public void SelectAvatar()
    {
        controller.SelectedAvatar = id;
    }

    private void CheckAvatarStatus()
    {
        Debug.Log("SHOULD CHECK AVATAR STATS");
        if (controller.SelectedAvatar == id)
        {
            if (userData.AvatarID == id)
                avatarImg.color = enabledColor;
            else
                avatarImg.color = selectedColor;
        }
        else
        {
            if (userData.AvatarID == id)
                avatarImg.color = enabledColor;
            else
                avatarImg.color = unselectedColor;
        }
    }
}
