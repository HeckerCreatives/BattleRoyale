using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaitingAreaTimerController : NetworkBehaviour
{
    [SerializeField] private PlayerNetworkCore core;

    [Header("DEBUGGER")]
    [MyBox.ReadOnly][SerializeField] private bool doneInitialize;
    [field: MyBox.ReadOnly][field: SerializeField] private TextMeshProUGUI timerTMP;
    [MyBox.ReadOnly][SerializeField] private DedicatedServerManager ServerManager { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField] public GameObject Timer { get; set; }
    //[field: MyBox.ReadOnly][field: SerializeField] public TextMeshProUGUI SpawnMapTimer { get; set; }

    public IEnumerator InitializeWaitingAreaTimer()
    {
        while (core.ServerManager == null || Timer == null) yield return null;

        ServerManager = core.ServerManager.GetComponent<DedicatedServerManager>();

        timerTMP = Timer.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();

        timerTMP.text = $"{GameManager.Instance.GetMinuteSecondsTime(ServerManager.WaitingAreaTimer)}";
        //SpawnMapTimer.text = $"{GameManager.Instance.GetMinuteSecondsTime(ServerManager.WaitingAreaTimer)}";

        doneInitialize = true;
    }

    private void Update()
    {
        if (doneInitialize && HasInputAuthority)
        {
            if (ServerManager.CurrentGameState == GameState.WAITINGAREA)
            {
                if (ServerManager.CanCountWaitingAreaTimer)
                {
                    Timer.gameObject.SetActive(true);
                    timerTMP.text = $"{GameManager.Instance.GetMinuteSecondsTime(ServerManager.WaitingAreaTimer)}";
                    //SpawnMapTimer.text = $"{GameManager.Instance.GetMinuteSecondsTime(ServerManager.WaitingAreaTimer)}";
                }
            }
            else
            {
                Timer.gameObject.SetActive(false);
            }
        }
    }
}
