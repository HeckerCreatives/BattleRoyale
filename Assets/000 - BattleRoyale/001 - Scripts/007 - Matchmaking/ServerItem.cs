using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerItem : MonoBehaviour
{
    [SerializeField] private UserData userData; 
    [SerializeField] private LoginManager loginManager;
    [SerializeField] private string serverCode;
    [SerializeField] private TextMeshProUGUI selectedServerTMP;
    [SerializeField] private Sprite goodPing;
    [SerializeField] private Sprite mediumPing;
    [SerializeField] private Sprite badPing;
    [SerializeField] private Image pingImg;
    [SerializeField] private Button changeServer;
    [SerializeField] private Toggle rememberMe;

    private void OnEnable()
    {
        changeServer.interactable = loginManager.AvailableServers.ContainsKey(serverCode);

        if (!loginManager.AvailableServers.ContainsKey(serverCode))
        {
            selectedServerTMP.text = $"{GameManager.GetRegionName(serverCode)} <size=25><color=red>Not Available</color></size>";
            changeServer.interactable = false;
            pingImg.sprite = badPing;
        }
        else
        {
            selectedServerTMP.text = $"<color=white>{GameManager.GetRegionName(serverCode)}</color> <size=25>{(loginManager.AvailableServers[serverCode] < 100 ? $"<color=#00BA0D>{loginManager.AvailableServers[serverCode]}ms</color>" : loginManager.AvailableServers[serverCode] > 100 && loginManager.AvailableServers[serverCode] < 250 ? $"<color=#D26E05>{loginManager.AvailableServers[serverCode]}ms</color>" : $"<color=red>{loginManager.AvailableServers[serverCode]}ms</color>")}</size>";
            pingImg.sprite = loginManager.AvailableServers[serverCode] < 100 ? goodPing : loginManager.AvailableServers[serverCode] > 100 && loginManager.AvailableServers[serverCode] < 250 ? mediumPing : badPing;
            changeServer.interactable = true;
        }
    }

    public void ChangeServer()
    {
        userData.SelectedServer = serverCode;

        loginManager.CheckSelectedServer();

        if (rememberMe.isOn)
            PlayerPrefs.SetString("server", serverCode);
    }
}
