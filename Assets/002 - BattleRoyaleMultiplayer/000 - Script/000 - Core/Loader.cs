#if !UNITY_ANDROID && !UNITY_IOS
using Aws.GameLift.Server;
#endif

using Fusion;
using Fusion.Sample.DedicatedServer;
using MyBox;
using OneSignalSDK;
using OneSignalSDK.Notifications;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Loader : MonoBehaviour
{
    [SerializeField] private UserData userData;

    [SerializeField] private bool isDebugServer;
    [SerializeField] private bool useGameLiftServer;
    [SerializeField] private string scene;

    private static string logFilePath = "/local/game/server.log";

    //  Lobby: AsiaBR

    private void Start()
    {
        if (useGameLiftServer)
        {
            DontDestroyOnLoad(this);

            // Ensure directory exists
            string dir = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Clear old logs on start
            File.WriteAllText(logFilePath, "=== New GameLift Session Started ===\n");

            // Hook Unity?s logging
            Application.logMessageReceived += HandleLog;

            GameLiftInitialize();
        }
        else
            CustomServer();
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }



    private void HandleLog(string condition, string stackTrace, UnityEngine.LogType type)
    {
        string logEntry = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}] {condition}\n";

        if (type == UnityEngine.LogType.Exception)
            logEntry += stackTrace + "\n";

        File.AppendAllText(logFilePath, logEntry);
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
                    // Health check ? return true if healthy
                    return true;
                },
                7777, // Your listening port
                new LogParameters(new List<string>() { logFilePath })
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
            Debug.Log("GET ARGUMENT VALUE FOR SERVER: " + server);

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
        {
            Debug.Log("NO SERVER AGUMENTS FOUND");
            SceneManager.LoadScene("Persistent");
        }
#else
        if (isDebugServer)
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 30;
            userData.Username = "Server";

            SceneManager.LoadScene(scene);
            return;
        }

        InitializeOneSignal();
#endif
    }

    private async Task InitializeOneSignal()
    {
        OneSignal.ConsentRequired = true;

        OneSignal.Initialize("ea9a6f18-f823-4e81-a388-6ba45633972a");

        OneSignal.ConsentGiven = true;

        if (!OneSignal.Notifications.Permission)
        {
            //OneSignal.Notifications.PermissionChanged += CheckPermission;
            await OneSignal.Notifications.RequestPermissionAsync(true);
        }


        SceneManager.LoadScene("Persistent");
    }

    //private void CheckPermission(object sender, NotificationPermissionChangedEventArgs e)
    //{
        // You can check what the new permission is
        //Debug.Log($"Notification permission changed: {e.Permission}");

        // If you want to continue regardless:
        //SceneManager.LoadScene("Persistent");
    //}
}
