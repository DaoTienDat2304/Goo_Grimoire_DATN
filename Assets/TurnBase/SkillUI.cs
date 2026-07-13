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
            UpdateSkillUI(bodySkill, slime.bodySkill);
            UpdateSkillUI(armorSkill, slime.armorSkill);
            UpdateSkillUI(weaponSkill, slime.weaponSkill);
        }
    }

    private void UpdateSkillUI(Image skillImage, SkillInstance skill)
    {
        if (skillImage == null) return;

        // Cập nhật Sprite và trạng thái màu sắc hồi chiêu
        if (skill != null && skill.baseSkill != null)
        {
            if (skill.baseSkill.icon != null)
            {
                skillImage.sprite = skill.baseSkill.icon;
            }
            else
            {
                skillImage.sprite = border;
            }
            
            // Làm tối icon nếu đang hồi chiêu
            skillImage.color = skill.currentCooldown > 0 ? new Color(0.4f, 0.4f, 0.4f, 1.0f) : Color.white;
        }
        else
        {
            skillImage.sprite = border;
            skillImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }

        // Cập nhật nhãn Text bên dưới nút để hiển thị tên kỹ năng
        Text textComp = skillImage.GetComponentInChildren<Text>();
        if (textComp == null)
        {
            Button btn = skillImage.GetComponent<Button>();
            if (btn == null) btn = skillImage.GetComponentInParent<Button>();
            if (btn != null) textComp = btn.GetComponentInChildren<Text>();
        }

        if (textComp != null)
        {
            if (skill != null && skill.baseSkill != null)
            {
                if (skill.currentCooldown > 0)
                {
                    textComp.text = $"{skill.baseSkill.skillName}\n(CD: {skill.currentCooldown})";
                    textComp.color = Color.red;
                }
                else
                {
                    textComp.text = $"{skill.baseSkill.skillName}\n(Sẵn sàng)";
                    textComp.color = Color.green;
                }
            }
            else
            {
                textComp.text = "Trống";
                textComp.color = Color.gray;
            }
        }
    }
}
