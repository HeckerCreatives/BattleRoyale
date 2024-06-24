using Cinemachine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNetworkCore : NetworkBehaviour
{
    [Header("PLAYER SETTINGS PUBLIC")]
    [field: SerializeField] public NetworkPrefabRef PlayerCharacterObj { get; private set; }
    [field: SerializeField] public Vector3 SpawnPosition { get; private set; }

    [Header("DEBUGGER")]
    [MyBox.ReadOnly][SerializeField] private CinemachineVirtualCamera playerVCam;
    [MyBox.ReadOnly][SerializeField] private CinemachineVirtualCamera aimPlayerVCam;
    [MyBox.ReadOnly][SerializeField] private GameObject pickupItemButton;
    [MyBox.ReadOnly][SerializeField] private GameObject pickupItemList;

    [field: Header("DEBUGGER PUBLIC")]
    [Networked][field: SerializeField][field: MyBox.ReadOnly] public NetworkObject PlayerCharacterSpawnedObj { get; set; }
    [Networked][field: SerializeField][field: MyBox.ReadOnly] public NetworkObject ServerManager { get; set; }

    private void Awake()
    {
        StartCoroutine(Initialize());
    }

    IEnumerator Initialize()
    {
        while (Runner == null) yield return null;

        while (!PlayerCharacterSpawnedObj) yield return null;

        PlayerCharacterSpawnedObj.GetComponent<PlayerInventory>().DedicatedServer = ServerManager;

        if (!HasInputAuthority) yield break;

        Debug.Log($"Is runner Client: {Runner.IsClient}");

        playerVCam = GameObject.FindGameObjectWithTag("PlayerVCam").GetComponent<CinemachineVirtualCamera>();
        aimPlayerVCam = GameObject.FindGameObjectWithTag("AimVCam").GetComponent<CinemachineVirtualCamera>();

        pickupItemButton = GameObject.FindGameObjectWithTag("PickupButton");
        pickupItemList = GameObject.FindGameObjectWithTag("PickupItemList");

        playerVCam.Follow = PlayerCharacterSpawnedObj.transform.GetChild(2);
        playerVCam.LookAt = PlayerCharacterSpawnedObj.transform.GetChild(2);

        aimPlayerVCam.Follow = PlayerCharacterSpawnedObj.transform.GetChild(2);
        aimPlayerVCam.LookAt = PlayerCharacterSpawnedObj.transform.GetChild(2);

        aimPlayerVCam.gameObject.SetActive(false);
        pickupItemButton.SetActive(false);
        pickupItemList.SetActive(false);

        PlayerCharacterSpawnedObj.GetComponent<PlayerAim>().AimVCam = aimPlayerVCam;
        PlayerCharacterSpawnedObj.GetComponent<PlayerPickupWeaponController>().PickupItemBtn = pickupItemButton;
        PlayerCharacterSpawnedObj.GetComponent<PlayerPickupWeaponController>().PickupItemList = pickupItemList;

        PlayerCharacterSpawnedObj.GetComponent<PlayerPickupWeaponController>().InitializeContentTF();
    }
}
