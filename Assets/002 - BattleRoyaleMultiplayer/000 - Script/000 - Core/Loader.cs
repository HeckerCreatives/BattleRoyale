using Fusion.Sample.DedicatedServer;
using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Loader : MonoBehaviour
{
    [SerializeField] private bool isServer;
    [SerializeField] private UserData userData;
    [SerializeField] private bool debugMode;
    [ConditionalField("debugMode")][SerializeField] private string sceneName;

    //  Lobby: AsiaBR

    private void Awake()
    {
        if (isServer)
        {
            Application.targetFrameRate = 30;
            userData.Username = "Server";
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene("Persistent");
        }
    }
}
