using UnityEngine;

public enum EffectType { Damage, Heal, Buff, Debuff, Stun }
public enum BuffStat { Defense, Attack, Speed }

// Bên nào nhận hiệu ứng
public enum TargetSide { Allies = 0, Enemies = 1, All = 2 }

// Hình dạng vùng ảnh hưởng tính từ điểm mốc (anchor)
public enum AoEShape
{
    Single    = 0, // 1 ô (chính anchor)
    Row       = 1, // Cả hàng chứa anchor
    Column    = 2, // Cả cột chứa anchor
    FullSide  = 3, // Toàn bộ 1 bên (bên của TargetSide)
    Everything = 4  // Toàn bộ sân (cả 2 bên)
}

// Điểm mốc xác định tâm của AoE
public enum AnchorType
{
    Self           = 0, // Bản thân caster
    AttackTarget   = 1, // Mục tiêu tấn công hiện tại
    RandomAlly     = 2, // Đồng minh ngẫu nhiên
    RandomEnemy    = 3, // Kẻ địch ngẫu nhiên
    LowestHPAlly   = 4, // Đồng minh ít HP nhất
    LowestHPEnemy  = 5, // Kẻ địch ít HP nhất
    HighestHPAlly  = 6, // Đồng minh nhiều HP nhất
    HighestHPEnemy = 7, // Kẻ địch nhiều HP nhất
    SameRowEnemy   = 8, // Kẻ địch cùng hàng với anchor (dùng với Row shape)

    // Hàng cố định (ally formation — 3 hàng)
    Row0           = 9,  // Hàng đầu (hàng 0)
    Row1           = 10, // Hàng giữa (hàng 1)
    Row2           = 11, // Hàng cuối (hàng 2)

    // Cột cố định (ally formation — 4 cột)
    Col0           = 12, // Cột 1
    Col1           = 13, // Cột 2
    Col2           = 14, // Cột 3
    Col3           = 15, // Cột 4
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
