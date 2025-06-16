using Fusion;
using Fusion.Sockets;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMultiplayerEvents : SimulationBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private GameplayController gameplayController;

    [Header("DEBUGGER")]
    [MyBox.ReadOnly] public bool doneLoadingScene;

    //  ================

    public Action queuedisconnection;

    //  ================

    #region NETWORK

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        gameplayController.InitializeControllers();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (shutdownReason == ShutdownReason.ConnectionTimeout || shutdownReason == ShutdownReason.OperationTimeout || shutdownReason == ShutdownReason.ConnectionRefused || shutdownReason == ShutdownReason.DisconnectedByPluginLogic)
        {
            Debug.Log($"Server error: {shutdownReason}");

            GameManager.Instance.NotificationController.ShowError($"There's a problem with the game server! Please queue up and try again. Error: {(shutdownReason == ShutdownReason.DisconnectedByPluginLogic ? "Server Shutdown due to unexpected error." : shutdownReason)}");

            Runner.Shutdown();
            GameManager.Instance.SceneController.CurrentScene = "Lobby";
        }
        else if (shutdownReason == ShutdownReason.MaxCcuReached || shutdownReason == ShutdownReason.GameIsFull || shutdownReason == ShutdownReason.GameClosed)
        {

            string errormessage = "";

            switch (shutdownReason)
            {
                case ShutdownReason.GameIsFull:
                    errormessage = $"The game you're trying to join is full! Please queue up and try again";
                    break;
                case ShutdownReason.GameClosed:
                    errormessage = $"The game you're trying to join is now closed! Please queue up and try again";
                    break;
                case ShutdownReason.MaxCcuReached:
                    errormessage = $"The game server is experiencing high level of players! Please try again later or contact customer support for more details";
                    break;
            }

            GameManager.Instance.NotificationController.ShowError(errormessage);

            queuedisconnection?.Invoke();
        }
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    #endregion
}
