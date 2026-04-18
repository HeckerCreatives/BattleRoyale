using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerItem : MonoBehaviour
{
    [SerializeField] private UserData userData;

    [Space]
    [SerializeField] private bool isLoggin;
    [SerializeField] private LoginManager loginManager;
    [SerializeField] private string toPing;
    [SerializeField] private int toPingPort;

    [Space]
    [SerializeField] private string serverCode;
    [SerializeField] private TextMeshProUGUI selectedServerTMP;
    [SerializeField] private TextMeshProUGUI pingIndicatorTMP;
    [SerializeField] private Sprite goodPing;
    [SerializeField] private Sprite mediumPing;
    [SerializeField] private Sprite badPing;
    [SerializeField] private Image pingImg;
    [SerializeField] private Toggle changeServer;
    [SerializeField] private Toggle rememberMe;
    [SerializeField] private TextMeshProUGUI userCount;

    [Header("DEBUGGER")]
    [SerializeField] private bool isInitialized;

    //  ====================

    Coroutine pingCoroutine;

    //  ====================

    private void OnEnable()
    {
        LoginServers();

        switch (serverCode)
        {
            case "asia": GameManager.Instance.SocketMngr.OnPlayerCountAsiaServerChange += AsiaChange; break;
            case "tr": GameManager.Instance.SocketMngr.OnPlayerCountAfricaServerChange += AfricaChange; break;
            case "uae": GameManager.Instance.SocketMngr.OnPlayerCounUAEtServerChange += UAEChange; break;
            case "us": GameManager.Instance.SocketMngr.OnPlayerCountAmericaEastServerChange += USChange; break;
            case "usw": GameManager.Instance.SocketMngr.OnPlayerCountAmericaWestServerChange += USWChange; break;
        }

        ChangeServerCount();
    }


    private void OnDisable()
    {
        if (pingCoroutine != null) StopCoroutine(pingCoroutine);

        changeServer.onValueChanged.RemoveAllListeners();

        switch (serverCode)
        {
            case "asia": GameManager.Instance.SocketMngr.OnPlayerCountAsiaServerChange -= AsiaChange; break;
            case "tr": GameManager.Instance.SocketMngr.OnPlayerCountAfricaServerChange -= AfricaChange; break;
            case "uae": GameManager.Instance.SocketMngr.OnPlayerCounUAEtServerChange -= UAEChange; break;
            case "us": GameManager.Instance.SocketMngr.OnPlayerCountAmericaEastServerChange -= USChange; break;
            case "usw": GameManager.Instance.SocketMngr.OnPlayerCountAmericaWestServerChange -= USWChange; break;
        }
    }

    private void USWChange(object sender, EventArgs e)
    {
        ChangeServerCount();
    }

    private void USChange(object sender, EventArgs e)
    {
        string display = "0";
        display = GameManager.Instance.SocketMngr.PlayerAmericaEastCountServer.ToString("n0");
        userCount.text = display;
        userCount.color = Color.white; // just in case
    }

    private void UAEChange(object sender, EventArgs e)
    {
        string display = "0";
        display = GameManager.Instance.SocketMngr.PlayerUAECountServer.ToString("n0");
        userCount.text = display;
        userCount.color = Color.white; // just in case
    }

    private void AfricaChange(object sender, EventArgs e)
    {
        string display = "0";
        display = GameManager.Instance.SocketMngr.PlayerAfricaCountServer.ToString("n0");
        userCount.text = display;
        userCount.color = Color.white; // just in case
    }

    private void AsiaChange(object sender, EventArgs e)
    {
        string display = "0";
        display = GameManager.Instance.SocketMngr.PlayerAsiaCountServer.ToString("n0");
        userCount.text = display;
        userCount.color = Color.white; // just in case
    }

    private void ChangeServerCount()
    {
        string display = "0";
        switch (serverCode)
        {
            case "asia": display = GameManager.Instance.SocketMngr.PlayerAsiaCountServer.ToString("n0"); break;
            case "tr": display = GameManager.Instance.SocketMngr.PlayerAfricaCountServer.ToString("n0"); break;
            case "uae": display = GameManager.Instance.SocketMngr.PlayerUAECountServer.ToString("n0"); break;
            case "us": display = GameManager.Instance.SocketMngr.PlayerAmericaEastCountServer.ToString("n0"); break;
            case "usw": display = GameManager.Instance.SocketMngr.PlayerAmericaWestCountServer.ToString("n0"); break;
        }
        UnityEngine.Debug.Log($"DISPLAY {serverCode}  count {display}");
        if (userCount != null)
        {
            userCount.text = display;
            userCount.color = Color.white; // just in case
        }
        else
        {
            UnityEngine.Debug.LogWarning("userCount TMP is null!");
        }
    }

    private void LoginServers()
    {
        if (!userData.ServerList.Contains(serverCode))
        {
            selectedServerTMP.text = $"{GameManager.GetRegionName(serverCode)}";
            pingIndicatorTMP.text = "Unavailable";
            changeServer.interactable = false;
            changeServer.isOn = false;
            pingImg.sprite = badPing;
        }
        else
        {
            selectedServerTMP.text = $"<color=white>{GameManager.GetRegionName(serverCode)}</color>";

            changeServer.onValueChanged.AddListener(delegate
            {
                ToggleValueChanged(changeServer);
            });

            if (userData.SelectedServer == serverCode)
            {
                changeServer.isOn = true;
            }

            pingCoroutine = StartCoroutine(OneTimePing(toPing, toPingPort, tempping =>
            {
                pingIndicatorTMP.text = $"Ping: {(tempping < 999 ? tempping : "Unavailable")}";
                pingImg.sprite = tempping < 80 ? goodPing :
                 tempping < 150 ? mediumPing :
                 badPing;

                changeServer.interactable = tempping < 999;
                changeServer.interactable = true;
            }, () =>
            {
                pingIndicatorTMP.text = $"Ping: Unavailable";
                changeServer.isOn = false;
                changeServer.interactable = false;
                pingImg.sprite = badPing;
            }));
        }
    }

    void ToggleValueChanged(Toggle change)
    {
        if (!change.isOn)
        {
            userData.SelectedServer = "";

            if (isLoggin)
                loginManager.CheckSelectedServer();

            if (rememberMe == null) return;

            if (rememberMe.isOn)
                PlayerPrefs.SetString("server", serverCode);

            return;
        }

        ChangeServer();
    }

    public IEnumerator OneTimePing(string host, int port, System.Action<int> onResult, System.Action onFailed)
    {
        while (true)
        {
            using (UdpClient client = new UdpClient())
            {
                client.Client.ReceiveTimeout = 2000;

                byte[] data = new byte[] { 1 };
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

                Stopwatch sw = new Stopwatch();
                bool received = false;

                try
                {
                    sw.Start();
                    client.Send(data, data.Length, host, port);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"Error sending UDP ping to {host}:{port}. Error: {e}");
                    onFailed?.Invoke();
                    yield break;
                }

                float timeout = 2f;
                float timer = 0f;

                while (timer < timeout)
                {
                    if (client.Available > 0)
                    {
                        try
                        {
                            client.Receive(ref ep);
                            sw.Stop();
                            received = true;
                            onResult?.Invoke((int)sw.ElapsedMilliseconds);
                            break;
                        }
                        catch (Exception e)
                        {
                            UnityEngine.Debug.LogWarning($"Error receiving UDP ping reply from {host}:{port}. Error: {e}");
                            onFailed?.Invoke();
                            yield break;
                        }
                    }

                    timer += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (!received)
                {
                    sw.Stop();
                    onResult?.Invoke(999);
                }
            }

            yield return new WaitForSecondsRealtime(2f);
        }
    }

    public void ChangeServer()
    {
        GameManager.Instance.SocketMngr.EmitEvent("changeregion", JsonConvert.SerializeObject(new Dictionary<string, string>()
        {
            { "oldregion", userData.SelectedServer },
            { "newregion", serverCode }
        }));

        userData.SelectedServer = serverCode;

        if (isLoggin )
            loginManager.CheckSelectedServer();

        userData.ChangeServerEventTrigger();

        if (rememberMe == null) return;

        if (rememberMe.isOn)
            PlayerPrefs.SetString("server", serverCode);

    }
}
