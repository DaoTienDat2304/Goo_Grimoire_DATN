using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VirtualJoystickUI : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler
{
    private const string EditorPrefabPath = "Assets/UI/prefab/MobileControlsCanvas.prefab";
    private const string ResourcesPrefabPath = "UI/MobileControlsCanvas";
    private const int MobileControlsSortingOrder = 5000;
    private const float LeftControlWidthRatio = 0.45f;
    private const float LowerControlHeightRatio = 0.72f;
    private static readonly string[] DefaultActiveSceneNames =
    {
        "adventureSence",
        "travelSence",
        "Map1_IceMap",
        "Map2_Fantasymap",
        "Map3_DungeonMap",
        "Map2",
        "Frozen_Map",
        "NonameMap"
    };

    [Header("Scenes")]
    [SerializeField] private string[] activeSceneNames = DefaultActiveSceneNames;
    [SerializeField] private bool requirePlayerMovement = false;
    [SerializeField] private bool showInEditor = true;

    [Header("Responsive Layout")]
    [SerializeField] private Vector2 restPositionRatio = new Vector2(0.24f, 0.26f);
    [SerializeField] private float edgePadding = 24f;
    [SerializeField] private float baseSize = 240f;
    [SerializeField] private float knobSize = 92f;
    [SerializeField] private float handleRange = 86f;
    [SerializeField] private float inputDeadZone = 6f;
    [SerializeField] private float movementSensitivity = 1f;
    [SerializeField] private bool showJoystickAtRest = true;
    [SerializeField] private bool moveCenterToFirstTouch = true;

    [Header("Replaceable UI Images")]
    [SerializeField] private Image movementZoneImage;
    [SerializeField] private Image joystickBaseImage;
    [SerializeField] private Image joystickRingImage;
    [SerializeField] private Image joystickKnobImage;

    [Header("Visual")]
    [SerializeField] private Color zoneColor = new Color(0f, 0f, 0f, 0.035f);
    [SerializeField] private Color baseColor = new Color(1f, 1f, 1f, 0.22f);
    [SerializeField] private Color ringColor = new Color(1f, 1f, 1f, 0.34f);
    [SerializeField] private Color knobColor = new Color(1f, 1f, 1f, 0.58f);

    private RectTransform root;
    private RectTransform baseRect;
    private RectTransform knob;
    private int activePointerId = int.MinValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureControlsExist()
    {
        EnsureEventSystem();

        var existingJoystick = FindAnyObjectByType<VirtualJoystickUI>();
        if (existingJoystick != null)
        {
            var existingCanvas = existingJoystick.GetComponentInParent<Canvas>();
            if (existingCanvas != null)
            {
                ConfigureControlsCanvas(existingCanvas);
                EnsureEventBlocker(existingCanvas.gameObject);
            }
            return;
        }

        if (TryCreateControlsFromPrefab())
            return;

        var canvasObject = new GameObject("MobileControlsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);
        EnsureEventBlocker(canvasObject);

        var canvas = canvasObject.GetComponent<Canvas>();
        ConfigureControlsCanvas(canvas);

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CreateJoystick(canvasObject.transform);
        MobileThrowButtonUI.Create(canvasObject.transform);
    }

    private static bool TryCreateControlsFromPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>(ResourcesPrefabPath);

#if UNITY_EDITOR
        if (prefab == null)
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(EditorPrefabPath);
#endif

        if (prefab == null)
            return false;

        GameObject instance = Instantiate(prefab);
        instance.name = prefab.name;
        DontDestroyOnLoad(instance);
        var canvas = instance.GetComponent<Canvas>();
        if (canvas != null)
        {
            ConfigureControlsCanvas(canvas);
            ConfigureControlsScaler(canvas);
        }
        EnsureEventBlocker(instance);
        return true;
    }

    private static void ConfigureControlsCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = MobileControlsSortingOrder;
    }

    private static void ConfigureControlsScaler(Canvas canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static void EnsureEventBlocker(GameObject controlsCanvas)
    {
        if (controlsCanvas.GetComponent<MobileControlsEventBlocker>() == null)
            controlsCanvas.AddComponent<MobileControlsEventBlocker>();
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

#if ENABLE_INPUT_SYSTEM
        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
#else
        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
        DontDestroyOnLoad(eventSystemObject);
    }

    private static void CreateJoystick(Transform parent)
    {
        var zone = new GameObject("LeftMovementZone", typeof(RectTransform), typeof(Image), typeof(VirtualJoystickUI));
        zone.transform.SetParent(parent, false);

        var zoneRect = zone.GetComponent<RectTransform>();
        zoneRect.anchorMin = Vector2.zero;
        zoneRect.anchorMax = new Vector2(LeftControlWidthRatio, LowerControlHeightRatio);
        zoneRect.offsetMin = Vector2.zero;
        zoneRect.offsetMax = Vector2.zero;

        var joystick = zone.GetComponent<VirtualJoystickUI>();
        joystick.activeSceneNames = DefaultActiveSceneNames;
        joystick.BuildVisuals();
    }

    private void Awake()
    {
        root = transform as RectTransform;
        CacheRects();
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnEnable()
    {
        UpdateVisibility();
    }

    private void Update()
    {
        UpdateVisibility();
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        ResetInput();
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        bool activeScene = IsActiveScene(SceneManager.GetActiveScene().name);
        bool hasMovement = !requirePlayerMovement || FindAnyObjectByType<PlayerMovement>() != null;
#if UNITY_EDITOR
        gameObject.SetActive(Application.isPlaying && showInEditor && activeScene && hasMovement);
#else
        gameObject.SetActive(Application.isMobilePlatform && activeScene && hasMovement);
#endif
    }

    private bool IsActiveScene(string sceneName)
    {
        if (activeSceneNames == null || activeSceneNames.Length == 0)
            return true;

        foreach (var activeSceneName in activeSceneNames)
        {
            if (sceneName == activeSceneName)
                return true;
        }

        return false;
    }

    private void BuildVisuals()
    {
        root = transform as RectTransform;
        movementZoneImage = GetComponent<Image>();
        movementZoneImage.color = zoneColor;
        movementZoneImage.raycastTarget = true;

        var baseObject = CreateCircle("JoystickBase", root, baseSize, baseColor, out joystickBaseImage);
        baseRect = baseObject.GetComponent<RectTransform>();
        baseRect.anchorMin = new Vector2(0.5f, 0.5f);
        baseRect.anchorMax = new Vector2(0.5f, 0.5f);
        baseRect.anchoredPosition = GetRestPosition();
        baseObject.SetActive(showJoystickAtRest);

        var ringObject = CreateCircle("JoystickRing", baseRect, baseSize * 0.72f, ringColor, out joystickRingImage);
        joystickRingImage.raycastTarget = false;

        var knobObject = CreateCircle("JoystickKnob", baseRect, knobSize, knobColor, out joystickKnobImage);
        knob = knobObject.GetComponent<RectTransform>();
        joystickKnobImage.raycastTarget = false;
    }

    private void CacheRects()
    {
        if (root == null)
            root = transform as RectTransform;
        if (baseRect == null && joystickBaseImage != null)
            baseRect = joystickBaseImage.rectTransform;
        if (knob == null && joystickKnobImage != null)
            knob = joystickKnobImage.rectTransform;
    }

    private GameObject CreateCircle(string objectName, Transform parent, float size, Color color, out Image image)
    {
        var obj = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);

        var rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        image = obj.GetComponent<Image>();
        image.sprite = CreateCircleSprite(objectName + "Sprite");
        image.color = color;

        return obj;
    }

    private Sprite CreateCircleSprite(string spriteName)
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.name = spriteName;

        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;
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

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != int.MinValue)
            return;

        activePointerId = eventData.pointerId;
        if (moveCenterToFirstTouch)
            MoveBaseToFinger(eventData);
        else
            UpdateInput(eventData);
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        UpdateInput(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        ResetInput();
    }

    public void CancelInput()
    {
        ResetInput();
    }

    private void MoveBaseToFinger(PointerEventData eventData)
    {
        CacheRects();

        if (baseRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, eventData.position, eventData.pressEventCamera, out var localPoint))
            return;

        baseRect.gameObject.SetActive(true);
        baseRect.anchoredPosition = ClampCenterToZone(localPoint);
        knob.anchoredPosition = Vector2.zero;
    }

    private void UpdateInput(PointerEventData eventData)
    {
        CacheRects();

        if (baseRect == null || knob == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, eventData.pressEventCamera, out var localPoint))
            return;

        if (localPoint.magnitude < inputDeadZone)
            localPoint = Vector2.zero;

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);
        SetInput(clamped);
    }

    private void SetInput(Vector2 clampedLocalPoint)
    {
        if (knob != null)
            knob.anchoredPosition = clampedLocalPoint;

        MobileInput.VirtualJoystickVector = Vector2.ClampMagnitude((clampedLocalPoint / handleRange) * movementSensitivity, 1f);
        MobileInput.IsVirtualJoystickActive = MobileInput.VirtualJoystickVector.sqrMagnitude > 0.0025f;
    }

    private void ResetInput()
    {
        activePointerId = int.MinValue;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
        if (baseRect != null)
        {
            baseRect.anchoredPosition = GetRestPosition();
            baseRect.gameObject.SetActive(showJoystickAtRest);
        }
        MobileInput.VirtualJoystickVector = Vector2.zero;
        MobileInput.IsVirtualJoystickActive = false;
    }

    private Vector2 GetRestPosition()
    {
        CacheRects();

        if (root == null)
            return Vector2.zero;

        Rect rect = root.rect;
        var target = new Vector2(
            rect.xMin + rect.width * restPositionRatio.x,
            rect.yMin + rect.height * restPositionRatio.y
        );
        return ClampCenterToZone(target);
    }

    private Vector2 ClampCenterToZone(Vector2 localPoint)
    {
        CacheRects();

        if (root == null)
            return localPoint;

        Rect rect = root.rect;
        float radius = baseSize * 0.5f;
        float minX = rect.xMin + radius + edgePadding;
        float maxX = rect.xMax - radius - edgePadding;
        float minY = rect.yMin + radius + edgePadding;
        float maxY = rect.yMax - radius - edgePadding;

        if (minX > maxX)
        {
            minX = rect.xMin;
            maxX = rect.xMax;
        }

        if (minY > maxY)
        {
            minY = rect.yMin;
            maxY = rect.yMax;
        }

        return new Vector2(
            Mathf.Clamp(localPoint.x, minX, maxX),
            Mathf.Clamp(localPoint.y, minY, maxY)
        );
    }
}
