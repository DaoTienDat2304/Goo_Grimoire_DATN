using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillTooltipTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum SkillSlotType { Auto, Body, Armor, Weapon }

    [Header("Slot Type Selection")]
    public SkillSlotType slotType = SkillSlotType.Auto;

    public SkillInstance customSkill;

    private SkillInstance currentSkill;
    private SlimeBattleStats currentBattleStats;
    private bool isPointerDown = false;
    private float pointerDownTime = 0f;
    private bool isTooltipShown = false;
    private const float LongPressDuration = 0.5f;

    public void Setup(SkillInstance skill, SlimeBattleStats battleStats)
    {
        currentSkill = skill;
        currentBattleStats = battleStats;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TriggerDown();
    }

    private void OnMouseDown()
    {
        TriggerDown();
    }

    private void TriggerDown()
    {
        isPointerDown = true;
        pointerDownTime = Time.unscaledTime;
        isTooltipShown = false;
        Debug.Log($"[SkillTooltipTrigger] PointerDown trên nút: {gameObject.name}");
    }

    private void Update()
    {
        if (isPointerDown && !isTooltipShown)
        {
            if (Time.unscaledTime - pointerDownTime >= LongPressDuration)
            {
                isTooltipShown = true;
                SkillInstance skillToDisplay = ResolveSkillToDisplay();

                if (skillToDisplay != null && skillToDisplay.baseSkill != null)
                {
                    Debug.Log($"[SkillTooltipTrigger] Bật thông tin Skill: {skillToDisplay.baseSkill.skillName}");
                    if (BattleInfoDisplayUI.Instance != null)
                    {
                        BattleInfoDisplayUI.Instance.ShowSkillInfo(skillToDisplay, currentBattleStats);
                    }
                    else if (SkillTooltipUI.Instance != null)
                    {
                        SkillTooltipUI.Instance.ShowTooltip(skillToDisplay, currentBattleStats);
                    }
                }
                else
                {
                    Debug.LogWarning($"[SkillTooltipTrigger] Nút {gameObject.name} chưa tìm thấy dữ liệu Skill!");
                }
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetPointer();
    }

    private void OnMouseUp()
    {
        ResetPointer();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetPointer();
    }

    private void ResetPointer()
    {
        isPointerDown = false;
        if (isTooltipShown)
        {
            isTooltipShown = false;
            if (BattleInfoDisplayUI.Instance != null)
            {
                BattleInfoDisplayUI.Instance.HideInfo();
            }
            else if (SkillTooltipUI.Instance != null)
            {
                SkillTooltipUI.Instance.HideTooltip();
            }
        }
    }

    private SkillInstance ResolveSkillToDisplay()
    {
        if (customSkill != null && customSkill.baseSkill != null) return customSkill;
        if (currentSkill != null && currentSkill.baseSkill != null) return currentSkill;

        var skillUI = Object.FindFirstObjectByType<SkillUI>();
        if (skillUI != null && skillUI.slime != null)
        {
            currentBattleStats = skillUI.slime.GetComponent<SlimeBattleStats>();

            // Kiểm tra khớp theo SlotType chọn trong Inspector
            if (slotType == SkillSlotType.Body) return skillUI.slime.bodySkill;
            if (slotType == SkillSlotType.Armor) return skillUI.slime.armorSkill;
            if (slotType == SkillSlotType.Weapon)
            {
                if (currentBattleStats != null && skillUI.slime.weaponUltimateSkill != null && skillUI.slime.weaponUltimateSkill.baseSkill != null)
                {
                    if (currentBattleStats.CurrentEnergy >= skillUI.slime.weaponUltimateSkill.baseSkill.energyCost)
                        return skillUI.slime.weaponUltimateSkill;
                }
                return skillUI.slime.weaponSkill;
            }

            // Kiểm tra theo reference Image trên SkillUI
            Image img = GetComponent<Image>();
            if (img == null) img = GetComponentInChildren<Image>();
            Button btn = GetComponent<Button>();
            if (img == null && btn != null) img = btn.image;

            if (img != null)
            {
                if (img == skillUI.bodySkill) return skillUI.slime.bodySkill;
                if (img == skillUI.armorSkill) return skillUI.slime.armorSkill;
                if (img == skillUI.weaponSkill)
                {
                    if (currentBattleStats != null && skillUI.slime.weaponUltimateSkill != null && skillUI.slime.weaponUltimateSkill.baseSkill != null)
                    {
                        if (currentBattleStats.CurrentEnergy >= skillUI.slime.weaponUltimateSkill.baseSkill.energyCost)
                            return skillUI.slime.weaponUltimateSkill;
                    }
                    return skillUI.slime.weaponSkill;
                }
            }

            // Fallback tên nút
            string nameLower = gameObject.name.ToLower();
            if (nameLower.Contains("body") || nameLower.Contains("thân")) return skillUI.slime.bodySkill;
            if (nameLower.Contains("armor") || nameLower.Contains("giáp")) return skillUI.slime.armorSkill;
            if (nameLower.Contains("weapon") || nameLower.Contains("vũ khí") || nameLower.Contains("kiếm"))
            {
                if (currentBattleStats != null && skillUI.slime.weaponUltimateSkill != null && skillUI.slime.weaponUltimateSkill.baseSkill != null)
                {
                    if (currentBattleStats.CurrentEnergy >= skillUI.slime.weaponUltimateSkill.baseSkill.energyCost)
                        return skillUI.slime.weaponUltimateSkill;
                }
                return skillUI.slime.weaponSkill;
            }
        }

        return currentSkill;
    }
}
