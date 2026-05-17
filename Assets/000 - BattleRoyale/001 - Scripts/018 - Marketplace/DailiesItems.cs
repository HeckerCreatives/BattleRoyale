using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailiesItems : MonoBehaviour
{

    [SerializeField] private MarketItems items;
    [SerializeField] private UserData userData;
    [SerializeField] private LobbyQuestController lobbyController;

    [Space]
    [SerializeField] private float imageSizeIcon = 0.2f;
    [SerializeField] private bool useItemResize;

    [Space]
    [SerializeField] private TextMeshProUGUI nameTMP;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private Button getRewardsBtn;
    [SerializeField] private Button getRewardsParentBtn;

    [Header("DEBUGGER")]
    [SerializeField] private MarketplaceController controller;
    [SerializeField] private string _id;
    [SerializeField] private string owner;
    [SerializeField] private string itemid;
    [SerializeField] private string itemname;
    [SerializeField] private string type;
    [SerializeField] private bool isClaimed;

    private void OnEnable()
    {
        nameTMP.text = items.ItemName;

        CheckData();
    }

    private void Update()
    {
        CheckData();
    }

    private void CheckData()
    {
        if (isClaimed)
        {
            timerTMP.text = GameManager.Instance.GetHourMinuteSecondsTime(controller.AdsTimer);

            getRewardsBtn.interactable = false;
            getRewardsParentBtn.interactable = false;
        }
        else
        {
            timerTMP.text = "00 : 00 : 00";

            if (GameManager.Instance.AdsManager.AdsInitialized && GameManager.Instance.AdsManager.RewardAdsAvailable)
            {
                getRewardsBtn.interactable = true;
                getRewardsParentBtn.interactable = true;
            }
            else
            {
                getRewardsBtn.interactable = false;
                getRewardsParentBtn.interactable = false;
            }
        }
    }

    public void InitializedData(string _id, string owner, string itemid, string itemname, string type, bool isClaimed)
    {
        this._id = _id;
        this.owner = owner;
        this.itemid = itemid;
        this.itemname = itemname;
        this.type = type;
        this.isClaimed = isClaimed;
    }

    public void GetRewards()
    {
        if (isClaimed) return;

        if (!GameManager.Instance.AdsManager.AdsInitialized || !GameManager.Instance.AdsManager.RewardAdsAvailable)
        {
            GameManager.Instance.NotificationController.ShowError("There's no ads available! Please try again later", null);
            return;
        }

        Debug.Log("GETTING REWARDS");

        GameManager.Instance.AdsManager.ShowRewardAd(items, _id, () =>
        {
            if (items.ItemType == "energy")
            {
                isClaimed = true;
                userData.GameDetails.energy += items.Consumable;
                userData.EnergyChangeFireEvent();
                StartCoroutine(lobbyController.RefreshQuestAfterClaim(false, false, false, () => GameManager.Instance.NoBGLoading.SetActive(false)));
            }
        });
    }
}
