using UnityEngine;
using UnityEngine.InputSystem;

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

    public static Vector2 GetMovementVector(float joystickRadius = DefaultJoystickRadius)
    {
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
            if (current.x > Screen.width * 0.45f)
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
        screenPosition = Vector2.zero;
        pressed = false;
        held = false;
        released = false;

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                Vector2 position = touch.position.ReadValue();
                bool isPressed = touch.press.isPressed;
                bool started = touch.press.wasPressedThisFrame;
                bool ended = touch.press.wasReleasedThisFrame;

                if (!isPressed && !started && !ended)
                    continue;

                if (position.x < Screen.width * 0.45f)
                    continue;

                screenPosition = position;
                pressed = started;
                held = isPressed;
                released = ended;
                return true;
            }
        }

        if (Mouse.current == null)
            return false;

        screenPosition = Mouse.current.position.ReadValue();
        pressed = Mouse.current.leftButton.wasPressedThisFrame;
        held = Mouse.current.leftButton.isPressed;
        released = Mouse.current.leftButton.wasReleasedThisFrame;
        return pressed || held || released;
    }

    public static bool TryGetDirectionDown(out MobileDirection direction)
    {
        direction = MobileDirection.None;

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

    private static bool SetDirection(MobileDirection value, out MobileDirection direction)
    {
        direction = value;
        return value != MobileDirection.None;
    }

    private static MobileDirection VectorToDirection(Vector2 vector)
    {
        if (Mathf.Abs(vector.x) >= Mathf.Abs(vector.y))
            return vector.x >= 0f ? MobileDirection.Right : MobileDirection.Left;

        return vector.y >= 0f ? MobileDirection.Up : MobileDirection.Down;
    }
}
