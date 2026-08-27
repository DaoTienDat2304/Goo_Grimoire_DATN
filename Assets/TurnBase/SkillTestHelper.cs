#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// </summary>
public class SkillTestHelper : MonoBehaviour
{
    [Header("Assign TurnSystem and FormationManager")]
    public TurnSystem turnSystem;
    public FormationManager formationManager;

    [Header("Skills to test (index 0–3 keys 1–4)")]
    public SkillSO[] testSkills = new SkillSO[4];

    [Header("Skill power test (default 1.0)")]
    public float testSkillPower = 1f;

    private readonly int[] _cooldowns = new int[4];

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) FireSkill(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) FireSkill(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) FireSkill(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) FireSkill(3);

        if (Input.GetKeyDown(KeyCode.R)) ResetAllCooldowns();
    }

    [ContextMenu("Test Skill [0]")] void TestSkill0() => FireSkill(0);
    [ContextMenu("Test Skill [1]")] void TestSkill1() => FireSkill(1);
    [ContextMenu("Test Skill [2]")] void TestSkill2() => FireSkill(2);
    [ContextMenu("Test Skill [3]")] void TestSkill3() => FireSkill(3);
    [ContextMenu("Reset All Cooldowns")] void ResetAllCooldowns()
    {
        for (int i = 0; i < _cooldowns.Length; i++) _cooldowns[i] = 0;
    }

    void FireSkill(int index)
    {
        if (index >= testSkills.Length || testSkills[index] == null)
        {
            Debug.LogWarning($"[SkillTest] testSkills[{index}] not assigned.");
            return;
        }

        if (turnSystem == null || formationManager == null)
        {
            Debug.LogWarning("[SkillTest] Missing TurnSystem hoac FormationManager.");
            return;
        }

        var skill = testSkills[index];

        if (_cooldowns[index] > 0)
        {
            Debug.LogWarning($"[SkillTest] Skill [{index}] '{skill.skillName}' cooldown left {_cooldowns[index]} turn. Press R to reset.");
            return;
        }

        var boss = turnSystem.boss;

        foreach (var entry in skill.effects)
        {
            if (entry.effect == null) continue;

            List<GameObject> targets = formationManager.ResolveTargets(entry, boss, boss, boss);

            if (targets.Count == 0)
            {
                continue;
            }

            foreach (var go in targets)
            {
                if (!go.TryGetComponent<SlimeBattleStats>(out var stats)) continue;

                // Roll applyChance
                if (Random.Range(0f, 100f) > entry.applyChance)
                {
                    continue;
                }

                int hpBefore = stats.CurrentHP;

                switch (entry.effect.type)
                {
                    case EffectType.Damage:
                        int dmg = Mathf.RoundToInt(stats.BattleAttack * testSkillPower * entry.value);
                        stats.TakeDamage(dmg);
                        break;

                    case EffectType.Heal:
                        int heal = Mathf.RoundToInt(stats.MaxHP * testSkillPower * entry.value);
                        stats.Heal(heal);
                        break;

                    case EffectType.Buff:
                        stats.ApplyBuff(entry.effect.buffStat, testSkillPower * entry.value, entry.duration, false);
                        break;

                    case EffectType.Debuff:
                        stats.ApplyBuff(entry.effect.buffStat, testSkillPower * entry.value, entry.duration, true);
                        break;

                    case EffectType.Stun:
                        stats.ApplyStun(entry.duration);
                        break;
                }
            }
        }

    }
}

#endif
