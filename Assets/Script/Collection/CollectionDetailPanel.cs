using UnityEngine;
using UnityEngine.UI;
using System.Text;

/// <summary>
/// </summary>
public class CollectionDetailPanel : MonoBehaviour
{
    [Header("Slime Avatar (3 layers)")]
    public Image avatarBody;
    public Image avatarArmor;
    public Image avatarWeapon;
    public Image avatarFrame;

    [Header("Basic Info")]
    public Text titleText;
    public Text subtitleText;

    [Header("Stats Display")]
    public GameObject statsGroup;
    public Text hpText;
    public Text atkText;
    public Text matkText;
    public Text defText;
    public Text spdText;
    public Text critRateText;

    [Header("Equipment Slots")]
    public Image[] equipSlots = new Image[6];

    [Header("Description")]
    public GameObject descriptionGroup;
    public Text descriptionText;

    private const int MAX_DISPLAY_HP = 60000;
    private const int MAX_DISPLAY_STAT = 5000;

    // ─────────────────────────────────────────
    // Public Show Methods
    // ─────────────────────────────────────────

    public void ShowSlimeDetail(Slime slime, TraitSO bodyTrait)
    {
        if (slime == null) return;

        SetAvatarLayers(slime.body?.sprite, slime.armor?.sprite, slime.weapon?.sprite);
        SetAvatarFrameColor(bodyTrait != null ? bodyTrait.rarity : Rarity.Common);

        // Title
        SetTitle(slime.slimeName, bodyTrait != null ? bodyTrait.rarity.ToString() : "", bodyTrait != null ? bodyTrait.rarity : Rarity.Common);

        // Stats
        ShowStats(true);
        SetStats(slime.totalHP, slime.totalAttack, slime.totalMagicAttack,
                 slime.totalDefense, slime.totalSpeed, slime.totalCritRate);

        SetRowActive(0, true);
        SetRowActive(1, true);
        SetRowActive(2, true);
        SetEquipSlots(slime);

        if (descriptionGroup != null) descriptionGroup.SetActive(false);
    }

    public void ShowTraitDetail(TraitSO trait)
    {
        if (trait == null) return;

        SetAvatarLayers(trait.sprite, null, null);
        SetAvatarFrameColor(trait.rarity);

        SetTitle(trait.traitName, $"{trait.type} • {trait.rarity}", trait.rarity);

        bool isBody = trait.type == TraitType.Body;
        bool isArmor = trait.type == TraitType.Armor;
        bool isWeapon = trait.type == TraitType.Weapon;

        SetRowActive(0, isBody);
        SetRowActive(1, isArmor);
        SetRowActive(2, isWeapon);

        ShowStats(true);
        SetTraitStats(trait);

        for (int i = 0; i < equipSlots.Length; i++)
        {
            if (equipSlots[i] == null) continue;
            equipSlots[i].sprite = null;
            equipSlots[i].color = Color.clear;
            equipSlots[i].gameObject.SetActive(false);
        }
        
        int traitSlotIndex = isBody ? 0 : (isArmor ? 1 : 2);
        if (equipSlots[traitSlotIndex] != null && trait.sprite != null)
        {
            equipSlots[traitSlotIndex].gameObject.SetActive(true);
            equipSlots[traitSlotIndex].sprite = trait.sprite;
            equipSlots[traitSlotIndex].color = Color.white;
        }

        int skillSlotIndex = isBody ? 3 : (isArmor ? 4 : 5);
        if (trait.skill != null && equipSlots[skillSlotIndex] != null)
        {
            equipSlots[skillSlotIndex].gameObject.SetActive(true);
            equipSlots[skillSlotIndex].sprite = trait.skill.icon;
            equipSlots[skillSlotIndex].color = Color.white;
        }

        if (descriptionGroup != null)
        {
            descriptionGroup.SetActive(true);
            var sb = new StringBuilder();
            if (trait.skill != null) sb.AppendLine("Skill: " + trait.skill.skillName);
            if (trait.ultimateSkill != null) sb.AppendLine("Ultimate: " + trait.ultimateSkill.skillName);
            if (descriptionText != null)
            {
                descriptionText.resizeTextForBestFit = true;
                descriptionText.text = sb.ToString().Trim();
            }
        }
    }

    public void ShowSkillDetail(SkillSO skill)
    {
        if (skill == null) return;

        SetAvatarLayers(skill.icon, null, null);
        SetAvatarFrameColor(skill.rarity);

        SetTitle(skill.skillName, $"Skill • {skill.rarity}", skill.rarity);

        ShowStats(false);
        SetRowActive(0, false);
        SetRowActive(1, false);
        SetRowActive(2, false);

        if (descriptionGroup != null)
        {
            descriptionGroup.SetActive(true);
            var sb = new StringBuilder();
            sb.AppendLine(skill.description);
            if (skill.battlePointCost > 0) sb.AppendLine($"Cost: {skill.battlePointCost} Skill Point(s)");
            if (skill.energyCost > 0) sb.AppendLine($"Cost: {skill.energyCost} Energy");
            if (skill.battlePointGain > 0) sb.AppendLine($"Gain: +{skill.battlePointGain} Skill Point(s)");
            if (descriptionText != null)
            {
                descriptionText.resizeTextForBestFit = true;
                descriptionText.text = sb.ToString().Trim();
            }
        }

        for (int i = 0; i < equipSlots.Length; i++)
        {
            if (equipSlots[i] != null)
            {
                equipSlots[i].sprite = null;
                equipSlots[i].color = Color.clear;
                equipSlots[i].gameObject.SetActive(false);
            }
        }
    }

    public void ClearDetail()
    {
        SetAvatarLayers(null, null, null);
        if (titleText != null)
        {
            titleText.resizeTextForBestFit = true;
            titleText.text = "Select an item to view details";
        }
        if (subtitleText != null)
        {
            subtitleText.resizeTextForBestFit = true;
            subtitleText.text = "";
        }
        ShowStats(false);
        SetRowActive(0, false);
        SetRowActive(1, false);
        SetRowActive(2, false);
        if (descriptionGroup != null) descriptionGroup.SetActive(false);
        for (int i = 0; i < equipSlots.Length; i++)
        {
            if (equipSlots[i] != null)
            {
                equipSlots[i].sprite = null;
                equipSlots[i].color = Color.clear;
                equipSlots[i].gameObject.SetActive(false);
            }
        }
    }

    // ─────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────

    private void SetAvatarLayers(Sprite body, Sprite armor, Sprite weapon)
    {
        SetAvatarImage(avatarBody, body);
        SetAvatarImage(avatarArmor, armor);
        SetAvatarImage(avatarWeapon, weapon);
    }

    private void SetAvatarImage(Image img, Sprite sprite)
    {
        if (img == null) return;
        img.sprite = sprite;
        img.color = sprite != null ? Color.white : Color.clear;
    }

    private void SetAvatarFrameColor(Rarity rarity)
    {
        if (avatarFrame != null)
            avatarFrame.color = CollectionGridItem.GetRarityColor(rarity);
    }

    void Awake()
    {
        if (titleText != null)
        {
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 10;
            titleText.resizeTextMaxSize = 20;
            titleText.alignment = TextAnchor.MiddleCenter;
        }
        if (subtitleText != null)
        {
            subtitleText.resizeTextForBestFit = true;
            subtitleText.resizeTextMinSize = 9;
            subtitleText.resizeTextMaxSize = 16;
            subtitleText.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void SetTitle(string title, string subtitle, Rarity rarity = Rarity.Common)
    {
        if (titleText != null)
        {
            titleText.resizeTextForBestFit = true;
            titleText.text = title;
        }
        if (subtitleText != null)
        {
            subtitleText.resizeTextForBestFit = true;
            subtitleText.text = subtitle;
            subtitleText.color = CollectionGridItem.GetRarityColor(rarity);
        }
    }

    private void ShowStats(bool show)
    {
        if (statsGroup != null) statsGroup.SetActive(show);
    }

    private void SetStats(int hp, int atk, int matk, int def, int spd, float crit)
    {
        if (hpText != null)       { hpText.resizeTextForBestFit = true; hpText.text = hp.ToString("N0"); }
        if (atkText != null)      { atkText.resizeTextForBestFit = true; atkText.text = atk.ToString("N0"); }
        if (matkText != null)     { matkText.resizeTextForBestFit = true; matkText.text = matk.ToString("N0"); }
        if (defText != null)      { defText.resizeTextForBestFit = true; defText.text = def.ToString("N0"); }
        if (spdText != null)      { spdText.resizeTextForBestFit = true; spdText.text = spd.ToString(); }
        if (critRateText != null) { critRateText.resizeTextForBestFit = true; critRateText.text = (crit * 100f).ToString("F0") + "%"; }
    }

    private void SetTraitStats(TraitSO trait)
    {
        if (trait == null) return;
        var b = StatBalance.Get(trait.rarity);

        if (trait.type == TraitType.Body)
        {
            if (hpText != null)       { hpText.resizeTextForBestFit = true; hpText.text = $"{b.hpMin} ~ {b.hpMax}"; }
            if (defText != null)      { defText.resizeTextForBestFit = true; defText.text = $"{b.defMin} ~ {b.defMax}"; }
            if (spdText != null)      { spdText.resizeTextForBestFit = true; spdText.text = $"{b.spdMin} ~ {b.spdMax}"; }
            if (atkText != null)      { atkText.resizeTextForBestFit = true; atkText.text = "-"; }
            if (matkText != null)     { matkText.resizeTextForBestFit = true; matkText.text = "-"; }
            if (critRateText != null) { critRateText.resizeTextForBestFit = true; critRateText.text = "-"; }
        }
        else if (trait.type == TraitType.Weapon)
        {
            if (hpText != null)       { hpText.resizeTextForBestFit = true; hpText.text = "-"; }
            if (defText != null)      { defText.resizeTextForBestFit = true; defText.text = "-"; }
            if (spdText != null)      { spdText.resizeTextForBestFit = true; spdText.text = "-"; }
            int atkMin = trait.attackRange.x > 0 ? trait.attackRange.x : b.atkMin;
            int atkMax = trait.attackRange.y > 0 ? trait.attackRange.y : b.atkMax;
            int matkMin = trait.magicAttackRange.x > 0 ? trait.magicAttackRange.x : b.magMin;
            int matkMax = trait.magicAttackRange.y > 0 ? trait.magicAttackRange.y : b.magMax;
            if (atkText != null)      { atkText.resizeTextForBestFit = true; atkText.text = $"{atkMin} ~ {atkMax}"; }
            if (matkText != null)     { matkText.resizeTextForBestFit = true; matkText.text = $"{matkMin} ~ {matkMax}"; }
            if (critRateText != null) { critRateText.resizeTextForBestFit = true; critRateText.text = "-"; }
        }
        else if (trait.type == TraitType.Armor)
        {
            if (hpText != null)       { hpText.resizeTextForBestFit = true; hpText.text = "-"; }
            if (defText != null)      { defText.resizeTextForBestFit = true; defText.text = "-"; }
            if (spdText != null)      { spdText.resizeTextForBestFit = true; spdText.text = "-"; }
            if (atkText != null)      { atkText.resizeTextForBestFit = true; atkText.text = "-"; }
            if (matkText != null)     { matkText.resizeTextForBestFit = true; matkText.text = "-"; }
            if (critRateText != null) { critRateText.resizeTextForBestFit = true; critRateText.text = $"{b.critRate * 100f:F0}%"; }
        }
    }

    private void SetRowActive(int index, bool active)
    {
        if (index >= 0 && index < 3 && equipSlots[index] != null)
        {
            var parent = equipSlots[index].transform.parent;
            if (parent != null) parent.gameObject.SetActive(active);
        }
    }

    private void SetEquipSlots(Slime slime)
    {
        
        Sprite[] sprites = {
            slime.body?.sprite,
            slime.armor?.sprite,
            slime.weapon?.sprite,
            slime.body?.skill?.baseSkill?.icon,
            slime.armor?.skill?.baseSkill?.icon,
            slime.weapon?.skill?.baseSkill?.icon
        };

        for (int i = 0; i < equipSlots.Length; i++)
        {
            if (equipSlots[i] == null) continue;
            var spr = i < sprites.Length ? sprites[i] : null;
            equipSlots[i].sprite = spr;
            equipSlots[i].gameObject.SetActive(true);
            equipSlots[i].color = spr != null ? Color.white : Color.clear;
        }
    }
}
