using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIOClient;
using UnityEngine.Networking;
using CandyCoded.env;
using System;
using SocketIOClient.Newtonsoft.Json;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class SocketManager : MonoBehaviour
{
    public enum LoginState
    {
        NONE,
        FAILED,
        SUCCESS
    }

    [SerializeField] private UserData userData;

    [Space]
    [SerializeField] private NotificationController notificationController;
    [SerializeField] private SceneController sceneController;

    [Space]
    [SerializeField] public GameObject errorPanelObj;
    [SerializeField] private Text errorTMP;

    [field: Header("DEBUGGER")]
    [field: SerializeField] public string ConnectionStatus { get; private set; }
    [field: SerializeField] public LoginState LoginStatus { get; private set; }

    //  ===========================

    public SocketIOUnity Socket {get; private set; }

    //  ===========================

    public void InitializeSocket()
    {
        Debug.Log("starting initialize socket");
        if (env.TryParseEnvironmentVariable("SOCKET_URL", out string httpRequest))
        {
            var uri = new Uri(httpRequest);

            Debug.Log($"Initializing URI.... {httpRequest}");

            try
            {

                Socket = new SocketIOUnity(uri, new SocketIOOptions
                {
                    Query = new Dictionary<string, string>
            {
                { "token", "UNITY" }
            },
                    EIO = 4,
                    Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
                });

                Socket.JsonSerializer = new NewtonsoftJsonSerializer();

                Socket.OnConnected += SocketConnected;
                Socket.OnDisconnected += SocketDisconnected;

                Debug.Log("Socket connecting to server ....");

                Socket.Connect();
            }
            catch (Exception ex)
            {
                Debug.Log($"Error: {ex}");
            }
        }
    }

    private void SocketConnected(object sender, EventArgs e)
    {
        Debug.Log("Socket Connected to server");
        ConnectionStatus = "Connected";

        Socket.On("ping", async (response) =>
        {
            await Task.Delay(5000);

            DateTime utcNow = DateTime.UtcNow;
            EmitEvent("pong", utcNow);
        });

        EmitEvent("login", userData.Username);
    }

    private void SocketDisconnected(object sender, string e)
    {
        Debug.Log("Socket Disconnected to server");
        GameManager.Instance.AddJob(() =>
        {
            GameManager.Instance.NoBGLoading.SetActive(false);
            ConnectionStatus = "Disconnected";
            sceneController.CurrentScene = "Login";
        });
    }

    public void EmitEvent(string eventname, object data)
    {
        Socket.Emit(eventname, data);
    }
}
