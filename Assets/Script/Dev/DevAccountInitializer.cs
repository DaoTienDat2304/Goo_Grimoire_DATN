#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DevAccountInitializer
{
    private static readonly (Rarity rarity, int count, string prefix)[] Distribution =
    {
        (Rarity.Common,    4, "Dev_Common"),
        (Rarity.Uncommon,  4, "Dev_Uncommon"),
        (Rarity.Rare,      4, "Dev_Rare"),
        (Rarity.SuperRare, 4, "Dev_SuperRare"),
        (Rarity.UltraRare, 4, "Dev_UltraRare"),
        (Rarity.Legendary, 4, "Dev_Legendary"),
        (Rarity.Mythic,    4, "Dev_Mythic"),
        (Rarity.Secret,    2, "Dev_Secret"),
    };

    public static bool IsDevAccount()
    {
        var devEmail = RemoteConfigManager.Instance?.DevAccountEmail;
        if (string.IsNullOrEmpty(devEmail)) return false;
        return AuthManager.Instance?.Email == devEmail;
    }

    public static void InitializeDevSlimes()
    {
        if (SlimeGen.Instance == null || BreedingManager.Instance == null)
        {
            Debug.LogError("[DevInit] SlimeGen hoặc BreedingManager chưa sẵn sàng.");
            return;
        }
        Debug.Log("[DevInit] Tài khoản dev — khởi tạo 30 slimes...");
        var devSlimes = BuildDevSlimeList();
        BreedingManager.Instance.SetAllSlimes(devSlimes);
        Debug.Log($"[DevInit] Xong. Đã tạo {devSlimes.Count} slimes.");
    }

    private static List<Slime> BuildDevSlimeList()
    {
        var result = new List<Slime>();
        foreach (var (rarity, count, prefix) in Distribution)
            result.AddRange(CreateSlimesForRarity(rarity, count, prefix));
        return result;
    }

    private static List<Slime> CreateSlimesForRarity(Rarity rarity, int count, string namePrefix)
    {
        var bodyPool = GetBodyTraitsForRarity(rarity);
        if (bodyPool.Count == 0)
        {
            Debug.LogWarning($"[DevInit] Không có body trait cho rarity={rarity}. Bỏ qua.");
            return new List<Slime>();
        }

        var result = new List<Slime>();
        for (int i = 1; i <= count; i++)
        {
            var bodySO   = bodyPool[(i - 1) % bodyPool.Count];
            var armorSO  = GetRandomTraitOfType(TraitType.Armor);
            var weaponSO = GetRandomTraitOfType(TraitType.Weapon);
            if (armorSO == null || weaponSO == null)
            {
                Debug.LogWarning($"[DevInit] Thiếu armor/weapon. Bỏ qua {namePrefix}_{i}.");
                continue;
            }
            var s = new Slime
            {
                slimeName = $"{namePrefix}_{i}",
                body      = bodySO.GenerateInstance(),
                armor     = armorSO.GenerateInstance(),
                weapon    = weaponSO.GenerateInstance()
            };
            s.CalculateStats();
            result.Add(s);
        }
        return result;
    }

    private static List<TraitSO> GetBodyTraitsForRarity(Rarity rarity)
    {
        if (rarity == Rarity.Secret)
        {
            var gen = SlimeGen.Instance;
            var r = gen?.allTraits?
                .Where(t => t != null && t.rarity == Rarity.Secret && t.type == TraitType.Body)
                .ToList() ?? new List<TraitSO>();
            // Fallback: BreedingManager.secret[]
            if (r.Count == 0 && BreedingManager.Instance?.secret != null)
                r = BreedingManager.Instance.secret.Where(t => t != null).ToList();
            return r;
        }

        var traits = SlimeGen.Instance?.allTraits;
        if (traits == null) return new List<TraitSO>();
        return traits.Where(t => t != null && t.type == TraitType.Body && t.rarity == rarity).ToList();
    }

    private static TraitSO GetRandomTraitOfType(TraitType type)
    {
        var pool = SlimeGen.Instance?.allTraits?
            .Where(t => t != null && t.type == type && t.dropRate > 0f)
            .ToList();
        return pool?.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
    }
}
#endif
