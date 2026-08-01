using UnityEngine;
using Spine.Unity;

public enum TraitType { Body, Armor, Weapon , special}
public enum Rarity { Common, Uncommon, Rare, SuperRare, UltraRare, Legendary, Mythic, Secret }

[CreateAssetMenu(fileName = "NewTrait", menuName = "SlimeGame/Trait")]
public class TraitSO : ScriptableObject
{
    public string traitName;
    public TraitType type;
    public Rarity rarity;
    public bool unlocked = false;

    [Range(0f, 100f)] public float dropRate;

    public Sprite sprite;
    [Header("Animation")]
    public SkeletonDataAsset animationAsset; // Spine animation asset cho trait
    public string animationName = "animation"; // Tên animation mặc định
    
    public Vector2Int HPRange;
    public Vector2Int attackRange;
    public Vector2Int magicAttackRange;
    public Vector2Int defenseRange;
    public Vector2Int speedRange;
    public Vector2Int critRateRange;
    public Vector2Int critDMGRange;
    public SkillSO skill;
    public SkillSO ultimateSkill;

    public TraitInstance GenerateInstance()
    {
        return new TraitInstance(this);
    }
}