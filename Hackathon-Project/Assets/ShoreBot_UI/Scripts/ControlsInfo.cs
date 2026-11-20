using UnityEngine;

namespace ShoreBot
{
    public enum Hand { Left, Right, Both }
    public enum ControlKind { Button, Stick, Hold }

    [System.Serializable]
    public class ControlInfo
    {
        public string id;                    // used by the scripts, e.g. "a" or "left_stick"
        public string buttonName;            // shown in the tutorial, e.g. "A button"
        public string label;                 // short tag on the bottom panel, e.g. "A" or "GRIP"
        public Hand hand;
        public ControlKind kind;             // Hold = must be held for holdSeconds
        public float holdSeconds;
        public string binding;               // Input System path(s); separate several with ;
        [TextArea(1, 3)] public string action;       // what the button does (bottom panel callout)
        [TextArea(2, 4)] public string description;  // longer text for the tutorial page
        public bool toolModeOnly;            // only works in Tool mode (shown dimmed in Navigation mode)
        public bool inBar = true;            // gets a callout on the bottom panel
        public bool inTutorial = true;       // gets its own tutorial page
        public Vector2 anchor;               // where the button is on the controller drawing (0..1 from top-left)
        public float ringSize = 0.2f;        // highlight ring size, as a share of the drawing's width
    }

    // ONE place for every control's name and text. Select Assets/ShoreBot_UI/ControlsInfo in the Project
    // window to edit it in the Inspector. The tutorial and the bottom panel both read from here.
    [CreateAssetMenu(menuName = "ShoreBot/Controls Info", fileName = "ControlsInfo")]
    public class ControlsInfo : ScriptableObject
    {
        public int version;
        public ControlInfo[] controls;

        public ControlInfo Find(string id)
        {
            if (controls == null) return null;
            foreach (var c in controls) if (c != null && c.id == id) return c;
            return null;
        }
    }
}
