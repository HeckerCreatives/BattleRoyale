using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class QuestTab : MonoBehaviour
{
    [SerializeField] private LobbyQuestController controller;
    [SerializeField] private QuestTabState currentTab;

    [Space]
    [SerializeField] private Image btnImg;
    [SerializeField] private Color selected;
    [SerializeField] private Color unselected;

    [Space]
    [SerializeField] private GameObject panelObj;

    private void OnEnable()
    {
        CheckBtn();
        controller.OnQuestTabChange += TabChange;   
    }

    private void OnDisable()
    {
        controller.OnQuestTabChange -= TabChange;
    }

    private void TabChange(object sender, EventArgs e)
    {
        CheckBtn();
    }

    private void CheckBtn()
    {
        btnImg.color = controller.CurrentQuestTab == currentTab ? selected : unselected;
        panelObj.SetActive(controller.CurrentQuestTab == currentTab);
    }

    public void ChangeTab()
    {
        if (controller.CurrentQuestTab == currentTab) return;

        controller.CurrentQuestTab = currentTab;
    }
}
