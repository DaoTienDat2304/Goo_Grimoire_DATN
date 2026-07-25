using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tiện ích UI dùng chung cho Thành tựu &amp; Nhiệm vụ: phủ một panel đen mờ lên 1 dòng đã
/// hoàn thành để "che mờ" nó (tạo/toggle bằng code, không cần sửa prefab).
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
                img.raycastTarget = false; // không chặn cuộn/nhấn
            }
            else
            {
                ov = existing.gameObject;
                ov.SetActive(true);
            }
            ov.transform.SetAsLastSibling(); // trên cùng để che các phần tử dưới
        }
        else if (existing != null)
        {
            existing.gameObject.SetActive(false);
        }
    }
}
