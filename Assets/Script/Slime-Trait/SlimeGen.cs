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
        return s;
    }

    public Slime GenerateSpecial(string name)
    {
        var bodySo = RollTrait(TraitType.special);
        Slime s = new Slime();
        s.slimeName = name;
        s.body = bodySo.GenerateInstance();
        s.CalculateStats();
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
        s.body.Rarity = rarity; // Đảm bảo độ hiếm chính xác
        s.armor = armorSo.GenerateInstance();
        s.armor.Rarity = rarity;
        s.weapon = weaponSo.GenerateInstance();
        s.weapon.Rarity = rarity;
        s.CalculateStats();
        return s;
    }
}

