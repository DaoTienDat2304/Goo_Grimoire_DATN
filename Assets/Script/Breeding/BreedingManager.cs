using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Xsl;

public class BreedingManager : MonoBehaviour
{
    [Header("Breeding Settings")]
    [Tooltip("Fallback nếu Remote Config chưa sẵn sàng")]
    public float breedingTime = 5f;
    public float mutationChance = 0.1f;
    public int maxSlimes = 30;

    // Đọc từ Remote Config nếu có, fallback về field trên nếu chưa sẵn sàng
    private float BreedingTime     => RemoteConfigManager.Instance != null ? RemoteConfigManager.Instance.BreedingTime     : breedingTime;
    private float MutationChance   => RemoteConfigManager.Instance != null ? RemoteConfigManager.Instance.MutationChance   : mutationChance;
    private int   MaxSlimes        => RemoteConfigManager.Instance != null ? RemoteConfigManager.Instance.MaxSlimes        : maxSlimes;
    private int   BreedingCost     => RemoteConfigManager.Instance != null ? RemoteConfigManager.Instance.BreedingCost     : 1;
    private float BreedingCooldown => RemoteConfigManager.Instance != null ? RemoteConfigManager.Instance.BreedingCooldown : 2f;
    public WildSlimes wildSlimes;

    [Header("UI References")]
    public Transform breedingPanel;
    public Transform slimeCollectionPanel;

    [SerializeField] private List<Slime> allSlimes = new List<Slime>();
    private Slime selectedSlime1;
    private Slime selectedSlime2;
    public GameObject showslot;

    /// <summary>Một phiên lai tạo đang chạy (mục 3). Chỉ 1 phiên tại một thời điểm.</summary>
    public class BreedingSession
    {
        public Slime parent1;
        public Slime parent2;
        public Rarity eggRarity;
        public long startUnixMs; // mốc bắt đầu theo THỜI GIAN THỰC (chạy nền/offline)
        public float duration;   // tổng thời gian (giây)
        public int goldPaid;
    }
    private BreedingSession activeSession;

    // Đồng hồ thực để lai tạo chạy nền: đóng game rồi mở lại vẫn tính thời gian đã trôi.
    private static long NowUnixMs() => System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private float SessionElapsedSeconds()
        => activeSession == null ? 0f : Mathf.Max(0f, (NowUnixMs() - activeSession.startUnixMs) / 1000f);

    public static BreedingManager Instance { get; private set; }

    [Header("Initial Fixed Slimes")]
    public bool useFixedInitialSlimes = true;
    public TraitSO[] secret;
    public TraitSO fixed1Body;
    public TraitSO fixed1Armor;
    public TraitSO fixed1Weapon;
    public TraitSO fixed2Body;
    public TraitSO fixed2Armor;
    public TraitSO fixed2Weapon;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (allSlimes.Count == 0)
        {
            CreateInitialSlimes();
        }

        // Cập nhật lại UI sau khi đảm bảo có slimes
        var ui = FindAnyObjectByType<BreedingUIManager>();
        if (ui != null)
        {
            ui.RefreshAllUI();
        }

    }
    private void Update()
    {
        // Cập nhật breeding cooldown cho tất cả slime
        for (int i = 0; i < allSlimes.Count; i++)
        {
            allSlimes[i].id = i;
        }
        foreach (var slime in allSlimes)
        {
            slime.UpdateBreedingCooldown(Time.deltaTime);
        }

        // Tiến trình phiên lai tạo (mục 3): tính theo thời gian thực → chạy nền/offline.
        if (activeSession != null && SessionElapsedSeconds() >= activeSession.duration)
        {
            CompleteBreeding();
        }
    }

    public void SetAllSlimes(List<Slime> slimes)
    {
        allSlimes = slimes ?? new List<Slime>();
        var ui = FindAnyObjectByType<BreedingUIManager>();
        if (ui != null) ui.RefreshAllUI();
    }
    public void GenTamedSlime()
    {
        foreach (var slimeTraits in wildSlimes.tamedSlimes)
        {
            Slime slime = new Slime();
            slime.slimeName = slimeTraits.wildSlimeTraits[0].traitName + " " + slimeTraits.wildSlimeTraits[1].traitName + " " + slimeTraits.wildSlimeTraits[2].traitName;
            slime.body = slimeTraits.wildSlimeTraits[0].GenerateInstance();
            slime.armor = slimeTraits.wildSlimeTraits[1].GenerateInstance();
            slime.weapon = slimeTraits.wildSlimeTraits[2].GenerateInstance();
            slime.CalculateStats();
            allSlimes.Add(slime);
        }
        wildSlimes.tamedSlimes.Clear();
    }

    public void CreateInitialSlimes()
    {
        // Bảo đảm có SlimeGen nếu cần fallback random
        if (SlimeGen.Instance == null)
        {
            var slimeGenGO = FindAnyObjectByType<SlimeGen>();
            if (slimeGenGO == null)
            {
                var go = new GameObject("SlimeGen");
                go.AddComponent<SlimeGen>();
            }
        }

        // Nếu đã cấu hình slime cố định, tạo 2 slime theo cấu hình
        if (useFixedInitialSlimes
            && fixed1Body != null && fixed1Armor != null && fixed1Weapon != null
            && fixed2Body != null && fixed2Armor != null && fixed2Weapon != null)
        {
            var s1 = CreateSlimeFromTraits("Starter_1", fixed1Body, fixed1Armor, fixed1Weapon);
            var s2 = CreateSlimeFromTraits("Starter_2", fixed2Body, fixed2Armor, fixed2Weapon);
            if (s1 != null) allSlimes.Add(s1);
            if (s2 != null) allSlimes.Add(s2);
            return;
        }

        // Fallback: random như cũ
        if (SlimeGen.Instance == null) return;
        for (int i = 0; i < 2; i++)
        {
            var newSlime = SlimeGen.Instance.GenerateSlime($"Slime_{i + 1}");
            if (newSlime != null)
            {
                allSlimes.Add(newSlime);
            }
        }
    }
    private Slime CreateSlimeFromTraits(string name, TraitSO body, TraitSO armor, TraitSO weapon)
    {
        if (body == null || armor == null || weapon == null) return null;
        var s = new Slime();
        s.slimeName = name;
        s.body = body.GenerateInstance();
        s.armor = armor.GenerateInstance();
        s.weapon = weapon.GenerateInstance();
        s.CalculateStats();
        return s;
    }


    public void GenSpecialSlime()
    {
        var s1 = GetSpecialSlime("", secret[Random.Range(0,secret.Length)], fixed1Armor, fixed1Weapon);
        showslot.SetActive(true);
        if (s1 != null) allSlimes.Add(s1);
        s1.canBreed = false;
        var slotScript = showslot.GetComponentInChildren<viewslime>();
        if (slotScript != null)
        {
            slotScript.SetupSlime(s1);
        }

        if (ArchievementManager.Instance != null)
        {
            ArchievementManager.Instance.GetArchivement(1); // 0 = Breed achievement
        }
    }

    public Slime GetSpecialSlime(string name, TraitSO body, TraitSO armor, TraitSO weapon)
    {
        if (body == null || armor == null || weapon == null) return null;
        var s = new Slime();
        s.slimeName = name;
        s.body = body.GenerateInstance();
        s.armor = armor.GenerateInstance();
        s.weapon = weapon.GenerateInstance();
        s.CalculateStats();
        return s;
    }

    public void removeslime(Slime slime)
    {
        allSlimes.Remove(slime);
    }

    public void SelectSlimeForBreeding(Slime slime)
    {
        if (selectedSlime1 == null)
        {
            selectedSlime1 = slime;

        }
        else if (selectedSlime2 == null && selectedSlime1 != slime)
        {
            selectedSlime2 = slime;


            // Kiểm tra xem có thể breeding không
            if (CanBreedSelectedSlimes())
            {
                StartBreeding();
            }
            else
            {

                ResetSelection();
            }
        }
    }

    private bool CanBreedSelectedSlimes()
    {
        if (selectedSlime1 == null || selectedSlime2 == null) return false;
        return selectedSlime1.CanBreedWith(selectedSlime2);
    }

    private void StartBreeding()
    {
        // Mục 3: mỗi lần chỉ 1 phiên lai tạo.
        if (activeSession != null)
        {
            Debug.LogWarning("Đang có một phiên lai tạo khác chạy! Chờ hoàn thành.");
            ResetSelection();
            return;
        }

        if (allSlimes.Count >= MaxSlimes)
        {
            ResetSelection();
            return;
        }

        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("CurrencyManager không tồn tại! Không thể breeding.");
            ResetSelection();
            return;
        }

        // Độ hiếm trứng = độ hiếm cao nhất của cặp; chi phí & thời gian theo tier.
        Rarity eggRarity = SelectiveBreeding.GetEggRarity(selectedSlime1, selectedSlime2);
        int cost = SelectiveBreeding.GetGoldCost(eggRarity);
        float duration = SelectiveBreeding.GetDurationSeconds(eggRarity);

        if (!CurrencyManager.Instance.HasEnoughCurrency(CurrencyType.Coins, cost))
        {
            Debug.LogWarning($"Không đủ Gold để lai tạo! Cần: {cost}, Có: {CurrencyManager.Instance.GetCurrency(CurrencyType.Coins)}");
            ResetSelection();
            return;
        }

        if (!CurrencyManager.Instance.SpendCurrency(CurrencyType.Coins, cost))
        {
            Debug.LogWarning("Không thể trừ Gold! Lai tạo bị hủy.");
            ResetSelection();
            return;
        }

        // Khóa cặp bố mẹ cho tới khi hoàn thành.
        selectedSlime1.breedingLocked = true;
        selectedSlime1.canBreed = false;
        selectedSlime2.breedingLocked = true;
        selectedSlime2.canBreed = false;

        activeSession = new BreedingSession
        {
            parent1 = selectedSlime1,
            parent2 = selectedSlime2,
            eggRarity = eggRarity,
            startUnixMs = NowUnixMs(),
            duration = duration,
            goldPaid = cost
        };

        FirebaseAnalyticsManager.LogBreedStart(
            SelectiveBreeding.GetSlimeRarity(selectedSlime1).ToString(),
            SelectiveBreeding.GetSlimeRarity(selectedSlime2).ToString(),
            cost,
            allSlimes.Count);

        ResetSelection();

        var startUi = FindAnyObjectByType<BreedingUIManager>();
        if (startUi != null) startUi.RefreshAllUI();

        SaveAndLoadSystem.Instance?.Save();
    }

    private void CompleteBreeding()
    {
        var session = activeSession;
        activeSession = null;
        if (session == null) return;

        // Sinh slime con theo mục 3 (không kế thừa stat trực tiếp; đột biến theo từng trait).
        var offspring = SelectiveBreeding.GenerateChild(session.parent1, session.parent2, session.eggRarity);
        bool hadMutation = offspring != null && offspring.eggStatQuality == "Mutation";

        // Mở khóa bố mẹ.
        UnlockParent(session.parent1);
        UnlockParent(session.parent2);

        if (offspring == null)
        {
            Debug.LogError("[Breeding] Không sinh được slime con!");
            var failUi = FindAnyObjectByType<BreedingUIManager>();
            if (failUi != null) failUi.RefreshAllUI();
            return;
        }

        offspring.slimeName = $"Slime_{allSlimes.Count + 1}";
        allSlimes.Add(offspring);

        // Hiện slime con ngay trên màn chơi (world).
        var worldManager = FindAnyObjectByType<SlimeWorldManager>();
        if (worldManager != null) worldManager.RefreshWorldSlimes();

        if (showslot != null)
        {
            showslot.SetActive(true);
            var slotScript = showslot.GetComponentInChildren<viewslime>();
            if (slotScript != null) slotScript.SetupSlime(offspring);
        }

        FirebaseAnalyticsManager.LogBreedComplete(
            SelectiveBreeding.GetSlimeRarity(offspring).ToString(),
            hadMutation,
            allSlimes.Count);

        var ui = FindAnyObjectByType<BreedingUIManager>();
        if (ui != null) ui.RefreshAllUI();

        if (AudioManager.Instance != null) AudioManager.Instance.PlayBreedingSFX();
        if (ArchievementManager.Instance != null) ArchievementManager.Instance.GetArchivement(0);

        SaveAndLoadSystem.Instance?.Save();
    }

    private void UnlockParent(Slime s)
    {
        if (s == null) return;
        s.breedingLocked = false;
        s.canBreed = true;
        s.breedingCooldown = 0f;
    }



    private void ResetSelection()
    {
        selectedSlime1 = null;
        selectedSlime2 = null;
    }

    public List<Slime> GetAllSlimes()
    {
        return allSlimes;
    }

    public List<Slime> GetBreedableSlimes()
    {
        // Filter ra các slime có thể breeding, không bị khóa và không có Secret body trait
        return allSlimes.Where(s => s.canBreed && !s.breedingLocked && !HasSecretBodyTrait(s)).ToList();
    }

    /// <summary>
    /// Kiểm tra xem slime có trait body với độ hiếm Secret không
    /// </summary>
    private bool HasSecretBodyTrait(Slime slime)
    {
        if (slime == null || slime.body == null) return false;
        return slime.body.Rarity == Rarity.Secret && slime.body.TraitType == TraitType.Body;
    }

    public float GetBreedingProgress()
    {
        if (activeSession == null || activeSession.duration <= 0f) return 0f;
        return Mathf.Clamp01(SessionElapsedSeconds() / activeSession.duration);
    }

    public bool IsBreeding()
    {
        return activeSession != null;
    }

    // ---------- API cho UI (mục 3) ----------

    /// <summary>Xem trước độ hiếm trứng của một cặp (độ hiếm cao nhất).</summary>
    public Rarity PreviewEggRarity(Slime s1, Slime s2) => SelectiveBreeding.GetEggRarity(s1, s2);

    /// <summary>Chi phí Gold để lai một cặp.</summary>
    public int PreviewGoldCost(Slime s1, Slime s2) => SelectiveBreeding.GetGoldCost(SelectiveBreeding.GetEggRarity(s1, s2));

    /// <summary>Thời gian lai (giây) của một cặp.</summary>
    public float PreviewDurationSeconds(Slime s1, Slime s2) => SelectiveBreeding.GetDurationSeconds(SelectiveBreeding.GetEggRarity(s1, s2));

    public Rarity GetActiveEggRarity() => activeSession != null ? activeSession.eggRarity : Rarity.Common;
    public Slime GetActiveParent1() => activeSession?.parent1;
    public Slime GetActiveParent2() => activeSession?.parent2;

    public float GetActiveRemainingSeconds()
    {
        if (activeSession == null) return 0f;
        return Mathf.Max(0f, activeSession.duration - SessionElapsedSeconds());
    }

    /// <summary>Số Gem cần để hoàn thành ngay phiên đang chạy (mục 3.2).</summary>
    public int GetActiveFinishGemCost() => SelectiveBreeding.GetGemCostForRemaining(GetActiveRemainingSeconds());

    /// <summary>Tăng tốc bằng Gem: trả phí Gem để hoàn thành ngay.</summary>
    public bool FinishActiveWithGems()
    {
        if (activeSession == null) return false;
        int gems = GetActiveFinishGemCost();
        if (CurrencyManager.Instance == null) return false;
        if (gems > 0 && !CurrencyManager.Instance.SpendCurrency(CurrencyType.Gems, gems)) return false;
        CompleteBreeding();
        return true;
    }

    // ---------- Persistence (Save/Load phiên lai tạo) ----------

    public BreedingSession GetActiveSessionForSave() => activeSession;

    /// <summary>Khôi phục phiên lai tạo từ save. parent1/parent2 tra theo id.</summary>
    public void RestoreSession(int parent1Id, int parent2Id, Rarity eggRarity, long startUnixMs, float duration, int goldPaid)
    {
        var p1 = allSlimes.FirstOrDefault(s => s != null && s.id == parent1Id);
        var p2 = allSlimes.FirstOrDefault(s => s != null && s.id == parent2Id);
        if (p1 == null || p2 == null) return; // bố mẹ không còn → bỏ phiên

        p1.breedingLocked = true; p1.canBreed = false;
        p2.breedingLocked = true; p2.canBreed = false;

        activeSession = new BreedingSession
        {
            parent1 = p1,
            parent2 = p2,
            eggRarity = eggRarity,
            startUnixMs = startUnixMs,
            duration = duration,
            goldPaid = goldPaid
        };
    }

    public int GetCurrentSlimeCount()
    {
        return allSlimes.Count;
    }

    public int GetMaxSlimeCount()
    {
        return MaxSlimes;
    }

    public bool CanBreedMore()
    {
        return allSlimes.Count < MaxSlimes;
    }


}

