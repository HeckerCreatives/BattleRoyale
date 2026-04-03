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

    public PlayerCameraRotation CameraRotation
    {
        get => cameraRotation;
    }

    public PlayerHealthV2 PlayerHealth
    {
        get => health;
    }

    //  ==============

    [SerializeField] private UserData userData;

    [Space]
    [SerializeField] private PlayerPlayables playerPlayables;
    [SerializeField] private PlayerCameraRotation cameraRotation;
    [SerializeField] private PlayerMovementV2 movementV2;
    [SerializeField] private GameSettingController gameSettingController;
    [SerializeField] private PlayerInventoryV2 inventory;
    [SerializeField] private PlayerHealthV2 health;
    [SerializeField] private Transform target;

    [Space]
    [SerializeField] private GameObject reconObj;
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

    [Header("DEBUGGER")]
    [SerializeField] private bool isStateAuthority;
    [SerializeField] private bool isInputAuthority;

    [field: Header("DEBUGGER")]
    [Networked][field: SerializeField] public NetworkString<_64> Username { get; set; }
    [Networked][field: SerializeField] public NetworkString<_64> UserID { get; set; }
    [Networked][field: SerializeField] public bool Removing { get; set; }
    [Networked][field: SerializeField] public bool DoneInit { get; set; }

    private void OnDisable()
    {
        GameManager.Instance.SocketMngr.IsOnGame = false;
    }

    private void OnDestroy()
    {
        if (isStateAuthority) return;

        if (!isInputAuthority) return;

        Destroy(canvasPlayer);
        Destroy(playerVcam);
        Destroy(playerAimVCam);
        Destroy(playerMinimapCam);
        Destroy(playerSpawnLocCam);
        Destroy(target.gameObject);
    }

    public async void OnEnable()
    {
        while (!Runner) await Task.Yield();

        target.parent = null;

        if (HasStateAuthority)
        {
            footstepSFX.SetActive(false);
            punchSFX.SetActive(false);

            isStateAuthority = true;
        }

        if (!HasInputAuthority) return;

        isInputAuthority = true;

        GameManager.Instance.SocketMngr.IsOnGame = true;

        GameManager.Instance.SceneController.AddActionLoadinList(InitializePlayer());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetVolumeSlidersOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetGraphicsOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetLookSensitivityOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(ChangeDoneInitialize());
        GameManager.Instance.SceneController.ActionPass = true;

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

        MultiplayerServerManager.Instance.OnCurrentStateChange += StateChange;
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

    private void StateChange(object sender, EventArgs e)
    {
        if (MultiplayerServerManager.Instance.CurrentGameState == GameState.ARENA)
        {
            if (HasInputAuthority)
            {
                //GameManager.Instance.SceneController.SpawnArenaLoading = true;
                GameManager.Instance.SceneController.AddActionLoadinList(ReadyForBattle());
                GameManager.Instance.SceneController.ActionPass = true;
            }
        }
    }

    IEnumerator ReadyForBattle()
    {
        while (!MultiplayerServerManager.Instance.DonePlayerBattlePositions) yield return null;
    }


    IEnumerator InitializePlayer()
    {
        PlayerPhotonReconnect.Instance.SetMatchInfo(Runner, Runner.SessionInfo.Name, GameMode.Client, true);

        cameraRotation.InitializeCameraRotationSensitivity();
        //movementV2.PlayerMovementInitialize();
        yield return null;
    }

    public void QuitBtn()
    {
        if (!Runner) return;

        if (!HasInputAuthority) return;

        GameManager.Instance.NotificationController.ShowConfirmation("Quit now and lose all XP, points, and 2 energy.", () =>
        {
            RPC_Removeplayer(false);
            Runner.Shutdown();
            GameManager.Instance.SceneController.CurrentScene = "Lobby";
        }, null);
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
