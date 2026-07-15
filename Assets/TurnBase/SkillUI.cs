using UnityEngine;
using UnityEngine.UI;

public class SkillUI : MonoBehaviour
{
    public SlimeStats slime;
    public Image bodySkill;
    public Image armorSkill;
    public Image weaponSkill;
    public Image fullSetSkill;
    public Sprite border;

    void Update()
    {
        if (slime != null)
        {
            var battleStats = slime.GetComponent<SlimeBattleStats>();

            UpdateSkillUI(bodySkill, slime.bodySkill, battleStats);
            UpdateSkillUI(armorSkill, slime.armorSkill, battleStats);
            UpdateSkillUI(weaponSkill, slime.weaponSkill, battleStats);
        }
    }

    private void UpdateSkillUI(Image skillImage, SkillInstance skill, SlimeBattleStats battleStats)
    {
        if (skillImage == null) return;

        Button btn = skillImage.GetComponent<Button>();
        if (btn == null) btn = skillImage.GetComponentInParent<Button>();

        Text textComp = skillImage.GetComponentInChildren<Text>();
        if (textComp == null && btn != null)
        {
            textComp = btn.GetComponentInChildren<Text>();
        }

        // Nếu không có skill hoặc baseSkill
        if (skill == null || skill.baseSkill == null)
        {
            skillImage.sprite = border;
            skillImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            if (btn != null) btn.interactable = false;

            if (textComp != null)
            {
                textComp.text = "Trống";
                textComp.color = Color.gray;
            }
            return;
        }

        // Cập nhật Sprite
        if (skill.baseSkill.icon != null)
        {
            skillImage.sprite = skill.baseSkill.icon;
        }
        else
        {
            skillImage.sprite = border;
        }

        bool isInteractable = false;
        string skillInfo = skill.baseSkill.skillName;
        Color textColor = Color.white;

        // Phân tích logic dựa theo SkillType
        switch (skill.baseSkill.type)
        {
            case SkillType.Passive:
                // Passive: Luôn không click được, màu sáng thường, chữ Nội tại
                isInteractable = false;
                skillInfo += "\n(Nội tại)";
                textColor = new Color(0.2f, 0.8f, 1f);
                skillImage.color = Color.white;
                break;

            case SkillType.BasicAttack:
                // Đánh thường: Thường luôn click được nếu đến lượt, hồi ĐCK
                isInteractable = true;
                if (skill.baseSkill.battlePointGain > 0)
                {
                    skillInfo += $"\n(+{skill.baseSkill.battlePointGain} ĐCK)";
                }
                textColor = Color.green;
                skillImage.color = Color.white;
                break;

            case SkillType.Active:
                // Active (Chiến kỹ): Cần đủ điểm ĐCK
                if (BattleSystemManager.Instance != null && battleStats != null)
                {
                    isInteractable = BattleSystemManager.Instance.TeamBattlePoints >= skill.baseSkill.battlePointCost;
                }

                if (skill.baseSkill.battlePointCost > 0)
                {
                    skillInfo += $"\n(-{skill.baseSkill.battlePointCost} ĐCK)";
                }

                textColor = isInteractable ? Color.white : Color.red;
                skillImage.color = isInteractable ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1.0f);
                break;

            case SkillType.Ultimate:
                // Ultimate (Tuyệt kỹ): Cần đủ năng lượng
                if (battleStats != null)
                {
                    isInteractable = battleStats.CurrentEnergy >= skill.baseSkill.energyCost;
                }

                if (skill.baseSkill.energyCost > 0)
                {
                    skillInfo += $"\n(-{skill.baseSkill.energyCost} NL)";
                }

                textColor = isInteractable ? Color.yellow : Color.red;
                skillImage.color = isInteractable ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1.0f);
                break;

            default:
                isInteractable = true;
                skillImage.color = Color.white;
                break;
        }

        if (btn != null)
        {
            btn.interactable = isInteractable;
        }

        if (textComp != null)
        {
            textComp.text = skillInfo;
            textComp.color = textColor;
        }
    }
}