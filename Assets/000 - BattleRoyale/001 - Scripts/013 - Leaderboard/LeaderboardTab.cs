using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardTab : MonoBehaviour
{
    [SerializeField] private LeaderboardState tabState;
    [SerializeField] private Color onTab;
    [SerializeField] private Color offTab;

    [Space]
    [SerializeField] private string header;
    [SerializeField] private TextMeshProUGUI headerTMP;

    [Space]
    [SerializeField] private LobbyController lobbyController;

    [Space]
    [SerializeField] private GameObject leaderboardContainer;
    [SerializeField] private Image tabBtn;

    private void OnEnable()
    {
        CheckTab();
        lobbyController.OnLeaderboardStateChange += StateChange;
    }

    private void OnDisable()
    {
        lobbyController.OnLeaderboardStateChange -= StateChange;
    }

    private void StateChange(object sender, EventArgs e)
    {
        CheckTab();
    }

    private void CheckTab()
    {
        if (lobbyController.CurrentLeaderboardState == tabState)
        {
            leaderboardContainer.SetActive(true);
            tabBtn.color = onTab;
            headerTMP.text = header;
        }
        else
        {
            leaderboardContainer.SetActive(false);
            tabBtn.color = offTab;
        }
    }

    public void ChangeTab() => lobbyController.CurrentLeaderboardState = tabState;
}
