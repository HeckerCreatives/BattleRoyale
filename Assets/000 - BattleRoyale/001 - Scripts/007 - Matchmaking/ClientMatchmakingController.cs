using Fusion;
using Fusion.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Services.Matchmaker;
using System;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Linq;
using System.Threading;
using Unity.VisualScripting.Antlr3.Runtime;
using System.Drawing;
using Fusion.Photon.Realtime;
using Newtonsoft.Json;
using System.Text;

public class ClientMatchmakingController : MonoBehaviour
{
    private event EventHandler ServerChange;
    public event EventHandler OnServerChange
    {
        add
        {
            if (ServerChange == null || ServerChange.GetInvocationList().Contains(value))
                ServerChange += value;
        }
        remove { ServerChange -= value; }
    }
    public void AddServerSelected(string server)
    {
        if (serverSelected.Contains(server)) return;

        serverSelected.Add(server);
    }
    public void RemoveServerSelected(string server)
    {
        if (!serverSelected.Contains(server)) return;

        serverSelected.Remove(server);
    }
    public List<string> ServerSelected
    {
        get => serverSelected;
    }

    //  ========================

    [SerializeField] private UserData userData;
    [SerializeField] private string lobbyName;
    [SerializeField] private bool useMultiplay;
    [SerializeField] private bool usePrivateServer;
    [SerializeField] private Button changeServerBtn;
    [SerializeField] private Button changeServerBtn1;

    [Space]
    [SerializeField] private NetworkRunner instanceRunner;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private GameObject matchBtn;
    [SerializeField] private GameObject matchmakingObj;
    [SerializeField] private GameObject findABattleObj;
    [SerializeField] private GameObject reconObj;

    [Space]
    [SerializeField] private GameObject serverListLoader;
    [SerializeField] private GameObject serverListContent;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] public NetworkRunner currentRunnerInstance;
    [ReadOnly][SerializeField] private float currentTime;
    [ReadOnly][SerializeField] private bool findingMatch;
    [ReadOnly][SerializeField] private bool matchFound;
    [ReadOnly][SerializeField] int minutesFindMatch;
    [ReadOnly][SerializeField] int secondsFindMatch;
    [SerializeField] private List<string> serverSelected;
    [SerializeField] private string roomname;
    [SerializeField] private bool finishCheckingIfCanRecon;

    //  =====================

    CreateTicketResponse ticketResponse;

    //  =====================

    private void OnEnable()
    {
        GameManager.Instance.SocketMngr.Socket.On("reconnectfail", (response) =>
        {
            GameManager.Instance.AddJob(() =>
            {
                finishCheckingIfCanRecon = true;
            });
        });


        GameManager.Instance.SocketMngr.Socket.On("reconnectexist", (response) =>
        {
            GameManager.Instance.AddJob(() =>
            {
                finishCheckingIfCanRecon = true;

                List<Dictionary<string, string>> tempdata = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(response.ToString());

                if (tempdata.Count <= 0) return;

                roomname = tempdata[0]["roomName"];

                reconObj.SetActive(true);

                matchBtn.SetActive(false);
            });
        });

        GameManager.Instance.SocketMngr.Socket.On("doneremovereconnect", (response) =>
        {
            GameManager.Instance.AddJob(() =>
            {
                GameManager.Instance.NoBGLoading.SetActive(false);

                reconObj.SetActive(false);

                matchBtn.SetActive(true);
            });
        });

        if (usePrivateServer)
        {
            GameManager.Instance.SocketMngr.Socket.On("matchfound", (response) =>
            {
                Debug.Log(response.ToString());
                GameManager.Instance.AddJob(() => roomname = response.GetValue<string>());
                GameManager.Instance.AddJob(StartMatchFinding);
            });

            GameManager.Instance.SocketMngr.Socket.On("matchstatuschanged", (response) =>
            {
                string tempresponse = response.ToString();

                Debug.Log(tempresponse);

                GameManager.Instance.AddJob(() =>
                {
                    if (matchFound) return;

                    List<Dictionary<string, string>> tempdata = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(tempresponse);

                    if (tempdata.Count <= 0) return;

                    if (tempdata[0]["status"] != "WAITING") return;

                    GameManager.Instance.AddJob(() => roomname = tempdata[0]["roomName"]);
                    GameManager.Instance.AddJob(StartMatchFinding);
                });
            });
        }
    }

    private void Update()
    {
        FindMatchTimer();
    }

    public IEnumerator WaitForReconnectStatus()
    {
        GameManager.Instance.SocketMngr.EmitEvent("needtoreconnect", null);

        while (!finishCheckingIfCanRecon) yield return null;
    }

    private void FindMatchTimer()
    {
        if (!findingMatch) return;

        if (matchFound) return;

        currentTime += Time.deltaTime;

        minutesFindMatch = Mathf.FloorToInt(currentTime / 60);

        secondsFindMatch = Mathf.FloorToInt(currentTime % 60);

        timerTMP.text = string.Format("{0:00} : {1:00}", minutesFindMatch, secondsFindMatch);
    }

    private FusionAppSettings BuildCustomAppSetting(string region)
    {

        var appSettings = PhotonAppSettings.Global.AppSettings.GetCopy(); ;

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

    public void FindMatch()
    {
        if (findingMatch) return;

        changeServerBtn.interactable = false;
        changeServerBtn1.interactable = false;

        if (userData.GameDetails.energy <= 0)
        {
            GameManager.Instance.NotificationController.ShowConfirmation("Your energy is empty! You can still queue for the game but you won't gain any xp and points. Would you like to continue?", () => 
            {
                Matching();
            }, null);
        }
        else
            Matching();
    }

    private void Matching()
    {
        findABattleObj.gameObject.SetActive(false);

        GameManager.Instance.NoBGLoading.SetActive(true);

        StartCoroutine(GameManager.Instance.GetRequest("/usergamedetail/checkingamemaintenance", "", false, async (resposne) =>
        {
            if (useMultiplay)
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                matchmakingObj.SetActive(true);

                findABattleObj.SetActive(false);

                findingMatch = true;

                cancelBtn.interactable = true;

                currentRunnerInstance = Instantiate(instanceRunner);

                currentRunnerInstance.GetComponent<PlayerMultiplayerEvents>().queuedisconnection = CancelMatch;

                var players = new List<Unity.Services.Matchmaker.Models.Player>
                {
                    new Unity.Services.Matchmaker.Models.Player(userData.Username, new Dictionary<string, object>())
                };

                var options = new CreateTicketOptions(
                      "HongKongTest",
                      //GameManager.GetServerRegionName(userData.SelectedServer), // The name of the queue defined in the previous step,
                      new Dictionary<string, object>());

                Debug.Log("JOINING LOBBY");

                await JoinLobby(lobbyName);

                Debug.Log("DONE JOINING LOBBY, RECEIVING TICKET RESPONSE");

                ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);

                Debug.Log($"ticket id: {ticketResponse.Id}");

                MultiplayAssignment assignment = null;
                bool gotAssignment = false;
                bool matchfound = false;

                do
                {
                    //Rate limit delay
                    await Task.Delay(TimeSpan.FromSeconds(1f));

                    // Poll ticket
                    var ticketStatus = await MatchmakerService.Instance.GetTicketAsync(ticketResponse.Id);

                    if (ticketStatus == null)
                    {
                        continue;
                    }

                    //Convert to platform assignment data (IOneOf conversion)
                    if (ticketStatus.Type == typeof(MultiplayAssignment))
                    {
                        assignment = ticketStatus.Value as MultiplayAssignment;
                    }

                    switch (assignment?.Status)
                    {
                        case MultiplayAssignment.StatusOptions.Found:
                            gotAssignment = true;
                            matchfound = true;
                            break;
                        case MultiplayAssignment.StatusOptions.InProgress:
                            //...
                            break;
                        case MultiplayAssignment.StatusOptions.Failed:
                            gotAssignment = true;

                            if (assignment.Message.Contains("maximum capacity reached"))
                                GameManager.Instance.NotificationController.ShowError("Due to high number of players, the current map is currently closed. Please try again later", null);
                            else
                                GameManager.Instance.NotificationController.ShowError($"There's a problem finding a match! Error: {assignment.Message}", null);

                            CancelMatch();
                            break;
                        case MultiplayAssignment.StatusOptions.Timeout:

                            CancelMatch();

                            GameManager.Instance.NotificationController.ShowConfirmation("Due to low number of players, the game couldn't find a match. Would you like to adjust the settings automatically and find a match again?", FindMatch, null);
                            break;
                        default:
                            CancelMatch();

                            GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later", null);

                            break;
                    }
                } while (!gotAssignment && findingMatch);

                if (matchfound)
                {
                    Debug.Log($"Server IP: {assignment.Ip}   Port: {(ushort)assignment.Port}");

                    JoinDropballSession();
                }
            }
            else
            {
                Debug.Log("Not using multiplay, starting photon find match");

                matchmakingObj.SetActive(true);

                findABattleObj.SetActive(false);

                findingMatch = true;

                cancelBtn.interactable = true;

                currentRunnerInstance = Instantiate(instanceRunner);

                currentRunnerInstance.GetComponent<PlayerMultiplayerEvents>().queuedisconnection = CancelMatch;

                if (usePrivateServer)
                    GameManager.Instance.SocketMngr.EmitEvent("findmatch", JsonConvert.SerializeObject(new Dictionary<string, string>()));
                else
                    StartMatchFinding();
            }
        }, null));
    }

    private void StartMatchFinding()
    {
        JoinDropballSession();
    }

    private async void JoinDropballSession()
    {
        cancelBtn.interactable = false;

        var sessionResult = await StartSimulation(currentRunnerInstance, GameMode.Client);

        if (sessionResult.Ok)
        {
            timerTMP.text = "MATCH FOUND!";
            matchFound = true;
            findingMatch = false;
            GameManager.Instance.SceneController.MultiplayerScene = true;
        }
        else
        {
            cancelBtn.interactable = true;
            Reconnect();
        }
    }

    public async void Reconnect()
    {
        if (!findingMatch)
            return;

        if (currentRunnerInstance != null)
        {
            await currentRunnerInstance.Shutdown(true);

            Destroy(currentRunnerInstance.gameObject);

            currentRunnerInstance = null;

            await Task.Delay(4000);

            if (!findingMatch)
                return;

            currentRunnerInstance = Instantiate(instanceRunner);

            currentRunnerInstance.GetComponent<PlayerMultiplayerEvents>().queuedisconnection = CancelMatch;

            Debug.Log("REJOINING LOBBY");

            await JoinLobby(lobbyName);

            JoinDropballSession();
        }
    }

    public Task<StartGameResult> JoinLobby(string lobbyName)
    {
        return currentRunnerInstance.JoinSessionLobby(SessionLobby.Custom, lobbyName);
    }

    public Task<StartGameResult> StartSimulation(NetworkRunner runner, GameMode gameMode)
    {
        if (currentRunnerInstance != null)
        {
            SceneRef sceneRef = default;

            var scenePath = SceneManager.GetActiveScene().path;

            Debug.Log($"scene path: {scenePath}");

            scenePath = scenePath.Substring("Assets/".Length, scenePath.Length - "Assets/".Length - ".unity".Length);
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);

            if (sceneIndex >= 0)
            {
                sceneRef = SceneRef.FromIndex(5);
            }

            NetworkSceneInfo networkSceneInfo = new NetworkSceneInfo();

            if (sceneRef != null)
            {
                if (sceneRef.IsValid == true)
                {
                    networkSceneInfo.AddSceneRef(sceneRef, LoadSceneMode.Single, LocalPhysicsMode.None, true);
                }

                var appSettings = BuildCustomAppSetting(userData.SelectedServer);

                Debug.Log($"FINDING MATCH TO SERVER {userData.SelectedServer}");

                return runner.StartGame(new StartGameArgs()
                {
                    GameMode = gameMode,
                    SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                    Scene = sceneRef,
                    SessionName = roomname,
                    CustomPhotonAppSettings = appSettings,
                    ConnectionToken = Encoding.UTF8.GetBytes(userData.Username)
                });
            }
        }

        return null;
    }

    public void CancelMatch()
    {
        GameManager.Instance.SocketMngr.EmitEvent("quitonmatch", "");

        currentTime = 0f;

        findingMatch = false;
        matchFound = false;

        cancelBtn.interactable = false;

        timerTMP.text = "Canceling matchmaking...";

        matchmakingObj.SetActive(false);

        findABattleObj.SetActive(true);

        changeServerBtn.interactable = true;

        changeServerBtn1.interactable = true;

        ShutdownServer();
    }

    public async Task ShutdownServer()
    {
        var tempRunner = currentRunnerInstance;

        if (ticketResponse != null)
            await MatchmakerService.Instance.DeleteTicketAsync(ticketResponse.Id);

        if (tempRunner == null)
        {
            matchmakingObj.SetActive(false);
            return;
        }

        tempRunner.GetComponent<PlayerMultiplayerEvents>().queuedisconnection = null;

        await tempRunner.Shutdown(true);

        Destroy(tempRunner.gameObject);
    }

    public async void ReconnectMatch()
    {
        GameManager.Instance.NoBGLoading.SetActive(true);

        currentRunnerInstance = Instantiate(instanceRunner);

        var sessionResult = await StartSimulation(currentRunnerInstance, GameMode.Client);

        if (sessionResult.Ok)
        {
            GameManager.Instance.SceneController.MultiplayerScene = true;

            GameManager.Instance.NoBGLoading.SetActive(false);
        }
        else
        {
            GameManager.Instance.NoBGLoading.SetActive(false);

            roomname = "";

            reconObj.SetActive(false);

            matchBtn.SetActive(true);

            GameManager.Instance.NotificationController.ShowError("Your current match is not available or already finished the match. Please queue up again!", null);

            ShutdownServer();
        }
    }

    public void CancelReconnectMatch()
    {
        GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to leave the current match? You will lose the progress and any currency that you use.", () =>
        {
            GameManager.Instance.NoBGLoading.SetActive(true);

            GameManager.Instance.SocketMngr.EmitEvent("removereconnect", null);
        }, null);
    }
}
