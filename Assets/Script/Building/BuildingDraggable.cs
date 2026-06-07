using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Building building;

    private Transform originalParent;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 dragOffset;
    private bool isDraggable = true; // Mặc định có thể kéo

    public void SetBuilding(Building b)
    {
        building = b;
    }

    /// <summary>
    /// Đặt trạng thái có thể kéo hay không
    /// </summary>
    public void SetDraggable(bool draggable)
    {
        isDraggable = draggable;
        
        // Disable/enable CanvasGroup để ngăn tương tác khi không thể kéo
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = draggable;
            canvasGroup.interactable = draggable;
        }
    }

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        rectTransform = transform as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Không cho phép kéo nếu đã disable
        if (!isDraggable) return;

        originalParent = transform.parent;
        transform.SetParent(rootCanvas.transform, true);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;

        // Use the event camera and track offset so icon stays under cursor
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint))
        {
            dragOffset = rectTransform.anchoredPosition - localPoint;
            rectTransform.anchoredPosition = localPoint + dragOffset;
        }
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Không cho phép kéo nếu đã disable
        if (!isDraggable) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint))
        {
            rectTransform.anchoredPosition = localPoint + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Không cho phép kéo nếu đã disable
        if (!isDraggable) return;

        canvasGroup.blocksRaycasts = isDraggable; // Sử dụng giá trị isDraggable
        canvasGroup.alpha = 1f;
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = Vector2.zero;
    }
}
