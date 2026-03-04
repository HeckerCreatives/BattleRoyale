using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class SocketServerController : MonoBehaviour
{
    public static SocketServerController Instance { get; private set; }

    //  ======================

    public SocketIOUnity Socket { get; private set; }

    private CancellationTokenSource pingTimeoutCts;

    private const int MaxMissedPings = 3;

    Queue<Action> jobs = new Queue<Action>();

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (jobs.Count > 0)
            jobs.Dequeue().Invoke();
    }

    public void InitializeSocket()
    {
        Debug.Log("starting initialize socket");
        var uri = new Uri("http://localhost:5009/");

        Debug.Log($"Initializing URI.... http://localhost:5009/");

        try
        {

            Socket = new SocketIOUnity(uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
            {
                { "token", "UNITY" }
            },
                EIO = EngineIO.V4,
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
            });

            Socket.JsonSerializer = new NewtonsoftJsonSerializer();

            Socket.OnConnected += SocketConnected;

            Debug.Log("Socket connecting to server ....");

            Socket.Connect();
        }
        catch (Exception ex)
        {
            Debug.Log($"Error: {ex}");
        }
    }

    private void SocketConnected(object sender, EventArgs e)
    {
        MultiplayerServerManager.Instance.ChangeServerStatus();

        Socket.On("gameremoveplayer", (response) =>
        {
            jobs.Enqueue(() =>
            {
                Debug.Log($"game remove player response: {response}");

                string username = response.GetValue<string>();

                Debug.Log($"STARTING TO REMOVE PLAYER {username}");

                PlayerJoinedController.Instance.RemovePlayerByUsername(username);
            });
        });
    }
}
