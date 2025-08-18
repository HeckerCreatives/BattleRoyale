using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class LobbyUserProfile : MonoBehaviour
{
    [SerializeField] private UserData userData;

    [Header("LOBBY")]
    [SerializeField] private TextMeshProUGUI usernameTMP;
    [SerializeField] private TextMeshProUGUI levelTMP;
    [SerializeField] private TextMeshProUGUI xpTMP;
    [SerializeField] private GameObject userDetailsObj;
    [SerializeField] private TextMeshProUGUI leaderboardValueTMP;
    [SerializeField] private TextMeshProUGUI coinsTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;

    [Header("PROFILE")]
    [SerializeField] private TextMeshProUGUI usernameProfileTMP;
    [SerializeField] private TextMeshProUGUI levelProfileTMP;
    [SerializeField] private TextMeshProUGUI xpProfileTMP;
    [SerializeField] private TextMeshProUGUI killProfileTMP;
    [SerializeField] private TextMeshProUGUI deathProfileTMP;
    [SerializeField] private TextMeshProUGUI rankProfileTMP;
    [SerializeField] private TextMeshProUGUI coinsProfileTMP;

    [Header("ENERGY")]
    [SerializeField] private TextMeshProUGUI energyTMP;

    private void OnEnable()
    {
        userData.OnLeaderboardPointsChange += LeaderboardPointsChange;
        userData.OnCoinsPointsChange += CoinsPointsChange;
        userData.OnTitleChange += TitleChange;
        userData.OnEnergyChange += EnergyChange;
    }

    private void OnDisable()
    {
        userData.OnLeaderboardPointsChange -= LeaderboardPointsChange;
        userData.OnCoinsPointsChange -= CoinsPointsChange;
        userData.OnTitleChange -= TitleChange;
        userData.OnEnergyChange -= EnergyChange;
    }

    private void EnergyChange(object sender, EventArgs e)
    {
        energyTMP.text = $"{userData.GameDetails.energy:n0} / 20";
    }

    private void TitleChange(object sender, EventArgs e)
    {
        TitleChecker();
    }

    private void LeaderboardPointsChange(object sender, EventArgs e)
    {
        leaderboardValueTMP.text = $"{userData.GameDetails.leaderboard:n0}";
    }

    private void CoinsPointsChange(object sender, EventArgs e)
    {
        coinsTMP.text = $"{userData.GameDetails.coins:n4}";
        coinsProfileTMP.text = $"{userData.GameDetails.coins:n4}";
    }

    public void SetData()
    {
        usernameTMP.text = userData.Username;
        levelTMP.text = $"{userData.GameDetails.level:n0}";
        xpTMP.text = $"{userData.GameDetails.xp:n0} / {80 * userData.GameDetails.level}";
        leaderboardValueTMP.text = $"{userData.GameDetails.leaderboard:n0}";
        coinsTMP.text = $"{userData.GameDetails.coins:n4}";

        usernameProfileTMP.text = userData.Username;
        levelProfileTMP.text = $"{userData.GameDetails.level:n0}";
        xpProfileTMP.text = $"{userData.GameDetails.xp:n0} / {80 * userData.GameDetails.level}";
        killProfileTMP.text = $"{userData.GameDetails.kill:n0}";
        deathProfileTMP.text = $"{userData.GameDetails.death:n0}";
        rankProfileTMP.text = $"{userData.GameDetails.userrank:n0}";
        coinsProfileTMP.text = $"{userData.GameDetails.coins:n4}";

        energyTMP.text = $"{userData.GameDetails.energy:n0} / 20";
    }

    public void TitleChecker()
    {
        if (userData.PlayerInventory.Count > 0)
        {
            var filteredItems = userData.PlayerInventory
                .Where(kvp => kvp.Value.type == "title" && kvp.Value.isEquipped == true)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (filteredItems.Count > 0)
                titleTMP.text = filteredItems.ElementAt(0).Value.itemname;
            else
                titleTMP.text = "";
        }
        else
            titleTMP.text = "";
    }
}

[System.Serializable]
public class GameUserDetails
{
    public int kill;
    public int death;
    public int level;
    public int xp;
    public int userrank;
    public int energy;
    public float energyresettime;
    public int leaderboard;
    public float coins;
}