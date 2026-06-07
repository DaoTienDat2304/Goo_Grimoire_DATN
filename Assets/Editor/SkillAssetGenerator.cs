#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Chạy một lần: Tools → Skills → Generate All Skill Assets
/// Xóa asset cũ lỗi thời, tạo đầy đủ SkillEffectSO + SkillSO mới.
/// </summary>
public static class SkillAssetGenerator
{
    const string EFFECT_DIR = "Assets/SkillDB/SkillDB/EffectType";
    const string WEAPON_DIR = "Assets/SkillDB/SkillDB/WeaponSkill";
    const string BODY_DIR   = "Assets/SkillDB/SkillDB/BodySkill";
    const string HAT_DIR    = "Assets/SkillDB/SkillDB/HatSkill";
    const string TEST_DIR   = "Assets/SkillDB/SkillDB/TestSkill";

    [MenuItem("Tools/Skills/Generate All Skill Assets")]
    public static void GenerateAll()
    {
        EnsureDir(EFFECT_DIR);
        EnsureDir(WEAPON_DIR);
        EnsureDir(BODY_DIR);
        EnsureDir(HAT_DIR);
        EnsureDir(TEST_DIR);

        // Xóa asset EffectType cũ (có trường value/duration thừa từ thiết kế cũ)
        DeleteOld(EFFECT_DIR, "Attack 1");
        DeleteOld(EFFECT_DIR, "SelfHeal");
        DeleteOld(EFFECT_DIR, "DefendBuff");

        // ── 1. SkillEffectSO ─────────────────────────────────────────────

        // Damage
        var dmgSingle     = Effect("Dmg_Single_AttackTarget",    EffectType.Damage,  TargetSide.Enemies, AoEShape.Single,    AnchorType.AttackTarget);
        var dmgRow        = Effect("Dmg_Row_AttackTarget",        EffectType.Damage,  TargetSide.Enemies, AoEShape.Row,       AnchorType.AttackTarget);
        var dmgCol        = Effect("Dmg_Col_AttackTarget",        EffectType.Damage,  TargetSide.Enemies, AoEShape.Column,    AnchorType.AttackTarget);
        var dmgFullEnemy  = Effect("Dmg_FullSide_Enemy",          EffectType.Damage,  TargetSide.Enemies, AoEShape.FullSide,  AnchorType.AttackTarget);
        var dmgEverything = Effect("Dmg_Everything",              EffectType.Damage,  TargetSide.All,     AoEShape.Everything,AnchorType.Self);
        var dmgRandom     = Effect("Dmg_Single_RandomEnemy",      EffectType.Damage,  TargetSide.Enemies, AoEShape.Single,    AnchorType.RandomEnemy);
        var dmgLowest     = Effect("Dmg_Single_LowestHPEnemy",    EffectType.Damage,  TargetSide.Enemies, AoEShape.Single,    AnchorType.LowestHPEnemy);

        // Heal
        var healSelf      = Effect("Heal_Single_Self",            EffectType.Heal,    TargetSide.Allies,  AoEShape.Single,    AnchorType.Self);
        var healLowest    = Effect("Heal_Single_LowestHPAlly",    EffectType.Heal,    TargetSide.Allies,  AoEShape.Single,    AnchorType.LowestHPAlly);
        var healRandom    = Effect("Heal_Single_RandomAlly",      EffectType.Heal,    TargetSide.Allies,  AoEShape.Single,    AnchorType.RandomAlly);
        var healAll       = Effect("Heal_FullSide_Ally",          EffectType.Heal,    TargetSide.Allies,  AoEShape.FullSide,  AnchorType.Self);

        // Buff – Attack
        var buffAtkSelf   = Effect("Buff_Atk_Single_Self",        EffectType.Buff,    TargetSide.Allies,  AoEShape.Single,    AnchorType.Self,         BuffStat.Attack);
        var buffAtkAll    = Effect("Buff_Atk_FullSide_Ally",      EffectType.Buff,    TargetSide.Allies,  AoEShape.FullSide,  AnchorType.Self,         BuffStat.Attack);

        // Buff – Defense
        var buffDefSelf   = Effect("Buff_Def_Single_Self",        EffectType.Buff,    TargetSide.Allies,  AoEShape.Single,    AnchorType.Self,         BuffStat.Defense);
        var buffDefAll    = Effect("Buff_Def_FullSide_Ally",      EffectType.Buff,    TargetSide.Allies,  AoEShape.FullSide,  AnchorType.Self,         BuffStat.Defense);

        // Buff – Speed
        var buffSpdSelf   = Effect("Buff_Spd_Single_Self",        EffectType.Buff,    TargetSide.Allies,  AoEShape.Single,    AnchorType.Self,         BuffStat.Speed);
        var buffSpdAll    = Effect("Buff_Spd_FullSide_Ally",      EffectType.Buff,    TargetSide.Allies,  AoEShape.FullSide,  AnchorType.Self,         BuffStat.Speed);

        // Debuff – Attack
        var debuffAtkSingle = Effect("Debuff_Atk_Single_Target",  EffectType.Debuff,  TargetSide.Enemies, AoEShape.Single,    AnchorType.AttackTarget, BuffStat.Attack);
        var debuffAtkAll    = Effect("Debuff_Atk_FullSide_Enemy", EffectType.Debuff,  TargetSide.Enemies, AoEShape.FullSide,  AnchorType.AttackTarget, BuffStat.Attack);

        // Debuff – Defense
        var debuffDefSingle = Effect("Debuff_Def_Single_Target",  EffectType.Debuff,  TargetSide.Enemies, AoEShape.Single,    AnchorType.AttackTarget, BuffStat.Defense);
        var debuffDefAll    = Effect("Debuff_Def_FullSide_Enemy", EffectType.Debuff,  TargetSide.Enemies, AoEShape.FullSide,  AnchorType.AttackTarget, BuffStat.Defense);

        // Debuff – Speed
        var debuffSpdSingle = Effect("Debuff_Spd_Single_Target",  EffectType.Debuff,  TargetSide.Enemies, AoEShape.Single,    AnchorType.AttackTarget, BuffStat.Speed);
        var debuffSpdAll    = Effect("Debuff_Spd_FullSide_Enemy", EffectType.Debuff,  TargetSide.Enemies, AoEShape.FullSide,  AnchorType.AttackTarget, BuffStat.Speed);

        // Stun
        var stunSingle    = Effect("Stun_Single_AttackTarget",    EffectType.Stun,    TargetSide.Enemies, AoEShape.Single,    AnchorType.AttackTarget);
        var stunRow       = Effect("Stun_Row_AttackTarget",        EffectType.Stun,    TargetSide.Enemies, AoEShape.Row,       AnchorType.AttackTarget);
        var stunAll       = Effect("Stun_FullSide_Enemy",          EffectType.Stun,    TargetSide.Enemies, AoEShape.FullSide,  AnchorType.AttackTarget);

        // ── 2. Tái tạo skill cũ ──────────────────────────────────────────

        // KnightSlash — đòn chém + hồi máu bản thân (lifesteal)
        Skill("KnightSlash", WEAPON_DIR, SkillType.Active, "Knight Slash",
            "Chém mạnh một mục tiêu, hồi lại 10% HP tối đa.", cooldown: 3,
            (dmgSingle,  1.2f, 0, 100f),
            (healSelf,   0.1f, 0, 100f));

        // Regeneration — hồi máu đồng minh ít HP nhất
        Skill("Regeneration", BODY_DIR, SkillType.Active, "Regeneration",
            "Hồi 20% HP cho đồng minh ít máu nhất.", cooldown: 2,
            (healLowest, 0.2f, 0, 100f));

        // KnightProtection — buff phòng thủ toàn đội
        Skill("KnightProtection", HAT_DIR, SkillType.Active, "Knight Protection",
            "Tăng 50% phòng thủ cho toàn đội trong 2 lượt.", cooldown: 3,
            (buffDefAll, 1.5f, 2, 100f));

        // ── 3. Test skills ────────────────────────────────────────────────

        Skill("Test_SingleDmg", TEST_DIR, SkillType.Active, "Test: Đòn Đơn",
            "Gây 100% ATK cho 1 mục tiêu.", cooldown: 1,
            (dmgSingle, 1.0f, 0, 100f));

        Skill("Test_RowDmg", TEST_DIR, SkillType.Active, "Test: Phá Hàng",
            "Gây 80% ATK cho cả hàng kẻ địch.", cooldown: 2,
            (dmgRow, 0.8f, 0, 100f));

        Skill("Test_ColDmg", TEST_DIR, SkillType.Active, "Test: Chặt Cột",
            "Gây 80% ATK cho cả cột kẻ địch.", cooldown: 2,
            (dmgCol, 0.8f, 0, 100f));

        Skill("Test_AllEnemyDmg", TEST_DIR, SkillType.Active, "Test: Bão Lửa",
            "Gây 60% ATK cho tất cả kẻ địch.", cooldown: 4,
            (dmgFullEnemy, 0.6f, 0, 100f));

        Skill("Test_SelfHeal", TEST_DIR, SkillType.Active, "Test: Tự Hồi",
            "Hồi 20% HP tối đa bản thân.", cooldown: 2,
            (healSelf, 0.2f, 0, 100f));

        Skill("Test_GroupHeal", TEST_DIR, SkillType.Active, "Test: Hồi Nhóm",
            "Hồi 15% HP tối đa cho toàn đội.", cooldown: 4,
            (healAll, 0.15f, 0, 100f));

        Skill("Test_SelfAtkBuff", TEST_DIR, SkillType.Active, "Test: Buff Công",
            "Tăng 50% tấn công bản thân trong 3 lượt.", cooldown: 3,
            (buffAtkSelf, 1.5f, 3, 100f));

        Skill("Test_GroupDefBuff", TEST_DIR, SkillType.Active, "Test: Buff Thủ",
            "Tăng 40% phòng thủ toàn đội trong 2 lượt.", cooldown: 4,
            (buffDefAll, 1.4f, 2, 100f));

        Skill("Test_DefDebuff", TEST_DIR, SkillType.Active, "Test: Phá Giáp",
            "Giảm 30% phòng thủ mục tiêu trong 2 lượt.", cooldown: 2,
            (debuffDefSingle, 0.7f, 2, 100f));

        Skill("Test_AtkDebuffAll", TEST_DIR, SkillType.Active, "Test: Suy Yếu",
            "Giảm 20% tấn công toàn bộ kẻ địch trong 3 lượt.", cooldown: 3,
            (debuffAtkAll, 0.8f, 3, 100f));

        Skill("Test_StunSingle", TEST_DIR, SkillType.Active, "Test: Choáng Đơn",
            "Choáng 1 mục tiêu trong 1 lượt.", cooldown: 2,
            (stunSingle, 1.0f, 1, 100f));

        Skill("Test_DmgStun", TEST_DIR, SkillType.Active, "Test: Đập Choáng",
            "Đánh + 75% cơ hội choáng mục tiêu 1 lượt.", cooldown: 3,
            (dmgSingle,  1.0f, 0, 100f),
            (stunSingle, 1.0f, 1,  75f));

        Skill("Test_RandomDmg", TEST_DIR, SkillType.Active, "Test: Đòn Ngẫu Nhiên",
            "Đánh kẻ địch ngẫu nhiên với 130% ATK.", cooldown: 1,
            (dmgRandom, 1.3f, 0, 100f));

        Skill("Test_SlowStrike", TEST_DIR, SkillType.Active, "Test: Đòn Chậm",
            "Đánh + 80% cơ hội giảm 50% tốc độ mục tiêu 2 lượt.", cooldown: 2,
            (dmgSingle,       1.0f, 0, 100f),
            (debuffSpdSingle, 0.5f, 2,  80f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SkillGen] Hoàn tất! Tạo 26 SkillEffectSO + 17 SkillSO (3 cũ + 14 test).");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    static SkillEffectSO Effect(string name, EffectType type, TargetSide side,
        AoEShape shape, AnchorType anchor, BuffStat buffStat = BuffStat.Defense)
    {
        string path = $"{EFFECT_DIR}/{name}.asset";
        if (AssetDatabase.LoadAssetAtPath<SkillEffectSO>(path) != null)
            AssetDatabase.DeleteAsset(path);

        var so        = ScriptableObject.CreateInstance<SkillEffectSO>();
        so.type       = type;
        so.targetSide = side;
        so.aoeShape   = shape;
        so.anchorType = anchor;
        so.buffStat   = buffStat;
        AssetDatabase.CreateAsset(so, path);
        return so;
    }

    static void Skill(string fileName, string dir, SkillType skillType,
        string skillName, string desc, int cooldown,
        params (SkillEffectSO effect, float value, int duration, float applyChance)[] entries)
    {
        string path = $"{dir}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<SkillSO>(path) != null)
            AssetDatabase.DeleteAsset(path);

        var so         = ScriptableObject.CreateInstance<SkillSO>();
        so.skillName   = skillName;
        so.type        = skillType;
        so.description = desc;
        so.cooldown    = cooldown;
        so.effects     = new List<EffectEntry>();

        foreach (var (effect, value, duration, applyChance) in entries)
        {
            so.effects.Add(new EffectEntry
            {
                effect      = effect,
                value       = value,
                duration    = duration,
                applyChance = applyChance
            });
        }

        AssetDatabase.CreateAsset(so, path);
    }

    static void DeleteOld(string dir, string name)
    {
        string path = $"{dir}/{name}.asset";
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
            Debug.Log($"[SkillGen] Đã xóa asset cũ: {path}");
        }
    }

    static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
        string folder = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif
