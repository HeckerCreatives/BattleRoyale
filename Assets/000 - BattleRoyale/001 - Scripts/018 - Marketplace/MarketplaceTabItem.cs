using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MarketplaceTabItem : MonoBehaviour
{
    [SerializeField] private MarketplaceController controller;
    [SerializeField] private MarketplaceTab tab;

    [Space]
    [SerializeField] private GameObject contentObj;

    [Space]
    [SerializeField] private Image tabImg;
    [SerializeField] private Sprite unselected;
    [SerializeField] private Sprite selected;

    private void OnEnable()
    {
        CheckButton();

        controller.OnMarketplaceTabChange += TabChange;
    }

    private void OnDisable()
    {
        controller.OnMarketplaceTabChange -= TabChange;
    }

    private void TabChange(object sender, EventArgs e)
    {
        CheckButton();
    }

    private void CheckButton()
    {
        tabImg.sprite = controller.CurrentMarketplaceTab == tab ? selected : unselected;
        contentObj.SetActive(controller.CurrentMarketplaceTab == tab);
    }

    public void ChangeTab() => controller.CurrentMarketplaceTab = tab;
}
