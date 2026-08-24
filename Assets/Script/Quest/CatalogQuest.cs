using UnityEngine;

public enum MissionMetric
{
    TotalBred,
    OwnedSlimes,
    BattleWins,
    Captures,
    TowerFloor,
    FarmWins,
    RarityAtLeast
}
public class CatalogQuest : Quest
{
    public MissionMetric metric;
    public Rarity rarityTarget = Rarity.Common;
    public long target = 1;

    public long Current()
    {
        var st = PlayerStatsManager.Instance;
        if (st == null) return 0;
        switch (metric)
        {
            case MissionMetric.TotalBred:   return st.TotalSlimesBred;
            case MissionMetric.BattleWins:  return st.TotalBattleWins;
            case MissionMetric.Captures:    return st.TotalCaptures;
            case MissionMetric.TowerFloor:  return st.HighestTowerFloor;
            case MissionMetric.FarmWins:    return st.TotalFarmWins;
            case MissionMetric.RarityAtLeast: return st.GetRarityObtainedAtLeast(rarityTarget);
            case MissionMetric.OwnedSlimes:
                return BreedingManager.Instance != null ? BreedingManager.Instance.GetAllSlimes().Count : 0;
            default: return 0;
        }
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
