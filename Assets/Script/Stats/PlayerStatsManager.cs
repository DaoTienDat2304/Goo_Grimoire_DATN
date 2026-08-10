using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kho đếm tích luỹ (lifetime) toàn cục — nền tảng cho Thành tựu &amp; Nhiệm vụ.
/// Game gốc KHÔNG có bộ đếm lifetime nào (mọi thứ chỉ là trạng thái hiện tại),
/// nên toàn bộ counter ở đây là mới. Bền vững qua GameSaveData (xem SaveAndLoadSystem).
///
/// Singleton tự-sinh &amp; DontDestroyOnLoad để hook đếm được ở MỌI scene
/// (trận đấu / farm / tower chạy ở scene khác scene chính).
/// </summary>
public class PlayerStatsManager : MonoBehaviour
{
    private static PlayerStatsManager _instance;
    public static PlayerStatsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<PlayerStatsManager>();
                if (_instance == null)
                {
                    var go = new GameObject("PlayerStatsManager");
                    _instance = go.AddComponent<PlayerStatsManager>();
                }
            }
            return _instance;
        }
    }

    /// <summary>Bắn mỗi khi một counter đổi giá trị — AchievementService/DailyMission lắng nghe để chấm lại.</summary>
    public static event Action OnStatsChanged;

    // ─── Bộ đếm lifetime ────────────────────────────────────────────────
    public long TotalSlimesBred   { get; private set; }
    public int  TotalFarmWins     { get; private set; }
    public int  TotalCaptures     { get; private set; }
    public int  TotalBattleWins   { get; private set; }
    public int  TotalMutations    { get; private set; }
    public long TotalCoinsEarned  { get; private set; }
    public long TotalGemsEarned   { get; private set; }
    public int  HighestTowerFloor { get; private set; }

    private const int RarityCount = 8; // = số phần tử enum Rarity
    private readonly int[] rarityObtained = new int[RarityCount];      // đếm theo (int)Rarity
    private readonly HashSet<string> traitLedger = new HashSet<string>(); // trait KHÁC NHAU đã-từng-thấy

    // ─── Vòng đời ───────────────────────────────────────────────────────
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        CurrencyManager.OnCurrencyAdded += HandleCurrencyAdded;
    }

    private void OnDisable()
    {
        CurrencyManager.OnCurrencyAdded -= HandleCurrencyAdded;
    }

    // ─── Ghi nhận sự kiện (gọi từ các hệ thống) ─────────────────────────

    /// <summary>Lai tạo xong 1 slime con.</summary>
    public void RecordBreed(Slime offspring)
    {
        TotalSlimesBred++;
        if (offspring != null && offspring.eggStatQuality == "Mutation")
            TotalMutations++;
        RecordSlimeObtained(offspring);
        Changed();
    }

    /// <summary>Nhận được 1 slime Secret (từ GenSpecialSlime).</summary>
    public void RecordSecretObtained(Slime s)
    {
        RecordSlimeObtained(s);
        Changed();
    }

    /// <summary>Bắt được 1 slime hoang ở phiêu lưu (minigame thuần hoá hoặc thắng trận).</summary>
    public void RecordCapture(TraitSO[] traits)
    {
        TotalCaptures++;
        BumpRarity(MaxRarity(traits));
        RecordTraits(traits);
        Changed();
    }

    public void AddBattleWin() { TotalBattleWins++; Changed(); }
    public void AddFarmWin()   { TotalFarmWins++;   Changed(); }

    /// <summary>Ghi nhận tầng tháp cao nhất đạt được (chỉ tăng, không giảm).</summary>
    public void RecordTowerFloor(int floor)
    {
        if (floor > HighestTowerFloor) { HighestTowerFloor = floor; Changed(); }
    }

    private void HandleCurrencyAdded(CurrencyType type, int amount)
    {
        if (amount <= 0) return;
        if (type == CurrencyType.Coins) TotalCoinsEarned += amount;
        else if (type == CurrencyType.Gems) TotalGemsEarned += amount;
        Changed();
    }

    // ─── Truy vấn (Thành tựu đọc ở đây) ─────────────────────────────────
    public int DistinctTraitsCount => traitLedger.Count;
    public int GetRarityObtained(Rarity r)
    {
        int i = (int)r;
        return (i >= 0 && i < RarityCount) ? rarityObtained[i] : 0;
    }

    /// <summary>Tổng số slime từng sở hữu có độ hiếm >= r (ví dụ "Rare trở lên").</summary>
    public int GetRarityObtainedAtLeast(Rarity r)
    {
        int sum = 0;
        for (int i = (int)r; i < RarityCount; i++) sum += rarityObtained[i];
        return sum;
    }

    // ─── Nội bộ ─────────────────────────────────────────────────────────
    private void RecordSlimeObtained(Slime s)
    {
        if (s == null) return;
        BumpRarity(SelectiveBreeding.GetSlimeRarity(s));
        RecordTraitName(s.body?.baseTrait);
        RecordTraitName(s.armor?.baseTrait);
        RecordTraitName(s.weapon?.baseTrait);
    }

    private void BumpRarity(Rarity r)
    {
        int i = (int)r;
        if (i >= 0 && i < RarityCount) rarityObtained[i]++;
    }

    private void RecordTraits(TraitSO[] traits)
    {
        if (traits == null) return;
        foreach (var t in traits) RecordTraitName(t);
    }

    private void RecordTraitName(TraitSO t)
    {
        if (t != null && !string.IsNullOrEmpty(t.traitName))
            traitLedger.Add(t.traitName);
    }

    /// <summary>Độ hiếm tổng thể suy từ mảng trait (mirror SelectiveBreeding.GetSlimeRarity).</summary>
    private static Rarity MaxRarity(TraitSO[] traits)
    {
        Rarity best = Rarity.Common;
        bool anySecret = false;
        if (traits != null)
        {
            foreach (var t in traits)
            {
                if (t == null) continue;
                if (t.rarity == Rarity.Secret) { anySecret = true; continue; }
                if (t.rarity > best) best = t.rarity;
            }
        }
        if (best == Rarity.Common && anySecret) best = Rarity.Secret;
        return best;
    }

    private void Changed() => OnStatsChanged?.Invoke();

    // ─── Persistence (gọi từ SaveAndLoadSystem) ─────────────────────────
    public void WriteTo(GameSaveData data)
    {
        data.totalSlimesBred  = TotalSlimesBred;
        data.totalFarmWins    = TotalFarmWins;
        data.totalCaptures    = TotalCaptures;
        data.totalBattleWins  = TotalBattleWins;
        data.totalMutations   = TotalMutations;
        data.totalCoinsEarned = TotalCoinsEarned;
        data.totalGemsEarned  = TotalGemsEarned;
        data.towerHighestFloorStat = HighestTowerFloor;

        data.rarityObtainedCount = new List<int>(rarityObtained);
        data.unlockedTraitsEver  = new List<string>(traitLedger);
    }

    public void LoadFrom(GameSaveData data)
    {
        if (data == null) return;
        TotalSlimesBred  = data.totalSlimesBred;
        TotalFarmWins    = data.totalFarmWins;
        TotalCaptures    = data.totalCaptures;
        TotalBattleWins  = data.totalBattleWins;
        TotalMutations   = data.totalMutations;
        TotalCoinsEarned = data.totalCoinsEarned;
        TotalGemsEarned  = data.totalGemsEarned;
        HighestTowerFloor = data.towerHighestFloorStat;

        Array.Clear(rarityObtained, 0, RarityCount);
        if (data.rarityObtainedCount != null)
            for (int i = 0; i < data.rarityObtainedCount.Count && i < RarityCount; i++)
                rarityObtained[i] = data.rarityObtainedCount[i];

        traitLedger.Clear();
        if (data.unlockedTraitsEver != null)
            foreach (var n in data.unlockedTraitsEver)
                if (!string.IsNullOrEmpty(n)) traitLedger.Add(n);

        Changed();
    }

    /// <summary>Trả về bản sao read-only của trait ledger — dùng cho Collection Book.</summary>
    public IReadOnlyCollection<string> GetTraitLedger() => traitLedger;


    /// <summary>Gộp thêm trait từ các slime đang sở hữu vào ledger (bootstrap save cũ chưa có ledger).</summary>
    public void MergeOwnedSlimeTraits(IEnumerable<Slime> slimes)
    {
        if (slimes == null) return;
        bool any = false;
        foreach (var s in slimes)
        {
            if (s == null) continue;
            if (s.body?.baseTrait != null   && traitLedger.Add(s.body.baseTrait.traitName))   any = true;
            if (s.armor?.baseTrait != null  && traitLedger.Add(s.armor.baseTrait.traitName))  any = true;
            if (s.weapon?.baseTrait != null && traitLedger.Add(s.weapon.baseTrait.traitName)) any = true;
        }
        if (any) Changed();
    }

    /// <summary>Reset về 0 (tài khoản mới).</summary>
    public void ResetAll()
    {
        TotalSlimesBred = 0;
        TotalFarmWins = TotalCaptures = TotalBattleWins = TotalMutations = 0;
        TotalCoinsEarned = TotalGemsEarned = 0;
        HighestTowerFloor = 0;
        Array.Clear(rarityObtained, 0, RarityCount);
        traitLedger.Clear();
        Changed();
    }
}
