using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Script cho mỗi ô item trong lưới Collection Book.
/// Dùng được cho cả 3 tab: Slime, Trait (Parts), Skill.
/// </summary>
public class CollectionGridItem : MonoBehaviour
{
    [Header("Common UI — có ở tất cả 3 loại item")]
    public Image rarityBorder;        // Viền màu theo độ hiếm
    public Text itemNameText;         // Tên hiển thị
    public GameObject lockOverlay;    // Lớp phủ tối khi chưa unlock
    [Header("Icon (Dùng chung cho Slime, Trait, Skill)")]
    public Image iconImage;           // Hình ảnh (slime body / trait / skill icon)

    [Header("Rarity Border Sprites")]
    public Sprite[] rarityBorderSprites; // 0=Common 1=Uncommon 2=Rare 3=SuperRare 4=UltraRare 5=Legendary 6=Mythic 7=Secret

    private Button _btn;
    private Action _onClickCallback;

    void Awake()
    {
        _btn = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
        _btn.onClick.AddListener(OnClicked);
    }

    // ─────────────────────────────────────────
    // Setup Methods
    // ─────────────────────────────────────────

    /// <summary>Thiết lập ô hiển thị Loài Slime (tab 1).</summary>
    public void SetupAsSlime(TraitSO bodyTrait, Slime bestSlime, bool unlocked, Action onClick)
    {
        _onClickCallback = onClick;

        if (unlocked && bestSlime != null)
        {
            // Chỉ hiển thị hình ảnh body của slime vào Icon
            SetLayer(iconImage, bestSlime.body?.sprite);

            // Tên = tên con slime
            if (itemNameText != null) itemNameText.text = bestSlime.slimeName;

            // Viền theo rarity của body trait
            SetRarityBorder(bodyTrait.rarity);
        }
        else if (unlocked)
        {
            // Đã unlock nhưng chưa có con tốt nhất → chỉ hiển thị sprite body trait
            SetLayer(iconImage, bodyTrait.sprite);
            if (itemNameText != null) itemNameText.text = bodyTrait.traitName;
            SetRarityBorder(bodyTrait.rarity);
        }
        else
        {
            // Chưa unlock → bóng đen + dấu ?
            SetLayer(iconImage, null);
            if (itemNameText != null) itemNameText.text = "???";
            if (rarityBorder != null) rarityBorder.color = Color.gray;
        }

        if (lockOverlay != null) lockOverlay.SetActive(!unlocked);
    }

    /// <summary>Thiết lập ô hiển thị Bộ phận (tab 2).</summary>
    public void SetupAsTrait(TraitSO trait, bool unlocked, Action onClick)
    {
        _onClickCallback = onClick;

        if (unlocked)
        {
            SetLayer(iconImage, trait.sprite);
            if (itemNameText != null) itemNameText.text = trait.traitName;
            SetRarityBorder(trait.rarity);
        }
        else
        {
            SetLayer(iconImage, null);
            if (itemNameText != null) itemNameText.text = "???";
            if (rarityBorder != null) rarityBorder.color = Color.gray;
        }

        if (lockOverlay != null) lockOverlay.SetActive(!unlocked);
    }

    /// <summary>Thiết lập ô hiển thị Kỹ năng (tab 3).</summary>
    public void SetupAsSkill(SkillSO skill, bool unlocked, Action onClick)
    {
        _onClickCallback = onClick;

        if (unlocked)
        {
            SetLayer(iconImage, skill.icon);
            if (itemNameText != null) itemNameText.text = skill.skillName;
            SetRarityBorder(skill.rarity);
        }
        else
        {
            SetLayer(iconImage, null);
            if (itemNameText != null) itemNameText.text = "???";
            if (rarityBorder != null) rarityBorder.color = Color.gray;
        }

        if (lockOverlay != null) lockOverlay.SetActive(!unlocked);
    }

    // ─────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────

    private void SetLayer(Image layer, Sprite sprite)
    {
        if (layer == null) return;
        layer.gameObject.SetActive(true);
        layer.sprite = sprite;
        layer.color = sprite != null ? Color.white : Color.clear;
    }

    private void SetRarityBorder(Rarity rarity)
    {
        if (rarityBorder == null) return;
        int idx = (int)rarity;
        if (rarityBorderSprites != null && idx < rarityBorderSprites.Length && rarityBorderSprites[idx] != null)
        {
            rarityBorder.sprite = rarityBorderSprites[idx];
            rarityBorder.color = Color.white;
        }
        else
        {
            // Fallback: chỉ đổi màu border theo rarity
            rarityBorder.color = GetRarityColor(rarity);
        }
    }

    public static Color GetRarityColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return new Color(0.7f, 0.7f, 0.7f);
            case Rarity.Uncommon:  return new Color(0.3f, 0.8f, 0.3f);
            case Rarity.Rare:      return new Color(0.3f, 0.5f, 1.0f);
            case Rarity.SuperRare: return new Color(0.8f, 0.3f, 1.0f);
            case Rarity.UltraRare: return new Color(1.0f, 0.5f, 0.1f);
            case Rarity.Legendary: return new Color(1.0f, 0.85f, 0.0f);
            case Rarity.Mythic:    return new Color(1.0f, 0.2f, 0.2f);
            case Rarity.Secret:    return new Color(0.1f, 1.0f, 0.9f);
            default:               return Color.white;
        }
    }

    private void OnClicked()
    {
        _onClickCallback?.Invoke();
    }
}
