using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProfileHistoryItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI killTMP;
    [SerializeField] private TextMeshProUGUI placementTMP;
    [SerializeField] private TextMeshProUGUI dateTMP;

    public void InitializeHistory(string kill, string placement, string date)
    {
        killTMP.text = kill;
        placementTMP.text = placement;
        dateTMP.text = date;
    }
}
