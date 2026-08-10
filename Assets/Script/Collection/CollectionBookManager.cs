using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Quản lý trạng thái mở khóa của Collection Book.
/// Đóng vai trò "Registry" — tra cứu xem Trait/Skill nào đã từng được người chơi sở hữu.
/// </summary>
public class CollectionBookManager : MonoBehaviour
{
    public static CollectionBookManager Instance { get; private set; }

    // ── Dữ liệu nguồn (gắn trong Inspector hoặc load từ SlimeGen) ──
    [Header("Source Database — gắn vào Inspector hoặc để trống (sẽ tự load từ SlimeGen)")]
    public List<TraitSO> allTraitsDatabase = new List<TraitSO>();
    public List<SkillSO> allSkillsDatabase = new List<SkillSO>();

    // ── Cache trạng thái mở khóa ──
    private HashSet<string> _unlockedTraitNames = new HashSet<string>();
    private HashSet<string> _unlockedSkillNames = new HashSet<string>();

    // ── Dữ liệu Slime đang sở hữu từ Save ──
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
    /// Load lại toàn bộ dữ liệu từ SaveAndLoadSystem + SlimeGen.
    /// Gọi khi mở Collection Book.
    /// </summary>
    public void RefreshFromSave()
    {
        _unlockedTraitNames.Clear();
        _unlockedSkillNames.Clear();
        _ownedSlimes.Clear();

        // ── 1. Lấy database từ SlimeGen nếu chưa gắn ──
        if (SlimeGen.Instance != null)
        {
            if (allTraitsDatabase == null || allTraitsDatabase.Count == 0)
                allTraitsDatabase = new List<TraitSO>(SlimeGen.Instance.allTraits);
            if (allSkillsDatabase == null || allSkillsDatabase.Count == 0)
                allSkillsDatabase = new List<SkillSO>(SlimeGen.Instance.allSkillsDatabase ?? new List<SkillSO>());
        }

        // Fallback: load từ Resources nếu vẫn rỗng
        if (allTraitsDatabase == null || allTraitsDatabase.Count == 0)
            allTraitsDatabase = new List<TraitSO>(Resources.LoadAll<TraitSO>(string.Empty));

        if (allSkillsDatabase == null || allSkillsDatabase.Count == 0)
            allSkillsDatabase = new List<SkillSO>(Resources.LoadAll<SkillSO>("SkillDB"));

        // ── 2. Lấy Slime đang sở hữu từ BreedingManager ──
        if (BreedingManager.Instance != null)
        {
            var allSlimes = BreedingManager.Instance.GetAllSlimes();
            if (allSlimes != null) _ownedSlimes.AddRange(allSlimes);
        }

        // ── 3. Xây dựng danh sách trait đã unlock từ các slime đang có ──
        foreach (var s in _ownedSlimes)
        {
            RegisterTraitUnlock(s.body);
            RegisterTraitUnlock(s.armor);
            RegisterTraitUnlock(s.weapon);
        }

        // ── 4. Nạp thêm từ PlayerStatsManager.traitLedger (trait đã từng có, dù không còn sở hữu) ──
        if (PlayerStatsManager.Instance != null)
        {
            var ledger = PlayerStatsManager.Instance.GetTraitLedger();
            if (ledger != null)
            {
                foreach (var tName in ledger)
                    _unlockedTraitNames.Add(tName);
            }
        }


        // ── 5. Suy ra Skill đã unlock từ Trait đã unlock ──
        // NOTE: _unlockedTraitNames chứa TraitSO.traitName (display name),
        //       còn allTraitsDatabase.name là asset filename — cần match bằng traitName.
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
        _unlockedTraitNames.Add(ti.baseTrait.traitName); // traitName khớp với PlayerStatsManager ledger
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

    /// <summary>Trả về danh sách toàn bộ Slime đang sở hữu.</summary>
    public List<Slime> GetOwnedSlimes() => _ownedSlimes;

    /// <summary>Trả về toàn bộ Body traits (đại diện cho Loài Slime).</summary>
    public List<TraitSO> GetAllBodyTraits() =>
        allTraitsDatabase.Where(t => t != null && t.type == TraitType.Body).ToList();

    /// <summary>Trả về toàn bộ Armor traits.</summary>
    public List<TraitSO> GetAllArmorTraits() =>
        allTraitsDatabase.Where(t => t != null && t.type == TraitType.Armor).ToList();

    /// <summary>Trả về toàn bộ Weapon traits.</summary>
    public List<TraitSO> GetAllWeaponTraits() =>
        allTraitsDatabase.Where(t => t != null && t.type == TraitType.Weapon).ToList();

    /// <summary>Trả về toàn bộ Skills trong database.</summary>
    public List<SkillSO> GetAllSkills() =>
        allSkillsDatabase.Where(s => s != null).ToList();

    /// <summary>
    /// Lấy con slime tốt nhất (tổng stat cao nhất) thuộc một Body trait nhất định.
    /// Dùng để hiển thị thông số trong trang chi tiết.
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
