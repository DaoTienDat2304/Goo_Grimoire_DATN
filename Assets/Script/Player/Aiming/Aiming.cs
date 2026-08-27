using UnityEngine;
using UnityEngine.SceneManagement;

public class Aiming : MonoBehaviour
{
    private const float VirtualThrowReleaseThreshold = 12f;

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform startPosition;

    private Vector2 aimingLinePosition;
    private bool clickedWithinArea;
    [SerializeField] private float maxLength = 5f;
    [SerializeField] private Transform idlePosition;
    [SerializeField] private aimingArea aimingarea;
    [SerializeField] private ThrowingCatcher catcherPrefab;
    [SerializeField] private float force = 8f;
    [SerializeField] private GameObject tamingUI;
    [SerializeField] private int marshmallowCostPerThrow = 1;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Camera gameplayCamera;
    private ThrowingCatcher spawnedCatcher;
    private bool spriteFacesRight = true;
    private bool dragAttackFrameShown;
    private bool IsFreeThrowScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName == "travelSence" || sceneName.ToLower().Contains("travel");
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        ResolveReferences();
        if (lineRenderer != null)
            lineRenderer.enabled = false;

    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ResolveReferences();
        if (lineRenderer == null || startPosition == null || idlePosition == null)
            return;

        MobileInput.TryGetAimPointer(out var pointerPosition, out bool pointerPressed, out bool pointerHeld, out bool pointerReleased);
        bool isVirtualThrowButton = MobileInput.LastAimPointerFromVirtualButton;

        spriteFacesRight = (lineRenderer.GetPosition(0).x > lineRenderer.GetPosition(1).x);
        if (pointerPressed && (isVirtualThrowButton || (aimingarea != null && aimingarea.isWithinArea(pointerPosition))))
        {
            bool isFreeThrowScene = IsFreeThrowScene();
            bool canSpawn = isFreeThrowScene ||
                           (ResourceManager.Instance != null && 
                            ResourceManager.Instance.HasEnoughResource(ResourceType.Marshmallow, marshmallowCostPerThrow));
            
            if (canSpawn)
            {
                clickedWithinArea = true;
                dragAttackFrameShown = false;
                playerMovement?.HoldAttackFrame();
                SpawnCatcher();
            }
            else
            {
                Debug.LogWarning("Not enough Marshmallow.");
            }
        }
        if (pointerHeld && clickedWithinArea)
        {
            if (isVirtualThrowButton)
                DrawVirtualLine(MobileInput.VirtualAimDragVector);
            else
                DrawLine(pointerPosition);

            CatcherRotation();
            spawnedCatcher.transform.SetParent(this.transform);
            if (!dragAttackFrameShown)
            {
                playerMovement?.DragAttackFrame();
                dragAttackFrameShown = true;
            }
        }
        if (pointerReleased)
        {
            clickedWithinArea = false;
            dragAttackFrameShown = false;

            if (spawnedCatcher != null && isVirtualThrowButton && MobileInput.VirtualAimDragVector.magnitude < VirtualThrowReleaseThreshold)
            {
                Destroy(spawnedCatcher.gameObject);
                spawnedCatcher = null;
                SetLine(startPosition.position);
                lineRenderer.enabled = false;
                playerMovement?.CancelAttack();
                return;
            }

            if (spawnedCatcher != null)
            {
                bool isFreeThrowScene = IsFreeThrowScene();
                if (!isFreeThrowScene && ResourceManager.Instance != null)
                {
                    bool spent = ResourceManager.Instance.SpendResource(ResourceType.Marshmallow, marshmallowCostPerThrow);
                    if (!spent)
                    {
                        Debug.LogWarning("Not enough Marshmallow. Throw canceled.");
                        Destroy(spawnedCatcher.gameObject);
                        spawnedCatcher = null;
                        SetLine(startPosition.position);
                        lineRenderer.enabled = false;
                        playerMovement?.CancelAttack();
                        return;
                    }
                }

                spawnedCatcher.transform.SetParent(null);
                Vector3 a = lineRenderer.GetPosition(0);
                Vector3 b = lineRenderer.GetPosition(1);
                Vector2 dir = (b - a);
                if (dir.sqrMagnitude < 0.001f)
                    dir = transform.localScale.x < 0f ? Vector2.left : Vector2.right;

                spawnedCatcher.throwCatcher(dir, force);
                spawnedCatcher = null;

                // Play catcher throw sound effect
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayCatcherThrowSFX();
                }

                playerMovement?.ReleaseAttack();
            }
            else
            {
                playerMovement?.CancelAttack();
            }

            SetLine(startPosition.position);
            lineRenderer.enabled = false;
        }
    }
    private void DrawLine(Vector2 screenPosition)
    {
        Camera camera = GetGameplayCamera();
        if (camera == null) return;

        Vector3 touchPosition = camera.ScreenToWorldPoint(screenPosition);
        aimingLinePosition = startPosition.position + Vector3.ClampMagnitude(touchPosition - startPosition.position, maxLength);
        SetLine(aimingLinePosition);
    }
    private void DrawVirtualLine(Vector2 dragVector)
    {
        Camera camera = GetGameplayCamera();
        if (camera == null) return;

        Vector3 startScreen = camera.WorldToScreenPoint(startPosition.position);
        Vector3 endScreen = startScreen + new Vector3(dragVector.x, dragVector.y, 0f);
        Vector3 startWorld = camera.ScreenToWorldPoint(startScreen);
        Vector3 endWorld = camera.ScreenToWorldPoint(endScreen);

        aimingLinePosition = startPosition.position + Vector3.ClampMagnitude(endWorld - startWorld, maxLength);
        SetLine(aimingLinePosition);
    }
    private void SetLine(Vector2 position)
    {
        if (!lineRenderer.enabled) lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, position);
        lineRenderer.SetPosition(1, startPosition.position);
    }
    private void SpawnCatcher()
    {
        SetLine(idlePosition.position);
        spawnedCatcher = catcherPrefab != null
            ? Instantiate(catcherPrefab, idlePosition.position, Quaternion.identity)
            : CreateFallbackCatcher(idlePosition.position);
        spawnedCatcher.tamingPanel = tamingUI;
        spawnedCatcher.transform.localScale = catcherPrefab != null ? Vector3.one * 0.15f : Vector3.one;
    }
    private void CatcherRotation()
    {
        Vector3 a = lineRenderer.GetPosition(0);
        Vector3 b = lineRenderer.GetPosition(1);
        Vector2 dir = (b - a).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (spriteFacesRight)
            spawnedCatcher.transform.rotation = Quaternion.Euler(0, 0, angle);
        else
            spawnedCatcher.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    private void ResolveReferences()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponentInChildren<LineRenderer>(true);
        if (startPosition == null)
            startPosition = transform.Find("StartPosition");
        if (idlePosition == null)
            idlePosition = transform.Find("IdlePosition");
        if (aimingarea == null)
            aimingarea = FindAnyObjectByType<aimingArea>(FindObjectsInactive.Include);
        if (tamingUI == null)
            tamingUI = TamingPanelFlow.GetCanonicalPanel();
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (gameplayCamera == null)
            gameplayCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
    }

    private Camera GetGameplayCamera()
    {
        if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled)
            gameplayCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        return gameplayCamera;
    }

    public void ConfigureRuntimeReferences(
        LineRenderer runtimeLineRenderer,
        Transform runtimeStartPosition,
        Transform runtimeIdlePosition,
        ThrowingCatcher runtimeCatcherPrefab,
        PlayerMovement runtimePlayerMovement,
        Camera runtimeGameplayCamera)
    {
        if (lineRenderer == null)
            lineRenderer = runtimeLineRenderer;
        if (startPosition == null)
            startPosition = runtimeStartPosition;
        if (idlePosition == null)
            idlePosition = runtimeIdlePosition;
        if (catcherPrefab == null)
            catcherPrefab = runtimeCatcherPrefab;
        if (playerMovement == null)
            playerMovement = runtimePlayerMovement;
        if (gameplayCamera == null)
            gameplayCamera = runtimeGameplayCamera;

        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    public void SetTamingPanel(GameObject panel)
    {
        if (panel != null)
            tamingUI = panel;
    }

    private ThrowingCatcher CreateFallbackCatcher(Vector3 position)
    {
        var catcherObject = new GameObject("RuntimeSlimeCatcher", typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SpriteRenderer));
        catcherObject.tag = "Catcher";
        catcherObject.transform.position = position;

        var renderer = catcherObject.GetComponent<SpriteRenderer>();
        renderer.sprite = CreateFallbackCatcherSprite();
        renderer.color = new Color(1f, 0.9f, 0.45f, 1f);
        renderer.sortingOrder = 1000;

        return catcherObject.AddComponent<ThrowingCatcher>();
    }

    private Sprite CreateFallbackCatcherSprite()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.name = "RuntimeSlimeCatcherSprite";
        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.45f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - distance + 1f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
