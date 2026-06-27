using UnityEngine;

public class aimingArea : MonoBehaviour
{
    [SerializeField] private LayerMask aimingLayerMask;
    public bool isWithinArea(Vector2 screenPosition)
    {
        if (Camera.main == null) return false;

        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        if (Physics2D.OverlapPoint(worldPosition, aimingLayerMask))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool isWithinArea()
    {
        return MobileInput.TryGetAimPointer(out var screenPosition, out _, out _, out _) && isWithinArea(screenPosition);
    }
}
