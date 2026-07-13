using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MobileThrowButtonUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private const float RightControlStartRatio = 0.55f;
    private const float LowerControlHeightRatio = 0.72f;
    private static readonly string[] DefaultActiveSceneNames = { "adventureSence", "Map2", "Frozen_Map", "NonameMap" };

    [Header("Scenes")]
    [SerializeField] private string[] activeSceneNames = DefaultActiveSceneNames;
    [SerializeField] private bool requireAiming = false;
    [SerializeField] private bool showInEditor = true;

    [Header("Responsive Layout")]
    [SerializeField] private Vector2 restPositionRatio = new Vector2(0.76f, 0.26f);
    [SerializeField] private float edgePadding = 24f;
    [SerializeField] private float buttonSize = 152f;
    [SerializeField] private float innerSize = 96f;
    [SerializeField] private float handleRange = 92f;
    [SerializeField] private float inputDeadZone = 12f;
    [SerializeField] private float throwSensitivity = 1f;
    [SerializeField] private bool showButtonAtRest = true;
    [SerializeField] private bool moveCenterToFirstTouch = true;

    [Header("Replaceable UI Images")]
    [SerializeField] private Image throwButtonImage;
    [SerializeField] private Image throwButtonIconImage;

    [Header("Visual")]
    [SerializeField] private Color buttonColor = new Color(1f, 1f, 1f, 0.26f);
    [SerializeField] private Color iconColor = new Color(0.75f, 0.95f, 1f, 0.78f);

    private int activePointerId = int.MinValue;
    private RectTransform root;
    private RectTransform buttonRect;
    private RectTransform iconRect;
    private Vector2 currentDragVector;

    public static MobileThrowButtonUI Create(Transform parent)
    {
        var zone = new GameObject("RightThrowZone", typeof(RectTransform), typeof(Image), typeof(MobileThrowButtonUI));
        zone.transform.SetParent(parent, false);

        var zoneRect = zone.GetComponent<RectTransform>();
        zoneRect.anchorMin = new Vector2(RightControlStartRatio, 0f);
        zoneRect.anchorMax = new Vector2(1f, LowerControlHeightRatio);
        zoneRect.offsetMin = Vector2.zero;
        zoneRect.offsetMax = Vector2.zero;

        var zoneImage = zone.GetComponent<Image>();
        zoneImage.color = new Color(0f, 0f, 0f, 0f);
        zoneImage.raycastTarget = true;

        var ui = zone.GetComponent<MobileThrowButtonUI>();
        ui.activeSceneNames = DefaultActiveSceneNames;
        ui.BuildVisuals();
        return ui;
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
        bool hasAiming = !requireAiming || FindAnyObjectByType<Aiming>() != null;
#if UNITY_EDITOR
        gameObject.SetActive(showInEditor && activeScene && hasAiming);
#else
        gameObject.SetActive(Application.isMobilePlatform && activeScene && hasAiming);
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

        var buttonObject = new GameObject("ThrowBallButton", typeof(RectTransform), typeof(Image));
        buttonObject.transform.SetParent(transform, false);

        buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(buttonSize, buttonSize);
        buttonRect.anchoredPosition = GetRestPosition();
        buttonObject.SetActive(showButtonAtRest);

        throwButtonImage = buttonObject.GetComponent<Image>();
        throwButtonImage.sprite = CreateCircleSprite("ThrowBallButtonSprite");
        throwButtonImage.color = buttonColor;
        throwButtonImage.raycastTarget = false;

        var iconObject = new GameObject("ThrowBallIcon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(buttonRect, false);

        iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(innerSize, innerSize);
        iconRect.anchoredPosition = Vector2.zero;

        throwButtonIconImage = iconObject.GetComponent<Image>();
        throwButtonIconImage.sprite = CreateCircleSprite("ThrowBallIconSprite");
        throwButtonIconImage.color = iconColor;
        throwButtonIconImage.raycastTarget = false;
    }

    private void CacheRects()
    {
        if (root == null)
            root = transform as RectTransform;
        if (buttonRect == null && throwButtonImage != null)
            buttonRect = throwButtonImage.rectTransform;
        if (iconRect == null && throwButtonIconImage != null)
            iconRect = throwButtonIconImage.rectTransform;
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
            MoveButtonToFinger(eventData);
        SetDrag(Vector2.zero, true, true, false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        UpdateDrag(eventData, false, true, false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        UpdateDrag(eventData, false, false, true);
        ResetVisual();
        activePointerId = int.MinValue;
    }

    public void CancelInput()
    {
        ResetInput();
    }

    private void MoveButtonToFinger(PointerEventData eventData)
    {
        CacheRects();

        if (root == null || buttonRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, eventData.position, eventData.pressEventCamera, out var localPoint))
            return;

        buttonRect.gameObject.SetActive(true);
        buttonRect.anchoredPosition = ClampCenterToZone(localPoint);
        if (iconRect != null)
            iconRect.anchoredPosition = Vector2.zero;
    }

    private void UpdateDrag(PointerEventData eventData, bool pressed, bool held, bool released)
    {
        CacheRects();

        if (buttonRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(buttonRect, eventData.position, eventData.pressEventCamera, out var localPoint))
            return;

        if (localPoint.magnitude < inputDeadZone)
            localPoint = Vector2.zero;

        SetDrag(Vector2.ClampMagnitude(localPoint, handleRange), pressed, held, released);
    }

    private void SetDrag(Vector2 clampedLocalPoint, bool pressed, bool held, bool released)
    {
        currentDragVector = clampedLocalPoint;
        if (iconRect != null)
            iconRect.anchoredPosition = currentDragVector;

        MobileInput.VirtualAimPointerPosition = buttonRect != null
            ? RectTransformUtility.WorldToScreenPoint(null, buttonRect.position)
            : Vector2.zero;
        MobileInput.VirtualAimDragVector = currentDragVector * throwSensitivity;
        MobileInput.VirtualAimPressed = pressed;
        MobileInput.VirtualAimHeld = held;
        MobileInput.VirtualAimReleased = released;
    }

    private void ResetVisual()
    {
        currentDragVector = Vector2.zero;
        if (iconRect != null)
            iconRect.anchoredPosition = Vector2.zero;
        if (buttonRect != null)
        {
            buttonRect.anchoredPosition = GetRestPosition();
            buttonRect.gameObject.SetActive(showButtonAtRest);
        }
    }

    private void ResetInput()
    {
        activePointerId = int.MinValue;
        ResetVisual();
        MobileInput.VirtualAimDragVector = Vector2.zero;
        MobileInput.VirtualAimPressed = false;
        MobileInput.VirtualAimHeld = false;
        MobileInput.VirtualAimReleased = false;
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
        float radius = buttonSize * 0.5f;
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
