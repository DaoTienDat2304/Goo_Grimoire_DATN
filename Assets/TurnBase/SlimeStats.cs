using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

public class SlimeStats : MonoBehaviour
{
    public int HP;
    public int MaxHP;
    public int Attack;
    public int Defense;
    public int Speed;
    public int Evade;
    public bool isEnemy;
    public SkeletonGraphic skeletonGraphic;
    public Image armor;
    public Image weapon;
    public int id;
    public SkillInstance bodySkill;
    public SkillInstance armorSkill;
    public SkillInstance weaponSkill;
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
