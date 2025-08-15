using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryConsumableItem : MonoBehaviour
{
    [SerializeField] private UserData userData;

    [Space]
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private TextMeshProUGUI itemCountTMP;
    [SerializeField] private Image itemImage;

    [Header("DEBUGGER")]
    [SerializeField] private MarketItems items;
    [SerializeField] private int itemCount;

    private void OnEnable()
    {
        itemName.text = items.ItemName;
        itemDescription.text = items.Description;

        if (items.ItemIcon != null)
        {
            itemImage.gameObject.SetActive(true);
            Vector2 originalSize = items.ItemIcon.rect.size;
            Vector2 newSize = originalSize * 0.2f;
            itemImage.rectTransform.sizeDelta = newSize;

            itemImage.sprite = items.ItemIcon;
        }
        else
            itemImage.gameObject.SetActive(false);
    }

    public void InitializeItem(MarketItems item, float itemCount)
    {
        items = item;

        if (itemCount > 1)
        {
            itemCountTMP.text = $"x{itemCount}";
            itemCountTMP.gameObject.SetActive(true);
            return;
        }

        itemCountTMP.gameObject.SetActive(false);
    }

    public void Use()
    {
        MarketItems tempitem = items;
        UserData tempuser = userData;

        UseItem(tempitem, tempuser);
    }

    public void Sell()
    {
        MarketItems tempitem = items;
        UserData tempuser = userData;

        SellItem(tempitem, tempuser);
    }

    private void UseItem(MarketItems tempitem, UserData tempuser)
    {
        GameManager.Instance.NotificationController.ShowConfirmation($"Are you sure you want to use this item {tempitem.ItemName}?", () =>
        {
            if (tempitem.ItemType == "potion" && userData.PlayerItemEffects.Count > 0)
            {
                GameManager.Instance.NotificationController.ShowError("You already have an active xp potion!", null);
                return;
            }

            GameManager.Instance.NoBGLoading.SetActive(true);

            StartCoroutine(GameManager.Instance.PostRequest("/marketplace/use", "", new Dictionary<string, object>
            {
                { "itemid", tempitem.ItemID },
                { "quantity", 1 }
            }, false, (response) =>
            {
                if (tempuser.PlayerInventory[tempitem.ItemID].quantity <= 1)
                {
                    tempuser.PlayerInventory.Remove(tempitem.ItemID);
                    Destroy(gameObject);
                }
                else
                {
                    tempuser.PlayerInventory[tempitem.ItemID].quantity -= 1;

                    if (itemCount > 1)
                    {
                        itemCountTMP.text = $"x{itemCount}";
                        itemCountTMP.gameObject.SetActive(true);
                    }
                    else
                        itemCountTMP.gameObject.SetActive(false);
                }

                if (tempitem.ItemType == "potion")
                {
                    tempuser.PlayerItemEffects.Add(tempitem.ItemID, new ItemEffects
                    {
                        itemid = tempitem.ItemID,
                        itemname = tempitem.ItemName,
                        multiplier = tempitem.Consumable,
                        type = tempitem.ItemType,
                        timeRemaining = 86400
                    });
                }
                else if (tempitem.ItemType == "energy")
                {
                    tempuser.GameDetails.energy += tempitem.Consumable;
                    userData.EnergyChangeFireEvent();
                }

                GameManager.Instance.NotificationController.ShowCongratsOk($"You have successfully used {tempitem.ItemName}", null);

            }, () => { }));

        }, null);
    }

    private void SellItem(MarketItems tempitem, UserData tempuser)
    {
        GameManager.Instance.NotificationController.ShowConfirmation($"Are you sure you want to sell this item {tempitem.ItemName}?", () =>
        {
            GameManager.Instance.NoBGLoading.SetActive(true);

            StartCoroutine(GameManager.Instance.PostRequest("/marketplace/sell", "", new Dictionary<string, object>
            {
                { "itemid", tempitem.ItemID },
                { "quantity", 1 }
            }, false, (response) =>
            {
                if (tempuser.PlayerInventory[tempitem.ItemID].quantity <= 1)
                {
                    tempuser.PlayerInventory.Remove(tempitem.ItemID);
                    Destroy(gameObject);
                }
                else
                {
                    tempuser.PlayerInventory[tempitem.ItemID].quantity -= 1;

                    if (itemCount > 1)
                    {
                        itemCountTMP.text = $"x{itemCount}";
                        itemCountTMP.gameObject.SetActive(true);
                    }
                    else
                        itemCountTMP.gameObject.SetActive(false);
                }

                if (tempitem.Currency == "Points")
                {
                    int pointsToAdd = Mathf.CeilToInt(tempitem.Price * 0.5f);
                    tempuser.AddLeaderboardPoints(pointsToAdd);
                }
                else if (tempitem.Currency == "Coins")
                {
                    float coinsToAdd = tempitem.Price * 0.5f;
                    tempuser.AddCoins(coinsToAdd);
                }

                GameManager.Instance.NotificationController.ShowCongratsOk($"You have successfully sold {tempitem.ItemName}", null);

            }, () => { }));

        }, null);
    }
}
