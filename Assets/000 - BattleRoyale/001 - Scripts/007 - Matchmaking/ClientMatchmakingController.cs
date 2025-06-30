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
    [SerializeField] private bool useMultiplay;

    [Space]
    [SerializeField] private NetworkRunner instanceRunner;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private GameObject matchmakingObj;
    [SerializeField] private GameObject findABattleObj;

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

    //  =====================

    CreateTicketResponse ticketResponse;

    //  =====================

    private void Update()
    {
        FindMatchTimer();
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
                      //GameManager.GetServerRegionName(userData.SelectedServer) + "Test", // The name of the queue defined in the previous step,
                      GameManager.GetServerRegionName(userData.SelectedServer),
                      new Dictionary<string, object>());

                Debug.Log("JOINING LOBBY");

                await JoinLobby("AsiaBR");

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

                JoinDropballSession();
            }
        }, null));
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

            await JoinLobby("AsiaBR");

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

                //var appSettings = BuildCustomAppSetting(userData.SelectedServer);

                return runner.StartGame(new StartGameArgs()
                {
                    GameMode = gameMode,
                    SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                    Scene = sceneRef,
                    //CustomPhotonAppSettings = appSettings
                });
            }
        }

        return null;
    }

    public void CancelMatch()
    {
        currentTime = 0f;

        findingMatch = false;
        matchFound = false;

        cancelBtn.interactable = false;

        timerTMP.text = "Canceling matchmaking...";

        ShutdownServer();
    }

    public async Task ShutdownServer()
    {
        if (ticketResponse != null)
            await MatchmakerService.Instance.DeleteTicketAsync(ticketResponse.Id);

        if (currentRunnerInstance == null)
        {
            matchmakingObj.SetActive(false);
            return;
        }

        currentRunnerInstance.GetComponent<PlayerMultiplayerEvents>().queuedisconnection = null;

        await currentRunnerInstance.Shutdown(true);

        Destroy(currentRunnerInstance.gameObject);

        currentRunnerInstance = null;

        currentRunnerInstance = Instantiate(instanceRunner);

        matchmakingObj.SetActive(false);

        findABattleObj.SetActive(true);
    }
}
