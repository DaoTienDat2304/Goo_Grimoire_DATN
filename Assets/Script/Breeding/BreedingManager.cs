using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Xsl;

public class BreedingManager : MonoBehaviour
{
    [Header("Breeding Settings")]
    [Tooltip("Fallback if Remote Config not ready")]
    public int maxSlimes = 30;

    private int MaxSlimes => Mathf.Max(1, RemoteBalance.IntOr(RemoteConfigKeys.BreedingMaxSlimes, maxSlimes));
    public WildSlimes wildSlimes;

    [Header("UI References")]
    public Transform breedingPanel;
    public Transform slimeCollectionPanel;

    [SerializeField] private List<Slime> allSlimes = new List<Slime>();
    private Slime selectedSlime1;
    private Slime selectedSlime2;
    public GameObject showslot;

    public class BreedingSession
    {
        public Slime parent1;
        public Slime parent2;
        public Rarity eggRarity;
        public long startUnixMs;
        public float duration;
        public int goldPaid;
    }
    private BreedingSession activeSession;

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

        var ui = FindAnyObjectByType<BreedingUIManager>();
        if (ui != null)
        {
            ui.RefreshAllUI();
        }

    }
    private void Update()
    {
        foreach (var slime in allSlimes)
        {
            if (slime != null)
                slime.UpdateBreedingCooldown(Time.deltaTime);
        }

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
    public int GenTamedSlime()
    {
        if (wildSlimes == null || wildSlimes.tamedSlimes == null)
            return 0;

        if (allSlimes == null)
            allSlimes = new List<Slime>();

        int pendingCount = wildSlimes.tamedSlimes.Count;
        int importedCount = 0;

        foreach (var slimeTraits in wildSlimes.tamedSlimes)
        {
            if (slimeTraits == null
                || slimeTraits.wildSlimeTraits == null
                || slimeTraits.wildSlimeTraits.Length < 3
                || slimeTraits.wildSlimeTraits[0] == null
                || slimeTraits.wildSlimeTraits[1] == null
                || slimeTraits.wildSlimeTraits[2] == null)
            {
                Debug.LogWarning("[Breeding] Skipped invalid tamed slime while converting to owned slime.");
                continue;
            }

            Slime slime = new Slime();
            slime.slimeName = SlimeNameGenerator.GetRandomSlimeName();
            slime.body = slimeTraits.wildSlimeTraits[0].GenerateInstance();
            slime.armor = slimeTraits.wildSlimeTraits[1].GenerateInstance();
            slime.weapon = slimeTraits.wildSlimeTraits[2].GenerateInstance();
            slime.CalculateStats();
            slime.RollRandomSkillsMatchingRarity();
            slime.AssignCompactName();
            allSlimes.Add(slime);
            importedCount++;
        }

        wildSlimes.tamedSlimes.Clear();
        return importedCount;
    }

    public void CreateInitialSlimes()
    {
        if (SlimeGen.Instance == null)
        {
            var slimeGenGO = FindAnyObjectByType<SlimeGen>();
            if (slimeGenGO == null)
            {
                var go = new GameObject("SlimeGen");
                go.AddComponent<SlimeGen>();
            }
        }

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

        if (name.StartsWith("Starter_") || name.StartsWith("Slime_"))
        {
            if (s.body != null)
            {
                s.body.Rarity = Rarity.Common;
                s.body.HP = s.body.baseHP = 600;
                s.body.defense = s.body.baseDefense = 200;
                s.body.speed = s.body.baseSpeed = 80;
                s.body.skill = null;
                s.body.ultimateSkill = null;
            }
            if (s.weapon != null)
            {
                s.weapon.Rarity = Rarity.Common;
                s.weapon.attack = s.weapon.baseAttack = 60;
                s.weapon.magicAttack = s.weapon.baseMagicAttack = 120;
                s.weapon.skill = null;
                s.weapon.ultimateSkill = null;
            }
            if (s.armor != null)
            {
                s.armor.Rarity = Rarity.Common;
                s.armor.critRate = s.armor.baseCritRate = 0.05f;
                s.armor.critDMG = s.armor.baseCritDMG = 1.30f;
                s.armor.skill = null;
                s.armor.ultimateSkill = null;
            }
        }

        s.CalculateStats();
        s.RollRandomSkillsMatchingRarity();
        s.AssignCompactName();
        return s;
    }


    public void GenSpecialSlime()
    {
        Slime generatedSlime = TryGenerateFusionSlime();
        if (generatedSlime != null)
            SaveAndLoadSystem.Instance?.Save();
    }

    public Slime TryGenerateFusionSlime()
    {
        if (!CanBreedMore())
        {
            Debug.LogWarning("[Fusion] Slime collection is full; summon canceled.", this);
            return null;
        }

        TraitSO bodySo = GetRandomFusionBodyTrait();
        TraitSO armorSo = SlimeGen.Instance != null
            ? SlimeGen.Instance.RollTraitOfRarity(TraitType.Armor, Rarity.Secret)
            : null;
        TraitSO weaponSo = SlimeGen.Instance != null
            ? SlimeGen.Instance.RollTraitOfRarity(TraitType.Weapon, Rarity.Secret)
            : null;

        armorSo ??= fixed1Armor != null ? fixed1Armor : fixed2Armor;
        weaponSo ??= fixed1Weapon != null ? fixed1Weapon : fixed2Weapon;

        Slime generatedSlime = GetSpecialSlime(
            SlimeNameGenerator.GetRandomSlimeName(),
            bodySo,
            armorSo,
            weaponSo);
        if (generatedSlime == null)
        {
            Debug.LogError("[Fusion] Cannot create a complete slime. Check Secret body, armor, and weapon traits.", this);
            return null;
        }

        generatedSlime.canBreed = false;
        allSlimes.Add(generatedSlime);

        if (showslot != null)
        {
            showslot.SetActive(true);
            viewslime slotScript = showslot.GetComponentInChildren<viewslime>(true);
            if (slotScript != null)
                slotScript.SetupSlime(generatedSlime);
        }

        PlayerStatsManager.Instance?.RecordSecretObtained(generatedSlime);
        ArchievementManager.Instance?.GetArchivement(1);

        SlimeWorldManager worldManager = SlimeWorldManager.Instance != null
            ? SlimeWorldManager.Instance
            : FindAnyObjectByType<SlimeWorldManager>();
        worldManager?.RefreshWorldSlimes();

        BreedingUIManager breedingUI = FindAnyObjectByType<BreedingUIManager>();
        breedingUI?.RefreshAllUI();

        SaveAndLoadSystem.Instance?.MarkSlimeCollectionChanged();
        return generatedSlime;
    }

    private TraitSO GetRandomFusionBodyTrait()
    {
        if (secret != null)
        {
            TraitSO[] configuredTraits = secret.Where(trait => trait != null).ToArray();
            if (configuredTraits.Length > 0)
                return configuredTraits[Random.Range(0, configuredTraits.Length)];
        }

        if (SlimeGen.Instance != null)
        {
            TraitSO generatedTrait = SlimeGen.Instance.RollTraitOfRarity(TraitType.special, Rarity.Secret);
            if (generatedTrait != null)
                return generatedTrait;
        }

        return fixed1Body != null ? fixed1Body : fixed2Body;
    }

    public Slime GetSpecialSlime(string name, TraitSO body, TraitSO armor, TraitSO weapon)
    {
        if (body == null || armor == null || weapon == null) return null;
        var s = new Slime();
        s.slimeName = name;
        s.body = body.GenerateInstance();
        s.body.Rarity = Rarity.Secret;

        s.armor = armor != null ? armor.GenerateInstance() : null;
        if (s.armor != null) s.armor.Rarity = Rarity.Secret;

        s.weapon = weapon != null ? weapon.GenerateInstance() : null;
        if (s.weapon != null) s.weapon.Rarity = Rarity.Secret;

        s.CalculateStats();
        s.RollRandomSkillsMatchingRarity();
        s.AssignCompactName();
        return s;
    }

    public void removeslime(Slime slime)
    {
        if (slime == null) return;
        if (!allSlimes.Remove(slime)) return;

        SaveAndLoadSystem.Instance?.MarkSlimeCollectionChanged();

        if (SlimeWorldManager.Instance != null)
        {
            SlimeWorldManager.Instance.RefreshWorldSlimes();
        }
        else
        {
            FindAnyObjectByType<SlimeWorldManager>()?.RefreshWorldSlimes();
        }
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
        if (activeSession != null)
        {
            Debug.LogWarning("Another breeding session is running. Please wait.");
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
            Debug.LogWarning("CurrencyManager missing! Khong the breeding.");
            ResetSelection();
            return;
        }

        Rarity eggRarity = SelectiveBreeding.GetEggRarity(selectedSlime1, selectedSlime2);
        int cost = SelectiveBreeding.GetGoldCost(eggRarity);
        float duration = SelectiveBreeding.GetDurationSeconds(eggRarity);

        if (!CurrencyManager.Instance.HasEnoughCurrency(CurrencyType.Coins, cost))
        {
            Debug.LogWarning($"Not enough Gold to breed! Need: {cost}, Have: {CurrencyManager.Instance.GetCurrency(CurrencyType.Coins)}");
            ResetSelection();
            return;
        }

        if (!CurrencyManager.Instance.SpendCurrency(CurrencyType.Coins, cost))
        {
            Debug.LogWarning("Cannot spend Gold. Breeding canceled.");
            ResetSelection();
            return;
        }

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

        var offspring = SelectiveBreeding.GenerateChild(session.parent1, session.parent2, session.eggRarity);
        bool hadMutation = offspring != null && offspring.eggStatQuality == "Mutation";

        UnlockParent(session.parent1);
        UnlockParent(session.parent2);

        if (offspring == null)
        {
            Debug.LogError("[Breeding] Cannot create child slime!");
            var failUi = FindAnyObjectByType<BreedingUIManager>();
            if (failUi != null) failUi.RefreshAllUI();
            return;
        }

        offspring.AssignCompactName();
        allSlimes.Add(offspring);

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
        PlayerStatsManager.Instance?.RecordBreed(offspring);

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
        return allSlimes.Where(s => s.canBreed && !s.breedingLocked && !HasSecretBodyTrait(s)).ToList();
    }
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


    public Rarity PreviewEggRarity(Slime s1, Slime s2) => SelectiveBreeding.GetEggRarity(s1, s2);

    public int PreviewGoldCost(Slime s1, Slime s2) => SelectiveBreeding.GetGoldCost(SelectiveBreeding.GetEggRarity(s1, s2));

    public float PreviewDurationSeconds(Slime s1, Slime s2) => SelectiveBreeding.GetDurationSeconds(SelectiveBreeding.GetEggRarity(s1, s2));

    public Rarity GetActiveEggRarity() => activeSession != null ? activeSession.eggRarity : Rarity.Common;
    public Slime GetActiveParent1() => activeSession?.parent1;
    public Slime GetActiveParent2() => activeSession?.parent2;

    public float GetActiveRemainingSeconds()
    {
        if (activeSession == null) return 0f;
        return Mathf.Max(0f, activeSession.duration - SessionElapsedSeconds());
    }

    public int GetActiveFinishGemCost() => SelectiveBreeding.GetGemCostForRemaining(GetActiveRemainingSeconds());

    public bool FinishActiveWithGems()
    {
        if (activeSession == null) return false;
        int gems = GetActiveFinishGemCost();
        if (CurrencyManager.Instance == null) return false;
        if (gems > 0 && !CurrencyManager.Instance.SpendCurrency(CurrencyType.Gems, gems)) return false;
        CompleteBreeding();
        return true;
    }


    public BreedingSession GetActiveSessionForSave() => activeSession;

    public void RestoreSession(int parent1Id, int parent2Id, Rarity eggRarity, long startUnixMs, float duration, int goldPaid)
    {
        var p1 = allSlimes.FirstOrDefault(s => s != null && s.id == parent1Id);
        var p2 = allSlimes.FirstOrDefault(s => s != null && s.id == parent2Id);
        if (p1 == null || p2 == null) return;

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

