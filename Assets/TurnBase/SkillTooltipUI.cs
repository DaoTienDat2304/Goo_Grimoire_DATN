using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tooltip hiển thị thông tin mô tả chi tiết của Skill khi người chơi nhấn giữ hoặc chạm vào Skill.
/// </summary>
public class SkillTooltipUI : MonoBehaviour
{
    public static SkillTooltipUI Instance { get; private set; }

    [Header("Tooltip Panel")]
    public GameObject tooltipPanel;
    public Image skillIconImage;
    public Text skillNameText;
    public Text skillTypeText;
    public Text skillCostText;
    public Text skillCooldownText;
    public Text skillDescriptionText;
    public Button closeButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        if (closeButton != null)
            closeButton.onClick.AddListener(HideTooltip);

        // Hỗ trợ chạm vào bảng để đóng khi nút exit bị ẩn
        if (tooltipPanel != null)
        {
            var btn = tooltipPanel.GetComponent<Button>();
            if (btn == null) btn = tooltipPanel.AddComponent<Button>();
            btn.onClick.AddListener(HideTooltip);
            tooltipPanel.SetActive(false);
        }
    }

    public void ShowTooltip(SkillInstance skill, SlimeBattleStats battleStats = null)
    {
        if (skill == null || skill.baseSkill == null) return;
        var baseSkill = skill.baseSkill;

        if (tooltipPanel != null) tooltipPanel.SetActive(true);

        // Ẩn nút exit khi xem Skill theo yêu cầu
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
        }

        // Hiện icon của Skill
        if (skillIconImage != null)
        {
            skillIconImage.sprite = baseSkill.icon;
            skillIconImage.gameObject.SetActive(baseSkill.icon != null);
        }

        if (skillNameText != null)
            skillNameText.text = baseSkill.skillName;

        bool isUltimateSkill = baseSkill.type == SkillType.Ultimate || baseSkill.energyCost >= 100;

        if (skillTypeText != null)
        {
            string typeStr = baseSkill.type switch
            {
                SkillType.BasicAttack => "Đánh Thường (Basic Attack)",
                SkillType.Active => "Chiến Kỹ (Active Skill)",
                SkillType.Passive => "Nội Tại (Passive Skill)",
                SkillType.Ultimate => "Tuyệt Kỹ (Ultimate Skill)",
                _ => baseSkill.type.ToString()
            };

            if (isUltimateSkill)
                typeStr = "Tuyệt Kỹ (Ultimate Skill)";

            skillTypeText.text = typeStr;
        }

        if (skillCostText != null)
        {
            if (isUltimateSkill || baseSkill.energyCost > 0)
            {
                skillCostText.text = $"Chi phí: {baseSkill.energyCost} Năng lượng (Energy)";
                skillCostText.color = new Color(1f, 0.85f, 0.2f);
            }
            else if (baseSkill.battlePointCost > 0)
            {
                skillCostText.text = $"Chi phí: {baseSkill.battlePointCost} Điểm Chiến Kỹ (ĐCK)";
                skillCostText.color = new Color(0.4f, 0.8f, 1f);
            }
            else if (baseSkill.battlePointGain > 0)
            {
                skillCostText.text = $"Hồi phục: +{baseSkill.battlePointGain} Điểm Chiến Kỹ (ĐCK)";
                skillCostText.color = Color.green;
            }
            else
            {
                skillCostText.text = "Chi phí: Miễn phí";
                skillCostText.color = Color.white;
            }
        }

        if (skillDescriptionText != null)
        {
            string desc = string.IsNullOrEmpty(baseSkill.description)
                ? "Không có mô tả chi tiết cho kỹ năng này."
                : baseSkill.description;

            skillDescriptionText.text = desc;
        }
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
}
