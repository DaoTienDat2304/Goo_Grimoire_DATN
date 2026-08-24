using System.Collections.Generic;

public enum DailyMetric
{
    TotalBred,
    BattleWins,
    Captures,
    FarmWins,
    CoinsEarned,
    TowerFloor,
    RareObtained
}

public class DailyDef
{
    public int Id;
    public string Name;
    public string Description;
    public DailyMetric Metric;
    public long Target;
    public int GoldReward;

    public DailyDef(int id, string name, string desc, DailyMetric metric, long target, int gold)
    {
        Id = id; Name = name; Description = desc; Metric = metric; Target = target; GoldReward = gold;
    }
}
public static class DailyCatalog
{
    private static List<DailyDef> _all;
    public static List<DailyDef> All => _all ??= Build();

    private static List<DailyDef> Build() => new List<DailyDef>
    {
        new DailyDef(2001, "Daily Breeding", "Breed 1 Slime today",              DailyMetric.TotalBred,    1,   150),
        new DailyDef(2002, "First Blood",     "Win 1 battle today",              DailyMetric.BattleWins,   1,   150),
        new DailyDef(2003, "Wild Hunt",       "Capture 1 wild Slime today",      DailyMetric.Captures,     1,   200),
        new DailyDef(2004, "Gold Harvest",    "Win 1 Farm match today",          DailyMetric.FarmWins,     1,   200),
        new DailyDef(2005, "Money Maker",     "Earn 500 Coins today",            DailyMetric.CoinsEarned,  500, 300),
        new DailyDef(2006, "Climb Higher",    "Clear 1 additional Tower floor",  DailyMetric.TowerFloor,   1,   250),
        new DailyDef(2007, "Rare Catch",      "Obtain 1 Rare+ Slime today",      DailyMetric.RareObtained, 1,   400),
        new DailyDef(2008, "Battle Hardened", "Win 3 battles today",             DailyMetric.BattleWins,   3,   350),
    };

    public static DailyDef ById(int id) => All.Find(d => d.Id == id);
}
