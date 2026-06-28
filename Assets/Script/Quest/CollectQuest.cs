using UnityEngine;

/// <summary>Slot nào của slime cần kiểm tra. "Any" = bất kỳ slot nào đạt điều kiện là đủ.</summary>
public enum TraitSlotFilter { Any, Body, Armor, Weapon }

[CreateAssetMenu(menuName = "Quests/Collect Quest")]
public class CollectQuest : Quest
{
    [Header("Collect Quest Settings")]
    [Tooltip("Cần sưu tầm bao nhiêu slime đạt điều kiện")]
    public int slimeGoal = 1;

    [Header("Trait Filter")]
    [Tooltip("Slot cần kiểm tra. Any = bất kỳ slot nào đạt là đủ")]
    public TraitSlotFilter traitSlot = TraitSlotFilter.Any;

    [Tooltip("Trait cụ thể cần có. Để trống = chấp nhận mọi trait")]
    public TraitSO requiredTrait;

    [Tooltip("Độ hiếm tối thiểu cần đạt")]
    public Rarity minimumRarity = Rarity.Common;

    [Tooltip("Nếu bật: chỉ chấp nhận đúng độ hiếm. Tắt: >= minimumRarity")]
    public bool exactRarity = false;

    // ── Public API ────────────────────────────────────────────────────

    public override bool CheckCompletion() => CountQualifyingSlimes() >= slimeGoal;

    public int CountQualifyingSlimes()
    {
        if (BreedingManager.Instance == null) return 0;

        int count = 0;
        foreach (var slime in BreedingManager.Instance.GetAllSlimes())
        {
            if (slime != null && SlimeQualifies(slime))
                count++;
        }
        return count;
    }

    public override float GetProgressPercentage()
    {
        if (slimeGoal <= 0) return 0f;
        return Mathf.Clamp01((float)CountQualifyingSlimes() / slimeGoal) * 100f;
    }

    public override string GetProgressText()
    {
        int cur = CountQualifyingSlimes();
        return $"{cur} / {slimeGoal} slime ({BuildFilterLabel()}) — {GetProgressPercentage():F0}%";
    }

    // ── Internal helpers ──────────────────────────────────────────────

    private bool SlimeQualifies(Slime slime)
    {
        if (traitSlot == TraitSlotFilter.Any)
            return TraitInstanceQualifies(slime.body)
                || TraitInstanceQualifies(slime.armor)
                || TraitInstanceQualifies(slime.weapon);

        var instance = traitSlot switch
        {
            TraitSlotFilter.Body   => slime.body,
            TraitSlotFilter.Armor  => slime.armor,
            TraitSlotFilter.Weapon => slime.weapon,
            _                      => null
        };
        return TraitInstanceQualifies(instance);
    }

    private bool TraitInstanceQualifies(TraitInstance t)
    {
        if (t == null) return false;

        // Kiểm tra trait cụ thể
        if (requiredTrait != null && t.baseTrait != requiredTrait)
            return false;

        // Kiểm tra độ hiếm
        return exactRarity
            ? t.Rarity == minimumRarity
            : (int)t.Rarity >= (int)minimumRarity;
    }

    private string BuildFilterLabel()
    {
        string slot  = traitSlot == TraitSlotFilter.Any ? "" : $"[{traitSlot}] ";
        string trait = requiredTrait != null ? $"{requiredTrait.traitName} " : "";
        string rar   = exactRarity ? $"{minimumRarity}" : $"{minimumRarity}+";
        return $"{slot}{trait}{rar}";
    }
}
