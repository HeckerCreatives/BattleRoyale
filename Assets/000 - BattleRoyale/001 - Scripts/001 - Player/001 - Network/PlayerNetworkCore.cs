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

    [field: Header("DEBUGGER PUBLIC")]
    [Networked][field: SerializeField][field: MyBox.ReadOnly] public NetworkObject PlayerCharacterSpawnedObj { get; set; }

    private void Awake()
    {
        StartCoroutine(Initialize());
    }

    IEnumerator Initialize()
    {
        while (Runner == null) yield return null;

        if (!HasInputAuthority) yield break;

        Debug.Log($"Is runner Client: {Runner.IsClient}");

        playerVCam = GameObject.FindGameObjectWithTag("PlayerVCam").GetComponent<CinemachineVirtualCamera>();
        aimPlayerVCam = GameObject.FindGameObjectWithTag("AimVCam").GetComponent<CinemachineVirtualCamera>();

        playerVCam.Follow = PlayerCharacterSpawnedObj.transform.GetChild(2);
        playerVCam.LookAt = PlayerCharacterSpawnedObj.transform.GetChild(2);

        aimPlayerVCam.Follow = PlayerCharacterSpawnedObj.transform.GetChild(2);
        aimPlayerVCam.LookAt = PlayerCharacterSpawnedObj.transform.GetChild(2);

        aimPlayerVCam.gameObject.SetActive(false);

        PlayerCharacterSpawnedObj.GetComponent<PlayerAim>().AimVCam = aimPlayerVCam;
    }
}
