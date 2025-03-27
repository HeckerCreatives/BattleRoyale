using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class WaitingAreaTimerController : NetworkBehaviour
{
    [SerializeField] public GameObject Timer;
    [SerializeField] public GameObject TimerGetReady;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI timerGetReadyTMP;

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }

    public override void Render()
    {
        if (HasInputAuthority)
        {
            if (ServerManager == null) return;

            if (ServerManager.CurrentGameState != GameState.WAITINGAREA)
            {
                Timer.gameObject.SetActive(false);
                TimerGetReady.SetActive(false);
                return;
            }

            if (ServerManager.CurrentWaitingAreaTimerState == WaitingAreaTimerState.WAITING)
            {
                Timer.gameObject.SetActive(true);
                TimerGetReady.SetActive(false);

                timerTMP.text = $"{GameManager.Instance.GetMinuteSecondsTime(ServerManager.WaitingAreaTimer)}";
            }
            else
            {
                TimerGetReady.SetActive(true);
                Timer.gameObject.SetActive(false);

                timerGetReadyTMP.text = $"{GameManager.Instance.GetMinuteSecondsTime(ServerManager.WaitingAreaTimer)}";
            }
        }
    }
}
