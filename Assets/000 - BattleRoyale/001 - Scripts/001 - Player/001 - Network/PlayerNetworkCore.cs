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

    [field: Header("DEBUGGER PUBLIC")]
    [Networked][field: SerializeField][field: MyBox.ReadOnly] public NetworkObject PlayerCharacterSpawnedObj { get; set; }
    [Networked][field: SerializeField][field: MyBox.ReadOnly] public NetworkObject ServerManager { get; set; }
    [Networked][field: SerializeField][field: MyBox.ReadOnly] public bool DoneBattlePosition { get; set; }

    private async void Start()
    {
        if (HasInputAuthority)
        {
            GameManager.Instance.SceneController.AddActionLoadinList(Initialize());

            while (PlayerCharacterSpawnedObj == null) await Task.Delay(100);

            while (ServerManager == null) await Task.Delay(100);

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

        killCounterController.PlayerCount = GameObject.FindGameObjectWithTag("PlayerCount").GetComponent<TextMeshProUGUI>();

        waitingAreaTimerController.Timer = GameObject.FindGameObjectWithTag("WaitingAreaTimer");

        #endregion

        spawnLocationController.ServerManager = ServerManager.GetComponent<DedicatedServerManager>();
        spawnLocationController.inventory = PlayerCharacterSpawnedObj.GetComponent<PlayerInventory>();

        waitingAreaTimerController.Timer.SetActive(false);
    }
}
