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

    public override void Render()
    {
        if (!HasInputAuthority) return;

        if (MultiplayerServerManager.Instance.CurrentGameState != GameState.ARENA)
        {
            safeZoneTimerTMP.text = "00 : 00";
            safeZoneSlider.value = 0;
            //distanceSlider.value = 0;
            return;
        }

        if (SafeZoneServerController.Instance.CurrentSafeZoneState == SafeZoneState.TIMER)
        {
            safeZoneTimerTMP.text = $"{GameManager.Instance.GetMinuteSecondsTime(SafeZoneServerController.Instance.SafeZoneTimer)}";
        }
        else
        {
            safeZoneTimerTMP.text = "00 : 00";
            safeZoneSlider.value = 0;
            //distanceSlider.value = 0;
        }


        float percentage = Mathf.InverseLerp(
            SafeZoneServerController.Instance.SafeZone.StartShrinkSize.magnitude,
            SafeZoneServerController.Instance.SafeZone.CurrentShrink.magnitude,
            SafeZoneServerController.Instance.SafeZone.CurrentShrinkSize.magnitude
        );

        safeZoneSlider.value = percentage;
    }
}
