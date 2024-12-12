using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillNotificationController : MonoBehaviour
{
    public static KillNotificationController KillNotifInstance { get; private set; }

    //  ===================

    [Space]
    [SerializeField] private GameObject killNotificationObj;
    [SerializeField] private Transform killNotifTF;


    private void Awake()
    {
        KillNotifInstance = this;
    }

    public void ShowMessage(string killer, string killed)
    {
        Instantiate(killNotificationObj, killNotifTF).GetComponent<KillNotificationItem>().SetData(killer, killed);
    }
}
