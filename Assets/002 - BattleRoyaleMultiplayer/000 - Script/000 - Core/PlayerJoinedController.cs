using Fusion;
using Fusion.Addons.SimpleKCC;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerJoinedController : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    public static PlayerJoinedController Instance { get; private set; }

    //  ==========================

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

    public bool DoneSetupPlayers { get => doneSetupPlayers; }

    public List<Transform> SpawnWaitingAreaPositions { get => spawnWaitingAreaPositions; set => spawnWaitingAreaPositions = value; }

    //  ===========================

    [Space]
    [SerializeField] private NetworkObject playerObj;

    [Header("BOTS (dedicated server)")]
    [SerializeField] private NetworkObject botPrefab;

    [Space]
    [SerializeField] private NetworkObject healObj;
    [SerializeField] private NetworkObject armorObj;
    [SerializeField] private NetworkObject rifleAmmoObj;

    [Header("DEBUGGER")]
    [SerializeField] private List<Transform> spawnWaitingAreaPositions;
    [SerializeField] private bool doneSetupPlayers;

    //  ===========================

    [Networked, Capacity(30)] public NetworkDictionary<string, NetworkObject> Players => default;
    [Networked, Capacity(30)] public NetworkDictionary<string, NetworkObject> RemainingPlayers => default;
    [Networked, Capacity(50)] public NetworkDictionary<int, NetworkObject> Bots => default;

    public Dictionary<string, PlayerOwnObjectEnabler> ConnectedPlayers = new Dictionary<string, PlayerOwnObjectEnabler>();

    public Dictionary<PlayerRef, string> playerIdMap = new Dictionary<PlayerRef, string>();

    public List<string> PlayerIDs = new List<string>();

    private ChangeDetector _changeDetector;

    //  ===========================

    private void Awake()
    {
        Instance = this;
    }

    public override async void Spawned()
    {
        while (!Runner) await Task.Delay(100);

        Debug.Log("change detector initialized on dedicated server local player");
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void FixedUpdateNetwork()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(RemainingPlayers):
                case nameof(Bots):

                    if (HasStateAuthority)
                    {
                        TrySetGameStateDoneIfNoHumansLeft();
                    }

                    PlayerCountChange?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }

    private void TrySetGameStateDoneIfNoHumansLeft()
    {
        if (!HasStateAuthority) return;
        if (MultiplayerServerManager.Instance == null) return;
        if (MultiplayerServerManager.Instance.CurrentGameState != GameState.ARENA) return;

        // End the match once only bots remain, or one/no human remains with no bots alive.
        bool onlyBotsRemaining = RemainingPlayers.Count <= 0 && Bots.Count > 0;
        bool normalLastStanding = RemainingPlayers.Count <= 1 && Bots.Count <= 0;

        if (onlyBotsRemaining || normalLastStanding)
            MultiplayerServerManager.Instance.CurrentGameState = GameState.DONE;
    }

    public async void SpawnPlayers(string playerdata)
    {
        await SpawnMatchPopulationAsync(playerdata, "[]");
    }

    public async void SpawnMatchPopulation(string playerCostumeJson, string botCostumeJson)
    {
        await SpawnMatchPopulationAsync(playerCostumeJson, botCostumeJson);
    }

    public void RemoveBot(int botIndex)
    {
        if (!HasStateAuthority) return;
        Bots.Remove(botIndex);
    }

    private async Task SpawnMatchPopulationAsync(string playerCostumeJson, string botCostumeJson)
    {
        while (!Runner)
            await Task.Yield();

        Debug.Log($"SPAWNING MATCH POPULATION | players JSON len {playerCostumeJson?.Length ?? 0}, bot JSON len {botCostumeJson?.Length ?? 0}");

        List<PlayerSpawnData> tempdata = null;
        try
        {
            tempdata = JsonConvert.DeserializeObject<List<PlayerSpawnData>>(playerCostumeJson);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"playercostumedata parse failed, using debug fallback: {ex.Message}");
        }

        if (tempdata == null || tempdata.Count == 0)
        {
            tempdata = new List<PlayerSpawnData>
            {
                new PlayerSpawnData
                {
                    username = "DEBUG_Player",
                    ownerId = "debug-local",
                    _id = "debug-local",
                    avatarid = 0,
                    hairstyle = 0,
                    haircolor = 0,
                    clothingcolor = 0,
                    skincolor = 0
                }
            };
        }

        int spawnpos = 0;

        foreach (var data in tempdata)
        {
            int pos = spawnpos;
            string resolvedUserId = !string.IsNullOrEmpty(data.ownerId)
                ? data.ownerId
                : !string.IsNullOrEmpty(data._id) ? data._id : data.username;

            NetworkObject playerCharacter = Runner.Spawn(playerObj, spawnWaitingAreaPositions[pos].position, Quaternion.identity, PlayerRef.None, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
            {
                obj.GetComponent<SimpleKCC>().SetPosition(spawnWaitingAreaPositions[pos].position);

                PlayerOwnObjectEnabler core = obj.GetComponent<PlayerOwnObjectEnabler>();

                core.Username = data.username;
                core.UserID = resolvedUserId;
            });

            Debug.Log($"Does server have input authority to {data.username}? {playerCharacter.HasInputAuthority}");

            PlayerInventoryV2 tempinventory = playerCharacter.GetComponent<PlayerInventoryV2>();

            tempinventory.HairStyle = data.hairstyle;
            tempinventory.HairColorIndex = data.haircolor;
            tempinventory.ClothingColorIndex = data.clothingcolor;
            tempinventory.SkinColorIndex = data.skincolor;

            tempinventory.WeaponIndex = 1;

            tempinventory.IsSkinInitialized = true;

            if (!string.IsNullOrEmpty(data.username))
                ConnectedPlayers[data.username] = playerCharacter.GetComponent<PlayerOwnObjectEnabler>();

            Players.Add(data.username, playerCharacter);
            RemainingPlayers.Add(data.username, playerCharacter);

            spawnpos++;

            PlayerCountChange?.Invoke(this, EventArgs.Empty);

            await Task.Yield();
        }

        List<BotSpawnData> botList = null;
        try
        {
            botList = JsonConvert.DeserializeObject<List<BotSpawnData>>(string.IsNullOrWhiteSpace(botCostumeJson) ? "[]" : botCostumeJson);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"botcostumedata parse failed, spawning no bots: {ex.Message}");
            botList = new List<BotSpawnData>();
        }

        botList ??= new List<BotSpawnData>();
        int botCount = botList.Count;

        if (botCount > 0)
        {
            if (botPrefab == null)
            {
                Debug.LogError("Bot prefab is not assigned on PlayerJoinedController; skipping bot spawn.");
            }
            else
            {
                for (int i = 0; i < botCount; i++)
                {
                    int slot = spawnpos + i;
                    Transform tr = spawnWaitingAreaPositions[slot % spawnWaitingAreaPositions.Count];
                    BotSpawnData bd = botList[i];
                    int botIndex = i;

                    NetworkObject botCharacter = Runner.Spawn(botPrefab, tr.position, Quaternion.identity, PlayerRef.None, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                    {
                        var kcc = obj.GetComponent<SimpleKCC>();
                        if (kcc != null)
                            kcc.SetPosition(tr.position);

                        Botdata botdata = obj.GetComponent<Botdata>();
                        BotInventory inv = obj.GetComponent<BotInventory>();
                        string name = !string.IsNullOrEmpty(bd.username) ? bd.username : $"BOT_{botIndex}";
                        botdata.BotName = name;
                        botdata.BotIndex = botIndex;
                        inv.ApplyCostumeFromMatchData(bd.hairstyle, bd.haircolor, bd.clothingcolor, bd.skincolor);
                        inv.WeaponIndex = 1;
                    });

                    Bots.Add(botIndex, botCharacter);

                    await Task.Yield();
                }
            }
        }

        doneSetupPlayers = true;
    }

    string GetPlayerId(NetworkRunner runner, PlayerRef player)
    {
        if (runner.GetPlayerConnectionToken(player) is byte[] tokenBytes)
            return System.Text.Encoding.UTF8.GetString(tokenBytes);
        return null;
    }

    public bool TryGetPlayerRefFromUsername(string username, out PlayerRef playerRef)
    {
        Debug.Log($"TOTAL PLAYER ID MAPS: {playerIdMap.Count}");

        foreach (var pair in playerIdMap)
        {
            Debug.Log($"PLAYER ID MAP, KEY: {pair.Key}    VALUE: {pair.Value}");
            if (pair.Value == username)
            {
                playerRef = pair.Key;
                return true;
            }
        }

        playerRef = default;
        return false;
    }

    public void RemovePlayerByUsername(string username)
    {
        if (!HasStateAuthority)
            return;

        if (!TryGetPlayerRefFromUsername(username, out PlayerRef playerRef))
        {
            Debug.LogWarning($"Cannot remove player — username not found: {username}");
            return;
        }

        Debug.Log($"[Kick/Quit] Removing player: {username}");

        if (Players.TryGet(username, out NetworkObject remainingPlayer))
        {
            Debug.Log("trying get network object in players");

            remainingPlayer.GetComponent<PlayerOwnObjectEnabler>().Removing = true;

            Debug.Log("activating removing");

            PlayerLeftLocal(playerRef, username); // Call your existing logic
        }
    }

    private void PlayerLeftLocal(PlayerRef player, string username)
    {
        if (!HasStateAuthority) return;

        if (Players.TryGet(username, out NetworkObject remainingPlayer))
        {
            PlayerOwnObjectEnabler core = remainingPlayer.GetComponent<PlayerOwnObjectEnabler>();

            if (core.Removing)
                RemainingPlayers.Remove(username);
        }

        if (Players.TryGet(username, out NetworkObject clientPlayer))
        {
            PlayerOwnObjectEnabler core = remainingPlayer.GetComponent<PlayerOwnObjectEnabler>();

            if (core.Removing)
            {
                RemainingPlayers.Remove(username);

                var playerinventory = remainingPlayer.GetComponent<PlayerInventoryV2>();

                if (playerinventory.PrimaryWeapon != null) playerinventory.PrimaryWeapon.DropWeapon();

                if (playerinventory.SecondaryWeapon != null) playerinventory.SecondaryWeapon.DropWeapon();

                if (playerinventory.Armor != null) playerinventory.Armor.DropArmor();

                if (playerinventory.BowMagazine > 0)
                {
                    Runner.Spawn(healObj, playerinventory.transform.position, Quaternion.identity, null, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                    {
                        obj.GetComponent<MagazineContainerItem>().InitializeOnStart(playerinventory.transform.position, Quaternion.identity, playerinventory.HealCount);
                    });
                }

                if (playerinventory.HealCount > 0)
                {
                    Runner.Spawn(healObj, playerinventory.transform.position, Quaternion.identity, null, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                    {
                        obj.GetComponent<HealWeaponItem>().InitializeItem(playerinventory.transform.position, Quaternion.identity, playerinventory.HealCount);
                    });
                }

                if (playerinventory.ArmorRepairCount > 0)
                {
                    Runner.Spawn(armorObj, playerinventory.transform.position, Quaternion.identity, null, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                    {
                        obj.GetComponent<RepairWeaponItem>().InitializeItem(playerinventory.transform.position, Quaternion.identity, playerinventory.HealCount);
                    });
                }

                Players.Remove(username);

                //Debug.Log($"activating removing 10   player obj null? {remainingPlayer == null}");

                if (core.Object.InputAuthority == PlayerRef.None)
                    Runner.Despawn(clientPlayer);

                PlayerIDs.Remove(username);
                playerIdMap.Remove(player);
                ConnectedPlayers.Remove(username);

                if (Players.Count <= 0)
                {
                    if (MultiplayerServerManager.Instance.CurrentGameState != GameState.DONE)
                        MultiplayerServerManager.Instance.CurrentGameState = GameState.DONE;
                }

                TrySetGameStateDoneIfNoHumansLeft();
            }
            else
                core.Object.AssignInputAuthority(Runner.LocalPlayer);
        }

        PlayerCountChange?.Invoke(this, EventArgs.Empty);
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (HasStateAuthority)
        {
            string playerId = GetPlayerId(Runner, player);
            Debug.Log($"Player joined with ID: {playerId}");

            if (!PlayerIDs.Contains(playerId))
            {
                Debug.Log($"ADDING PLAYER ID TO LIST {playerId}");
                PlayerIDs.Add(playerId);
            }

            // ?? Check if this player already exists (reconnecting)
            if (!string.IsNullOrEmpty(playerId) && ConnectedPlayers.TryGetValue(playerId, out var existingCore))
            {
                //PlayerDisconnected = false;

                if (!string.IsNullOrEmpty(playerId))
                {
                    if (playerIdMap.ContainsValue(playerId))
                        playerIdMap[player] = playerId;
                    else
                        playerIdMap.Add(player, playerId);
                }

                // Restore their PlayerRef
                existingCore.Object.AssignInputAuthority(player);

                if (MultiplayerServerManager.Instance.CurrentGameState == GameState.WAITINGPLAYERS)
                {
                    MultiplayerServerManager.Instance.CurrentGameState = GameState.ARENA;

                    MultiplayerServerManager.Instance.DonePlayerBattlePositions = true;
                    SafeZoneServerController.Instance.SafeZoneTimer = DedicatedServerManager.Instance.SafeZoneTimeToShrink;
                    SafeZoneServerController.Instance.CurrentSafeZoneState = SafeZoneState.TIMER;

                    if (DedicatedServerManager.Instance.PrivateServer)
                        MultiplayerServerManager.Instance.ChangeServerStatus();
                }


                Debug.Log($"?? Player {playerId} reconnected, reassigned authority.");

                return;
            }
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority) return;

        //string playerId = GetPlayerId(Runner, player);

        playerIdMap.TryGetValue(player, out var playerId);

        Debug.Log($"left player: {playerId}");

        if (Players.TryGet(playerId, out NetworkObject remainingPlayer))
        {
            PlayerOwnObjectEnabler core = remainingPlayer.GetComponent<PlayerOwnObjectEnabler>();

            if (core.Removing || core.PlayerHealth.IsDead)
            {
                RemainingPlayers.Remove(playerId);
                SocketServerController.Instance.Socket.Emit("serverremovereconnect", JsonConvert.SerializeObject(
                    new Dictionary<string, string>() { { "username", playerId } }
                ));

                RemainingPlayers.Remove(playerId);

                var inv = remainingPlayer.GetComponent<PlayerInventoryV2>();
                if (inv != null)
                {
                    if (inv.PrimaryWeapon != null) inv.PrimaryWeapon.DropWeapon();

                    if (inv.SecondaryWeapon != null) inv.SecondaryWeapon.DropWeapon();

                    if (inv.Armor != null) inv.Armor.DropArmor();

                    if (inv.BowMagazine > 0) inv.MagazineContainer.DropWeapon();

                    if (inv.RifleMagazine > 0)
                    {
                        Runner.Spawn(rifleAmmoObj, inv.transform.position, Quaternion.identity, PlayerRef.None, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                        {
                            obj.GetComponent<AmmoRifleWeaponItem>().InitializeOnStart(transform.position, Quaternion.identity, inv.RifleMagazine);
                        });
                    }

                    if (inv.HealCount > 0)
                    {
                        Runner.Spawn(healObj, inv.transform.position, Quaternion.identity, PlayerRef.None, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                        {
                            obj.GetComponent<HealWeaponItem>().InitializeItem(inv.transform.position, Quaternion.identity, inv.HealCount);
                        });
                    }

                    if (inv.ArmorRepairCount > 0)
                    {
                        Runner.Spawn(armorObj, inv.transform.position, Quaternion.identity, PlayerRef.None, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                        {
                            obj.GetComponent<RepairWeaponItem>().InitializeItem(inv.transform.position, Quaternion.identity, inv.HealCount);
                        });
                    }
                }

                Players.Remove(playerId);

                PlayerCountChange?.Invoke(this, EventArgs.Empty);

                if (Players.Count <= 0)
                {
                    if (MultiplayerServerManager.Instance.CurrentGameState != GameState.DONE)
                        MultiplayerServerManager.Instance.CurrentGameState = GameState.DONE;
                }

                TrySetGameStateDoneIfNoHumansLeft();
            }
            //else
            //    core.Object.AssignInputAuthority(PlayerRef.None);
        }
    }
}
