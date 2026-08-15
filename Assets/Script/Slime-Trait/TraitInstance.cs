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

    // Base stats trước khi nhân rarity multiplier
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
        
        // Gán skill cho trait theo ScriptableObject
        if (so != null && so.skill != null)
        {
            skill = new SkillInstance(so.skill);
        }
        else
        {
            skill = null;
        }

        // Chỉ gán Ultimate Skill cho Rare trở lên
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

        // Secret: slime Secret dùng chỉ số BODY-ONLY (xem Slime.CalculateStats), nên body phải
        // mang ĐỦ mọi chỉ số. Roll full-block theo chuẩn StatBalance.Secret (khớp GDD/battle) —
        // đồng thời sửa lỗi cũ khiến Secret bị ATK/Magic/Crit = 0.
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
            // Design: MỌI độ hiếm đều có ATK và Magic ATK (bỏ random 50% cho bậc thấp).
            baseAttack = RollGDDATK(Rarity);
            attack = baseAttack;
            baseMagicAttack = RollGDDMagicATK(Rarity);
            magicAttack = baseMagicAttack;
        }
        else if (TraitType == TraitType.Armor || TraitType == TraitType.special)
        {
            // Design: MỌI độ hiếm đều có CẢ Crit Rate & Crit Damage (bỏ random chỉ 1 trong 2).
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
            case Rarity.Common:    return Random.Range(1000, 2001);
            case Rarity.Uncommon:  return Random.Range(1800, 3501);
            case Rarity.Rare:      return Random.Range(3200, 6001);
            case Rarity.SuperRare: return Random.Range(5500, 10001);
            case Rarity.UltraRare: return Random.Range(9000, 16001);
            case Rarity.Legendary: return Random.Range(14000, 25001);
            case Rarity.Mythic:    return Random.Range(22000, 50001); // GDD Mythic HP: 22000 - 50000 (khớp StatBalance)
            case Rarity.Secret:    return Random.Range(9000, 16001);
            default:               return Random.Range(1000, 2001);
        }
    }

    private int RollGDDATK(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return Random.Range(100, 201);
            case Rarity.Uncommon:  return Random.Range(180, 321);
            case Rarity.Rare:      return Random.Range(320, 601);
            case Rarity.SuperRare: return Random.Range(550, 1001);
            case Rarity.UltraRare: return Random.Range(900, 1601);
            case Rarity.Legendary: return Random.Range(1400, 2501);
            case Rarity.Mythic:    return Random.Range(2200, 5001);
            case Rarity.Secret:    return Random.Range(90, 161);
            default:               return Random.Range(100, 201);
        }
    }

    private int RollGDDMagicATK(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return Random.Range(200, 401);
            case Rarity.Uncommon:  return Random.Range(320, 641);
            case Rarity.Rare:      return Random.Range(640, 1201);
            case Rarity.SuperRare: return Random.Range(1100, 2001);
            case Rarity.UltraRare: return Random.Range(1800, 3201);
            case Rarity.Legendary: return Random.Range(2800, 5001);
            case Rarity.Mythic:    return Random.Range(4400, 10001);
            case Rarity.Secret:    return Random.Range(180, 321);
            default:               return Random.Range(200, 401);
        }
    }

    private int RollGDDDEF(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return Random.Range(400, 801);
            case Rarity.Uncommon:  return Random.Range(720, 1401);
            case Rarity.Rare:      return Random.Range(1280, 2401);
            case Rarity.SuperRare: return Random.Range(2200, 4001);
            case Rarity.UltraRare: return Random.Range(3600, 6401);
            case Rarity.Legendary: return Random.Range(5600, 10001);
            case Rarity.Mythic:    return Random.Range(8800, 20001);
            case Rarity.Secret:    return Random.Range(1440, 2561);
            default:               return Random.Range(400, 801);
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
            case Rarity.Common:    return 0.05f;
            case Rarity.Uncommon:  return 0.06f;
            case Rarity.Rare:      return 0.08f;
            case Rarity.SuperRare: return 0.10f;
            case Rarity.UltraRare: return 0.13f;
            case Rarity.Legendary: return 0.16f;
            case Rarity.Mythic:    return 0.20f;
            case Rarity.Secret:    return Random.Range(0.25f, 0.35f);
            default:               return 0.05f;
        }
    }

    private float RollGDDCritDMG(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return 1.30f;
            case Rarity.Uncommon:  return 1.35f;
            case Rarity.Rare:      return 1.45f;
            case Rarity.SuperRare: return 1.55f;
            case Rarity.UltraRare: return 1.70f;
            case Rarity.Legendary: return 1.90f;
            case Rarity.Mythic:    return 2.20f;
            case Rarity.Secret:    return 2.50f;
            default:               return 1.30f;
        }
    }

    /// <param name="newMultiplier">
    /// Không còn dùng — giữ tham số để các call site cũ không phải sửa.
    /// </param>
    public void RecalculateStats(float newMultiplier = 1f)
    {
        // GDD: chỉ số ATK/DEF/SPD là GIÁ TRỊ CUỐI CÙNG — KHÔNG nhân hệ số độ hiếm nữa.
        // (Trước đây nhân rarityMultiplier lúc load khiến slime đã-lưu mạnh lệch so với slime
        //  mới-tạo/battle. Nay mọi nơi đều dùng đúng giá trị GDD/StatBalance.)
        attack = baseAttack;
        defense = baseDefense;
        speed = baseSpeed;

        // Sức mạnh kỹ năng vẫn scale theo độ hiếm như GDD.
        if (skill != null) skill.power = GetSkillPower();
    }

    /// <summary>
    /// Sức mạnh kỹ năng = thang độ hiếm × hệ số remote `battle_skill_power_mult` (mặc định 1.5).
    /// Dùng chung ở TurnSystem/Member để mọi nơi cùng một công thức.
    /// </summary>
    public float GetSkillPower()
        => GetRarityMultiplier(Rarity) * RemoteBalance.Battle.skillPowerMult;

    public TraitInstance Clone()
    {
        return new TraitInstance(this);
    }

    /// <summary>
    /// Thang độ hiếm dùng cho SỨC MẠNH KỸ NĂNG và ĐỘ KHÓ THUẦN HOÁ.
    /// KHÔNG còn nhân vào chỉ số (HP/ATK/DEF/SPD) — chỉ số nay lấy thẳng từ StatBalance,
    /// nên các key `rarity_mult_*` cũ trên Remote Config đã bị loại bỏ.
    /// </summary>
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
