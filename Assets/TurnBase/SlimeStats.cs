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
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(hpbar != null)
        {
            hpbar.value = HP;
        }

        if (HP <= 0)
        {
            skeletonGraphic.color = Color.gray;
            armor.color = Color.gray;
            weapon.color = Color.gray;
        }
    }
}
