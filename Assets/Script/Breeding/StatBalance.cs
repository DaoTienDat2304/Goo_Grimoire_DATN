/// <summary>
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
        public float critRate;
        public float critDmg;
    }

    public static Range Get(Rarity rarity)
    {
        if (RemoteBalance.TryGetStatRange(rarity, out var remote)) return remote;

        switch (rarity)
        {
<<<<<<< HEAD
            case Rarity.Common:    return R(1500, 2000,  350, 500,   450, 650,    500, 800,     80, 100,  0.18f, 1.25f);
            case Rarity.Uncommon:  return R(2000, 2700,  450, 650,   600, 850,    700, 1000,    90, 110,  0.24f, 1.35f);
            case Rarity.Rare:      return R(2700, 3700,  600, 850,   800, 1100,   900, 1300,   100, 120,  0.32f, 1.48f);
            case Rarity.SuperRare: return R(3700, 5000,  800, 1100,  1050, 1450,  1200, 1700,  110, 135,  0.40f, 1.62f);
            case Rarity.UltraRare: return R(5000, 6500,  1000, 1400, 1350, 1850,  1600, 2300,  120, 150,  0.50f, 1.80f);
            case Rarity.Legendary: return R(6500, 8300,  1300, 1800, 1700, 2400,  2100, 3000,  135, 165,  0.59f, 2.00f);
            case Rarity.Mythic:    return R(8300, 10000, 1700, 2300, 2200, 3000,  2700, 3800,  150, 180,  0.66f, 2.20f);
            case Rarity.Secret:    return R(5000, 6500,  1000, 1400, 1350, 1850,  1600, 2300,  120, 150,  0.70f, 2.50f);
            default:               return R(1500, 2000,  350, 500,   450, 650,    500, 800,     80, 100,  0.18f, 1.25f);
=======
            case Rarity.Common:    return R(1000, 2000,  100, 200,   200, 400,    400, 800,     80, 100,  0.05f, 1.30f);
            case Rarity.Uncommon:  return R(1800, 3500,  180, 320,   320, 640,    720, 1400,    90, 110,  0.06f, 1.35f);
            case Rarity.Rare:      return R(3200, 6000,  320, 600,   640, 1200,   1280, 2400,   100, 120, 0.08f, 1.45f);
            case Rarity.SuperRare: return R(5500, 10000, 550, 1000,  1100, 2000,  2200, 4000,   110, 135, 0.10f, 1.55f);
            case Rarity.UltraRare: return R(9000, 16000, 900, 1600,  1800, 3200,  3600, 6400,   120, 150, 0.13f, 1.70f);
            case Rarity.Legendary: return R(14000, 25000, 1400, 2500, 2800, 5000, 5600, 10000,  135, 165, 0.16f, 1.90f);
            case Rarity.Mythic:    return R(22000, 50000, 2200, 5000, 4400, 10000, 8800, 20000, 150, 180, 0.20f, 2.20f);
            case Rarity.Secret:    return R(9000, 16000, 90, 160, 180, 320, 1440, 2560, 120, 150, 0.25f, 2.50f);
            default:               return R(1000, 2000,  100, 200,   200, 400,    400, 800,     80, 100,  0.05f, 1.30f);
>>>>>>> Player
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
