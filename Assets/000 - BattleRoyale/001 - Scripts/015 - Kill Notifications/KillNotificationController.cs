using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillNotificationController : MonoBehaviour
{
    //  ===================

    public GameObject damageIndicatorPrefab;
    public int poolSize = 20;
    public Transform uiContainer; // Set in inspector: your canvas panel

    private Queue<KillNotificationItem> pool = new Queue<KillNotificationItem>();
    private Queue<string> indicatorQueue = new Queue<string>();
    private bool isProcessing = false;

    private void Awake()
    {
        if (GameManager.Instance == null)
            uiContainer.gameObject.SetActive(false);
        else
            uiContainer.gameObject.SetActive(true);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(damageIndicatorPrefab, uiContainer);
            obj.GetComponent<KillNotificationItem>().pool = this;
            obj.SetActive(false);
            pool.Enqueue(obj.GetComponent<KillNotificationItem>());
        }
    }

    public void ShowIndicator(string message)
    {
        indicatorQueue.Enqueue(message);

        if (!isProcessing)
        {
            StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        isProcessing = true;

        while (indicatorQueue.Count > 0)
        {
            var tempmessage = indicatorQueue.Dequeue();

            KillNotificationItem indicator = GetIndicator();

            indicator.Setup(tempmessage);

            yield return null;
        }

        isProcessing = false;
    }

    public KillNotificationItem GetIndicator()
    {
        if (pool.Count > 0)
        {
            var obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }
        else
        {
            GameObject obj = Instantiate(damageIndicatorPrefab, uiContainer);
            return obj.GetComponent<KillNotificationItem>();
        }
    }

    public void ReturnIndicator(KillNotificationItem indicator)
    {
        indicator.gameObject.SetActive(false);
        pool.Enqueue(indicator);
    }
}
