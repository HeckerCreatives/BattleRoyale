using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerPhotonReconnect : MonoBehaviour, INetworkRunnerCallbacks
{
    public static PlayerPhotonReconnect Instance;

    //  =======================

    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private UserData userData;
    [SerializeField] private GameObject reconnectObject;

    [Header("DEBUGGER")]
    [SerializeField] private float lastOutOfFocusTime;

    public bool isPlayer;
    private NetworkRunner _runner;
    private string _sessionName;
    private GameMode _mode;

    private bool _reconnecting;
    public bool IsReconnecting => _reconnecting;

    private bool _isInBackground;

    // Tracks whether the runner was active last frame so Update can detect a drop.
    private bool _wasRunnerActive;

    private CancellationTokenSource _cts;

    private void Awake()
    {
        Instance = this;
    }

    // Update polls the runner state every frame. This catches network-interface switches
    // (WiFi → mobile data) reliably even if OnDisconnectedFromServer fires late or is missed.
    private void Update()
    {
        if (!isPlayer || _reconnecting || _isInBackground) return;
        if (GameManager.Instance == null) return;

        bool isRunnerActive = NetworkRunner.Instances.Any(r => r.IsRunning);

        if (_wasRunnerActive && !isRunnerActive)
            StartReconnect();

        _wasRunnerActive = isRunnerActive;
    }

    public void OnApplicationPause(bool paused)
    {
        if (!isPlayer) return;

        _isInBackground = paused;

        if (!paused)
        {
            // Reset so the first Update frame after resume can detect a transition.
            _wasRunnerActive = false;

            var runner = NetworkRunner.Instances.FirstOrDefault();
            if (runner == null || !runner.IsRunning)
                StartReconnect();
        }
    }

    public void SetMatchInfo(NetworkRunner runner, string sessionName, GameMode mode, bool player = false)
    {
        _runner = runner;
        _sessionName = sessionName;
        _mode = mode;
        isPlayer = player;
        _wasRunnerActive = player && runner != null;

        if (_runner != null)
            _runner.AddCallbacks(this);
    }

    public async void StartReconnect()
    {
        if (_reconnecting) return;

        _reconnecting = true;
        GameManager.Instance.AddJob(() => reconnectObject.SetActive(true));

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        await ReconnectLoop(_cts.Token);
    }

    private async Task ReconnectLoop(CancellationToken token)
    {
        const int maxTries = 10;

        for (int i = 1; i <= maxTries; i++)
        {
            if (token.IsCancellationRequested) return;

            Debug.Log($"[Reconnect] Attempt {i}/{maxTries}");

            if (_runner != null)
            {
                try { await _runner.Shutdown(); } catch { }
                Destroy(_runner.gameObject);
                _runner = null;
            }

            await Task.Delay(1000, token);

            _runner = Instantiate(runnerPrefab);
            _runner.AddCallbacks(this);

            var args = new StartGameArgs
            {
                GameMode = _mode,
                SessionName = _sessionName,
                SceneManager = _runner.GetComponent<INetworkSceneManager>(),
                ConnectionToken = Encoding.UTF8.GetBytes(userData.Username)
            };

            var result = await _runner.StartGame(args);

            if (result.Ok)
            {
                Debug.Log("[Reconnect] Connected. Re-claiming identity...");

                _reconnecting = false;
                _wasRunnerActive = true;

                GameManager.Instance.AddJob(() =>
                {
                    reconnectObject.SetActive(false);
                    GameManager.Instance.NoBGLoading.SetActive(false);
                    // Re-send identity claim RPC here (e.g. RPC_ClaimIdentity)
                });

                return;
            }

            Debug.LogWarning($"[Reconnect] Failed: {result.ShutdownReason}");
            await Task.Delay(1500, token);
        }

        // All attempts exhausted. Disable isPlayer first so Update doesn't re-trigger.
        _reconnecting = false;
        isPlayer = false;
        _wasRunnerActive = false;

        GameManager.Instance.AddJob(() =>
        {
            reconnectObject.SetActive(false);
            GameManager.Instance.NoBGLoading.SetActive(false);
            GameManager.Instance.SceneController.GetActionLoadingList.Clear();
            GameManager.Instance.NotificationController.ShowError("You were disconnected from the game.", null);
            GameManager.Instance.SceneController.CurrentScene = "Lobby";
        });

        Debug.LogError("[Reconnect] Gave up. Returning to lobby.");
    }

    // ---- INetworkRunnerCallbacks ----

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[Photon] Disconnected: {reason}");

        // Background disconnects are handled by the OnApplicationPause resume path.
        // Update() handles foreground drops; this is belt-and-suspenders for fast callbacks.
        if (!isPlayer || _isInBackground) return;

        StartReconnect();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[Photon] Shutdown: {shutdownReason}");
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
