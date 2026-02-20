using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProfileHistoryItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI typeTMP;
    [SerializeField] private TextMeshProUGUI killTMP;
    [SerializeField] private TextMeshProUGUI placementTMP;
    [SerializeField] private TextMeshProUGUI dateTMP;
    [SerializeField] private TextMeshProUGUI playtimeTMP;

    public void InitializeHistory(string type, string kill, string placement, string playtime, string date)
    {
        typeTMP.text = type;
        killTMP.text = kill;
        placementTMP.text = placement;
        playtimeTMP.text = playtime;
        dateTMP.text = date;
    }
}
