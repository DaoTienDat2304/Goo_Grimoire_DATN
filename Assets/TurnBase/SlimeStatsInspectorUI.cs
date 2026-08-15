using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng hiển thị thông số, bộ chỉ số chiến đấu (HP/ATK/DEF/SPD/Crit) và Trait trang bị của Slime
/// khi người chơi bấm vào bất kỳ Slime nào trong trận đấu (Player Slime hoặc Enemy Slime).
/// </summary>
public class SlimeStatsInspectorUI : MonoBehaviour
{
    public static SlimeStatsInspectorUI Instance { get; private set; }

    [Header("Inspector Panel")]
    public GameObject inspectorPanel;
    public Image slimeAvatarImage;
    public Text slimeNameText;
    public Text slimeLevelText;

    [Header("Status Bars")]
    public Slider hpSlider;
    public Text hpText;
    public Slider energySlider;
    public Text energyText;

    [Header("Battle Stats")]
    public Text atkText;
    public Text defText;
    public Text spdText;
    public Text magicAtkText;
    public Text critRateText;
    public Text critDmgText;

    [Header("Traits Info")]
    public Text bodyTraitText;
    public Text armorTraitText;
    public Text weaponTraitText;

    [Header("Close Button")]
    public Button closeButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        if (closeButton != null)
            closeButton.onClick.AddListener(HideInspector);

        if (inspectorPanel != null)
            inspectorPanel.SetActive(false);
    }

    /// <summary>
    /// Hiển thị thông số chi tiết của Slime khi người chơi chạm vào
    /// </summary>
    public void InspectSlime(SlimeStats slime)
    {
        if (slime == null) return;
        var battleStats = slime.GetComponent<SlimeBattleStats>();

        if (inspectorPanel != null) inspectorPanel.SetActive(true);

        // Ẩn icon/avatar khi xem thông tin Slime trong trận theo yêu cầu
        if (slimeAvatarImage != null)
        {
            slimeAvatarImage.gameObject.SetActive(false);
        }

        // Hiện nút Exit/Đóng khi xem thông tin Slime
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true);
        }

        if (slimeNameText != null)
        {
            slimeNameText.resizeTextForBestFit = true;
            string displayName = !string.IsNullOrEmpty(slime.slimeName) ? slime.slimeName : slime.gameObject.name.Replace("(Clone)", "").Trim();
            slimeNameText.text = displayName;
        }

        if (slimeLevelText != null)
        {
            // Game không có hệ thống cấp độ (Lv) riêng biệt -> Ẩn text Lv hoặc hiển thị độ hiếm
            slimeLevelText.gameObject.SetActive(false);
        }

        // HP & Energy & Battle Stats
        if (battleStats != null)
        {
            if (hpSlider != null)
            {
                hpSlider.maxValue = battleStats.MaxHP;
                hpSlider.value = battleStats.CurrentHP;
            }
            if (hpText != null)
            {
                hpText.resizeTextForBestFit = true;
                hpText.text = $"HP: {battleStats.CurrentHP} / {battleStats.MaxHP}";
            }

            if (energySlider != null)
            {
                energySlider.maxValue = 100;
                energySlider.value = battleStats.CurrentEnergy;
            }
            if (energyText != null)
            {
                energyText.resizeTextForBestFit = true;
                energyText.text = $"Năng lượng: {battleStats.CurrentEnergy} / 100";
            }

            // Stats
            if (atkText != null) { atkText.resizeTextForBestFit = true; atkText.text = $"Công: {battleStats.BattleAttack}"; }
            if (defText != null) { defText.resizeTextForBestFit = true; defText.text = $"Thủ: {battleStats.BattleDefense}"; }
            if (spdText != null) { spdText.resizeTextForBestFit = true; spdText.text = $"Tốc: {battleStats.BattleSpeed}"; }
            if (magicAtkText != null) { magicAtkText.resizeTextForBestFit = true; magicAtkText.text = $"Phép: {battleStats.BattleMagicAttack}"; }
            if (critRateText != null) { critRateText.resizeTextForBestFit = true; critRateText.text = $"Bạo kích: {battleStats.BattleCritRate * 100:F1}%"; }
            if (critDmgText != null) { critDmgText.resizeTextForBestFit = true; critDmgText.text = $"ST Bạo: {battleStats.BattleCritDMG * 100:F0}%"; }
        }
        else
        {
            if (atkText != null) { atkText.resizeTextForBestFit = true; atkText.text = $"Công: {slime.Attack}"; }
            if (defText != null) { defText.resizeTextForBestFit = true; defText.text = $"Thủ: {slime.Defense}"; }
            if (spdText != null) { spdText.resizeTextForBestFit = true; spdText.text = $"Tốc: {slime.Speed}"; }
            if (magicAtkText != null) { magicAtkText.resizeTextForBestFit = true; magicAtkText.text = $"Phép: {slime.MagicAttack}"; }
            if (critRateText != null) { critRateText.resizeTextForBestFit = true; critRateText.text = $"Bạo kích: {slime.CritRate * 100:F1}%"; }
            if (critDmgText != null) { critDmgText.resizeTextForBestFit = true; critDmgText.text = $"ST Bạo: {slime.CritDMG * 100:F0}%"; }
        }

        // Traits
        if (bodyTraitText != null)
        {
            bodyTraitText.resizeTextForBestFit = true;
            string bodyName = (slime.bodySkill != null && slime.bodySkill.baseSkill != null) ? slime.bodySkill.baseSkill.skillName : "Không";
            bodyTraitText.text = $"Thân: {bodyName}";
        }

        if (armorTraitText != null)
        {
            armorTraitText.resizeTextForBestFit = true;
            string armorName = (slime.armorSkill != null && slime.armorSkill.baseSkill != null) ? slime.armorSkill.baseSkill.skillName : "Không";
            armorTraitText.text = $"Giáp: {armorName}";
        }

        if (weaponTraitText != null)
        {
            weaponTraitText.resizeTextForBestFit = true;
            string weaponName = (slime.weaponSkill != null && slime.weaponSkill.baseSkill != null) ? slime.weaponSkill.baseSkill.skillName : "Không";
            weaponTraitText.text = $"Vũ khí: {weaponName}";
        }
    }

    public void HideInspector()
    {
        if (inspectorPanel != null)
            inspectorPanel.SetActive(false);
    }
}
