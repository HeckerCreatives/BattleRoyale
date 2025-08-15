using Aws.GameLift.Server;
using Fusion;
using Fusion.Sample.DedicatedServer;
using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Loader : MonoBehaviour
{
    [SerializeField] private UserData userData;

    [SerializeField] private bool isDebugServer;
    [SerializeField] private bool useGameLiftServer;
    [SerializeField] private string scene;
    //  Lobby: AsiaBR

    private void Start()
    {
        if (useGameLiftServer)
        {
            DontDestroyOnLoad(this);
            GameLiftInitialize();
        }
        else
            CustomServer();
    }


    private void GameLiftInitialize()
    {
#if !UNITY_ANDROID && !UNITY_IOS
        // Initialize GameLift SDK
        Debug.Log("STARTING GAMELIFT SDK");
        var initOutcome = GameLiftServerAPI.InitSDK();
        if (initOutcome.Success)
        {
            // Define what to do when a game session is created
            ProcessParameters processParams = new ProcessParameters(
                (gameSession) => {
                    // Activate the session so players can connect
                    GameLiftServerAPI.ActivateGameSession();

                    CustomServer();
                },
                (updateGameSession) => {
                    // Handle FlexMatch backfill or session property updates
                },
                () => {
                    // On process termination
                    GameLiftServerAPI.ProcessEnding();
                },
                () => {
                    // Health check — return true if healthy
                    return true;
                },
                7777, // Your listening port
                new LogParameters(new List<string>() { "/local/game/logs/myserver.log" })
            );

            var readyOutcome = GameLiftServerAPI.ProcessReady(processParams);
            if (readyOutcome.Success)
            {
                Debug.Log("GameLift process ready!");
            }
            else
            {
                Debug.Log("ProcessReady failed: " + readyOutcome.Error.ToString());
            }
        }
        else
        {
            Debug.Log("InitSDK failed: " + initOutcome.Error.ToString());
        }
#endif
    }

    private void CustomServer()
    {
        
#if !UNITY_ANDROID && !UNITY_IOS
            var args = CommandLineHelper.GetArgs();

            if (isDebugServer)
            {
                Application.runInBackground = true;
                Application.targetFrameRate = 30;
                userData.Username = "Server";

                SceneManager.LoadScene(scene);
                return;
            }

            if (args.TryGetValue("server", out string server))
            {
                if (server == "yes")
                {
                    Application.runInBackground = true;
                    Application.targetFrameRate = 60;
                    userData.Username = "Server";

                    if (args.TryGetValue("mapname", out string sceneName))
                        SceneManager.LoadScene(sceneName);
                    else
                        SceneManager.LoadScene("Persistent");
                }
                else
                    SceneManager.LoadScene("Persistent");
            }
            else
                SceneManager.LoadScene("Persistent");
#else
                if (isDebugServer)
                {
                    Application.runInBackground = true;
                    Application.targetFrameRate = 30;
                    userData.Username = "Server";

                    SceneManager.LoadScene(scene);
                    return;
                }

                SceneManager.LoadScene("Persistent");
#endif
    }
}
