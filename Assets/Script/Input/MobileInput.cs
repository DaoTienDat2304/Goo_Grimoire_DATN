using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public enum MobileDirection
{
    None,
    Right,
    Up,
    Left,
    Down
}

public static class MobileInput
{
    private const float DefaultJoystickRadius = 120f;
    private const float SwipeThreshold = 45f;
    private const float RightAimStartRatio = 0.55f;
    private const float LowerAimHeightRatio = 0.72f;
    private static MobileDirection queuedDirection = MobileDirection.None;
    private static bool fallbackAimActive;
    private static Vector2 fallbackAimStartPosition;

    public static Vector2 VirtualJoystickVector { get; set; }
    public static bool IsVirtualJoystickActive { get; set; }

    public static Vector2 VirtualAimPointerPosition { get; set; }
    public static bool VirtualAimPressed { get; set; }
    public static bool VirtualAimHeld { get; set; }
    public static bool VirtualAimReleased { get; set; }
    public static Vector2 VirtualAimDragVector { get; set; }
    public static bool LastAimPointerFromVirtualButton { get; private set; }

    public static Vector2 GetMovementVector(float joystickRadius = DefaultJoystickRadius)
    {
        if (IsVirtualJoystickActive)
            return Vector2.ClampMagnitude(VirtualJoystickVector, 1f);

        Vector2 keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (keyboard.sqrMagnitude > 0.001f)
            return Vector2.ClampMagnitude(keyboard, 1f);

        if (Touchscreen.current == null)
            return Vector2.zero;

        foreach (var touch in Touchscreen.current.touches)
        {
            if (!touch.press.isPressed)
                continue;

            Vector2 current = touch.position.ReadValue();
            if (current.x > Screen.width * 0.45f || current.y > Screen.height * 0.72f)
                continue;

            Vector2 start = touch.startPosition.ReadValue();
            Vector2 delta = current - start;
            if (delta.sqrMagnitude < 16f)
                return Vector2.zero;

            return Vector2.ClampMagnitude(delta / Mathf.Max(joystickRadius, 1f), 1f);
        }

        return Vector2.zero;
    }

    public static bool IsMobileRunInput(float joystickRadius = DefaultJoystickRadius, float runThreshold = 0.82f)
    {
        return GetMovementVector(joystickRadius).magnitude >= runThreshold;
    }

    public static bool TryGetAimPointer(out Vector2 screenPosition, out bool pressed, out bool held, out bool released)
    {
        if (VirtualAimPressed || VirtualAimHeld || VirtualAimReleased)
        {
            screenPosition = VirtualAimPointerPosition;
            pressed = VirtualAimPressed;
            held = VirtualAimHeld;
            released = VirtualAimReleased;
            VirtualAimPressed = false;
            VirtualAimReleased = false;
            LastAimPointerFromVirtualButton = true;
            return true;
        }

        LastAimPointerFromVirtualButton = false;
        screenPosition = Vector2.zero;
        pressed = false;
        held = false;
        released = false;

        if (TryGetFallbackAimPointer(out screenPosition, out pressed, out held, out released))
        {
            LastAimPointerFromVirtualButton = true;
            return true;
        }

        if (Mouse.current == null)
            return false;

        screenPosition = Mouse.current.position.ReadValue();
        pressed = Mouse.current.leftButton.wasPressedThisFrame;
        held = Mouse.current.leftButton.isPressed;
        released = Mouse.current.leftButton.wasReleasedThisFrame;
        return pressed || held || released;
    }

    public static bool TryGetPrimaryTap(out Vector2 screenPosition, bool ignoreUi = true)
    {
        screenPosition = Vector2.zero;

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (!touch.press.wasPressedThisFrame)
                    continue;

                screenPosition = touch.position.ReadValue();
                int touchId = touch.touchId.ReadValue();
                if (ignoreUi && IsPointerOverUi(touchId))
                    return false;

                return true;
            }
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return false;

        screenPosition = Mouse.current.position.ReadValue();
        return !ignoreUi || !IsPointerOverUi();
    }

    public static bool TryGetDirectionDown(out MobileDirection direction)
    {
        direction = MobileDirection.None;

        if (queuedDirection != MobileDirection.None)
        {
            var value = queuedDirection;
            queuedDirection = MobileDirection.None;
            return SetDirection(value, out direction);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            return SetDirection(MobileDirection.Right, out direction);
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            return SetDirection(MobileDirection.Up, out direction);
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            return SetDirection(MobileDirection.Left, out direction);
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            return SetDirection(MobileDirection.Down, out direction);

        if (Touchscreen.current == null)
            return false;

        foreach (var touch in Touchscreen.current.touches)
        {
            if (!touch.press.wasReleasedThisFrame)
                continue;

            Vector2 start = touch.startPosition.ReadValue();
            Vector2 end = touch.position.ReadValue();
            Vector2 delta = end - start;

            if (delta.magnitude >= SwipeThreshold)
                return SetDirection(VectorToDirection(delta), out direction);

            Vector2 fromCenter = end - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (fromCenter.sqrMagnitude > 1f)
                return SetDirection(VectorToDirection(fromCenter), out direction);
        }

        return false;
    }

    public static void QueueDirection(MobileDirection direction)
    {
        if (direction != MobileDirection.None)
            queuedDirection = direction;
    }

    public static void ResetVirtualControls()
    {
        queuedDirection = MobileDirection.None;
        VirtualJoystickVector = Vector2.zero;
        IsVirtualJoystickActive = false;
        VirtualAimPointerPosition = Vector2.zero;
        VirtualAimPressed = false;
        VirtualAimHeld = false;
        VirtualAimReleased = false;
        VirtualAimDragVector = Vector2.zero;
        LastAimPointerFromVirtualButton = false;
        fallbackAimActive = false;
        fallbackAimStartPosition = Vector2.zero;
    }

    private static bool TryGetFallbackAimPointer(out Vector2 screenPosition, out bool pressed, out bool held, out bool released)
    {
        screenPosition = Vector2.zero;
        pressed = false;
        held = false;
        released = false;

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                Vector2 position = touch.position.ReadValue();
                Vector2 start = touch.startPosition.ReadValue();

                if (touch.press.wasPressedThisFrame && IsInsideRightAimZone(position))
                {
                    if (IsScreenPositionOverUi(position))
                        continue;

                    fallbackAimActive = true;
                    fallbackAimStartPosition = start;
                }

                if (!fallbackAimActive)
                    continue;

                pressed = touch.press.wasPressedThisFrame;
                held = touch.press.isPressed;
                released = touch.press.wasReleasedThisFrame;
                screenPosition = position;
                VirtualAimPointerPosition = position;
                VirtualAimDragVector = position - fallbackAimStartPosition;

                if (released)
                    fallbackAimActive = false;

                return pressed || held || released;
            }
        }

        if (Mouse.current == null)
            return false;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (Mouse.current.leftButton.wasPressedThisFrame && IsInsideRightAimZone(mousePosition))
        {
            if (IsScreenPositionOverUi(mousePosition))
                return false;

            fallbackAimActive = true;
            fallbackAimStartPosition = mousePosition;
        }

        if (!fallbackAimActive)
            return false;

        pressed = Mouse.current.leftButton.wasPressedThisFrame;
        held = Mouse.current.leftButton.isPressed;
        released = Mouse.current.leftButton.wasReleasedThisFrame;
        screenPosition = mousePosition;
        VirtualAimPointerPosition = mousePosition;
        VirtualAimDragVector = mousePosition - fallbackAimStartPosition;

        if (released)
            fallbackAimActive = false;

        return pressed || held || released;
    }

    private static bool IsInsideRightAimZone(Vector2 screenPosition)
    {
        return screenPosition.x >= Screen.width * RightAimStartRatio
            && screenPosition.y <= Screen.height * LowerAimHeightRatio;
    }

    private static bool SetDirection(MobileDirection value, out MobileDirection direction)
    {
        direction = value;
        return value != MobileDirection.None;
    }

    private static bool IsPointerOverUi(int pointerId = -1)
    {
        if (EventSystem.current == null)
            return false;

        return pointerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(pointerId)
            : EventSystem.current.IsPointerOverGameObject();
    }

    private static bool IsScreenPositionOverUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        var data = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);
        return results.Count > 0;
    }

    private static MobileDirection VectorToDirection(Vector2 vector)
    {
        if (Mathf.Abs(vector.x) >= Mathf.Abs(vector.y))
            return vector.x >= 0f ? MobileDirection.Right : MobileDirection.Left;

        return vector.y >= 0f ? MobileDirection.Up : MobileDirection.Down;
    }
}
