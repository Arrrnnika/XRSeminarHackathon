using UnityEngine;
using UnityEngine.InputSystem;

namespace ShoreBot
{
    // Small helpers for reading controller buttons. Bindings come from ControlsInfo.
    public static class ShoreBotControls
    {
        public static InputAction Button(string name, string bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            AddBindings(action, bindings);
            action.Enable();
            return action;
        }

        public static InputAction Stick(string name, string bindings)
        {
            var action = new InputAction(name, InputActionType.Value, expectedControlType: "Vector2");
            AddBindings(action, bindings);
            action.Enable();
            return action;
        }

        static void AddBindings(InputAction action, string bindings)
        {
            if (string.IsNullOrEmpty(bindings)) return;
            foreach (var b in bindings.Split(';'))
                if (!string.IsNullOrWhiteSpace(b)) action.AddBinding(b.Trim());
        }

        public static InputAction For(ControlInfo control) =>
            control.kind == ControlKind.Stick ? Stick(control.id, control.binding) : Button(control.id, control.binding);

        public static bool IsActive(ControlInfo control, InputAction action, float stickThreshold = 0.4f) =>
            control.kind == ControlKind.Stick
                ? action.ReadValue<Vector2>().magnitude > stickThreshold
                : action.IsPressed();
    }
}
