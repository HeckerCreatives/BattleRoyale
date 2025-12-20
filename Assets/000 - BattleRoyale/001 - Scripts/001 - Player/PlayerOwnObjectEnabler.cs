using Fusion;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.CullingGroup;

public class PlayerOwnObjectEnabler : NetworkBehaviour
{
    public PlayerPlayables CurrentPlayerPlayables
    {
        get => playerPlayables;
    }

    public PlayerInventoryV2 Inventory
    {
        get => inventory;
    }

    //  ==============

    [SerializeField] private UserData userData;

    [Space]
    [SerializeField] private PlayerPlayables playerPlayables;
    [SerializeField] private PlayerCameraRotation cameraRotation;
    [SerializeField] private PlayerMovementV2 movementV2;
    [SerializeField] private GameSettingController gameSettingController;
    [SerializeField] private PlayerInventoryV2 inventory;

    [Space]
    [SerializeField] private GameObject canvasPlayer;
    [SerializeField] private GameObject playerVcam;
    [SerializeField] private GameObject playerAimVCam;
    [SerializeField] private GameObject playerMiniMapIcon;
    [SerializeField] private GameObject playerMapIcon;
    [SerializeField] private GameObject playerMinimapIcon;
    [SerializeField] private GameObject playerMinimapCam;
    [SerializeField] private GameObject playerSpawnLocCam;
    [SerializeField] private GameObject footstepSFX;
    [SerializeField] private GameObject punchSFX;

    [Space]
    [SerializeField] private GameObject reconnectObj;

    [field: Header("DEBUGGER")]
    [Networked][field: SerializeField] public DedicatedServerManager ServerManager { get; set; }
    [Networked][field: SerializeField] public bool NotEnoughPlayer { get; set; }
    [Networked][field: SerializeField] public string Username { get; set; }
    [Networked][field: SerializeField] public bool Removing { get; set; }
    [Networked][field: SerializeField] public bool DoneInit { get; set; }

    //  ====================

    private ChangeDetector _changeDetector;

    //  ====================

    private void OnDisable()
    {
        GameManager.Instance.SocketMngr.IsOnGame = false;
    }

    public async override void Spawned()
    {
        while (!Runner) await Task.Yield();

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority)
        {
            footstepSFX.SetActive(false);
            punchSFX.SetActive(false);
        }

        if (!HasInputAuthority) return;

        GameManager.Instance.SocketMngr.IsOnGame = true;

        if (!DoneInit)
        {
            GameManager.Instance.SceneController.AddActionLoadinList(GameManager.Instance.PostRequest("/usergamedetail/useenergy", "", new Dictionary<string, object> { }, false, (response) =>
            {
                userData.GameDetails.energy -= userData.GameDetails.energy > 0 ? 1 : 0;
            }, () =>
            {
                Runner.Shutdown(true);
                GameManager.Instance.SceneController.StopLoading();
                GameManager.Instance.SocketMngr.Socket.Disconnect();
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later.", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }));
        }
        else
            GameManager.Instance.SceneController.AddActionLoadinList(CheckArena());

        GameManager.Instance.SceneController.AddActionLoadinList(InitializePlayer());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetVolumeSlidersOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetGraphicsOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetLookSensitivityOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(ChangeDoneInitialize());
        GameManager.Instance.SceneController.ActionPass = true;

        RPC_SetUsername(userData.Username);

        canvasPlayer.SetActive(true);
        playerVcam.SetActive(true);
        playerAimVCam.SetActive(true);
        playerMiniMapIcon.SetActive(true);
        playerMinimapIcon.SetActive(true);
        playerMapIcon.SetActive(true);
        playerMinimapCam.SetActive(true);
        playerSpawnLocCam.SetActive(true);

        canvasPlayer.transform.parent = null;
        playerVcam.transform.parent = null;
        playerAimVCam.transform.parent = null;

        NetworkRunner.CloudConnectionLost += OnCloudConnectionLost;

        while (ServerManager == null) await Task.Yield();

        ServerManager.OnCurrentStateChange += StateChange;
    }

    private IEnumerator ChangeDoneInitialize()
    {
        RPC_ChangeDoneInit();
        yield return null;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ChangeDoneInit()
    {
        DoneInit = true;
    }

    private IEnumerator CheckArena()
    {
        if (ServerManager.CurrentGameState == GameState.WAITINGAREA)
            ServerManager.ArenaEnabler(true, false);
        else
            ServerManager.ArenaEnabler(false, true);

        yield return null;
    }

    private void OnCloudConnectionLost(NetworkRunner networkRunner, ShutdownReason shutdownReason, bool reconnecting)
    {
        Debug.Log($"Cloud Connection Lost: {shutdownReason} (Reconnecting: {reconnecting})");

        if (!reconnecting) 
        {
            Debug.Log($"Server error: {shutdownReason}");

            GameManager.Instance.NotificationController.ShowError($"There's a problem with the game server! Please queue up and try again. Error: {(shutdownReason == ShutdownReason.DisconnectedByPluginLogic ? "Server Shutdown due to unexpected error." : shutdownReason)}");

            Runner.Shutdown();

            GameManager.Instance.SceneController.CurrentScene = "Lobby";
        }
        else
        {
            StartCoroutine(WaitForReconnection(networkRunner));
        }
    }

    private IEnumerator WaitForReconnection(NetworkRunner runner)
    {
        reconnectObj.SetActive(true);
        yield return new WaitUntil(() => runner.IsInSession);
        Debug.Log("Reconnected to the Cloud!");
        reconnectObj.SetActive(false);
    }

    public override void Render()
    {
        if (!HasInputAuthority) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(NotEnoughPlayer):

                    if (!NotEnoughPlayer) return;

                    GameManager.Instance.NoBGLoading.SetActive(true);

                    StartCoroutine(GameManager.Instance.PostRequest("/usergamedetail/refundenergy", "", new Dictionary<string, object>(), false, (response) =>
                    {
                        GameManager.Instance.NotificationController.ShowError("Not enough players to start the game! Please try again later", () =>
                        {
                            Runner.Shutdown();
                            GameManager.Instance.SceneController.CurrentScene = "Lobby";
                        });

                    }, null));

                    break;
            }
        }
    }

    private void StateChange(object sender, EventArgs e)
    {
        if (ServerManager.CurrentGameState == GameState.ARENA)
        {
            if (HasInputAuthority)
            {
                GameManager.Instance.SceneController.SpawnArenaLoading = true;
                GameManager.Instance.SceneController.AddActionLoadinList(ReadyForBattle());
                GameManager.Instance.SceneController.ActionPass = true;
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetUsername(string uname)
    {
        Username = uname;
    }

    IEnumerator ReadyForBattle()
    {
        while (!ServerManager.DonePlayerBattlePositions) yield return null;
    }


    IEnumerator InitializePlayer()
    {
        cameraRotation.InitializeCameraRotationSensitivity();
        movementV2.PlayerMovementInitialize();
        yield return null;
    }

    public void QuitBtn()
    {
        if (!Runner) return;

        if (ServerManager == null) return;

        if (!HasInputAuthority) return;

        if (ServerManager.CurrentGameState == GameState.WAITINGAREA)
        {
            GameManager.Instance.NotificationController.ShowConfirmation("Quit now and lose 2 energy", () =>
            {
                RPC_Removeplayer(false);
                Runner.Shutdown();
                GameManager.Instance.SceneController.CurrentScene = "Lobby";
            }, null);
        }
        else if (ServerManager.CurrentGameState == GameState.ARENA)
        {
            GameManager.Instance.NotificationController.ShowConfirmation("Quit now and lose all XP, points, and 2 energy.", () =>
            {
                RPC_Removeplayer(false);
                Runner.Shutdown();
                GameManager.Instance.SceneController.CurrentScene = "Lobby";
            }, null);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_Removeplayer(bool value)
    {
        Removing = value;
    }

    public async void ReturnToMenuBtn()
    {
        GameManager.Instance.NoBGLoading.SetActive(true);

        RPC_Removeplayer(true);

        await Task.Delay(2000);

        GameManager.Instance.NoBGLoading.SetActive(false);

        await Runner.Shutdown();

        GameManager.Instance.SceneController.CurrentScene = "Lobby";
    }
}
