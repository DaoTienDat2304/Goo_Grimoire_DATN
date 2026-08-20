using UnityEngine;
using UnityEngine.UI;

/// <summary>
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

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
        }

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
                SkillType.BasicAttack => "Basic Attack",
                SkillType.Active => "Active Skill",
                SkillType.Passive => "Passive Skill",
                SkillType.Ultimate => "Ultimate Skill",
                _ => baseSkill.type.ToString()
            };

            if (isUltimateSkill)
                typeStr = "Ultimate Skill";

            skillTypeText.text = typeStr;
        }

        if (skillCostText != null)
        {
            if (isUltimateSkill || baseSkill.energyCost > 0)
            {
                skillCostText.text = $"Cost: {baseSkill.energyCost} Energy";
                skillCostText.color = new Color(1f, 0.85f, 0.2f);
            }
            else if (baseSkill.battlePointCost > 0)
            {
                skillCostText.text = $"Cost: {baseSkill.battlePointCost} Skill Point(s)";
                skillCostText.color = new Color(0.4f, 0.8f, 1f);
            }
            else if (baseSkill.battlePointGain > 0)
            {
                skillCostText.text = $"Gain: +{baseSkill.battlePointGain} Skill Point(s)";
                skillCostText.color = Color.green;
            }
            else
            {
                skillCostText.text = "Cost: Free";
                skillCostText.color = Color.white;
            }
        }

        if (skillDescriptionText != null)
        {
            string desc = string.IsNullOrEmpty(baseSkill.description)
                ? "No description available."
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
