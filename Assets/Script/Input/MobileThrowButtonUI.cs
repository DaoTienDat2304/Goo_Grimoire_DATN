using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MobileThrowButtonUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private const float RightControlStartRatio = 0.55f;
    private const float LowerControlHeightRatio = 0.72f;

    [Header("Scenes")]
    [SerializeField] private string[] activeSceneNames = { "adventureSence" };
    [SerializeField] private bool requireAiming = true;
    [SerializeField] private bool showInEditor = true;

    [Header("Layout 1920x1080")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(-220f, 190f);
    [SerializeField] private float buttonSize = 152f;
    [SerializeField] private float innerSize = 96f;

    [Header("Replaceable UI Images")]
    [SerializeField] private Image throwButtonImage;
    [SerializeField] private Image throwButtonIconImage;

    [Header("Visual")]
    [SerializeField] private Color buttonColor = new Color(1f, 1f, 1f, 0.26f);
    [SerializeField] private Color iconColor = new Color(0.75f, 0.95f, 1f, 0.78f);

    private int activePointerId = int.MinValue;

    public static MobileThrowButtonUI Create(Transform parent)
    {
        var zone = new GameObject("RightThrowZone", typeof(RectTransform), typeof(Image));
        zone.transform.SetParent(parent, false);

        var zoneRect = zone.GetComponent<RectTransform>();
        zoneRect.anchorMin = new Vector2(RightControlStartRatio, 0f);
        zoneRect.anchorMax = new Vector2(1f, LowerControlHeightRatio);
        zoneRect.offsetMin = Vector2.zero;
        zoneRect.offsetMax = Vector2.zero;

        var zoneImage = zone.GetComponent<Image>();
        zoneImage.color = new Color(0f, 0f, 0f, 0f);
        zoneImage.raycastTarget = false;

        var button = new GameObject("ThrowBallButton", typeof(RectTransform), typeof(Image), typeof(MobileThrowButtonUI));
        button.transform.SetParent(zone.transform, false);

        var ui = button.GetComponent<MobileThrowButtonUI>();
        ui.BuildVisuals();
        return ui;
    }

    private void Awake()
    {
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
        var rect = transform as RectTransform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(buttonSize, buttonSize);
        rect.anchoredPosition = anchoredPosition;

        throwButtonImage = GetComponent<Image>();
        throwButtonImage.sprite = CreateCircleSprite("ThrowBallButtonSprite");
        throwButtonImage.color = buttonColor;
        throwButtonImage.raycastTarget = true;

        var iconObject = new GameObject("ThrowBallIcon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(transform, false);

        var iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(innerSize, innerSize);
        iconRect.anchoredPosition = Vector2.zero;

        throwButtonIconImage = iconObject.GetComponent<Image>();
        throwButtonIconImage.sprite = CreateCircleSprite("ThrowBallIconSprite");
        throwButtonIconImage.color = iconColor;
        throwButtonIconImage.raycastTarget = false;
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
        SetVirtualAim(eventData.position, true, true, false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        SetVirtualAim(eventData.position, false, true, false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        SetVirtualAim(eventData.position, false, false, true);
        activePointerId = int.MinValue;
    }

    private void SetVirtualAim(Vector2 screenPosition, bool pressed, bool held, bool released)
    {
        MobileInput.VirtualAimPointerPosition = screenPosition;
        MobileInput.VirtualAimPressed = pressed;
        MobileInput.VirtualAimHeld = held;
        MobileInput.VirtualAimReleased = released;
    }

    private void ResetInput()
    {
        activePointerId = int.MinValue;
        MobileInput.VirtualAimPressed = false;
        MobileInput.VirtualAimHeld = false;
        MobileInput.VirtualAimReleased = false;
    }
}
