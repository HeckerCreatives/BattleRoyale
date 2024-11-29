using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerNetworkLoader : NetworkBehaviour
{
    [SerializeField] private GameSettingController gameSettingController;

    [Header("DEBUGGER")]
    [SerializeField] private PlayerMultiplayerEvents events;

    public override async void Spawned()
    {
        while (!Runner) await Task.Delay(100);

        if (!HasInputAuthority) return;

        events = Runner.GetComponent<PlayerMultiplayerEvents>();

        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetVolumeSlidersOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetGraphicsOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetLookSensitivityOnStart());

        events.doneLoadingScene = true;
    }
}
