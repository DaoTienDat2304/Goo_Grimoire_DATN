using UnityEngine;

/// <summary>
/// Stat Roll cho enemy Adventure (Turn Base) — theo design "Tỉ lệ Stat Roll cho Turn Base Adventure Scene":
/// Good 55% (40–60%) · Excellent 28% (60–80%) · Perfect 12% (80–95%) · God 5% (95–100%). Sàn 40%.
/// Áp lên slime theo StatBalance (per-trait rarity) để chỉ số enemy nằm đúng chuẩn GDD & không quá thấp.
/// </summary>
public static class AdventureStatRoll
{
    public static float RollQuality()
    {
        // Remote Config (`adventure_quality_bands`) ghi đè nếu có.
        var bands = RemoteBalance.AdventureQuality;
        if (bands != null) { bands.Roll(out float rt); return rt; }

        float r = Random.value * 100f;
        if (r < 55f) return Random.Range(0.40f, 0.60f);
        if (r < 83f) return Random.Range(0.60f, 0.80f);
        if (r < 95f) return Random.Range(0.80f, 0.95f);
        return Random.Range(0.95f, 1.00f);
    }

    // Một quality roll chung cho mọi chỉ số ranged (God Roll = mạnh đồng đều). Gọi CalculateStats() sau cùng.
    public static void Apply(Slime s)
    {
        if (s == null) return;
        float t = RollQuality();

        if (s.body != null)
        {
            var r = StatBalance.Get(s.body.Rarity);
            s.body.HP = s.body.baseHP = LerpInt(r.hpMin, r.hpMax, t);
            s.body.defense = s.body.baseDefense = LerpInt(r.defMin, r.defMax, t);
            s.body.speed = s.body.baseSpeed = LerpInt(r.spdMin, r.spdMax, t);
        }
        if (s.weapon != null)
        {
            var r = StatBalance.Get(s.weapon.Rarity);
            s.weapon.attack = s.weapon.baseAttack = LerpInt(r.atkMin, r.atkMax, t);
            s.weapon.magicAttack = s.weapon.baseMagicAttack = LerpInt(r.magMin, r.magMax, t);
        }
        if (s.armor != null)
        {
            var r = StatBalance.Get(s.armor.Rarity);
            s.armor.critRate = s.armor.baseCritRate = r.critRate;
            s.armor.critDMG = s.armor.baseCritDMG = r.critDmg;
        }
        s.CalculateStats();
    }

    private static int LerpInt(int min, int max, float t) => Mathf.RoundToInt(Mathf.Lerp(min, max, t));
}
