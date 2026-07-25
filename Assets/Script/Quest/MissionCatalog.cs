using System.Collections.Generic;

/// <summary>1 nhiệm vụ chính (data thuần). Thưởng luôn là VÀNG (Coins).</summary>
public class MissionDef
{
    public int Id;
    public string Name;
    public string Description;
    public MissionMetric Metric;
    public Rarity RarityTarget;
    public long Target;
    public int GoldReward;
    public int PrereqId; // -1 = không cần nhiệm vụ trước

    public MissionDef(int id, string name, string desc, MissionMetric metric, long target,
        int gold, int prereqId = -1, Rarity rarityTarget = Rarity.Common)
    {
        Id = id; Name = name; Description = desc; Metric = metric; Target = target;
        GoldReward = gold; PrereqId = prereqId; RarityTarget = rarityTarget;
    }
}

/// <summary>
/// Chuỗi nhiệm vụ chính định nghĩa bằng code (theo Docs/ThietKe-ThanhTuu-NhiemVu.md).
/// Mở khóa dây chuyền qua PrereqId. Chỉnh mốc/thưởng ngay tại đây.
/// ID bắt đầu từ 1001 để không đụng ID quest asset cũ.
/// </summary>
public static class MissionCatalog
{
    private static List<MissionDef> _all;
    public static List<MissionDef> All => _all ??= Build();

    private static List<MissionDef> Build() => new List<MissionDef>
    {
        new MissionDef(1001, "Bước đầu làm quen", "Lai tạo 1 slime",        MissionMetric.TotalBred,   1,  200),
        new MissionDef(1002, "Lứa đầu tiên",      "Lai tạo 5 slime",        MissionMetric.TotalBred,   5,  500, 1001),
        new MissionDef(1003, "Chuồng nhỏ",        "Sở hữu 10 slime",        MissionMetric.OwnedSlimes, 10, 800, 1002),
        new MissionDef(1004, "Ra trận",           "Thắng 1 trận đấu",       MissionMetric.BattleWins,  1,  700, 1003),
        new MissionDef(1005, "Thợ săn tập sự",    "Bắt 1 slime hoang",      MissionMetric.Captures,    1,  900, 1004),
        new MissionDef(1006, "Vượt màn 3",        "Đạt tầng tháp 3",        MissionMetric.TowerFloor,  3,  1200, 1004),
        new MissionDef(1007, "Chạm Siêu Hiếm",    "Sở hữu 1 slime Super Rare trở lên", MissionMetric.RarityAtLeast, 1, 1500, 1002, Rarity.SuperRare),
        new MissionDef(1008, "Đàn lớn",           "Lai tạo 10 slime",       MissionMetric.TotalBred,   10, 1500, 1002),
        new MissionDef(1009, "Vượt màn 5",        "Đạt tầng tháp 5",        MissionMetric.TowerFloor,  5,  2500, 1006),
        new MissionDef(1010, "Nông trại vàng",    "Thắng 5 trận Farm",      MissionMetric.FarmWins,    5,  2500, 1004),
        new MissionDef(1011, "Chạm hàng hiếm",    "Sở hữu 1 slime Rare trở lên", MissionMetric.RarityAtLeast, 1, 3000, 1008, Rarity.Rare),
        new MissionDef(1012, "Vượt màn 10",       "Đạt tầng tháp 10",       MissionMetric.TowerFloor,  10, 5000, 1009),
        new MissionDef(1013, "Siêu phẩm",         "Sở hữu 1 slime Ultra Rare trở lên", MissionMetric.RarityAtLeast, 1, 8000, 1011, Rarity.UltraRare),
        new MissionDef(1014, "Huyền thoại",       "Sở hữu 1 slime Legendary trở lên",  MissionMetric.RarityAtLeast, 1, 15000, 1013, Rarity.Legendary),
    };
}
