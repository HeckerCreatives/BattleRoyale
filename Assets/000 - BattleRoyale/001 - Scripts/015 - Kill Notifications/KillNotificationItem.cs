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
        if (Time.time >= startTime + 8f)
            Destroy(gameObject);
    }

    public void SetData(string killer)
    {
        startTime = Time.time;
        killNotifTMP.text = killer;
    }
}
