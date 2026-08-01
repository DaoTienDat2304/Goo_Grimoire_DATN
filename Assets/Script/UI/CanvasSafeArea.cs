using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps the direct UI groups of a screen-space canvas inside the phone's safe
/// area. Full-screen, non-interactive artwork is left edge-to-edge.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class CanvasSafeArea : MonoBehaviour
{
    private readonly Dictionary<RectTransform, Anchors> originalAnchors = new();
    private Canvas canvas;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private struct Anchors
    {
        public Vector2 min;
        public Vector2 max;
    }

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        CaptureChildren();
        Apply(true);
    }

    private void OnEnable()
    {
        Apply(true);
    }

    private void OnTransformChildrenChanged()
    {
        CaptureChildren();
        Apply(true);
    }

    private void Update()
    {
        Apply(false);
    }

    private void CaptureChildren()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i) is not RectTransform rect || ShouldIgnore(rect))
                continue;

            if (!originalAnchors.ContainsKey(rect))
            {
                originalAnchors.Add(rect, new Anchors
                {
                    min = rect.anchorMin,
                    max = rect.anchorMax
                });
            }
        }
    }

    private void Apply(bool force)
    {
        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace || Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safe = Screen.safeArea;
        var screenSize = new Vector2Int(Screen.width, Screen.height);
        if (!force && safe == lastSafeArea && screenSize == lastScreenSize)
            return;

        lastSafeArea = safe;
        lastScreenSize = screenSize;

        Vector2 safeMin = safe.position;
        Vector2 safeMax = safe.position + safe.size;
        safeMin.x /= Screen.width;
        safeMin.y /= Screen.height;
        safeMax.x /= Screen.width;
        safeMax.y /= Screen.height;
        Vector2 safeSize = safeMax - safeMin;

        CaptureChildren();
        foreach (var pair in originalAnchors)
        {
            RectTransform rect = pair.Key;
            if (rect == null)
                continue;

            rect.anchorMin = safeMin + Vector2.Scale(pair.Value.min, safeSize);
            rect.anchorMax = safeMin + Vector2.Scale(pair.Value.max, safeSize);
        }
    }

    private static bool ShouldIgnore(RectTransform rect)
    {
        // Preserve edge-to-edge decorative backgrounds. Interactive groups and
        // panels still get safe-area anchors even when they stretch.
        bool fullStretch = rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one;
        bool interactive = rect.GetComponentInChildren<UnityEngine.UI.Selectable>(true) != null;
        string lowerName = rect.name.ToLowerInvariant();
        bool looksDecorative = lowerName.Contains("background") || lowerName == "bg" || lowerName.Contains("backdrop");
        return fullStretch && looksDecorative && !interactive;
    }
}
