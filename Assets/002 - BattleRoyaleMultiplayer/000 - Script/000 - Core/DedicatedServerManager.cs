using Cinemachine;
using ExitGames.Client.Photon.StructWrapping;
using Fusion;
using Fusion.Addons.SimpleKCC;
using Fusion.Sample.DedicatedServer;
using Fusion.Sockets;
using MyBox;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Matchmaker.Models;
#if UNITY_SERVER || UNITY_EDITOR
using Unity.Services.Multiplay;
using Unity.VisualScripting.Antlr3.Runtime;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public class DedicatedServerManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Header("SERVER")]
    [SerializeField] NetworkPrefabRef playerPrefab;
    [SerializeField] private NetworkRunner serverNetworkRunnerPrefab;
    [SerializeField] private bool debugMode;
    [ConditionalField("debugMode")][SerializeField] private string lobby;

    [Header("CLIENT")]
    [SerializeField] private List<GameObject> clientObjs;

    [Header("CRATES")]
    [SerializeField] private NetworkObject createNO;
    [SerializeField] private List<Transform> createSpawnLocations;

    [Header("DEBUGGER")]
    [MyBox.ReadOnly][SerializeField] private NetworkRunner networkRunner;

    //  =================

#if UNITY_SERVER || UNITY_EDITOR
    private IMultiplayService _multiplayService;
    private MultiplayEventCallbacks _multiplayEventCallbacks;
    private IServerEvents _serverEvents;
#endif

    public MatchmakingResults MatchmakingResults;


    [Networked, Capacity(10)] private NetworkDictionary<PlayerRef, PlayerNetworkCore> Players => default;
    //  =================

    private void Awake()
    {
        if (GameManager.Instance == null)
        {
            StartServer();

            foreach (GameObject gameObj in clientObjs)
                gameObj.SetActive(false);
        }
    }

    private async void StartServer()
    {
        var config = DedicatedServerConfig.Resolve();

        Debug.Log($"Start Initializing Unity Services");

        if (debugMode)
        {
            await StartGame();
        }
        else 
        {
#if UNITY_SERVER || UNITY_EDITOR
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            Debug.Log($"Unity Services State: {UnityServices.State}");

            try
            {
                _multiplayService = MultiplayService.Instance;
                await _multiplayService.StartServerQueryHandlerAsync(10, serverName: "n/a", gameType: "n/a", buildId: "0", map: "n/a");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Something wrong with multiplay service {ex}");

                Application.Quit();
            }

            Debug.Log($"Is Multiplay Service null: {_multiplayService == null}");

            if (_multiplayService != null)
            {
                Debug.Log($"Start Setting up for allocation events and callbacks");

                // Setup allocations
                _multiplayEventCallbacks = new MultiplayEventCallbacks();
                _multiplayEventCallbacks.Allocate += OnAllocate;
                _multiplayEventCallbacks.Deallocate += OnDeallocate;
                _multiplayEventCallbacks.Error += OnError;
                _serverEvents = await MultiplayService.Instance.SubscribeToServerEventsAsync(_multiplayEventCallbacks);
            }
#endif
        }
    }

#if UNITY_SERVER || UNITY_EDITOR
    private async void OnAllocate(MultiplayAllocation allocation)
    {
        Debug.Log($"Done allocation");
        LogServerConfig();

        await StartGame();
    }

    private void OnDeallocate(MultiplayDeallocation deallocation)
    {
        Debug.Log("Deallocated");
        LogServerConfig();

        MatchmakingResults = null;

        // Hack for now, just exit the application on deallocate
        Application.Quit();
    }

    private void OnError(MultiplayError error)
    {
        LogServerConfig();
        throw new NotImplementedException();
    }

    private void LogServerConfig()
    {
        var serverConfig = MultiplayService.Instance.ServerConfig;
        Debug.Log($"Server ID[{serverConfig.ServerId}], AllocationId[{serverConfig.AllocationId}], Port[{serverConfig.Port}], QueryPort[{serverConfig.QueryPort}], LogDirectory[{serverConfig.ServerLogDirectory}]");
    }
#endif

    //private async Task<MatchmakingResults> GetMatchmakerPayload()

    private async Task StartGame()
    {
        Debug.Log($"Starting Photon Server");

        networkRunner = Instantiate(serverNetworkRunnerPrefab);

        SceneRef sceneRef = default;

        var scenePath = SceneManager.GetActiveScene().path;

        scenePath = scenePath.Substring("Assets/".Length, scenePath.Length - "Assets/".Length - ".unity".Length);
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);

        if (sceneIndex >= 0)
        {
            sceneRef = SceneRef.FromIndex(sceneIndex);
        }

        NetworkSceneInfo networkSceneInfo = new NetworkSceneInfo();

        if (sceneRef.IsValid == true)
        {
            networkSceneInfo.AddSceneRef(sceneRef, LoadSceneMode.Single, LocalPhysicsMode.None, true);
        }

        if (debugMode)
        {
            await networkRunner.StartGame(new StartGameArgs()
            {
                SessionName = Guid.NewGuid().ToString(),
                GameMode = GameMode.Server,
                IsVisible = true,
                IsOpen = true,
                SceneManager = networkRunner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                Scene = networkSceneInfo,
                PlayerCount = 10,
                Address = NetAddress.Any(),
                CustomLobbyName = lobby
            });

            Debug.Log($"Done Setting up photon server");

            Debug.Log($"Spawning Crates");
            StartCoroutine(SpawnCrates());

            return;
        }

        var config = DedicatedServerConfig.Resolve();

        var sessionName = Guid.NewGuid().ToString();

        Debug.Log($"Server command lines \n\n Session Name: {sessionName} \n Lobby: {config.Lobby} \n SceneIndex: {config.SceneIndex}");

        await networkRunner.StartGame(new StartGameArgs()
        {
            SessionName = sessionName,
            GameMode = GameMode.Server,
            IsVisible = true,
            IsOpen = true,
            SceneManager = networkRunner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
            Scene = networkSceneInfo,
            PlayerCount = 10,
            Address = NetAddress.Any(),
            CustomLobbyName = config.Lobby
        });

        Debug.Log($"Done Setting up photon server");

        Debug.Log($"Spawning Crates");
        StartCoroutine(SpawnCrates());
    }

    Dictionary<string, int> GenerateRandomItems()
    {
        List<string> itemPool = new List<string>
        {
            "rifle", "rifle", "rifle", "rifle", "rifle", // 5%
            "bow", "bow", "bow", "bow", "bow", // 5%
            "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", // 15%
            "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", // 15%
            "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo",
            "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo",
            "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo",
            "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", // 60% total ammo, divided into rifle and bow ammo
            "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo",
            "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo"
        };

        Dictionary<string, string> itemIDMap = new Dictionary<string, string>
        {
            { "sword", "001" },
            { "spear", "002" },
            { "rifle", "003" },
            { "bow", "004" },
            { "rifle ammo", "005" },
            { "bow ammo", "006" }
        };

        Dictionary<string, int> selectedItems = new Dictionary<string, int>();

        int itemListQTY = UnityEngine.Random.Range(1, 11);

        for (int i = 0; i < itemListQTY; i++)
        {
            string selectedItem = itemPool[UnityEngine.Random.Range(0, itemPool.Count)];
            string itemID = itemIDMap[selectedItem];

            if (selectedItem == "rifle ammo" || selectedItem == "bow ammo")
            {
                if (selectedItems.ContainsKey(itemID))
                {
                    selectedItems[itemID] += UnityEngine.Random.Range(1, 61); // Add a random quantity between 1 and 60
                }
                else
                {
                    selectedItems[itemID] = UnityEngine.Random.Range(1, 61); // Set a random quantity between 1 and 60
                }
            }
            else
            {
                if (!selectedItems.ContainsKey(itemID))
                {
                    selectedItems[itemID] = 1; // Set quantity to 1 for non-ammo items
                }
            }
        }

        return selectedItems;
    }

    IEnumerator SpawnCrates()
    {
        while (!Runner) yield return null;

        int index = 1;

        foreach(var spawnLocations in createSpawnLocations)
        {
            var gameobject = Runner.Spawn(createNO, spawnLocations.transform.position, Quaternion.identity, null);

            gameobject.GetComponent<CrateController>().SetDatas(GenerateRandomItems());

            index++;

            yield return null;
        }

        Runner.SessionInfo.Properties.TryAdd("IsOpen", true);
    }

    #region PLAYER JOIN LEAVE

    public void PlayerJoined(PlayerRef player)
    {
        if (HasStateAuthority)
        {
            NetworkObject playerObj = Runner.Spawn(playerPrefab, Vector3.up, Quaternion.identity, player);

            PlayerNetworkCore playerNetowrkCore = playerObj.GetComponent<PlayerNetworkCore>();

            NetworkObject playerCharacter = Runner.Spawn(playerNetowrkCore.PlayerCharacterObj, Vector3.up, Quaternion.identity, player);

            playerNetowrkCore.ServerManager = Object;
            playerNetowrkCore.PlayerCharacterSpawnedObj = playerCharacter;

            playerCharacter.GetComponent<SimpleKCC>().SetPosition(playerNetowrkCore.SpawnPosition);

            Players.Add(player, playerObj.GetComponent<PlayerNetworkCore>());
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority) return;

        if (Players.TryGet(player, out PlayerNetworkCore clientPlayer))
        {
            Players.Remove(player);
            Runner.Despawn(clientPlayer.PlayerCharacterSpawnedObj);
            Runner.Despawn(clientPlayer.Object);
        }
    }


    #endregion
}
