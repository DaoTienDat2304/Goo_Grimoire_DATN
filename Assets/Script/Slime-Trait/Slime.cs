using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Slime
{
    public string slimeName;
    public int generation; // Thế hệ của slime
    public float breedingCooldown; // Thời gian chờ để có thể lai tạo
    public bool canBreed; // Có thể lai tạo hay không
    public bool breedingLocked; // Đang trong một phiên lai tạo (mục 3) → bị khóa
    public List<string> parents; // Danh sách tên bố mẹ
    public float happiness; // Chỉ số hạnh phúc (ảnh hưởng đến breeding)
    public int experience; // Kinh nghiệm của slime
    public bool isPicked = false;
    public int id;

    public TraitInstance body;
    public TraitInstance armor;
    public TraitInstance weapon;
    public int totalHP;
    public int totalAttack;
    public int totalMagicAttack;
    public int totalDefense;
    public int totalSpeed;
    public float totalCritRate;
    public float totalCritDMG;
    // Chất lượng roll khi slime được sinh ra từ trứng (mục 1) hoặc lai tạo (mục 3).
    // Chỉ là metadata hiển thị — stat thật nằm trong các trait/total ở trên.
    public float eggStatRollPercent;
    public string eggStatQuality;
    public List<SkillInstance> Skills { get; private set; } = new List<SkillInstance>();

    private void AssignSkills()
    {
        Skills.Clear();
        if (body?.skill != null) Skills.Add(body.skill);
        if (armor?.skill != null) Skills.Add(armor.skill);
        if (weapon?.skill != null) Skills.Add(weapon.skill);
    }

    // Constructor mới
    public Slime()
    {
        generation = 0;
        breedingCooldown = 0f;
        canBreed = true;
        parents = new List<string>();
        happiness = 100f;
        experience = 0;
    }

    // Constructor cho slime được lai tạo
    public Slime(Slime parent1, Slime parent2)
    {
        generation = Mathf.Max(parent1.generation, parent2.generation) + 1;
        breedingCooldown = 0f; // Bỏ qua cooldown - có thể lai tạo ngay lập tức
        canBreed = true; // Có thể lai tạo ngay lập tức
        parents = new List<string> { parent1.slimeName, parent2.slimeName };
        happiness = 100f;
        experience = 0;

        // Kế thừa traits từ bố mẹ với xác suất
        body = InheritTrait(parent1.body, parent2.body);
        armor = InheritTrait(parent1.armor, parent2.armor);
        weapon = InheritTrait(parent1.weapon, parent2.weapon);

        // Fallback if inheritance failed
        if (body == null)
        {

            body = SlimeGen.Instance != null ?
                new TraitInstance(SlimeGen.Instance.RollTrait(TraitType.Body)) :
                new TraitInstance(CreateDefaultTrait(TraitType.Body));
        }

        if (armor == null)
        {

            armor = SlimeGen.Instance != null ?
                new TraitInstance(SlimeGen.Instance.RollTrait(TraitType.Armor)) :
                new TraitInstance(CreateDefaultTrait(TraitType.Armor));
        }

        if (weapon == null)
        {

            weapon = SlimeGen.Instance != null ?
                new TraitInstance(SlimeGen.Instance.RollTrait(TraitType.Weapon)) :
                new TraitInstance(CreateDefaultTrait(TraitType.Weapon));
        }

        CalculateStats();
        RollRandomSkillsMatchingRarity();
    }
    private Rarity GetNextRarity(Rarity current)
    {
        switch (current)
        {
            case Rarity.Common: return Rarity.Uncommon;
            case Rarity.Uncommon: return Rarity.Rare;
            case Rarity.Rare: return Rarity.SuperRare;
            case Rarity.SuperRare: return Rarity.UltraRare;
            case Rarity.UltraRare: return Rarity.Legendary;
            case Rarity.Legendary: return Rarity.Mythic;
            case Rarity.Mythic: return Rarity.Mythic; // Max rarity, không nâng nữa
            default: return current;
        }
    }
    private float GetStableStat(Rarity current)
    {
        switch (current)
        {
            case Rarity.Common: return 0f;
            case Rarity.Uncommon: return 2f;
            case Rarity.Rare: return 3.5f;
            case Rarity.SuperRare: return 4.5f;
            case Rarity.UltraRare: return 5f;
            case Rarity.Legendary: return 5.5f;
            case Rarity.Mythic: return 6f;
            default: return 6f;
        }
    }


    private TraitInstance InheritTrait(TraitInstance trait1, TraitInstance trait2)
    {
        // Check for null traits
        if (trait1 == null && trait2 == null)
        {

            return null;
        }

        if (trait1 == null) return trait2.Clone();
        if (trait2 == null) return trait1.Clone();

        // 90% chance for normal inheritance, 10% chance for mutation
        if (Random.Range(1, 10) > 6)
        {
            // Normal inheritance - 50% from each parent
            if (Random.Range(0f, 1f) < 0.5f)
            {
                return trait1.Clone();
            }
            else
            {
                return trait2.Clone();
            }
        }
        else
        {

            // Mutation chance - upgrade rarity if possible
            if (Random.Range(1, 10) > 4 + ((GetStableStat(trait1.Rarity) > GetStableStat(trait2.Rarity)) ? GetStableStat(trait1.Rarity) : GetStableStat(trait2.Rarity)))
            {
                TraitInstance selectedTrait = (Random.Range(0f, 1f) < 0.5f) ? trait1 : trait2;
                var mutatedTrait = selectedTrait.Clone();
                var nextRarity = GetNextRarity(selectedTrait.Rarity);
                mutatedTrait.mutanttraits(selectedTrait.TraitType, nextRarity);

                if (mutatedTrait.baseTrait != null)
                {

                    return mutatedTrait;
                }
                else
                {

                    return selectedTrait.Clone(); // Fallback về trait gốc
                }
            }
            else
            {
                TraitInstance selectedTrait = (Random.Range(0f, 1f) < 0.5f) ? trait1 : trait2;
                var mutatedTrait = selectedTrait.Clone();
                mutatedTrait.mutanttraits(selectedTrait.TraitType, mutatedTrait.Rarity);
                return mutatedTrait;
            }
        }
    }

    // Fallback method to create default traits if SlimeGen is not available
    private TraitSO CreateDefaultTrait(TraitType type)
    {
        var trait = ScriptableObject.CreateInstance<TraitSO>();
        trait.traitName = $"Default{type}";
        trait.type = type;
        trait.rarity = Rarity.Common;
        trait.dropRate = 100f;
        trait.HPRange = new Vector2Int(1000, 2000);
        trait.attackRange = new Vector2Int(100, 200);
        trait.magicAttackRange = new Vector2Int(200, 400);
        trait.defenseRange = new Vector2Int(400, 800);
        trait.speedRange = new Vector2Int(80, 100);
        trait.critRateRange = new Vector2Int(5, 5);
        trait.critDMGRange = new Vector2Int(130, 130);
        return trait;
    }

    public void CalculateStats()
    {
        if (body != null && body.Rarity == Rarity.Secret)
        {
            totalHP = body.HP;
            totalAttack = body.attack;
            totalMagicAttack = body.magicAttack;
            totalDefense = body.defense;
            totalSpeed = body.speed;
            totalCritRate = body.critRate;
            totalCritDMG = body.critDMG;
            AssignSkills();
            return;
        }

        totalHP = (body != null ? body.HP : 0) + (armor != null ? armor.HP : 0) + (weapon != null ? weapon.HP : 0);
        totalAttack = (body != null ? body.attack : 0) + (armor != null ? armor.attack : 0) + (weapon != null ? weapon.attack : 0);
        totalMagicAttack = (body != null ? body.magicAttack : 0) + (armor != null ? armor.magicAttack : 0) + (weapon != null ? weapon.magicAttack : 0);
        totalDefense = (body != null ? body.defense : 0) + (armor != null ? armor.defense : 0) + (weapon != null ? weapon.defense : 0);
        totalSpeed = (body != null ? body.speed : 0) + (armor != null ? armor.speed : 0) + (weapon != null ? weapon.speed : 0);

        // Crit Rate & Crit DMG comes from Head (armor/special) part
        totalCritRate = (armor != null ? armor.critRate : 0.05f);
        totalCritDMG = (armor != null ? armor.critDMG : 1.30f);

        AssignSkills();
    }

    public void RollRandomSkillsMatchingRarity()
    {
        if (SlimeGen.Instance == null) return;

        if (body != null)
        {
            var newSkillSO = SlimeGen.Instance.GetRandomSkill(TraitType.Body, body.Rarity);
            if (newSkillSO != null) body.skill = new SkillInstance(newSkillSO);
        }

        if (armor != null)
        {
            var newSkillSO = SlimeGen.Instance.GetRandomSkill(TraitType.Armor, armor.Rarity);
            if (newSkillSO != null) armor.skill = new SkillInstance(newSkillSO);
        }

        if (weapon != null)
        {
            var newSkillSO = SlimeGen.Instance.GetRandomSkill(TraitType.Weapon, weapon.Rarity);
            if (newSkillSO != null) weapon.skill = new SkillInstance(newSkillSO);
        }

        AssignSkills();
    }

    public void UpdateBreedingCooldown(float deltaTime)
    {
        if (breedingLocked) return; // Đang lai tạo: giữ khóa, không đếm ngược cooldown.
        if (!canBreed)
        {
            breedingCooldown -= deltaTime;
            if (breedingCooldown <= 0f)
            {
                canBreed = true;
                breedingCooldown = 0f;
            }
        }
    }

    public bool CanBreedWith(Slime other)
    {
        return canBreed && !breedingLocked && other != null && other.canBreed && !other.breedingLocked;
    }
}
