using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ShoreBot
{
    // The control bar fixed to the bottom of your view (it moves with your head).
    // Left-controller buttons sit bottom-left, right-controller buttons bottom-right.
    // A button's key lights up while you press it, and the tutorial highlights the button it is teaching.
    public class ControlBar : MonoBehaviour
    {
        [System.Serializable]
        public class Item
        {
            public string id;          // matches an id in ControlsInfo
            public Image keyCap;       // the square behind the icon
            public Image icon;
            public TMP_Text line1;
            public TMP_Text line2;     // only used for press-and-hold buttons
        }

        public static ControlBar Instance { get; private set; }

        public ControlsInfo info;
        public Item[] items;
        public Color capNormal = new Color(1f, 1f, 1f, 0.12f);
        public Color capPressed = new Color32(0x4D, 0xA9, 0x6A, 255);
        public Color capHighlight = new Color32(0xE5, 0xCA, 0x1B, 255);
        public Color iconLight = Color.white;
        public Color iconDark = new Color32(0x1B, 0x1A, 0x33, 255);
        public Color labelColor = new Color32(0xA9, 0xA7, 0xD3, 255);

        InputAction[] actions;
        ControlInfo[] controls;
        string highlighted;

        void Awake()
        {
            Instance = this;
            Refresh();
        }

        void OnEnable()
        {
            if (items == null || info == null) return;
            actions = new InputAction[items.Length];
            controls = new ControlInfo[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                controls[i] = info.Find(items[i].id);
                if (controls[i] != null && !string.IsNullOrEmpty(controls[i].binding))
                    actions[i] = ShoreBotControls.For(controls[i]);
            }
        }

        void OnDisable()
        {
            if (actions == null) return;
            foreach (var a in actions) a?.Dispose();
            actions = null;
        }

        // Re-reads names and texts from ControlsInfo.
        public void Refresh()
        {
            if (info == null || items == null) return;
            string hex = ColorUtility.ToHtmlStringRGB(labelColor);
            foreach (var item in items)
            {
                var c = info.Find(item.id);
                if (c == null) continue;
                if (item.icon != null && c.icon != null) item.icon.sprite = c.icon;
                bool two = c.kind == ControlKind.PressAndHold;
                if (item.line1 != null)
                    item.line1.text = two ? $"<color=#{hex}>PRESS</color>  {c.pressAction}" : c.pressAction;
                if (item.line2 != null)
                {
                    item.line2.gameObject.SetActive(two);
                    item.line2.text = $"<color=#{hex}>HOLD</color>  {c.holdAction}";
                }
            }
        }

        public void Highlight(string id) => highlighted = id;

        void Update()
        {
            if (items == null) return;
            for (int i = 0; i < items.Length; i++)
            {
                bool pressed = actions != null && actions[i] != null && ShoreBotControls.IsActive(controls[i], actions[i]);
                bool lit = !pressed && !string.IsNullOrEmpty(highlighted) && items[i].id == highlighted;
                if (items[i].keyCap) items[i].keyCap.color = pressed ? capPressed : lit ? capHighlight : capNormal;
                if (items[i].icon) items[i].icon.color = pressed || lit ? iconDark : iconLight;
            }
        }
    }
}
