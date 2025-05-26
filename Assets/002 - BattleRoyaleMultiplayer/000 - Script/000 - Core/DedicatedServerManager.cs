using Fusion;
using Fusion.Addons.SimpleKCC;
using Fusion.Photon.Realtime;
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

public enum WaitingAreaTimerState
{
    WAITING,
    GETREADY
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

    [SerializeField] private float waitingAreaStartTimer;
    [SerializeField] private int playerRequired;
    [SerializeField] private int maxPlayers;
    [SerializeField] private string lobby;
    [SerializeField] private bool useMultiplay;
    [SerializeField] private MultiplayController multiplayController;

    [Header("SERVER")]
    [SerializeField] private NetworkRunner serverNetworkRunnerPrefab;
    [SerializeField] private List<Transform> spawnWaitingAreaPositions;
    [SerializeField] private List<Transform> spawnBattleAreaPositions;

    [Header("PLAYER")]
    [SerializeField] private NetworkObject playerObj;
    [SerializeField] private NetworkObject bulletNO;
    [SerializeField] private NetworkObject arrowNO;

    [Header("CRATES")]
    [SerializeField] private NetworkObject createNO;
    [SerializeField] private List<Transform> createSpawnLocations;

    [Header("SPAWN POSITIONS")]
    [SerializeField] private List<Transform> spawnLocationsBattlefield;

    [Header("PLAYING FIELDS")]
    [SerializeField] private GameObject waitingAreaArenaObjects;
    [SerializeField] private GameObject battleFieldArenaObjects;
    public Terrain waitingAreaArena;
    public Terrain battleFieldArena;
    [SerializeField] private float waitTimerForReadyAfterBattlePos;

    [Header("SAFE ZONE")]
    [SerializeField] private NetworkObject safeZoneNO;
    [SerializeField] private List<Vector3> safeZonePosition;
    [SerializeField] private List<ShrinkSizeList> safeZoneShrink;

    [Header("DEBUGGER")]
    [MyBox.ReadOnly][SerializeField] private bool doneSpawnCrates;
    [MyBox.ReadOnly][SerializeField] private bool doneSetupBattlePos;
    [MyBox.ReadOnly][SerializeField] private bool doneSetupSafeZone;
    [MyBox.ReadOnly][SerializeField] private NetworkRunner networkRunner;
    
    [field: Space]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public GameState CurrentGameState { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public SafeZoneState CurrentSafeZoneState { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public WaitingAreaTimerState CurrentWaitingAreaTimerState { get; set; }
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
        else
        {
            GameManager.Instance.AudioController.StopBGMusic();
        }
    }

    #region GAME INITIALIZE

    Dictionary<string, int> GenerateRandomItems()
    {
        List<string> itemPool = new List<string>
        {
            "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", "rifle", // 15%
            "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", "bow", // 15%
            "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", "sword", // 20%
            "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", "spear", // 20%
            "heal", "heal", "heal", "heal", "heal", "heal", "heal", "heal", "heal", "heal", // 10%
            "repair armor", "repair armor", "repair armor", "repair armor", "repair armor", "repair armor", "repair armor", "repair armor", "repair armor", "repair armor", // 10%
            "armor", "armor", "armor", "armor", "armor", "armor", "armor", "armor", "armor", "armor", // 10%
            "rifle ammo", "rifle ammo", "rifle ammo", "rifle ammo", // 
            "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", "bow ammo", // 10%
            "trap", "trap", "trap", "trap", "trap", "trap", "trap", "trap", "trap", "trap", // 10%
        };


        Dictionary<string, string> itemIDMap = new Dictionary<string, string>
        {
            { "sword", "001" },
            { "spear", "002" },
            { "rifle", "003" },
            { "bow", "004" },
            { "rifle ammo", "005" },
            { "bow ammo", "006" },
            { "armor", "007" },
            { "heal", "008" },
            { "repair armor", "009" },
            { "trap", "010" }
        };

        Dictionary<string, int> selectedItems = new Dictionary<string, int>();

        int itemListQTY = UnityEngine.Random.Range(1, 11);

        for (int i = 0; i < itemListQTY; i++)
        {
            string selectedItem = itemPool[UnityEngine.Random.Range(0, itemPool.Count)];
            string itemID = itemIDMap[selectedItem];

            if (selectedItem == "rifle ammo")
            {
                if (!selectedItems.ContainsKey(itemID))
                    selectedItems[itemID] = UnityEngine.Random.Range(10, 21); // Add a random quantity between 1 and 60
            }
            else if (selectedItem == "bow ammo")
            {
                if (!selectedItems.ContainsKey(itemID))
                    selectedItems[itemID] = UnityEngine.Random.Range(5, 16); // Add a random quantity between 1 and 60
            }
            else if (selectedItem == "rifle")
            {
                selectedItems[itemID] = 10;
            }
            else if (selectedItem == "bow")
            {
                selectedItems[itemID] = 5;
            }
            else if (selectedItem == "armor")
            {
                selectedItems[itemID] = 100;
            }
            else if (selectedItem == "heal" || selectedItem == "repair armor")
            {
                selectedItems[itemID] = 1;
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

        foreach (var spawnLocations in createSpawnLocations)
        {
            var gameobject = Runner.Spawn(createNO, spawnLocations.transform.position, Quaternion.identity, null);

            gameobject.GetComponent<CrateController>().SetDatas(GenerateRandomItems());

            index++;

            await Task.Yield();
        }

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

        if (args.TryGetValue("region", out string region))
            appSettings = BuildCustomAppSetting(region);
        else
            appSettings = BuildCustomAppSetting("asia");

        Debug.Log($"STARTING REGION: {appSettings.FixedRegion}");

        await networkRunner.StartGame(new StartGameArgs()
        {
            SessionName = Guid.NewGuid().ToString(),
            GameMode = GameMode.Server,
            IsVisible = false,
            IsOpen = false,
            SceneManager = networkRunner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
            Scene = networkSceneInfo,
            PlayerCount = maxPlayers,
            Address = NetAddress.Any(),
            CustomLobbyName = lobby,
            CustomPhotonAppSettings = appSettings
        });

        if (networkRunner.IsRunning)
        {
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

            WaitingAreaTimer = waitingAreaStartTimer;

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
        CountDownSafeZoneTimer();
    }

    #region GAME LOGIC

    private void CountDownWaitingAreaTimer()
    {
        if (!HasStateAuthority) return;

        if (CurrentGameState != GameState.WAITINGAREA) return;

        if (!CanCountWaitingAreaTimer) return;

        WaitingAreaTimer -= Runner.DeltaTime;

        if (CurrentWaitingAreaTimerState == WaitingAreaTimerState.WAITING)
        {
            if (WaitingAreaTimer <= 0f)
            {
                WaitingAreaTimer = 30f;
                CurrentWaitingAreaTimerState = WaitingAreaTimerState.GETREADY;
            }
        }
        else if (CurrentWaitingAreaTimerState == WaitingAreaTimerState.GETREADY)
        {
            if (WaitingAreaTimer <= 30f && networkRunner.SessionInfo.IsOpen)
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

                StartCoroutine(BattlePosition());
            }
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

    private IEnumerator BattlePosition()
    {
        if (!HasStateAuthority) yield break;

        if (CurrentGameState != GameState.ARENA) yield break;

        if (DonePlayerBattlePositions) yield break;

        ArenaEnabler(false, true);

        bool isDone = false;

        while (!isDone)
        {
            for (int a = 0; a < Players.Count; a++)
            {
                // Set player position

                if (Players.ElementAt(a).Value.transform.position.x != spawnBattleAreaPositions[a].position.x && Players.ElementAt(a).Value.transform.position.z != spawnBattleAreaPositions[a].position.z)
                    Players.ElementAt(a).Value.GetComponent<SimpleKCC>().SetPosition(spawnBattleAreaPositions[a].position);
            }

            for (int a = 0; a < Players.Count; a++)
            {
                // Set player position

                if (Players.ElementAt(a).Value.transform.position.x != spawnBattleAreaPositions[a].position.x && Players.ElementAt(a).Value.transform.position.z != spawnBattleAreaPositions[a].position.z)
                    break;

                if (a == Players.Count - 1) isDone = true;
            }
        }

        yield return new WaitForSeconds(5f);

        //  WAIT 5 SECONDS HERE

        DonePlayerBattlePositions = true;

        SafeZoneTimer = 100f;
        CurrentSafeZoneState = SafeZoneState.TIMER;
    }

    private void ArenaEnabler(bool waitingArea, bool battleField)
    {
        waitingAreaArenaObjects.SetActive(waitingArea);
        battleFieldArenaObjects.SetActive(battleField);
    }

    #endregion

    #region SERVER LOGIC

    public void PlayerJoined(PlayerRef player)
    {
        if (HasStateAuthority)
        {
            //  OLD SPAWN BULLET
            //Runner.Spawn(bulletNO, playerInventory.SecondaryWeapon.impactPoint.position, Quaternion.LookRotation(aimDir, Vector3.up), onBeforeSpawned: (NetworkRunner runner, NetworkObject nobject) =>
            //{
            //    nobject.GetComponent<BulletController>().Fire(Object.InputAuthority, playerInventory.SecondaryWeapon.impactPoint.position, mainCorePlayable.Object, hit, playerNetworkLoader.Username);
            //});

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
                obj.GetComponent<MainCorePlayable>().ServerManager = this;
            });

            BulletObjectPool temppool = playerCharacter.GetComponent<BulletObjectPool>();

            for (int a = 0; a < 15; a++)
            {
                temppool.BulletQueue.Add(Runner.Spawn(bulletNO, Vector2.zero, Quaternion.identity, playerCharacter.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                {
                    temppool.TempBullets.Add(obj);
                    obj.GetComponent<BulletController>().PoolIndex = a;
                    obj.GetComponent<BulletController>().Pooler = temppool;
                }), false);
                temppool.ArrowQueue.Add(Runner.Spawn(arrowNO, Vector2.zero, Quaternion.identity, playerCharacter.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                {
                    temppool.TempArrows.Add(obj);
                    obj.GetComponent<ArrowController>().PoolIndex = a;
                    obj.GetComponent<ArrowController>().Pooler = temppool;
                }), false);
            }

            playerCharacter.GetComponent<BulletObjectPool>().DoneInitialize = true;

            Players.Add(player, playerCharacter);
            RemainingPlayers.Add(player, playerCharacter);
            PlayerCountChange?.Invoke(this, EventArgs.Empty);

            if (Players.Count >= 1 && !CanCountWaitingAreaTimer)
            {
                CanCountWaitingAreaTimer = true;
            }

            if (CanCountWaitingAreaTimer && WaitingAreaTimer > 60 && Players.Count >= Players.Capacity && CurrentWaitingAreaTimerState == WaitingAreaTimerState.WAITING)
            {
                CurrentWaitingAreaTimerState = WaitingAreaTimerState.GETREADY;
                WaitingAreaTimer = 30;
            }
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
            var playerinventory = clientPlayer.GetComponent<PlayerInventory>();

            if (playerinventory.PrimaryWeapon != null) playerinventory.PrimaryWeapon.DropPrimaryWeapon();

            if (playerinventory.SecondaryWeapon != null)
            {
                if (playerinventory.SecondaryWeapon.WeaponID == "003") playerinventory.SecondaryWeapon.DropSecondaryWeapon();
                else if (playerinventory.SecondaryWeapon.WeaponID == "004") playerinventory.SecondaryWeapon.DropSecondaryWithAmmoCaseWeapon();
            }

            if (playerinventory.Shield != null) playerinventory.Shield.DropShield();

            var bulletPooler = clientPlayer.GetComponent<BulletObjectPool>();

            for (int a = 0; a < bulletPooler.BulletQueue.Count; a++)
            {
                Runner.Despawn(bulletPooler.BulletQueue.ElementAt(a).Key);
            }

            for (int a = 0; a < bulletPooler.ArrowQueue.Count; a++)
            {
                Runner.Despawn(bulletPooler.ArrowQueue.ElementAt(a).Key);
            }

            Players.Remove(player);
            Runner.Despawn(clientPlayer);

            if (Players.Count <= 0 && CanCountWaitingAreaTimer)
            {
                WaitingAreaTimer = waitingAreaStartTimer;
                CanCountWaitingAreaTimer = false;
                CurrentWaitingAreaTimerState = WaitingAreaTimerState.WAITING;
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