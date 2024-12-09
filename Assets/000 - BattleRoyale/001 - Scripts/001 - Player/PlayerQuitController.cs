using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerQuitController : NetworkBehaviour
{
    [Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }

    public void QuitBtn()
    {
        if (!Runner) return;

        if (ServerManager == null) return;

        if (!HasInputAuthority) return;

        if (ServerManager.CurrentGameState == GameState.WAITINGAREA)
        {
            GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to quit the match?", () => 
            {
                Runner.Shutdown();
                GameManager.Instance.SceneController.CurrentScene = "Lobby";
            }, null);
        }
        else if (ServerManager.CurrentGameState == GameState.ARENA)
        {
            GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to quit the match? You will not gain any xp and points for this match.", () =>
            {
                Runner.Shutdown();
                GameManager.Instance.SceneController.CurrentScene = "Lobby";
            }, null);
        }
    }

    public void QuitGameConclusion()
    {
        if (!Runner) return;

        if (!HasInputAuthority) return;

        Runner.Shutdown();
        GameManager.Instance.SceneController.CurrentScene = "Lobby";
    }
}
