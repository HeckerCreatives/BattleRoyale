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
using Newtonsoft.Json.Linq;
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
                // Fresh socket instance — its event handlers are registered in
                // SocketConnected, gated by _handlersRegistered. That flag is
                // never reset elsewhere, so after a logout (Socket = null) the
                // new instance would skip registration and never receive
                // playercount/region counts. Reset it here only; plain
                // reconnects reuse the existing Socket and skip InitializeSocket,
                // so they still avoid duplicate handlers.
                _handlersRegistered = false;

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

        // Captured before the AddJob below resets these. A reconnect gives us a
        // new socket id, so the region server is still relaying room events to
        // the dead one — we must re-run the reconnect handshake to re-register.
        bool wasReconnect = _isReconnecting || retryReconnect > 0;

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

            Socket.On("ping", (response) =>
            {
                // Respond immediately. A delay here eats the server's pong
                // timeout budget and causes false force-disconnects on spikes.
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                EmitEvent("pong", timestamp);
                missedPingCount = 0;
                RestartPingTimeout();
            });

            Socket.On("playercount", (response) =>
            {
                int c = ReadCountAndAck(response, "playercount");
                GameManager.Instance.AddJob(() =>
                {
                    Debug.Log("player count change");
                    PlayerCountServer = c;
                });
            });

            Socket.On("asiacount", (response) =>
            {
                int c = ReadCountAndAck(response, "asiacount");
                GameManager.Instance.AddJob(() =>
                {
                    Debug.Log("Change asia user count");
                    PlayerAsiaCountServer = c;
                });
            });

            Socket.On("uaecount", (response) =>
            {
                int c = ReadCountAndAck(response, "uaecount");
                GameManager.Instance.AddJob(() =>
                {
                    Debug.Log("Change asia uae count");
                    PlayerUAECountServer = c;
                });
            });

            Socket.On("americacount", (response) =>
            {
                int c = ReadCountAndAck(response, "americacount");
                GameManager.Instance.AddJob(() =>
                {
                    PlayerAmericaEastCountServer = c;
                });
            });

            Socket.On("africacount", (response) =>
            {
                int c = ReadCountAndAck(response, "africacount");
                GameManager.Instance.AddJob(() =>
                {
                    PlayerAfricaCountServer = c;
                });
            });

            Socket.On("selectedservercount", (response) =>
            {
                // Now a reliable object payload: { asia, za, uae, us, messageId }
                JObject obj = response.GetValue<JObject>();
                string messageId = obj.Value<string>("messageId");
                if (!string.IsNullOrEmpty(messageId))
                    EmitEvent("ack", new { eventName = "selectedservercount", messageId });

                int total = obj.Value<int>("total");
                int asia = obj.Value<int>("asia");
                int za   = obj.Value<int>("za");
                int uae  = obj.Value<int>("uae");
                int us   = obj.Value<int>("us");

                GameManager.Instance.AddJob(() =>
                {
                    // Authoritative snapshot — covers the global total too so
                    // the server-selection screen never depends on a
                    // best-effort playercount delta.
                    PlayerCountServer = total;
                    PlayerAsiaCountServer = asia;
                    PlayerAfricaCountServer = za;
                    PlayerUAECountServer = uae;
                    PlayerAmericaEastCountServer = us;
                });
            });
        }

        // Always re-emit login on connect/reconnect to re-authenticate with backend
        EmitEvent("login", JsonConvert.SerializeObject(new Dictionary<string, string>
        {
            { "userid", userData.Username },
            { "region", userData.SelectedServer }
        }));

        // On reconnect, re-run the reconnect handshake. The region server replies
        // reconnectexist (re-registers our new socket id + restores the waiting
        // room) or reconnectfail (harmless if we weren't in a room). Login alone
        // does not re-sync room membership, so lobby events would otherwise stop.
        if (wasReconnect)
            EmitEvent("needtoreconnect", null);
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

    // Count deltas are best-effort: an object { count } with no messageId, so
    // no ACK is sent (nothing to retry — authoritative correctness comes from
    // the reliable selectedservercount snapshot on login/selectregion). The
    // messageId branch is kept defensively: if a count is ever sent reliably
    // again it still gets ACKed. ACK (when present) goes off the main thread;
    // only the value assignment is marshalled back via AddJob by callers.
    private int ReadCountAndAck(SocketIOResponse response, string eventName)
    {
        JObject obj = response.GetValue<JObject>();
        string messageId = obj.Value<string>("messageId");
        if (!string.IsNullOrEmpty(messageId))
            EmitEvent("ack", new { eventName, messageId });
        return obj.Value<int>("count");
    }

    public void EmitEvent(string eventname, object data)
    {
        if (Socket == null) return;
        Socket.Emit(eventname, data);
    }
}
