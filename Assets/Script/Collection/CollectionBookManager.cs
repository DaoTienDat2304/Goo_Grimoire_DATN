using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// </summary>
public class CollectionBookManager : MonoBehaviour
{
    public static CollectionBookManager Instance { get; private set; }

    [Header("Source Database")]
    public List<TraitSO> allTraitsDatabase = new List<TraitSO>();
    public List<SkillSO> allSkillsDatabase = new List<SkillSO>();

    private HashSet<string> _unlockedTraitNames = new HashSet<string>();
    private HashSet<string> _unlockedSkillNames = new HashSet<string>();

    private List<Slime> _ownedSlimes = new List<Slime>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        RefreshFromSave();
    }

    /// <summary>
    /// </summary>
    public void RefreshFromSave()
    {
        _unlockedTraitNames.Clear();
        _unlockedSkillNames.Clear();
        _ownedSlimes.Clear();

        if (SlimeGen.Instance != null)
        {
            if (allTraitsDatabase == null || allTraitsDatabase.Count == 0)
                allTraitsDatabase = new List<TraitSO>(SlimeGen.Instance.allTraits);
            if (allSkillsDatabase == null || allSkillsDatabase.Count == 0)
                allSkillsDatabase = new List<SkillSO>(SlimeGen.Instance.allSkillsDatabase ?? new List<SkillSO>());
        }

        if (allTraitsDatabase == null || allTraitsDatabase.Count == 0)
            allTraitsDatabase = new List<TraitSO>(Resources.LoadAll<TraitSO>(string.Empty));

        if (allSkillsDatabase == null || allSkillsDatabase.Count == 0)
            allSkillsDatabase = new List<SkillSO>(Resources.LoadAll<SkillSO>("SkillDB"));

        if (BreedingManager.Instance != null)
        {
            var allSlimes = BreedingManager.Instance.GetAllSlimes();
            if (allSlimes != null) _ownedSlimes.AddRange(allSlimes);
        }

        foreach (var s in _ownedSlimes)
        {
            RegisterTraitUnlock(s.body);
            RegisterTraitUnlock(s.armor);
            RegisterTraitUnlock(s.weapon);
        }

        if (PlayerStatsManager.Instance != null)
        {
            var ledger = PlayerStatsManager.Instance.GetTraitLedger();
            if (ledger != null)
            {
                foreach (var tName in ledger)
                    _unlockedTraitNames.Add(tName);
            }
        }


        foreach (var traitName in _unlockedTraitNames)
        {
            var traitSO = allTraitsDatabase.FirstOrDefault(t => t != null && t.traitName == traitName);
            if (traitSO == null) continue;
            if (traitSO.skill != null) _unlockedSkillNames.Add(traitSO.skill.name);
            if (traitSO.ultimateSkill != null) _unlockedSkillNames.Add(traitSO.ultimateSkill.name);
        }

    }

    private void RegisterTraitUnlock(TraitInstance ti)
    {
        if (ti?.baseTrait == null) return;
        _unlockedTraitNames.Add(ti.baseTrait.traitName);
        if (ti.skill?.baseSkill != null) _unlockedSkillNames.Add(ti.skill.baseSkill.name);
        if (ti.ultimateSkill?.baseSkill != null) _unlockedSkillNames.Add(ti.ultimateSkill.baseSkill.name);
    }


    // ─────────────────────────────────────────
    // Public Query Methods
    // ─────────────────────────────────────────

    public bool IsTraitUnlocked(TraitSO trait) =>
        trait != null && _unlockedTraitNames.Contains(trait.traitName);

    public bool IsSkillUnlocked(SkillSO skill) =>
        skill != null && _unlockedSkillNames.Contains(skill.name);

    public List<Slime> GetOwnedSlimes() => _ownedSlimes;

    public List<TraitSO> GetAllBodyTraits() =>
        allTraitsDatabase.Where(t => t != null && t.type == TraitType.Body).ToList();

    public List<TraitSO> GetAllArmorTraits() =>
        allTraitsDatabase.Where(t => t != null && t.type == TraitType.Armor).ToList();

    public List<TraitSO> GetAllWeaponTraits() =>
        allTraitsDatabase.Where(t => t != null && t.type == TraitType.Weapon).ToList();

    public List<SkillSO> GetAllSkills() =>
        allSkillsDatabase.Where(s => s != null).ToList();

    /// <summary>
    /// </summary>
    public Slime GetBestSlimeForBodyTrait(TraitSO bodyTrait)
    {
        if (bodyTrait == null) return null;
        return _ownedSlimes
            .Where(s => s?.body?.baseTrait == bodyTrait)
            .OrderByDescending(s => s.totalHP + s.totalAttack + s.totalDefense)
            .FirstOrDefault();
    }
}
