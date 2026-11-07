using UnityEngine;
#if SMART_TOOLTIP_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SmartTooltip
{
    /// <summary>Reads the main mouse button or touch, with either input system.</summary>
    static class PointerInput
    {
        public static bool WasPressed(out Vector2 position)
        {
#if SMART_TOOLTIP_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var pointer = Pointer.current;
            position = pointer != null ? pointer.position.ReadValue() : default;
            return pointer != null && pointer.press.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            position = Input.mousePosition;
            return Input.GetMouseButtonDown(0);
#else
            position = default;
            return false;
#endif
        }

        public static bool WasReleased(out Vector2 position)
        {
#if SMART_TOOLTIP_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var pointer = Pointer.current;
            position = pointer != null ? pointer.position.ReadValue() : default;
            return pointer != null && pointer.press.wasReleasedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            position = Input.mousePosition;
            return Input.GetMouseButtonUp(0);
#else
            position = default;
            return false;
#endif
        }
    }
}
