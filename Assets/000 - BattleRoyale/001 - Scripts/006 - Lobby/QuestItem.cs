using System.Collections.Generic;
using System;
using System.Text;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestItem : MonoBehaviour
{
    [Serializable]
    private class QuestTypeSpriteMap
    {
        public string questType;
        public Sprite sprite;
    }

    [Header("Optional Labels")]
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI descriptionTMP;
    [SerializeField] private GameObject claimIndicator;

    [Header("Quest Type Logo")]
    [SerializeField] private Image logoImage;
    [SerializeField] private Sprite defaultLogoSprite;
    [SerializeField] private List<QuestTypeSpriteMap> questTypeSprites = new List<QuestTypeSpriteMap>();

    [SerializeField] private TextMeshProUGUI totalProgress;
    [SerializeField] private Slider progress;
    [SerializeField] private Button claimBtn;
    [SerializeField] private Button viewRewardsBtn;

    [Serializable]
    public class QuestReward
    {
        public string type;
        public int amount;
        public string itemid;
    }

    public static readonly Dictionary<string, string> RewardTypeLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "leaderboard", "Leaderboard Points" },
        { "exp",         "Experience"         },
    };

    [Header("Reward Item Lookup")]
    [SerializeField] private List<MarketItems> rewardMarketItems;

    private Dictionary<string, Sprite> _questTypeSpriteCache;
    private Dictionary<string, MarketItems> _marketItemCache;
    private string _questRecordId;
    private Action<string> _onClaimPressed;
    private List<QuestReward> _rewards;

    public string QuestRecordId => _questRecordId;
    public bool IsClaimable { get; private set; }
    public bool IsClaimed { get; private set; }

    private void Awake()
    {
        BuildQuestTypeSpriteCacheIfNeeded();

        if (claimBtn != null)
            claimBtn.onClick.AddListener(OnClickClaim);

        if (viewRewardsBtn != null)
            viewRewardsBtn.onClick.AddListener(OnClickViewRewards);

        ApplyClaimVisualState();
    }

    private void OnEnable()
    {
        ApplyClaimVisualState();
    }

    private void OnDestroy()
    {
        if (claimBtn != null)
            claimBtn.onClick.RemoveListener(OnClickClaim);

        if (viewRewardsBtn != null)
            viewRewardsBtn.onClick.RemoveListener(OnClickViewRewards);
    }

    public void SetData(
        string questRecordId,
        string questType,
        string title,
        string description,
        int currentProgress,
        int targetProgress,
        bool isClaimed,
        bool isCompleted,
        Action<string> onClaimPressed,
        List<QuestReward> rewards = null,
        List<MarketItems> marketItems = null)
    {
        _questRecordId = questRecordId;
        _onClaimPressed = onClaimPressed;
        _rewards = rewards;

        // Rebuild cache from passed list (takes priority over Inspector field)
        if (marketItems != null)
        {
            _marketItemCache = new Dictionary<string, MarketItems>(marketItems.Count, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < marketItems.Count; i++)
            {
                var mi = marketItems[i];
                if (mi != null && !string.IsNullOrWhiteSpace(mi.ItemID))
                    _marketItemCache[mi.ItemID] = mi;
            }
        }
        else
        {
            _marketItemCache = null; // will lazy-build from Inspector field on first use
        }
        if (titleTMP != null) titleTMP.text = title;
        if (descriptionTMP != null) descriptionTMP.text = description;
        ApplyQuestTypeLogo(questType);

        int safeTarget = Mathf.Max(1, targetProgress);
        int clampedProgress = Mathf.Clamp(currentProgress, 0, safeTarget);

        if (progress != null)
        {
            progress.minValue = 0f;
            progress.maxValue = safeTarget;
            progress.value = clampedProgress;
        }

        if (totalProgress != null)
            totalProgress.text = $"{clampedProgress:n0} / {safeTarget:n0}";

        IsClaimed = isClaimed;
        IsClaimable = !isClaimed && (isCompleted || clampedProgress >= safeTarget);

        ApplyClaimVisualState();
    }

    private void ApplyQuestTypeLogo(string questType)
    {
        if (logoImage == null) return;
        BuildQuestTypeSpriteCacheIfNeeded();

        if (!string.IsNullOrWhiteSpace(questType) &&
            _questTypeSpriteCache != null &&
            _questTypeSpriteCache.TryGetValue(questType.Trim(), out Sprite cached))
            logoImage.sprite = cached;
        else
            logoImage.sprite = defaultLogoSprite;
    }

    private void BuildQuestTypeSpriteCacheIfNeeded()
    {
        if (_questTypeSpriteCache != null)
            return;

        int capacity = questTypeSprites != null ? questTypeSprites.Count : 0;
        _questTypeSpriteCache = new Dictionary<string, Sprite>(capacity, StringComparer.OrdinalIgnoreCase);

        if (questTypeSprites == null)
            return;

        for (int i = 0; i < questTypeSprites.Count; i++)
        {
            QuestTypeSpriteMap entry = questTypeSprites[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.questType) || entry.sprite == null)
                continue;

            _questTypeSpriteCache[entry.questType.Trim()] = entry.sprite;
        }
    }

    public void ResetClientSide()
    {
        if (progress != null)
            progress.value = 0f;

        if (totalProgress != null)
        {
            int target = progress != null ? Mathf.RoundToInt(progress.maxValue) : 1;
            totalProgress.text = $"0 / {target:n0}";
        }

        IsClaimed = false;
        IsClaimable = false;

        ApplyClaimVisualState();
    }

    private void OnClickClaim()
    {
        if (!IsClaimable || string.IsNullOrWhiteSpace(_questRecordId))
            return;

        IsClaimable = false;
        ApplyClaimVisualState();
        _onClaimPressed?.Invoke(_questRecordId);
    }

    private void OnClickViewRewards()
    {
        if (_rewards == null || _rewards.Count == 0)
        {
            GameManager.Instance.NotificationController.ShowError("No rewards available for this quest.", null);
            return;
        }

        var parts = new StringBuilder();
        for (int i = 0; i < _rewards.Count; i++)
        {
            var r = _rewards[i];
            if (r == null) continue;
            if (parts.Length > 0) parts.Append(", ");
            parts.Append("<color=#00FF00>").Append(GetRewardLabel(r)).Append(" x ").Append(r.amount).Append("</color>");
        }

        GameManager.Instance.NotificationController.ShowError($"You will get {parts} after you completed the challenge");
    }

    public string GetRewardLabel(QuestReward r)
    {
        if (r == null) return "Reward";

        if (RewardTypeLabels.TryGetValue(r.type ?? string.Empty, out string mapped))
            return mapped;

        if (!string.IsNullOrWhiteSpace(r.itemid))
        {
            BuildMarketItemCacheIfNeeded();
            if (_marketItemCache != null && _marketItemCache.TryGetValue(r.itemid, out MarketItems item))
                return item.ItemName;
        }

        return System.Globalization.CultureInfo.InvariantCulture.TextInfo
            .ToTitleCase((r.type ?? "Reward").ToLower());
    }

    private void BuildMarketItemCacheIfNeeded()
    {
        if (_marketItemCache != null) return;

        _marketItemCache = new Dictionary<string, MarketItems>(StringComparer.OrdinalIgnoreCase);
        if (rewardMarketItems == null) return;

        for (int i = 0; i < rewardMarketItems.Count; i++)
        {
            var item = rewardMarketItems[i];
            if (item != null && !string.IsNullOrWhiteSpace(item.ItemID))
                _marketItemCache[item.ItemID] = item;
        }
    }

    private void ApplyClaimVisualState()
    {
        if (claimBtn != null)
            claimBtn.interactable = IsClaimable;

        if (claimIndicator != null)
            claimIndicator.SetActive(IsClaimable);
    }
}
