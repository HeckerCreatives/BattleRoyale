using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KillNotificationItem : MonoBehaviour
{
    public TextMeshProUGUI damageText;
    public float lifetime = 5f;

    private float timer;

    public KillNotificationController pool;

    public void Setup(string message)
    {
        damageText.text = message;

        timer = lifetime;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            pool.ReturnIndicator(this);
        }
    }
}
