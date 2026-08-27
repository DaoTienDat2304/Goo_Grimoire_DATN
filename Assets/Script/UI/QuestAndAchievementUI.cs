using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if TMP_PRESENT || UNITY_2018_1_OR_NEWER
using TMPro;
#endif

public class QuestAndAchievementUI : MonoBehaviour
{
    public static QuestAndAchievementUI Instance { get; private set; }

    public enum TabType
    {
        DailyQuests = 0,
        MainMissions = 1,
        Achievements = 2
    }

    [Header("Window & Canvas Controls")]
    public CanvasGroup canvasGroup;
    public Button closeButton;
    public Button openButton;

    [Header("Tabs")]
    public Button tabDailyButton;
    public Button tabMainButton;
    public Button tabAchievementsButton;
    public Image tabDailyHighlight;
    public Image tabMainHighlight;
    public Image tabAchievementsHighlight;
    public Color tabActiveColor = new Color(1f, 0.85f, 0.3f, 1f);
    public Color tabInactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Content")]
    public Transform contentContainer;
    public GameObject cardPrefab;

    [Header("Header Info (Optional)")]
    public GameObject sectionTitleObject;
    public GameObject sectionCounterObject;

    [Header("Daily Streak Bonus")]
    public GameObject streakBonusPanel;
    public Slider streakSlider;
    public GameObject streakTextObject;
    public Button streakClaimButton;
    public GameObject streakClaimButtonTextObject;

    [Header("Reward Icons")]
    public Sprite coinSprite;
    public Sprite gemSprite;

    private TabType _currentTab = TabType.DailyQuests;
    private const string PrefKeyPrefix = "ACH_";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (openButton != null) openButton.onClick.AddListener(Open);

        if (tabDailyButton != null) tabDailyButton.onClick.AddListener(() => SwitchTab(TabType.DailyQuests));
        if (tabMainButton != null) tabMainButton.onClick.AddListener(() => SwitchTab(TabType.MainMissions));
        if (tabAchievementsButton != null) tabAchievementsButton.onClick.AddListener(() => SwitchTab(TabType.Achievements));

        if (streakClaimButton != null) streakClaimButton.onClick.AddListener(ClaimDailyStreakBonus);
    }

    private void Start()
    {
        Close();
    }

    private void OnEnable()
    {
        PlayerStatsManager.OnStatsChanged += RefreshCurrentTab;
        RefreshCurrentTab();
    }

    private void OnDisable()
    {
        PlayerStatsManager.OnStatsChanged -= RefreshCurrentTab;
    }

    public void Open()
    {
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        var worldManager = FindFirstObjectByType<SlimeWorldManager>();
        if (worldManager != null)
        {
            worldManager.ClearWorldSlimes();
        }

        RefreshCurrentTab();
    }

    public void OpenAchievements()
    {
        Open();
        SwitchTab(TabType.Achievements);
    }

    public void Close()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        gameObject.SetActive(false);

        var worldManager = FindFirstObjectByType<SlimeWorldManager>();
        if (worldManager != null)
        {
            worldManager.StartWorldView();
        }
    }

    public void SwitchTab(TabType newTab)
    {
        _currentTab = newTab;
        UpdateTabVisuals();
        RefreshCurrentTab();
    }

    private void UpdateTabVisuals()
    {
        if (tabDailyHighlight != null) tabDailyHighlight.color = _currentTab == TabType.DailyQuests ? tabActiveColor : tabInactiveColor;
        if (tabMainHighlight != null) tabMainHighlight.color = _currentTab == TabType.MainMissions ? tabActiveColor : tabInactiveColor;
        if (tabAchievementsHighlight != null) tabAchievementsHighlight.color = _currentTab == TabType.Achievements ? tabActiveColor : tabInactiveColor;

        if (streakBonusPanel != null)
        {
            streakBonusPanel.SetActive(_currentTab == TabType.DailyQuests);
        }
    }

    public void RefreshCurrentTab()
    {
        if (!gameObject.activeInHierarchy && (canvasGroup == null || canvasGroup.alpha < 0.01f)) return;

        UpdateTabVisuals();

        switch (_currentTab)
        {
            case TabType.DailyQuests:
                RenderDailyQuests();
                break;
            case TabType.MainMissions:
                RenderMainMissions();
                break;
            case TabType.Achievements:
                RenderAchievements();
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 1. DAILY QUESTS (TAB 1)
    // ─────────────────────────────────────────────────────────────────────────────
    private void RenderDailyQuests()
    {
        ClearContent();
        SetText(sectionTitleObject, "Daily Quests");

        var manager = DailyMissionManager.Instance;
        var questManager = QuestManager.Instance;
        if (manager == null || questManager == null) return;

        var dailyQuests = new List<DailyQuest>();
        foreach (var q in questManager.allQuests)
        {
            if (q is DailyQuest dq)
            {
                dailyQuests.Add(dq);
            }
        }

        int completedCount = 0;
        int totalCount = dailyQuests.Count;

        dailyQuests.Sort((a, b) => GetQuestSortOrder(a).CompareTo(GetQuestSortOrder(b)));

        foreach (var dq in dailyQuests)
        {
            if (dq.state == Quest.QuestState.Rewarded) completedCount++;

            GameObject cardObj = Instantiate(cardPrefab, contentContainer);
            var cardUI = cardObj.GetComponent<QuestCardItemUI>();
            if (cardUI != null)
            {
                var (current, target) = GetQuestProgress(dq);
                bool isClaimed = dq.state == Quest.QuestState.Rewarded;
                Sprite rewardSpr = GetQuestRewardSprite(dq);
                int rewardAmt = dq.reward != null ? dq.reward.amount : 200;

                cardUI.Setup(
                    dq.questName,
                    dq.description,
                    current,
                    target,
                    rewardSpr,
                    rewardAmt,
                    "Coins",
                    isClaimed,
                    () => {
                        dq.ClaimReward();
                        RefreshCurrentTab();
                    }
                );
            }
        }

        SetText(sectionCounterObject, $"{completedCount} / {totalCount}");

        if (streakSlider != null)
        {
            streakSlider.minValue = 0;
            streakSlider.maxValue = totalCount > 0 ? totalCount : 3;
            streakSlider.value = completedCount;
        }

        bool allDone = totalCount > 0 && completedCount >= totalCount;
        SetText(streakTextObject, $"Daily Streak: {completedCount} / {totalCount}  (+{DailyMissionManager.StreakBonusGold} Coins)");

        if (streakClaimButton != null)
        {
            streakClaimButton.interactable = allDone;
        }
    }

    private void ClaimDailyStreakBonus()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, DailyMissionManager.StreakBonusGold);
        }
        if (streakClaimButton != null)
        {
            streakClaimButton.interactable = false;
        }
        SetText(streakClaimButtonTextObject, "CLAIMED");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 2. MAIN MISSIONS (TAB 2)
    // ─────────────────────────────────────────────────────────────────────────────
    private void RenderMainMissions()
    {
        ClearContent();
        SetText(sectionTitleObject, "Main Missions");

        var questManager = QuestManager.Instance;
        if (questManager == null) return;

        var mainQuests = new List<Quest>();
        foreach (var q in questManager.allQuests)
        {
            if (!(q is DailyQuest))
            {
                mainQuests.Add(q);
            }
        }

        int completedCount = 0;
        int totalCount = mainQuests.Count;

        mainQuests.Sort((a, b) => GetQuestSortOrder(a).CompareTo(GetQuestSortOrder(b)));

        foreach (var q in mainQuests)
        {
            if (q.state == Quest.QuestState.Rewarded) completedCount++;

            GameObject cardObj = Instantiate(cardPrefab, contentContainer);
            var cardUI = cardObj.GetComponent<QuestCardItemUI>();
            if (cardUI != null)
            {
                var (current, target) = GetQuestProgress(q);
                bool isClaimed = q.state == Quest.QuestState.Rewarded;
                Sprite rewardSpr = GetQuestRewardSprite(q);
                int rewardAmt = q.reward != null ? q.reward.amount : (q.currencyReward != null && q.currencyReward.rewards.Count > 0 ? q.currencyReward.rewards[0].amount : 500);
                string suffix = (q.currencyReward != null && q.currencyReward.rewards.Count > 0 && q.currencyReward.rewards[0].type == CurrencyType.Gems) ? "Gems" : "Coins";

                cardUI.Setup(
                    q.questName,
                    q.description,
                    current,
                    target,
                    rewardSpr,
                    rewardAmt,
                    suffix,
                    isClaimed,
                    () => {
                        q.ClaimReward();
                        RefreshCurrentTab();
                    }
                );
            }
        }

        SetText(sectionCounterObject, $"{completedCount} / {totalCount}");
    }

    private Sprite GetQuestRewardSprite(Quest q)
    {
        if (q != null && q.rewardIcon != null)
        {
            return q.rewardIcon;
        }

        if (q != null && q.currencyReward != null && q.currencyReward.rewards != null)
        {
            foreach (var r in q.currencyReward.rewards)
            {
                if (r.type == CurrencyType.Gems) return gemSprite;
                if (r.type == CurrencyType.Coins) return coinSprite;
            }
        }

        return coinSprite;
    }

    private (long current, long target) GetQuestProgress(Quest q)
    {
        if (q is DailyQuest dq)
        {
            return (dq.Current(), dq.target);
        }
        if (q is CatalogQuest cq)
        {
            return (cq.Current(), cq.target);
        }
        if (q.slimeRequirement > 0)
        {
            int currentSlimes = BreedingManager.Instance != null ? BreedingManager.Instance.GetAllSlimes().Count : 0;
            return (currentSlimes, q.slimeRequirement);
        }

        float pct = q.GetProgressPercentage() / 100f;
        return ((long)(pct * 100), 100);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 3. ACHIEVEMENTS (TAB 3)
    // ─────────────────────────────────────────────────────────────────────────────
    private void RenderAchievements()
    {
        ClearContent();
        SetText(sectionTitleObject, "Achievements");

        var stats = PlayerStatsManager.Instance;
        if (stats == null) return;

        var allDefs = AchievementCatalog.All;
        int unlockedCount = 0;
        int totalCount = allDefs.Count;

        var list = new List<AchievementDef>(allDefs);
        list.Sort((a, b) =>
        {
            bool aClaimed = PlayerPrefs.GetInt(PrefKeyPrefix + a.Id, 0) == 1;
            bool bClaimed = PlayerPrefs.GetInt(PrefKeyPrefix + b.Id, 0) == 1;
            long aCur = stats.ReadMetric(a.Metric, a.RarityTarget);
            long bCur = stats.ReadMetric(b.Metric, b.RarityTarget);
            bool aReady = !aClaimed && aCur >= a.Target;
            bool bReady = !bClaimed && bCur >= b.Target;

            if (aReady != bReady) return aReady ? -1 : 1;
            if (aClaimed != bClaimed) return aClaimed ? 1 : -1;
            return a.Target.CompareTo(b.Target);
        });

        foreach (var def in list)
        {
            bool isClaimed = PlayerPrefs.GetInt(PrefKeyPrefix + def.Id, 0) == 1;
            if (isClaimed) unlockedCount++;

            long current = stats.ReadMetric(def.Metric, def.RarityTarget);
            long target = def.Target;

            GameObject cardObj = Instantiate(cardPrefab, contentContainer);
            var cardUI = cardObj.GetComponent<QuestCardItemUI>();
            if (cardUI != null)
            {
                cardUI.Setup(
                    def.Title,
                    string.Format(def.Description, def.Target),
                    current,
                    target,
                    gemSprite,
                    def.GemReward,
                    "Gems",
                    isClaimed,
                    () => {
                        PlayerPrefs.SetInt(PrefKeyPrefix + def.Id, 1);
                        PlayerPrefs.Save();
                        if (CurrencyManager.Instance != null)
                        {
                            CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, def.GemReward);
                        }
                        RefreshCurrentTab();
                    }
                );
            }
        }

        SetText(sectionCounterObject, $"{unlockedCount} / {totalCount}");
    }

    private int GetQuestSortOrder(Quest q)
    {
        if (q.state == Quest.QuestState.Completed) return 0;
        if (q.state == Quest.QuestState.InProgress || q.state == Quest.QuestState.Available) return 1;
        return 2;
    }

    private void ClearContent()
    {
        if (contentContainer == null) return;
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
        {
            var child = contentContainer.GetChild(i);
            if (child != null)
            {
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }
    }

    private void SetText(GameObject obj, string text)
    {
        if (obj == null) return;

#if TMP_PRESENT || UNITY_2018_1_OR_NEWER
        var tmp = obj.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = text;
            return;
        }
#endif
        var legacyText = obj.GetComponent<Text>();
        if (legacyText != null)
        {
            legacyText.text = text;
        }
    }
}
