using UnityEngine;

public enum TraitSlotFilter { Any, Body, Armor, Weapon }

[CreateAssetMenu(menuName = "Quests/Collect Quest")]
public class CollectQuest : Quest
{
    [Header("Collect Quest Settings")]
    [Tooltip("Required slimes")]
    public int slimeGoal = 1;

    [Header("Trait Filter")]
    [Tooltip("Slot to check")]
    public TraitSlotFilter traitSlot = TraitSlotFilter.Any;

    [Tooltip("Required trait. Empty accepts any trait.")]
    public TraitSO requiredTrait;

    [Tooltip("Minimum rarity")]
    public Rarity minimumRarity = Rarity.Common;

    [Tooltip("Exact rarity only")]
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

        if (requiredTrait != null && t.baseTrait != requiredTrait)
            return false;

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
