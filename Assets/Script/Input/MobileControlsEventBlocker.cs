using UnityEngine;
using UnityEngine.UI;

public class MobileControlsEventBlocker : MonoBehaviour
{
    private GraphicRaycaster[] raycasters;
    private showteam[] teamPanels;
    private AdventureBag[] tameInventories;
    private bool wasBlocked;

    private void Awake()
    {
        CacheRaycasters();
        CacheBlockingPanels();
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

    private void CacheBlockingPanels()
    {
        teamPanels = FindObjectsByType<showteam>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        tameInventories = FindObjectsByType<AdventureBag>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
        if (teamPanels == null || tameInventories == null)
            CacheBlockingPanels();

        foreach (var teamPanel in teamPanels)
        {
            if (teamPanel != null && teamPanel.IsOpen)
                return true;
        }

        foreach (var tameInventory in tameInventories)
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
