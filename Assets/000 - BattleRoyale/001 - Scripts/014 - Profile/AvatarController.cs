using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AvatarController : MonoBehaviour
{
    private event EventHandler SelectedAvatarChange;
    public event EventHandler OnSelectedAvatarChange
    {
        add
        {
            if (SelectedAvatarChange == null || !SelectedAvatarChange.GetInvocationList().Contains(value))
                SelectedAvatarChange += value;
        }
        remove { SelectedAvatarChange -= value; }
    }
    public string SelectedAvatar
    {
        get => selectedAvatar;
        set
        {
            selectedAvatar = value;
            SelectedAvatarChange?.Invoke(this, EventArgs.Empty);
        }
    }


    [SerializeField] private UserData userData;

    [Space]
    [SerializeField] private GameObject avatarOne;
    [SerializeField] private GameObject avatarTwo;
    [SerializeField] private GameObject avatarThree;
    [SerializeField] private GameObject avatarFour;
    [SerializeField] private GameObject avatarFive;
    [SerializeField] private GameObject avatarSix;

    [Space]
    [SerializeField] private Button saveBtn;
    [SerializeField] private GameObject avatarChangerObj;

    [Header("DEBUGGER")]
    [SerializeField] private string selectedAvatar;

    private void OnEnable()
    {
        AvatarChecker();
        ButtonChecker();
        OnSelectedAvatarChange += ChangeChecker;
    }

    private void OnDisable()
    {
        OnSelectedAvatarChange -= ChangeChecker;
    }

    private void ChangeChecker(object sender, EventArgs e)
    {
        ButtonChecker();
    }

    private void ButtonChecker()
    {
        if (userData.AvatarID == selectedAvatar) saveBtn.interactable = false;
        else saveBtn.interactable = true;
    }

    public void AvatarChecker()
    {
        avatarOne.SetActive(userData.AvatarID == "AVATAR1");
        avatarTwo.SetActive(userData.AvatarID == "AVATAR2");
        avatarThree.SetActive(userData.AvatarID == "AVATAR3");
        avatarFour.SetActive(userData.AvatarID == "AVATAR4");
        avatarFive.SetActive(userData.AvatarID == "AVATAR5");
        avatarSix.SetActive(userData.AvatarID == "AVATAR6");
    }

    public void CloseChanger()
    {
        if (userData.AvatarID != selectedAvatar && selectedAvatar != "")
        {
            GameManager.Instance.NotificationController.ShowConfirmation("You have an usaved avatar settings! Do you want to cancel changing your avatar?", () =>
            {
                avatarChangerObj.SetActive(false);
            }, null);
        }
        else
        {
            selectedAvatar = "";
            avatarChangerObj.SetActive(false);
        }
    }

    public void ChangeAvatar()
    {
        GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to change your current avatar?", () =>
        {
            GameManager.Instance.NoBGLoading.SetActive(true);

            StartCoroutine(GameManager.Instance.PostRequest("/avatar/saveavatar", "", new Dictionary<string, object>
            {
                { "avatarid", selectedAvatar }
            }, false, (response) =>
            {
                try
                {
                    userData.AvatarID = selectedAvatar;
                    SelectedAvatar = "";
                    AvatarChecker();
                }
                catch (Exception ex)
                {
                    GameManager.Instance.SceneController.StopLoading();
                    Debug.Log(ex.ToString());
                    GameManager.Instance.SocketMngr.Socket.Disconnect();
                    GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later. 3", null);
                    GameManager.Instance.SceneController.CurrentScene = "Login";
                }
            }, () =>
            {
                GameManager.Instance.SceneController.StopLoading();
                GameManager.Instance.SocketMngr.Socket.Disconnect();
                GameManager.Instance.NotificationController.ShowError("There's a problem with your network connection! Please try again later. 4", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }));
        }, null);
    }
}
