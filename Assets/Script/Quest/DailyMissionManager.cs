using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// </summary>
public class DailyMissionManager : MonoBehaviour
{
    public static int DailyCount => RemoteBalance.Reward.dailyCount;

    public static int StreakBonusGold => RemoteBalance.Reward.dailyStreakBonusGold;

    private static DailyMissionManager _instance;
    public static DailyMissionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<DailyMissionManager>();
                if (_instance == null)
                {
                    var go = new GameObject("DailyMissionManager");
                    _instance = go.AddComponent<DailyMissionManager>();
                }
            }
            return _instance;
        }
    }

    private string currentDate;
    private readonly List<int> todayIDs = new List<int>();
    private readonly List<long> baselines = new List<long>();
    private bool streakClaimed;
    private bool built;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private static string Today() => DateTime.Now.ToString("yyyy-MM-dd");

    public void ApplyLoad(string savedDate, List<int> ids, List<long> bases, bool streak)
    {
        string today = Today();
        if (savedDate == today && ids != null && ids.Count == DailyCount
            && bases != null && bases.Count == ids.Count)
        {
            currentDate = today;
            todayIDs.Clear(); todayIDs.AddRange(ids);
            baselines.Clear(); baselines.AddRange(bases);
            streakClaimed = streak;
        }
        else
        {
            RollNewDay(today);
        }
        built = true;
        RegisterInto(QuestManager.Instance, force: true);
    }

    public void WriteTo(GameSaveData data)
    {
        if (!built) return;
        data.lastDailyResetDate = currentDate;
        data.todayDailyIDs = new List<int>(todayIDs);
        data.todayDailyBaselines = new List<long>(baselines);
        data.dailyStreakClaimed = streakClaimed;
    }

    private void RollNewDay(string today)
    {
        currentDate = today;
        streakClaimed = false;
        todayIDs.Clear();
        baselines.Clear();

        var pool = new List<DailyDef>(DailyCatalog.All);
        int pick = Mathf.Min(DailyCount, pool.Count);
        for (int i = 0; i < pick; i++)
        {
            int idx = UnityEngine.Random.Range(0, pool.Count);
            var def = pool[idx];
            pool.RemoveAt(idx);
            todayIDs.Add(def.Id);
            baselines.Add(DailyQuest.Lifetime(def.Metric));
        }
    }

    private void RegisterInto(QuestManager qm, bool force = false)
    {
        if (qm == null || !built) return;

        for (int i = 0; i < todayIDs.Count; i++)
        {
            var ex = qm.GetQuest(todayIDs[i]);
            if (ex != null) qm.RemoveQuest(ex);
        }

        for (int i = 0; i < todayIDs.Count; i++)
        {
            var def = DailyCatalog.ById(todayIDs[i]);
            if (def == null) continue;

            var q = ScriptableObject.CreateInstance<DailyQuest>();
            q.questID = def.Id;
            q.questName = "[Daily] " + def.Name;
            q.description = def.Description;
            q.slimeRequirement = 0;
            q.questreq = new List<int>();

            q.metric = def.Metric;
            q.target = def.Target;
            q.baseline = baselines[i];

            int gold = RemoteBalance.ScaleReward(def.GoldReward, RemoteBalance.Reward.dailyGold);

            q.currencyReward = new CurrencyReward(CurrencyType.Coins, gold);
            q.reward = new QuestReward
            {
                rewardType = "coins",
                amount = gold,
                description = $"{gold} gold"
            };
            q.state = Quest.QuestState.Locked;

            qm.AddQuest(q);
        }
    }

    private bool RegisteredInto(QuestManager qm)
        => qm != null && todayIDs.Count > 0 && qm.GetQuest(todayIDs[0]) != null;

    private void Update()
    {
        if (built && currentDate != Today())
        {
            RollNewDay(Today());
            RegisterInto(QuestManager.Instance, force: true);
            SaveAndLoadSystem.Instance?.Save();
        }

        var qmNow = QuestManager.Instance;
        if (built && qmNow != null && !RegisteredInto(qmNow))
            RegisterInto(qmNow);

        StreakCheck(qmNow);
    }

    private void StreakCheck(QuestManager qm)
    {
        if (!built || streakClaimed || qm == null || todayIDs.Count < DailyCount) return;

        int done = 0;
        foreach (var id in todayIDs)
        {
            var q = qm.GetQuest(id);
            if (q != null && q.state == Quest.QuestState.Rewarded) done++;
        }

        if (done >= DailyCount)
        {
            streakClaimed = true;
            CurrencyManager.Instance?.AddCurrency(CurrencyType.Coins, StreakBonusGold);
            Debug.Log($"[Daily] Completed all {DailyCount} missions daily → bonus {StreakBonusGold} gold!");
            SaveAndLoadSystem.Instance?.Save();
        }
    }
}
