using Spine;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public static Transform currentDropZone = null;
    public bool isOccupied = false;
    public void OnPointerEnter(PointerEventData eventData)
    {
        currentDropZone = this.transform;
        Debug.Log(currentDropZone.name);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentDropZone == this.transform) currentDropZone = null;
        Debug.Log("out");
        if (this.GetComponentInChildren<SlimeDragHandler>() == null)
        {
            isOccupied = false;
        }
    }
    public void OnDrop(PointerEventData eventData)
    {
        if (!isOccupied) isOccupied = true;
        else
        {
            var curSlime = this.GetComponentInChildren<SlimeDragHandler>();
            curSlime.transform.SetParent(curSlime.unusedSlime, true);
            curSlime.transform.position = curSlime.unusedSlime.position;
            curSlime.isUsed = false;
        }
    }
}


