using System.Collections.Generic;

public class MissionDef
{
    public int Id;
    public string Name;
    public string Description;
    public MissionMetric Metric;
    public Rarity RarityTarget;
    public long Target;
    public int GoldReward;
    public int PrereqId;

    public MissionDef(int id, string name, string desc, MissionMetric metric, long target,
        int gold, int prereqId = -1, Rarity rarityTarget = Rarity.Common)
    {
        Id = id; Name = name; Description = desc; Metric = metric; Target = target;
        GoldReward = gold; PrereqId = prereqId; RarityTarget = rarityTarget;
    }
}

/// <summary>
/// </summary>
public static class MissionCatalog
{
    private static List<MissionDef> _all;
    public static List<MissionDef> All => _all ??= Build();

    private static List<MissionDef> Build() => new List<MissionDef>
    {
        new MissionDef(1001, "First Steps",           "Breed 1 Slime",                         MissionMetric.TotalBred,     1,  200),
        new MissionDef(1002, "Growing Family",        "Breed 5 Slimes",                        MissionMetric.TotalBred,     5,  500, 1001),
        new MissionDef(1003, "Small Habitat",         "Own 10 Slimes simultaneously",          MissionMetric.OwnedSlimes,   10, 800, 1002),
        new MissionDef(1004, "First Victory",         "Win 1 battle",                          MissionMetric.BattleWins,    1,  700, 1003),
        new MissionDef(1005, "Apprentice Hunter",     "Capture 1 wild Slime",                  MissionMetric.Captures,      1,  900, 1004),
        new MissionDef(1006, "Tower Climber I",       "Reach Tower Floor 3",                   MissionMetric.TowerFloor,    3,  1200, 1004),
        new MissionDef(1007, "Super Rare Touch",      "Own 1 Super Rare+ Slime",               MissionMetric.RarityAtLeast, 1,  1500, 1002, Rarity.SuperRare),
        new MissionDef(1008, "Breeding Enthusiast",   "Breed 10 Slimes",                       MissionMetric.TotalBred,     10, 1500, 1002),
        new MissionDef(1009, "Tower Climber II",      "Reach Tower Floor 5",                   MissionMetric.TowerFloor,    5,  2500, 1006),
        new MissionDef(1010, "Farm Expert",           "Win 5 Farm matches",                    MissionMetric.FarmWins,      5,  2500, 1004),
        new MissionDef(1011, "Rare Collector",        "Own 1 Rare+ Slime",                     MissionMetric.RarityAtLeast, 1,  3000, 1008, Rarity.Rare),
        new MissionDef(1012, "Tower Master",          "Reach Tower Floor 10",                  MissionMetric.TowerFloor,    10, 5000, 1009),
        new MissionDef(1013, "Ultra Rare Legend",     "Own 1 Ultra Rare+ Slime",               MissionMetric.RarityAtLeast, 1,  8000, 1011, Rarity.UltraRare),
        new MissionDef(1014, "Legendary Goo",         "Own 1 Legendary+ Slime",                MissionMetric.RarityAtLeast, 1,  15000, 1013, Rarity.Legendary),
    };
}
