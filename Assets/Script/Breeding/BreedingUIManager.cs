using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BreedingUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject breedingUIRoot;
    public GameObject breedingPanel;
    public GameObject slimeCollectionPanel;
    public GameObject breedingProgressPanel;
    public Button closeButton;

    [Header("Breeding UI")]
    public Transform slimeGridParent;
    public GameObject slimeSlotPrefab;
    public Button breedButton;
    public Button cancelButton;
    public Sprite slotsprite;
    public Image selectedSlime1Image;
    public Image selectedSlime2Image;
    public Image selectedSlime1Body;
    public Image selectedSlime1Armor;
    public Image selectedSlime1Weapon;
    public Image selectedSlime2Body;
    public Image selectedSlime2Armor;
    public Image selectedSlime2Weapon;
    public TMP_Text mutationPercentText;
    public TMP_Text energyCostText;

    [Header("Progress UI")]
    public Slider breedingProgressBar;
    public Text breedingStatusText;
    public Text selectedSlimesText;
    [Tooltip("Tom tat cap dang chon: rarity, chi phi va thoi gian du kien.")]
    public Text breedingPreviewText;

    [Header("Runtime Safety")]
    [Tooltip("Chi bat khi scene khong co UI duoc gan san. Tat trong firstsave de tranh tu sinh object long xong.")]
    public bool createMissingUIAtRuntime;

    [Header("Chi phí lai — kéo vào Inspector cho khớp UI PNG (màn CHỌN slime)")]
    [Tooltip("Icon coin của bạn, hiện cạnh số Gold. Chỉ bật khi đã chọn đủ 2 slime.")]
    public Image costCoinIcon;
    [Tooltip("Text hiển thị SỐ Gold cần để lai cặp đang chọn (đặt cạnh icon coin).")]
    public Text breedingCostText;

    [Header("Tăng tốc bằng Gem — kéo vào Inspector (màn CHỜ đếm ngược)")]
    [Tooltip("Nút bấm để tốn Gem hoàn thành lai ngay.")]
    public Button finishWithGemsButton;
    [Tooltip("Icon gem của bạn, hiện trên/cạnh nút tăng tốc.")]
    public Image gemIcon;
    [Tooltip("Text hiển thị SỐ Gem cần để hoàn thành ngay.")]
    public Text gemCostText;

    [Header("Collection UI")]
    public Transform collectionGridParent;
    public GameObject collectionSlotPrefab;
    public Button previousPageButton;
    public Button nextPageButton;
    public Image[] pageDots;
    public Sprite activePageDotSprite;
    public Sprite inactivePageDotSprite;
    [Min(1)] public int collectionPageSize = 9;

    [Header("Slime Counter UI")]
    public Text slimeCounterText;

    private List<GameObject> slimeSlots = new List<GameObject>();
    private List<GameObject> collectionSlots = new List<GameObject>();
    private Slime selectedSlime1;
    private Slime selectedSlime2;
    public float interval = 1f;
    private float timer = 0f;
    private bool currentlyBreeding = false;
    private int currentCollectionPage;
    public bool panelBreedingActive;
    private void Awake()
    {
        AutoWireIfNeeded();
        if (createMissingUIAtRuntime)
            EnsureRuntimeFallbacks();
    }

    private void Start()
    {
        SetupUI();
        RefreshAllUI();
        StartCoroutine(Countdown());
    }

    private void OnEnable()
    {
        // Mở lại UI breeding (vd sau khi ra world rồi vào lại): dựng lại grid slime (kẻo
        // mất hình) và hiện ngay đúng màn theo trạng thái (đếm ngược nếu đang lai).
        RefreshAllUI();
        UpdateBreedingProgress();
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
        if (selectedSlime1Image == null)
            selectedSlime1Image = FindChildRecursive(root, "Slime1")?.GetComponent<Image>();
        if (selectedSlime2Image == null)
            selectedSlime2Image = FindChildRecursive(root, "Slime2")?.GetComponent<Image>();
        AutoWireSelectedSlimeLayers(root, "Slime1", ref selectedSlime1Body, ref selectedSlime1Armor, ref selectedSlime1Weapon);
        AutoWireSelectedSlimeLayers(root, "Slime2", ref selectedSlime2Body, ref selectedSlime2Armor, ref selectedSlime2Weapon);
        if (mutationPercentText == null)
            mutationPercentText = FindChildRecursive(root, "SoPhanTram")?.GetComponent<TMP_Text>();
        if (energyCostText == null)
            energyCostText = FindChildRecursive(root, "SoNangLuong")?.GetComponent<TMP_Text>();

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
            if (t == null) t = FindChildRecursive(root, "CollectionGrid");
            if (t != null) collectionGridParent = t;
        }
        if (previousPageButton == null)
            previousPageButton = FindChildRecursive(root, "PreviousPageButton")?.GetComponent<Button>();
        if (nextPageButton == null)
            nextPageButton = FindChildRecursive(root, "NextPageButton")?.GetComponent<Button>();
        if (pageDots == null || pageDots.Length == 0)
        {
            var dots = new List<Image>();
            for (int i = 1; i <= 6; i++)
            {
                Image dot = FindChildRecursive(root, "PageDot_" + i)?.GetComponent<Image>();
                if (dot != null) dots.Add(dot);
            }
            pageDots = dots.ToArray();
            if (pageDots.Length > 0 && activePageDotSprite == null) activePageDotSprite = pageDots[0].sprite;
            if (pageDots.Length > 1 && inactivePageDotSprite == null) inactivePageDotSprite = pageDots[1].sprite;
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
        // UI chi phí Gold + nút Gem là các object THẬT trong scene (dựng bằng Editor tool
        // "Tools/Goo Grimoire/Build Breeding Cost + Gem UI" để kéo/chỉnh được). Code KHÔNG
        // tự tạo lúc chạy nữa (tránh lệch & không sửa được). Chỉ điều khiển hiện/ẩn + số liệu.

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
        if (previousPageButton != null)
        {
            previousPageButton.onClick.RemoveListener(ShowPreviousCollectionPage);
            previousPageButton.onClick.AddListener(ShowPreviousCollectionPage);
        }
        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveListener(ShowNextCollectionPage);
            nextPageButton.onClick.AddListener(ShowNextCollectionPage);
        }

        if (finishWithGemsButton != null)
        {
            finishWithGemsButton.onClick.RemoveListener(OnFinishWithGemsClicked);
            finishWithGemsButton.onClick.AddListener(OnFinishWithGemsClicked);
            finishWithGemsButton.gameObject.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HideBreedingUI);
            closeButton.onClick.AddListener(HideBreedingUI);
        }

        // Setup breeding progress panel
        if (breedingProgressPanel != null)
            breedingProgressPanel.SetActive(false);
    }

    public void HideBreedingUI()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClickSFX();
        panelBreedingActive = false;
        SlimeWorldManager worldManager = FindFirstObjectByType<SlimeWorldManager>();
        if (worldManager != null)
            worldManager.StartWorldView();
        else if (breedingUIRoot != null)
            breedingUIRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void OnFinishWithGemsClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClickSFX();
        if (BreedingManager.Instance != null) BreedingManager.Instance.FinishActiveWithGems();
    }

    /// <summary>Định dạng giây → mm:ss (hoặc hh:mm:ss nếu ≥ 1 giờ).</summary>
    private static string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int h = total / 3600;
        int m = (total % 3600) / 60;
        int s = total % 60;
        return h > 0 ? $"{h:00}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
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
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 14;
        nameText.alignment = TextAnchor.UpperLeft;

        var statsGO = new GameObject("Stats", typeof(RectTransform), typeof(Text));
        statsGO.transform.SetParent(root.transform, false);
        var statsText = statsGO.GetComponent<Text>();
        statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statsText.fontSize = 12;
        statsText.alignment = TextAnchor.UpperLeft;

        var genGO = new GameObject("Gen", typeof(RectTransform), typeof(Text));
        genGO.transform.SetParent(root.transform, false);
        var genText = genGO.GetComponent<Text>();
        genText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        genText.fontSize = 12;
        genText.alignment = TextAnchor.LowerLeft;

        var statusGO = new GameObject("Status", typeof(RectTransform), typeof(Text));
        statusGO.transform.SetParent(root.transform, false);
        var statusText = statusGO.GetComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = content;
        t.fontSize = 14;
        t.color = Color.white;
        t.raycastTarget = false; // text không chặn click các nút bên dưới (vd dấu X)
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
        if (selectedSlime1Image != null || selectedSlime2Image != null || selectedSlime1Body != null || selectedSlime2Body != null)
        {
            SetSelectedSlime(selectedSlime1Image, selectedSlime1Body, selectedSlime1Armor, selectedSlime1Weapon, selectedSlime1);
            SetSelectedSlime(selectedSlime2Image, selectedSlime2Body, selectedSlime2Armor, selectedSlime2Weapon, selectedSlime2);
            UpdateBreedButton();
            return;
        }

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
        var allSlimes = BreedingManager.Instance.GetAllSlimes()
            .Where(slime => !HasSecretBodyTrait(slime))
            .ToList();
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(allSlimes.Count / (float)collectionPageSize));
        currentCollectionPage = Mathf.Clamp(currentCollectionPage, 0, pageCount - 1);
        int firstIndex = currentCollectionPage * collectionPageSize;
        int lastIndex = Mathf.Min(firstIndex + collectionPageSize, allSlimes.Count);

        // Create new slots - filter ra các slime có Secret body trait
        for (int i = firstIndex; i < lastIndex; i++)
        {
            Slime slime = allSlimes[i];
            // Bỏ qua slime có Secret body trait
            if (HasSecretBodyTrait(slime))
            {
                continue;
            }

            GameObject slot = Instantiate(collectionSlotPrefab, collectionGridParent);
            var slotScript = slot.GetComponent<SlimeSlotUI>();
            if (slotScript != null)
            {
                if (slotsprite != null) slotScript.sprite = slotsprite;
                slotScript.SetupSlime(slime);
                slotScript.OnSlimeSelected += OnSlimeSelected;
                slotScript.SetSelected(slime == selectedSlime1 || slime == selectedSlime2);
            }
            collectionSlots.Add(slot);
        }
        UpdatePagination(pageCount);
    }

    /// <summary>
    /// Kiểm tra xem slime có trait body với độ hiếm Secret không
    /// </summary>
    private static void SetSelectedSlime(Image fallback, Image body, Image armor, Image weapon, Slime slime)
    {
        if (body != null)
        {
            SetLayer(body, slime?.body?.sprite);
            SetLayer(armor, slime?.armor?.sprite);
            SetLayer(weapon, slime?.weapon?.sprite);
            if (fallback != null) fallback.enabled = false;
            return;
        }
        SetLayer(fallback, slime?.body?.sprite);
    }

    private static void SetLayer(Image target, Sprite sprite)
    {
        if (target == null) return;
        target.sprite = sprite;
        target.enabled = sprite != null;
    }

    private void AutoWireSelectedSlimeLayers(Transform root, string containerName, ref Image body, ref Image armor, ref Image weapon)
    {
        Transform container = FindChildRecursive(root, containerName);
        if (container == null) return;
        if (body == null) body = FindChildRecursive(container, "slimeBody")?.GetComponent<Image>();
        if (armor == null) armor = FindChildRecursive(container, "SlimeArmor")?.GetComponent<Image>();
        if (weapon == null) weapon = FindChildRecursive(container, "SlimeWeapon")?.GetComponent<Image>();
    }

    private void ShowPreviousCollectionPage()
    {
        if (currentCollectionPage <= 0) return;
        currentCollectionPage--;
        RefreshCollectionGrid();
    }

    private void ShowNextCollectionPage()
    {
        int count = BreedingManager.Instance != null
            ? BreedingManager.Instance.GetAllSlimes().Count(slime => !HasSecretBodyTrait(slime))
            : 0;
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(count / (float)collectionPageSize));
        if (currentCollectionPage >= pageCount - 1) return;
        currentCollectionPage++;
        RefreshCollectionGrid();
    }

    private void UpdatePagination(int pageCount)
    {
        if (previousPageButton != null) previousPageButton.interactable = currentCollectionPage > 0;
        if (nextPageButton != null) nextPageButton.interactable = currentCollectionPage < pageCount - 1;

        if (pageDots == null) return;
        for (int i = 0; i < pageDots.Length; i++)
        {
            Image dot = pageDots[i];
            if (dot == null) continue;
            bool visible = i < pageCount;
            dot.gameObject.SetActive(visible);
            if (visible)
                dot.sprite = i == currentCollectionPage ? activePageDotSprite : inactivePageDotSprite;
        }
    }

    private bool HasSecretBodyTrait(Slime slime)
    {
        if (slime == null || slime.body == null) return false;
        return slime.body.Rarity == Rarity.Secret && slime.body.TraitType == TraitType.Body;
    }

    private void OnSlimeSelected(Slime slime)
    {
        if (slime == null || !slime.canBreed || slime.breedingLocked ||
            (BreedingManager.Instance != null && BreedingManager.Instance.IsBreeding()))
            return;

        if (selectedSlime1 == slime)
        {
            selectedSlime1 = selectedSlime2;
            selectedSlime2 = null;
            UpdateSelectedSlimesText();
            RefreshSlimeGrid();
            RefreshCollectionGrid();
            UpdateBreedButton();
            return;
        }
        else if (selectedSlime2 == slime)
        {
            selectedSlime2 = null;
            UpdateSelectedSlimesText();
            RefreshSlimeGrid();
            RefreshCollectionGrid();
            UpdateBreedButton();
            return;
        }
        if (selectedSlime1 == null)
        {
            selectedSlime1 = slime;

        }
        else if (selectedSlime2 == null && selectedSlime1 != slime)
        {
            // Chỉ kiểm tra khả năng GHÉP CẶP (không khóa, không đang lai). KHÔNG chặn vì
            // thiếu Gold — để người chơi vẫn chọn được 2 slime và xem chi phí; nút Breed
            // sẽ tự mờ nếu chưa đủ Gold (UpdateBreedButton).
            bool pairOk = selectedSlime1.CanBreedWith(slime)
                && (BreedingManager.Instance == null || !BreedingManager.Instance.IsBreeding());
            if (pairOk)
            {
                selectedSlime2 = slime;
            }
            else
            {
                ResetSelection();
            }
        }
        UpdateSelectedSlimesText();
        RefreshSlimeGrid();
        RefreshCollectionGrid();
        UpdateBreedButton();
    }

    private bool CanBreedSelectedSlimes()
    {
        if (selectedSlime1 == null || selectedSlime2 == null) return false;
        
        // Kiểm tra slime có thể breeding với nhau
        if (!selectedSlime1.CanBreedWith(selectedSlime2)) return false;

        // Chỉ 1 phiên lai tạo tại một thời điểm (mục 3)
        if (BreedingManager.Instance == null || BreedingManager.Instance.IsBreeding()) return false;

        // Kiểm tra có đủ Gold theo tier độ hiếm (mục 3.1)
        if (CurrencyManager.Instance == null) return false;
        int breedingCost = BreedingManager.Instance.PreviewGoldCost(selectedSlime1, selectedSlime2);
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
            if (breedingPreviewText != null) breedingPreviewText.gameObject.SetActive(false);
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
        RefreshSlimeGrid();
        RefreshCollectionGrid();
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
            float remaining = BreedingManager.Instance.GetActiveRemainingSeconds();
            Rarity eggRarity = BreedingManager.Instance.GetActiveEggRarity();

            // UI bám trạng thái: ĐANG LAI thì LUÔN hiện màn đếm ngược và ẩn nút chọn/breed —
            // kể cả sau khi đóng rồi mở lại popup. Không cho quay về màn chọn slime giữa chừng.
            if (breedingProgressPanel != null) breedingProgressPanel.SetActive(true);
            if (breedButton != null) breedButton.gameObject.SetActive(false);
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            if (breedingPreviewText != null) breedingPreviewText.gameObject.SetActive(false);
            if (breedingProgressBar != null) breedingProgressBar.value = progress;

            int gemCost = BreedingManager.Instance.GetActiveFinishGemCost();

            if (breedingStatusText != null)
            {
                string s = $"Đang lai tạo ({eggRarity})...\nCòn lại {FormatTime(remaining)} • {(progress * 100):F0}%";
                // Nếu không gán nút Gem riêng, hiện số Gem tăng tốc ngay trong status text.
                if (finishWithGemsButton == null && remaining > 0f)
                    s += $"\nTăng tốc: {gemCost} Gem";
                breedingStatusText.text = s;
            }

            bool showGem = remaining > 0f;
            if (gemCostText != null) gemCostText.text = showGem ? $"{gemCost}" : string.Empty;
            if (finishWithGemsButton != null) finishWithGemsButton.gameObject.SetActive(showGem);
            if (gemIcon != null) gemIcon.gameObject.SetActive(showGem);
        }
        else
        {
            if (breedingProgressBar != null) breedingProgressBar.value = 0f;
            if (finishWithGemsButton != null) finishWithGemsButton.gameObject.SetActive(false);
            if (gemIcon != null) gemIcon.gameObject.SetActive(false);
            if (gemCostText != null) gemCostText.text = string.Empty;

            // Không lai: ẩn màn đếm ngược, hiện lại nút chọn/breed.
            if (breedingProgressPanel != null) breedingProgressPanel.SetActive(false);
            if (breedButton != null) breedButton.gameObject.SetActive(true);
            if (cancelButton != null) cancelButton.gameObject.SetActive(selectedSlime1 != null || selectedSlime2 != null);
            if (breedingPreviewText != null) breedingPreviewText.gameObject.SetActive(true);

            if (currentlyBreeding)
            {
                // Vừa lai xong.
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
                text += $"2: {selectedSlime2.slimeName}\n";

            // Hiện chi phí Gold/thời gian ngay trong text sẵn có (khi chưa gán breedingCostText riêng).
            if (breedingCostText == null && selectedSlime1 != null && selectedSlime2 != null && BreedingManager.Instance != null)
            {
                Rarity r = BreedingManager.Instance.PreviewEggRarity(selectedSlime1, selectedSlime2);
                int gold = BreedingManager.Instance.PreviewGoldCost(selectedSlime1, selectedSlime2);
                float sec = BreedingManager.Instance.PreviewDurationSeconds(selectedSlime1, selectedSlime2);
                text += $"Giá: {gold:N0} Gold  |  {r}  |  {FormatTime(sec)}";
            }

            selectedSlimesText.text = text;
        }

        UpdateBreedingCostPreview();
    }

    /// <summary>Hiện SỐ Gold + icon coin cho cặp đang chọn (mục 3.1). Ẩn khi đang lai/chưa đủ cặp.</summary>
    private void UpdateBreedingCostPreview()
    {
        bool show = selectedSlime1 != null && selectedSlime2 != null
                    && BreedingManager.Instance != null && !BreedingManager.Instance.IsBreeding();

        int gold = show ? BreedingManager.Instance.PreviewGoldCost(selectedSlime1, selectedSlime2) : 0;
        float mutationChance = 0f;
        if (show)
        {
            Rarity rarity = BreedingManager.Instance.PreviewEggRarity(selectedSlime1, selectedSlime2);
            float perTraitRate = SelectiveBreeding.GetMutationRate(rarity);
            mutationChance = 1f - Mathf.Pow(1f - perTraitRate, 3f);
        }

        if (breedingCostText != null)
            breedingCostText.text = show ? $"{gold:N0}" : string.Empty;

        if (costCoinIcon != null)
            costCoinIcon.gameObject.SetActive(show);

        if (energyCostText != null)
            energyCostText.text = show ? gold.ToString("N0") : "0";

        if (mutationPercentText != null)
            mutationPercentText.text = show ? $"{mutationChance * 100f:F0}%" : "0%";

        if (breedingPreviewText != null)
        {
            breedingPreviewText.text = show
                ? $"Trung {BreedingManager.Instance.PreviewEggRarity(selectedSlime1, selectedSlime2)}  |  {FormatTime(BreedingManager.Instance.PreviewDurationSeconds(selectedSlime1, selectedSlime2))}"
                : "Chon 2 slime o danh sach ben phai";
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
        if (slimeCollectionPanel != null) slimeCollectionPanel.SetActive(true);
        RefreshAllUI();
        // KHÔNG ép tắt màn đếm ngược ở đây — để UpdateBreedingProgress quyết định theo
        // trạng thái: nếu đang lai thì hiện màn đếm ngược ngay, nếu không thì hiện màn chọn.
        UpdateBreedingProgress();
    }

    public void ShowCollectionPanel()
    {
        // Play button click sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSFX();
        }

        panelBreedingActive = true;
        if (breedingPanel != null) breedingPanel.SetActive(true);
        if (slimeCollectionPanel != null) slimeCollectionPanel.SetActive(true);
        RefreshAllUI();
        UpdateBreedingProgress();
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
