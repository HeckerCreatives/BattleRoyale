using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private ClientMatchmakingController matchmakingController;
    [SerializeField] private CharacterCreationController characterCreationController;
    [SerializeField] private GameSettingController gameSettingController;
    [SerializeField] private ControllerSetting controllerSetting;
    [SerializeField] private LobbyUserProfile userProfile;
    [SerializeField] private List<LeaderboardItem> leaderboardItems;
    [SerializeField] private AudioClip bgMusicClip;

    private void Awake()
    {
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/characters/getcharactersetting", "", false, (response) =>
        {
            try
            {
                PlayerCharacterSetting charactersetting = JsonConvert.DeserializeObject<PlayerCharacterSetting>(response.ToString());

                Debug.Log(response.ToString());
                userData.CharacterSetting = charactersetting;
                characterCreationController.InitializeCharacterSettings(userData.CharacterSetting.hairstyle, userData.CharacterSetting.haircolor, userData.CharacterSetting.clothingcolor, userData.CharacterSetting.skincolor);
            }
            catch (Exception ex)
            {
                GameManager.Instance.SceneController.StopLoading();
                Debug.Log(ex.ToString());
                GameManager.Instance.SocketMngr.Socket.Disconnect();
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later. 1", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }
        }, () =>
        {
            GameManager.Instance.SceneController.StopLoading();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
            GameManager.Instance.NotificationController.ShowError("There's a problem with your network connection! Please try again later. 2", null);
            GameManager.Instance.SceneController.CurrentScene = "Login";
        }));
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/usergamedetail/getusergamedetails", "", false, (response) =>
        {
            try
            {
                GameUserDetails gameUserDetails = JsonConvert.DeserializeObject<GameUserDetails>(response.ToString());

                userData.GameDetails = gameUserDetails;
                userProfile.SetData();
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
        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.GetRequest("/leaderboard/getleaderboard", "", false, (response) =>
        {
            try
            {
                Dictionary<string, object> responsetempdata = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.ToString());

                if (responsetempdata.Count <= 0) return;

                Dictionary<string, LeaderboardData> tempdata = JsonConvert.DeserializeObject<Dictionary<string, LeaderboardData>>(responsetempdata["leaderboard"].ToString());

                for (int a = 0; a < leaderboardItems.Count; a++)
                {
                    if (a < tempdata.Count)
                    {
                        leaderboardItems[a].SetData(tempdata[a.ToString()].user, tempdata[a.ToString()].amount.ToString("n0"));
                    }
                    else
                    {
                        leaderboardItems[a].SetData("", "");
                    }
                }
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
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetVolumeSlidersOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetGraphicsOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetLookSensitivityOnStart());
        GameManager.Instance.AudioController.SetBGMusic(bgMusicClip);
        GameManager.Instance.SceneController.ActionPass = true;
    }

    public void ChangeScene()
    {
        GameManager.Instance.SceneController.CurrentScene = "Prototype";
    }

    public void Logout()
    {
        GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to logout?", async () => 
        {
            GameManager.Instance.NoBGLoading.SetActive(true);
            if (matchmakingController.currentRunnerInstance != null)
                await matchmakingController.ShutdownServer();

            userData.ResetLogin();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
        }, null);
    }
}

[System.Serializable]
public class LeaderboardData
{
    public string user;
    public int amount;
}