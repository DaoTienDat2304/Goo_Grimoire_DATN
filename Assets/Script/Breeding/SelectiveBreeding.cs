using UnityEngine;

/// <summary>
/// Mục 3 - Lai tạo tự chọn (Breeding-Collection.docx).
/// Chọn 2 Slime bất kỳ để lai. Độ hiếm trứng = độ hiếm cao nhất của cặp.
/// Slime con KHÔNG kế thừa chỉ số trực tiếp: mọi chỉ số được roll trong range của
/// độ hiếm trứng (bảng "Cân bằng chỉ số.xlsx"). Bố mẹ chỉ ảnh hưởng chất lượng roll
/// và tỷ lệ đột biến. Đột biến là cơ chế duy nhất nâng tier (theo từng trait).
/// </summary>
public static class SelectiveBreeding
{
    // ------------------------------------------------------------------
    // 3.1 Chi phí (Gold) và thời gian (giây) theo độ hiếm cao nhất của cặp.
    // ------------------------------------------------------------------
    public struct TierCost { public int gold; public float minutes; }

    public static TierCost GetTierCost(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return new TierCost { gold = 200,   minutes = 1f   };
            case Rarity.Uncommon:  return new TierCost { gold = 600,   minutes = 10f  };
            case Rarity.Rare:      return new TierCost { gold = 2500,  minutes = 25f  };
            case Rarity.SuperRare: return new TierCost { gold = 6000,  minutes = 50f  };
            case Rarity.UltraRare: return new TierCost { gold = 12000, minutes = 90f  };
            case Rarity.Legendary: return new TierCost { gold = 25000, minutes = 120f };
            case Rarity.Mythic:    return new TierCost { gold = 45000, minutes = 240f };
            case Rarity.Secret:    return new TierCost { gold = 45000, minutes = 240f };
            default:               return new TierCost { gold = 200,   minutes = 1f   };
        }
    }

    public static int GetGoldCost(Rarity rarity) => GetTierCost(rarity).gold;
    public static float GetDurationSeconds(Rarity rarity) => GetTierCost(rarity).minutes * 60f;

    // ------------------------------------------------------------------
    // 3.2 Tăng tốc bằng Gem: Gem = thời gian còn lại (phút) × 0.8 (làm tròn lên).
    // Bảng đối chiếu: 10p→8, 25p→20, 50p→40, 90p→72, 120p→96, 240p→192.
    // ------------------------------------------------------------------
    public static int GetGemCostForRemaining(float remainingSeconds)
    {
        float minutes = Mathf.Max(0f, remainingSeconds) / 60f;
        return Mathf.CeilToInt(minutes * 0.8f);
    }

    // ------------------------------------------------------------------
    // 3.3 Tỷ lệ đột biến theo độ hiếm (tính theo TỪNG trait, 3 trait riêng biệt).
    // ------------------------------------------------------------------
    public static float GetMutationRate(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return 0.35f;
            case Rarity.Uncommon:  return 0.30f;
            case Rarity.Rare:      return 0.25f;
            case Rarity.SuperRare: return 0.20f;
            case Rarity.UltraRare: return 0.15f;
            case Rarity.Legendary: return 0.12f;
            case Rarity.Mythic:    return 0.10f;
            default:               return 0.10f;
        }
    }

    public static Rarity NextRarity(Rarity r)
    {
        switch (r)
        {
            case Rarity.Common:    return Rarity.Uncommon;
            case Rarity.Uncommon:  return Rarity.Rare;
            case Rarity.Rare:      return Rarity.SuperRare;
            case Rarity.SuperRare: return Rarity.UltraRare;
            case Rarity.UltraRare: return Rarity.Legendary;
            case Rarity.Legendary: return Rarity.Mythic;
            default:               return Rarity.Mythic; // Mythic là trần
        }
    }

    /// <summary>Độ hiếm tổng thể của 1 slime = trait có độ hiếm cao nhất.</summary>
    public static Rarity GetSlimeRarity(Slime s)
    {
        Rarity best = Rarity.Common;
        if (s == null) return best;
        if (s.body != null && s.body.Rarity > best && s.body.Rarity != Rarity.Secret) best = s.body.Rarity;
        if (s.armor != null && s.armor.Rarity > best && s.armor.Rarity != Rarity.Secret) best = s.armor.Rarity;
        if (s.weapon != null && s.weapon.Rarity > best && s.weapon.Rarity != Rarity.Secret) best = s.weapon.Rarity;
        // Secret vẫn được coi là độ hiếm của chính nó nếu cả 3 đều Secret.
        if (best == Rarity.Common && s.body != null && s.body.Rarity == Rarity.Secret) best = Rarity.Secret;
        return best;
    }

    public static Rarity GetEggRarity(Slime p1, Slime p2)
    {
        Rarity r1 = GetSlimeRarity(p1);
        Rarity r2 = GetSlimeRarity(p2);
        return r1 >= r2 ? r1 : r2;
    }

    // ------------------------------------------------------------------
    // Stat Roll cho lai tạo (mục 3.4.2): Good / Excellent / Perfect / God Roll.
    // Bảng chất lượng cao (Turn Base) — không có Poor/Normal.
    // ------------------------------------------------------------------
    public static string RollBreedingQuality(out float t)
    {
        float r = Random.value * 100f;
        float min, max; string quality;
        if (r < 55f)      { quality = "Good";      min = .40f; max = .60f; }
        else if (r < 83f) { quality = "Excellent"; min = .60f; max = .80f; }
        else if (r < 95f) { quality = "Perfect";   min = .80f; max = .95f; }
        else              { quality = "God Roll";  min = .95f; max = 1f;   }
        t = Random.Range(min, max);
        return quality;
    }

    // Độ mạnh của "thiên lệch roll" khi bố mẹ khác độ hiếm (mục 3.4.3).
    private const float DifferentRarityRollBias = 0.20f;

    /// <summary>
    /// Sinh slime con theo mục 3. eggRarity đã tính = độ hiếm cao nhất của cặp.
    /// </summary>
    public static Slime GenerateChild(Slime parent1, Slime parent2, Rarity eggRarity)
    {
        var child = new Slime
        {
            generation = Mathf.Max(parent1?.generation ?? 0, parent2?.generation ?? 0) + 1,
            parents = new System.Collections.Generic.List<string>
            {
                parent1?.slimeName ?? "?",
                parent2?.slimeName ?? "?"
            },
            happiness = 100f,
            experience = 0,
            canBreed = true,
            breedingCooldown = 0f
        };

        // Mỗi trait roll đột biến độc lập (3.3): nếu đột biến → nâng 1 tier cho trait đó.
        float mutRate = GetMutationRate(eggRarity);
        bool bodyMut   = Random.value < mutRate;
        bool armorMut  = Random.value < mutRate;
        bool weaponMut = Random.value < mutRate;

        Rarity bodyRarity   = bodyMut   ? NextRarity(eggRarity) : eggRarity;
        Rarity armorRarity  = armorMut  ? NextRarity(eggRarity) : eggRarity;
        Rarity weaponRarity = weaponMut ? NextRarity(eggRarity) : eggRarity;

        child.body   = MakeTrait(TraitType.Body,   bodyRarity);
        child.armor  = MakeTrait(TraitType.Armor,  armorRarity);
        child.weapon = MakeTrait(TraitType.Weapon, weaponRarity);

        // 3.4.2 Stat Roll (một quality roll dùng chung cho mọi chỉ số → God Roll đồng đều).
        RollBreedingQuality(out float t);

        // 3.4.3 Khác độ hiếm → thiên lệch roll nhẹ (60% từ slime cao hơn, 40% từ thấp hơn).
        Rarity r1 = GetSlimeRarity(parent1);
        Rarity r2 = GetSlimeRarity(parent2);
        if (r1 != r2)
        {
            Rarity hi = r1 >= r2 ? r1 : r2;
            Rarity lo = r1 >= r2 ? r2 : r1;
            float influence = 0.6f * ((int)hi / 6f) + 0.4f * ((int)lo / 6f);
            t = Mathf.Clamp01(Mathf.Lerp(t, 1f, influence * DifferentRarityRollBias));
        }

        // Áp chỉ số theo range của độ hiếm từng trait (mọi chỉ số nằm trong range).
        ApplyBodyStats(child.body, bodyRarity, t);
        ApplyWeaponStats(child.weapon, weaponRarity, t);
        ApplyArmorStats(child.armor, armorRarity);

        // Bố mẹ không quyết định stat trực tiếp — chỉ ghi nhận chất lượng roll (metadata).
        child.eggStatRollPercent = t * 100f;
        child.eggStatQuality = (bodyMut || armorMut || weaponMut)
            ? "Mutation"
            : DescribeQuality(t);

        child.RollRandomSkillsMatchingRarity(); // skill khớp độ hiếm (đã đột biến) từng trait
        child.CalculateStats();
        return child;
    }

    private static string DescribeQuality(float t)
    {
        if (t >= 0.95f) return "God Roll";
        if (t >= 0.80f) return "Perfect";
        if (t >= 0.60f) return "Excellent";
        return "Good";
    }

    private static TraitInstance MakeTrait(TraitType type, Rarity rarity)
    {
        TraitSO so = SlimeGen.Instance != null ? SlimeGen.Instance.RollTraitOfRarity(type, rarity) : null;
        TraitInstance ti = so != null ? so.GenerateInstance() : null;
        if (ti == null)
        {
            // Fallback: nếu không tìm được TraitSO của rarity này, hạ về eggRarity gốc đã đảm bảo tồn tại.
            return null;
        }
        ti.Rarity = rarity;
        ti.TraitType = type;
        return ti;
    }

    // ---- Áp stat theo bảng cân bằng chính thức (Cân bằng chỉ số.xlsx = bảng GDD) ----
    private static void ApplyBodyStats(TraitInstance body, Rarity rarity, float t)
    {
        if (body == null) return;
        var b = StatBalance.Get(rarity);
        body.HP      = body.baseHP      = LerpInt(b.hpMin,  b.hpMax,  t);
        body.defense = body.baseDefense = LerpInt(b.defMin, b.defMax, t);
        body.speed   = body.baseSpeed   = LerpInt(b.spdMin, b.spdMax, t);
    }

    private static void ApplyWeaponStats(TraitInstance weapon, Rarity rarity, float t)
    {
        if (weapon == null) return;
        var b = StatBalance.Get(rarity);
        weapon.attack      = weapon.baseAttack      = LerpInt(b.atkMin, b.atkMax, t);
        weapon.magicAttack = weapon.baseMagicAttack = LerpInt(b.magMin, b.magMax, t);
    }

    private static void ApplyArmorStats(TraitInstance armor, Rarity rarity)
    {
        if (armor == null) return;
        var b = StatBalance.Get(rarity);
        // Crit theo giá trị điểm của tier (mục 2/xlsx: Crit ở Head/Armor).
        armor.critRate = armor.baseCritRate = b.critRate;
        armor.critDMG  = armor.baseCritDMG  = b.critDmg;
    }

    private static int LerpInt(int min, int max, float t) => Mathf.RoundToInt(Mathf.Lerp(min, max, t));
}
