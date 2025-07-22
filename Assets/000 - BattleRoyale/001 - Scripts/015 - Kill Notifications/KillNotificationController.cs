using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillNotificationController : NetworkBehaviour
{
    //  ===================

    [Space]
    [SerializeField] private GameObject killNotificationObj;
    public Transform killNotifTF;

    public void ShowMessage(string killer)
    {
        Instantiate(killNotificationObj, killNotifTF).GetComponent<KillNotificationItem>().SetData(killer);
    }
}
