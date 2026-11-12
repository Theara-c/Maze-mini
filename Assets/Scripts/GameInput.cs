using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// One place for all input. Works whether your project uses the new Input System,
// the old Input Manager, or both (Project Settings > Player > Active Input Handling).
public static class GameInput
{
    // x = left/right, y = forward/back. WASD or arrow keys.
    public static Vector2 Move()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        float x = 0f, y = 0f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    y += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  y -= 1f;
        return new Vector2(x, y).normalized;
#else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
#endif
    }

    // Mouse movement this frame, already converted to roughly "degrees".
    public static Vector2 Look(float sensitivity)
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse == null) return Vector2.zero;
        return mouse.delta.ReadValue() * 0.1f * sensitivity;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 2f * sensitivity;
#endif
    }

    public static bool RestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }
}