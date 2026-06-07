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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (slime != null)
        {
            if (slime.bodySkill?.baseSkill?.icon != null)
                bodySkill.sprite = slime.bodySkill.baseSkill.icon;
            else bodySkill.sprite = border;

            if (slime.armorSkill?.baseSkill?.icon != null)
                armorSkill.sprite = slime.armorSkill.baseSkill.icon;
            else armorSkill.sprite = border;

            if (slime.weaponSkill?.baseSkill?.icon != null)
                weaponSkill.sprite = slime.weaponSkill.baseSkill.icon;
            else weaponSkill.sprite = border;
        }
    }
}
