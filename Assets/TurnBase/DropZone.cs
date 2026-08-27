using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public static Transform currentDropZone = null;
    public bool isOccupied = false; // Keep the variable to prevent compilation errors in other scripts if any

    public void OnPointerEnter(PointerEventData eventData)
    {
        currentDropZone = this.transform;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentDropZone == this.transform)
        {
            currentDropZone = null;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        SlimeDragHandler dragHandler = draggedObject.GetComponent<SlimeDragHandler>();
        if (dragHandler == null) return;

        // If this zone is already occupied, kick the existing slime back to unusedSlime
        SlimeDragHandler existingSlime = GetComponentInChildren<SlimeDragHandler>();
        if (existingSlime != null && existingSlime.gameObject != draggedObject)
        {
            existingSlime.transform.SetParent(existingSlime.unusedSlime, false);
            existingSlime.transform.localScale = Vector3.one;
            existingSlime.transform.localPosition = Vector3.zero;
            existingSlime.isUsed = false;
        }

        // Set the parent of the dragged object to this drop zone
        dragHandler.SetNewParent(this.transform);
        isOccupied = true;
    }

    private void Update()
    {
        // Keep isOccupied in sync with child presence dynamically to be safe
        isOccupied = (GetComponentInChildren<SlimeDragHandler>() != null);
    }
}
