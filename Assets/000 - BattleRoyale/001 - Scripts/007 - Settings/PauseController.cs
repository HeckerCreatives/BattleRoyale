using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseController : MonoBehaviour
{
    public void KillNotifEnabler(bool active) => KillNotificationController.KillNotifInstance.gameObject.SetActive(active);
}
