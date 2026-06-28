using UnityEngine;

public enum EffectType { Damage, Heal, Buff, Debuff, Stun }
public enum BuffStat { Defense, Attack, Speed }

// Bên nào nhận hiệu ứng
public enum TargetSide { Allies = 0, Enemies = 1, All = 2 }

// Hình dạng vùng ảnh hưởng tính từ điểm mốc (anchor)
public enum AoEShape
{
    Single    = 0, // 1 ô (chính anchor)
    Blast     = 1, // Lan (chính anchor + các ô lân cận trong hàng)
    FullSide  = 2  // Toàn bộ 1 bên (allies hoặc enemies)
}

// Điểm mốc xác định tâm của AoE
public enum AnchorType
{
    Self           = 0, // Bản thân caster
    AttackTarget   = 1  // Mục tiêu tấn công hiện tại
}

// Định nghĩa loại hiệu ứng và targeting — dùng chung giữa nhiều skill.
// Giá trị (value, duration, applyChance) được đặt trên EffectEntry trong SkillSO.
[CreateAssetMenu(fileName = "NewEffect", menuName = "SlimeGame/SkillEffect")]
public class SkillEffectSO : ScriptableObject
{
    public EffectType type;
    public TargetSide targetSide;
    public AoEShape aoeShape;
    public AnchorType anchorType;
    public BuffStat buffStat;  // Chỉ dùng khi type == Buff hoặc Debuff
}
