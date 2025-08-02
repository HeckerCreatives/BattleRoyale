using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProfileHistoryItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI killTMP;
    [SerializeField] private TextMeshProUGUI placementTMP;
    [SerializeField] private TextMeshProUGUI dateTMP;

    public void OnEnable()
    {
        killTMP.text = "-";
        placementTMP.text = "-";
        dateTMP.text = "-";
    }
}
