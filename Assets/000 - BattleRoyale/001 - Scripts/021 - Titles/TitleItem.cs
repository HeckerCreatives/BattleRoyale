using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleItem : MonoBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private MarketItems items;

    [Space]
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private Button changeBtn;
    [SerializeField] private Button parentChangeBtn;
    [SerializeField] private GameObject lockedObj;

    private void OnEnable()
    {
        titleTMP.text = items.ItemName;

        LockChecker();

        userData.OnTitleChange += TitleChange;
    }

    private void OnDisable()
    {
        userData.OnTitleChange -= TitleChange;
    }

    private void TitleChange(object sender, EventArgs e)
    {
        LockChecker();
    }

    private void LockChecker()
    {
        if (userData.PlayerInventory.Count <= 0)
        {
            parentChangeBtn.interactable = false;
            changeBtn.gameObject.SetActive(false);
            lockedObj.SetActive(true);
            return;
        }

        if (userData.PlayerInventory.ContainsKey(items.ItemID))
        {
            lockedObj.SetActive(false);
            changeBtn.gameObject.SetActive(true);

            if (userData.PlayerInventory[items.ItemID].isEquipped)
            {
                changeBtn.interactable = false;
                parentChangeBtn.interactable = false;
            }
            else
            {
                changeBtn.interactable = true;
                parentChangeBtn.interactable = true;
            }
        }
        else
        {
            parentChangeBtn.interactable = false;
            changeBtn.gameObject.SetActive(false);
            lockedObj.SetActive(true);
        }
    }

    public void EquipTitle()
    {
        GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to equip this title?", () =>
        {
            MarketItems tempitems = items;
            UserData tempuser = userData;

            GameManager.Instance.NoBGLoading.SetActive(true);

            StartCoroutine(GameManager.Instance.PostRequest("/marketplace/equip", "", new Dictionary<string, object>
            {
                { "itemid", tempitems.ItemID },
                { "equip", true }
            }, false, (response) =>
            {
                var filteredItems = userData.PlayerInventory
                .Where(kvp => kvp.Value.type == "title" && kvp.Value.isEquipped == true)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                filteredItems.ElementAt(0).Value.isEquipped = false;

                tempuser.PlayerInventory[filteredItems.ElementAt(0).Value.itemid] = filteredItems.ElementAt(0).Value;

                tempuser.PlayerInventory[tempitems.ItemID].isEquipped = true;

                tempuser.TitleChangeFireEvent();

            }, () => { }));

        }, null);
    }
}
