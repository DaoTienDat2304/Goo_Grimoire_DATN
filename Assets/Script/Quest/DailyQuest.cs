using UnityEngine;

/// <summary>
/// Nhiệm vụ hàng ngày runtime. Tiến trình = (counter lifetime hiện tại − baseline đầu ngày),
/// nên chỉ tính phần làm được TRONG NGÀY. Tái dùng state machine/UI của QuestManager.
/// </summary>
public class DailyQuest : Quest
{
    public DailyMetric metric;
    public long target = 1;
    public long baseline; // giá trị counter lúc bắt đầu ngày

    public static long Lifetime(DailyMetric m)
    {
        var st = PlayerStatsManager.Instance;
        if (st == null) return 0;
        switch (m)
        {
            case DailyMetric.TotalBred:    return st.TotalSlimesBred;
            case DailyMetric.BattleWins:   return st.TotalBattleWins;
            case DailyMetric.Captures:     return st.TotalCaptures;
            case DailyMetric.FarmWins:     return st.TotalFarmWins;
            case DailyMetric.CoinsEarned:  return st.TotalCoinsEarned;
            case DailyMetric.TowerFloor:   return st.HighestTowerFloor;
            case DailyMetric.RareObtained: return st.GetRarityObtainedAtLeast(Rarity.Rare);
            default: return 0;
        }
    }

    public long Current()
    {
        long delta = Lifetime(metric) - baseline;
        return delta < 0 ? 0 : delta;
    }

    public override bool CheckCompletion() => Current() >= target;

    public override float GetProgressPercentage()
    {
        if (target <= 0) return 0f;
        return Mathf.Clamp01((float)Current() / target) * 100f;
    }

    public override string GetProgressText()
    {
        long cur = Current();
        long shown = cur > target ? target : cur;
        return $"{shown} / {target} ({GetProgressPercentage():F0}%)";
    }
}
