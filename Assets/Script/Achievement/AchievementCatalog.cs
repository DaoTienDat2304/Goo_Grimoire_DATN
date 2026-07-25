using System.Collections.Generic;

/// <summary>
/// Chỉ số mà 1 thành tựu chấm điểm — trỏ vào bộ đếm lifetime của PlayerStatsManager
/// (trừ OwnedSlimes đọc trực tiếp số slime đang sở hữu).
/// </summary>
public enum AchievementMetric
{
    TotalBred,       // tổng slime đã lai tạo
    DistinctTraits,  // số trait khác nhau đã thu thập
    CoinsEarned,     // tổng vàng kiếm được
    GemsEarned,      // tổng gem kiếm được
    FarmWins,        // số lần thắng Farm
    Captures,        // số slime bắt được ở phiêu lưu
    RarityObtained,  // số slime từng sở hữu theo 1 độ hiếm (dùng RarityTarget)
    TowerFloor,      // tầng tháp cao nhất
    BattleWins,      // tổng trận thắng
    OwnedSlimes,     // số slime đang sở hữu
    Mutations        // số slime đột biến khi lai
}

/// <summary>1 bậc thành tựu (data thuần). Thưởng luôn là GEM.</summary>
public class AchievementDef
{
    public string Id;                 // khóa lưu (PlayerPrefs / save)
    public string Title;
    public string Description;
    public AchievementMetric Metric;
    public Rarity RarityTarget;       // chỉ dùng khi Metric == RarityObtained
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
/// Danh mục thành tựu định nghĩa bằng code (theo Docs/ThietKe-ThanhTuu-NhiemVu.md).
/// Chỉnh mốc/thưởng ngay tại đây — không cần tạo asset trong Unity.
/// </summary>
public static class AchievementCatalog
{
    private static List<AchievementDef> _all;
    public static List<AchievementDef> All => _all ??= Build();

    private static List<AchievementDef> Build()
    {
        var l = new List<AchievementDef>();

        // A. Lai tạo — "Nhà lai tạo"
        Chain(l, "bred", "Nhà lai tạo", "Lai tạo {0} slime", AchievementMetric.TotalBred,
            new long[] { 10, 50, 100, 500, 1000 }, new int[] { 5, 15, 40, 100, 300 });

        // B. Sưu tập trait — "Nhà sưu tầm"
        Chain(l, "trait", "Nhà sưu tầm", "Sưu tập {0} trait khác nhau", AchievementMetric.DistinctTraits,
            new long[] { 10, 25, 50, 100 }, new int[] { 10, 25, 60, 150 });

        // C. Vàng kiếm được — "Trọc phú"
        Chain(l, "coins", "Trọc phú", "Kiếm được tổng cộng {0} vàng", AchievementMetric.CoinsEarned,
            new long[] { 1000, 10000, 100000, 1000000 }, new int[] { 15, 40, 120, 500 });

        // D. Gem kiếm được — "Kho báu"
        Chain(l, "gems", "Kho báu", "Kiếm được tổng cộng {0} gem", AchievementMetric.GemsEarned,
            new long[] { 50, 500, 5000 }, new int[] { 10, 50, 200 });

        // E. Farm — "Nông dân"
        Chain(l, "farm", "Nông dân", "Thắng {0} lần chế độ Farm", AchievementMetric.FarmWins,
            new long[] { 1, 10, 50, 100 }, new int[] { 5, 25, 80, 200 });

        // F. Phiêu lưu bắt slime — "Thợ săn"
        Chain(l, "catch", "Thợ săn", "Bắt được {0} slime hoang", AchievementMetric.Captures,
            new long[] { 10, 30, 100, 300 }, new int[] { 10, 30, 90, 250 });

        // G. Săn hàng hiếm — theo độ hiếm
        Chain(l, "superrare", "Săn Siêu Hiếm", "Sở hữu {0} slime Super Rare", AchievementMetric.RarityObtained,
            new long[] { 1, 10, 50 }, new int[] { 10, 40, 120 }, Rarity.SuperRare);
        Chain(l, "ultrarare", "Săn Cực Hiếm", "Sở hữu {0} slime Ultra Rare", AchievementMetric.RarityObtained,
            new long[] { 1, 10, 50 }, new int[] { 20, 80, 200 }, Rarity.UltraRare);
        Chain(l, "legendary", "Huyền Thoại", "Sở hữu {0} slime Legendary", AchievementMetric.RarityObtained,
            new long[] { 1, 10, 25 }, new int[] { 50, 150, 350 }, Rarity.Legendary);
        Chain(l, "mythic", "Thần Thoại", "Sở hữu {0} slime Mythic", AchievementMetric.RarityObtained,
            new long[] { 1, 5, 10 }, new int[] { 100, 300, 700 }, Rarity.Mythic);
        Chain(l, "secret", "Bí Ẩn", "Sưu tập {0} slime Secret", AchievementMetric.RarityObtained,
            new long[] { 1, 3 }, new int[] { 150, 400 }, Rarity.Secret);

        // H. Leo tháp — "Kẻ leo tháp"
        Chain(l, "tower", "Kẻ leo tháp", "Đạt tầng tháp {0}", AchievementMetric.TowerFloor,
            new long[] { 5, 10, 20, 50 }, new int[] { 20, 60, 150, 500 });

        // I. Chiến đấu — "Chiến binh"
        Chain(l, "battle", "Chiến binh", "Thắng {0} trận đấu", AchievementMetric.BattleWins,
            new long[] { 10, 50, 200, 500 }, new int[] { 10, 40, 120, 300 });

        // J. Bộ sưu tập hiện có — "Vườn slime"
        Chain(l, "owned", "Vườn slime", "Sở hữu cùng lúc {0} slime", AchievementMetric.OwnedSlimes,
            new long[] { 10, 20, 30 }, new int[] { 15, 40, 100 });

        // K. Đột biến — "Nhà giả kim"
        Chain(l, "mutation", "Nhà giả kim", "Lai ra {0} slime đột biến", AchievementMetric.Mutations,
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
