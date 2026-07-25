using UnityEngine;

/// <summary>
/// Hệ số chỉ số Boss theo ĐỘ HIẾM và TỪNG chỉ số — theo bảng "Hệ số chỉ số Boss" (design Adventure).
/// Thay cho hệ số phẳng ×3 cũ. Áp khi 1 slime trở thành enemy/boss trong Adventure battle.
/// (Crit Rate / Crit DMG KHÔNG nhân — giữ như chỉ số gốc.)
/// </summary>
public static class BossStatScaling
{
    public struct Mult { public float hp, atk, magic, def, speed; }

    public static Mult Get(Rarity r)
    {
        switch (r)
        {
            case Rarity.Common:    return M(4.0f, 1.2f, 1.2f, 1.3f, 1.00f);
            case Rarity.Uncommon:  return M(4.5f, 1.3f, 1.3f, 1.4f, 1.05f);
            case Rarity.Rare:      return M(5.2f, 1.4f, 1.4f, 1.5f, 1.10f);
            case Rarity.SuperRare: return M(6.0f, 1.5f, 1.5f, 1.7f, 1.15f);
            case Rarity.UltraRare: return M(7.0f, 1.7f, 1.7f, 1.9f, 1.20f);
            case Rarity.Legendary: return M(8.2f, 1.9f, 1.9f, 2.2f, 1.25f);
            case Rarity.Mythic:    return M(9.5f, 2.2f, 2.2f, 2.5f, 1.30f);
            case Rarity.Secret:    return M(9.5f, 2.2f, 2.2f, 2.5f, 1.30f); // design không nêu → dùng như Mythic
            default:               return M(4.0f, 1.2f, 1.2f, 1.3f, 1.00f);
        }
    }

    private static Mult M(float hp, float atk, float magic, float def, float speed)
        => new Mult { hp = hp, atk = atk, magic = magic, def = def, speed = speed };
}
