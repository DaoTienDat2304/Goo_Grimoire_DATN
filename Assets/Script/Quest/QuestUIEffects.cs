using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// </summary>
public static class QuestUIEffects
{
    private const string OverlayName = "__DimOverlay";

    public static void SetDimmed(GameObject row, bool dimmed)
    {
        if (row == null) return;

        var existing = row.transform.Find(OverlayName);

        if (dimmed)
        {
            GameObject ov;
            if (existing == null)
            {
                ov = new GameObject(OverlayName, typeof(RectTransform), typeof(Image));
                ov.transform.SetParent(row.transform, false);

                var rt = ov.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var img = ov.GetComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0.55f);
                img.raycastTarget = false;
            }
            else
            {
                ov = existing.gameObject;
                ov.SetActive(true);
            }
            ov.transform.SetAsLastSibling();
        }
        else if (existing != null)
        {
            existing.gameObject.SetActive(false);
        }
    }
}
