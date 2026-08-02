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

        ConfigureTextProperties();

        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    private void ConfigureTextProperties()
    {
        if (mainInfoText != null)
        {
            mainInfoText.fontSize = 24;
            mainInfoText.verticalOverflow = VerticalWrapMode.Overflow;
            mainInfoText.horizontalOverflow = HorizontalWrapMode.Wrap;
            mainInfoText.resizeTextForBestFit = true;
            mainInfoText.resizeTextMinSize = 16;
            mainInfoText.resizeTextMaxSize = 30;
        }
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

        ConfigureTextProperties();

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

        string info = $"[Tên]: {slime.gameObject.name}  |  [Cấp độ]: {(slime.id > 0 ? $"Lv. {slime.id}" : "Lv. 1")}\n";

        if (battleStats != null)
        {
            info += $"[HP]: {battleStats.CurrentHP} / {battleStats.MaxHP}  |  [NL]: {battleStats.CurrentEnergy} / 100\n";
            info += $"[Công ATK]: {battleStats.BattleAttack}  |  [Thủ DEF]: {battleStats.BattleDefense}\n";
            info += $"[Tốc SPD]: {battleStats.BattleSpeed}  |  [Phép MAG]: {battleStats.BattleMagicAttack}\n";
            info += $"[Chí mạng]: {battleStats.BattleCritRate * 100:F1}%  |  [Sát thương CM]: {battleStats.BattleCritDMG * 100:F0}%\n\n";
        }
        else
        {
            info += $"[HP]: {slime.HP} / {slime.MaxHP}\n";
            info += $"[Công ATK]: {slime.Attack}  |  [Thủ DEF]: {slime.Defense}\n";
            info += $"[Tốc SPD]: {slime.Speed}  |  [Phép MAG]: {slime.MagicAttack}\n";
            info += $"[Chí mạng]: {slime.CritRate * 100:F1}%  |  [Sát thương CM]: {slime.CritDMG * 100:F0}%\n\n";
        }

        info += "[KỸ NĂNG TRANG BỊ]:\n";
        string body = GetSkillName(slime.bodySkill);
        string armor = GetSkillName(slime.armorSkill);
        string weapon = GetSkillName(slime.weaponSkill);

        info += $"- Thân (Body): {body}\n";
        info += $"- Giáp (Armor): {armor}\n";
        info += $"- Vũ khí (Weapon): {weapon}";

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

        ConfigureTextProperties();

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
            SkillType.BasicAttack => "Đánh Thường (Basic Attack)",
            SkillType.Active => "Chiến Kỹ (Active Skill)",
            SkillType.Passive => "Nội Tại (Passive Skill)",
            SkillType.Ultimate => "Tuyệt Kỹ (Ultimate Skill)",
            _ => baseSkill.type.ToString()
        };

        if (baseSkill.type == SkillType.Ultimate || baseSkill.energyCost >= 100)
            typeStr = "Tuyệt Kỹ (Ultimate Skill)";

        string costStr;
        if (baseSkill.type == SkillType.Ultimate || baseSkill.energyCost > 0)
            costStr = $"{baseSkill.energyCost} Năng lượng (Energy)";
        else if (baseSkill.battlePointCost > 0)
            costStr = $"{baseSkill.battlePointCost} Điểm Chiến Kỹ (ĐCK)";
        else if (baseSkill.battlePointGain > 0)
            costStr = $"Hồi +{baseSkill.battlePointGain} Điểm Chiến Kỹ (ĐCK)";
        else
            costStr = "Miễn phí";

        string info = $"[Tên Skill]: {sName}\n";
        info += $"[Loại Skill]: {typeStr}\n";
        info += $"[Chi Phí]: {costStr}\n\n";

        info += "[MÔ TẢ CHI TIẾT]:\n";
        info += string.IsNullOrEmpty(baseSkill.description) ? "Không có mô tả chi tiết cho kỹ năng này." : baseSkill.description;

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
