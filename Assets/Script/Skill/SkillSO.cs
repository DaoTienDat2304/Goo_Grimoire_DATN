using System.Collections.Generic;
using UnityEngine;

public enum SkillType { Active, Passive, Ultimate }

// Wrapper gắn một SkillEffectSO (targeting/loại) với chỉ số cụ thể của skill này.
// Cho phép nhiều skill dùng chung một SkillEffectSO nhưng có value/duration khác nhau.
[System.Serializable]
public class EffectEntry
{
    public SkillEffectSO effect;            // targeting & loại hiệu ứng (asset dùng chung)
    public float value;                     // Damage multiplier / Heal% / Buff multiplier (>1) / Debuff multiplier (<1)
    public int duration;                    // Số lượt tồn tại (Buff/Debuff/Stun); 0 = vĩnh viễn
    public float applyChance = 100f;        // % áp dụng lên mỗi target (0–100)
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "SlimeGame/Skill")]
public class SkillSO : ScriptableObject
{
    public string skillName;
    public SkillType type;
    public string description;
    public Sprite icon;
    public int cooldown;
    public List<EffectEntry> effects = new();
}

