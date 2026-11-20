using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ShoreBot
{
    // The panel fixed to the bottom of your view: a drawing of each controller with lines to what
    // each button does. A ring lights up green on a pressed button and fills up while you hold a
    // "hold" button (B, Menu). The tutorial highlights the button it is teaching in yellow.
    // The badge shows NAVIGATION MODE / TOOL MODE; grip callouts are dimmed outside Tool mode.
    public class ControlBar : MonoBehaviour
    {
        [Serializable]
        public class Item
        {
            public string id;          // matches an id in ControlsInfo
            public Image ring;         // highlight ring around the button on the drawing
            public TMP_Text label;     // the callout text
            public Graphic line;       // the line from the button to the text
            public Graphic dot;
        }

        public static ControlBar Instance { get; private set; }

        // Robot mode shown on the badge. The controls team can call SetToolMode(...) from the real mode
        // switch and untick "Toggle Mode With A" so the badge always matches the robot.
        public static bool ToolMode { get; private set; }
        public static event Action<bool> ModeChanged;
        public static void SetToolMode(bool toolMode)
        {
            ToolMode = toolMode;
            ModeChanged?.Invoke(toolMode);
            if (Instance) Instance.RefreshMode();
        }

        public ControlsInfo info;
        public Item[] items;
        public TMP_Text modeText;
        public Image modeBadge;
        public bool toggleModeWithA = true;

        public Color ringIdle = new Color(1f, 1f, 1f, 0f);
        public Color ringPressed = new Color32(0x4D, 0xA9, 0x6A, 255);
        public Color ringHighlight = new Color32(0xE5, 0xCA, 0x1B, 255);
        public Color labelOn = Color.white;
        public Color labelDim = new Color(1f, 1f, 1f, 0.35f);
        public Color navigationColor = new Color32(0x55, 0x53, 0x7F, 255);
        public Color toolColor = new Color32(0xE5, 0xCA, 0x1B, 255);

        InputAction[] actions;
        ControlInfo[] controls;
        float[] downSince;
        InputAction modeAction;
        string highlighted;

        void Awake()
        {
            Instance = this;
            Refresh();
            RefreshMode();
        }

        void OnEnable()
        {
            if (items == null || info == null) return;
            actions = new InputAction[items.Length];
            controls = new ControlInfo[items.Length];
            downSince = new float[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                controls[i] = info.Find(items[i].id);
                if (controls[i] != null && !string.IsNullOrEmpty(controls[i].binding))
                    actions[i] = ShoreBotControls.For(controls[i]);
                downSince[i] = -1f;
            }
            var a = info.Find("a");
            if (toggleModeWithA && a != null) modeAction = ShoreBotControls.Button("ModeToggle", a.binding);
        }

        void OnDisable()
        {
            if (actions != null) foreach (var x in actions) x?.Dispose();
            actions = null;
            modeAction?.Dispose();
            modeAction = null;
        }

        // Re-reads the callout texts from ControlsInfo.
        public void Refresh()
        {
            if (info == null || items == null) return;
            foreach (var item in items)
            {
                var c = info.Find(item.id);
                if (c == null || item.label == null) continue;
                string mode = c.toolModeOnly ? "  <size=80%><color=#A9A7D3>(Tool mode)</color></size>" : "";
                item.label.text = $"<color=#E5CA1B><b>{c.label}</b></color>  {c.action}{mode}";
            }
        }

        public void RefreshMode()
        {
            if (modeText) modeText.text = ToolMode ? "TOOL MODE" : "NAVIGATION MODE";
            if (modeText) modeText.color = ToolMode ? new Color32(0x1B, 0x1A, 0x33, 255) : Color.white;
            if (modeBadge) modeBadge.color = ToolMode ? toolColor : navigationColor;
        }

        public void Highlight(string id) => highlighted = id;

        void Update()
        {
            if (modeAction != null && modeAction.WasPressedThisFrame()) SetToolMode(!ToolMode);
            if (items == null || controls == null) return;

            for (int i = 0; i < items.Length; i++)
            {
                var c = controls[i];
                if (c == null) continue;
                bool pressed = actions[i] != null && ShoreBotControls.IsActive(c, actions[i]);
                if (pressed && downSince[i] < 0f) downSince[i] = Time.time;
                if (!pressed) downSince[i] = -1f;

                bool lit = !pressed && !string.IsNullOrEmpty(highlighted) &&
                           (items[i].id == highlighted || (highlighted == "grip" && items[i].id.EndsWith("grip")));
                var ring = items[i].ring;
                if (ring)
                {
                    ring.color = pressed ? ringPressed : lit ? ringHighlight : ringIdle;
                    ring.fillAmount = pressed && c.kind == ControlKind.Hold && c.holdSeconds > 0f
                        ? Mathf.Clamp01((Time.time - downSince[i]) / c.holdSeconds)
                        : 1f;
                }

                Color text = c.toolModeOnly && !ToolMode ? labelDim : labelOn;
                if (items[i].label) items[i].label.color = text;
                if (items[i].line) items[i].line.color = new Color(text.r, text.g, text.b, text.a * 0.8f);
                if (items[i].dot) items[i].dot.color = text;
            }
        }
    }
}
