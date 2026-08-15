using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chuẩn hoá chỉ số slime CŨ về đúng quy chuẩn GDD (StatBalance). Chạy mỗi lần load.
/// IDEMPOTENT: chỉ đụng vào slime còn lệch chuẩn; slime đã đúng thì bỏ qua, nên có thể
/// chạy lặp lại an toàn (kể cả khi chưa kịp save).
///
/// Xử lý 3 nhóm slime bị lệch do lịch sử:
///  (1) Secret cũ: bị ATK/Magic/Crit = 0 và HP sai scale (body-only nhưng body không roll đủ).
///  (2) Slime nở-từ-trứng cũ: dùng bảng scale nhỏ riêng (đã bỏ), giờ kéo về StatBalance theo
///      đúng percentile chất lượng đã lưu (eggStatRollPercent) để không mất "chất lượng" roll.
///  (3) Mythic dính lỗi typo HP (roll 2200–50000 thay vì 22000–50000): remap giữ nguyên percentile.
///
/// Lưu ý: phần "ATK/DEF/SPD bị nhân hệ số độ hiếm lúc load" đã được sửa tại
/// TraitInstance.RecalculateStats nên tự chuẩn hoá khi load, không cần migrate ở đây.
/// </summary>
public static class StatStandardMigration
{
    public static int NormalizeAll(IEnumerable<Slime> slimes)
    {
        if (slimes == null) return 0;
        int changed = 0;
        foreach (var s in slimes)
            if (s != null && Normalize(s)) changed++;
        if (changed > 0)
            Debug.Log($"[StatMigration] Đã chuẩn hoá {changed} slime cũ về quy chuẩn GDD mới.");
        return changed;
    }

    // Trả về true nếu slime bị sửa.
    private static bool Normalize(Slime s)
    {
        Rarity rarity = GetRarity(s);

        // (1) Secret — đảm bảo toàn bộ bộ phận Body, Armor, Weapon đều mang Rarity.Secret và có Kỹ năng Secret
        if (s.body != null && s.body.Rarity == Rarity.Secret)
        {
            bool modified = false;
            if (s.armor != null && s.armor.Rarity != Rarity.Secret)
            {
                s.armor.Rarity = Rarity.Secret;
                modified = true;
            }
            if (s.weapon != null && s.weapon.Rarity != Rarity.Secret)
            {
                s.weapon.Rarity = Rarity.Secret;
                modified = true;
            }

            // Đảm bảo có đầy đủ bộ kỹ năng Secret (Body, Hat, Weapon _A, Weapon _U)
            if (s.armor?.skill == null || s.armor.skill.baseSkill?.rarity != Rarity.Secret
                || s.weapon?.skill == null || s.weapon.skill.baseSkill?.rarity != Rarity.Secret
                || s.weapon?.ultimateSkill == null || s.weapon.ultimateSkill.baseSkill?.rarity != Rarity.Secret
                || s.body?.skill == null || s.body.skill.baseSkill?.rarity != Rarity.Secret)
            {
                s.RollRandomSkillsMatchingRarity();
                modified = true;
            }

            var rs = StatBalance.Get(Rarity.Secret);
            bool needs = s.body.attack <= 0 || s.body.HP < rs.hpMin;
            if (needs)
            {
                // Giữ "chất lượng" theo percentile HP cũ (range Secret body-only cũ ~9000–16000).
                float p = Mathf.Clamp01((s.body.HP - 9000f) / (16000f - 9000f));
                SetStat(s.body, LerpInt(rs.hpMin, rs.hpMax, p), LerpInt(rs.atkMin, rs.atkMax, p),
                        LerpInt(rs.magMin, rs.magMax, p), LerpInt(rs.defMin, rs.defMax, p),
                        LerpInt(rs.spdMin, rs.spdMax, p), rs.critRate, rs.critDmg);
                modified = true;
            }

            if (modified)
            {
                s.CalculateStats();
                return true;
            }
            return false;
        }

        // (2) Slime nở-từ-trứng — "chưa migrate" khi HP body dưới min chuẩn của độ hiếm.
        if (!string.IsNullOrEmpty(s.eggStatQuality) && s.body != null)
        {
            var r = StatBalance.Get(rarity);
            if (s.body.HP >= r.hpMin) return false; // đã đúng chuẩn

            float t = Mathf.Clamp01(s.eggStatRollPercent / 100f);
            s.body.HP = s.body.baseHP = LerpInt(r.hpMin, r.hpMax, t);
            s.body.defense = s.body.baseDefense = LerpInt(r.defMin, r.defMax, t);
            s.body.speed = s.body.baseSpeed = LerpInt(r.spdMin, r.spdMax, t);
            if (s.weapon != null)
            {
                s.weapon.attack = s.weapon.baseAttack = LerpInt(r.atkMin, r.atkMax, t);
                s.weapon.magicAttack = s.weapon.baseMagicAttack = LerpInt(r.magMin, r.magMax, t);
            }
            if (s.armor != null)
            {
                s.armor.critRate = s.armor.baseCritRate = r.critRate;
                s.armor.critDMG = s.armor.baseCritDMG = r.critDmg;
            }
            s.CalculateStats();
            return true;
        }

        // (3) Mythic dính lỗi typo HP: remap [2200,50000] -> [22000,50000] giữ nguyên percentile.
        if (s.body != null && s.body.Rarity == Rarity.Mythic && s.body.HP < 22000)
        {
            float p = Mathf.Clamp01((s.body.HP - 2200f) / (50000f - 2200f));
            s.body.HP = s.body.baseHP = LerpInt(22000, 50000, p);
            s.CalculateStats();
            return true;
        }

        // (4) Đảm bảo có ĐỦ Crit Rate/Damage & Magic ATK theo design — sửa slime bậc thấp cũ
        //     (trước đây Common/Uncommon chỉ random 1 trong 2 crit, và có thể thiếu Magic ATK).
        if (EnsureCritAndMagic(s))
        {
            s.CalculateStats();
            return true;
        }

        return false;
    }

    // Bổ sung Crit Rate/Damage (cố định theo độ hiếm) & Magic ATK (nếu đang = 0) cho khớp design.
    private static bool EnsureCritAndMagic(Slime s)
    {
        bool changed = false;

        if (s.armor != null)
        {
            var r = StatBalance.Get(s.armor.Rarity);
            if (!Mathf.Approximately(s.armor.critRate, r.critRate))
            {
                s.armor.critRate = s.armor.baseCritRate = r.critRate;
                changed = true;
            }
            if (!Mathf.Approximately(s.armor.critDMG, r.critDmg))
            {
                s.armor.critDMG = s.armor.baseCritDMG = r.critDmg;
                changed = true;
            }
        }

        if (s.weapon != null && s.weapon.magicAttack <= 0)
        {
            var r = StatBalance.Get(s.weapon.Rarity);
            s.weapon.magicAttack = s.weapon.baseMagicAttack = (r.magMin + r.magMax) / 2;
            changed = true;
        }

        return changed;
    }

    private static void SetStat(TraitInstance t, int hp, int atk, int mag, int def, int spd,
                                float critRate, float critDmg)
    {
        t.HP = t.baseHP = hp;
        t.attack = t.baseAttack = atk;
        t.magicAttack = t.baseMagicAttack = mag;
        t.defense = t.baseDefense = def;
        t.speed = t.baseSpeed = spd;
        t.critRate = t.baseCritRate = critRate;
        t.critDMG = t.baseCritDMG = critDmg;
    }

    private static Rarity GetRarity(Slime s)
        => s.body != null ? s.body.Rarity : (s.weapon != null ? s.weapon.Rarity : Rarity.Common);

    private static int LerpInt(int min, int max, float t) => Mathf.RoundToInt(Mathf.Lerp(min, max, t));
}
