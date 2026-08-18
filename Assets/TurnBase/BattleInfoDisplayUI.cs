using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI hiển thị Ô Chữ Thông Tin Dùng Chung trong Trận Đấu (Shared Battle Info Display Panel):
/// - Chạm vào Slime: Hiển thị thông tin Slime + HIỆN NÚT CLOSE (Bấm X hoặc bấm chỗ khác để tắt).
/// - Nhấn giữ Skill: Hiển thị thông tin Skill + ẨN NÚT CLOSE (Thả tay ra tự động tắt).
/// </summary>
public class BattleInfoDisplayUI : MonoBehaviour
{
    private static BattleInfoDisplayUI _instance;
    public static BattleInfoDisplayUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<BattleInfoDisplayUI>();
            }
            return _instance;
        }
    }

    [Header("Shared Info Panel & Text Display")]
    public GameObject infoPanel;               // Panel ô chữ dùng chung
    public Text mainInfoText;                  // Ô Text to dùng chung hiển thị nội dung chi tiết
    public Text headerTitleText;               // Tiêu đề ô thông tin ("-- SLIME --" hoặc "-- SKILL --")
    public Image infoIconImage;                // (Tùy chọn) Icon Avatar Slime hoặc Icon Skill
    public Button closeButton;                 // Nút đóng ô thông tin (Ẩn khi giữ skill, Hiện khi bấm Slime)

    private enum DisplayType { None, SlimeInfo, SkillInfo }
    private DisplayType currentDisplayType = DisplayType.None;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            if (_instance.infoPanel != null && this.infoPanel == null)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(HideInfo);

        if (infoPanel == null && GetComponent<Image>() != null)
        {
            infoPanel = gameObject;
        }

        if (infoPanel != null)
            infoPanel.SetActive(false);
    }



    /// <summary>
    /// Hiển thị thông tin Slime khi người chơi chạm vào Slime (HIỆN nút Close)
    /// </summary>
    public void ShowSlimeInfo(SlimeStats slime)
    {
        if (slime == null) return;
        var battleStats = slime.GetComponent<SlimeBattleStats>();

        currentDisplayType = DisplayType.SlimeInfo;
        GameObject targetPanel = infoPanel != null ? infoPanel : gameObject;
        targetPanel.SetActive(true);

        // HIỆN Nút Close khi xem thông tin Slime
        if (closeButton != null)
            closeButton.gameObject.SetActive(true);



        if (headerTitleText != null)
            headerTitleText.text = $"--- SLIME: {slime.gameObject.name.ToUpper()} ---";

        if (infoIconImage != null)
        {
            var sr = slime.GetComponent<SpriteRenderer>();
            var img = slime.GetComponent<Image>();
            if (sr != null && sr.sprite != null) infoIconImage.sprite = sr.sprite;
            else if (img != null && img.sprite != null) infoIconImage.sprite = img.sprite;
            infoIconImage.gameObject.SetActive(infoIconImage.sprite != null);
        }

        string info = $"[Name]: {slime.gameObject.name}  |  [Level]: {(slime.id > 0 ? $"Lv. {slime.id}" : "Lv. 1")}\n";

        if (battleStats != null)
        {
            info += $"[HP]: {battleStats.CurrentHP} / {battleStats.MaxHP}  |  [Energy]: {battleStats.CurrentEnergy} / 100\n";
            info += $"[ATK]: {battleStats.BattleAttack}  |  [DEF]: {battleStats.BattleDefense}\n";
            info += $"[SPD]: {battleStats.BattleSpeed}  |  [MAG]: {battleStats.BattleMagicAttack}\n";
            info += $"[Crit Rate]: {battleStats.BattleCritRate * 100:F1}%  |  [Crit DMG]: {battleStats.BattleCritDMG * 100:F0}%\n\n";
        }
        else
        {
            info += $"[HP]: {slime.HP} / {slime.MaxHP}\n";
            info += $"[ATK]: {slime.Attack}  |  [DEF]: {slime.Defense}\n";
            info += $"[SPD]: {slime.Speed}  |  [MAG]: {slime.MagicAttack}\n";
            info += $"[Crit Rate]: {slime.CritRate * 100:F1}%  |  [Crit DMG]: {slime.CritDMG * 100:F0}%\n\n";
        }

        info += "[EQUIPPED SKILLS]:\n";
        string body = GetSkillName(slime.bodySkill);
        string armor = GetSkillName(slime.armorSkill);
        string weapon = GetSkillName(slime.weaponSkill);

        info += $"- Body: {body}\n";
        info += $"- Armor: {armor}\n";
        info += $"- Weapon: {weapon}";

        if (mainInfoText != null)
        {
            mainInfoText.text = info;
        }

        Debug.Log($"[BattleInfoDisplayUI] Show Slime Info: {slime.gameObject.name}");
    }

    /// <summary>
    /// Hiển thị thông tin Skill khi người chơi nhấn giữ nút Skill (ẨN nút Close)
    /// </summary>
    public void ShowSkillInfo(SkillInstance skill, SlimeBattleStats battleStats = null)
    {
        if (skill == null || skill.baseSkill == null) return;
        var baseSkill = skill.baseSkill;

        currentDisplayType = DisplayType.SkillInfo;
        GameObject targetPanel = infoPanel != null ? infoPanel : gameObject;
        targetPanel.SetActive(true);

        // ẨN Nút Close khi nhấn giữ Skill (vì thả tay ra bảng tự động ẩn)
        if (closeButton != null)
            closeButton.gameObject.SetActive(false);



        string sName = !string.IsNullOrEmpty(baseSkill.skillName) ? baseSkill.skillName : baseSkill.name;

        if (headerTitleText != null)
            headerTitleText.text = $"--- SKILL: {sName.ToUpper()} ---";

        if (infoIconImage != null)
        {
            infoIconImage.sprite = baseSkill.icon;
            infoIconImage.gameObject.SetActive(baseSkill.icon != null);
        }

        string typeStr = baseSkill.type switch
        {
            SkillType.BasicAttack => "Basic Attack",
            SkillType.Active => "Active Skill",
            SkillType.Passive => "Passive Skill",
            SkillType.Ultimate => "Ultimate Skill",
            _ => baseSkill.type.ToString()
        };

        if (baseSkill.type == SkillType.Ultimate || baseSkill.energyCost >= 100)
            typeStr = "Ultimate Skill";

        string costStr;
        if (baseSkill.type == SkillType.Ultimate || baseSkill.energyCost > 0)
            costStr = $"{baseSkill.energyCost} Energy";
        else if (baseSkill.battlePointCost > 0)
            costStr = $"{baseSkill.battlePointCost} Skill Point(s)";
        else if (baseSkill.battlePointGain > 0)
            costStr = $"Gain +{baseSkill.battlePointGain} Skill Point(s)";
        else
            costStr = "Free";

        string info = $"[Skill Name]: {sName}\n";
        info += $"[Type]: {typeStr}\n";
        info += $"[Cost]: {costStr}\n\n";

        info += "[DESCRIPTION]:\n";
        info += string.IsNullOrEmpty(baseSkill.description) ? "No description available." : baseSkill.description;

        if (mainInfoText != null)
        {
            mainInfoText.text = info;
        }

        Debug.Log($"[BattleInfoDisplayUI] Show Skill Info: {sName}");
    }

    public void HideInfo()
    {
        currentDisplayType = DisplayType.None;
        GameObject targetPanel = infoPanel != null ? infoPanel : gameObject;
        if (targetPanel != null)
            targetPanel.SetActive(false);

        if (closeButton != null)
            closeButton.gameObject.SetActive(true);
    }

    private string GetSkillName(SkillInstance skill)
    {
        if (skill == null || skill.baseSkill == null) return "Trống";
        if (!string.IsNullOrEmpty(skill.baseSkill.skillName)) return skill.baseSkill.skillName;
        if (!string.IsNullOrEmpty(skill.baseSkill.name)) return skill.baseSkill.name;
        return "Không tên";
    }
}
