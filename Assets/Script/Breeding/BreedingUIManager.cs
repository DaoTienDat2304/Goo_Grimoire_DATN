using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BreedingUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject breedingPanel;
    public GameObject slimeCollectionPanel;
    public GameObject breedingProgressPanel;

    [Header("Breeding UI")]
    public Transform slimeGridParent;
    public GameObject slimeSlotPrefab;
    public Button breedButton;
    public Button cancelButton;
    public Sprite slotsprite;

    [Header("Progress UI")]
    public Slider breedingProgressBar;
    public Text breedingStatusText;
    public Text selectedSlimesText;

    [Header("Collection UI")]
    public Transform collectionGridParent;
    public GameObject collectionSlotPrefab;

    [Header("Slime Counter UI")]
    public Text slimeCounterText;

    private List<GameObject> slimeSlots = new List<GameObject>();
    private List<GameObject> collectionSlots = new List<GameObject>();
    private Slime selectedSlime1;
    private Slime selectedSlime2;
    public float interval = 1f;
    private float timer = 0f;
    private bool currentlyBreeding = false;
    public bool panelBreedingActive;
    private void Awake()
    {
        AutoWireIfNeeded();
        EnsureRuntimeFallbacks();
    }

    private void Start()
    {
        SetupUI();
        RefreshAllUI();
        StartCoroutine(Countdown());
    }

    IEnumerator Countdown()
    {
        yield return new WaitForSeconds(1);

        // Kiểm tra xem BreedingManager đã tạo slimes chưa
        if (BreedingManager.Instance != null)
        {
            var allSlimes = BreedingManager.Instance.GetAllSlimes();
            int slimeCount = allSlimes.Count;

            if (slimeCount == 0)
            {
                yield return new WaitForSeconds(2); // Đợi thêm 2 giây nữa
            }
        }

        RefreshAllUI(); // Refresh UI để đọc slimes đã được tạo sẵn
    }

    private void Update()
    {
        UpdateBreedingProgress();
        UpdateSelectedSlimesText();
        UpdateSlimeCounter(); // Cập nhật counter liên tục
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0f; // reset
            RefreshCollectionGrid();

            // Kiểm tra và refresh nếu có slimes mới được tạo
            CheckAndRefreshIfNeeded();
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

    private void AutoWireIfNeeded()
    {
        // Try find by common names under the active Canvas
        var canvas = FindAnyObjectByType<Canvas>();
        Transform root = canvas != null ? canvas.transform : this.transform;

        if (breedingPanel == null)
        {
            var t = FindChildRecursive(root, "BreedingPanel");
            if (t != null) breedingPanel = t.gameObject;
        }
        if (slimeCollectionPanel == null)
        {
            var t = FindChildRecursive(root, "CollectionPanel");
            if (t != null) slimeCollectionPanel = t.gameObject;
        }
        if (breedingProgressPanel == null)
        {
            var t = FindChildRecursive(root, "BreedingProgressPanel");
            if (t != null) breedingProgressPanel = t.gameObject;
        }

        if (slimeGridParent == null)
        {
            var t = FindChildRecursive(root, "SlimeGridParent");
            if (t != null) slimeGridParent = t;
        }
        if (breedButton == null)
        {
            var t = FindChildRecursive(root, "BreedButton");
            if (t != null) breedButton = t.GetComponent<Button>();
        }
        if (cancelButton == null)
        {
            var t = FindChildRecursive(root, "CancelButton");
            if (t != null) cancelButton = t.GetComponent<Button>();
        }

        if (breedingProgressBar == null)
        {
            var t = FindChildRecursive(root, "BreedingProgressBar");
            if (t == null && breedingProgressPanel != null)
            {
                // Fallback: first Slider under progress panel
                t = breedingProgressPanel.transform.GetComponentInChildren<Slider>(true)?.transform;
            }
            if (t != null) breedingProgressBar = t.GetComponent<Slider>();
        }
        if (breedingStatusText == null)
        {
            var t = FindChildRecursive(root, "BreedingStatusText");
            if (t != null) breedingStatusText = t.GetComponent<Text>();
        }
        if (selectedSlimesText == null)
        {
            var t = FindChildRecursive(root, "SelectedSlimesText");
            if (t != null) selectedSlimesText = t.GetComponent<Text>();
        }

        if (collectionGridParent == null)
        {
            var t = FindChildRecursive(root, "CollectionGridParent");
            if (t != null) collectionGridParent = t;
        }

        if (slimeCounterText == null)
        {
            var t = FindChildRecursive(root, "SlimeCounterText");
            if (t != null) slimeCounterText = t.GetComponent<Text>();
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

    private void SetupUI()
    {
        if (breedButton != null)
            breedButton.onClick.AddListener(OnBreedButtonClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelButtonClicked);

        // Setup breeding progress panel
        if (breedingProgressPanel != null)
            breedingProgressPanel.SetActive(false);
    }

    private void EnsureRuntimeFallbacks()
    {
        // Create a basic Canvas and EventSystem if none present
        if (FindAnyObjectByType<Canvas>() == null)
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        var canvasRoot = FindAnyObjectByType<Canvas>().transform;

        // Ensure a grid parents exist if not assigned
        if (slimeGridParent == null)
        {
            var go = new GameObject("SlimeGridParent", typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(canvasRoot, false);
            slimeGridParent = go.transform;
        }
        if (collectionGridParent == null)
        {
            var go = new GameObject("CollectionGridParent", typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(canvasRoot, false);
            collectionGridParent = go.transform;
        }

        // Panels and basic controls if missing
        if (breedingPanel == null)
        {
            breedingPanel = CreatePanel(canvasRoot, "BreedingPanel");
            slimeGridParent.SetParent(breedingPanel.transform, false);
            if (breedButton == null)
            {
                breedButton = CreateButton(breedingPanel.transform, "BreedButton", "Breed");
            }
            if (cancelButton == null)
            {
                cancelButton = CreateButton(breedingPanel.transform, "CancelButton", "Cancel");
            }
        }

        if (breedingProgressPanel == null)
        {
            breedingProgressPanel = CreatePanel(canvasRoot, "BreedingProgressPanel");
            breedingProgressBar = CreateSlider(breedingProgressPanel.transform, "BreedingProgressBar");
            breedingStatusText = CreateText(breedingProgressPanel.transform, "BreedingStatusText", "Ready to breed!");
        }

        if (slimeCollectionPanel == null)
        {
            slimeCollectionPanel = CreatePanel(canvasRoot, "CollectionPanel");
            collectionGridParent.SetParent(slimeCollectionPanel.transform, false);
        }




        // Create simple runtime slot prefab if none assigned
        if (slimeSlotPrefab == null)
        {
            slimeSlotPrefab = CreateRuntimeSlotPrefab("RuntimeSlimeSlotPrefab");
        }
        if (collectionSlotPrefab == null)
        {
            collectionSlotPrefab = slimeSlotPrefab;
        }

        // Hook up button events if created at runtime
        if (breedButton != null)
        {
            breedButton.onClick.RemoveListener(OnBreedButtonClicked);
            breedButton.onClick.AddListener(OnBreedButtonClicked);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }

        // Default visible panels
        if (breedingPanel != null) breedingPanel.SetActive(true);
        if (breedingProgressPanel != null) breedingProgressPanel.SetActive(false);
    }

    private GameObject CreateRuntimeSlotPrefab(string name)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image));
        var bg = root.GetComponent<Image>();
        bg.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);

        var imageGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        imageGO.transform.SetParent(root.transform, false);

        var nameGO = new GameObject("Name", typeof(RectTransform), typeof(Text));
        nameGO.transform.SetParent(root.transform, false);
        var nameText = nameGO.GetComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        nameText.fontSize = 14;
        nameText.alignment = TextAnchor.UpperLeft;

        var statsGO = new GameObject("Stats", typeof(RectTransform), typeof(Text));
        statsGO.transform.SetParent(root.transform, false);
        var statsText = statsGO.GetComponent<Text>();
        statsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        statsText.fontSize = 12;
        statsText.alignment = TextAnchor.UpperLeft;

        var genGO = new GameObject("Gen", typeof(RectTransform), typeof(Text));
        genGO.transform.SetParent(root.transform, false);
        var genText = genGO.GetComponent<Text>();
        genText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        genText.fontSize = 12;
        genText.alignment = TextAnchor.LowerLeft;

        var statusGO = new GameObject("Status", typeof(RectTransform), typeof(Text));
        statusGO.transform.SetParent(root.transform, false);
        var statusText = statusGO.GetComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        statusText.fontSize = 12;
        statusText.alignment = TextAnchor.LowerRight;

        var borderGO = new GameObject("SelectionBorder", typeof(RectTransform), typeof(Image));
        borderGO.transform.SetParent(root.transform, false);
        borderGO.SetActive(false);

        // Layout positions (simple anchors)
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 100);
        imageGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-70, 0);
        imageGO.GetComponent<RectTransform>().sizeDelta = new Vector2(64, 64);
        nameGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, 30);
        statsGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, 10);
        genGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, -20);
        statusGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(70, -20);

        // Add SlimeSlotUI and wire components
        var slot = root.AddComponent<SlimeSlotUI>();
        slot.nameText = nameText;
        slot.breedingStatusText = statusText;
        slot.backgroundImage = bg;
        slot.selectionBorder = borderGO.GetComponent<Image>();

        return root;
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

    private Button CreateButton(Transform parent, string name, string label)
    {
        var btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);
        var img = btnGO.GetComponent<Image>();
        img.color = new Color(0.2f, 0.5f, 1f, 0.9f);
        var txt = CreateText(btnGO.transform, name + "Text", label);
        txt.alignment = TextAnchor.MiddleCenter;
        var rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(140, 36);
        return btnGO.GetComponent<Button>();
    }

    private Slider CreateSlider(Transform parent, string name)
    {
        var sGO = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sGO.transform.SetParent(parent, false);
        var rt = sGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 20);
        return sGO.GetComponent<Slider>();
    }

    private Text CreateText(Transform parent, string name, string content)
    {
        var tGO = new GameObject(name, typeof(RectTransform), typeof(Text));
        tGO.transform.SetParent(parent, false);
        var t = tGO.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.text = content;
        t.fontSize = 14;
        t.color = Color.white;
        var rt = tGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 24);
        return t;
    }

    public void RefreshAllUI()
    {
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

        GameObject slot1 = Instantiate(slimeSlotPrefab, slimeGridParent);
        var slotScript = slot1.GetComponent<SlimeSlotUI>();
        if (slotScript != null)
        {
            slotScript.SetupSlime(selectedSlime1);
            slotScript.OnSlimeSelected += OnSlimeSelected;
        }
        slimeSlots.Add(slot1);

        GameObject slot2 = Instantiate(slimeSlotPrefab, slimeGridParent);
        var slotScript2 = slot2.GetComponent<SlimeSlotUI>();
        if (slotScript2 != null)
        {
            slotScript2.SetupSlime(selectedSlime2);
            slotScript2.OnSlimeSelected += OnSlimeSelected;
        }
        slimeSlots.Add(slot2);

        UpdateBreedButton();
    }

    private void RefreshCollectionGrid()
    {
        // Clear existing slots
        foreach (var slot in collectionSlots)
        {
            Destroy(slot);
        }
        collectionSlots.Clear();

        if (BreedingManager.Instance == null)
            return;

        // Get all slimes
        var allSlimes = BreedingManager.Instance.GetAllSlimes();

        // Create new slots - filter ra các slime có Secret body trait
        foreach (var slime in allSlimes)
        {
            // Bỏ qua slime có Secret body trait
            if (HasSecretBodyTrait(slime))
            {
                continue;
            }

            GameObject slot = Instantiate(collectionSlotPrefab, collectionGridParent);
            var slotScript = slot.GetComponent<SlimeSlotUI>();
            slotScript.sprite = slotsprite;
            if (slotScript != null)
            {
                slotScript.SetupSlime(slime);
                slotScript.OnSlimeSelected += OnSlimeSelected;
            }
            collectionSlots.Add(slot);
        }
    }

    /// <summary>
    /// Kiểm tra xem slime có trait body với độ hiếm Secret không
    /// </summary>
    private bool HasSecretBodyTrait(Slime slime)
    {
        if (slime == null || slime.body == null) return false;
        return slime.body.Rarity == Rarity.Secret && slime.body.TraitType == TraitType.Body;
    }

    private void OnSlimeSelected(Slime slime)
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
                if (breedButton != null)
                    breedButton.interactable = true;
            }
            else
            {

                ResetSelection();
            }
        }
        UpdateSelectedSlimesText();
        UpdateBreedButton();
    }

    private bool CanBreedSelectedSlimes()
    {
        if (selectedSlime1 == null || selectedSlime2 == null) return false;
        
        // Kiểm tra slime có thể breeding với nhau
        if (!selectedSlime1.CanBreedWith(selectedSlime2)) return false;
        
        // Kiểm tra có đủ coins không
        const int breedingCost = 1; // Chi phí breeding: 1 coins
        if (CurrencyManager.Instance == null) return false;
        if (!CurrencyManager.Instance.HasEnoughCurrency(CurrencyType.Coins, breedingCost)) return false;
        
        return true;
    }

    private void OnBreedButtonClicked()
    {
        // Play button click sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSFX();
        }

        // Kiểm tra giới hạn slime trước khi breeding
        if (BreedingManager.Instance == null || !BreedingManager.Instance.CanBreedMore())
        {
            return;
        }

        if (CanBreedSelectedSlimes())
        {
            BreedingManager.Instance.SelectSlimeForBreeding(selectedSlime1);
            BreedingManager.Instance.SelectSlimeForBreeding(selectedSlime2);

            // Show breeding progress
            if (breedingProgressPanel != null) breedingProgressPanel.SetActive(true);
            if (breedButton != null) breedButton.gameObject.SetActive(false);
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
        }
    }

    private void OnCancelButtonClicked()
    {
        ResetSelection();
        if (breedingProgressPanel != null) breedingProgressPanel.SetActive(false);
        if (breedingPanel != null) breedingPanel.SetActive(true);
    }

    private void ResetSelection()
    {
        selectedSlime1 = null;
        selectedSlime2 = null;
        UpdateSelectedSlimesText();
        UpdateBreedButton();
    }
    private void UpdateBreedingProgress()
    {
        if (BreedingManager.Instance == null)
            return;

        if (BreedingManager.Instance.IsBreeding())
        {
            currentlyBreeding = true;
            float progress = BreedingManager.Instance.GetBreedingProgress();
            if (breedingProgressBar != null)
                breedingProgressBar.value = progress;

            if (breedingStatusText != null)
                breedingStatusText.text = $"Breeding in progress... {(progress * 100):F0}%";
        }
        else
        {
            if (breedingProgressBar != null)
                breedingProgressBar.value = 0f;

            if (breedingStatusText != null)
            {
                if (breedingProgressPanel != null) breedingProgressPanel.SetActive(false);
                if (breedButton != null) breedButton.gameObject.SetActive(true);
                if (cancelButton != null) cancelButton.gameObject.SetActive(true);
            }
            if (currentlyBreeding)
            {
                // Just finished breeding
                ResetSelection();
                currentlyBreeding = false;
            }
        }
    }

    private void UpdateSelectedSlimesText()
    {
        if (selectedSlimesText != null)
        {
            string text = "Selected Slimes:\n";
            if (selectedSlime1 != null)
                text += $"1: {selectedSlime1.slimeName}\n";
            if (selectedSlime2 != null)
                text += $"2: {selectedSlime2.slimeName}";

            selectedSlimesText.text = text;
            RefreshSlimeGrid();
        }
    }

    private void UpdateBreedButton()
    {
        if (breedButton != null)
        {
            // Vô hiệu hóa button nếu không thể breeding hoặc đã đạt giới hạn
            bool canBreed = CanBreedSelectedSlimes() && BreedingManager.Instance != null && BreedingManager.Instance.CanBreedMore();
            breedButton.interactable = canBreed;
        }
    }
    public void ShowBreedingPanel()
    {
        // Play button click sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSFX();
        }

        panelBreedingActive = true;
        if (breedingPanel != null) breedingPanel.SetActive(true);
        if (slimeCollectionPanel != null) slimeCollectionPanel.SetActive(false);
        if (breedingProgressPanel != null) breedingProgressPanel.SetActive(false);
        RefreshSlimeGrid();
    }

    public void ShowCollectionPanel()
    {
        // Play button click sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSFX();
        }

        panelBreedingActive = true;
        if (breedingPanel != null) breedingPanel.SetActive(false);
        if (slimeCollectionPanel != null) slimeCollectionPanel.SetActive(true);
        if (breedingProgressPanel != null) breedingProgressPanel.SetActive(false);
        RefreshCollectionGrid();
    }

    private void UpdateSlimeCounter()
    {
        if (slimeCounterText != null && BreedingManager.Instance != null)
        {
            int current = BreedingManager.Instance.GetCurrentSlimeCount();
            int max = BreedingManager.Instance.GetMaxSlimeCount();
            slimeCounterText.text = $"{current}/{max}";
        }
    }


}
