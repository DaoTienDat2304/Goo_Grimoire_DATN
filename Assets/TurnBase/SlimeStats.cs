using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

public class SlimeStats : MonoBehaviour
{
    public string slimeName;
    public int HP;
    public int MaxHP;
    public int Attack;
    public int MagicAttack;
    public int Defense;
    public int Speed;
    public float CritRate;
    public float CritDMG;
    public bool isEnemy;
    public Rarity enemyRarity = Rarity.Common;
    public bool useRarityBossScaling = false;
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

    public void SetDeadVisual()
    {
        Color deadColor = new Color(0.25f, 0.25f, 0.25f, 0.65f);

        if (skeletonGraphic != null)
        {
            skeletonGraphic.color = deadColor;
        }

        if (armor != null)
        {
            armor.color = deadColor;
        }

        if (weapon != null)
        {
            weapon.color = deadColor;
        }

        var allImages = GetComponentsInChildren<Image>(true);
        foreach (var img in allImages)
        {
            if (hpbar != null && (img.transform == hpbar.transform || img.transform.IsChildOf(hpbar.transform)))
                continue;
            img.color = deadColor;
        }

        var allSprites = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in allSprites)
        {
            sr.color = deadColor;
        }

        if (turnHalo != null)
        {
            turnHalo.SetActive(false);
        }
    }
}
