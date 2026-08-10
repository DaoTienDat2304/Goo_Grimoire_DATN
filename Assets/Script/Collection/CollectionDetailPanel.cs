using UnityEngine;
using UnityEngine.UI;
using System.Text;

/// <summary>
/// Trang chi tiết bên trái của Collection Book.
/// Cập nhật nội dung khi người chơi nhấn vào một item trong lưới.
/// Hỗ trợ hiển thị 3 chế độ: chi tiết Slime, chi tiết Trait, chi tiết Skill.
/// </summary>
public class CollectionDetailPanel : MonoBehaviour
{
    [Header("Slime Avatar (3 layers)")]
    public Image avatarBody;
    public Image avatarArmor;
    public Image avatarWeapon;
    public Image avatarFrame;       // Khung viền avatar theo rarity

    [Header("Basic Info")]
    public Text titleText;          // Tên slime / tên trait / tên skill
    public Text subtitleText;       // Loại (Body/Armor/Weapon) + Rarity

    [Header("Stats Display")]
    public GameObject statsGroup;   // Group ẩn/hiện tùy loại item
    public Text hpText;
    public Text atkText;
    public Text matkText;
    public Text defText;
    public Text spdText;
    public Text critRateText;

    [Header("Equipment Slots (6 ô: Thân, Giáp, Vũ khí, Skill Thân, Skill Giáp, Skill Vũ khí)")]
    public Image[] equipSlots = new Image[6];

    [Header("Description (dùng cho Skill/Trait)")]
    public GameObject descriptionGroup;
    public Text descriptionText;

    // ── Stat display max values (dùng để tính tỉ lệ bar) ──
    private const int MAX_DISPLAY_HP = 60000;
    private const int MAX_DISPLAY_STAT = 5000;

    // ─────────────────────────────────────────
    // Public Show Methods
    // ─────────────────────────────────────────

    /// <summary>Hiển thị chi tiết một con Slime cụ thể.</summary>
    public void ShowSlimeDetail(Slime slime, TraitSO bodyTrait)
    {
        if (slime == null) return;

        SetAvatarLayers(slime.body?.sprite, slime.armor?.sprite, slime.weapon?.sprite);
        SetAvatarFrameColor(bodyTrait != null ? bodyTrait.rarity : Rarity.Common);

        // Title
        SetTitle(slime.slimeName, bodyTrait != null ? bodyTrait.rarity.ToString() : "");

        // Stats
        ShowStats(true);
        SetStats(slime.totalHP, slime.totalAttack, slime.totalMagicAttack,
                 slime.totalDefense, slime.totalSpeed, slime.totalCritRate);

        // Equipment slots: hiển thị skill icon trong 3 ô đầu

        // Equipment slots: hiển thị skill icon trong 3 ô đầu
        SetRowActive(0, true);
        SetRowActive(1, true);
        SetRowActive(2, true);
        SetEquipSlots(slime);

        // Ẩn description
        if (descriptionGroup != null) descriptionGroup.SetActive(false);
    }

    /// <summary>Hiển thị chi tiết một Trait (bộ phận).</summary>
    public void ShowTraitDetail(TraitSO trait)
    {
        if (trait == null) return;

        // Avatar: chỉ hiển thị sprite của trait
        SetAvatarLayers(trait.sprite, null, null);
        SetAvatarFrameColor(trait.rarity);

        SetTitle(trait.traitName, trait.rarity.ToString());

        bool isBody = trait.type == TraitType.Body;
        bool isArmor = trait.type == TraitType.Armor;
        bool isWeapon = trait.type == TraitType.Weapon;

        // Bật/tắt các hàng cha dựa trên loại bộ phận đang xem
        SetRowActive(0, isBody);
        SetRowActive(1, isArmor);
        SetRowActive(2, isWeapon);

        // Stats — hiển thị range stats của bộ phận này
        ShowStats(true);
        SetTraitStats(trait);

        // Reset toàn bộ các ô con
        for (int i = 0; i < equipSlots.Length; i++)
        {
            if (equipSlots[i] == null) continue;
            equipSlots[i].sprite = null;
            equipSlots[i].color = Color.clear;
            equipSlots[i].gameObject.SetActive(false);
        }
        
        // Hiện ô bộ phận tương ứng
        int traitSlotIndex = isBody ? 0 : (isArmor ? 1 : 2);
        if (equipSlots[traitSlotIndex] != null && trait.sprite != null)
        {
            equipSlots[traitSlotIndex].gameObject.SetActive(true);
            equipSlots[traitSlotIndex].sprite = trait.sprite;
            equipSlots[traitSlotIndex].color = Color.white;
        }

        // Hiện ô skill tương ứng nếu bộ phận có skill
        int skillSlotIndex = isBody ? 3 : (isArmor ? 4 : 5);
        if (trait.skill != null && equipSlots[skillSlotIndex] != null)
        {
            equipSlots[skillSlotIndex].gameObject.SetActive(true);
            equipSlots[skillSlotIndex].sprite = trait.skill.icon;
            equipSlots[skillSlotIndex].color = Color.white;
        }

        // Description: hiển thị tên skill nếu có
        if (descriptionGroup != null)
        {
            descriptionGroup.SetActive(true);
            var sb = new StringBuilder();
            if (trait.skill != null) sb.AppendLine("Kỹ năng: " + trait.skill.skillName);
            if (trait.ultimateSkill != null) sb.AppendLine("Ultimate: " + trait.ultimateSkill.skillName);
            if (descriptionText != null) descriptionText.text = sb.ToString().Trim();
        }
    }

    /// <summary>Hiển thị chi tiết một Skill.</summary>
    public void ShowSkillDetail(SkillSO skill)
    {
        if (skill == null) return;

        // Avatar: icon của skill
        SetAvatarLayers(skill.icon, null, null);
        SetAvatarFrameColor(skill.rarity);

        SetTitle(skill.skillName, skill.rarity.ToString());

        // Ẩn stats, hiển thị description
        ShowStats(false);
        SetRowActive(0, false);
        SetRowActive(1, false);
        SetRowActive(2, false);

        if (descriptionGroup != null)
        {
            descriptionGroup.SetActive(true);
            var sb = new StringBuilder();
            sb.AppendLine(skill.description);
            if (skill.battlePointCost > 0) sb.AppendLine($"Tốn: {skill.battlePointCost} Điểm Chiến Đấu");
            if (skill.energyCost > 0) sb.AppendLine($"Tốn: {skill.energyCost} Năng Lượng");
            if (skill.battlePointGain > 0) sb.AppendLine($"Nhận: +{skill.battlePointGain} Điểm Chiến Đấu");
            if (descriptionText != null) descriptionText.text = sb.ToString().Trim();
        }

        // Ẩn toàn bộ 6 ô trang bị
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

    /// <summary>Xóa nội dung chi tiết về trạng thái trống (khi chuyển tab).</summary>
    public void ClearDetail()
    {
        SetAvatarLayers(null, null, null);
        if (titleText != null) titleText.text = "Chọn một mục để xem chi tiết";
        if (subtitleText != null) subtitleText.text = "";
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

    private void SetTitle(string title, string subtitle)
    {
        if (titleText != null) titleText.text = title;
        if (subtitleText != null) subtitleText.text = subtitle;
    }

    private void ShowStats(bool show)
    {
        if (statsGroup != null) statsGroup.SetActive(show);
    }

    private void SetStats(int hp, int atk, int matk, int def, int spd, float crit)
    {
        if (hpText != null)       hpText.text = hp.ToString("N0");
        if (atkText != null)      atkText.text = atk.ToString("N0");
        if (matkText != null)     matkText.text = matk.ToString("N0");
        if (defText != null)      defText.text = def.ToString("N0");
        if (spdText != null)      spdText.text = spd.ToString();
        if (critRateText != null) critRateText.text = (crit * 100f).ToString("F0") + "%";
    }

    private void SetTraitStats(TraitSO trait)
    {
        // Hiển thị range stats từ TraitSO
        if (hpText != null)    hpText.text    = trait.type == TraitType.Body   ? $"{trait.HPRange.x} ~ {trait.HPRange.y}" : "-";
        if (atkText != null)   atkText.text   = trait.type == TraitType.Weapon ? $"{trait.attackRange.x} ~ {trait.attackRange.y}" : "-";
        if (matkText != null)  matkText.text  = trait.type == TraitType.Weapon ? $"{trait.magicAttackRange.x} ~ {trait.magicAttackRange.y}" : "-";
        if (defText != null)   defText.text   = trait.type == TraitType.Body   ? $"{trait.defenseRange.x} ~ {trait.defenseRange.y}" : "-";
        if (spdText != null)   spdText.text   = trait.type == TraitType.Body   ? $"{trait.speedRange.x} ~ {trait.speedRange.y}" : "-";
        if (critRateText != null) critRateText.text = trait.type == TraitType.Armor ? $"{trait.critRateRange.x} ~ {trait.critRateRange.y}%" : "-";
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
        // 6 ô:
        // Slot 0: Thân, Slot 1: Giáp, Slot 2: Vũ khí
        // Slot 3: Skill Thân, Slot 4: Skill Giáp, Slot 5: Skill Vũ khí
        
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
            equipSlots[i].gameObject.SetActive(true); // Luôn hiện đủ 6 ô khi xem Slime
            equipSlots[i].color = spr != null ? Color.white : Color.clear; // Nếu trống thì ẩn icon chính nhưng giữ khung active
        }
    }
}
