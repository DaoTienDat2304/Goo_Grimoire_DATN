using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdventureBag : MonoBehaviour
{
    public GameObject slimeCollectionPanel;
    public GameObject showslot;
    public Animator animator;

    [Header("Sidebar motion")]
    [SerializeField] private float closedX = 720f;
    [SerializeField] private float openX = 0f;
    [SerializeField] private float slideDuration = 0.22f;

    [Header("Breeding UI")]
    public Sprite slotsprite;
    private bool open = false;

    public bool IsOpen => open;

    [Header("Collection UI")]
    public Transform collectionGridParent;
    public GameObject collectionSlotPrefab;
    public WildSlimes wildSlimes;
    public TMP_Text tamedCountText;
    [SerializeField] private GameObject hierarchySlotTemplate;

    private List<GameObject> slimeSlots = new List<GameObject>();
    private List<GameObject> collectionSlots = new List<GameObject>();
    private RectTransform sidebarRect;
    private Coroutine sidebarRoutine;
    private bool sidebarInitialized;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        ResolveReferences();
        InitializeSidebar();
    }

    private void Start()
    {
        RefreshAllUI();
    }

    private void OnEnable()
    {
        RefreshAllUI();
    }

    public void click()
    {
        ResolveReferences();
        InitializeSidebar();
        open = !open;
        SlideSidebar(open);
        RefreshAllUI();
    }

    public void InitializeSidebar()
    {
        if (sidebarInitialized)
            return;

        sidebarRect = transform as RectTransform;
        if (sidebarRect == null)
        {
            Debug.LogWarning("[TameSidebar] TameInventory must use a RectTransform.", this);
            return;
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        // These clips were authored for the old layout and overwrite the
        // RectTransform coordinates used by the current sidebar.
        if (animator != null)
            animator.enabled = false;

        gameObject.SetActive(true);
        CanvasGroup panelGroup = GetComponent<CanvasGroup>();
        if (panelGroup != null)
        {
            panelGroup.alpha = 1f;
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
        }

        Vector2 position = sidebarRect.anchoredPosition;
        position.x = closedX;
        sidebarRect.anchoredPosition = position;
        open = false;
        sidebarInitialized = true;
    }

    private void SlideSidebar(bool shouldOpen)
    {
        if (sidebarRect == null)
            return;

        if (sidebarRoutine != null)
            StopCoroutine(sidebarRoutine);

        float targetX = shouldOpen ? openX : closedX;
        if (!gameObject.activeInHierarchy || slideDuration <= 0f)
        {
            Vector2 position = sidebarRect.anchoredPosition;
            position.x = targetX;
            sidebarRect.anchoredPosition = position;
            sidebarRoutine = null;
            return;
        }

        sidebarRoutine = StartCoroutine(SlideSidebarTo(targetX));
    }

    private IEnumerator SlideSidebarTo(float targetX)
    {
        Vector2 start = sidebarRect.anchoredPosition;
        Vector2 target = new Vector2(targetX, start.y);
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            sidebarRect.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
            yield return null;
        }

        sidebarRect.anchoredPosition = target;
        sidebarRoutine = null;
    }
    public void RefreshAllUI()
    {
        ResolveReferences();
        RefreshSlimeGrid();
        RefreshCollectionGrid();
    }

    private void RefreshSlimeGrid()
    {
        // Clear existing slots
        foreach (var slot in slimeSlots)
        {
            Destroy(slot);
        }
        slimeSlots.Clear();
    }

    private void RefreshCollectionGrid()
    {
        ClearRuntimeCollectionSlots();

        foreach (var slot in collectionSlots)
        {
            if (slot != null)
                Destroy(slot);
        }
        collectionSlots.Clear();

        if (wildSlimes == null || wildSlimes.tamedSlimes == null || collectionGridParent == null)
        {
            UpdateTamedCountText(0);
            return;
        }

        EnsureGridLayout();
        GameObject template = GetSlotTemplate();
        if (template == null)
        {
            UpdateTamedCountText(wildSlimes.tamedSlimes.Count);
            return;
        }

        HideHierarchyTemplates();

        var allSlimes = wildSlimes.tamedSlimes;
        int shownCount = 0;

        foreach (var WildSlimeTraits in allSlimes)
        {
            if (WildSlimeTraits == null)
                continue;

            GameObject slot = Instantiate(template, collectionGridParent);
            slot.name = $"TamedSlime_{WildSlimeTraits.slimeID}";
            slot.SetActive(true);
            collectionSlots.Add(slot);

            SetupCollectionSlot(slot, WildSlimeTraits);
            shownCount++;
        }

        UpdateTamedCountText(shownCount);
    }

    private void ResolveReferences()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (slimeCollectionPanel == null)
            slimeCollectionPanel = gameObject;

        if (collectionGridParent == null)
        {
            Transform content = FindChildByName(transform, "Content");
            collectionGridParent = content != null ? content : transform;
        }

        if (hierarchySlotTemplate == null && collectionGridParent != null && collectionGridParent.childCount > 0)
            hierarchySlotTemplate = FindChildByName(collectionGridParent, "TameInventorySlot_1")?.gameObject ?? collectionGridParent.GetChild(0).gameObject;

        if (tamedCountText == null)
            tamedCountText = FindTamedCountText();

        if (wildSlimes == null)
        {
            var save = SaveAndLoadSystem.Instance != null ? SaveAndLoadSystem.Instance : FindAnyObjectByType<SaveAndLoadSystem>(FindObjectsInactive.Include);
            if (save != null)
                wildSlimes = save.wildSlimes;
        }

        if (wildSlimes == null)
            wildSlimes = FindAnyObjectByType<WildSlimes>(FindObjectsInactive.Include);
    }

    private GameObject GetSlotTemplate()
    {
        if (collectionSlotPrefab != null)
            return collectionSlotPrefab;

        if (hierarchySlotTemplate == null && collectionGridParent != null)
            hierarchySlotTemplate = FindChildByName(collectionGridParent, "TameInventorySlot_1")?.gameObject;

        if (hierarchySlotTemplate == null && collectionGridParent != null && collectionGridParent.childCount > 0)
            hierarchySlotTemplate = collectionGridParent.GetChild(0).gameObject;

        if (hierarchySlotTemplate != null)
            return hierarchySlotTemplate;

        return null;
    }

    private void HideHierarchyTemplates()
    {
        if (collectionGridParent == null)
            return;

        for (int i = 0; i < collectionGridParent.childCount; i++)
        {
            GameObject child = collectionGridParent.GetChild(i).gameObject;
            if (!collectionSlots.Contains(child))
                child.SetActive(false);
        }
    }

    private void EnsureGridLayout()
    {
        if (collectionGridParent == null)
            return;

        var grid = collectionGridParent.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = collectionGridParent.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(78f, 78f);
            grid.spacing = new Vector2(12f, 12f);
            grid.childAlignment = TextAnchor.UpperCenter;
        }

        if (collectionGridParent.GetComponent<ContentSizeFitter>() == null)
        {
            var fitter = collectionGridParent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private void SetupCollectionSlot(GameObject slot, WildSlimes.WildSlimeTraits slime)
    {
        var slotScript = slot.GetComponent<tameslimeslot>();
        if (slotScript == null)
            slotScript = slot.AddComponent<tameslimeslot>();

        slotScript.wildSlimes = wildSlimes;
        slotScript.SetupSlime(slime.slimeID);

        SetLayerSprite(slot.transform, "slimeBody", slime.wildSlimeTraits, 0);
        SetLayerSprite(slot.transform, "SlimeBody", slime.wildSlimeTraits, 0);
        SetLayerSprite(slot.transform, "SlimeArmor", slime.wildSlimeTraits, 1);
        SetLayerSprite(slot.transform, "SlimeWeapon", slime.wildSlimeTraits, 2);
    }

    private void SetLayerSprite(Transform slot, string childName, TraitSO[] traits, int index)
    {
        if (traits == null || index < 0 || index >= traits.Length || traits[index] == null)
            return;

        Transform child = FindChildByName(slot, childName);
        Image image = child != null ? child.GetComponent<Image>() : null;
        if (image == null)
            return;

        image.sprite = traits[index].sprite;
        image.enabled = image.sprite != null;
        image.preserveAspect = true;
    }

    private void ClearRuntimeCollectionSlots()
    {
        if (collectionGridParent == null)
            return;

        for (int i = collectionGridParent.childCount - 1; i >= 0; i--)
        {
            Transform child = collectionGridParent.GetChild(i);
            if (child.name.StartsWith("TamedSlime_"))
                Destroy(child.gameObject);
        }
    }

    private void UpdateTamedCountText(int count)
    {
        if (tamedCountText == null)
            tamedCountText = FindTamedCountText();

        if (tamedCountText != null)
            tamedCountText.text = $"{count} / 30";
    }

    private TMP_Text FindTamedCountText()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var text in texts)
        {
            if (text != null && (text.name.Contains("Count") || text.text.Contains("/")))
                return text;
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root.name == childName)
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindChildByName(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
