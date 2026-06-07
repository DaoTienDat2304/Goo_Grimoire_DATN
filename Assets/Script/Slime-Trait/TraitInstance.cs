using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Spine.Unity;

[System.Serializable]
public class TraitInstance
{
    public List<TraitSO> allTraits;
    public TraitSO baseTrait;
    public Rarity Rarity;
    public int HP;
    public int attack;
    public int defense;
    public int speed;
    public int evade;

    // Base stats trước khi nhân rarity multiplier — dùng để recalculate khi Remote Config thay đổi
    public int baseHP;
    public int baseAttack;
    public int baseDefense;
    public int baseSpeed;
    public int baseEvade;
    public TraitType TraitType;
    public string traitname;
    public SkillInstance skill;


    public Sprite sprite => baseTrait.sprite;
    
    // Animation properties
    public SkeletonDataAsset animationAsset => baseTrait.animationAsset;
    public string animationName => baseTrait.animationName;
    public bool hasAnimation => baseTrait.animationAsset != null;

    public TraitInstance(TraitSO so)
    {
        baseTrait = so;
        traitname = so.name;
        Rarity = so.rarity;
        TraitType = so.type;
        allTraits = new List<TraitSO>();
        if (so.skill != null)
            skill = new SkillInstance(so.skill);

        float rarityMultiplier = GetRarityMultiplier(so.rarity);
        baseHP = Random.Range(so.HPRange.x, so.HPRange.y + 1);
        baseAttack = Random.Range(so.attackRange.x, so.attackRange.y + 1);
        baseDefense = Random.Range(so.defenseRange.x, so.defenseRange.y + 1);
        baseSpeed = Random.Range(so.speedRange.x, so.speedRange.y + 1);
        baseEvade = Random.Range(so.evadeRange.x, so.evadeRange.y + 1);

        HP = baseHP;
        attack = Mathf.RoundToInt(baseAttack * rarityMultiplier);
        defense = Mathf.RoundToInt(baseDefense * rarityMultiplier);
        speed = Mathf.RoundToInt(baseSpeed * rarityMultiplier);
        evade = Mathf.RoundToInt(baseEvade * rarityMultiplier);
        if (so.skill != null) skill.power = rarityMultiplier*1.5f;
        
    }

    // Copy constructor cho breeding
    public TraitInstance(TraitInstance other)
    {
        baseTrait = other.baseTrait;
        Rarity = other.Rarity;
        TraitType = other.TraitType;
        allTraits = new List<TraitSO>(other.allTraits ?? new List<TraitSO>());
        HP = other.HP;
        attack = other.attack;
        defense = other.defense;
        speed = other.speed;
        evade = other.evade;
        baseHP = other.baseHP;
        baseAttack = other.baseAttack;
        baseDefense = other.baseDefense;
        baseSpeed = other.baseSpeed;
        baseEvade = other.baseEvade;
    }

    // Recalculate stats từ base values với multiplier mới (dùng khi Remote Config thay đổi)
    public void RecalculateStats(float newMultiplier)
    {
        attack = Mathf.RoundToInt(baseAttack * newMultiplier);
        defense = Mathf.RoundToInt(baseDefense * newMultiplier);
        speed = Mathf.RoundToInt(baseSpeed * newMultiplier);
        evade = Mathf.RoundToInt(baseEvade * newMultiplier);
        if (skill != null) skill.power = newMultiplier * 1.5f;
    }

    public TraitInstance Clone()
    {
        return new TraitInstance(this);
    }

    public float GetRarityMultiplier(Rarity rarity)
    {
        // Nếu Remote Config đã sẵn sàng, dùng giá trị từ server
        if (RemoteConfigManager.Instance != null && RemoteConfigManager.Instance.IsReady)
            return RemoteConfigManager.Instance.GetRarityMultiplier(rarity);

        // Fallback hardcode khi chưa có Remote Config
        switch (rarity)
        {
            case Rarity.Common:    return 1f;
            case Rarity.Uncommon:  return 1.2f;
            case Rarity.Rare:      return 1.4f;
            case Rarity.SuperRare: return 1.6f;
            case Rarity.UltraRare: return 1.8f;
            case Rarity.Legendary: return 2f;
            case Rarity.Mythic:    return 2.25f;
            case Rarity.Secret:    return 2f;
            default:               return 1f;
        }
    }
    
    public TraitInstance mutanttraits(TraitType type, Rarity rarity)
    {
        var newTrait = RollTrait(type, rarity);
        if (newTrait != null)
        {
            baseTrait = newTrait;
            Rarity = newTrait.rarity;
            TraitType = newTrait.type;
            
            // Recalculate stats with new rarity multiplier
            float rarityMultiplier = GetRarityMultiplier(newTrait.rarity);
            baseHP = Random.Range(newTrait.HPRange.x, newTrait.HPRange.y + 1);
            baseAttack = Random.Range(newTrait.attackRange.x, newTrait.attackRange.y + 1);
            baseDefense = Random.Range(newTrait.defenseRange.x, newTrait.defenseRange.y + 1);
            baseSpeed = Random.Range(newTrait.speedRange.x, newTrait.speedRange.y + 1);
            baseEvade = Random.Range(newTrait.evadeRange.x, newTrait.evadeRange.y + 1);

            HP = baseHP;
            attack = Mathf.RoundToInt(baseAttack * rarityMultiplier);
            defense = Mathf.RoundToInt(baseDefense * rarityMultiplier);
            speed = Mathf.RoundToInt(baseSpeed * rarityMultiplier);
            evade = Mathf.RoundToInt(baseEvade * rarityMultiplier);
        }
        return this;
    }
    
    private TraitSO RollTrait(TraitType type, Rarity rarity)
    {
        // Get traits from SlimeGen if allTraits is empty
        if (allTraits == null || allTraits.Count == 0)
        {
            if (SlimeGen.Instance != null && SlimeGen.Instance.allTraits != null)
            {
                allTraits = new List<TraitSO>(SlimeGen.Instance.allTraits);
            }
            else
            {

                return null;
            }
        }

        var pool = allTraits.Where(t => t != null && t.type == type && t.rarity == rarity && t.dropRate > 0f).ToList();
        if (pool.Count == 0)
        {

            return null;
        }

        float totalRate = pool.Sum(t => t.dropRate);
        if (totalRate <= 0f)
        {

            return null;
        }

        float roll = Random.Range(0f, totalRate);
        float cumulative = 0f;

        foreach (var t in pool)
        {
            cumulative += t.dropRate;
            if (roll <= cumulative) return t;
        }

        return pool[0];
    }
}
