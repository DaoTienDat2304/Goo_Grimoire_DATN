using System.Collections.Generic;

/// <summary>
/// </summary>
public enum AchievementMetric
{
    TotalBred,
    DistinctTraits,
    CoinsEarned,
    GemsEarned,
    FarmWins,
    Captures,
    RarityObtained,
    TowerFloor,
    BattleWins,
    OwnedSlimes,
    Mutations
}

public class AchievementDef
{
    public string Id;
    public string Title;
    public string Description;
    public AchievementMetric Metric;
    public Rarity RarityTarget;
    public long Target;
    public int GemReward;

    public AchievementDef(string id, string title, string desc,
        AchievementMetric metric, long target, int gemReward, Rarity rarityTarget = Rarity.Common)
    {
        Id = id; Title = title; Description = desc;
        Metric = metric; Target = target; GemReward = gemReward; RarityTarget = rarityTarget;
    }
}

/// <summary>
/// </summary>
public static class AchievementCatalog
{
    private static List<AchievementDef> _all;
    public static List<AchievementDef> All => _all ??= Build();

    private static List<AchievementDef> Build()
    {
        var l = new List<AchievementDef>();

        // A. Breeding
        Chain(l, "bred", "Breeding Master", "Breed {0} Slimes", AchievementMetric.TotalBred,
            new long[] { 10, 50, 100, 500, 1000 }, new int[] { 5, 15, 40, 100, 300 });

        // B. Trait Collection
        Chain(l, "trait", "Trait Collector", "Collect {0} distinct Traits", AchievementMetric.DistinctTraits,
            new long[] { 10, 25, 50, 100 }, new int[] { 10, 25, 60, 150 });

        // C. Gold Earned
        Chain(l, "coins", "Coin Tycoon", "Earn {0} Coins in total", AchievementMetric.CoinsEarned,
            new long[] { 1000, 10000, 100000, 1000000 }, new int[] { 15, 40, 120, 500 });

        // D. Gem Earned
        Chain(l, "gems", "Treasure Hoarder", "Earn {0} Gems in total", AchievementMetric.GemsEarned,
            new long[] { 50, 500, 5000 }, new int[] { 10, 50, 200 });

        // E. Farm Wins
        Chain(l, "farm", "Farm Champion", "Win {0} Farm matches", AchievementMetric.FarmWins,
            new long[] { 1, 10, 50, 100 }, new int[] { 5, 25, 80, 200 });

        // F. Adventure Captures
        Chain(l, "catch", "Wild Hunter", "Capture {0} wild Slimes", AchievementMetric.Captures,
            new long[] { 10, 30, 100, 300 }, new int[] { 10, 30, 90, 250 });

        // G. Rarity Hunters
        Chain(l, "superrare", "Super Rare Seeker", "Own {0} Super Rare Slimes", AchievementMetric.RarityObtained,
            new long[] { 1, 10, 50 }, new int[] { 10, 40, 120 }, Rarity.SuperRare);
        Chain(l, "ultrarare", "Ultra Rare Seeker", "Own {0} Ultra Rare Slimes", AchievementMetric.RarityObtained,
            new long[] { 1, 10, 50 }, new int[] { 20, 80, 200 }, Rarity.UltraRare);
        Chain(l, "legendary", "Legendary Master", "Own {0} Legendary Slimes", AchievementMetric.RarityObtained,
            new long[] { 1, 10, 25 }, new int[] { 50, 150, 350 }, Rarity.Legendary);
        Chain(l, "mythic", "Mythic Legend", "Own {0} Mythic Slimes", AchievementMetric.RarityObtained,
            new long[] { 1, 5, 10 }, new int[] { 100, 300, 700 }, Rarity.Mythic);
        Chain(l, "secret", "Secret Slime Master", "Collect {0} Secret Slimes", AchievementMetric.RarityObtained,
            new long[] { 1, 3 }, new int[] { 150, 400 }, Rarity.Secret);

        // H. Tower Floor
        Chain(l, "tower", "Tower Conqueror", "Reach Tower Floor {0}", AchievementMetric.TowerFloor,
            new long[] { 5, 10, 20, 50 }, new int[] { 20, 60, 150, 500 });

        // I. Battle Wins
        Chain(l, "battle", "Gladiator", "Win {0} battles", AchievementMetric.BattleWins,
            new long[] { 10, 50, 200, 500 }, new int[] { 10, 40, 120, 300 });

        // J. Current Owned Slimes
        Chain(l, "owned", "Slime Sanctuary", "Own {0} Slimes simultaneously", AchievementMetric.OwnedSlimes,
            new long[] { 10, 20, 30 }, new int[] { 15, 40, 100 });

        // K. Mutations
        Chain(l, "mutation", "Alchemist", "Breed {0} mutated Slimes", AchievementMetric.Mutations,
            new long[] { 1, 10, 50 }, new int[] { 20, 80, 300 });

        return l;
    }

    private static readonly string[] TierSuffix =
        { " I", " II", " III", " IV", " V", " VI" };

    private static void Chain(List<AchievementDef> list, string idPrefix, string title,
        string descFormat, AchievementMetric metric, long[] targets, int[] gems,
        Rarity rarityTarget = Rarity.Common)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            string tier = i < TierSuffix.Length ? TierSuffix[i] : " " + (i + 1);
            list.Add(new AchievementDef(
                id: $"{idPrefix}_{targets[i]}",
                title: title + tier,
                desc: string.Format(descFormat, targets[i]),
                metric: metric,
                target: targets[i],
                gemReward: gems[i],
                rarityTarget: rarityTarget));
        }
    }
}
