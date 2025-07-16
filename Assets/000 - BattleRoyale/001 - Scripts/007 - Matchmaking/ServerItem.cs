using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerItem : MonoBehaviour
{
    [SerializeField] private UserData userData;

    [Space]
    [SerializeField] private bool isLoggin;
    [SerializeField] private LoginManager loginManager;
    [SerializeField] private LobbyController lobbyController;

    [Space]
    [SerializeField] private string serverCode;
    [SerializeField] private TextMeshProUGUI selectedServerTMP;
    [SerializeField] private Sprite goodPing;
    [SerializeField] private Sprite mediumPing;
    [SerializeField] private Sprite badPing;
    [SerializeField] private Image pingImg;
    [SerializeField] private Button changeServer;
    [SerializeField] private Toggle rememberMe;
    [SerializeField] private TextMeshProUGUI userCount;

    private void OnEnable()
    {
        LoginServers();
        LobbyServers();

        GameManager.Instance.SocketMngr.OnPlayerCountAsiaServerChange += AsiaChange;
        GameManager.Instance.SocketMngr.OnPlayerCountAfricaServerChange += AfricaChange;
        GameManager.Instance.SocketMngr.OnPlayerCounUAEtServerChange += UAEChange;
        GameManager.Instance.SocketMngr.OnPlayerCountAmericaEastServerChange += USChange;
        GameManager.Instance.SocketMngr.OnPlayerCountAmericaWestServerChange += USWChange;

        ChangeServerCount();
    }


    private void OnDisable()
    {
        GameManager.Instance.SocketMngr.OnPlayerCountAsiaServerChange -= AsiaChange;
        GameManager.Instance.SocketMngr.OnPlayerCountAfricaServerChange -= AfricaChange;
        GameManager.Instance.SocketMngr.OnPlayerCounUAEtServerChange -= UAEChange;
        GameManager.Instance.SocketMngr.OnPlayerCountAmericaEastServerChange -= USChange;
        GameManager.Instance.SocketMngr.OnPlayerCountAmericaWestServerChange -= USWChange;
    }

    void Update()
    {
        Debug.Log($"[MAIN THREAD] Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
    }

    private void USWChange(object sender, EventArgs e)
    {
        ChangeServerCount();
    }

    private void USChange(object sender, EventArgs e)
    {
        ChangeServerCount();
    }

    private void UAEChange(object sender, EventArgs e)
    {
        ChangeServerCount();
    }

    private void AfricaChange(object sender, EventArgs e)
    {
        ChangeServerCount();
    }

    private void AsiaChange(object sender, EventArgs e)
    {
        ChangeServerCount();
    }

    private void ChangeServerCount()
    {
        string display = "0";
        switch (serverCode)
        {
            case "asia": display = GameManager.Instance.SocketMngr.PlayerAsiaCountServer.ToString("n0"); break;
            case "za": display = GameManager.Instance.SocketMngr.PlayerAfricaCountServer.ToString("n0"); break;
            case "uae": display = GameManager.Instance.SocketMngr.PlayerUAECountServer.ToString("n0"); break;
            case "us": display = GameManager.Instance.SocketMngr.PlayerAmericaEastCountServer.ToString("n0"); break;
            case "usw": display = GameManager.Instance.SocketMngr.PlayerAmericaWestCountServer.ToString("n0"); break;
        }

        if (userCount != null)
        {
            userCount.text = display;
            userCount.color = Color.white; // just in case
        }
        else
        {
            Debug.LogWarning("userCount TMP is null!");
        }
    }

    private void LoginServers()
    {
        if (!isLoggin) return;

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

    private void LobbyServers()
    {
        if (isLoggin) return;

        changeServer.interactable = lobbyController.AvailableServers.ContainsKey(serverCode);

        if (!lobbyController.AvailableServers.ContainsKey(serverCode))
        {
            selectedServerTMP.text = $"{GameManager.GetRegionName(serverCode)} <size=25><color=red>Not Available</color></size>";
            changeServer.interactable = false;
            pingImg.sprite = badPing;
        }
        else
        {
            selectedServerTMP.text = $"<color=white>{GameManager.GetRegionName(serverCode)}</color> <size=25>{(lobbyController.AvailableServers[serverCode] < 100 ? $"<color=#00BA0D>{lobbyController.AvailableServers[serverCode]}ms</color>" : lobbyController.AvailableServers[serverCode] > 100 && lobbyController.AvailableServers[serverCode] < 250 ? $"<color=#D26E05>{lobbyController.AvailableServers[serverCode]}ms</color>" : $"<color=red>{lobbyController.AvailableServers[serverCode]}ms</color>")}</size>";
            pingImg.sprite = lobbyController.AvailableServers[serverCode] < 100 ? goodPing : lobbyController.AvailableServers[serverCode] > 100 && lobbyController.AvailableServers[serverCode] < 250 ? mediumPing : badPing;
            changeServer.interactable = true;
        }
    }

    public void ChangeServer()
    {
        if (!isLoggin)
        {
            GameManager.Instance.SocketMngr.EmitEvent("changeregion", JsonConvert.SerializeObject(new Dictionary<string, string>()
            {
                { "oldregion", userData.SelectedServer },
                { "newregion", serverCode }
            }));
        }

        userData.SelectedServer = serverCode;

        if (isLoggin )
            loginManager.CheckSelectedServer();

        userData.ChangeServerEventTrigger();

        if (rememberMe == null) return;

        if (rememberMe.isOn)
            PlayerPrefs.SetString("server", serverCode);

    }
}
