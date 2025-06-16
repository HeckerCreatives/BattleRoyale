using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerOwnObjectEnabler : NetworkBehaviour
{
    [SerializeField] private GameObject canvasPlayer;
    [SerializeField] private GameObject playerVcam;
    [SerializeField] private GameObject playerAimVCam;
    [SerializeField] private GameObject playerMiniMapIcon;
    [SerializeField] private GameObject playerMapIcon;
    [SerializeField] private GameObject playerMinimapCam;
    [SerializeField] private GameObject playerSpawnLocCam;

    private async void OnEnable()
    {
        while (!Runner)
        {
            Debug.Log($"waiting for runner");
            await Task.Yield();
        }


        Debug.Log($"player has input authority? {HasInputAuthority}");

        if (!HasInputAuthority) return;

        canvasPlayer.SetActive(true);
        playerVcam.SetActive(true);
        playerAimVCam.SetActive(true);
        playerMiniMapIcon.SetActive(true);
        playerMapIcon.SetActive(true);
        playerMinimapCam.SetActive(true);
        playerSpawnLocCam.SetActive(true);

        Debug.Log(GameManager.Instance.SceneController.ActionPass);
        GameManager.Instance.SceneController.ActionPass = true;
        Debug.Log(GameManager.Instance.SceneController.ActionPass);
    }
}
