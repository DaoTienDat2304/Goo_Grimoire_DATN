using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingSlot : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    public int slotID;
    public bool isOccupied = false;
    public GameObject buildingMenu;
    public BuildingMenuManager buildingMenuManager;
    public Image placedBuildingIcon;
    public SlimeWorldManager slimeWorldManager;
    public int slotIndex;
    public SaveAndLoadSystem saveAndLoadSystem;
    public GameObject TowerPanel;
    public GameObject shop;

    private void Awake()
    {
        AutoWireMissingReferences();
        RefreshBuildingCollider();
    }

    private void Start()
    {
        AutoWireMissingReferences();
        RefreshBuildingCollider();
        if (buildingMenu != null)
            buildingMenu.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AutoWireMissingReferences();

        // Play building click sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBuildingClickSFX();
        }

        if (isOccupied == false)
        {
            if (buildingMenuManager != null)
                buildingMenuManager.ToggleMenu();
            else
                Debug.LogWarning($"{nameof(BuildingSlot)} on {name} cannot open building menu because BuildingMenuManager is missing.", this);
            return;
        }

        if (slotID == 1)
        {
            if (slimeWorldManager != null)
                slimeWorldManager.StartBreedingView();
            else
                Debug.LogWarning($"{nameof(BuildingSlot)} on {name} cannot open breeding view because SlimeWorldManager is missing.", this);
            return;
        }

        if (slotID == 2)
        {
            if (saveAndLoadSystem != null)
                saveAndLoadSystem.Save();
            else
                SaveAndLoadSystem.Instance?.Save();

            ClearWorldSlimesIfAvailable();
            ShowFirstChild("MapSelection");
            return;
        }

        if (slotID == 3)
        {
            if (slimeWorldManager != null)
            {
                slimeWorldManager.StartinventoryView();
                slimeWorldManager.ClearWorldSlimes();
            }
            else
            {
                Debug.LogWarning($"{nameof(BuildingSlot)} on {name} cannot open inventory view because SlimeWorldManager is missing.", this);
            }
            return;
        }

        if (slotID == 4)
        {
            ClearWorldSlimesIfAvailable();
            SetPanelActive(TowerPanel, "TowerPanel");
            return;
        }

        if (slotID == 5)
        {
            ClearWorldSlimesIfAvailable();
            SetPanelActive(shop, "shop");
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (isOccupied == true) return;

        var dragged = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<BuildingDraggable>() : null;
        if (dragged == null || dragged.building == null) return;
        if (ArchievementManager.Instance != null)
        {
            ArchievementManager.Instance.GetArchivement(2); // 0 = Breed achievement
        }
        PlaceBuilding(dragged.building);
        if (buildingMenu != null) buildingMenu.SetActive(false);
    }

    private void PlaceBuilding(Building building)
    {
        if (isOccupied == false)
        {
            // Kiểm tra và trừ tiền trước khi xây dựng
            if (building.CanAfford())
            {
                // Trừ tiền
                bool purchaseSuccess = building.Purchase();
                if (purchaseSuccess)
                {
                    // Xây dựng thành công
                    if (placedBuildingIcon != null)
                    {
                        placedBuildingIcon.sprite = building.sprite;
                        placedBuildingIcon.enabled = true;
                        isOccupied = true;
                        slotID = building.buildingID;
                        RefreshBuildingCollider();

                        Debug.Log($"Đã xây dựng {building.buildingName} với chi phí: {building.GetCostDescription()}");

                        // Refresh menu để cập nhật trạng thái building (làm nhạt và disable drag)
                        if (buildingMenuManager != null)
                        {
                            buildingMenuManager.RefreshMenu();
                        }

                        SaveAndLoadSystem.Instance?.Save();
                    }
                }
                else
                {
                    Debug.Log($"Không thể xây dựng {building.buildingName}: Không đủ tiền!");
                }
            }
            else
            {
                Debug.Log($"Không thể xây dựng {building.buildingName}: Không đủ tiền! Cần: {building.GetCostDescription()}");
            }
        }
        else return;
        
    }

    private void AutoWireMissingReferences()
    {
        if (placedBuildingIcon == null)
            placedBuildingIcon = GetComponent<Image>();

        if (buildingMenuManager == null)
            buildingMenuManager = FindAnyObjectByType<BuildingMenuManager>(FindObjectsInactive.Include);

        if (buildingMenu == null && buildingMenuManager != null)
            buildingMenu = buildingMenuManager.menuRoot;

        if (slimeWorldManager == null)
            slimeWorldManager = FindAnyObjectByType<SlimeWorldManager>(FindObjectsInactive.Include);

        if (saveAndLoadSystem == null)
            saveAndLoadSystem = SaveAndLoadSystem.Instance != null
                ? SaveAndLoadSystem.Instance
                : FindAnyObjectByType<SaveAndLoadSystem>(FindObjectsInactive.Include);

        if (TowerPanel == null)
            TowerPanel = FindSceneObjectByName("TowerPanel");

        if (shop == null)
            shop = FindSceneObjectByName("shop") ?? FindSceneObjectByName("Shop");
    }

    public void RefreshBuildingCollider()
    {
        if (placedBuildingIcon == null)
            return;

        PolygonCollider2D polygon = GetComponent<PolygonCollider2D>();
        Sprite sprite = placedBuildingIcon.sprite;
        bool visible = isOccupied && placedBuildingIcon.enabled && sprite != null;
        foreach (CircleCollider2D circle in GetComponents<CircleCollider2D>())
            circle.enabled = false;

        if (!visible)
        {
            if (polygon != null) polygon.enabled = false;
            return;
        }

        if (polygon == null)
            polygon = gameObject.AddComponent<PolygonCollider2D>();

        int obstacleLayer = LayerMask.NameToLayer("obstacle");
        if (obstacleLayer >= 0)
            gameObject.layer = obstacleLayer;

        Rect rect = placedBuildingIcon.rectTransform.rect;
        Vector2 spriteSize = sprite.bounds.size;
        Vector2 displaySize = rect.size;
        if (placedBuildingIcon.preserveAspect && sprite.rect.width > 0f && sprite.rect.height > 0f)
        {
            float spriteAspect = sprite.rect.width / sprite.rect.height;
            float rectAspect = rect.width / rect.height;
            if (spriteAspect > rectAspect)
                displaySize.y = rect.width / spriteAspect;
            else
                displaySize.x = rect.height * spriteAspect;
        }

        Vector2 shapeScale = new Vector2(
            spriteSize.x > 0f ? displaySize.x / spriteSize.x : 1f,
            spriteSize.y > 0f ? displaySize.y / spriteSize.y : 1f);
        Vector2 centerOffset = sprite.pixelsPerUnit > 0f
            ? (sprite.pivot - sprite.rect.size * 0.5f) / sprite.pixelsPerUnit
            : Vector2.zero;
        int shapeCount = sprite.GetPhysicsShapeCount();

        if (shapeCount > 0)
        {
            polygon.pathCount = shapeCount;
            var points = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < shapeCount; i++)
            {
                points.Clear();
                sprite.GetPhysicsShape(i, points);
                for (int p = 0; p < points.Count; p++)
                {
                    points[p] = Vector2.Scale(points[p] + centerOffset, shapeScale);
                }
                polygon.SetPath(i, points);
            }
        }
        else
        {
            float halfWidth = displaySize.x * 0.5f;
            float halfHeight = displaySize.y * 0.5f;
            polygon.pathCount = 1;
            Vector2[] box =
            {
                new Vector2(-halfWidth, -halfHeight),
                new Vector2(-halfWidth, halfHeight),
                new Vector2(halfWidth, halfHeight),
                new Vector2(halfWidth, -halfHeight)
            };
            polygon.SetPath(0, box);
        }

        polygon.offset = Vector2.zero;
        polygon.isTrigger = false;
        polygon.enabled = true;
    }

    private void ClearWorldSlimesIfAvailable()
    {
        if (slimeWorldManager != null)
            slimeWorldManager.ClearWorldSlimes();
    }

    private void ShowFirstChild(string objectName)
    {
        GameObject target = FindSceneObjectByName(objectName);
        if (target == null)
        {
            Debug.LogWarning($"{nameof(BuildingSlot)} on {name} cannot find '{objectName}' in the scene.", this);
            return;
        }

        if (target.transform.childCount == 0)
        {
            Debug.LogWarning($"{nameof(BuildingSlot)} on {name} found '{objectName}', but it has no child to show.", target);
            return;
        }

        target.transform.GetChild(0).gameObject.SetActive(true);
    }

    private void SetPanelActive(GameObject panel, string panelName)
    {
        if (panel != null)
            panel.SetActive(true);
        else
            Debug.LogWarning($"{nameof(BuildingSlot)} on {name} cannot open {panelName} because the reference is missing.", this);
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var transform in transforms)
        {
            if (transform.name == objectName)
                return transform.gameObject;
        }

        return null;
    }
}
