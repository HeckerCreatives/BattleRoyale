using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class InventoryTabItem : MonoBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private InventoryController controller;
    [SerializeField] private InventoryTab tab;

    [Space]
    [SerializeField] private List<MarketItems> marketItems;

    [Space]
    [SerializeField] private GameObject loader;

    [Space]
    [SerializeField] private List<string> filters;

    [Space]
    [SerializeField] private GameObject contentObj;
    [SerializeField] private Transform itemListContentTF;
    [SerializeField] private GameObject itemListObj;
    [SerializeField] private GameObject noItemsYetObj;
    [SerializeField] private GameObject inventoryItem;

    [Space]
    [SerializeField] private Image tabImg;
    [SerializeField] private Sprite unselected;
    [SerializeField] private Sprite selected;

    //  ==================

    Coroutine consumables;

    private void OnEnable()
    {
        CheckButton();
        ItemChecker();

        controller.OnInventoryTabChange += TabChange;
    }

    private void OnDisable()
    {
        controller.OnInventoryTabChange -= TabChange;
    }

    private void TabChange(object sender, EventArgs e)
    {
        CheckButton();
        ItemChecker();
    }

    private void CheckButton()
    {
        tabImg.sprite = controller.CurrentInventoryTab == tab ? selected : unselected;
        contentObj.SetActive(controller.CurrentInventoryTab == tab);
    }

    private void ItemChecker()
    {
        if (controller.CurrentInventoryTab == tab)
        {
            if (consumables != null) StopCoroutine(consumables);

            consumables = StartCoroutine(ShowItems(filters));
        }
    }

    IEnumerator ShowItems(List<string> filters)
    {
        loader.SetActive(true);
        itemListObj.SetActive(false);

        if (userData.PlayerInventory.Count <= 0)
        {
            noItemsYetObj.SetActive(true);
            loader.SetActive(false);
            yield break;
        }


        // Filter based on the filters list
        var filteredItems = userData.PlayerInventory
            .Where(kvp => filters.Contains(kvp.Value.type))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        if (filteredItems.Count <= 0)
        {
            noItemsYetObj.SetActive(true);
            loader.SetActive(false);
            yield break;
        }

        // Clear old items
        while (itemListContentTF.childCount > 0)
        {
            Destroy(itemListContentTF.GetChild(0).gameObject);
            yield return null;
        }

        // Example: loop through filtered items
        foreach (var kvp in filteredItems)
        {
            GameObject tempitem = Instantiate(inventoryItem, itemListContentTF);

            if (tempitem.TryGetComponent(out InventoryConsumableItem consumableItem))
            {
                MarketItems tempmarketitem = marketItems.Find(filter => filter.ItemID == kvp.Key);

                if (tempmarketitem == null)
                {
                    Destroy(tempitem);
                    continue;
                }

                consumableItem.InitializeItem(tempmarketitem, kvp.Value.quantity);
            }

            yield return null;
        }

        loader.SetActive(false);
        itemListObj.SetActive(true);
        noItemsYetObj.SetActive(false);
    }


    public void ChangeTab() => controller.CurrentInventoryTab = tab;
}
