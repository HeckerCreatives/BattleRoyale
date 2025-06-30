using Fusion;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerOwnObjectEnabler : NetworkBehaviour
{
    [SerializeField] private UserData userData;

    [Space]
    [SerializeField] private PlayerPlayables playerPlayables;

    [Space]
    [SerializeField] private GameObject canvasPlayer;
    [SerializeField] private GameObject playerVcam;
    [SerializeField] private GameObject playerAimVCam;
    [SerializeField] private GameObject playerMiniMapIcon;
    [SerializeField] private GameObject playerMapIcon;
    [SerializeField] private GameObject playerMinimapCam;
    [SerializeField] private GameObject playerSpawnLocCam;

    public async override void Spawned()
    {
        while (!Runner) await Task.Yield();

        if (!HasInputAuthority) return;

        GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.PostRequest("/usergamedetail/useenergy", "", null, false, (response) =>
        {
            userData.GameDetails.energy -= userData.GameDetails.energy > 0 ? 1 : 0;
        }, () =>
        {
            Runner.Shutdown(true);
            GameManager.Instance.SceneController.StopLoading();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
            GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later.", null);
            GameManager.Instance.SceneController.CurrentScene = "Login";
        }));
        GameManager.Instance.SceneController.AddActionLoadinList(InitializePlayer());
        GameManager.Instance.SceneController.ActionPass = true;

        canvasPlayer.SetActive(true);
        playerVcam.SetActive(true);
        playerAimVCam.SetActive(true);
        playerMiniMapIcon.SetActive(true);
        playerMapIcon.SetActive(true);
        playerMinimapCam.SetActive(true);
        playerSpawnLocCam.SetActive(true);
    }

    IEnumerator InitializePlayer()
    {
        playerPlayables.InitializePlayables();
        yield return null;
    }
}
