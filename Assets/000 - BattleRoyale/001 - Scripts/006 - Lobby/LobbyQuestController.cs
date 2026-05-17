using GoogleMobileAds.Api;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public enum QuestTabState
{
    DAILIES,
    EVENTS
}
public class LobbyQuestController : MonoBehaviour
{
    private event EventHandler QuestTabChange;
    public event EventHandler OnQuestTabChange
    {
        add
        {
            if (QuestTabChange == null || !QuestTabChange.GetInvocationList().Contains(value))
                QuestTabChange += value;
        }
        remove { QuestTabChange -= value; }
    }

    public QuestTabState CurrentQuestTab
    {
        get => currentQuestTab;
        set
        {
            currentQuestTab = value;
            QuestTabChange?.Invoke(this, EventArgs.Empty);
        }
    }

    [SerializeField] private UserData userData;
    [SerializeField] private LobbyController lobbyController;
    [SerializeField] private InventoryController inventoryController;

    [Header("Quest UI")]
    [SerializeField] private List<QuestItem> questItems;
    [SerializeField] private List<MarketItems> rewardMarketItems;

    [Space]
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private GameObject questObj;

    [Space]
    [SerializeField] private GameObject haveQuestIndicator;
    [SerializeField] private GameObject haveDailiesQuestIndicator;

    [Header("Runtime")]
    [SerializeField] private bool canCount;
    [SerializeField] private float currentTimer;
    [SerializeField] private QuestTabState currentQuestTab;

    private bool _isFetching;
    private bool _isClaiming;
    private int _lastDisplayedSecond = -1;
    private readonly List<LobbyQuestData> _loadedQuests = new List<LobbyQuestData>();

    private const float DailyResetSeconds = 86400f;

    [Serializable]
    private class LobbyQuestResponse
    {
        public int resettime;
        public List<LobbyQuestData> quests;
    }

    [Serializable]
    private class LobbyQuestData
    {
        [JsonProperty("_id")] public string id;
        public LobbyQuestDefinition quest;
        public int progress;
        public bool isCompleted;
        public bool isClaimed;
        public bool isSkipped;
    }

    [Serializable]
    private class LobbyQuestDefinition
    {
        public string type;
        public string title;
        public string description;
        public int target;
        public List<QuestItem.QuestReward> rewards;
    }

    private void Update()
    {
        if (!canCount)
        {
            if (_lastDisplayedSecond != -1)
            {
                _lastDisplayedSecond = -1;
                if (timerTMP != null) timerTMP.text = "00 : 00 : 00";
            }
            return;
        }

        if (currentTimer <= 0)
        {
            ResetQuestsClientSideOnly();
            currentTimer = DailyResetSeconds;
        }
        else
            currentTimer -= Time.unscaledDeltaTime;

        int sec = (int)currentTimer;
        if (sec != _lastDisplayedSecond)
        {
            _lastDisplayedSecond = sec;
            if (timerTMP != null)
                timerTMP.text = GameManager.Instance.GetHourMinuteSecondsTime(currentTimer);
        }
    }

    public IEnumerator FetchQuestDataRoutine(bool stayActiveLoading = false)
    {
        _isFetching = true;
        yield return StartCoroutine(GameManager.Instance.GetRequest("/quest/getquests", "", stayActiveLoading, (response) =>
        {
            Debug.Log(response);
            try
            {
                LobbyQuestResponse questResponse = JsonConvert.DeserializeObject<LobbyQuestResponse>(response.ToString());
                ApplyQuestResponse(questResponse);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Quest parse failed: {ex}");
                SetIndicators(false);
            }
            finally
            {
                _isFetching = false;
            }
        },
        () =>
        {
            _isFetching = false;
            SetIndicators(false);
        }));
    }

    private void ApplyQuestResponse(LobbyQuestResponse questResponse)
    {
        _loadedQuests.Clear();
        if (questResponse != null && questResponse.quests != null)
            _loadedQuests.AddRange(questResponse.quests);

        currentTimer = Mathf.Max(0f, questResponse != null ? questResponse.resettime : 0f);
        canCount = true;

        bool hasClaimable = false;

        for (int i = 0; i < questItems.Count; i++)
        {
            if (questItems[i] == null)
                continue;

            if (i >= _loadedQuests.Count)
            {
                questItems[i].gameObject.SetActive(false);
                continue;
            }

            var questData = _loadedQuests[i];
            var def = questData.quest;
            questItems[i].gameObject.SetActive(true);
            questItems[i].SetData(
                questData.id,
                def?.type ?? string.Empty,
                def?.title ?? "-",
                def?.description ?? "-",
                questData.progress,
                def?.target ?? 1,
                questData.isClaimed,
                questData.isCompleted,
                OnClaimQuestPressed,
                def?.rewards,
                rewardMarketItems
            );

            hasClaimable |= questItems[i].IsClaimable;
        }

        SetIndicators(hasClaimable);
    }

    private void OnClaimQuestPressed(string questRecordId)
    {
        if (_isClaiming || string.IsNullOrWhiteSpace(questRecordId))
            return;

        StartCoroutine(ClaimQuestRoutine(questRecordId));
    }

    public IEnumerator RefreshQuestAfterClaim(bool shouldRefreshPlayerData = true, bool shouldRefreshInventory = true, bool stayAliveLoadingAfter = true, Action lastAction = null)
    {
        yield return StartCoroutine(FetchQuestDataRoutine(stayAliveLoadingAfter));

        if (shouldRefreshPlayerData)
            yield return StartCoroutine(lobbyController.RefreshUserData(stayAliveLoadingAfter));

        if (shouldRefreshInventory)
            yield return StartCoroutine(inventoryController.GetInventory(true));

        _isClaiming = false;
        lastAction?.Invoke();
    }

    private IEnumerator ClaimQuestRoutine(string questRecordId)
    {
        _isClaiming = true;

        Dictionary<string, object> body = new Dictionary<string, object>
        {
            { "questProgressId", questRecordId }
        };

        GameManager.Instance.NoBGLoading.SetActive(true);

        yield return StartCoroutine(GameManager.Instance.PostRequest("/quest/claimreward", "", body, true, (response) =>
        {
            StartCoroutine(RefreshQuestAfterClaim(true, true, true, async () =>
            {
                var parts = new System.Text.StringBuilder();

                foreach (var item in _loadedQuests)
                {
                    if (item.id != questRecordId) continue;

                    parts.Clear();

                    foreach (var rewards in item.quest.rewards)
                    {
                        if (rewards == null) continue;

                        string label = questItems.Count > 0 && questItems[0] != null
                            ? questItems[0].GetRewardLabel(rewards)
                            : QuestItem.RewardTypeLabels.TryGetValue(rewards.type ?? string.Empty, out string mapped)
                                ? mapped
                                : System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase((rewards.type ?? "Reward").ToLower());


                        if (parts.Length > 0) parts.Append(", ");

                        parts.Append("<color=#00FF00>").Append(label).Append(" x ").Append(rewards.amount).Append("</color>");

                        await Task.Yield();
                    }
                    await Task.Yield();
                }

                GameManager.Instance.NotificationController.ShowError($"You received {parts}!");

                GameManager.Instance.NoBGLoading.SetActive(false);
            }));
        },
        () =>
        {
            _isClaiming = false;
            RefreshClaimInteractsOnly();
        }));

        yield return null;
    }

    private void RefreshClaimInteractsOnly()
    {
        bool hasClaimable = false;
        for (int i = 0; i < questItems.Count; i++)
        {
            if (questItems[i] == null || !questItems[i].gameObject.activeSelf)
                continue;

            hasClaimable |= questItems[i].IsClaimable;
        }

        SetIndicators(hasClaimable);
    }

    private void ResetQuestsClientSideOnly()
    {
        for (int i = 0; i < _loadedQuests.Count; i++)
        {
            _loadedQuests[i].progress = 0;
            _loadedQuests[i].isCompleted = false;
            _loadedQuests[i].isClaimed = false;
            _loadedQuests[i].isSkipped = false;
        }

        for (int i = 0; i < questItems.Count; i++)
        {
            if (questItems[i] == null || !questItems[i].gameObject.activeSelf)
                continue;

            questItems[i].ResetClientSide();
        }

        SetIndicators(false);
    }

    private void SetIndicators(bool hasClaimableQuest)
    {
        if (haveQuestIndicator != null)
            haveQuestIndicator.SetActive(hasClaimableQuest);

        if (haveDailiesQuestIndicator != null)
            haveDailiesQuestIndicator.SetActive(hasClaimableQuest);
    }

    public void CloseQuest()
    {
        questObj.SetActive(false);
        CurrentQuestTab = QuestTabState.DAILIES;
    }
}
