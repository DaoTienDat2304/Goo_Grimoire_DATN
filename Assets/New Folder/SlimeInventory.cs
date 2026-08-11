using Spine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SlimeInventory : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject breedingPanel;
    public GameObject slimeCollectionPanel;
    public GameObject breedingProgressPanel;
    public Button button;
    public GameObject showslot;

    [Header("Breeding UI")]
    public Sprite slotsprite;

    [Header("Collection UI")]
    public Transform collectionGridParent;
    public GameObject collectionSlotPrefab;

    [Header("Slime Counter UI")]
    public Text slimeCounterText;
    public GameObject messagePanel;
    public Text messageText;

    private List<GameObject> slimeSlots = new List<GameObject>();
    private List<GameObject> collectionSlots = new List<GameObject>();
    public float interval = 1f;
    public bool panelBreedingActive;
    public int maxsacrifice = 100;
    public int sacrifice;
    public Slider Slider;
    [Tooltip("(Tuỳ chọn) Text hiển thị số điểm hi sinh hiện tại, ví dụ 45/100.")]
    public Text sacrificeText;

    /// <summary>
    /// Điểm hi sinh theo độ hiếm của slime. Cộng dồn đủ 100 → summon 1 slime Secret.
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
        EnsureRuntimeFallbacks();
    }

    private void Start()
    {
        RefreshAllUI();
        StartCoroutine(Countdown());
    }

    private void OnEnable()
    {
        RefreshAllUI();
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
        /*UpdateSlimeCounter(); // Cập nhật counter liên tục
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0f; // reset
            RefreshCollectionGrid();

            // Kiểm tra và refresh nếu có slimes mới được tạo
            CheckAndRefreshIfNeeded();
        }*/
        // Nút summon chỉ hiện khi ĐÃ chốt đủ 100 điểm (không tính preview).
        if (button != null) button.gameObject.SetActive(sacrifice >= maxsacrifice);

        if (Slider != null)
        {
            // Preview: thanh dâng lên theo điểm của slime đang chọn (demo). Bỏ chọn → hạ về mốc cũ.
            float target = Mathf.Min(sacrifice + PreviewPoints(), maxsacrifice);
            Slider.value = Mathf.MoveTowards(Slider.value, target, 120f * Time.deltaTime);

            // Số X/100 ở vị trí cũ (chữ FUSION) — hoặc sacrificeText nếu đã gán.
            var disp = GetNumberDisplay();
            if (disp != null)
                disp.text = $"{Mathf.Clamp(Mathf.RoundToInt(Slider.value), 0, maxsacrifice)}/{maxsacrifice}";
        }
    }

    // Tổng điểm hi sinh của các slime ĐANG được chọn (để preview thanh).
    private int PreviewPoints()
    {
        int sum = 0;
        if (collectionSlots == null) return 0;
        foreach (var go in collectionSlots)
        {
            if (go == null) continue;
            var s = go.GetComponent<InventorySlot>();
            if (s != null && s.onselect)
                sum += SacrificePoints(SelectiveBreeding.GetSlimeRarity(s.GetSlime()));
        }
        return sum;
    }

    private Text cachedNumber;

    // Text hiển thị số X/100: ưu tiên sacrificeText; nếu chưa gán thì TỰ TẠO 1 Text căn GIỮA
    // bên trong thanh (Slider) — giữ nguyên chữ FUSION.
    private Text GetNumberDisplay()
    {
        if (sacrificeText != null) return sacrificeText;
        if (cachedNumber != null) return cachedNumber;
        if (Slider == null) return null;

        var go = new GameObject("SacrificeNumber", typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(Slider.transform, false);
        // Đặt ở ĐÚNG TÂM thanh (anchor + pivot giữa), hộp nhỏ cố định.
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(90f, 28f);
        rt.anchoredPosition = new Vector2(-5f, 0f); // dịch trái 5 cho khớp tâm thanh
        rt.localScale = Vector3.one;

        var txt = go.GetComponent<Text>();
        txt.font = FindAnyFont();
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.fontStyle = FontStyle.Bold;
        txt.raycastTarget = false;
        // Tự co chữ cho vừa hộp → không bị to.
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = 6;
        txt.resizeTextMaxSize = 16;

        go.transform.SetAsLastSibling(); // nổi trên phần fill để luôn nhìn thấy số
        cachedNumber = txt;
        return txt;
    }

    private Font FindAnyFont()
    {
        foreach (var t in GetComponentsInChildren<Text>(true))
            if (t != null && t.font != null) return t.font;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public void summonbutton()
    {
        sacrifice -= maxsacrifice;
        Slider.value = sacrifice;
    }


    public void ondeseclect()
    {
        foreach (GameObject inventorySlot in collectionSlots)
        {
            InventorySlot i = inventorySlot.GetComponent<InventorySlot>();
            i.SetBreedingSelected(false);
            Debug.Log("can not");
        }
        RefreshCollectionGrid();
    }
    public void ondelete()
    {
        foreach (GameObject inventorySlot in collectionSlots)
        {
            InventorySlot i = inventorySlot.GetComponent<InventorySlot>();
            i.removedslime();
            Debug.Log("can not");
        }
        RefreshCollectionGrid();
        // Kiểm tra và refresh nếu có slimes mới được tạo
        CheckAndRefreshIfNeeded();
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
        // Clear existing slots
        foreach (var slot in collectionSlots)
        {
            Destroy(slot);
        }
        collectionSlots.Clear();

        // Get all slimes
        var allSlimes = BreedingManager.Instance.GetAllSlimes();

        // Create new slots
        foreach (var slime in allSlimes)
        {
            GameObject slot = Instantiate(collectionSlotPrefab, collectionGridParent);
            var slotScript = slot.GetComponent<InventorySlot>();
            slotScript.canselect = true;
            slotScript.sprite = slotsprite;
            collectionSlots.Add(slot);
            if (slotScript != null)
            {
                slotScript.SetupSlime(slime);
            }
        }
    }

    public void ShowCollectionPanel()
    {
        breedingPanel.SetActive(false);
        slimeCollectionPanel.SetActive(true);
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
