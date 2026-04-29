using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIOClient;
using CandyCoded.env;
using System;
using SocketIOClient.Newtonsoft.Json;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

public class SocketManager : MonoBehaviour
{
    public enum LoginState
    {
        NONE,
        FAILED,
        SUCCESS
    }

    private event EventHandler PlayerCountServerChange;
    public event EventHandler OnPlayerCountServerChange
    {
        add
        {
            if (PlayerCountServerChange == null || !PlayerCountServerChange.GetInvocationList().Contains(value))
                PlayerCountServerChange += value;
        }
        remove { PlayerCountServerChange -= value; }
    }
    public int PlayerCountServer
    {
        get => playerCountServer;
        set
        {
            playerCountServer = value;
            PlayerCountServerChange?.Invoke(this, EventArgs.Empty);
        }
    }

    private event EventHandler PlayerCountAsiaServerChange;
    public event EventHandler OnPlayerCountAsiaServerChange
    {
        add
        {
            if (PlayerCountAsiaServerChange == null || !PlayerCountAsiaServerChange.GetInvocationList().Contains(value))
                PlayerCountAsiaServerChange += value;
        }
        remove { PlayerCountAsiaServerChange -= value; }
    }
    public int PlayerAsiaCountServer
    {
        get => playerAsiaCountServer;
        set
        {
            playerAsiaCountServer = value;
            PlayerCountAsiaServerChange?.Invoke(this, EventArgs.Empty);
        }
    }

    private event EventHandler PlayerCountUAEServerChange;
    public event EventHandler OnPlayerCounUAEtServerChange
    {
        add
        {
            if (PlayerCountUAEServerChange == null || !PlayerCountUAEServerChange.GetInvocationList().Contains(value))
                PlayerCountUAEServerChange += value;
        }
        remove { PlayerCountUAEServerChange -= value; }
    }
    public int PlayerUAECountServer
    {
        get => playerUAECountServer;
        set
        {
            playerUAECountServer = value;
            PlayerCountUAEServerChange?.Invoke(this, EventArgs.Empty);
        }
    }

    private event EventHandler PlayerCountAfricaServerChange;
    public event EventHandler OnPlayerCountAfricaServerChange
    {
        add
        {
            if (PlayerCountAfricaServerChange == null || !PlayerCountAfricaServerChange.GetInvocationList().Contains(value))
                PlayerCountAfricaServerChange += value;
        }
        remove { PlayerCountAfricaServerChange -= value; }
    }
    public int PlayerAfricaCountServer
    {
        get => playerAfricaCountServer;
        set
        {
            playerAfricaCountServer = value;
            PlayerCountAfricaServerChange?.Invoke(this, EventArgs.Empty);
        }
    }

    private event EventHandler PlayerCountAmericaEastServerChange;
    public event EventHandler OnPlayerCountAmericaEastServerChange
    {
        add
        {
            if (PlayerCountAfricaServerChange == null || !PlayerCountAfricaServerChange.GetInvocationList().Contains(value))
                PlayerCountAfricaServerChange += value;
        }
        remove { PlayerCountAfricaServerChange -= value; }
    }
    public int PlayerAmericaEastCountServer
    {
        get => playerAmericaEastCountServer;
        set
        {
            playerAmericaEastCountServer = value;
            PlayerCountAmericaEastServerChange?.Invoke(this, EventArgs.Empty);
        }
    }

    private event EventHandler PlayerCountAmericaWestServerChange;
    public event EventHandler OnPlayerCountAmericaWestServerChange
    {
        add
        {
            if (PlayerCountAmericaWestServerChange == null || !PlayerCountAmericaWestServerChange.GetInvocationList().Contains(value))
                PlayerCountAmericaWestServerChange += value;
        }
        remove { PlayerCountAmericaWestServerChange -= value; }
    }
    public int PlayerAmericaWestCountServer
    {
        get => playerAmericaWestCountServer;
        set
        {
            playerAmericaWestCountServer = value;
            PlayerCountAmericaWestServerChange?.Invoke(this, EventArgs.Empty);
        }
    }

    // ============================

    [SerializeField] private UserData userData;
    [SerializeField] private GameObject reconObj;

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
    [field: SerializeField] public bool isOnLogin;
    [SerializeField] private int playerCountServer;
    [SerializeField] private int playerAsiaCountServer;
    [SerializeField] private int playerUAECountServer;
    [SerializeField] private int playerAfricaCountServer;
    [SerializeField] private int playerAmericaEastCountServer;
    [SerializeField] private int playerAmericaWestCountServer;
    [SerializeField] private int retryReconnect;

    //  ===========================

    public SocketIOUnity Socket {get; private set; }

    private CancellationTokenSource pingTimeoutCts;

    private const int MaxMissedPings = 3;

    public Action DisconnectAction;

    private bool _handlersRegistered = false;
    private bool _isReconnecting = false;
    private bool _intentionalLogout = false;

    private const int MaxReconnectAttempts = 5;

    //  ===========================

    private void OnApplicationPause(bool paused)
    {
        if (!paused && ConnectionStatus == "Disconnected")
            Reconnect();
    }

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
                    EIO = EngineIO.V4,
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

        GameManager.Instance.AddJob(() =>
        {
            retryReconnect = 0;
            _isReconnecting = false;
            GameManager.Instance.NoBGLoading.SetActive(false);
        });

        // Register handlers only once — re-registering on reconnect accumulates duplicates
        if (!_handlersRegistered)
        {
            _handlersRegistered = true;

            Socket.On("ping", async (response) =>
            {
                await Task.Delay(5000);
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                EmitEvent("pong", timestamp);
                missedPingCount = 0;
                RestartPingTimeout();
            });

            Socket.On("playercount", (response) =>
            {
                GameManager.Instance.AddJob(() =>
                {
                    Debug.Log("player count change");
                    PlayerCountServer = response.GetValue<int>();
                });
            });

            Socket.On("asiacount", (response) =>
            {
                GameManager.Instance.AddJob(() =>
                {
                    Debug.Log("Change asia user count");
                    PlayerAsiaCountServer = response.GetValue<int>();
                });
            });

            Socket.On("uaecount", (response) =>
            {
                GameManager.Instance.AddJob(() =>
                {
                    Debug.Log("Change asia uae count");
                    PlayerUAECountServer = response.GetValue<int>();
                });
            });

            Socket.On("americawestcount", (response) =>
            {
                GameManager.Instance.AddJob(() =>
                {
                    PlayerAmericaWestCountServer = response.GetValue<int>();
                });
            });

            Socket.On("americacount", (response) =>
            {
                GameManager.Instance.AddJob(() =>
                {
                    PlayerAmericaEastCountServer = response.GetValue<int>();
                });
            });

            Socket.On("africacount", (response) =>
            {
                GameManager.Instance.AddJob(() =>
                {
                    PlayerAfricaCountServer = response.GetValue<int>();
                });
            });

            Socket.On("selectedservercount", (response) =>
            {
                GameManager.Instance.AddJob(() =>
                {
                    if (response.GetValue<string>() != "")
                    {
                        Dictionary<string, int> tempservercount = JsonConvert.DeserializeObject<Dictionary<string, int>>(response.GetValue<string>());
                        PlayerAsiaCountServer = tempservercount["asia"];
                        PlayerAfricaCountServer = tempservercount["za"];
                        PlayerUAECountServer = tempservercount["uae"];
                        PlayerAmericaEastCountServer = tempservercount["us"];
                    }
                });
            });
        }

        // Always re-emit login on connect/reconnect to re-authenticate with backend
        EmitEvent("login", JsonConvert.SerializeObject(new Dictionary<string, string>
        {
            { "userid", userData.Username },
            { "region", userData.SelectedServer }
        }));
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
        Debug.Log("Socket Disconnected from server");
        CancelPingTimeout();
        ConnectionStatus = "Disconnected";

        Debug.Log($"INTENTIONAL LOGOUT? {_intentionalLogout}");
        if (_intentionalLogout)
        {
            _intentionalLogout = false;
            return;
        }

        GameManager.Instance.AddJob(() =>
        {
            GameManager.Instance.NoBGLoading.SetActive(true);
        });

        StartAutoReconnect();
    }

    public void LogoutAndDisconnect()
    {
        _intentionalLogout = true;
        if (Socket != null)
        {
            Socket.Disconnect();
            Socket = null;
        }
        ConnectionStatus = "Disconnected";
    }

    private void StartAutoReconnect()
    {
        if (_isReconnecting) return;
        _isReconnecting = true;
        retryReconnect = 0;
        GameManager.Instance.AddJob(AttemptReconnect);
    }

    private async void AttemptReconnect()
    {
        if (ConnectionStatus == "Connected")
        {
            _isReconnecting = false;
            return;
        }

        if (retryReconnect >= MaxReconnectAttempts)
        {
            OnReconnectFailed();
            return;
        }

        retryReconnect++;
        int delayMs = Mathf.Min(2000 * retryReconnect, 16000);
        Debug.LogWarning($"[Socket] Reconnect attempt {retryReconnect}/{MaxReconnectAttempts} in {delayMs}ms...");

        await Task.Delay(delayMs);

        if (ConnectionStatus == "Connected")
        {
            _isReconnecting = false;
            return;
        }

        try
        {
            if (Socket != null)
                Socket.Connect();
            else
                GameManager.Instance.AddJob(InitializeSocket);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Socket] Reconnect attempt {retryReconnect} failed: {ex.Message}");
            AttemptReconnect();
        }
    }

    private void OnReconnectFailed()
    {
        Debug.LogError("[Socket] Max reconnect attempts reached. Giving up.");
        _isReconnecting = false;

        GameManager.Instance.AddJob(() =>
        {
            GameManager.Instance.NoBGLoading.SetActive(false);
            ConnectionStatus = "Disconnected";
            DisconnectAction?.Invoke();
            if (!isOnLogin)
                sceneController.CurrentScene = "Login";
            userData.ResetLogin();
            Socket = null;
        });
    }

    private void Reconnect()
    {
        if (ConnectionStatus == "Connected") return;

        GameManager.Instance.AddJob(() =>
        {
            GameManager.Instance.NoBGLoading.SetActive(true);
        });

        StartAutoReconnect();
    }

    public void EmitEvent(string eventname, object data)
    {
        if (Socket == null) return;
        Socket.Emit(eventname, data);
    }
}
