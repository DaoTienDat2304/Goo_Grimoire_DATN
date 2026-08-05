using UnityEngine;

// Công thức chỉ số chiến đấu DÙNG CHUNG cho cả:
//  - Trong trận: SlimeBattleStats (GetEffective*, GetFinal*, TakeDamage)
//  - Ngoài trận (home/firstsave): ShowSlime, SlimeDisplayBehavior
// Mục tiêu: chỉ số hiển thị bên ngoài LUÔN khớp với chỉ số thực chiến trong trận.
// Nếu cần cân bằng lại, chỉ sửa DUY NHẤT ở đây.
public static class BattleStatFormula
{
    // Các hằng số này có thể chỉnh từ xa qua Remote Config (nhóm `battle_*`).
    // Không có Remote Config → RemoteBalance.Battle giữ đúng giá trị gốc bên dưới.
    public static float CritRateCap => RemoteBalance.Battle.critRateCap;                   // Crit Rate tối đa 75%
    public static float CritDMGCap => RemoteBalance.Battle.critDmgCap;                     // Crit DMG tối đa 250%
    public static float MaxDefReduction => RemoteBalance.Battle.maxDefReduction;           // Giảm sát thương tối đa 80% (design cap 75–80%)
    public static float DefReductionPerPoint => RemoteBalance.Battle.defReductionPerPoint; // 1 DEF = 0.8% giảm sát thương (theo design: DEF×0.008)
    public static float CritOverflowToAtk => RemoteBalance.Battle.critOverflowToAtk;       // 1% Crit DMG vượt cap = 5 ATK/Magic ATK

    // Crit Rate hiệu dụng (chưa cap). critChanceBonus là buff cộng thêm dạng % (vd 10 = +10%).
    public static float EffectiveCritRate(float baseCritRate, float critChanceBonus = 0f)
    {
        return baseCritRate + critChanceBonus / 100f;
    }

    // Crit DMG hiệu dụng (chưa cap): phần Crit Rate vượt 75% quy đổi 1:1 sang Crit DMG.
    public static float EffectiveCritDMG(float baseCritRate, float baseCritDMG, float critChanceBonus = 0f)
    {
        float rate = EffectiveCritRate(baseCritRate, critChanceBonus);
        float excessCritRate = Mathf.Max(0f, rate - CritRateCap);
        return baseCritDMG + excessCritRate; // quy đổi 1:1
    }

    public static float FinalCritRate(float baseCritRate, float critChanceBonus = 0f)
    {
        return Mathf.Min(CritRateCap, EffectiveCritRate(baseCritRate, critChanceBonus));
    }

    public static float FinalCritDMG(float baseCritRate, float baseCritDMG, float critChanceBonus = 0f)
    {
        return Mathf.Min(CritDMGCap, EffectiveCritDMG(baseCritRate, baseCritDMG, critChanceBonus));
    }

    // Phần Crit DMG vượt 250% quy đổi sang ATK: 1% vượt = 5 ATK.
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

    // Tỉ lệ giảm sát thương do DEF (0..0.80).
    public static float DefenseReduction(int defense)
    {
        return Mathf.Min(MaxDefReduction, defense * DefReductionPerPoint);
    }
}
