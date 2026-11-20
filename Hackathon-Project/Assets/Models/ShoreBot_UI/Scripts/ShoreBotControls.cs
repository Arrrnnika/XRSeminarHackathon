using UnityEngine.InputSystem;

namespace ShoreBot
{
    // Small helpers for reading controller buttons.
    // Which button is which (the "binding") is set per control in the ControlsInfo asset.
    public static class ShoreBotControls
    {
        public static InputAction Button(string name, string binding)
        {
            var action = new InputAction(name, InputActionType.Button, binding);
            action.Enable();
            return action;
        }

        public static InputAction Stick(string name, string binding)
        {
            var action = new InputAction(name, InputActionType.Value, binding, expectedControlType: "Vector2");
            action.Enable();
            return action;
        }

        public static InputAction For(ControlInfo control) =>
            control.kind == ControlKind.Stick ? Stick(control.id, control.binding) : Button(control.id, control.binding);

        public static bool IsActive(ControlInfo control, InputAction action, float stickThreshold = 0.4f) =>
            control.kind == ControlKind.Stick
                ? action.ReadValue<UnityEngine.Vector2>().magnitude > stickThreshold
                : action.IsPressed();
    }
}
