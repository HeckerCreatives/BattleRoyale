using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public enum MarketplaceTab
{
    DAILIES,
    USABLEITEMS,
    TITLES,
    SKINCOLOR,
    HAIRCOLOR,
    HAIRSTYLES,
    COMINGSOON
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

    public float AdsTimer { get => adsTimer; }
    public bool InitializedAdsTimer { get => initializedAdsTimer; }

    //  =============

    [SerializeField] private UserData userData;

    [Space]
    [SerializeField] private TextMeshProUGUI points;
    [SerializeField] private TextMeshProUGUI coins;
    [SerializeField] private GameObject marketplaceObj;

    [Space]
    [SerializeField] private List<DailiesItems> dailiesItems;

    [Header("DEBUGGER")]
    [SerializeField] private MarketplaceTab currentMarketplaceTab;
    [SerializeField] private float adsTimer;
    [SerializeField] private bool initializedAdsTimer;

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

    public IEnumerator GetAdsData()
    {
        //  API HERE
        yield return StartCoroutine(GameManager.Instance.GetRequest("/ads/getadsdata", "", false, (response) =>
        {
            try
            {
                Ads tempads = JsonConvert.DeserializeObject<Ads>(response.ToString());

                for (int a = 0; a < tempads.ads.Count; a++)
                {
                    dailiesItems[a].InitializedData(tempads.ads[a]._id, tempads.ads[a].owner, tempads.ads[a].itemid, tempads.ads[a].itemname, tempads.ads[a].type, tempads.ads[a].isClaimed);
                }

                adsTimer = tempads.time;

                initializedAdsTimer = true;
            }
            catch (Exception ex)
            {
                GameManager.Instance.SceneController.StopLoading();
                Debug.Log(ex.ToString());
                GameManager.Instance.SocketMngr.Socket.Disconnect();
                GameManager.Instance.NotificationController.ShowError("There's a problem with the server! Please try again later. 3", null);
                GameManager.Instance.SceneController.CurrentScene = "Login";
            }
        }, () =>
        {
            GameManager.Instance.SceneController.StopLoading();
            GameManager.Instance.SocketMngr.Socket.Disconnect();
            GameManager.Instance.NotificationController.ShowError("There's a problem with your network connection! Please try again later. 4", null);
            GameManager.Instance.SceneController.CurrentScene = "Login";
        }));

        yield return null;
    }

    public void OpenMarketplace()
    {
        points.text = userData.GameDetails.leaderboard.ToString("n0");
        coins.text = userData.GameDetails.coins.ToString("n4");
        CurrentMarketplaceTab = MarketplaceTab.DAILIES;
        marketplaceObj.SetActive(true);
    }

    private void Update()
    {
        if (!InitializedAdsTimer) return;

        if (adsTimer > 0)
            adsTimer -= Time.deltaTime;
        else
            adsTimer = 0;
    }
}

[System.Serializable]
public class Ads
{
    public float time;
    public List<AdsData> ads;
}

[System.Serializable]
public class AdsData
{
    public string _id;
    public string owner;
    public string itemid;
    public string itemname;
    public string type;
    public bool isClaimed;
}