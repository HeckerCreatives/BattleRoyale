using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketplaceItems : MonoBehaviour
{
    [SerializeField] private MarketItems items;
    [SerializeField] private UserData userData;

    [Space]
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private TextMeshProUGUI itemPrice;
    [SerializeField] private Image itemImage;
    [SerializeField] private GameObject pointsCurrency;
    [SerializeField] private GameObject coinsCurrency;

    [Space]
    [SerializeField] private TextMeshProUGUI descriptionItemName;
    [SerializeField] private TextMeshProUGUI descriptionItemDescription;
    [SerializeField] private TextMeshProUGUI descriptionItemPrice;
    [SerializeField] private Image descriptionItemImage;
    [SerializeField] private GameObject descriptionPointsCurrency;
    [SerializeField] private GameObject descriptionCoinsCurrency;
    [SerializeField] private Button descriptionBuyBtn;
    [SerializeField] private GameObject description;


    private void OnEnable()
    {
        itemName.text = items.ItemName;
        itemDescription.text = items.Description;
        itemPrice.text = items.Currency == "Coins" ? items.Price.ToString("n4") : items.Price.ToString("n0");

        if (items.ItemIcon != null)
        {
            itemImage.gameObject.SetActive(true);
            Vector2 originalSize = items.ItemIcon.rect.size;
            Vector2 newSize = originalSize * 0.2f;
            itemImage.rectTransform.sizeDelta = newSize;

            itemImage.sprite = items.ItemIcon;
        }
        else
        {
            if (itemImage != null)
                itemImage.gameObject.SetActive(false);
        }

        pointsCurrency.SetActive(items.Currency == "Points");
        coinsCurrency.SetActive(items.Currency == "Coins");
    }

    public void ShowDescription()
    {
        descriptionBuyBtn.onClick.RemoveAllListeners();

        descriptionItemName.text = items.ItemName;
        descriptionItemDescription.text = items.Description;
        descriptionItemPrice.text = items.Currency == "Coins" ? items.Price.ToString("n4") : items.Price.ToString("n0");

        if (items.ItemIcon != null)
        {
            Vector2 originalSize = items.ItemIcon.rect.size;
            Vector2 newSize = originalSize * 0.7f;
            descriptionItemImage.rectTransform.sizeDelta = newSize;
            descriptionItemImage.sprite = items.ItemIcon;
        }

        descriptionPointsCurrency.SetActive(items.Currency == "Points");
        descriptionCoinsCurrency.SetActive(items.Currency == "Coins");

        MarketItems tempitem = items;
        UserData tempuser = userData;

        descriptionBuyBtn.onClick.AddListener(() => StartCoroutine(BuyItem(tempitem, tempuser)));

        description.SetActive(true);
    }

    IEnumerator BuyItem(MarketItems tempitem, UserData tempuser)
    {
        if (tempitem.Currency == "Points")
        {
            if (tempuser.GameDetails.leaderboard < tempitem.Price)
            {
                GameManager.Instance.NotificationController.ShowError($"Not enough points to buy this item ({tempitem.ItemName})", null);
                yield break;
            }
        }
        else if (tempitem.Currency == "Coins")
        {
            if (tempuser.GameDetails.coins < tempitem.Price)
            {
                GameManager.Instance.NotificationController.ShowError($"Not enough coins to buy this item ({tempitem.ItemName})", null);
                yield break;
            }
        }

        if (tempitem.ItemType == "title")
        {
            if (userData.PlayerInventory.ContainsKey(tempitem.ItemID))
            {
                GameManager.Instance.NotificationController.ShowError($"You already bought this title");
                yield break;
            }
        }

        GameManager.Instance.NoBGLoading.SetActive(true);

        StartCoroutine(GameManager.Instance.PostRequest("/marketplace/buy", "", new Dictionary<string, object>
        {
            { "itemid", tempitem.ItemID },
            { "quantity", 1 }
        }, false, (response) =>
        {
            if (response != null)
            {
                try
                {
                    if (userData.PlayerInventory.ContainsKey(tempitem.ItemID))
                    {
                        userData.PlayerInventory[tempitem.ItemID].quantity += 1;
                    }
                    else
                    {
                        userData.PlayerInventory.Add(tempitem.ItemID, new Inventory
                        {
                            itemid = tempitem.ItemID,
                            itemname = tempitem.ItemName,
                            quantity = 1,
                            isEquipped = false,
                            canUse = false,
                            canEquip = false,
                            canSell = false,
                            type = tempitem.ItemType
                        });
                    }

                    if (tempitem.Currency == "Points")
                        userData.AddLeaderboardPoints(-Convert.ToInt32(tempitem.Price));
                    else if (tempitem.Currency == "Coins")
                        userData.AddCoins(-Convert.ToInt32(tempitem.Price));

                    GameManager.Instance.NotificationController.ShowCongratsOk($"You've successfully bought {tempitem.ItemName} for {tempitem.Price} {tempitem.Currency}", null);

                    GameManager.Instance.NoBGLoading.SetActive(false);
                }
                catch(Exception e)
                {
                    GameManager.Instance.NotificationController.ShowError($"There's a problem with the server! Please try again later. Error: {response.ToString()}");
                }
            }
        }, () =>
        {
            GameManager.Instance.NoBGLoading.SetActive(false);
        }));

        yield return null;
    }
}
