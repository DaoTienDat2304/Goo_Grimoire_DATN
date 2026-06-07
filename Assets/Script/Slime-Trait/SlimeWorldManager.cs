using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Spine.Unity;

public class SlimeWorldManager : MonoBehaviour
{
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
        isWorldViewActive = true;
        breedingUI.panelBreedingActive = false;

        // Ẩn breeding UI
        if (breedingUI != null)
        {
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
        breedingUI.panelBreedingActive = false;
        inventory.SetActive(true);
        // Xóa slimes cũ và tạo mới
        ClearWorldSlimes();
    }
    public void StartBreedingView()
    {
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
        armorRenderer.sortingOrder = 1;

        weaponRenderer.sprite = (slime != null ? slime.weapon?.sprite : null) ?? CreateDefaultSlimeSprite();
        weaponRenderer.sortingOrder = 2;


        // Thêm CircleCollider2D để click
        var collider = slimeGO.AddComponent<CircleCollider2D>();
        collider.radius = 5.5f;
        collider.isTrigger = false;
        //Them Rigidbody2D de co the tuong tac
        var rb = slimeGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
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

    public Sprite CreateDefaultSlimeSprite()
    {
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
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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
