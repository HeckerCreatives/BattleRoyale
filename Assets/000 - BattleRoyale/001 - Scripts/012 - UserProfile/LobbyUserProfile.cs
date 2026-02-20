using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class LobbyUserProfile : MonoBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private LobbyController lobbyController;

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
    [SerializeField] private TextMeshProUGUI winProfileTMP;
    [SerializeField] private TextMeshProUGUI lossProfileTMP;
    [SerializeField] private TextMeshProUGUI playtimeProfileTMP;

    [Header("GUEST ACCOUNT")]
    [SerializeField] private GameObject registerGuestObj;
    [SerializeField] private GameObject registerGuestBtn;
    [SerializeField] private TMP_InputField guestUsernameTMP;
    [SerializeField] private TMP_InputField guestEmailTMP;
    [SerializeField] private TMP_InputField guestPasswordTMP;
    [SerializeField] private TMP_InputField guestConfirmPasswordTMP;

    [Header("ENERGY")]
    [SerializeField] private TextMeshProUGUI energyTMP;

    private void OnEnable()
    {
        registerGuestBtn.SetActive(userData.IsGuest);

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
        playtimeProfileTMP.text = $"{GameManager.Instance.GetHourDecimal(userData.GameDetails.playtime)}";
        winProfileTMP.text = $"{userData.GameDetails.win:n0}";
        lossProfileTMP.text = $"{userData.GameDetails.loss:n0}";

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

    public void GuestBind()
    {
        if (guestUsernameTMP.text == "")
        {
            GameManager.Instance.NotificationController.ShowError("Please enter your username first and try again!", null);
            return;
        }
        else if (guestPasswordTMP.text == "")
        {
            GameManager.Instance.NotificationController.ShowError("Please enter your password first and try again!", null);
            return;
        }
        else if (guestConfirmPasswordTMP.text == "")
        {
            GameManager.Instance.NotificationController.ShowError("Please enter confirm password first and try again!", null);
            return;
        }
        else if (guestPasswordTMP.text != guestConfirmPasswordTMP.text)
        {
            GameManager.Instance.NotificationController.ShowError("Password does not match confirm password!", null);
            return;
        }
        else if (guestEmailTMP.text == "")
        {
            GameManager.Instance.NotificationController.ShowError("Please enter your email first and try again!", null);
            return;
        }
        else if (guestUsernameTMP.text.Length < 5 || guestUsernameTMP.text.Length > 15)
        {
            GameManager.Instance.NotificationController.ShowError("Minimum of 5 and maximum of 15 characters only for username! Please try again.", null);
            return;
        }
        else if (guestPasswordTMP.text.Length < 5 || guestPasswordTMP.text.Length > 20)
        {
            GameManager.Instance.NotificationController.ShowError("Minimum of 5 and maximum of 20 characters only for password! Please try again.", null);
            return;
        }
        else if (Regex.IsMatch(guestUsernameTMP.text, @"[^\w]"))
        {
            GameManager.Instance.NotificationController.ShowError("Username contains spaces or special characters.", null);
            return;
        }
        else if (Regex.IsMatch(guestPasswordTMP.text, @"[^a-zA-Z0-9\s\[\]@]"))
        {
            GameManager.Instance.NotificationController.ShowError("Password contains spaces or special characters (excluding [ ] and @).", null);
            return;
        }
        else if (!Regex.IsMatch(guestEmailTMP.text, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
        {
            GameManager.Instance.NotificationController.ShowError("The email is invalid! Please enter a valid email.", null);
            return;
        }

        GameManager.Instance.NoBGLoading.SetActive(true);

        StartCoroutine(GameManager.Instance.PostRequest("/auth/guestaccbind", "", new Dictionary<string, object>
        {
            { "username", guestUsernameTMP.text },
            { "password", guestPasswordTMP.text },
            { "email", guestEmailTMP.text },
        }, false, (response) =>
        {
            GameManager.Instance.NotificationController.ShowCongratsOk("You have successfully registered your guest account! You will now be logged out. Please relogin again using your credentials.", () =>
            {
                PlayerPrefs.DeleteKey("guestuname");
                PlayerPrefs.DeleteKey("guestpword");
                lobbyController.Logout(false);
            });

            registerGuestObj.SetActive(false);
            registerGuestBtn.SetActive(false);
        }, () =>
        {
            GameManager.Instance.NoBGLoading.SetActive(false);
        }));
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
    public int win;
    public int loss;
    public int playtime;
}