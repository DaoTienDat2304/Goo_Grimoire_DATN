using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MobileUIFeedback : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler,
    IPointerClickHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [Header("Motion")]
    [SerializeField] private bool useMotion = true;
    [SerializeField] private float pressedScale = 0.97f;
    [SerializeField] private float hoverScale = 1.01f;
    [SerializeField] private float selectedScale = 1.008f;
    [SerializeField] private float scaleDuration = 0.14f;
    [SerializeField] private float releaseBounceScale = 1.012f;

    [Header("Visuals")]
    [SerializeField] private bool addShadow = true;
    [SerializeField] private bool useRipple = true;
    [SerializeField] private Color rippleColor = new Color(1f, 1f, 1f, 0.34f);

    [Header("Audio")]
    [SerializeField] private bool playSound = true;
    [SerializeField] private float soundCooldown = 0.04f;

    private Selectable selectable;
    private RectTransform rectTransform;
    private Graphic targetGraphic;
    private Shadow shadow;
    private Vector3 initialScale;
    private Coroutine scaleRoutine;
    private bool pointerInside;
    private bool pointerDown;
    private bool selected;
    private bool touchPointerActive;
    private bool textInput;
    private float lastSoundTime;

    private static Sprite circleSprite;

    public void SetRippleEnabled(bool enabled)
    {
        useRipple = enabled;
    }

    public void SetMotionEnabled(bool enabled)
    {
        useMotion = enabled;
        if (enabled) return;

        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        if (initialScale != Vector3.zero)
            transform.localScale = initialScale;
    }

    public void ConfigureForTextInput(bool enabled)
    {
        textInput = enabled;
        if (enabled)
            useRipple = false;
    }

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        rectTransform = transform as RectTransform;
        targetGraphic = selectable != null ? selectable.targetGraphic : GetComponent<Graphic>();
        initialScale = transform.localScale;

        if (addShadow && targetGraphic != null && GetComponent<Shadow>() == null)
        {
            shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.12f);
            shadow.effectDistance = new Vector2(0f, -2f);
            shadow.useGraphicAlpha = true;
        }
        else
        {
            shadow = GetComponent<Shadow>();
        }
    }

    private void OnDisable()
    {
        pointerInside = false;
        pointerDown = false;
        selected = false;
        touchPointerActive = false;
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);
        transform.localScale = initialScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanInteract()) return;
        pointerDown = true;
        touchPointerActive = IsTouchPointer(eventData);
        if (useMotion)
            AnimateScale(textInput ? 1f : pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanInteract()) return;
        pointerDown = false;
        if (touchPointerActive)
            pointerInside = false;
        if (useMotion)
            AnimateScale(GetRestScale());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanInteract()) return;
        pointerInside = true;
        if (useMotion && !pointerDown)
            AnimateScale(textInput && selected ? selectedScale : hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        if (!CanInteract()) return;
        if (useMotion && !pointerDown)
            AnimateScale(textInput && selected ? selectedScale : 1f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CanInteract()) return;
        PlayFeedbackSound();
        if (IsTouchPointer(eventData))
            pointerInside = false;
        if (useMotion && !textInput)
            StartCoroutine(ClickBounce());
        if (useRipple && !textInput)
            SpawnRipple(eventData.position);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!CanInteract()) return;
        selected = true;
        if (useMotion && !pointerDown)
            AnimateScale(textInput ? selectedScale : GetRestScale());
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        if (useMotion && !pointerDown)
            AnimateScale(pointerInside ? hoverScale : 1f);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (!CanInteract()) return;
        PlayFeedbackSound();
        if (useMotion && !textInput)
            StartCoroutine(ClickBounce());
        if (useRipple && !textInput)
            SpawnRipple(null);
    }

    private bool CanInteract()
    {
        return gameObject.activeInHierarchy && (selectable == null || selectable.IsInteractable());
    }

    private void AnimateScale(float scale)
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleTo(initialScale * scale, scaleDuration));
    }

    private IEnumerator ClickBounce()
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        yield return ScaleTo(initialScale * releaseBounceScale, 0.1f);
        yield return ScaleTo(initialScale * GetRestScale(), 0.16f);
        scaleRoutine = null;
    }

    private float GetRestScale()
    {
        if (textInput && selected)
            return selectedScale;
        if (touchPointerActive)
            return 1f;
        return pointerInside ? hoverScale : 1f;
    }

    private IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 start = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // SmootherStep có vận tốc bằng 0 ở cả hai đầu, tránh cảm giác giật
            // khi chuyển giữa nhấn, thả và focus.
            t = t * t * t * (t * (6f * t - 15f) + 10f);
            transform.localScale = Vector3.LerpUnclamped(start, target, t);
            yield return null;
        }

        transform.localScale = target;
    }

    private void PlayFeedbackSound()
    {
        if (!playSound || Time.unscaledTime - lastSoundTime < soundCooldown)
            return;

        lastSoundTime = Time.unscaledTime;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClickSFX();
    }

    private void SpawnRipple(Vector2? screenPosition)
    {
        if (rectTransform == null)
            return;

        var rippleObject = new GameObject("UIRipple", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rippleObject.transform.SetParent(transform, false);
        rippleObject.transform.SetAsFirstSibling();

        var rippleRect = (RectTransform)rippleObject.transform;
        var image = rippleObject.GetComponent<Image>();
        image.sprite = GetCircleSprite();
        image.color = rippleColor;
        image.raycastTarget = false;

        Vector2 localPoint = Vector2.zero;
        if (screenPosition.HasValue)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition.Value, null, out localPoint);

        rippleRect.anchoredPosition = localPoint;
        float size = Mathf.Max(rectTransform.rect.width, rectTransform.rect.height) * 1.35f;
        rippleRect.sizeDelta = Vector2.one * Mathf.Max(size, 28f);

        StartCoroutine(RippleRoutine(rippleObject, image, rippleRect));
    }

    private static bool IsTouchPointer(PointerEventData eventData)
    {
        return eventData != null && eventData.pointerId >= 0;
    }

    private IEnumerator RippleRoutine(GameObject rippleObject, Image image, RectTransform rippleRect)
    {
        float duration = 0.28f;
        float elapsed = 0f;
        Color startColor = image.color;
        Vector3 startScale = Vector3.one * 0.18f;
        Vector3 endScale = Vector3.one;
        rippleRect.localScale = startScale;

        while (elapsed < duration && rippleObject != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rippleRect.localScale = Vector3.LerpUnclamped(startScale, endScale, 1f - Mathf.Pow(1f - t, 2f));
            image.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            yield return null;
        }

        if (rippleObject != null)
            Destroy(rippleObject);
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Generated_UI_Ripple_Circle";
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (distance - radius + 2f) / 2f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }
}
