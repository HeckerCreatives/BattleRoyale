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
    [SerializeField] private string scene;
    //  Lobby: AsiaBR

    private void Awake()
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
                Application.targetFrameRate = 30;
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
