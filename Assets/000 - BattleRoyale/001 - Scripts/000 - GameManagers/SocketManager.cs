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
using System.Threading;

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
    [field: SerializeField] public int missedPingCount;

    //  ===========================

    public SocketIOUnity Socket {get; private set; }

    private CancellationTokenSource pingTimeoutCts;

    private const int MaxMissedPings = 3;

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

        // Handle "ping" event
        Socket.On("ping", async (response) =>
        {
            await Task.Delay(5000);
            // Respond with "pong" after 5 seconds
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            EmitEvent("pong", timestamp);
            missedPingCount = 0;
            // Start or reset the timeout timer
            RestartPingTimeout();
        });

    EmitEvent("login", userData.Username);
    }

    private void RestartPingTimeout()
    {
        CancelPingTimeout(); // Cancel existing timeout

        pingTimeoutCts = new CancellationTokenSource();
        var token = pingTimeoutCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(10000, token);
                if (!token.IsCancellationRequested)
                {
                    missedPingCount++;
                    Debug.LogWarning($"Missed ping! Count: {missedPingCount}");

                    if (missedPingCount >= MaxMissedPings)
                    {
                        Debug.LogError("Too many missed pings. Disconnecting...");
                        GameManager.Instance.SocketMngr.Socket.Disconnect();
                    }
                    else
                    {
                        RestartPingTimeout(); // Keep waiting for the next ping
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when a new ping arrives in time
            }
        }, token);
    }

    // Cancels the current timeout timer
    private void CancelPingTimeout()
    {
        if (pingTimeoutCts != null)
        {
            pingTimeoutCts.Cancel();
            pingTimeoutCts.Dispose();
            pingTimeoutCts = null;
        }
    }

    private void SocketDisconnected(object sender, string e)
    {
        Debug.Log("Socket Disconnected to server");
        CancelPingTimeout();

        GameManager.Instance.AddJob(() =>
        {
            EmitEvent("disconnect", null);
            GameManager.Instance.NoBGLoading.SetActive(false);
            ConnectionStatus = "Disconnected";
            sceneController.CurrentScene = "Login";
            Socket = null;
        });
    }

    public void EmitEvent(string eventname, object data)
    {
        Socket.Emit(eventname, data);
    }
}
