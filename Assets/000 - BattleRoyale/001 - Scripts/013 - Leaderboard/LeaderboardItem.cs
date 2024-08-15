using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI usernameTMP;
    [SerializeField] private TextMeshProUGUI killTMP;

    public void SetData(string username, string kill)
    {
        usernameTMP.text = username;
        killTMP.text = kill;
    }
}
