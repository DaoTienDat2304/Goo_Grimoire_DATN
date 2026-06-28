using UnityEngine;
using UnityEngine.InputSystem;

public class aimingArea : MonoBehaviour
{
    [SerializeField] private LayerMask aimingLayerMask;
    public bool isWithinArea()
    {
        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        if (Physics2D.OverlapPoint(worldPosition, aimingLayerMask))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
