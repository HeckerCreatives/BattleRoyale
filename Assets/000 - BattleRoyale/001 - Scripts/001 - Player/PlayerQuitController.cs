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

    private async void Awake()
    {
        if (!Runner) await Task.Yield();

        if (!HasInputAuthority) return;

        gameConclusionCountdown = 10f;

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

        if (!HasInputAuthority) return;

        GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to quit the match? You will not gain any xp and points for this match.", () =>
        {
            isQuit = true;
            Runner.Shutdown();
            GameManager.Instance.SceneController.CurrentScene = "Lobby";
        }, null);
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
