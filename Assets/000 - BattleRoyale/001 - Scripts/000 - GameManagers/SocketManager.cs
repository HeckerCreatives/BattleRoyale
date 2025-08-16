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
    [SerializeField] private int playerCountServer;
    [SerializeField] private int playerAsiaCountServer;
    [SerializeField] private int playerUAECountServer;
    [SerializeField] private int playerAfricaCountServer;
    [SerializeField] private int playerAmericaEastCountServer;
    [SerializeField] private int playerAmericaWestCountServer;

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

        Socket.On("playercount", (response) =>
        {
            GameManager.Instance.AddJob(() =>
            {
                PlayerCountServer = response.GetValue<int>();
            });
        });


        Socket.On("asiacount", (response) =>
        {
            GameManager.Instance.AddJob(() =>
            {
                PlayerAsiaCountServer = response.GetValue<int>();
            });
        });

        Socket.On("uaecount", (response) =>
        {
            GameManager.Instance.AddJob(() =>
            {
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

        Socket.On("americaeastcount", (response) =>
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
                    //PlayerAmericaWestCountServer = tempservercount["usw"];
                }

            });
        });


        //EmitEvent("login", JsonConvert.SerializeObject(new Dictionary<string, string>
        //{
        //    { "userid", userData.Username },
        //    { "region", userData.SelectedServer }
        //}));

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
