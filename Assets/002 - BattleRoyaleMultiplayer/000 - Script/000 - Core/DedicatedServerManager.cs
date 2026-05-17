using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DedicatedServerManager : MonoBehaviour
{
    public static DedicatedServerManager Instance;

    //  ==========================

    public float SafeZoneTimeToShrink { get => safeZoneTimeToShrink; }

    public bool PrivateServer { get => usePrivateServer; }

    //  ==========================

    public NetworkObject serverManager;

    //[Space]
    //public PoolObjectProvider poolObjectProvider;

    [Space]
    [SerializeField] private NetworkRunner serverNetworkRunnerPrefab;

    [Space]
    [SerializeField] private bool usePrivateServer;
    [SerializeField] private string lobby;
    [SerializeField] private int maxPlayers;

    [Space]
    [SerializeField] private float safeZoneTimeToShrink;

    [Space]
    [SerializeField] private List<Transform> spawnWaitingAreaPositions;
    [SerializeField] private List<Transform> createSpawnLocations;

    [Header("DEBUGGER")]
    [SerializeField] private NetworkRunner networkRunner;
    [SerializeField] private string sessionname;

    public async void Awake()
    {
        Instance = this;

        if (GameManager.Instance == null)
        {
            Debug.Log("THIS IS A SERVER INSTANTIATING SERVER MANAGER");

            await spawnWaitingAreaPositions.Shuffle();

            await StartGame();

            Debug.Log("DONE SERVER INSTANTIATING");
        }
    }

    private FusionAppSettings BuildCustomAppSetting(string region)
    {

        var appSettings = PhotonAppSettings.Global.AppSettings.GetCopy();

        appSettings.UseNameServer = true;
        //appSettings.AppVersion = appVersion;

        //if (string.IsNullOrEmpty(customAppID) == false)
        //{
        //    appSettings.AppIdFusion = customAppID;
        //}

        if (string.IsNullOrEmpty(region) == false)
        {
            appSettings.FixedRegion = region.ToLower();
        }

        // If the Region is set to China (CN),
        // the Name Server will be automatically changed to the right one
        // appSettings.Server = "ns.photonengine.cn";

        return appSettings;
    }

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

        var args = CommandLineHelper.GetArgs();

        FusionAppSettings appSettings;

        if (usePrivateServer && args.TryGetValue("roomname", out string roomname))
            sessionname = roomname;
        else
            sessionname = "testing";

        if (args.TryGetValue("region", out string region))
            appSettings = BuildCustomAppSetting(region);
        else
            appSettings = BuildCustomAppSetting("asia");

        // Full lobby = 30: 1 human + 29 bots when both costume CLI args are omitted.
        const int debugFullLobbySlots = 30;
        string playercostumedata;
        if (args.TryGetValue("playercostumedata", out playercostumedata) && !string.IsNullOrWhiteSpace(playercostumedata))
            Debug.Log(playercostumedata);
        else
            playercostumedata = BuildDebugPlayerCostumeJson(1);

        int realPlayerCount = TryCountPlayersFromCostumeJson(playercostumedata);
        if (realPlayerCount <= 0)
            realPlayerCount = 1;

        int fallbackBotCount = Mathf.Max(0, debugFullLobbySlots - realPlayerCount);

        string botcostumedata;
        if (args.TryGetValue("botcostumedata", out botcostumedata) && !string.IsNullOrWhiteSpace(botcostumedata))
        {
            try
            {
                var parsed = JsonConvert.DeserializeObject<List<BotSpawnData>>(botcostumedata);
                if (parsed == null)
                    botcostumedata = BuildDeterministicBotCostumeJson(fallbackBotCount);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"botcostumedata invalid; using deterministic fill ({fallbackBotCount}). {ex.Message}");
                botcostumedata = BuildDeterministicBotCostumeJson(fallbackBotCount);
            }
        }
        else
            botcostumedata = BuildDeterministicBotCostumeJson(fallbackBotCount);

        int botSpawnCount = TryCountBotsFromCostumeJson(botcostumedata);

        Debug.Log($"STARTING REGION: {appSettings.FixedRegion}");

        await networkRunner.StartGame(new StartGameArgs()
        {
            //ObjectProvider = poolObjectProvider,
            SessionName = sessionname,
            GameMode = GameMode.Server,
            IsVisible = false,
            IsOpen = false,
            SceneManager = networkRunner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
            Scene = networkSceneInfo,
            PlayerCount = maxPlayers,
            Address = NetAddress.Any(),
            CustomLobbyName = lobby,
            CustomPhotonAppSettings = appSettings,
        });

        if (networkRunner.IsRunning)
        {
            NetworkObject temprunner = networkRunner.Spawn(serverManager, Vector3.zero, Quaternion.identity, PlayerRef.None, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
            {
                Debug.Log($"Done Setting up photon server");

                obj.GetComponent<MultiplayerServerManager>().SessionName = sessionname;

                obj.GetComponent<WeaponCratesSpawnerController>().CreateSpawnLocations = createSpawnLocations;
                obj.GetComponent<PlayerJoinedController>().SpawnWaitingAreaPositions = spawnWaitingAreaPositions;

                //poolObjectProvider.SetMaxPoolCount(100);

                obj.GetComponent<KillNotifServerController>().SpawnNotifUI();

                Debug.Log($"Spawning Crates");
                obj.GetComponent<WeaponCratesSpawnerController>().SpawnCrates();

                Debug.Log($"Scattering items");
                obj.GetComponent<MeshMapScatterTool>().Generate();

                Debug.Log($"Set Spawn Positions");
                obj.GetComponent<MultiplayerServerManager>().SetSpawnPositionPlayers();

                Debug.Log($"Set Safe Zone");
                obj.GetComponent<SafeZoneServerController>().SetSafeZoneArea();

                Debug.Log($"Spawning players ({realPlayerCount}) and bots ({botSpawnCount}) from botcostumedata length");
                obj.GetComponent<PlayerJoinedController>().SpawnMatchPopulation(playercostumedata, botcostumedata);
            });

            MultiplayerServerManager tempmanager = temprunner.GetComponent<MultiplayerServerManager>();
            SafeZoneServerController tempzone = temprunner.GetComponent<SafeZoneServerController>();
            WeaponCratesSpawnerController tempcrates = temprunner.GetComponent<WeaponCratesSpawnerController>();
            PlayerJoinedController tempjoined = temprunner.GetComponent<PlayerJoinedController>();
            MeshMapScatterTool scatterTool = temprunner.GetComponent<MeshMapScatterTool>();

            while (!tempmanager.DoneSetupBattlePos || !tempcrates.DoneSpawnCrates || !tempzone.DoneSetupSafeZone || !tempjoined.DoneSetupPlayers || !scatterTool.doneInitialize)
            {
                await Task.Yield();
            }

            networkRunner.SessionInfo.IsOpen = true;
            networkRunner.SessionInfo.IsVisible = true;

            if (tempmanager.CurrentGameState != GameState.ARENA)
            {
                tempmanager.CurrentGameState = GameState.ARENA;

                tempmanager.DonePlayerBattlePositions = true;
                tempzone.SafeZoneTimer = safeZoneTimeToShrink;
                tempzone.CurrentSafeZoneState = SafeZoneState.TIMER;
            }

            if (usePrivateServer)
                tempmanager.ChangeServerStatus();

            Debug.Log("ALL PLAYERS CAN NOW JOIN");
        }
    }

    private static int TryCountPlayersFromCostumeJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0;
        try
        {
            var list = JsonConvert.DeserializeObject<List<PlayerSpawnData>>(json);
            return list?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private static int TryCountBotsFromCostumeJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0;
        try
        {
            var list = JsonConvert.DeserializeObject<List<BotSpawnData>>(json);
            return list?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>Stable bot rows for local / headless debug (same seed pattern every run).</summary>
    private static string BuildDeterministicBotCostumeJson(int count)
    {
        count = Mathf.Max(0, count);
        var list = new List<BotSpawnData>();
        for (int i = 0; i < count; i++)
        {
            list.Add(new BotSpawnData
            {
                username = $"BOT_debug_{i:00}",
                avatarid = (i * 3) % 8,
                hairstyle = i % 5,
                haircolor = i % 4,
                clothingcolor = i % 6,
                skincolor = i % 4
            });
        }

        return JsonConvert.SerializeObject(list);
    }

    /// <summary>
    /// Debug humans when <c>-playercostumedata</c> is omitted. Use <paramref name="humanCount"/> = 1 for one <c>DEBUG_Player</c> + 29 bots (30 total).
    /// Use a higher count only for stress tests (spawns that many player prefabs).
    /// </summary>
    private static string BuildDebugPlayerCostumeJson(int humanCount)
    {
        humanCount = Mathf.Max(1, humanCount);
        var list = new List<PlayerSpawnData>();
        for (int i = 0; i < humanCount; i++)
        {
            if (humanCount == 1)
            {
                list.Add(new PlayerSpawnData
                {
                    _id = "debug-local",
                    username = "DEBUG_Player",
                    ownerId = "debug-local",
                    avatarid = 0,
                    hairstyle = 0,
                    haircolor = 0,
                    clothingcolor = 0,
                    skincolor = 0
                });
                break;
            }

            list.Add(new PlayerSpawnData
            {
                _id = $"debug-local-{i}",
                username = $"DEBUG_Player_{i:00}",
                ownerId = $"debug-local-{i}",
                avatarid = (i * 2) % 8,
                hairstyle = i % 5,
                haircolor = i % 4,
                clothingcolor = i % 6,
                skincolor = i % 4
            });
        }

        return JsonConvert.SerializeObject(list);
    }
}