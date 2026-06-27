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
    private bool isBreeding = false;
    private float breedingProgress = 0f;
    public GameObject showslot;

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

        // Cập nhật breeding progress
        if (isBreeding)
        {
            breedingProgress += Time.deltaTime;
            if (breedingProgress >= BreedingTime)
            {
                CompleteBreeding();
            }
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
        if (allSlimes.Count >= MaxSlimes)
        {
            ResetSelection();
            return;
        }

        int cost = BreedingCost;
        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("CurrencyManager không tồn tại! Không thể breeding.");
            ResetSelection();
            return;
        }

        if (!CurrencyManager.Instance.HasEnoughCurrency(CurrencyType.Coins, cost))
        {
            Debug.LogWarning($"Không đủ coins để breeding! Cần: {cost} Coins, Có: {CurrencyManager.Instance.GetCurrency(CurrencyType.Coins)}");
            ResetSelection();
            return;
        }

        bool spentSuccess = CurrencyManager.Instance.SpendCurrency(CurrencyType.Coins, cost);
        if (!spentSuccess)
        {
            Debug.LogWarning("Không thể trừ coins! Breeding bị hủy.");
            ResetSelection();
            return;
        }

        isBreeding = true;
        breedingProgress = 0f;

        FirebaseAnalyticsManager.LogBreedStart(
            selectedSlime1.body?.Rarity.ToString() ?? "Unknown",
            selectedSlime2.body?.Rarity.ToString() ?? "Unknown",
            cost,
            allSlimes.Count);
    }

    private void CompleteBreeding()
    {
        isBreeding = false;
        breedingProgress = 0f;

        // Tạo slime con
        var offspring = new Slime(selectedSlime1, selectedSlime2);
        showslot.SetActive(true);
        var slotScript = showslot.GetComponentInChildren<viewslime>();
        if (slotScript != null)
        {
            slotScript.SetupSlime(offspring);
        }
        // Áp dụng mutation nếu có
        bool hadMutation = Random.Range(0f, 1f) < MutationChance;
        if (hadMutation)
        {
            ApplyMutation(offspring);
        }

        // Đặt tên cho slime con
        offspring.slimeName = $"Slime_{allSlimes.Count+1}";



        // Thêm vào collection
        allSlimes.Add(offspring);

        FirebaseAnalyticsManager.LogBreedComplete(
            offspring.body?.Rarity.ToString() ?? "Unknown",
            hadMutation,
            allSlimes.Count);

        // Bỏ qua breeding cooldown - tất cả slime đều có thể lai tạo ngay lập tức
        // selectedSlime1.breedingCooldown = 30f;
        // selectedSlime1.canBreed = false;
        // selectedSlime2.breedingCooldown = 30f;
        // selectedSlime2.canBreed = false;

        // Thay vào đó, đặt cooldown rất ngắn (1 giây) để tất cả slime đều có thể lai tạo
        selectedSlime1.breedingCooldown = BreedingCooldown;
        selectedSlime1.canBreed = false;
        selectedSlime2.breedingCooldown = BreedingCooldown;
        selectedSlime2.canBreed = false;



        ResetSelection();

        // Cập nhật lại UI sau khi breeding hoàn tất
        var ui = FindAnyObjectByType<BreedingUIManager>();
        if (ui != null)
        {
            ui.RefreshAllUI();
        }

        // Play breeding sound effect when breeding completes
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBreedingSFX();
        }

        // Trigger achievement khi breeding thành công
        if (ArchievementManager.Instance != null)
        {
            ArchievementManager.Instance.GetArchivement(0); // 0 = Breed achievement
        }

        SaveAndLoadSystem.Instance?.Save();
    }

    private void ApplyMutation(Slime slime)
    {
        // Tăng ngẫu nhiên một stat
        int statChoice = Random.Range(0, 5);
        int bonus = Random.Range(1, 4);
        string[] statNames = { "hp", "attack", "defense", "speed", "evade" };

        switch (statChoice)
        {
            case 0: slime.body.HP += bonus; break;
            case 1: slime.body.attack += bonus; break;
            case 2: slime.body.defense += bonus; break;
            case 3: slime.body.speed += bonus; break;
            case 4: slime.body.evade += bonus; break;
        }

        slime.CalculateStats();
        FirebaseAnalyticsManager.LogBreedMutation(statNames[statChoice], bonus);
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
        // Filter ra các slime có thể breeding và không có Secret body trait
        return allSlimes.Where(s => s.canBreed && !HasSecretBodyTrait(s)).ToList();
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
        return breedingProgress / BreedingTime;
    }

    public bool IsBreeding()
    {
        return isBreeding;
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

