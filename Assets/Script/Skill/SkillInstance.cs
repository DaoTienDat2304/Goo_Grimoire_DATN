using UnityEngine;

[System.Serializable]
public class SkillInstance
{
    public SkillSO baseSkill;
    public int currentCooldown;
    public float power;

    public SkillInstance(SkillSO so)
    {
        baseSkill = so;
        currentCooldown = 0;
        power = 1f;
    }

    public bool IsReady => currentCooldown <= 0;

}
