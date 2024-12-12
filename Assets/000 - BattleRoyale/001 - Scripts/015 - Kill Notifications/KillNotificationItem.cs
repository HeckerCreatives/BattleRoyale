using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KillNotificationItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI killNotifTMP;

    [Header("DEBUGGER")]
    [SerializeField] private float startTime;

    private void Update()
    {
        if (Time.time >= startTime + 3f)
            Destroy(gameObject);
    }

    public void SetData(string killer, string killed)
    {
        startTime = Time.time;
        killNotifTMP.text = $"{killer} killed {killed}";
    }
}
