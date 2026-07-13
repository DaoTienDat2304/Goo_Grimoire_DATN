using UnityEngine;

public enum EffectType { Damage, Heal, Buff, Debuff, Stun, Poison, Bleed, Shield, Cleanse, Dispel, ActionValue, Energy, Revive }

public enum BuffStat { Defense, Attack, Speed, CritRate, CritDMG, MaxHP }

public enum TargetSide { Allies = 0, Enemies = 1, All = 2 }

public enum AoEShape
{
    Single = 0,
    Blast = 1,
    FullSide = 2
}

public enum AnchorType
{
    Self = 0,
    AttackTarget = 1
}

[CreateAssetMenu(fileName = "NewEffect", menuName = "SlimeGame/SkillEffect")]
public class SkillEffectSO : ScriptableObject
{
    public EffectType type;
    public TargetSide targetSide;
    public AoEShape aoeShape;
    public AnchorType anchorType;
    public BuffStat buffStat;
}