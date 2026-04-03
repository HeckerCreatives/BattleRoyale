using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerPhotonReconnect : MonoBehaviour
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
    private CancellationTokenSource _cts;

    public void OnApplicationFocus(bool focus)
    {
        if (!isPlayer) return;

        //if (focus)
        //{
        //    if (lastOutOfFocusTime > 0f && (Time.time - lastOutOfFocusTime) < 60f)
        //        StartReconnect();
        //}
        //else
        //{
        //    lastOutOfFocusTime = Time.time;
        //}
    }

    private void Awake()
    {
        Instance = this;
    }

    public void SetMatchInfo(NetworkRunner runner, string sessionName, GameMode mode, bool player = false)
    {
        _runner = runner;
        _sessionName = sessionName;
        _mode = mode;
        isPlayer = player;
    }

    public async void StartReconnect()
    {
        if (_reconnecting) return;

        reconnectObject.SetActive(true);

        _reconnecting = true;

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

            // Ensure runner is fully shut down
            if (_runner != null)
            {
                try { await _runner.Shutdown(); } catch { }
                Destroy(_runner.gameObject);
                _runner = null;
            }

            await Task.Delay(1000, token);

            // Create a fresh runner
            _runner = Instantiate(runnerPrefab);

            var args = new StartGameArgs
            {
                GameMode = _mode,                // Client
                SessionName = _sessionName,      // same room name
                SceneManager = _runner.GetComponent<INetworkSceneManager>(), // if you use it.
                ConnectionToken = Encoding.UTF8.GetBytes(userData.Username)
            };

            var result = await _runner.StartGame(args);

            if (result.Ok)
            {
                Debug.Log("[Reconnect] Connected. Re-claiming identity...");

                _reconnecting = false;

                reconnectObject.SetActive(false);
                // IMPORTANT: run your existing reclaim handshake again
                // e.g. send RPC_ClaimIdentity(LocalUserId) after you have input authority context

                return;
            }

            Debug.LogWarning($"[Reconnect] Failed: {result.ShutdownReason}");
            await Task.Delay(1500, token);
        }

        _reconnecting = false;

        reconnectObject.SetActive(false);
        Debug.LogError("[Reconnect] Gave up. Return to lobby / show 'match ended'.");
        // TODO: go to lobby UI
    }
}
