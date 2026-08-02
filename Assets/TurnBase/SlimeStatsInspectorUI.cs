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

        // Avatar & Name
        if (slimeAvatarImage != null)
        {
            var sr = slime.GetComponent<SpriteRenderer>();
            var img = slime.GetComponent<Image>();
            if (sr != null && sr.sprite != null) slimeAvatarImage.sprite = sr.sprite;
            else if (img != null && img.sprite != null) slimeAvatarImage.sprite = img.sprite;
        }

        if (slimeNameText != null)
            slimeNameText.text = slime.gameObject.name;

        if (slimeLevelText != null)
            slimeLevelText.text = slime.id > 0 ? $"Lv. {slime.id}" : "Lv. 1";

        // HP & Energy & Battle Stats
        if (battleStats != null)
        {
            if (hpSlider != null)
            {
                hpSlider.maxValue = battleStats.MaxHP;
                hpSlider.value = battleStats.CurrentHP;
            }
            if (hpText != null)
                hpText.text = $"HP: {battleStats.CurrentHP} / {battleStats.MaxHP}";

            if (energySlider != null)
            {
                energySlider.maxValue = 100;
                energySlider.value = battleStats.CurrentEnergy;
            }
            if (energyText != null)
                energyText.text = $"Energy: {battleStats.CurrentEnergy} / 100";

            // Stats
            if (atkText != null) atkText.text = $"ATK: {battleStats.BattleAttack}";
            if (defText != null) defText.text = $"DEF: {battleStats.BattleDefense}";
            if (spdText != null) spdText.text = $"SPD: {battleStats.BattleSpeed}";
            if (magicAtkText != null) magicAtkText.text = $"MAG: {battleStats.BattleMagicAttack}";
            if (critRateText != null) critRateText.text = $"Crit Rate: {battleStats.BattleCritRate * 100:F1}%";
            if (critDmgText != null) critDmgText.text = $"Crit DMG: {battleStats.BattleCritDMG * 100:F0}%";
        }
        else
        {
            if (atkText != null) atkText.text = $"ATK: {slime.Attack}";
            if (defText != null) defText.text = $"DEF: {slime.Defense}";
            if (spdText != null) spdText.text = $"SPD: {slime.Speed}";
            if (magicAtkText != null) magicAtkText.text = $"MAG: {slime.MagicAttack}";
            if (critRateText != null) critRateText.text = $"Crit Rate: {slime.CritRate * 100:F1}%";
            if (critDmgText != null) critDmgText.text = $"Crit DMG: {slime.CritDMG * 100:F0}%";
        }

        // Traits
        if (bodyTraitText != null)
        {
            string bodyName = (slime.bodySkill != null && slime.bodySkill.baseSkill != null) ? slime.bodySkill.baseSkill.skillName : "Không";
            bodyTraitText.text = $"Body: {bodyName}";
        }

        if (armorTraitText != null)
        {
            string armorName = (slime.armorSkill != null && slime.armorSkill.baseSkill != null) ? slime.armorSkill.baseSkill.skillName : "Không";
            armorTraitText.text = $"Armor: {armorName}";
        }

        if (weaponTraitText != null)
        {
            string weaponName = (slime.weaponSkill != null && slime.weaponSkill.baseSkill != null) ? slime.weaponSkill.baseSkill.skillName : "Không";
            weaponTraitText.text = $"Weapon: {weaponName}";
        }
    }

    public void HideInspector()
    {
        if (inspectorPanel != null)
            inspectorPanel.SetActive(false);
    }
}
