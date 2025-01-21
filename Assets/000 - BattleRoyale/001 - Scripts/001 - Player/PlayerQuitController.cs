using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class PlayerQuitController : NetworkBehaviour
{
    [SerializeField] private bool isQuit;
    [SerializeField] private GameObject gameOverObj;
    [SerializeField] private TextMeshProUGUI returningToLobbyTimer;

    [Header("DEBUGGER LOCAL")]
    [SerializeField] private float gameConclusionCountdown;

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }

    private async void Awake()
    {
        if (!Runner) await Task.Yield();

        if (!HasInputAuthority) return;

        gameConclusionCountdown = 5f;

        GameManager.Instance.SceneController.onSceneChange += SceneChange;
    }

    private void LateUpdate()
    {
        if (HasInputAuthority && gameOverObj.activeInHierarchy)
        {
            returningToLobbyTimer.text = $"Returning to lobby in {gameConclusionCountdown:n0}..";

            if (gameConclusionCountdown <= 0f && !isQuit)
            {
                QuitGameConclusion();
                return;
            }

            if (gameConclusionCountdown > 0f)
            gameConclusionCountdown -= Time.deltaTime;
        }
    }

    private void OnDisable()
    {
        if (!HasInputAuthority) return;

        GameManager.Instance.SceneController.onSceneChange -= SceneChange;
    }

    private void SceneChange(object sender, EventArgs e)
    {
        if (isQuit) return;

        Debug.Log("Quiting because of Disconnection");

        Runner.Shutdown();
    }

    public void QuitBtn()
    {
        if (!Runner) return;

        if (ServerManager == null) return;

        if (!HasInputAuthority) return;

        if (ServerManager.CurrentGameState == GameState.WAITINGAREA)
        {
            GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to quit the match?", () => 
            {
                isQuit = true;
                Runner.Shutdown();
                GameManager.Instance.SceneController.CurrentScene = "Lobby";
            }, null);
        }
        else if (ServerManager.CurrentGameState == GameState.ARENA)
        {
            GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to quit the match? You will not gain any xp and points for this match.", () =>
            {
                isQuit = true;
                Runner.Shutdown();
                GameManager.Instance.SceneController.CurrentScene = "Lobby";
            }, null);
        }
    }

    public void QuitGameConclusion()
    {
        if (!Runner) return;

        if (!HasInputAuthority) return;

        isQuit = true;
        Runner.Shutdown();
        GameManager.Instance.SceneController.CurrentScene = "Lobby";
    }
}
