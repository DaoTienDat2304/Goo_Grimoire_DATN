using UnityEngine;
using UnityEngine.UI;

public class MobileControlsEventBlocker : MonoBehaviour
{
    private GraphicRaycaster[] raycasters;
    private bool wasBlocked;

    private void Awake()
    {
        CacheRaycasters();
    }

    private void OnEnable()
    {
        UpdateRaycastState();
    }

    private void Update()
    {
        UpdateRaycastState();
    }

    private void CacheRaycasters()
    {
        raycasters = GetComponentsInChildren<GraphicRaycaster>(true);
    }

    private void UpdateRaycastState()
    {
        if (raycasters == null || raycasters.Length == 0)
            CacheRaycasters();

        bool blocked = IsAnyInventoryPanelOpen();
        if (blocked == wasBlocked)
            return;

        foreach (var raycaster in raycasters)
        {
            if (raycaster != null)
                raycaster.enabled = !blocked;
        }

        if (blocked)
            CancelMobileInput();

        wasBlocked = blocked;
    }

    private bool IsAnyInventoryPanelOpen()
    {
        foreach (var teamPanel in FindObjectsByType<showteam>(FindObjectsSortMode.None))
        {
            if (teamPanel != null && teamPanel.IsOpen)
                return true;
        }

        foreach (var tameInventory in FindObjectsByType<AdventureBag>(FindObjectsSortMode.None))
        {
            if (tameInventory != null && tameInventory.IsOpen)
                return true;
        }

        return false;
    }

    private void CancelMobileInput()
    {
        foreach (var joystick in GetComponentsInChildren<VirtualJoystickUI>(true))
            joystick.CancelInput();
        foreach (var throwButton in GetComponentsInChildren<MobileThrowButtonUI>(true))
            throwButton.CancelInput();

        MobileInput.ResetVirtualControls();
    }
}
