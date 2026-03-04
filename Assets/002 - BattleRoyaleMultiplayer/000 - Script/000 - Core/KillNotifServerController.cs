using Fusion;
using System.Threading.Tasks;
using UnityEngine;

public class KillNotifServerController : NetworkBehaviour
{
    public static KillNotifServerController Instance { get; private set; }

    //  ===================

    [SerializeField] private NetworkObject killNotifObj;

    [field: Header("DEBUGGER")]
    [field: SerializeField] [Networked] public KillNotificationController KillNotifController { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public async void SpawnNotifUI()
    {
        while (!Runner)
            await Task.Yield();

        Debug.Log($"Spawning Kill Notification UI");
        Runner.Spawn(killNotifObj, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
        {
            KillNotifController = obj.GetComponent<KillNotificationController>();
        });
    }
}
