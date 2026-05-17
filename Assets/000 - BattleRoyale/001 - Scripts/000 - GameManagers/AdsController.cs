using GoogleMobileAds;
using GoogleMobileAds.Api;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdsController : MonoBehaviour
{
    public bool AdsInitialized { get => adsInitialized; }

    public bool RewardAdsAvailable { get => rewardAdsAvailable; }

    [Header("DEBUGGER")]
    [SerializeField] private bool adsInitialized;
    [SerializeField] private bool rewardAdsAvailable;

    private RewardedAd rewardedAd;

    public void Start()
    {
        Debug.Log("MOBILE ADS STARTING TO INITIALIZE");
        // Initialize Google Mobile Ads Unity Plugin.
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            adsInitialized = true;
            Debug.Log("ADS INITIALIZED");
        });
    }

    public void LoadRewardedAd()
    {
        var adRequest = new AdRequest();

        Debug.Log("LOAD REWARD ADS");

        #if UNITY_EDITOR

        RewardedAd.Load("ca-app-pub-3940256099942544/5224354917", adRequest,
        (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                rewardAdsAvailable = false;
                Debug.Log("REWARDS ADS NOT AVAILABLE");
                return;
            }

            rewardedAd = ad;
            rewardAdsAvailable = true;
            Debug.Log("REWARDS EDITOR ADS AVAILABLE");
        });

#elif UNITY_ANDROID
        RewardedAd.Load("ca-app-pub-7002238140224739/5485742495", adRequest,
        (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                rewardAdsAvailable = false;
                Debug.Log("REWARDS ADS NOT AVAILABLE");
                return;
            }

            rewardedAd = ad;
            rewardAdsAvailable = true;
            Debug.Log("REWARDS ADNROID ADS AVAILABLE");
        });
#elif UNITY_IOS

        RewardedAd.Load("ca-app-pub-7002238140224739/2392675299", adRequest,
        (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                rewardAdsAvailable = false;
                Debug.Log("REWARDS ADS NOT AVAILABLE");
                return;
            }

            rewardedAd = ad;
            rewardAdsAvailable = true;
            Debug.Log("REWARDS IOS ADS AVAILABLE");
        });
#endif
    }

    public void ShowRewardAd(MarketItems item, string adsid, Action finalAction)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                GameManager.Instance.NoBGLoading.SetActive(true);

                StartCoroutine(GameManager.Instance.PostRequest("/ads/givereward", "", new Dictionary<string, object>
                {
                    { "adsid", adsid },
                    { "type", item.ItemType },
                    { "itemid", item.ItemID }
                }, true, (response) =>
                {
                    try
                    {
                        finalAction?.Invoke();

                        GameManager.Instance.NotificationController.ShowCongratsOk($"You have successfully claimed your reward: {item.ItemName}", null);
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
            });

            // 🔁 IMPORTANT: Load next ad after showing
            LoadRewardedAd();
        }
        else
        {
            Debug.Log("Ad not ready");
        }
    }
}
