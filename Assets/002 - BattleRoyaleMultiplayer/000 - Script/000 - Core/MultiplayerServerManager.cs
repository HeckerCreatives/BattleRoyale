using CandyCoded.env;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using Newtonsoft.Json;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using static Fusion.NetworkBehaviour;

public enum GameState
{
    WAITINGPLAYERS,
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

public class MultiplayerServerManager : NetworkBehaviour
{
    public static MultiplayerServerManager Instance { get; private set; }

    //  =========================

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

    public bool DoneSetupBattlePos { get => doneSetupBattlePos; }

    //  =========================

    [SerializeField] private DeveloperConsole developerConsole;

    [Space]
    public WeaponCratesSpawnerController weaponCratesSpawner;
    public SafeZoneServerController safeZoneServerController;
    public PlayerJoinedController playerJoinedController;
    public SocketServerController socketServer;
    public BotSpawnerController botSpawnerController;
    public KillNotifServerController killNotifServerController;


    [Space]
    [SerializeField] private List<Transform> spawnBattleAreaPositions;

    [Space]
    [SerializeField] private List<GameObject> soundsObjs;

    [Header("DEBUGGER")]
    [SerializeField] private string sessionname;
    [SerializeField] private bool canStartCountDownToCloseServer;
    [SerializeField] private float closeServerCount;
    [SerializeField] private bool doneSetupBattlePos;

    [field: Header("NETWORK DEBUGGER")]
    [field: SerializeField][Networked] public GameState CurrentGameState { get; set; }
    [field: SerializeField][Networked] public bool DonePlayerBattlePositions { get; set; }

    private ChangeDetector _changeDetector;

    //  =========================

    public override async void Spawned()
    {
        while (!Runner) await Task.Delay(100);

        Debug.Log("change detector initialized on dedicated server local player");
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    private async void Awake()
    {
        Instance = this;

        if (GameManager.Instance != null)
            GameManager.Instance.AudioController.StopBGMusic();
    }

    public async void StartServer()
    {
        socketServer.InitializeSocket();

        for (int a = 0; a < soundsObjs.Count; a++)
            soundsObjs[a].SetActive(false);

        await botSpawnerController.CBSBotNames.Shuffle();
        await botSpawnerController.BotNames.Shuffle();

        developerConsole.doShow = true;
    }

    private void Update()
    {
        if (canStartCountDownToCloseServer)
        {
            if (closeServerCount <= 0)
            {
                Application.Quit();

                return;
            }

            closeServerCount -= Time.deltaTime;
        }
    }

    public override void FixedUpdateNetwork()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(CurrentGameState):
                    if (CurrentGameState == GameState.DONE)
                    {

                        socketServer.Socket.Emit("doneroom", JsonConvert.SerializeObject(new Dictionary<string, string>
                        {
                            { "sessioname", sessionname }
                        }));
                    }

                    CurrentStateChange?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }

    

    public async void SetSpawnPositionPlayers()
    {
        await Shuffler.Shuffle(spawnBattleAreaPositions);

        doneSetupBattlePos = true;
    }

    public void ChangeServerStatus()
    {
        if ((!doneSetupBattlePos || !weaponCratesSpawner.DoneSpawnCrates || !safeZoneServerController.DoneSetupSafeZone)) return;


        switch (CurrentGameState)
        {
            case GameState.WAITINGPLAYERS:

                socketServer.Socket.Emit("changematchstate", JsonConvert.SerializeObject(new Dictionary<string, string>
                {
                    { "sessioname", sessionname },
                    { "status", "WAITINGPLAYERS" }
                }));

                break;
            case GameState.ARENA:

                socketServer.Socket.Emit("changematchstate", JsonConvert.SerializeObject(new Dictionary<string, string>
                {
                    { "sessioname", sessionname },
                    { "status", "BATTLE" }
                }));

                break;
        }
    }

    

    public IEnumerator PostRequest(string route, string query, Dictionary<string, object> paramsBody, bool loaderEndState, System.Action<System.Object> callback, System.Action errorAction)
    {
        UnityWebRequest apiRquest;

        if (env.TryParseEnvironmentVariable("API_URL", out string httpRequest))
        {

            string finalQuery;

            // If query is empty or only "?", just use appversion
            if (string.IsNullOrEmpty(query) || query == "?")
            {
                finalQuery = "?appversion=" + Application.version;
            }
            else
            {
                finalQuery = "?appversion=" + Application.version + "&" + query.TrimStart('?');
            }

            apiRquest = UnityWebRequest.PostWwwForm(httpRequest + route + finalQuery, UnityWebRequest.kHttpVerbPOST);
        }
        else
        {
            //  ERROR PANEL HERE
            Debug.Log("Error API CALL! Error Code: ENV FAILED TO PARSE");
            errorAction?.Invoke();
            yield break;
        }

        byte[] credBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(paramsBody));
        UploadHandler uploadHandler = new UploadHandlerRaw(credBytes);

        apiRquest.uploadHandler = uploadHandler;

        apiRquest.SetRequestHeader("Content-Type", "application/json");

        yield return apiRquest.SendWebRequest();

        if (apiRquest.result == UnityWebRequest.Result.Success)
        {
            string response = apiRquest.downloadHandler.text;

            if (response[0] == '{' && response[response.Length - 1] == '}')
            {
                try
                {
                    Dictionary<string, object> apiresponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);

                    if (!apiresponse.ContainsKey("message"))
                    {
                        //  ERROR PANEL HERE
                        Debug.Log("Error API CALL! Error Code: " + response);

                        errorAction?.Invoke();
                        yield break;
                    }

                    if (apiresponse["message"].ToString() != "success")
                    {
                        //  ERROR PANEL HERE
                        if (!apiresponse.ContainsKey("data"))
                        {
                            Debug.Log("Error API CALL! Error Code: " + response);

                            yield break;
                        }
                        Debug.Log($"Error API CALL! Error Code: {apiresponse["data"]}");
                        yield break;
                    }

                    if (apiresponse.ContainsKey("data"))
                        callback?.Invoke(apiresponse["data"].ToString());
                    else
                        callback?.Invoke("");

                    Debug.Log("SUCCESS API CALL");
                }
                catch (Exception ex)
                {
                    //  ERROR PANEL HERE
                    Debug.Log("Error API CALL! Error Code: " + ex.Message);
                    errorAction?.Invoke();
                }
            }
            else
            {
                //  ERROR PANEL HERE
                Debug.Log("Error API CALL! Error Code: " + response);
                errorAction?.Invoke();
            }
        }

        else
        {
            try
            {
                errorAction?.Invoke();

                Dictionary<string, object> apiresponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(apiRquest.downloadHandler.text);


                switch (apiRquest.responseCode)
                {
                    case 400:
                        Debug.Log($"{apiresponse["data"]}");
                        break;
                    case 300:
                        Debug.Log($"{apiresponse["data"]}");
                        break;
                    case 301:
                        Debug.Log($"{apiresponse["data"]}");
                        break;
                    case 405:
                        Debug.Log($"{apiresponse["data"]}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Error API CALL! Error Code: " + apiRquest.result + ", " + apiRquest.downloadHandler.text);
                errorAction?.Invoke();
            }
        }
    }
}


[System.Serializable]
public class ShrinkSizeList
{
    public List<Vector3> shrinksizes;
}

public class RemovePlayerData
{
    public string username { get; set; }
}

[System.Serializable]
public class PlayerSpawnData
{
    public string _id;
    public string username;
    public string ownerid;
    public int hairstyle;
    public int haircolor;
    public int clothingcolor;
    public int skincolor;
}