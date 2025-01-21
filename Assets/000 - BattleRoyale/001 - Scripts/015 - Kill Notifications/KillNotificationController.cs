using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillNotificationController : MonoBehaviour
{
    public static KillNotificationController KillNotifInstance { get; private set; }

    //  ===================

    [Space]
    [SerializeField] private GameObject killNotificationObj;
    public Transform killNotifTF;


    private void Awake()
    {
        KillNotifInstance = this;
    }

    private void OnDisable()
    {
        KillNotifInstance = null;
    }

    public void ShowMessage(string killer)
    {
        Instantiate(killNotificationObj, killNotifTF).GetComponent<KillNotificationItem>().SetData(killer);
    }
}
