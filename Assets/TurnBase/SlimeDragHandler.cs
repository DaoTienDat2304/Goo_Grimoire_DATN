using Spine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class SlimeDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Slime slime;
    private Transform originalParent;
    public Transform unusedSlime;
    public bool isUsed = false;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 dragOffset;
    public GameObject armor;
    public GameObject weapon;
    void Start()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        rectTransform = transform as RectTransform;
        unusedSlime = transform.parent;
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
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
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint))
        {
            rectTransform.anchoredPosition = localPoint + dragOffset;
        }
        if (DropZone.currentDropZone != null)
        {
            originalParent = DropZone.currentDropZone;
            Debug.Log(originalParent.name);
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = Vector2.zero;
        if (originalParent != unusedSlime) isUsed = true;
        
        // Cập nhật formation position cho combat animation
        var combatAnim = GetComponent<SimpleCombatAnimation>();
        if (combatAnim != null)
        {
            combatAnim.OnDroppedToFormation();
        }
    }
}


