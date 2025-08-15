using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public enum InventoryTab
{
    HAIRSTYLES,
    SKINS,
    WEAPONS,
    USABLEITEMS
}

public class InventoryController : MonoBehaviour
{
    private event EventHandler InventoryTabChange;
    public event EventHandler OnInventoryTabChange
    {
        add
        {
            if (InventoryTabChange == null || !InventoryTabChange.GetInvocationList().Contains(value))
                InventoryTabChange += value;
        }
        remove { InventoryTabChange -= value; }
    }
    public InventoryTab CurrentInventoryTab
    {
        get => currentInventoryObjTab;
        set
        {
            currentInventoryObjTab = value;
            InventoryTabChange?.Invoke(this, EventArgs.Empty);
        }
    }

    //  =============

    [SerializeField] private UserData userData;
    [SerializeField] private LobbyController lobbyController;

    [Space]
    [SerializeField] private TextMeshProUGUI points;
    [SerializeField] private TextMeshProUGUI coins;
    [SerializeField] private GameObject InventoryObj;

    [Header("DEBUGGER")]
    [SerializeField] private InventoryTab currentInventoryObjTab;

    private void OnEnable()
    {
        userData.OnLeaderboardPointsChange += LeaderboardPointsChange;
        userData.OnCoinsPointsChange += CoinsPointsChange;
    }

    private void OnDisable()
    {
        userData.OnLeaderboardPointsChange -= LeaderboardPointsChange;
        userData.OnCoinsPointsChange -= CoinsPointsChange;
    }

    private void LeaderboardPointsChange(object sender, EventArgs e)
    {
        points.text = $"{userData.GameDetails.leaderboard:n0}";
    }

    private void CoinsPointsChange(object sender, EventArgs e)
    {
        coins.text = $"{userData.GameDetails.coins:n4}";
    }


    public void OpenInventory()
    {
        points.text = userData.GameDetails.leaderboard.ToString("n0");
        coins.text = userData.GameDetails.coins.ToString("n4");
        CurrentInventoryTab = InventoryTab.HAIRSTYLES;
        InventoryObj.SetActive(true);
    }

    public IEnumerator GetInventory()
    {
        yield return StartCoroutine(GameManager.Instance.GetRequest("/marketplace/inventory", "", false, (response) =>
        {
            try
            {
                Dictionary<string, Inventory> tempinventory = JsonConvert.DeserializeObject<Dictionary<string, Inventory>>(response.ToString());

                userData.PlayerInventory = tempinventory;

                lobbyController.TitleChecker();
            }       
            catch (Exception e)
            {
                GameManager.Instance.NotificationController.ShowError($"There's a problem with the server! Please try again later. Error: {response.ToString()}");
            }
        }, () =>{ }));
    }
}
