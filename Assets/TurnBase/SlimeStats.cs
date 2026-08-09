using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

public class SlimeStats : MonoBehaviour
{
    public int HP;
    public int MaxHP;
    public int Attack;
    public int MagicAttack;
    public int Defense;
    public int Speed;
    public float CritRate;
    public float CritDMG;
    public bool isEnemy;
    public Rarity enemyRarity = Rarity.Common;   // độ hiếm boss (để tra hệ số scale theo design)
    public bool useRarityBossScaling = false;    // true = dùng bảng BossStatScaling (Adventure); false = giữ hệ số cũ (Tower)
    public SkeletonGraphic skeletonGraphic;
    public Image armor;
    public Image weapon;
    public int id;
    public SkillInstance bodySkill;
    public SkillInstance armorSkill;
    public SkillInstance weaponSkill;
    public SkillInstance weaponUltimateSkill;
    public SkillInstance specialSkill;
    public Slider hpbar;
    public GameObject turnHalo;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MaxHP = HP;
        if (hpbar != null)
        {
            hpbar.maxValue = MaxHP;
            hpbar.value = HP;
        }
    }

    // Đã xóa Update() polling HP mỗi frame.
    // HP bar được cập nhật trực tiếp khi HP thay đổi (bởi SlimeBattleStats.TakeDamage/Heal).
    // Màu xám khi HP = 0 cũng được xử lý tại điểm gây chết thay vì kiểm tra mỗi frame.
    public void SetDeadVisual()
    {
        if (skeletonGraphic != null) skeletonGraphic.color = Color.gray;
        if (armor != null) armor.color = Color.gray;
        if (weapon != null) weapon.color = Color.gray;
    }
}
