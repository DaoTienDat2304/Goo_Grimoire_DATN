using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingMenuManager : MonoBehaviour
{
    [Header("Menu Root")]
    public GameObject menuRoot;

    [Header("Grid")]
    public Transform gridParent;
    public GameObject buildingItemPrefab;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.B;

    private readonly List<GameObject> spawnedItems = new List<GameObject>();
    public BreedingManager breedingManager;

    private void Awake()
    {
        EnsureRuntimeFallbacks();
    }

    private void Start()
    {
        EnsureRuntimeFallbacks();
        RefreshMenu();
        if (menuRoot != null) menuRoot.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        if (menuRoot == null) return;
        
        // Play button click sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSFX();
        }

        bool next = !menuRoot.activeSelf;
        menuRoot.SetActive(next);
        if (next)
        {
            RefreshMenu();
        }
    }

    public void RefreshMenu()
    {
        if (BuildingManager.Instance == null) return;

        foreach (var go in spawnedItems) Destroy(go);
        spawnedItems.Clear();

        foreach (var b in BuildingManager.Instance.GetAllBuildings())
        {
            if (b == null)
            {

            }
            else
            {
                if (b.slimeRequirement > breedingManager.GetAllSlimes().Count) b.buildable = false;
                else
                {
                     b.buildable = true;
                }
            }

        }

        var unlocked = BuildingManager.Instance.GetUnlockedBuildings();
        foreach (var b in unlocked)
        {
            var go = Instantiate(buildingItemPrefab, gridParent);
            var ui = go.GetComponent<BuildingUI>();
            if (ui != null) ui.SetupBuilding(b);

            // ensure draggable
            var drag = go.GetComponent<BuildingDraggable>();
            if (drag == null) drag = go.AddComponent<BuildingDraggable>();
            drag.SetBuilding(b);

            // Kiểm tra xem building đã được đặt chưa
            bool isAlreadyPlaced = IsBuildingAlreadyPlaced(b);
            if (isAlreadyPlaced)
            {
                // Làm nhạt màu building
                if (ui != null) ui.SetDimmed(true);
                
                // Disable drag
                if (drag != null) drag.SetDraggable(false);
            }
            else
            {
                // Đảm bảo màu sáng và có thể drag
                if (ui != null) ui.SetDimmed(false);
                if (drag != null) drag.SetDraggable(true);
            }

            spawnedItems.Add(go);
        }
    }

    /// <summary>
    /// Kiểm tra xem building đã được đặt trong slot nào chưa
    /// </summary>
    private bool IsBuildingAlreadyPlaced(Building building)
    {
        if (building == null) return false;

        var slots = FindObjectsByType<BuildingSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var slot in slots)
        {
            if (slot.isOccupied && slot.slotID == building.buildingID)
            {
                return true;
            }
        }
        return false;
    }

    private void EnsureRuntimeFallbacks()
    {
        // Canvas + EventSystem if missing
        if (FindAnyObjectByType<Canvas>() == null)
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(StandaloneInputModule));
        }

        var root = FindAnyObjectByType<Canvas>().transform;
        if (menuRoot == null)
        {
            menuRoot = new GameObject("BuildingMenu", typeof(RectTransform), typeof(Image));
            menuRoot.transform.SetParent(root, false);
            var img = menuRoot.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.4f);
            var rt = menuRoot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(12f, 12f);
            rt.sizeDelta = new Vector2(660f, 440f);
        }
        if (gridParent == null)
        {
            var gridGO = new GameObject("BuildingGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGO.transform.SetParent(menuRoot.transform, false);
            var rt = gridGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(12, 12);
            rt.offsetMax = new Vector2(-12, -12);
            var grid = gridGO.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(96, 96);
            grid.spacing = new Vector2(8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            gridParent = gridGO.transform;
        }
        if (buildingItemPrefab == null)
        {
            buildingItemPrefab = CreateRuntimeBuildingItemPrefab();
        }
    }

    private GameObject CreateRuntimeBuildingItemPrefab()
    {
        var root = new GameObject("RuntimeBuildingItem", typeof(RectTransform), typeof(Image));
        var bg = root.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.15f);

        var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(root.transform, false);
        var iconRT = icon.GetComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(72, 72);
        iconRT.anchoredPosition = Vector2.zero;

        var nameGO = new GameObject("Name", typeof(RectTransform), typeof(Text));
        nameGO.transform.SetParent(root.transform, false);
        var nameText = nameGO.GetComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        nameText.fontSize = 12;
        nameText.alignment = TextAnchor.LowerCenter;
        nameText.color = Color.white;
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchoredPosition = new Vector2(0, -38);
        nameRT.sizeDelta = new Vector2(96, 20);

        var ui = root.AddComponent<BuildingUI>();
        ui.buildingImage = icon.GetComponent<Image>();
        ui.nameText = nameText;

        return root;
    }
}
