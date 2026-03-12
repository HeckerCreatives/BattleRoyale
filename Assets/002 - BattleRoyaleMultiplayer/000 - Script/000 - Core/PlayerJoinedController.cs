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

    [Header("DEBUGGER")]
    [SerializeField] private List<Transform> spawnWaitingAreaPositions;
    [SerializeField] private bool doneSetupPlayers;

    //  ===========================

    [Networked, Capacity(50)] public NetworkDictionary<string, NetworkObject> Players => default;
    [Networked, Capacity(50)] public NetworkDictionary<string, NetworkObject> RemainingPlayers => default;



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
                case nameof(BotSpawnerController.Instance.Bots):

                    if (HasStateAuthority)
                    {
                        if (RemainingPlayers.Count <= 1 && BotSpawnerController.Instance.Bots.Count <= 0 && MultiplayerServerManager.Instance.CurrentGameState == GameState.ARENA)
                            MultiplayerServerManager.Instance.CurrentGameState = GameState.DONE;
                    }

                    PlayerCountChange?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }

    public async void SpawnPlayers(string playerdata)
    {
        while (!Runner)
            await Task.Yield();

        Debug.Log($"SPAWNING PLAYERS WITH DATAS {playerdata}");

        List<PlayerSpawnData> tempdata = JsonConvert.DeserializeObject<List<PlayerSpawnData>>(playerdata);

        int spawnpos = 0;

        foreach (var data in tempdata)
        {
            NetworkObject playerCharacter = Runner.Spawn(playerObj, spawnWaitingAreaPositions[spawnpos].position, Quaternion.identity, PlayerRef.None, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
            {
                obj.GetComponent<SimpleKCC>().SetPosition(spawnWaitingAreaPositions[spawnpos].position);

                PlayerOwnObjectEnabler core = obj.GetComponent<PlayerOwnObjectEnabler>();

                core.Username = data.username;
                core.UserID = data.ownerid;
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

                if (playerinventory.MagazineContainer != null) playerinventory.MagazineContainer.DropWeapon();

                Players.Remove(username);

                //Debug.Log($"activating removing 10   player obj null? {remainingPlayer == null}");

                if (core.Object.InputAuthority == PlayerRef.None)
                    Runner.Despawn(clientPlayer);

                PlayerIDs.Remove(username);
                playerIdMap.Remove(player);
                ConnectedPlayers.Remove(username);

                if (Players.Count <= 0)
                {
                    Application.Quit();
                }
            }
            else
                core.Object.AssignInputAuthority(Runner.LocalPlayer);
        }

        if ((MultiplayerServerManager.Instance.CurrentGameState == GameState.DONE || MultiplayerServerManager.Instance.CurrentGameState == GameState.ARENA) && Players.Count <= 0)
            Application.Quit();

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
                RemainingPlayers.Remove(playerId);
            else
                core.Object.AssignInputAuthority(PlayerRef.None);
        }

        if (Players.TryGet(playerId, out NetworkObject clientPlayer))
        {
            PlayerOwnObjectEnabler core = clientPlayer.GetComponent<PlayerOwnObjectEnabler>();
            if (core != null && (core.Removing || core.PlayerHealth.IsDead))
            {
                RemainingPlayers.Remove(playerId);

                var inv = clientPlayer.GetComponent<PlayerInventoryV2>();
                if (inv != null)
                {
                    if (inv.PrimaryWeapon != null) inv.PrimaryWeapon.DropWeapon();
                    if (inv.SecondaryWeapon != null) inv.SecondaryWeapon.DropWeapon();
                    if (inv.Armor != null) inv.Armor.DropArmor();
                    if (inv.MagazineContainer != null) inv.MagazineContainer.DropWeapon();
                }

                Players.Remove(playerId);

                if (Players.Count <= 0)
                    Application.Quit();
            }
        }


        if (Players.Count <= 0)
            Application.Quit();

        PlayerCountChange?.Invoke(this, EventArgs.Empty);
    }
}
