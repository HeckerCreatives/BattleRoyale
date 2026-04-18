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
            Debug.Log("REWARDS ADS AVAILABLE");
        });
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
                }, false, (response) =>
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
