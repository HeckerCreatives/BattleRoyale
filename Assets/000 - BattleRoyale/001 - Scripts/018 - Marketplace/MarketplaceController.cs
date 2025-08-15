using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public enum MarketplaceTab
{
    HAIRSTYLES,
    SKINS,
    WEAPONS,
    USABLEITEMS,
    TITLES
}

public class MarketplaceController : MonoBehaviour
{
    private event EventHandler MarketplaceTabChange;
    public event EventHandler OnMarketplaceTabChange
    {
        add
        {
            if (MarketplaceTabChange == null || !MarketplaceTabChange.GetInvocationList().Contains(value))
                MarketplaceTabChange += value;
        }
        remove { MarketplaceTabChange -= value; }
    }
    public MarketplaceTab CurrentMarketplaceTab
    {
        get => currentMarketplaceTab;
        set
        {
            currentMarketplaceTab = value;
            MarketplaceTabChange?.Invoke(this, EventArgs.Empty);
        }
    }

    //  =============

    [SerializeField] private UserData userData;

    [Space]
    [SerializeField] private TextMeshProUGUI points;
    [SerializeField] private TextMeshProUGUI coins;
    [SerializeField] private GameObject marketplaceObj;

    [Header("DEBUGGER")]
    [SerializeField] private MarketplaceTab currentMarketplaceTab;

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

    public void OpenMarketplace()
    {
        points.text = userData.GameDetails.leaderboard.ToString("n0");
        coins.text = userData.GameDetails.coins.ToString("n4");
        CurrentMarketplaceTab = MarketplaceTab.USABLEITEMS;
        marketplaceObj.SetActive(true);
    }
}
