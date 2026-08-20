/// <summary>
/// Bảng range base-stat theo độ hiếm — nguồn: "Cân bằng chỉ số.xlsx" (khớp bảng GDD
/// trong TraitInstance.RollStatsByGDD). Dùng chung cho lai tạo (mục 3) để mọi chỉ số
/// của slime con luôn nằm trong range của độ hiếm trứng.
/// Bố cục: HP/DEF/Speed = Body, ATK/Magic = Weapon, Crit = Head/Armor.
/// </summary>
public static class StatBalance
{
    public struct Range
    {
        public int hpMin, hpMax;
        public int atkMin, atkMax;
        public int magMin, magMax;
        public int defMin, defMax;
        public int spdMin, spdMax;
        public float critRate; // phân số (0.05 = 5%)
        public float critDmg;  // hệ số nhân (1.30 = +30%)
    }

    public static Range Get(Rarity rarity)
    {
        // Remote Config (`stat_balance_table`) ghi đè nếu có; không có → bảng gốc bên dưới.
        if (RemoteBalance.TryGetStatRange(rarity, out var remote)) return remote;

        switch (rarity)
        {
            case Rarity.Common:    return R(1500, 2000,  350, 500,   450, 650,    500, 800,     80, 100,  0.18f, 1.25f);
            case Rarity.Uncommon:  return R(2000, 2700,  450, 650,   600, 850,    700, 1000,    90, 110,  0.24f, 1.35f);
            case Rarity.Rare:      return R(2700, 3700,  600, 850,   800, 1100,   900, 1300,   100, 120,  0.32f, 1.48f);
            case Rarity.SuperRare: return R(3700, 5000,  800, 1100,  1050, 1450,  1200, 1700,  110, 135,  0.40f, 1.62f);
            case Rarity.UltraRare: return R(5000, 6500,  1000, 1400, 1350, 1850,  1600, 2300,  120, 150,  0.50f, 1.80f);
            case Rarity.Legendary: return R(6500, 8300,  1300, 1800, 1700, 2400,  2100, 3000,  135, 165,  0.59f, 2.00f);
            case Rarity.Mythic:    return R(8300, 10000, 1700, 2300, 2200, 3000,  2700, 3800,  150, 180,  0.66f, 2.20f);
            case Rarity.Secret:    return R(5000, 6500,  1000, 1400, 1350, 1850,  1600, 2300,  120, 150,  0.70f, 2.50f);
            default:               return R(1500, 2000,  350, 500,   450, 650,    500, 800,     80, 100,  0.18f, 1.25f);
        }
    }

    private static Range R(int hpMin, int hpMax, int atkMin, int atkMax, int magMin, int magMax,
                           int defMin, int defMax, int spdMin, int spdMax, float critRate, float critDmg)
    {
        return new Range
        {
            hpMin = hpMin, hpMax = hpMax,
            atkMin = atkMin, atkMax = atkMax,
            magMin = magMin, magMax = magMax,
            defMin = defMin, defMax = defMax,
            spdMin = spdMin, spdMax = spdMax,
            critRate = critRate, critDmg = critDmg
        };
    }
}
