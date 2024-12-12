using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerNetworkLoader : NetworkBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private GameSettingController gameSettingController;

    [Header("DEBUGGER")]
    [SerializeField] private PlayerMultiplayerEvents events;
    [field: SerializeField] [Networked] public string Username { get; set; }

    public override async void Spawned()
    {
        while (!Runner) await Task.Delay(100);

        if (!HasInputAuthority) return;

        RPC_SetUserData(userData.Username);

        events = Runner.GetComponent<PlayerMultiplayerEvents>();

        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetVolumeSlidersOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetGraphicsOnStart());
        GameManager.Instance.SceneController.AddActionLoadinList(gameSettingController.SetLookSensitivityOnStart());

        events.doneLoadingScene = true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetUserData(string username)
    {
        this.Username = username;
    }
}
