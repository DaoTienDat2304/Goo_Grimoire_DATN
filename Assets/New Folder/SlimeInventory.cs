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
    [Tooltip("(Optional) Text hien thi so diem sacrifice current, e.g. 45/100.")]
    public Text sacrificeText;

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

            var disp = GetNumberDisplay();
            if (disp != null)
                disp.text = $"{Mathf.Clamp(Mathf.RoundToInt(Slider.value), 0, maxsacrifice)}/{maxsacrifice}";
        }
    }

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

    private Font FindAnyFont()
    {
        foreach (var t in GetComponentsInChildren<Text>(true))
            if (t != null && t.font != null) return t.font;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public void summonbutton()
    {
        sacrifice -= maxsacrifice;
        if (sacrifice < 0) sacrifice = 0;
        if (Slider != null) Slider.value = sacrifice;
        SaveAndLoadSystem.Instance?.Save();
    }


    public void ondeseclect()
    {
        foreach (GameObject inventorySlot in collectionSlots)
        {
            if (inventorySlot == null) continue;
            InventorySlot i = inventorySlot.GetComponent<InventorySlot>();
            if (i != null) i.SetBreedingSelected(false);
        }
        RefreshCollectionGrid();
    }
    public void ondelete()
    {
        foreach (GameObject inventorySlot in collectionSlots)
        {
            if (inventorySlot == null) continue;
            InventorySlot i = inventorySlot.GetComponent<InventorySlot>();
            if (i != null) i.removedslime();
        }
        RefreshCollectionGrid();
        CheckAndRefreshIfNeeded();

        SaveAndLoadSystem.Instance?.Save();
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
