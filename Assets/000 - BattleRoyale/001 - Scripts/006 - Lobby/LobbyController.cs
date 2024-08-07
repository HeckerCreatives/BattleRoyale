using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private CharacterCreationController characterCreationController;
    [SerializeField] private GameSettingController gameSettingController;
    [SerializeField] private ControllerSetting controllerSetting;
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
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later. 1", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }
        }, () =>
        {
            GameManager.Instance.SceneController.StopLoading();
            GameManager.Instance.NotificationController.ShowError("There's a problem with your network connection! Please try again later. 2", null);
            GameManager.Instance.SceneController.CurrentScene = "Login";
        }));
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetVolumeSlidersOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetGraphicsOnStart());
        GameManager.Instance.AudioController.SetBGMusic(bgMusicClip);
        GameManager.Instance.SceneController.ActionPass = true;
    }

    public void ChangeScene()
    {
        GameManager.Instance.SceneController.CurrentScene = "Prototype";
    }
}
