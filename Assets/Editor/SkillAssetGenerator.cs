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

        // ── 1. SkillEffectSO ─────────────────────────────────────────────

        // Sát thương (Damage)
        var dmgSingle     = Effect("Dmg_Single_AttackTarget",    EffectType.Damage,  TargetSide.Enemies, AoEShape.Single,    AnchorType.AttackTarget);
        var dmgBlast      = Effect("Dmg_Blast_AttackTarget",     EffectType.Damage,  TargetSide.Enemies, AoEShape.Blast,     AnchorType.AttackTarget);
        var dmgFullEnemy  = Effect("Dmg_FullSide_Enemy",          EffectType.Damage,  TargetSide.Enemies, AoEShape.FullSide,  AnchorType.AttackTarget);

        // Hồi máu (Heal)
        var healSelf      = Effect("Heal_Single_Self",            EffectType.Heal,    TargetSide.Allies,  AoEShape.Single,    AnchorType.Self);
        var healAll       = Effect("Heal_FullSide_Ally",          EffectType.Heal,    TargetSide.Allies,  AoEShape.FullSide,  AnchorType.Self);

        // Hỗ trợ (Buff)
        var buffDefSelf   = Effect("Buff_Def_Single_Self",        EffectType.Buff,    TargetSide.Allies,  AoEShape.Single,    AnchorType.Self,         BuffStat.Defense);
        var buffDefAll    = Effect("Buff_Def_FullSide_Ally",      EffectType.Buff,    TargetSide.Allies,  AoEShape.FullSide,  AnchorType.Self,         BuffStat.Defense);
        var buffAtkSelf   = Effect("Buff_Atk_Single_Self",        EffectType.Buff,    TargetSide.Allies,  AoEShape.Single,    AnchorType.Self,         BuffStat.Attack);
        var buffAtkAll    = Effect("Buff_Atk_FullSide_Ally",      EffectType.Buff,    TargetSide.Allies,  AoEShape.FullSide,  AnchorType.Self,         BuffStat.Attack);

        // Giảm sức mạnh (Debuff)
        var debuffDefSingle = Effect("Debuff_Def_Single_Target",  EffectType.Debuff,  TargetSide.Enemies, AoEShape.Single,    AnchorType.AttackTarget, BuffStat.Defense);
        var debuffDefAll    = Effect("Debuff_Def_FullSide_Enemy", EffectType.Debuff,  TargetSide.Enemies, AoEShape.FullSide,  AnchorType.AttackTarget, BuffStat.Defense);
        var debuffAtkSingle = Effect("Debuff_Atk_Single_Target",  EffectType.Debuff,  TargetSide.Enemies, AoEShape.Single,    AnchorType.AttackTarget, BuffStat.Attack);
        var debuffAtkAll    = Effect("Debuff_Atk_FullSide_Enemy", EffectType.Debuff,  TargetSide.Enemies, AoEShape.FullSide,  AnchorType.AttackTarget, BuffStat.Attack);

        // Khống chế (Stun)
        var stunSingle    = Effect("Stun_Single_AttackTarget",    EffectType.Stun,    TargetSide.Enemies, AoEShape.Single,    AnchorType.AttackTarget);

        // ── 2. Tái tạo các skill tiêu chuẩn ───────────────────────────────

        // KnightSlash — đòn chém đơn + hồi máu bản thân
        Skill("KnightSlash", WEAPON_DIR, SkillType.Active, "Knight Slash",
            "Chém mạnh một mục tiêu, hồi lại 10% HP tối đa.", cooldown: 3,
            (dmgSingle,  1.2f, 0, 100f),
            (healSelf,   0.1f, 0, 100f));

        // Regeneration — hồi máu toàn đội
        Skill("Regeneration", BODY_DIR, SkillType.Active, "Regeneration",
            "Hồi 20% HP cho toàn bộ đồng minh.", cooldown: 2,
            (healAll, 0.2f, 0, 100f));

        // KnightProtection — buff phòng thủ toàn đội
        Skill("KnightProtection", HAT_DIR, SkillType.Active, "Knight Protection",
            "Tăng 50% phòng thủ cho toàn đội trong 2 lượt.", cooldown: 3,
            (buffDefAll, 1.5f, 2, 100f));

        // ── 3. Test skills ────────────────────────────────────────────────

        Skill("Test_SingleDmg", TEST_DIR, SkillType.Active, "Test: Đòn Đơn",
            "Gây 100% ATK cho 1 mục tiêu.", cooldown: 1,
            (dmgSingle, 1.0f, 0, 100f));

        Skill("Test_BlastDmg", TEST_DIR, SkillType.Active, "Test: Đòn Lan (Blast)",
            "Gây 100% ATK cho mục tiêu chính và 50% ATK cho các mục tiêu lân cận.", cooldown: 2,
            (dmgBlast, 1.0f, 0, 100f));

        Skill("Test_AllEnemyDmg", TEST_DIR, SkillType.Active, "Test: Bão Lửa (AoE)",
            "Gây 60% ATK cho tất cả kẻ địch.", cooldown: 3,
            (dmgFullEnemy, 0.6f, 0, 100f));

        Skill("Test_SelfHeal", TEST_DIR, SkillType.Active, "Test: Tự Hồi",
            "Hồi 20% HP tối đa bản thân.", cooldown: 2,
            (healSelf, 0.2f, 0, 100f));

        Skill("Test_GroupHeal", TEST_DIR, SkillType.Active, "Test: Hồi Nhóm",
            "Hồi 15% HP tối đa cho toàn đội.", cooldown: 3,
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

        // ── HSR Custom Test skills ───────────────────────────────────────
        Skill("Test_HSR_SingleDmg", TEST_DIR, SkillType.Active, "HSR Test: Đơn Mục Tiêu",
            "Tấn công đơn thể, gây 1.5x sát thương cho 1 kẻ địch.", cooldown: 1,
            (dmgSingle, 1.5f, 0, 100f));

        Skill("Test_HSR_GroupHeal", TEST_DIR, SkillType.Active, "HSR Test: Hồi Máu Toàn Đội",
            "Hỗ trợ toàn đội, hồi 25% HP tối đa cho tất cả đồng minh.", cooldown: 3,
            (healAll, 0.25f, 0, 100f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AutoAssignSkillsToTraits();

        Debug.Log("[SkillGen] Hoàn tất! Tạo các SkillEffectSO + SkillSO mới và tự động gán.");
    }

    private static void AutoAssignSkillsToTraits()
    {
        var singleDmgSkill = AssetDatabase.LoadAssetAtPath<SkillSO>($"{TEST_DIR}/Test_HSR_SingleDmg.asset");
        var groupHealSkill = AssetDatabase.LoadAssetAtPath<SkillSO>($"{TEST_DIR}/Test_HSR_GroupHeal.asset");
        var groupDefSkill = AssetDatabase.LoadAssetAtPath<SkillSO>($"{TEST_DIR}/Test_GroupDefBuff.asset");

        if (singleDmgSkill == null || groupHealSkill == null)
        {
            Debug.LogError("[SkillGen] Could not find custom HSR test skills to auto-assign!");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:TraitSO", new[] { "Assets/TraitDatabase" });
        int assignedCount = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var trait = AssetDatabase.LoadAssetAtPath<TraitSO>(path);
            if (trait != null)
            {
                if (trait.type == TraitType.Weapon)
                {
                    trait.skill = singleDmgSkill;
                    EditorUtility.SetDirty(trait);
                    assignedCount++;
                }
                else if (trait.type == TraitType.Body)
                {
                    trait.skill = groupHealSkill;
                    EditorUtility.SetDirty(trait);
                    assignedCount++;
                }
                else if (trait.type == TraitType.Armor)
                {
                    trait.skill = groupDefSkill;
                    EditorUtility.SetDirty(trait);
                    assignedCount++;
                }
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[SkillGen] Auto-assigned skills to {assignedCount} TraitSO assets!");
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

    static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
        string folder = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif
