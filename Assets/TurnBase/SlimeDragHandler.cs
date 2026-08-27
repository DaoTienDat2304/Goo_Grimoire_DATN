using Spine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class SlimeDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Slime slime;
    public Transform unusedSlime;
    public bool isUsed = false;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 dragOffset;
    public GameObject armor;
    public GameObject weapon;

    private Transform targetParent;

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
        if (!enabled) return;

        // By default, if drop is not captured by any DropZone, return to unusedSlime
        targetParent = unusedSlime;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;

        // Reparent to canvas to draw on top
        transform.SetParent(rootCanvas.transform, true);

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
        if (!enabled) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint))
        {
            rectTransform.anchoredPosition = localPoint + dragOffset;
        }

        // If hovered over a valid dropzone, update the target parent
        if (DropZone.currentDropZone != null)
        {
            targetParent = DropZone.currentDropZone;
        }
        else
        {
            targetParent = unusedSlime;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!enabled) return;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Set parent with worldPositionStays = false to prevent scale distortion
        transform.SetParent(targetParent, false);
        transform.localScale = Vector3.one * 1.3f;
        transform.localPosition = Vector3.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        isUsed = (targetParent != unusedSlime);

        // Hide or show the original placeholder graphics in the Member panel slot
        if (unusedSlime != null)
        {
            Member member = unusedSlime.GetComponent<Member>();
            if (member != null)
            {
                if (member.body != null) member.body.SetActive(!isUsed);
                if (member.armor != null) member.armor.SetActive(!isUsed);
                if (member.weapon != null) member.weapon.SetActive(!isUsed);
            }
        }
        
        var combatAnim = GetComponent<SimpleCombatAnimation>();
        if (combatAnim != null)
        {
            combatAnim.CheckAndFixScale();
            combatAnim.OnDroppedToFormation();
        }
    }

    public void SetNewParent(Transform newParent)
    {
        targetParent = newParent;
    }
}
