using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
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

        if (args.TryGetValue("playercostumedata", out string playercostumedata))
        {
            Debug.Log(playercostumedata);
        }
        else
        {
            playercostumedata = "[{\"_id\":\"69a78ec0e4e0229d2556a284\",\"username\":\"STRONGWARRIOR53\",\"ownerId\":\"69a78ec0e4e0229d2556a284\",\"hairstyle\":0,\"haircolor\":0,\"clothingcolor\":0,\"skincolor\":0}]";
        }

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

                Debug.Log($"SpawningPLayers");
                obj.GetComponent<PlayerJoinedController>().SpawnPlayers(playercostumedata);
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
}