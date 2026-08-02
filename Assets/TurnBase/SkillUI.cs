using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillUI : MonoBehaviour
{
    public SlimeStats slime;
    public Image bodySkill;
    public Image armorSkill;
    public Image weaponSkill;
    public Image fullSetSkill;
    public Sprite border;

    private void Start()
    {
        EnsureTooltipTriggers();
    }

    private void EnsureTooltipTriggers()
    {
        if (bodySkill != null) AttachTooltipTrigger(bodySkill.gameObject, null, null);
        if (armorSkill != null) AttachTooltipTrigger(armorSkill.gameObject, null, null);
        if (weaponSkill != null) AttachTooltipTrigger(weaponSkill.gameObject, null, null);
    }

    void Update()
    {
        if (slime != null)
        {
            var battleStats = slime.GetComponent<SlimeBattleStats>();

            UpdateSkillUI(bodySkill, slime.bodySkill, battleStats);
            UpdateSkillUI(armorSkill, slime.armorSkill, battleStats);

            SkillInstance weaponSkillToDisplay = slime.weaponSkill;
            if (battleStats != null && slime.weaponUltimateSkill != null && slime.weaponUltimateSkill.baseSkill != null)
            {
                if (battleStats.CurrentEnergy >= slime.weaponUltimateSkill.baseSkill.energyCost)
                {
                    weaponSkillToDisplay = slime.weaponUltimateSkill;
                }
            }

            UpdateSkillUI(weaponSkill, weaponSkillToDisplay, battleStats);
        }
    }

    private void UpdateSkillUI(Image skillImage, SkillInstance skill, SlimeBattleStats battleStats)
    {
        if (skillImage == null) return;

        Button btn = skillImage.GetComponent<Button>();
        if (btn == null) btn = skillImage.GetComponentInParent<Button>();

        GameObject targetGO = (btn != null) ? btn.gameObject : skillImage.gameObject;

        // Đảm bảo bật raycastTarget để nhận sự kiện Nhấn Giữ
        if (skillImage != null) skillImage.raycastTarget = true;
        if (btn != null && btn.image != null) btn.image.raycastTarget = true;

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

            var emptyTrigger = targetGO.GetComponent<SkillTooltipTrigger>();
            if (emptyTrigger != null) emptyTrigger.Setup(null, null);
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
                isInteractable = false;
                skillInfo += "\n(Nội tại)";
                textColor = new Color(0.2f, 0.8f, 1f);
                skillImage.color = Color.white;
                break;

            case SkillType.BasicAttack:
                isInteractable = true;
                if (skill.baseSkill.battlePointGain > 0)
                {
                    skillInfo += $"\n(+{skill.baseSkill.battlePointGain} ĐCK)";
                }
                textColor = Color.green;
                skillImage.color = Color.white;
                break;

            case SkillType.Active:
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

        // Gắn Component SkillTooltipTrigger trực tiếp vào Button và SkillImage để bắt chuẩn sự kiện PointerDown
        AttachTooltipTrigger(targetGO, skill, battleStats);
        if (skillImage != null && skillImage.gameObject != targetGO)
        {
            AttachTooltipTrigger(skillImage.gameObject, skill, battleStats);
        }
    }

    private void AttachTooltipTrigger(GameObject go, SkillInstance skill, SlimeBattleStats battleStats)
    {
        if (go == null) return;

        Button btn = go.GetComponent<Button>();
        if (btn != null && btn.image != null) btn.image.raycastTarget = true;
        Image img = go.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;

        var holdTrigger = go.GetComponent<SkillTooltipTrigger>();
        if (holdTrigger == null) holdTrigger = go.AddComponent<SkillTooltipTrigger>();
        holdTrigger.Setup(skill, battleStats);

        var eventTrigger = go.GetComponent<EventTrigger>();
        if (eventTrigger == null) eventTrigger = go.AddComponent<EventTrigger>();
        eventTrigger.triggers.Clear();

        var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        downEntry.callback.AddListener((data) => {
            holdTrigger.OnPointerDown((PointerEventData)data);
        });
        eventTrigger.triggers.Add(downEntry);

        var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        upEntry.callback.AddListener((data) => {
            holdTrigger.OnPointerUp((PointerEventData)data);
        });
        eventTrigger.triggers.Add(upEntry);
    }
}