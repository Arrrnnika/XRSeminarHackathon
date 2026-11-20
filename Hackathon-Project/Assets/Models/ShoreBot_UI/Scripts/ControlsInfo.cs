using UnityEngine;

namespace ShoreBot
{
    public enum Hand { Left, Right }
    public enum ControlKind { Button, Stick, PressAndHold }

    [System.Serializable]
    public class ControlInfo
    {
        public string id;                 // used by the scripts, e.g. "a" or "left_stick"
        public string buttonName;         // shown to the user, e.g. "A button"
        public Hand hand;
        public ControlKind kind;          // PressAndHold shows two actions: one for press, one for hold
        public Sprite icon;
        public string binding;            // Input System path for the button on the Quest controller
        public string pressAction = "[Press action]";
        public string holdAction = "[Hold action]";
        [TextArea(2, 4)] public string description;
        public bool inTutorial = true;    // gets its own tutorial page
    }

    // ONE place for every control's name, icon and text.
    // Select Assets/ShoreBot_UI/ControlsInfo in the Project window and type the real actions into the
    // Inspector when the controls are finished. The tutorial and the bottom bar both read from here.
    [CreateAssetMenu(menuName = "ShoreBot/Controls Info", fileName = "ControlsInfo")]
    public class ControlsInfo : ScriptableObject
    {
        public ControlInfo[] controls;

        public ControlInfo Find(string id)
        {
            if (controls == null) return null;
            foreach (var c in controls) if (c != null && c.id == id) return c;
            return null;
        }
    }
}
