using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SlimeGen : MonoBehaviour
{
    public List<TraitSO> allTraits;

    public static SlimeGen Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            EnsureDefaultTraits();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void EnsureDefaultTraits()
    {
        if (allTraits == null) allTraits = new List<TraitSO>();
        bool hasBody = allTraits.Any(t => t != null && t.type == TraitType.Body && t.dropRate > 0f);
        bool hasArmor = allTraits.Any(t => t != null && t.type == TraitType.Armor && t.dropRate > 0f);
        bool hasWeapon = allTraits.Any(t => t != null && t.type == TraitType.Weapon && t.dropRate > 0f);

        if (hasBody && hasArmor && hasWeapon) return;


        // Try load all TraitSO from Resources (user can place ScriptableObjects under Assets/Resources)
        var loaded = Resources.LoadAll<TraitSO>(string.Empty);
        if (loaded != null && loaded.Length > 0)
        {
            foreach (var so in loaded)
            {
                if (!allTraits.Contains(so)) allTraits.Add(so);
            }
        }

        hasBody = allTraits.Any(t => t != null && t.type == TraitType.Body && t.dropRate > 0f);
        hasArmor = allTraits.Any(t => t != null && t.type == TraitType.Armor && t.dropRate > 0f);
        hasWeapon = allTraits.Any(t => t != null && t.type == TraitType.Weapon && t.dropRate > 0f);

        if (!hasBody || !hasArmor || !hasWeapon)
        {

        }
    }

    public TraitSO RollTrait(TraitType type)
    {
        if (allTraits == null)
        {

            return null;
        }

        var pool = allTraits.Where(t => t != null && t.type == type && t.dropRate > 0f).ToList();
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

    public Slime GenerateSlime(string name)
    {
        var bodySo = RollTrait(TraitType.Body);
        var armorSo = RollTrait(TraitType.Armor);
        var weaponSo = RollTrait(TraitType.Weapon);

        if (bodySo == null || armorSo == null || weaponSo == null)
        {

            return null;
        }

        Slime s = new Slime();
        s.slimeName = name;
        s.body = bodySo.GenerateInstance();
        s.armor = armorSo.GenerateInstance();
        s.weapon = weaponSo.GenerateInstance();
        s.CalculateStats();
        s.RollRandomSkillsMatchingRarity();
        s.AssignCompactName();
        return s;
    }

    public Slime GenerateSpecial(string name)
    {
        var bodySo = RollTrait(TraitType.special);
        Slime s = new Slime();
        s.slimeName = name;
        s.body = bodySo.GenerateInstance();
        s.CalculateStats();
        s.AssignCompactName();
        return s;
    }
    public TraitSO RollTraitOfRarity(TraitType type, Rarity rarity)
    {
        if (allTraits == null) return null;
        var pool = allTraits.Where(t => t != null && t.type == type && t.rarity == rarity).ToList();
        if (pool.Count == 0)
        {
            pool = allTraits.Where(t => t != null && t.type == type).ToList();
        }
        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    public Slime GenerateSlimeOfRarity(string name, Rarity rarity)
    {
        EnsureDefaultTraits();
        var bodySo = RollTraitOfRarity(TraitType.Body, rarity);
        var armorSo = RollTraitOfRarity(TraitType.Armor, rarity);
        var weaponSo = RollTraitOfRarity(TraitType.Weapon, rarity);

        if (bodySo == null || armorSo == null || weaponSo == null) return null;

        Slime s = new Slime();
        s.slimeName = name;
        s.body = bodySo.GenerateInstance();
        s.body.Rarity = rarity;
        s.armor = armorSo.GenerateInstance();
        s.armor.Rarity = rarity;
        s.weapon = weaponSo.GenerateInstance();
        s.weapon.Rarity = rarity;
        s.CalculateStats();
        s.RollRandomSkillsMatchingRarity();
        s.AssignCompactName();
        return s;
    }

    [Header("Skill Database")]
    public List<SkillSO> allSkillsDatabase;

    private void EnsureSkillDatabase()
    {
        if (allSkillsDatabase == null || allSkillsDatabase.Count == 0)
        {
            allSkillsDatabase = new List<SkillSO>(Resources.LoadAll<SkillSO>("SkillDB"));
            if (allSkillsDatabase.Count == 0)
            {
                allSkillsDatabase = new List<SkillSO>(Resources.LoadAll<SkillSO>(""));
            }
        }
    }

    public void EnsureSkillDatabasePublic() => EnsureSkillDatabase();

    public SkillSO GetRandomSkill(TraitType type, Rarity rarity)
    {
        EnsureSkillDatabase();
        if (allSkillsDatabase == null) return null;

        var pool = allSkillsDatabase.Where(s => s != null && s.targetTrait == type && s.rarity == rarity && s.type != SkillType.Ultimate && !s.name.EndsWith("_U", System.StringComparison.OrdinalIgnoreCase)).ToList();

        if (pool.Count > 0)
        {
            return pool[Random.Range(0, pool.Count)];
        }
        else
        {
            var fallbackPool = allSkillsDatabase.Where(s => s != null && s.targetTrait == type && s.type != SkillType.Ultimate && !s.name.EndsWith("_U", System.StringComparison.OrdinalIgnoreCase)).ToList();
            if (fallbackPool.Count > 0)
            {
                return fallbackPool[Random.Range(0, fallbackPool.Count)];
            }
        }

        return null;
    }

    public SkillSO GetRandomWeaponSkill(Rarity rarity, bool isUltimate)
    {
        EnsureSkillDatabase();
        if (allSkillsDatabase == null) return null;

        var pool = allSkillsDatabase.Where(s => s != null && s.targetTrait == TraitType.Weapon 
            && s.rarity == rarity 
            && (isUltimate ? (s.type == SkillType.Ultimate || s.name.EndsWith("_U", System.StringComparison.OrdinalIgnoreCase)) 
                           : (s.type == SkillType.Active || s.name.EndsWith("_A", System.StringComparison.OrdinalIgnoreCase)))).ToList();

        if (pool.Count > 0)
        {
            return pool[Random.Range(0, pool.Count)];
        }
        else
        {
            var fallbackPool = allSkillsDatabase.Where(s => s != null && s.targetTrait == TraitType.Weapon 
                && (isUltimate ? (s.type == SkillType.Ultimate || s.name.EndsWith("_U", System.StringComparison.OrdinalIgnoreCase)) 
                               : (s.type == SkillType.Active || s.name.EndsWith("_A", System.StringComparison.OrdinalIgnoreCase)))).ToList();
            if (fallbackPool.Count > 0)
            {
                return fallbackPool[Random.Range(0, fallbackPool.Count)];
            }
        }

        return null;
    }

    public SkillSO GetMatchingUltimateWeaponSkill(SkillSO activeSkill)
    {
        if (activeSkill == null) return null;
        EnsureSkillDatabase();
        if (allSkillsDatabase == null || allSkillsDatabase.Count == 0) return null;

        string activeName = activeSkill.name.Trim();

        string targetUltName = activeName.EndsWith("_A", System.StringComparison.OrdinalIgnoreCase)
            ? activeName.Substring(0, activeName.Length - 2) + "_U"
            : activeName + "_U";

        var ultSO = allSkillsDatabase.FirstOrDefault(s => s != null && s.name.Equals(targetUltName, System.StringComparison.OrdinalIgnoreCase));
        if (ultSO != null) return ultSO;

        string basePrefix = activeName.Replace("_A", "").Trim();
        ultSO = allSkillsDatabase.FirstOrDefault(s => s != null && s.name.StartsWith(basePrefix, System.StringComparison.OrdinalIgnoreCase) && s.name.EndsWith("_U", System.StringComparison.OrdinalIgnoreCase));
        if (ultSO != null) return ultSO;

        ultSO = allSkillsDatabase.FirstOrDefault(s => s != null && s.targetTrait == TraitType.Weapon && s.rarity == activeSkill.rarity && (s.type == SkillType.Ultimate || s.name.EndsWith("_U", System.StringComparison.OrdinalIgnoreCase)));
        if (ultSO != null) return ultSO;

        return GetRandomWeaponSkill(activeSkill.rarity, true);
    }
}
