using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI usernameTMP;
    [SerializeField] private TextMeshProUGUI rankTMP;
    [SerializeField] private TextMeshProUGUI killTMP;

    public void SetData(string username, string rank, string kill)
    {
        usernameTMP.text = username;
        rankTMP.text = rank;
        killTMP.text = kill;
    }
}
