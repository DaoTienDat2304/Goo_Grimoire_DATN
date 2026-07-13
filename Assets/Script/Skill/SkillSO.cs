using System.Collections.Generic;
using UnityEngine;

public enum SkillType { BasicAttack, Active, Passive, Ultimate }

[System.Serializable]
public class EffectEntry
{
    public SkillEffectSO effect;
    public float value; // Multiplier, % HP, hoặc % AV
    public int flatBonus; // Số N cộng cố định (vd: 150% ATK + 325) -> flatBonus = 325
    public int duration; // Số lượt
    public float applyChance = 100f; // Tỉ lệ trúng
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "SlimeGame/Skill")]
public class SkillSO : ScriptableObject
{
    public string skillName;
    public SkillType type;
    public string description;
    public Sprite icon;

    [Header("Generation Tags")]
    public TraitType targetTrait; // Nhận diện loại bộ phận (Weapon, Armor, Body)
    public Rarity rarity;         // Nhận diện độ hiếm (Common, Rare,...)

    [Header("Resource Costs")]
    public int battlePointCost = 0; // Tốn bao nhiêu ĐCK (Thường 1-3)
    public int battlePointGain = 0; // Hồi bao nhiêu ĐCK (Đánh thường = 1)
    public int energyCost = 0; // Tốn năng lượng (Ultimate = 100)
    public int energyGain = 0; // Hồi năng lượng khi dùng (Basic = 20, Active = 25)

    public List<EffectEntry> effects = new();
}