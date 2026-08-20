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
    public string animationName = "animation";
    
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

public static class RarityExtensions
{
    public static string ToVietnamese(this Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return "Common";
            case Rarity.Uncommon:  return "Uncommon";
            case Rarity.Rare:      return "Rare";
            case Rarity.SuperRare: return "Super Rare";
            case Rarity.UltraRare: return "Ultra Rare";
            case Rarity.Legendary: return "Legendary";
            case Rarity.Mythic:    return "Mythic";
            case Rarity.Secret:    return "Secret";
            default:               return "Common";
        }
    }

    public static string ToVietnamese(this TraitType type)
    {
        switch (type)
        {
            case TraitType.Body:    return "Body";
            case TraitType.Armor:   return "Armor";
            case TraitType.Weapon:  return "Weapon";
            case TraitType.special: return "Special";
            default:                return "Part";
        }
    }
}
