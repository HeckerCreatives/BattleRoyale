using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class WaitingAreaTimerController : NetworkBehaviour
{
    [SerializeField] public GameObject Timer;
    [SerializeField] private TextMeshProUGUI timerTMP; 

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }

    private void Update()
    {
        if (HasInputAuthority)
        {
            if (ServerManager.CurrentGameState != GameState.WAITINGAREA)
            {
                Timer.gameObject.SetActive(false);
                return;
            }

            if (ServerManager.WaitingAreaTimer > 0f)
            {
                Timer.gameObject.SetActive(true);
                timerTMP.text = $"{GameManager.Instance.GetMinuteSecondsTime(ServerManager.WaitingAreaTimer)}";
            }
        }
    }
}
