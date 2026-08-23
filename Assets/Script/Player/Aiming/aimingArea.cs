using UnityEngine;

public class aimingArea : MonoBehaviour
{
    [SerializeField] private LayerMask aimingLayerMask;
    [SerializeField] private Camera gameplayCamera;

    public bool isWithinArea(Vector2 screenPosition)
    {
        Camera camera = GetGameplayCamera();
        if (camera == null) return false;

        Vector2 worldPosition = camera.ScreenToWorldPoint(screenPosition);
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

    private Camera GetGameplayCamera()
    {
        if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled)
            gameplayCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        return gameplayCamera;
    }
}
