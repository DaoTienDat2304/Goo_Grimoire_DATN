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
    public int magicAttack;
    public int defense;
    public int speed;
    public float critRate;
    public float critDMG;

    public int baseHP;
    public int baseAttack;
    public int baseMagicAttack;
    public int baseDefense;
    public int baseSpeed;
    public float baseCritRate;
    public float baseCritDMG;

    public TraitType TraitType;
    public string traitname;
    public SkillInstance skill;
    public SkillInstance ultimateSkill;


    public Sprite sprite => baseTrait != null ? baseTrait.sprite : null;
    
    // Animation properties
    public SkeletonDataAsset animationAsset => baseTrait != null ? baseTrait.animationAsset : null;
    public string animationName => baseTrait != null ? baseTrait.animationName : "animation";
    public bool hasAnimation => baseTrait != null && baseTrait.animationAsset != null;

    public TraitInstance(TraitSO so)
    {
        baseTrait = so;
        traitname = so != null ? so.name : "Unknown";
        Rarity = so != null ? so.rarity : Rarity.Common;
        TraitType = so != null ? so.type : TraitType.Body;
        allTraits = new List<TraitSO>();
        
        if (so != null && so.skill != null)
        {
            skill = new SkillInstance(so.skill);
        }
        else
        {
            skill = null;
        }

        if (so != null && so.ultimateSkill != null && Rarity != Rarity.Common && Rarity != Rarity.Uncommon)
        {
            ultimateSkill = new SkillInstance(so.ultimateSkill);
        }
        else
        {
            ultimateSkill = null;
        }

        RollStatsByGDD();
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
        magicAttack = other.magicAttack;
        defense = other.defense;
        speed = other.speed;
        critRate = other.critRate;
        critDMG = other.critDMG;

        baseHP = other.baseHP;
        baseAttack = other.baseAttack;
        baseMagicAttack = other.baseMagicAttack;
        baseDefense = other.baseDefense;
        baseSpeed = other.baseSpeed;
        baseCritRate = other.baseCritRate;
        baseCritDMG = other.baseCritDMG;

        if (other.skill != null)
        {
            skill = new SkillInstance(other.skill.baseSkill);
            skill.power = other.skill.power;
        }
        if (other.ultimateSkill != null)
        {
            ultimateSkill = new SkillInstance(other.ultimateSkill.baseSkill);
            ultimateSkill.power = other.ultimateSkill.power;
        }
    }

    private void RollStatsByGDD()
    {
        HP = 0;
        attack = 0;
        magicAttack = 0;
        defense = 0;
        speed = 0;
        critRate = 0f;
        critDMG = 0f;

        baseHP = 0;
        baseAttack = 0;
        baseMagicAttack = 0;
        baseDefense = 0;
        baseSpeed = 0;
        baseCritRate = 0f;
        baseCritDMG = 0f;

        if (Rarity == Rarity.Secret && TraitType == TraitType.Body)
        {
            var rs = StatBalance.Get(Rarity.Secret);
            baseHP = HP = Random.Range(rs.hpMin, rs.hpMax + 1);
            baseAttack = attack = Random.Range(rs.atkMin, rs.atkMax + 1);
            baseMagicAttack = magicAttack = Random.Range(rs.magMin, rs.magMax + 1);
            baseDefense = defense = Random.Range(rs.defMin, rs.defMax + 1);
            baseSpeed = speed = Random.Range(rs.spdMin, rs.spdMax + 1);
            baseCritRate = critRate = rs.critRate;
            baseCritDMG = critDMG = rs.critDmg;
            return;
        }

        if (TraitType == TraitType.Body)
        {
            baseHP = RollGDDHP(Rarity);
            baseDefense = RollGDDDEF(Rarity);
            baseSpeed = RollGDDSpeed(Rarity);

            HP = baseHP;
            defense = baseDefense;
            speed = baseSpeed;
        }
        else if (TraitType == TraitType.Weapon)
        {
            baseAttack = RollGDDATK(Rarity);
            attack = baseAttack;
            baseMagicAttack = RollGDDMagicATK(Rarity);
            magicAttack = baseMagicAttack;
        }
        else if (TraitType == TraitType.Armor || TraitType == TraitType.special)
        {
            baseCritRate = RollGDDCritRate(Rarity);
            baseCritDMG = RollGDDCritDMG(Rarity);
            critRate = baseCritRate;
            critDMG = baseCritDMG;
        }
    }

    private int RollGDDHP(Rarity rarity)
    {
        switch (rarity)
        {

            case Rarity.Common:    return Random.Range(1500, 2001);
            case Rarity.Uncommon:  return Random.Range(2000, 2701);
            case Rarity.Rare:      return Random.Range(2700, 3701);
            case Rarity.SuperRare: return Random.Range(3700, 5001);
            case Rarity.UltraRare: return Random.Range(5000, 6501);
            case Rarity.Legendary: return Random.Range(6500, 8301);
            case Rarity.Mythic:    return Random.Range(8300, 10001);
            case Rarity.Secret:    return Random.Range(5000, 6501);
            default:               return Random.Range(1500, 2001);
        }
    }

    private int RollGDDATK(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return Random.Range(350, 501);
            case Rarity.Uncommon:  return Random.Range(450, 651);
            case Rarity.Rare:      return Random.Range(600, 851);
            case Rarity.SuperRare: return Random.Range(800, 1101);
            case Rarity.UltraRare: return Random.Range(1000, 1401);
            case Rarity.Legendary: return Random.Range(1300, 1801);
            case Rarity.Mythic:    return Random.Range(1700, 2301);
            case Rarity.Secret:    return Random.Range(1000, 1401);
            default:               return Random.Range(350, 501);
        }
    }

    private int RollGDDMagicATK(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return Random.Range(450, 651);
            case Rarity.Uncommon:  return Random.Range(600, 851);
            case Rarity.Rare:      return Random.Range(800, 1101);
            case Rarity.SuperRare: return Random.Range(1050, 1451);
            case Rarity.UltraRare: return Random.Range(1350, 1851);
            case Rarity.Legendary: return Random.Range(1700, 2401);
            case Rarity.Mythic:    return Random.Range(2200, 3001);
            case Rarity.Secret:    return Random.Range(1350, 1851);
            default:               return Random.Range(450, 651);
        }
    }

    private int RollGDDDEF(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return Random.Range(500, 801);
            case Rarity.Uncommon:  return Random.Range(700, 1001);
            case Rarity.Rare:      return Random.Range(900, 1301);
            case Rarity.SuperRare: return Random.Range(1200, 1701);
            case Rarity.UltraRare: return Random.Range(1600, 2301);
            case Rarity.Legendary: return Random.Range(2100, 3001);
            case Rarity.Mythic:    return Random.Range(2700, 3801);
            case Rarity.Secret:    return Random.Range(1600, 2301);
            default:               return Random.Range(500, 801);
        }
    }

    private int RollGDDSpeed(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return Random.Range(80, 101);
            case Rarity.Uncommon:  return Random.Range(90, 111);
            case Rarity.Rare:      return Random.Range(100, 121);
            case Rarity.SuperRare: return Random.Range(110, 136);
            case Rarity.UltraRare: return Random.Range(120, 151);
            case Rarity.Legendary: return Random.Range(135, 166);
            case Rarity.Mythic:    return Random.Range(150, 181);
            case Rarity.Secret:    return Random.Range(120, 151);
            default:               return Random.Range(80, 101);
        }
    }

    private float RollGDDCritRate(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return Random.Range(0.15f, 0.20f);
            case Rarity.Uncommon:  return Random.Range(0.20f, 0.28f);
            case Rarity.Rare:      return Random.Range(0.28f, 0.36f);
            case Rarity.SuperRare: return Random.Range(0.36f, 0.45f);
            case Rarity.UltraRare: return Random.Range(0.45f, 0.55f);
            case Rarity.Legendary: return Random.Range(0.55f, 0.63f);
            case Rarity.Mythic:    return Random.Range(0.63f, 0.70f);
            case Rarity.Secret:    return Random.Range(0.65f, 0.75f);
            default:               return Random.Range(0.15f, 0.20f);
        }
    }

    private float RollGDDCritDMG(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return Random.Range(1.20f, 1.30f);
            case Rarity.Uncommon:  return Random.Range(1.30f, 1.40f);
            case Rarity.Rare:      return Random.Range(1.40f, 1.55f);
            case Rarity.SuperRare: return Random.Range(1.55f, 1.70f);
            case Rarity.UltraRare: return Random.Range(1.70f, 1.90f);
            case Rarity.Legendary: return Random.Range(1.90f, 2.10f);
            case Rarity.Mythic:    return Random.Range(2.10f, 2.30f);
            case Rarity.Secret:    return Random.Range(2.40f, 2.60f);
            default:               return Random.Range(1.20f, 1.30f);
        }
    }

    /// <param name="newMultiplier">
    /// </param>
    public void RecalculateStats(float newMultiplier = 1f)
    {
        attack = baseAttack;
        defense = baseDefense;
        speed = baseSpeed;

        if (skill != null) skill.power = GetSkillPower();
    }
    public float GetSkillPower()
        => GetRarityMultiplier(Rarity) * RemoteBalance.Battle.skillPowerMult;

    public TraitInstance Clone()
    {
        return new TraitInstance(this);
    }
    public float GetRarityMultiplier(Rarity rarity)
    {
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
            
            if (newTrait.skill != null && Rarity != Rarity.Common && Rarity != Rarity.Uncommon)
            {
                skill = new SkillInstance(newTrait.skill);
            }
            else
            {
                skill = null;
            }

            RollStatsByGDD();
        }
        return this;
    }
    
    private TraitSO RollTrait(TraitType type, Rarity rarity)
    {
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
