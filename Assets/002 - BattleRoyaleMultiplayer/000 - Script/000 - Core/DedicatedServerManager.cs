using Fusion;
using Fusion.Addons.SimpleKCC;
using Fusion.Sockets;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    WAITINGAREA,
    ARENA,
    DONE
}

public enum SafeZoneState
{
    NONE,
    TIMER,
    SHRINK
}

public struct PlayerBattleSpawnPosition : INetworkStruct
{
    [Networked, Capacity(43)] public NetworkArray<Vector3> NetworkSpawnLocation => default;

    public static PlayerBattleSpawnPosition Defaults
    {
        get
        {
            var result = new PlayerBattleSpawnPosition();
            result.NetworkSpawnLocation.Set(0, Vector3.zero);
            return result;
        }
    }
}

public class DedicatedServerManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    //  ====================================

    private event EventHandler PlayerCountChange;
    public event EventHandler OnPlayerCountChange
    {
        add
        {
            if (PlayerCountChange == null || !PlayerCountChange.GetInvocationList().Contains(value))
                PlayerCountChange += value;
        }
        remove { PlayerCountChange -= value; }
    }

    private event EventHandler CurrentStateChange;
    public event EventHandler OnCurrentStateChange
    {
        add
        {
            if (CurrentStateChange == null || !CurrentStateChange.GetInvocationList().Contains(value))
                CurrentStateChange += value;
        }
        remove { CurrentStateChange -= value; }
    }

    private event EventHandler SafeZoneStateChange;
    public event EventHandler OnSafeZoneStateChange
    {
        add
        {
            if (SafeZoneStateChange == null || !SafeZoneStateChange.GetInvocationList().Contains(value))
                SafeZoneStateChange += value;
        }
        remove { SafeZoneStateChange -= value; }
    }

    //  ===================================

    [SerializeField] private int playerRequired;
    [SerializeField] private string lobby;
    [SerializeField] private bool useMultiplay;
    [SerializeField] private MultiplayController multiplayController;

    [Header("SERVER")]
    [SerializeField] private NetworkRunner serverNetworkRunnerPrefab;
    [SerializeField] private List<Transform> spawnWaitingAreaPositions;
    [SerializeField] private List<Transform> spawnBattleAreaPositions;

    [Header("PLAYER")]
    [SerializeField] private NetworkObject playerObj;

    [Header("CRATES")]
    [SerializeField] private NetworkObject createNO;
    [SerializeField] private List<Transform> createSpawnLocations;

    [Header("SPAWN POSITIONS")]
    [SerializeField] private List<Transform> spawnLocationsBattlefield;

    [Header("PLAYING FIELDS")]
    [SerializeField] private GameObject waitingAreaArena;
    [SerializeField] private GameObject battleFieldArena;

    [Header("SAFE ZONE")]
    [SerializeField] private NetworkObject safeZoneNO;
    [SerializeField] private List<Vector3> safeZonePosition;
    [SerializeField] private List<ShrinkSizeList> safeZoneShrink;

    [Header("DEBUGGER")]
    [MyBox.ReadOnly][SerializeField] private string sessionName;
    [MyBox.ReadOnly][SerializeField] private bool doneSpawnCrates;
    [MyBox.ReadOnly][SerializeField] private bool doneSetupBattlePos;
    [MyBox.ReadOnly][SerializeField] private bool doneSetupSafeZone;
    [MyBox.ReadOnly][SerializeField] private NetworkRunner networkRunner;
    
    [field: Space]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public GameState CurrentGameState { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public SafeZoneState CurrentSafeZoneState { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float WaitingAreaTimer { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float SafeZoneTimer { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public bool CanCountWaitingAreaTimer { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public bool DonePlayerBattlePositions { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public bool StartBattleRoyale { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public SafeZoneController SafeZone { get; set; }


    private ChangeDetector _changeDetector;
    [Networked, Capacity(50)] public NetworkDictionary<PlayerRef, NetworkObject> Players => default;
    [Networked, Capacity(50)] public NetworkDictionary<PlayerRef, NetworkObject> RemainingPlayers => default;

    //  =================

    private void Awake()
    {
        if (GameManager.Instance == null)
        {
            StartGame();
        }
    }

    #region GAME INITIALIZE

    Dictionary<string, int> GenerateRandomItems()
    {
        List<string> itemPool = new List<string>
        {
            "rifle", "rifle", "rifle", "rifle", "rifle", // 5%
            "bow", "bow", "bow", "bow", "bow", // 5%
            "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", // 15%
            "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", // 15%
            //"rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo",
            //"rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo",
            //"rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo",
            //"rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", // 60% total ammo, divided into rifle and bow ammo
            //"bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo",
            //"bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo"
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

    private async void SpawnCrates()
    {
        Debug.Log("start spawning crates");

        while (!Runner)
            await Task.Yield();

        int index = 1;

        //foreach (var spawnLocations in createSpawnLocations)
        //{
        //    var gameobject = Runner.Spawn(createNO, spawnLocations.transform.position, Quaternion.identity, null);

        //    gameobject.GetComponent<CrateController>().SetDatas(GenerateRandomItems());

        //    index++;

        //    await Task.Yield();
        //}

        Debug.Log("done for spawn crates");

        doneSpawnCrates = true;
    }

    private async void SetSpawnPositionPlayers()
    {
        await Shuffler.Shuffle(spawnBattleAreaPositions);

        doneSetupBattlePos = true;
    }

    private async void SetSafeZoneArea()
    {
        while (!Runner) await Task.Yield();

        int rand = UnityEngine.Random.Range(0, safeZonePosition.Count);

        SafeZone = Runner.Spawn(safeZoneNO, safeZonePosition[rand], Quaternion.identity, Object.StateAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
        {
            
            obj.GetComponent<SafeZoneController>().safeZoneShrinkSize = safeZoneShrink[rand].shrinksizes;
            obj.GetComponent<SafeZoneController>().SpawnPosition = safeZonePosition[rand];
            obj.GetComponent<SafeZoneController>().ShrinkSizeIndex = 1;
            obj.GetComponent<SafeZoneController>().ServerManager = this;
            obj.GetBehaviour<SafeZoneController>().InitializeSafeZone();

        }).GetComponent<SafeZoneController>();

        doneSetupSafeZone = true;
    }

    #endregion

    #region SERVER INITIALIZE

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


        await networkRunner.StartGame(new StartGameArgs()
        {
            SessionName = sessionName,
            GameMode = GameMode.Server,
            IsVisible = false,
            IsOpen = false,
            SceneManager = networkRunner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
            Scene = networkSceneInfo,
            PlayerCount = 50,
            Address = NetAddress.Any(),
            CustomLobbyName = lobby,
        });

        Debug.Log($"Done Setting up photon server");

        Debug.Log($"Spawning Crates");
        SpawnCrates();

        Debug.Log($"Set Spawn Positions");
        SetSpawnPositionPlayers();

        Debug.Log($"Set Safe Zone");
        SetSafeZoneArea();

        while (!doneSetupBattlePos || !doneSpawnCrates || !doneSetupSafeZone)
        {
            Debug.Log($"Done setup battle pos init: {doneSetupBattlePos} : Done spawn crates init: {doneSpawnCrates}  :  Done Safe Zone Init: {doneSetupSafeZone}");
            await Task.Yield();
        }

        Debug.Log("Adding waiting Area Timer");

        WaitingAreaTimer = 300f;

        Debug.Log("Done adding waiting Area Timer");



#if !UNITY_ANDROID && !UNITY_IOS
        if (useMultiplay)
        {
            Debug.Log("Initializing Multiplay");
            await multiplayController.InitializeUnityAuthentication();
        }
#endif

        networkRunner.SessionInfo.IsOpen = true;
        networkRunner.SessionInfo.IsVisible = true;

        Debug.Log("ALL PLAYERS CAN NOW JOIN");
    }

    #endregion

    public override async void Spawned()
    {
        while (!Runner) await Task.Delay(100);

        Debug.Log("change detector initialized on dedicated server local player");
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(CurrentGameState): 
                    if (CurrentGameState == GameState.ARENA)
                        ArenaEnabler(false, true);

                    CurrentStateChange?.Invoke(this, EventArgs.Empty);
                    break;

                case nameof(RemainingPlayers):

                    if (HasStateAuthority)
                    {
                        if (RemainingPlayers.Count <= 1 && CurrentGameState == GameState.ARENA)
                            CurrentGameState = GameState.DONE;
                    }

                    PlayerCountChange?.Invoke(this, EventArgs.Empty);
                    break;
                case nameof(CurrentSafeZoneState):
                    SafeZoneStateChange?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }


    }

    public override void FixedUpdateNetwork()
    {
        CountDownWaitingAreaTimer();
        BattlePosition();
        CountDownSafeZoneTimer();
    }

    #region GAME LOGIC

    private void CountDownWaitingAreaTimer()
    {
        if (!HasStateAuthority) return;

        if (CurrentGameState != GameState.WAITINGAREA) return;

        if (!CanCountWaitingAreaTimer) return;

        WaitingAreaTimer -= Runner.DeltaTime;


        if (WaitingAreaTimer <= 60f && networkRunner.SessionInfo.IsOpen)
        {
            networkRunner.SessionInfo.IsOpen = false;
            networkRunner.SessionInfo.IsVisible = false;
        }

        if (WaitingAreaTimer <= 0f)
        {
            StartBattleRoyale = true;
            CurrentGameState = GameState.ARENA;
            CanCountWaitingAreaTimer = false;
            CurrentStateChange?.Invoke(this, EventArgs.Empty);
        }
    }

    private void CountDownSafeZoneTimer()
    {
        if (!HasStateAuthority) return;

        if (CurrentSafeZoneState != SafeZoneState.TIMER) return;

        SafeZoneTimer -= Runner.DeltaTime;

        if (SafeZoneTimer <= 0f)
        {
            CurrentSafeZoneState = SafeZoneState.SHRINK;
        }
    }

    private void BattlePosition()
    {
        if (!HasStateAuthority) return;

        if (CurrentGameState != GameState.ARENA) return;

        if (DonePlayerBattlePositions) return;

        ArenaEnabler(false, true);

        bool allPlayersInPosition = true;

        for (int a = 0; a < Players.Count; a++)
        {

            // Set player position
            Players.ElementAt(a).Value.GetComponent<SimpleKCC>().SetPosition(spawnBattleAreaPositions[a].position);
        }

        for (int a = 0; a < Players.Count; a++)
        {
            if (Players.ElementAt(a).Value.transform.position != spawnBattleAreaPositions[a].position)
            {
                allPlayersInPosition = false;
                break;
            }
        }

        if (allPlayersInPosition)
            DonePlayerBattlePositions = true;

        SafeZoneTimer = 150f;
        CurrentSafeZoneState = SafeZoneState.TIMER;
    }

    private void ArenaEnabler(bool waitingArea, bool battleField)
    {
        waitingAreaArena.SetActive(waitingArea);
        battleFieldArena.SetActive(battleField);
    }

    #endregion

    #region SERVER LOGIC

    public void PlayerJoined(PlayerRef player)
    {
        if (HasStateAuthority)
        {
            int tempspawnpos = UnityEngine.Random.Range(0, spawnWaitingAreaPositions.Count);

            NetworkObject playerCharacter = Runner.Spawn(playerObj, Vector3.up, Quaternion.identity, player, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
            {
                obj.GetComponent<SimpleKCC>().SetPosition(spawnWaitingAreaPositions[tempspawnpos].position);
                obj.GetComponent<KillCountCounterController>().ServerManager = this;
                obj.GetComponent<WaitingAreaTimerController>().ServerManager = this;
                obj.GetComponent<PlayerHealth>().ServerManager = this;
                obj.GetComponent<PlayerSpawnLocationController>().ServerManager = this;
                obj.GetComponent<PlayerQuitController>().ServerManager = this;
                obj.GetComponent<MapZoomInOut>().ServerManager = this;
                obj.GetComponent<PlayerGameOverScreen>().ServerManager = this;
                obj.GetComponent<PlayerShrinkZoneTimer>().ServerManager = this;
            });

            Players.Add(player, playerCharacter);
            RemainingPlayers.Add(player, playerCharacter);
            PlayerCountChange?.Invoke(this, EventArgs.Empty);

            if (!CanCountWaitingAreaTimer && Players.Count >= playerRequired)
            {
                CanCountWaitingAreaTimer = true;
            }

            if (CanCountWaitingAreaTimer && WaitingAreaTimer > 60 && Players.Count >= Players.Capacity)
                WaitingAreaTimer = 60;
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority) return;

        if (Players.TryGet(player, out NetworkObject remainingPlayer))
        {
            RemainingPlayers.Remove(player);
        }

        if (Players.TryGet(player, out NetworkObject clientPlayer))
        {
            Players.Remove(player);
            Runner.Despawn(clientPlayer);

            if (Players.Count <= 0 && CanCountWaitingAreaTimer)
            {
                WaitingAreaTimer = 300f;
                CanCountWaitingAreaTimer = false;
            }

            if (CurrentGameState == GameState.DONE && Players.Count <= 0)
                Application.Quit();
        }

        

        PlayerCountChange?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}

[System.Serializable]
public class ShrinkSizeList
{
    public List<Vector3> shrinksizes;
}