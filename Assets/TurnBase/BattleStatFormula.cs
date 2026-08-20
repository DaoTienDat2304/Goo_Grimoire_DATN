using UnityEngine;

public static class BattleStatFormula
{
    public static float CritRateCap => RemoteBalance.Battle.critRateCap;
    public static float CritDMGCap => RemoteBalance.Battle.critDmgCap;
    public static float MaxDefReduction => RemoteBalance.Battle.maxDefReduction;
    public static float DefReductionPerPoint => RemoteBalance.Battle.defReductionPerPoint;
    public static float CritOverflowToAtk => RemoteBalance.Battle.critOverflowToAtk;

    public static float EffectiveCritRate(float baseCritRate, float critChanceBonus = 0f)
    {
        return baseCritRate + critChanceBonus / 100f;
    }

    public static float EffectiveCritDMG(float baseCritRate, float baseCritDMG, float critChanceBonus = 0f)
    {
        float rate = EffectiveCritRate(baseCritRate, critChanceBonus);
        float excessCritRate = Mathf.Max(0f, rate - CritRateCap);
        return baseCritDMG + excessCritRate;
    }

    public static float FinalCritRate(float baseCritRate, float critChanceBonus = 0f)
    {
        return Mathf.Min(CritRateCap, EffectiveCritRate(baseCritRate, critChanceBonus));
    }

    public static float FinalCritDMG(float baseCritRate, float baseCritDMG, float critChanceBonus = 0f)
    {
        return Mathf.Min(CritDMGCap, EffectiveCritDMG(baseCritRate, baseCritDMG, critChanceBonus));
    }

    public static int AttackConversionBonus(float baseCritRate, float baseCritDMG, float critChanceBonus = 0f)
    {
        float critDmg = EffectiveCritDMG(baseCritRate, baseCritDMG, critChanceBonus);
        float excessCritDmg = Mathf.Max(0f, critDmg - CritDMGCap);
        return Mathf.RoundToInt(excessCritDmg * 100f * CritOverflowToAtk);
    }

    public static int EffectiveAttack(int baseAttack, float baseCritRate, float baseCritDMG, float critChanceBonus = 0f)
    {
        return baseAttack + AttackConversionBonus(baseCritRate, baseCritDMG, critChanceBonus);
    }

    public static int EffectiveMagicAttack(int baseMagicAttack, float baseCritRate, float baseCritDMG, float critChanceBonus = 0f)
    {
        return baseMagicAttack + AttackConversionBonus(baseCritRate, baseCritDMG, critChanceBonus);
    }

    public static float DefenseReduction(int defense)
    {
        return Mathf.Min(MaxDefReduction, defense * DefReductionPerPoint);
    }
}
