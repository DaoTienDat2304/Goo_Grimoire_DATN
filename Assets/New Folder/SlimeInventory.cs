using Spine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlimeInventory : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject breedingPanel;
    public GameObject slimeCollectionPanel;
    public GameObject breedingProgressPanel;
    [Tooltip("(Optional) Anh nen che den ben trong Fusion panel.")]
    public GameObject fusionBackdrop;
    public Button button;
    public GameObject showslot;

    [Header("Breeding UI")]
    public Sprite slotsprite;

    [Header("Collection UI")]
    public Transform collectionGridParent;
    public GameObject collectionSlotPrefab;

    [Header("Slime Counter UI")]
    public Text slimeCounterText;
    public TMP_Text slimeCounterTmpText;
    public GameObject messagePanel;
    public Text messageText;
    [Header("Fusion Filters")]
    [Tooltip("(Optional) Button Rarity ben trai Fusion. Neu de trong script se tu tim object ten Rarity.")]
    public Button rarityFilterButton;
    [Tooltip("(Optional) Text tren button Rarity.")]
    public TMP_Text rarityFilterLabel;
    public Text rarityFilterLegacyLabel;

    private List<GameObject> slimeSlots = new List<GameObject>();
    private List<GameObject> collectionSlots = new List<GameObject>();
    public float interval = 1f;
    public bool panelBreedingActive;
    public int maxsacrifice = 100;
    public int sacrifice;
    public Slider Slider;
    [Tooltip("(Optional) Text hien thi so diem sacrifice current, e.g. 45/100.")]
    public Text sacrificeText;
    public TMP_Text sacrificeTmpText;
    [Tooltip("(Optional) Parent cua cac o slime duoc chon de hien thi ben phai.")]
    public Transform selectedSacrificeGrid;
    [Tooltip("(Optional) Cac o preview slime ben phai. Neu de trong, script se tu lay con cua SelectedSacrificeGrid.")]
    public Image[] selectedSacrificeBodies;
    public TMP_Text selectedSacrificeCounterText;
    [Tooltip("(Optional) Content cua ScrollView hien thi slime duoc chon ben phai.")]
    public RectTransform selectedSacrificeContent;
    [Tooltip("(Optional) Template item trong ScrollView, se duoc clone theo so slime duoc chon.")]
    public GameObject selectedSacrificeItemTemplate;

    [Header("Fusion Cost")]
    [Min(1)]
    [Tooltip("Coin cost for each selected slime. Set this in the Inspector to rebalance Fusion.")]
    [SerializeField] private int coinCostPerSlime = 100;
    [SerializeField] private TMP_Text goldRequirementText;
    [SerializeField] private Text goldRequirementLegacyText;
    [SerializeField] private Button dismantleButton;

    /// <summary>
    /// Common 1 · Uncommon 3 · Rare 5 · SuperRare 15 · UltraRare 30 · Legendary 50 · Mythic 100 · Secret 1.
    /// </summary>
    public static int SacrificePoints(Rarity r)
    {
        switch (r)
        {
            case Rarity.Common:    return 1;
            case Rarity.Uncommon:  return 3;
            case Rarity.Rare:      return 5;
            case Rarity.SuperRare: return 15;
            case Rarity.UltraRare: return 30;
            case Rarity.Legendary: return 50;
            case Rarity.Mythic:    return 100;
            case Rarity.Secret:    return 1;
            default:               return 1;
        }
    }

    private void Awake()
    {
        coinCostPerSlime = Mathf.Max(1, coinCostPerSlime);
        EnsureRuntimeFallbacks();
        EnsureFusionBackdrop();
        EnsureFusionCostUIRefs();
        UpdateFusionCostUI();
    }

    private void Start()
    {
        RefreshAllUI();
        StartCoroutine(Countdown());
    }

    private void OnEnable()
    {
        CurrencyManager.OnCurrencyChanged -= HandleCurrencyChanged;
        CurrencyManager.OnCurrencyChanged += HandleCurrencyChanged;
        EnsureFusionBackdrop();
        SetFusionBackdropVisible(true);
        RefreshAllUI();
        UpdateFusionCostUI();
    }

    private void OnDisable()
    {
        CurrencyManager.OnCurrencyChanged -= HandleCurrencyChanged;
        SetFusionBackdropVisible(false);
    }

    IEnumerator Countdown()
    {
        yield return new WaitForSeconds(1);

        if (BreedingManager.Instance != null)
        {
            var allSlimes = BreedingManager.Instance.GetAllSlimes();
            int slimeCount = allSlimes.Count;

            if (slimeCount == 0)
            {
                yield return new WaitForSeconds(2);
            }
        }

        RefreshAllUI();
    }

    private void Update()
    {
        /*UpdateSlimeCounter();
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0f; // reset
            RefreshCollectionGrid();

            CheckAndRefreshIfNeeded();
        }*/
        if (button != null) button.gameObject.SetActive(sacrifice >= maxsacrifice);

        if (Slider != null)
        {
            float target = Mathf.Min(sacrifice + PreviewPoints(), maxsacrifice);
            Slider.value = Mathf.MoveTowards(Slider.value, target, 120f * Time.deltaTime);

            string value = $"{Mathf.Clamp(Mathf.RoundToInt(Slider.value), 0, maxsacrifice)}/{maxsacrifice}";
            var tmpDisp = GetTmpNumberDisplay();
            if (tmpDisp != null)
                tmpDisp.text = value;
            else
            {
                var disp = GetNumberDisplay();
                if (disp != null) disp.text = value;
            }
        }

        UpdateSelectedSacrificePreview();
    }

    private int PreviewPoints()
    {
        int sum = 0;
        foreach (var slime in selectedSacrificeSlimes)
        {
            if (slime == null) continue;
            sum += SacrificePoints(SelectiveBreeding.GetSlimeRarity(slime));
        }
        return sum;
    }

    private Text cachedNumber;
    private TMP_Text cachedTmpNumber;
    private readonly List<Slime> previewSlimes = new List<Slime>();
    private readonly List<GameObject> selectedPreviewItems = new List<GameObject>();
    private readonly List<Slime> renderedPreviewSlimes = new List<Slime>();
    private readonly HashSet<Slime> selectedSacrificeSlimes = new HashSet<Slime>();
    private int rarityFilterIndex = -1;
    private Rarity? ActiveRarityFilter => rarityFilterIndex >= 0 ? (Rarity?)((Rarity)rarityFilterIndex) : null;

    private Text GetNumberDisplay()
    {
        if (sacrificeText != null) return sacrificeText;
        if (cachedNumber != null) return cachedNumber;
        if (Slider == null) return null;

        var go = new GameObject("SacrificeNumber", typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(Slider.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(90f, 28f);
        rt.anchoredPosition = new Vector2(-5f, 0f);
        rt.localScale = Vector3.one;

        var txt = go.GetComponent<Text>();
        txt.font = FindAnyFont();
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.fontStyle = FontStyle.Bold;
        txt.raycastTarget = false;
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = 6;
        txt.resizeTextMaxSize = 16;

        go.transform.SetAsLastSibling();
        cachedNumber = txt;
        return txt;
    }

    private TMP_Text GetTmpNumberDisplay()
    {
        if (sacrificeTmpText != null) return sacrificeTmpText;
        if (cachedTmpNumber != null) return cachedTmpNumber;
        if (Slider == null) return null;

        var existing = FindChildRecursive(Slider.transform, "SacrificeNumber")?.GetComponent<TMP_Text>();
        if (existing != null)
        {
            cachedTmpNumber = existing;
            return cachedTmpNumber;
        }

        var go = new GameObject("SacrificeNumber", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(Slider.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(90f, 28f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;

        var txt = go.GetComponent<TMP_Text>();
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.fontStyle = FontStyles.Bold;
        txt.raycastTarget = false;
        txt.enableAutoSizing = true;
        txt.fontSizeMin = 6;
        txt.fontSizeMax = 16;

        go.transform.SetAsLastSibling();
        cachedTmpNumber = txt;
        return txt;
    }

    private void UpdateSelectedSacrificePreview()
    {
        EnsureSelectedSacrificePreviewRefs();

        previewSlimes.Clear();
        foreach (var slime in selectedSacrificeSlimes)
            if (slime != null)
                previewSlimes.Add(slime);

        if (selectedSacrificeContent != null && selectedSacrificeItemTemplate != null)
        {
            RebuildSelectedSacrificeScrollItems();
        }
        else if (selectedSacrificeBodies != null && selectedSacrificeBodies.Length > 0)
        {
            for (int i = 0; i < selectedSacrificeBodies.Length; i++)
            {
                var body = selectedSacrificeBodies[i];
                if (body == null) continue;

                var slime = i < previewSlimes.Count ? previewSlimes[i] : null;
                body.gameObject.SetActive(slime != null);
                if (slime != null)
                    body.sprite = GetSlimePreviewSprite(slime);
            }
        }

        if (selectedSacrificeCounterText != null)
            selectedSacrificeCounterText.text = $"SELECTED ({previewSlimes.Count})";

        UpdateFusionCostUI();
    }

    public int GetSelectedSacrificeCoinCost()
    {
        int selectedCount = selectedSacrificeSlimes.Count(slime => slime != null);
        long total = (long)Mathf.Max(0, coinCostPerSlime) * selectedCount;
        return total > int.MaxValue ? int.MaxValue : (int)total;
    }

    private void EnsureFusionCostUIRefs()
    {
        if (goldRequirementText == null && goldRequirementLegacyText == null)
        {
            Transform goldTextTransform = FindChildRecursive(transform, "GoldText");
            if (goldTextTransform != null)
            {
                goldRequirementText = goldTextTransform.GetComponent<TMP_Text>();
                goldRequirementLegacyText = goldTextTransform.GetComponent<Text>();
            }
        }

        if (dismantleButton == null)
            dismantleButton = FindChildRecursive(transform, "DismantleButton")?.GetComponent<Button>();
    }

    private void UpdateFusionCostUI()
    {
        EnsureFusionCostUIRefs();

        int selectedCount = selectedSacrificeSlimes.Count(slime => slime != null);
        int cost = GetSelectedSacrificeCoinCost();
        string value = $"GOLD x{CurrencyAmountFormatter.Format(cost)}";

        if (goldRequirementText != null)
            goldRequirementText.text = value;
        if (goldRequirementLegacyText != null)
            goldRequirementLegacyText.text = value;

        if (dismantleButton != null)
            dismantleButton.interactable = selectedCount > 0;
    }

    private void HandleCurrencyChanged(CurrencyType type, int oldAmount, int newAmount)
    {
        if (type == CurrencyType.Coins)
            UpdateFusionCostUI();
    }

    private void RebuildSelectedSacrificeScrollItems()
    {
        if (SamePreviewSlimes()) return;

        foreach (var item in selectedPreviewItems)
            if (item != null) Destroy(item);
        selectedPreviewItems.Clear();
        renderedPreviewSlimes.Clear();

        selectedSacrificeItemTemplate.SetActive(false);

        foreach (var slime in previewSlimes)
        {
            var item = Instantiate(selectedSacrificeItemTemplate, selectedSacrificeContent);
            item.name = "SelectedSlimeItem";
            item.SetActive(true);

            var body = FindChildRecursive(item.transform, "PreviewBody")?.GetComponent<Image>();
            if (body != null)
            {
                body.gameObject.SetActive(true);
            }

            SetSlimePreviewLayers(item.transform, slime);

            var remove = FindChildRecursive(item.transform, "RemoveButton")?.GetComponent<Button>();
            if (remove != null)
            {
                var selectedSlime = slime;
                remove.onClick.RemoveAllListeners();
                remove.onClick.AddListener(() => DeselectSacrificeSlime(selectedSlime));
            }

            selectedPreviewItems.Add(item);
            renderedPreviewSlimes.Add(slime);
        }
    }

    private bool SamePreviewSlimes()
    {
        if (previewSlimes.Count != renderedPreviewSlimes.Count) return false;
        for (int i = 0; i < previewSlimes.Count; i++)
            if (!ReferenceEquals(previewSlimes[i], renderedPreviewSlimes[i])) return false;
        return true;
    }

    private Sprite GetSlimePreviewSprite(Slime slime)
    {
        return (slime != null && slime.body != null ? slime.body.sprite : null) ?? FindAnyObjectByType<SlimeWorldManager>()?.CreateDefaultSlimeSprite();
    }

    private void SetSlimePreviewLayers(Transform item, Slime slime)
    {
        if (item == null || slime == null) return;

        var body = FindChildRecursive(item, "PreviewBody")?.GetComponent<Image>();
        var armor = FindChildRecursive(item, "PreviewArmor")?.GetComponent<Image>();
        var weapon = FindChildRecursive(item, "PreviewWeapon")?.GetComponent<Image>();
        Sprite fallback = FindAnyObjectByType<SlimeWorldManager>()?.CreateDefaultSlimeSprite();

        if (body != null)
        {
            body.gameObject.SetActive(true);
            body.sprite = (slime.body != null ? slime.body.sprite : null) ?? fallback;
        }

        if (armor != null)
        {
            armor.gameObject.SetActive(slime.armor != null && slime.armor.sprite != null);
            armor.sprite = slime.armor != null ? slime.armor.sprite : null;
        }

        if (weapon != null)
        {
            weapon.gameObject.SetActive(slime.weapon != null && slime.weapon.sprite != null);
            weapon.sprite = slime.weapon != null ? slime.weapon.sprite : null;
        }
    }

    private void EnsureSelectedSacrificePreviewRefs()
    {
        if (selectedSacrificeGrid == null)
        {
            var found = FindChildRecursive(transform, "SelectedSacrificeGrid");
            if (found != null) selectedSacrificeGrid = found;
        }

        if (selectedSacrificeContent == null)
            selectedSacrificeContent = FindChildRecursive(transform, "SelectedSacrificeContent")?.GetComponent<RectTransform>();

        if (selectedSacrificeItemTemplate == null)
        {
            var template = FindChildRecursive(transform, "SelectedSlimeItemTemplate");
            if (template != null) selectedSacrificeItemTemplate = template.gameObject;
        }

        if (selectedSacrificeContent == null && (selectedSacrificeBodies == null || selectedSacrificeBodies.Length == 0) && selectedSacrificeGrid != null)
        {
            var bodies = new List<Image>();
            for (int i = 0; i < selectedSacrificeGrid.childCount; i++)
            {
                var slot = selectedSacrificeGrid.GetChild(i);
                var body = FindChildRecursive(slot, "PreviewBody")?.GetComponent<Image>();
                if (body != null) bodies.Add(body);
            }
            selectedSacrificeBodies = bodies.ToArray();
        }

        if (selectedSacrificeCounterText == null)
            selectedSacrificeCounterText = FindChildRecursive(transform, "SelectedSacrificeCounterText")?.GetComponent<TMP_Text>();
    }

    public void DeselectSacrificeSlime(Slime slime)
    {
        if (slime == null) return;
        selectedSacrificeSlimes.Remove(slime);
        if (collectionSlots == null) return;
        foreach (GameObject inventorySlot in collectionSlots)
        {
            if (inventorySlot == null) continue;
            InventorySlot slot = inventorySlot.GetComponent<InventorySlot>();
            if (slot != null && ReferenceEquals(slot.GetSlime(), slime))
            {
                slot.SetBreedingSelected(false);
                break;
            }
        }
    }

    private Font FindAnyFont()
    {
        foreach (var t in GetComponentsInChildren<Text>(true))
            if (t != null && t.font != null) return t.font;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public void summonbutton()
    {
        if (sacrifice < maxsacrifice)
        {
            Debug.LogWarning($"[Fusion] Not enough energy to summon. Need {maxsacrifice}, have {sacrifice}.", this);
            return;
        }

        BreedingManager breedingManager = BreedingManager.Instance != null
            ? BreedingManager.Instance
            : FindAnyObjectByType<BreedingManager>();
        if (breedingManager == null)
        {
            Debug.LogWarning("[Fusion] BreedingManager is missing; summon canceled.", this);
            return;
        }

        Slime generatedSlime = breedingManager.TryGenerateFusionSlime();
        if (generatedSlime == null)
            return;

        sacrifice = Mathf.Max(0, sacrifice - maxsacrifice);
        if (Slider != null) Slider.value = sacrifice;

        RefreshAllUI();
        if (button != null)
            button.gameObject.SetActive(sacrifice >= maxsacrifice);

        SaveAndLoadSystem.Instance?.MarkSlimeCollectionChanged();
        SaveAndLoadSystem.Instance?.Save();
    }


    public void ondeseclect()
    {
        selectedSacrificeSlimes.Clear();
        foreach (GameObject inventorySlot in collectionSlots)
        {
            if (inventorySlot == null) continue;
            InventorySlot i = inventorySlot.GetComponent<InventorySlot>();
            if (i != null) i.SetBreedingSelected(false);
        }
        RefreshCollectionGrid();
        UpdateSelectedSacrificePreview();
    }
    public void ondelete()
    {
        var selectedSlimes = selectedSacrificeSlimes.Where(slime => slime != null).ToList();

        if (selectedSlimes.Count == 0) return;

        var breedingManager = BreedingManager.Instance != null ? BreedingManager.Instance : FindAnyObjectByType<BreedingManager>();
        if (breedingManager == null)
        {
            Debug.LogWarning("Fusion canceled because BreedingManager is missing.", this);
            return;
        }

        int coinCost = GetSelectedSacrificeCoinCost();
        if (coinCost > 0)
        {
            if (CurrencyManager.Instance == null)
            {
                Debug.LogWarning("Fusion canceled because CurrencyManager is missing.", this);
                return;
            }

            if (!CurrencyManager.Instance.SpendCurrency(CurrencyType.Coins, coinCost))
            {
                UpdateFusionCostUI();
                return;
            }
        }

        selectedSacrificeSlimes.Clear();

        foreach (var slime in selectedSlimes)
            SacrificeSlime(slime, breedingManager);

        if (Slider != null)
            Slider.value = sacrifice;

        // Persist the new collection and partial Fusion energy before rebuilding UI.
        SaveAndLoadSystem.Instance?.MarkSlimeCollectionChanged();
        SaveAndLoadSystem.Instance?.Save();

        RefreshCollectionGrid();
        CheckAndRefreshIfNeeded();
        UpdateSelectedSacrificePreview();
    }

    private void SacrificeSlime(Slime slime, BreedingManager breedingManager)
    {
        if (slime == null || breedingManager == null) return;

        sacrifice += SacrificePoints(SelectiveBreeding.GetSlimeRarity(slime));
        RemoveSacrificedSlimeFromTeams(slime);
        breedingManager.removeslime(slime);
    }

    private void RemoveSacrificedSlimeFromTeams(Slime slime)
    {
        if (slime == null) return;

        Team[] teams = Resources.FindObjectsOfTypeAll<Team>();
        foreach (var team in teams)
        {
            if (team == null || team.team == null) continue;
            if (team.team.Remove(slime))
                slime.isPicked = false;
        }
    }

    private int lastKnownSlimeCount = 0;
    private void CheckAndRefreshIfNeeded()
    {
        if (BreedingManager.Instance != null)
        {
            int currentCount = BreedingManager.Instance.GetAllSlimes().Count;
            if (currentCount != lastKnownSlimeCount)
            {
                lastKnownSlimeCount = currentCount;
                RefreshAllUI();
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name) return child;
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }


    private void EnsureRuntimeFallbacks()
    {
        // Create a basic Canvas and EventSystem if none present
        if (FindAnyObjectByType<Canvas>() == null)
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        var canvasRoot = FindAnyObjectByType<Canvas>().transform;

        // Ensure a grid parents exist if not assigned
     

        // Panels and basic controls if missing
       
        if (slimeCollectionPanel == null)
        {
            slimeCollectionPanel = CreatePanel(canvasRoot, "CollectionPanel");
            collectionGridParent.SetParent(slimeCollectionPanel.transform, false);
        }




        // Create simple runtime slot prefab if none assigned

        // Hook up button events if created at runtime
     

        // Default visible panels
        EnsureRarityFilterButton();
    }

    private void EnsureRarityFilterButton()
    {
        if (rarityFilterButton == null)
            rarityFilterButton = FindChildRecursive(transform, "Rarity")?.GetComponent<Button>();
        if (rarityFilterButton == null)
            rarityFilterButton = FindChildRecursive(transform.root, "Rarity")?.GetComponent<Button>();

        if (rarityFilterButton != null)
        {
            rarityFilterButton.onClick.RemoveListener(OnRarityFilterClicked);
            rarityFilterButton.onClick.AddListener(OnRarityFilterClicked);
            if (rarityFilterLabel == null)
                rarityFilterLabel = rarityFilterButton.GetComponentInChildren<TMP_Text>(true);
            if (rarityFilterLegacyLabel == null)
                rarityFilterLegacyLabel = rarityFilterButton.GetComponentInChildren<Text>(true);
        }

        UpdateRarityFilterLabel();
    }

    public void OnRarityFilterClicked()
    {
        int rarityCount = System.Enum.GetValues(typeof(Rarity)).Length;
        rarityFilterIndex++;
        if (rarityFilterIndex >= rarityCount)
            rarityFilterIndex = -1;

        UpdateRarityFilterLabel();
        RefreshCollectionGrid();
        UpdateSelectedSacrificePreview();
    }

    private void UpdateRarityFilterLabel()
    {
        if (rarityFilterLabel == null && rarityFilterButton != null)
            rarityFilterLabel = rarityFilterButton.GetComponentInChildren<TMP_Text>(true);
        if (rarityFilterLegacyLabel == null && rarityFilterButton != null)
            rarityFilterLegacyLabel = rarityFilterButton.GetComponentInChildren<Text>(true);

        string label = ActiveRarityFilter.HasValue
            ? ActiveRarityFilter.Value.ToVietnamese()
            : "Rarity";
        if (rarityFilterLabel != null)
            rarityFilterLabel.text = label;
        if (rarityFilterLegacyLabel != null)
            rarityFilterLegacyLabel.text = label;
    }

    private void EnsureFusionBackdrop()
    {
        if (fusionBackdrop == null)
        {
            var found = FindChildRecursive(transform, "Fushion");
            if (found != null) fusionBackdrop = found.gameObject;
        }

        if (fusionBackdrop == null) return;

        fusionBackdrop.transform.SetAsFirstSibling();

        foreach (var graphic in fusionBackdrop.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }

    private void SetFusionBackdropVisible(bool visible)
    {
        if (fusionBackdrop == null) return;
        if (fusionBackdrop.activeSelf != visible)
            fusionBackdrop.SetActive(visible);
    }

    private GameObject CreatePanel(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.1f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(10f, 10f);
        rt.offsetMax = new Vector2(-10f, -10f);
        return go;
    }
    public void RefreshAllUI()
    {
        EnsureRarityFilterButton();
        RefreshSlimeGrid();
        RefreshCollectionGrid();
        UpdateSlimeCounter();
    }

    private void RefreshSlimeGrid()
    {
        // Clear existing slots
        foreach (var slot in slimeSlots)
        {
            Destroy(slot);
        }
        slimeSlots.Clear();

        // Get breedable slimes
        if (BreedingManager.Instance == null)
        {
            return;
        }
        var breedableSlimes = BreedingManager.Instance.GetBreedableSlimes();
    }

    private void RefreshCollectionGrid()
    {
        var selectedBeforeRefresh = new HashSet<Slime>(selectedSacrificeSlimes);
        foreach (var oldSlot in collectionSlots)
        {
            if (oldSlot == null) continue;
            var oldInventorySlot = oldSlot.GetComponent<InventorySlot>();
            if (oldInventorySlot != null && oldInventorySlot.onselect && oldInventorySlot.GetSlime() != null)
                selectedBeforeRefresh.Add(oldInventorySlot.GetSlime());
        }

        // Clear existing slots
        foreach (var slot in collectionSlots)
        {
            Destroy(slot);
        }
        collectionSlots.Clear();

        if (BreedingManager.Instance == null)
            return;

        selectedSacrificeSlimes.Clear();
        foreach (var slime in selectedBeforeRefresh)
        {
            if (slime != null && BreedingManager.Instance.GetAllSlimes().Contains(slime))
                selectedSacrificeSlimes.Add(slime);
        }

        var activeRarity = ActiveRarityFilter;
        var allSlimes = BreedingManager.Instance.GetAllSlimes()
            .Where(slime => slime != null && (!activeRarity.HasValue || SelectiveBreeding.GetSlimeRarity(slime) == activeRarity.Value))
            .OrderByDescending(SelectiveBreeding.GetSlimeRarity)
            .ThenBy(slime => slime.slimeName)
            .ToList();

        // Create new slots
        foreach (var slime in allSlimes)
        {
            GameObject slot = Instantiate(collectionSlotPrefab, collectionGridParent);
            var slotScript = slot.GetComponent<InventorySlot>();
            collectionSlots.Add(slot);
            if (slotScript != null)
            {
                slotScript.canselect = true;
                slotScript.sprite = slotsprite;
                slotScript.SetupSlime(slime);
                slotScript.OnSacrificeSelectionChanged -= OnSlotSacrificeSelectionChanged;
                slotScript.OnSacrificeSelectionChanged += OnSlotSacrificeSelectionChanged;
                slotScript.SetBreedingSelected(selectedSacrificeSlimes.Contains(slime));
            }
        }
    }

    private void OnSlotSacrificeSelectionChanged(Slime slime, bool selected)
    {
        if (slime == null) return;

        if (selected)
            selectedSacrificeSlimes.Add(slime);
        else
            selectedSacrificeSlimes.Remove(slime);

        UpdateSelectedSacrificePreview();
    }

    public void ShowCollectionPanel()
    {
        breedingPanel.SetActive(false);
        slimeCollectionPanel.SetActive(true);
        RefreshCollectionGrid();
    }

    private void UpdateSlimeCounter()
    {
        if (BreedingManager.Instance == null) return;

        if (slimeCounterText == null)
            slimeCounterText = FindChildRecursive(transform, "Soluong")?.GetComponent<Text>();
        if (slimeCounterTmpText == null)
            slimeCounterTmpText = FindChildRecursive(transform, "Soluong")?.GetComponent<TMP_Text>();

        int current = BreedingManager.Instance.GetCurrentSlimeCount();
        int max = BreedingManager.Instance.GetMaxSlimeCount();
        string value = $"{current}/{max}";

        if (slimeCounterText != null)
            slimeCounterText.text = value;
        if (slimeCounterTmpText != null)
            slimeCounterTmpText.text = value;
    }

    private void LateUpdate()
    {
        if (BreedingManager.Instance != null)
        {
            int currentCount = BreedingManager.Instance.GetCurrentSlimeCount();
            if (currentCount != lastKnownSlimeCount)
            {
                lastKnownSlimeCount = currentCount;
                UpdateSlimeCounter();
            }
        }
    }


}
