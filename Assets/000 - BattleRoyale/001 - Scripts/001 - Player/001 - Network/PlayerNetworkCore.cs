using Cinemachine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using MyBox;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class PlayerNetworkCore : NetworkBehaviour
{
    [SerializeField] private KillCountCounterController killCounterController;
    [SerializeField] private WaitingAreaTimerController waitingAreaTimerController;
    [SerializeField] private PlayerSpawnLocationController spawnLocationController;

    [Header("PLAYER SETTINGS PUBLIC")]
    [field: SerializeField] public NetworkPrefabRef PlayerCharacterObj { get; private set; }
    [field: SerializeField] public Vector3 SpawnPosition { get; private set; }

    [Header("DEBUGGER")]
    [MyBox.ReadOnly][SerializeField] private CinemachineVirtualCamera playerVCam;
    [MyBox.ReadOnly][SerializeField] private CinemachineVirtualCamera aimPlayerVCam;
    [MyBox.ReadOnly][SerializeField] private GameObject pickupItemButton;
    [MyBox.ReadOnly][SerializeField] private GameObject pickupItemList;
    [MyBox.ReadOnly][SerializeField] private WeaponEquipBtnController handBtn;
    [MyBox.ReadOnly][SerializeField] private WeaponEquipBtnController primaryBtn;
    [MyBox.ReadOnly][SerializeField] private WeaponEquipBtnController secondaryBtn;

    [field: Header("DEBUGGER PUBLIC")]
    [Networked][field: SerializeField][field: MyBox.ReadOnly] public NetworkObject PlayerCharacterSpawnedObj { get; set; }
    [Networked][field: SerializeField][field: MyBox.ReadOnly] public NetworkObject ServerManager { get; set; }
    [Networked][field: SerializeField][field: MyBox.ReadOnly] public bool DoneBattlePosition { get; set; }

    private async void Start()
    {
        if (HasInputAuthority)
        {
            GameManager.Instance.SceneController.AddActionLoadinList(Initialize());

            while (PlayerCharacterSpawnedObj == null) await Task.Delay(1000);

            GameManager.Instance.SceneController.AddActionLoadinList(PlayerCharacterSpawnedObj.GetComponent<PlayerInventory>().CheckIfSkinIsReady());
            GameManager.Instance.SceneController.AddActionLoadinList(PlayerCharacterSpawnedObj.GetComponent<PlayerInventory>().CheckIfWeaponInitialize());

            while (ServerManager == null) await Task.Delay(1000);

            GameManager.Instance.SceneController.AddActionLoadinList(killCounterController.InitializeKillCount());

            GameManager.Instance.SceneController.AddActionLoadinList(waitingAreaTimerController.InitializeWaitingAreaTimer());

            GameManager.Instance.SceneController.AddActionLoadinList(spawnLocationController.InitializeSpawnLocation());

            GameManager.Instance.SceneController.ActionPass = true;
        }
    }

    IEnumerator Initialize()
    {
        while (Runner == null) yield return null;

        if (!HasInputAuthority) yield break;

        Debug.Log($"Is runner Client: {Runner.IsClient}");

        #region OBJECT REFERENCES

        playerVCam = GameObject.FindGameObjectWithTag("PlayerVCam").GetComponent<CinemachineVirtualCamera>();
        aimPlayerVCam = GameObject.FindGameObjectWithTag("AimVCam").GetComponent<CinemachineVirtualCamera>();

        pickupItemButton = GameObject.FindGameObjectWithTag("PickupButton");
        pickupItemList = GameObject.FindGameObjectWithTag("PickupItemList");

        handBtn = GameObject.FindGameObjectWithTag("HandBtn").GetComponent<WeaponEquipBtnController>();
        primaryBtn = GameObject.FindGameObjectWithTag("PrimaryBtn").GetComponent<WeaponEquipBtnController>();
        secondaryBtn = GameObject.FindGameObjectWithTag("SecondaryBtn").GetComponent<WeaponEquipBtnController>();

        GameObject.FindGameObjectWithTag("MinimapCamera").GetComponent<MinimapCamera>().playerCharacterTF = PlayerCharacterSpawnedObj.transform;

        killCounterController.PlayerCount = GameObject.FindGameObjectWithTag("PlayerCount").GetComponent<TextMeshProUGUI>();

        waitingAreaTimerController.Timer = GameObject.FindGameObjectWithTag("WaitingAreaTimer");
        //waitingAreaTimerController.SpawnMapTimer = GameObject.FindGameObjectWithTag("SpawnLocationTimer").GetComponent<TextMeshProUGUI>();

        playerVCam.Follow = PlayerCharacterSpawnedObj.transform.GetChild(2);
        playerVCam.LookAt = PlayerCharacterSpawnedObj.transform.GetChild(2);

        aimPlayerVCam.Follow = PlayerCharacterSpawnedObj.transform.GetChild(2);
        aimPlayerVCam.LookAt = PlayerCharacterSpawnedObj.transform.GetChild(2);

        #endregion

        aimPlayerVCam.gameObject.SetActive(false);
        pickupItemButton.SetActive(false);
        pickupItemList.SetActive(false);

        PlayerCharacterSpawnedObj.GetComponent<PlayerAim>().AimVCam = aimPlayerVCam;
        PlayerCharacterSpawnedObj.GetComponent<PlayerPickupWeaponController>().PickupItemBtn = pickupItemButton;
        PlayerCharacterSpawnedObj.GetComponent<PlayerPickupWeaponController>().PickupItemList = pickupItemList;

        PlayerCharacterSpawnedObj.GetComponent<PlayerPickupWeaponController>().InitializeContentTF();

        PlayerCharacterSpawnedObj.GetComponent<PlayerInventory>().HandBtn = handBtn;
        PlayerCharacterSpawnedObj.GetComponent<PlayerInventory>().PrimaryBtn = primaryBtn;
        PlayerCharacterSpawnedObj.GetComponent<PlayerInventory>().SecondaryBtn = secondaryBtn;

        PlayerCharacterSpawnedObj.GetComponent<PlayerInventory>().WeaponIndex = 1;

        spawnLocationController.ServerManager = ServerManager.GetComponent<DedicatedServerManager>();
        spawnLocationController.inventory = PlayerCharacterSpawnedObj.GetComponent<PlayerInventory>();

        waitingAreaTimerController.Timer.SetActive(false);
    }
}
