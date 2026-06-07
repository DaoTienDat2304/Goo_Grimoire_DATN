// ============================================================
// FirebaseAnalyticsManager.cs
//
// Define symbols cần có:
//   FIREBASE_ANALYTICS — bật Firebase Analytics thật
//
// Không có symbol → chỉ Debug.Log, không gửi data lên Firebase.
// Firebase app phải được khởi tạo bởi AuthManager trước khi
// gọi bất kỳ LogEvent nào.
// ============================================================

using UnityEngine;

#if FIREBASE_ANALYTICS
using Firebase.Analytics;
#endif

public class FirebaseAnalyticsManager : MonoBehaviour
{
    public static FirebaseAnalyticsManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    // ── Auth Events ──────────────────────────────────────────

    /// <summary>Bắn khi user đăng nhập thành công.</summary>
    public static void LogLogin(string method)
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLogin,
            new Parameter(FirebaseAnalytics.ParameterMethod, method));
#else
        Debug.Log($"[Analytics] login | method={method}");
#endif
    }

    /// <summary>Bắn khi user đăng ký tài khoản mới thành công.</summary>
    public static void LogSignUp(string method)
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventSignUp,
            new Parameter(FirebaseAnalytics.ParameterMethod, method));
#else
        Debug.Log($"[Analytics] sign_up | method={method}");
#endif
    }

    /// <summary>Bắn khi user đăng xuất.</summary>
    public static void LogLogout()
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.LogEvent("logout");
#else
        Debug.Log("[Analytics] logout");
#endif
    }

    // ── Breeding Events ──────────────────────────────────────

    /// <summary>Bắn khi bắt đầu nhân giống (sau khi trừ coins).</summary>
    public static void LogBreedStart(string parent1Rarity, string parent2Rarity, int costCoins, int slimeCount)
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.LogEvent("breed_start",
            new Parameter("parent1_rarity", parent1Rarity),
            new Parameter("parent2_rarity", parent2Rarity),
            new Parameter("cost_coins",     (long)costCoins),
            new Parameter("slime_count",    (long)slimeCount));
#else
        Debug.Log($"[Analytics] breed_start | p1={parent1Rarity} p2={parent2Rarity} cost={costCoins} count={slimeCount}");
#endif
    }

    /// <summary>Bắn khi nhân giống hoàn tất và slime con được thêm vào collection.</summary>
    public static void LogBreedComplete(string offspringRarity, bool hadMutation, int slimeCount)
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.LogEvent("breed_complete",
            new Parameter("offspring_rarity", offspringRarity),
            new Parameter("had_mutation",      hadMutation ? 1L : 0L),
            new Parameter("slime_count",       (long)slimeCount));
#else
        Debug.Log($"[Analytics] breed_complete | rarity={offspringRarity} mutation={hadMutation} count={slimeCount}");
#endif
    }

    /// <summary>Bắn khi mutation xảy ra — ghi nhận stat nào được tăng.</summary>
    public static void LogBreedMutation(string statBoosted, int bonusAmount)
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.LogEvent("breed_mutation",
            new Parameter("stat_boosted",  statBoosted),
            new Parameter("bonus_amount",  (long)bonusAmount));
#else
        Debug.Log($"[Analytics] breed_mutation | stat={statBoosted} bonus=+{bonusAmount}");
#endif
    }

    // ── Battle Events ────────────────────────────────────────

    /// <summary>Bắn khi trận đấu bắt đầu (user nhấn Start).</summary>
    public static void LogBattleStart(string battleMode, string difficulty, int teamSize)
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.LogEvent("battle_start",
            new Parameter("battle_mode", battleMode),
            new Parameter("difficulty",  difficulty),
            new Parameter("team_size",   (long)teamSize));
#else
        Debug.Log($"[Analytics] battle_start | mode={battleMode} diff={difficulty} team={teamSize}");
#endif
    }

    /// <summary>Bắn khi thắng trận — trước khi xử lý reward.</summary>
    public static void LogBattleWin(string battleMode, string difficulty, int turnsTaken, int coinsEarned)
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.LogEvent("battle_win",
            new Parameter("battle_mode",  battleMode),
            new Parameter("difficulty",   difficulty),
            new Parameter("turns_taken",  (long)turnsTaken),
            new Parameter("coins_earned", (long)coinsEarned));
#else
        Debug.Log($"[Analytics] battle_win | mode={battleMode} diff={difficulty} turns={turnsTaken} coins={coinsEarned}");
#endif
    }

    /// <summary>Bắn khi thua trận.</summary>
    public static void LogBattleLose(string battleMode, string difficulty, int turnsTaken)
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.LogEvent("battle_lose",
            new Parameter("battle_mode", battleMode),
            new Parameter("difficulty",  difficulty),
            new Parameter("turns_taken", (long)turnsTaken));
#else
        Debug.Log($"[Analytics] battle_lose | mode={battleMode} diff={difficulty} turns={turnsTaken}");
#endif
    }

    // ── Shop Events ──────────────────────────────────────────

    /// <summary>Bắn khi user xác nhận mua item trong shop.</summary>
    public static void LogShopPurchase(string currencyType, int price, string resourceGranted, int resourceAmount)
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.LogEvent("shop_purchase",
            new Parameter("currency_type",    currencyType),
            new Parameter("price",            (long)price),
            new Parameter("resource_granted", resourceGranted),
            new Parameter("resource_amount",  (long)resourceAmount));
#else
        Debug.Log($"[Analytics] shop_purchase | currency={currencyType} price={price} resource={resourceGranted} x{resourceAmount}");
#endif
    }

    // ── Quest Events ─────────────────────────────────────────

    /// <summary>Bắn khi quest chuyển từ Locked → Available.</summary>
    public static void LogQuestUnlock(int questId, string questName, string questType)
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.LogEvent("quest_unlock",
            new Parameter("quest_id",   (long)questId),
            new Parameter("quest_name", questName),
            new Parameter("quest_type", questType));
#else
        Debug.Log($"[Analytics] quest_unlock | id={questId} name={questName} type={questType}");
#endif
    }

    /// <summary>Bắn khi quest chuyển sang Completed.</summary>
    public static void LogQuestComplete(int questId, string questName, string questType)
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.LogEvent("quest_complete",
            new Parameter("quest_id",   (long)questId),
            new Parameter("quest_name", questName),
            new Parameter("quest_type", questType));
#else
        Debug.Log($"[Analytics] quest_complete | id={questId} name={questName} type={questType}");
#endif
    }

    // ── Farm Events ──────────────────────────────────────────

    /// <summary>Bắn khi user chọn độ khó farm và bắt đầu trận.</summary>
    public static void LogFarmDifficultySelect(string difficultyName, int difficultyIndex)
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.LogEvent("farm_difficulty_select",
            new Parameter("difficulty_name",  difficultyName),
            new Parameter("difficulty_index", (long)difficultyIndex));
#else
        Debug.Log($"[Analytics] farm_difficulty_select | name={difficultyName} index={difficultyIndex}");
#endif
    }
}
