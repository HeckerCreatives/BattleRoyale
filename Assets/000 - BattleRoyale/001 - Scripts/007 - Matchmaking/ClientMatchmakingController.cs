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

public class ClientMatchmakingController : MonoBehaviour
{
    [SerializeField] private UserData userData;

    [Space]
    //  delete this after today's presentation
    [SerializeField] private NetworkRunner instanceRunner;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private GameObject matchmakingObj;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] public NetworkRunner currentRunnerInstance;
    [ReadOnly][SerializeField] private float currentTime;
    [ReadOnly][SerializeField] private bool findingMatch;
    [ReadOnly][SerializeField] private bool matchFound;
    [ReadOnly][SerializeField] string minutesFindMatch;
    [ReadOnly][SerializeField] string secondsFindMatch;

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

        minutesFindMatch = Mathf.Floor(currentTime / 60).ToString("00");

        secondsFindMatch = (currentTime % 60).ToString("00");

        timerTMP.text = string.Format("{0} : {1}", minutesFindMatch, secondsFindMatch);
    }

    public async void FindMatch()
    {
        if (findingMatch) return;

        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        findingMatch = true;

        cancelBtn.interactable = true;

        currentRunnerInstance = Instantiate(instanceRunner);

        var players = new List<Unity.Services.Matchmaker.Models.Player>
        {
            new Unity.Services.Matchmaker.Models.Player(userData.Username, new Dictionary<string, object>())
        };

        var options = new CreateTicketOptions(
              "BattleRoyale", // The name of the queue defined in the previous step,
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
                    Debug.LogError("Failed to get ticket status. Error: " + assignment.Message);
                    break;
                case MultiplayAssignment.StatusOptions.Timeout:
                    Debug.LogError("Failed to get ticket status. Ticket timed out. Retrying to find match");
                    break;
                default:
                    throw new InvalidOperationException();
            }
        } while (!gotAssignment && findingMatch);

        if (matchfound)
        {
            Debug.Log($"Server IP: {assignment.Ip}   Port: {(ushort)assignment.Port}");

            JoinDropballSession();
        }
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

                return runner.StartGame(new StartGameArgs()
                {
                    GameMode = gameMode,
                    SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                    Scene = sceneRef
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

    private async void ShutdownServer()
    {
        if (ticketResponse != null)
            await MatchmakerService.Instance.DeleteTicketAsync(ticketResponse.Id);

        if (currentRunnerInstance == null)
        {
            matchmakingObj.SetActive(false);
            return;
        }

        await currentRunnerInstance.Shutdown(true);

        Destroy(currentRunnerInstance.gameObject);

        currentRunnerInstance = null;

        matchmakingObj.SetActive(false);
    }
}
