using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Spine.Unity;

public class SlimeWorldManager : MonoBehaviour
{
    private static Sprite cachedDefaultSlimeSprite;
    private const int BackgroundUiSortingOrder = -100;
    private const int BuildingUiSortingOrder = -90;
    private const int WorldSlimeSortingOrder = -50;

    [Header("World Display")]
    public bool showSlimesInWorld = true;
    public float worldRadius = 12f;
    public int maxWorldSlimes = 20;

    [Header("Movement Area")]
    public List<Transform> buildingAreas;
    public Transform movementArea; // Optional area object to define movement region
    public bool useAreaCollider = true; // Read size from Circle/BoxCollider2D on movementArea if present
    [Range(0f, 1f)] public float innerSafeZoneFactor = 0.3f; // Inner zone factor for sampling/gizmos

    [Header("Slime Movement")]
    public float slimeMoveSpeed = 1.5f;
    public float slimeRotationSpeed = 25f;
    public float slimeBounceHeight = 0.4f;
    public float slimeBounceSpeed = 1.5f;

    [Header("UI Integration")]
    public BreedingUIManager breedingUI;
    public GameObject breedingUIButton;
    public GameObject inventory;
    public Button worldViewButton;

    private GameObject[] worldSlimes;
    private Vector3[] slimePositions;
    private Vector3[] slimeTargets;
    private float[] slimeBounceOffsets;
    private float[] slimeBounceTimes;
    private Slime[] slimeData;
    public GameObject breedUI;
    public Button traitCollection;

    public bool isWorldViewActive = false;
    private Camera mainCamera;

    // Cached movement area colliders
    private CircleCollider2D areaCircleCollider;
    private BoxCollider2D areaBoxCollider;
    private void Start()
    {
        InitializeWorld();
        SetupUI();

        if (showSlimesInWorld)
        {
            StartWorldView();
        }
        RefreshWorldSlimes();
    }

    private void InitializeWorld()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindAnyObjectByType<Camera>();
        }

        ConfigureWorldViewSorting();

        // Cache movement area colliders if provided
        if (movementArea != null)
        {
            areaCircleCollider = movementArea.GetComponent<CircleCollider2D>();
            areaBoxCollider = movementArea.GetComponent<BoxCollider2D>();
        }

        // Tạo arrays
        worldSlimes = new GameObject[maxWorldSlimes];
        slimePositions = new Vector3[maxWorldSlimes];
        slimeTargets = new Vector3[maxWorldSlimes];
        slimeBounceOffsets = new float[maxWorldSlimes];
        slimeBounceTimes = new float[maxWorldSlimes];
        slimeData = new Slime[maxWorldSlimes];

        // Tạo vị trí ban đầu
        CreateInitialPositions();
    }

    private void CreateInitialPositions()
    {
        for (int i = 0; i < maxWorldSlimes; i++)
        {
            Vector3 position = GetRandomPointInArea();
            slimePositions[i] = position;
            slimeTargets[i] = position;
            slimeBounceOffsets[i] = Random.Range(0f, 2f * Mathf.PI);
            slimeBounceTimes[i] = 0f;
        }
    }

    private void SetupUI()
    {
        // Tìm breeding UI nếu không được gán
        if (breedingUI == null)
        {
            breedingUI = FindAnyObjectByType<BreedingUIManager>();

        }

        // Luôn tạo buttons mới để đảm bảo
        CreateViewButtons();


    }

    private void CreateViewButtons()
    {
        // Tìm Canvas có sẵn (Canvas của breeding UI)
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {

            return;
        }



        // Tìm buttons đã được tạo thủ công
        var worldButton = canvas.transform.Find("WorldViewButton");

        if (worldButton != null)
        {
            worldViewButton = worldButton.GetComponent<Button>();

        }
        else
        {

        }

        // Thiết lập onClick events nếu buttons tồn tại
        if (worldViewButton != null)
        {
            worldViewButton.onClick.RemoveAllListeners();
            worldViewButton.onClick.AddListener(StartWorldView);
        }


    }



    public void StartWorldView()
    {
        ConfigureWorldViewSorting();
        isWorldViewActive = true;

        // Ẩn breeding UI
        if (breedingUI != null)
        {
            breedingUI.panelBreedingActive = false;
            breedingUI.gameObject.SetActive(false);
        }
        if (breedUI != null)
        {
            breedUI.SetActive(false);
        }

        // Hiển thị world view buttons
        if (worldViewButton != null)
        {
            worldViewButton.gameObject.SetActive(false);
        }
        if (traitCollection != null)
        {
            traitCollection.gameObject.SetActive(true);
        }

        CreateWorldSlimes();
    }

    public void StartinventoryView()
    {
        isWorldViewActive = true;
        if (breedingUI != null)
        {
            breedingUI.panelBreedingActive = false;
            breedingUI.gameObject.SetActive(false);
        }
        if (breedUI != null)
        {
            breedUI.SetActive(false);
        }
        if (inventory != null)
        {
            inventory.SetActive(true);
        }
        // Xóa slimes cũ và tạo mới
        ClearWorldSlimes();
    }
    public void StartBreedingView()
    {
        if (breedingUI != null)
            breedingUI.panelBreedingActive = true;
        isWorldViewActive = false;

        // Hiển thị breeding UI
        if (breedingUI != null)
        {
            breedingUI.gameObject.SetActive(true);
        }
        if (breedUI != null)
        {
            breedUI.SetActive(true);
        }
        if (traitCollection != null)
        {
            traitCollection.gameObject.SetActive(false);
        }

        // Hiển thị world view buttons
        if (worldViewButton != null) worldViewButton.gameObject.SetActive(true);

        // Xóa slime trong thế giới
        ClearWorldSlimes();
    }

    public void CreateWorldSlimes()
    {
        // Kiểm tra BreedingManager
        if (BreedingManager.Instance == null)
        {

            return;
        }

        // Lấy danh sách slime từ BreedingManager
        var allSlimes = BreedingManager.Instance.GetAllSlimes();
        if (allSlimes == null || allSlimes.Count == 0)
        {

            return;
        }



        // Tạo slimes mới
        int slimeCount = Mathf.Min(allSlimes.Count, maxWorldSlimes);
        for (int i = 0; i < slimeCount; i++)
        {
            if (allSlimes[i] == null)
            {

                continue;
            }
            CreateSingleWorldSlime(i, allSlimes[i]);
        }
    }

    private void CreateSingleWorldSlime(int index, Slime slime)
    {
        // Tạo GameObject cho slime

        GameObject slimeGO = new GameObject($"WorldSlime_{index}");
        slimeGO.transform.position = slimePositions[index];

        // Thêm SlimeAnimationController để quản lý body
        var animationController = slimeGO.AddComponent<SlimeAnimationController>();
        
        if (slime?.body?.hasAnimation == true)
        {
            // Nếu có animation, khởi tạo với animation
            animationController.Initialize(slime.body.animationAsset, slime.body.animationName);

            animationController.PlayAnimation("animation");
        }
        else
        {
            // Nếu không có animation, khởi tạo với sprite
            animationController.Initialize(null);
            animationController.SetSprite((slime != null ? slime.body?.sprite : null) ?? CreateDefaultSlimeSprite());
        }

        var armorGO = new GameObject("Armor");
        armorGO.transform.SetParent(slimeGO.transform, false);
        
        armorGO.transform.localScale = Vector3.one;
        var armorRenderer = armorGO.AddComponent<SpriteRenderer>();
        armorGO.transform.localPosition = Vector3.up * 4.1f;
        var weaponGO = new GameObject("Weapon");
        weaponGO.transform.SetParent(slimeGO.transform, false);
        weaponGO.transform.localScale = Vector3.one;
        var weaponRenderer = weaponGO.AddComponent<SpriteRenderer>();
        weaponGO.transform.localPosition = Vector3.up * 4.1f;

        // Gán sprite cho armor và weapon
        armorRenderer.sprite = (slime != null ? slime.armor?.sprite : null) ?? CreateDefaultSlimeSprite();
        armorRenderer.sortingOrder = WorldSlimeSortingOrder + 1;

        weaponRenderer.sprite = (slime != null ? slime.weapon?.sprite : null) ?? CreateDefaultSlimeSprite();
        weaponRenderer.sortingOrder = WorldSlimeSortingOrder + 2;


        // Thêm CircleCollider2D để click
        var collider = slimeGO.AddComponent<CircleCollider2D>();
        collider.radius = 5.5f;
        // Slime nay duoc di chuyen truc tiep bang Transform. Collider trigger van
        // click/raycast duoc ma khong bat physics solver xu ly contact lien tuc.
        collider.isTrigger = true;
        // Thêm tên slime
        var nameText = CreateSlimeNameText(slime != null ? slime.slimeName : "Slime");
        nameText.transform.SetParent(slimeGO.transform);
        nameText.transform.localPosition = Vector3.up*12f;
        nameText.characterSize = 2f;

        // Thêm outline cho tên
        var outlineText = CreateSlimeNameText(slime != null ? slime.slimeName : "Slime");
        outlineText.transform.SetParent(slimeGO.transform);
        outlineText.transform.localPosition = Vector3.up*12f;
        outlineText.transform.SetSiblingIndex(nameText.transform.GetSiblingIndex());
        outlineText.color = Color.black;
        outlineText.characterSize = 2f;

        // Keep every part of a world slime between the background UI and the
        // rest of the UI, regardless of whether its body is Spine or a sprite.
        SetRendererSortingOrder(slimeGO, WorldSlimeSortingOrder);
        armorRenderer.sortingOrder = WorldSlimeSortingOrder + 1;
        weaponRenderer.sortingOrder = WorldSlimeSortingOrder + 2;
        nameText.GetComponent<MeshRenderer>().sortingOrder = WorldSlimeSortingOrder + 3;
        outlineText.GetComponent<MeshRenderer>().sortingOrder = WorldSlimeSortingOrder + 3;

        // Thiết lập kích thước
        slimeGO.transform.localScale = Vector3.one*0.08f;

        // Lưu trữ
        worldSlimes[index] = slimeGO;
        slimeData[index] = slime;

        // Thêm click handler
        if (slime != null)
        {
            var clickHandler = slimeGO.AddComponent<SlimeClickHandler>();
            clickHandler.Initialize(slime);
        }
    }

    private TextMesh CreateSlimeNameText(string slimeName)
    {
        var textGO = new GameObject("SlimeName");
        var textMesh = textGO.AddComponent<TextMesh>();
        textMesh.text = slimeName;
        textMesh.fontSize = 10;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = Color.white;
        textMesh.characterSize = 0.08f;
        textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return textMesh;
    }

    private void ConfigureWorldViewSorting()
    {
        Canvas rootCanvas = breedingUI != null ? breedingUI.GetComponentInParent<Canvas>() : null;
        if (rootCanvas == null || rootCanvas.transform.Find("BackGround") == null)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].transform.Find("BackGround") == null && canvases[i].transform.Find("BuildingSlotArea") == null)
                    continue;
                rootCanvas = canvases[i];
                break;
            }
        }
        if (rootCanvas == null)
        {
            return;
        }

        ConfigureBackgroundCanvas(rootCanvas.transform.Find("BackGround"), rootCanvas, BackgroundUiSortingOrder, false);
        ConfigureBackgroundCanvas(rootCanvas.transform.Find("BuildingSlotArea"), rootCanvas, BuildingUiSortingOrder, true);
    }

    private static void ConfigureBackgroundCanvas(Transform target, Canvas rootCanvas, int sortingOrder, bool needsRaycaster)
    {
        if (target == null)
        {
            return;
        }

        Canvas canvas = target.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = target.gameObject.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingLayerID = rootCanvas.sortingLayerID;
        canvas.sortingOrder = sortingOrder;

        Graphic rootGraphic = target.GetComponent<Graphic>();
        if (rootGraphic != null) rootGraphic.raycastTarget = false;
        if (needsRaycaster && target.GetComponent<GraphicRaycaster>() == null)
            target.gameObject.AddComponent<GraphicRaycaster>();
    }

    private static void SetRendererSortingOrder(GameObject root, int sortingOrder)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sortingOrder = sortingOrder;
        }
    }

    public Sprite CreateDefaultSlimeSprite()
    {
        if (cachedDefaultSlimeSprite != null)
            return cachedDefaultSlimeSprite;

        int size = 64;
        Texture2D texture = new Texture2D(size, size);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2, size / 2));
                if (distance < size / 2)
                {
                    float alpha = 1f - (distance / (size / 2));
                    texture.SetPixel(x, y, new Color(0.2f, 0.8f, 0.3f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        cachedDefaultSlimeSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        cachedDefaultSlimeSprite.name = "DefaultSlimeSprite_Cached";
        return cachedDefaultSlimeSprite;
    }

    public void ClearWorldSlimes()
    {
        for (int i = 0; i < worldSlimes.Length; i++)
        {
            if (worldSlimes[i] != null)
            {
                Destroy(worldSlimes[i]);
                worldSlimes[i] = null;
                slimeData[i] = null;
            }
        }
    }

    private void Update()
    {
        if (isWorldViewActive)
        {
            UpdateWorldSlimes();
        }
    }

    private void UpdateWorldSlimes()
    {
        for (int i = 0; i < worldSlimes.Length; i++)
        {
            if (worldSlimes[i] == null) continue;

            // Cập nhật bounce
            slimeBounceTimes[i] += Time.deltaTime * slimeBounceSpeed;
            float bounce = Mathf.Sin(slimeBounceTimes[i] + slimeBounceOffsets[i]) * slimeBounceHeight;

            // Cập nhật vị trí
            Vector3 currentPos = worldSlimes[i].transform.position;
            Vector3 targetPos = slimeTargets[i] + Vector3.up * bounce;

            // Di chuyển đến vị trí mục tiêu
            if (Vector3.Distance(currentPos, targetPos) > 0.1f)
            {
                Vector3 newPos = Vector3.MoveTowards(currentPos, targetPos, slimeMoveSpeed * Time.deltaTime);
                worldSlimes[i].transform.position = newPos;
            }

            // Xoay slime
            worldSlimes[i].transform.Rotate(0, 0, slimeRotationSpeed * Time.deltaTime);

            // Thay đổi vị trí mục tiêu ngẫu nhiên
            if (Random.Range(0f, 1f) < 0.005f)
            {
                Vector3 newTarget = GetRandomPointInArea();
                slimeTargets[i] = newTarget;
            }
        }
    }

    public void RefreshWorldSlimes()
    {
        if (isWorldViewActive)
        {
            ClearWorldSlimes();
            CreateWorldSlimes();
        }
        else
        {

        }
    }



    private void OnDrawGizmosSelected()
    {
        // Determine area center
        Transform area = movementArea != null ? movementArea : transform;
        Vector3 center = area.position;

        // Try draw based on collider if present
        CircleCollider2D circle = null;
        BoxCollider2D box = null;
        if (movementArea != null)
        {
            circle = movementArea.GetComponent<CircleCollider2D>();
            box = movementArea.GetComponent<BoxCollider2D>();
        }

        if (circle != null && useAreaCollider)
        {
            float scale = Mathf.Max(movementArea.lossyScale.x, movementArea.lossyScale.y);
            float r = circle.radius * scale;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((Vector2)center + circle.offset, r);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere((Vector2)center + circle.offset, r * innerSafeZoneFactor);
            return;
        }

        if (box != null && useAreaCollider)
        {
            Vector3 size = Vector3.Scale((Vector3)box.size, movementArea.lossyScale);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center + (Vector3)box.offset, size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center + (Vector3)box.offset, size * innerSafeZoneFactor);
            return;
        }

        // Fallback to circle with worldRadius around area center
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, worldRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, worldRadius * innerSafeZoneFactor);
    }

    private Vector3 GetRandomPointInArea()
    {
        Vector3 center = movementArea != null ? movementArea.position : transform.position;

        // Prefer collider shape if requested and available
        if (useAreaCollider && movementArea != null)
        {
            if (areaCircleCollider == null && areaBoxCollider == null)
            {
                areaCircleCollider = movementArea.GetComponent<CircleCollider2D>();
                areaBoxCollider = movementArea.GetComponent<BoxCollider2D>();
            }

            if (areaCircleCollider != null)
            {
                float scale = Mathf.Max(movementArea.lossyScale.x, movementArea.lossyScale.y);
                float maxR = areaCircleCollider.radius * scale;
                float r = Random.Range(maxR * innerSafeZoneFactor, maxR);
                float ang = Random.Range(0f, Mathf.PI * 2f);
                Vector2 offset = new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
                return (Vector2)center + areaCircleCollider.offset + offset;
            }

            if (areaBoxCollider != null)
            {
                Vector3 size = Vector3.Scale((Vector3)areaBoxCollider.size, movementArea.lossyScale);
                Vector2 half = new Vector2(size.x, size.y) * 0.5f;
                Vector2 innerHalf = half * innerSafeZoneFactor;
                float x = Random.Range(-half.x, half.x);
                float y = Random.Range(-half.y, half.y);
                if (Mathf.Abs(x) < innerHalf.x) x = Mathf.Sign(x == 0 ? Random.value - 0.5f : x) * Random.Range(innerHalf.x, half.x);
                if (Mathf.Abs(y) < innerHalf.y) y = Mathf.Sign(y == 0 ? Random.value - 0.5f : y) * Random.Range(innerHalf.y, half.y);
                Vector2 offset = new Vector2(x, y);
                return center + (Vector3)areaBoxCollider.offset + (Vector3)offset;
            }
        }

        // Fallback: circle around center using worldRadius
        float radius = Random.Range(worldRadius * innerSafeZoneFactor, worldRadius);
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        return center + pos;
    }
}
