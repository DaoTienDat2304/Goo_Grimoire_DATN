using System.Collections.Generic;
using UnityEngine;

public enum SkillType { BasicAttack, Active, Passive, Ultimate }

[System.Serializable]
public class EffectEntry
{
    public SkillEffectSO effect;
    public float value;
    public int flatBonus;
    public int duration;
    public float applyChance = 100f;
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "SlimeGame/Skill")]
public class SkillSO : ScriptableObject
{
    public string skillName;
    public SkillType type;
    public string description;
    public Sprite icon;

    [Header("Generation Tags")]
    public TraitType targetTrait;
    public Rarity rarity;

    [Header("Resource Costs")]
    public int battlePointCost = 0;
    public int battlePointGain = 0;
    public int energyCost = 0;
    public int energyGain = 0;

    public List<EffectEntry> effects = new();
}
