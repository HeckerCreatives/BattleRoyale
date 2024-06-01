using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClientMatchmakingController : MonoBehaviour
{
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

        timerTMP.text = "FINDING GAME:  " + string.Format("{0}:{1}", minutesFindMatch, secondsFindMatch);
    }

    public void ConnectToServer()
    {
        currentRunnerInstance = Instantiate(instanceRunner);

        findingMatch = true;

        JoinDropballSession();
    }

    private async void JoinDropballSession()
    {
        if (findingMatch)
        {
            currentTime = 0f;

            //  JOIN SESSION HERE
            var result = await JoinLobby("AsiaBR");

            if (result.Ok)
            {
                var sessionResult = await StartSimulation(currentRunnerInstance, GameMode.Client);

                cancelBtn.interactable = false;

                if (sessionResult.Ok)
                {
                    timerTMP.text = "MATCH FOUND! JOINING GAME...";
                    matchFound = true;
                    findingMatch = false;
                    //GameManager.Instance.SceneController.MultiplayerScene = true;
                }
                else
                {
                    cancelBtn.interactable = true;
                    Reconnect();
                }
            }
            //else
            //{
            //    Reconnect();
            //}
        }
    }

    public async void Reconnect()
    {
        if (currentRunnerInstance != null)
        {
            await currentRunnerInstance.Shutdown(true);

            Destroy(currentRunnerInstance.gameObject);

            currentRunnerInstance = null;

            await Task.Delay(4000);

            ConnectToServer();
        }
    }

    public Task<StartGameResult> JoinLobby(string lobbyName)
    {
        return currentRunnerInstance.JoinSessionLobby(SessionLobby.Custom, lobbyName);
    }

    public Task<StartGameResult> StartSimulation(
       NetworkRunner runner,
       GameMode gameMode
     )
    {
        SceneRef sceneRef = default;

        var scenePath = SceneManager.GetActiveScene().path;

        Debug.Log($"scene path: {scenePath}");

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

        return runner.StartGame(new StartGameArgs()
        {
            GameMode = gameMode,
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
            Scene = sceneRef,
        });
    }

    public void CancelMatch()
    {
        findingMatch = false;
        matchFound = false;

        cancelBtn.interactable = false;

        timerTMP.text = "Canceling matchmaking...";

        ShutdownServer();
    }

    private async void ShutdownServer()
    {
        await currentRunnerInstance.Shutdown(true);

        Destroy(currentRunnerInstance.gameObject);

        currentRunnerInstance = null;

        matchmakingObj.SetActive(false);
    }
}
