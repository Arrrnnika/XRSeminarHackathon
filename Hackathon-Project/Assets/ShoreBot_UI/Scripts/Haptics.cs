using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace ShoreBot
{
    // Short controller vibrations, so people feel a step complete or a tool switch.
    public static class Haptics
    {
        public static void Pulse(bool rightHand, float amplitude = 0.4f, float duration = 0.08f)
        {
            var usage = rightHand ? CommonUsages.RightHand : CommonUsages.LeftHand;
            var device = InputSystem.GetDevice<XRControllerWithRumble>(usage);
            if (device != null) device.SendImpulse(amplitude, duration);
        }
    }
}
