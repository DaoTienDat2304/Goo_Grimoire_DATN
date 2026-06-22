using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VirtualJoystickUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;
    private const float LeftControlWidthRatio = 0.45f;
    private const float LowerControlHeightRatio = 0.72f;

    [Header("Scenes")]
    [SerializeField] private string[] activeSceneNames = { "adventureSence" };
    [SerializeField] private bool requirePlayerMovement = true;
    [SerializeField] private bool showInEditor = true;

    [Header("Layout 1920x1080")]
    [SerializeField] private Vector2 fallbackAnchoredPosition = new Vector2(220f, 190f);
    [SerializeField] private float baseSize = 240f;
    [SerializeField] private float knobSize = 92f;
    [SerializeField] private float handleRange = 86f;

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
        if (FindAnyObjectByType<VirtualJoystickUI>() != null)
            return;

        var canvasObject = new GameObject("MobileControlsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystem();
        CreateJoystick(canvasObject.transform);
        MobileThrowButtonUI.Create(canvasObject.transform);
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
        joystick.BuildVisuals();
    }

    private void Awake()
    {
        root = transform as RectTransform;
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
        gameObject.SetActive(showInEditor && activeScene && hasMovement);
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
        baseRect.anchorMin = Vector2.zero;
        baseRect.anchorMax = Vector2.zero;
        baseRect.anchoredPosition = fallbackAnchoredPosition;
        baseObject.SetActive(false);

        var ringObject = CreateCircle("JoystickRing", baseRect, baseSize * 0.72f, ringColor, out joystickRingImage);
        joystickRingImage.raycastTarget = false;

        var knobObject = CreateCircle("JoystickKnob", baseRect, knobSize, knobColor, out joystickKnobImage);
        knob = knobObject.GetComponent<RectTransform>();
        joystickKnobImage.raycastTarget = false;
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
        MoveBaseToFinger(eventData);
        UpdateInput(eventData);
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

    private void MoveBaseToFinger(PointerEventData eventData)
    {
        if (baseRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, eventData.position, eventData.pressEventCamera, out var localPoint))
            return;

        baseRect.gameObject.SetActive(true);
        baseRect.anchoredPosition = localPoint;
        knob.anchoredPosition = Vector2.zero;
    }

    private void UpdateInput(PointerEventData eventData)
    {
        if (baseRect == null || knob == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, eventData.pressEventCamera, out var localPoint))
            return;

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);
        knob.anchoredPosition = clamped;

        MobileInput.VirtualJoystickVector = clamped / handleRange;
        MobileInput.IsVirtualJoystickActive = MobileInput.VirtualJoystickVector.sqrMagnitude > 0.0025f;
    }

    private void ResetInput()
    {
        activePointerId = int.MinValue;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
        if (baseRect != null) baseRect.gameObject.SetActive(false);
        MobileInput.VirtualJoystickVector = Vector2.zero;
        MobileInput.IsVirtualJoystickActive = false;
    }
}
