using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerShrinkZoneTimer : NetworkBehaviour
{
    [SerializeField] private Slider safeZoneSlider;
    //[SerializeField] private Slider distanceSlider;
    [SerializeField] private TextMeshProUGUI safeZoneTimerTMP;

    [field: Header("DEBUGGER")]
    [field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }

    public override void Render()
    {
        if (!HasInputAuthority) return;

        if (ServerManager == null) return;

        if (ServerManager.CurrentGameState != GameState.ARENA)
        {
            safeZoneTimerTMP.text = "00 : 00";
            safeZoneSlider.value = 0;
            //distanceSlider.value = 0;
            return;
        }

        if (ServerManager.CurrentSafeZoneState == SafeZoneState.TIMER)
        {
            safeZoneTimerTMP.text = $"{GameManager.Instance.GetMinuteSecondsTime(ServerManager.SafeZoneTimer)}";
        }
        else
        {
            safeZoneTimerTMP.text = "00 : 00";
            safeZoneSlider.value = 0;
            //distanceSlider.value = 0;
        }


        float percentage = Mathf.InverseLerp(
            ServerManager.SafeZone.StartShrinkSize.magnitude,
            ServerManager.SafeZone.CurrentShrink.magnitude,
            ServerManager.SafeZone.CurrentShrinkSize.magnitude
        );
        //float distanceFromCenter = Vector3.Distance(
        //    new Vector3(transform.position.x, 0, transform.position.z),
        //    new Vector3(ServerManager.SafeZone.transform.position.x, 0, ServerManager.SafeZone.transform.position.z)
        //);

        //float circleRadius = ServerManager.SafeZone.CurrentShrinkSize.x / 2; // Assuming scale.x is the diameter

        // Calculate the absolute distance to the boundary
        //float distanceToEdge = Mathf.Abs(circleRadius - distanceFromCenter);

        safeZoneSlider.value = percentage;
        //distanceSlider.value = Mathf.Clamp01(1 - (distanceToEdge / circleRadius));
    }



}
