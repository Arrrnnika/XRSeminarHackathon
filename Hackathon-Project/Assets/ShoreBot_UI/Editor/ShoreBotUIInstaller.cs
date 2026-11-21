using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ShoreBot.EditorTools
{
    // Adds the ShoreBot user-experience layer to the open scene:
    //   - two control panels fixed low on the left and right of the view: a drawing of each controller
    //     with a line from every button to what it does, plus a NAVIGATION / TOOL MODE badge.
    // Everything is put on the "HUD" layer, so a URP "Render Objects" feature can draw it on top of the world.
    // Needs an XR Origin in the scene (Main Camera).
    // Safe to run again: it replaces its own old UI objects (including an old wrist display). Button texts live in ControlsInfo.
    public static class ShoreBotUIInstaller
    {
        const string Root = "Assets/ShoreBot_UI";
        const string InfoPath = Root + "/ControlsInfo.asset";
        const string IconFolder = Root + "/Icons";
        const string HudLayer = "HUD";
        static readonly string[] UIObjects = { "ShoreBot_ControlBar", "ShoreBot_TutorialPanel", "ShoreBot_WristInfo", "ShoreBot_Systems", "ShoreBot_UISystems" };

        static readonly Color Navy = Hex("1B1A33", 1f);
        static readonly Color NavyGlass = Hex("1B1A33", 0.82f);
        static readonly Color NavySolid = Hex("2B2950");
        static readonly Color Yellow = Hex("E5CA1B");
        static readonly Color Lavender = Hex("A9A7D3");
        static readonly Color Green = Hex("4DA96A");
        static readonly Color DotTodo = Hex("55537F");
        static readonly Color CapNormal = new Color(1f, 1f, 1f, 0.12f);

        [MenuItem("ShoreBot/Add Control Panels to Scene")]
        public static void Build()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                EditorUtility.DisplayDialog("ShoreBot",
                    "TextMeshPro needs its basic files first. In the window that opens, click 'Import TMP Essentials', " +
                    "then run ShoreBot > Add Control Panels to Scene again.", "OK");
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                EditorUtility.DisplayDialog("ShoreBot",
                    "Couldn't find the Main Camera of the XR Origin in the open scene. " +
                    "Add the XR Origin (XR Rig) from the XR Interaction Toolkit Starter Assets first.", "OK");
                return;
            }

            RemoveOldUI();
            var info = LoadOrCreateControlsInfo();

            // ---------- Control panels, low on the left and right of the view ----------
            var bar = BuildControlBar(cam.transform, info);
            SetLayer(bar);

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
            Selection.activeObject = info;
            Debug.Log("ShoreBot: control panels added. Edit button texts in Assets/ShoreBot_UI/ControlsInfo.");
        }

        static void RemoveOldUI()
        {
            var old = new List<GameObject>();
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (System.Array.IndexOf(UIObjects, t.name) >= 0) old.Add(t.gameObject);
            foreach (var go in old) if (go != null) Object.DestroyImmediate(go);
        }

        // ---------- Control panels (one per hand, low and to the sides of the view) ----------

        const float GroupW = 600, BarH = 300, DrawW = 190, DrawH = 222;
        const float PanelScale = 0.0007f;     // canvas pixels to metres (600 px = 0.42 m)
        const float PanelDistance = 0.9f;     // metres from the eyes
        const float PanelHeight = -0.45f;     // metres below eye height
        const float PanelAngle = 22f;         // degrees to the left / right of straight ahead
        const float BadgeHeight = 0.2f;       // metres above eye height for the mode badge
        
        
        static GameObject BuildControlBar(Transform head, ControlsInfo info)
        {
            var root = new GameObject("ShoreBot_ControlBar");
            root.transform.SetParent(head, false);
            var cb = root.AddComponent<ControlBar>();
            cb.info = info;
            cb.ringPressed = Green;
            cb.ringHighlight = Yellow;
            cb.navigationColor = DotTodo;
            cb.toolColor = Yellow;

            var items = new List<ControlBar.Item>();
            BuildHand(HandCanvas(root.transform, "LeftHandPanel", -PanelAngle).transform, info, items, Hand.Left);
            BuildHand(HandCanvas(root.transform, "RightHandPanel", PanelAngle).transform, info, items, Hand.Right);
            cb.items = items.ToArray();

            // Small mode badge, low in the middle.
            var badge = WorldCanvas("ModeBadge", root.transform, new Vector2(300, 52), PanelScale);
            badge.transform.localPosition = new Vector3(0f, BadgeHeight, PanelDistance);
            badge.transform.localRotation = Quaternion.LookRotation(badge.transform.localPosition);   // faces the eyes
            badge.sortingOrder = 10;
            cb.modeBadge = Panel("Badge", badge.transform, DotTodo, Stretch());
            cb.modeText = Label("ModeText", cb.modeBadge.transform, "NAVIGATION MODE", 24, Color.white, FontStyles.Bold,
                                TextAlignmentOptions.Center, 0, 0, 300, 52);
            return root;
        }

        static Canvas HandCanvas(Transform parent, string name, float yaw)
        {
            var c = WorldCanvas(name, parent, new Vector2(GroupW, BarH), PanelScale);
            float r = yaw * Mathf.Deg2Rad;
            c.transform.localPosition = new Vector3(Mathf.Sin(r) * PanelDistance, PanelHeight, Mathf.Cos(r) * PanelDistance);
            c.transform.localRotation = Quaternion.LookRotation(c.transform.localPosition);   // turned to face the eyes
            c.sortingOrder = 10;
            return c;
        }

        // One controller drawing with a line from each button to what it does.
        static void BuildHand(Transform canvas, ControlsInfo info, List<ControlBar.Item> items, Hand hand)
        {
            bool left = hand == Hand.Left;
            var bg = Panel(left ? "LeftController" : "RightController", canvas, NavyGlass, Stretch());

            var draw = Panel("Drawing", bg.transform, Color.white, null);
            draw.sprite = LoadSprite(left ? "controller_left" : "controller_right");
            draw.type = Image.Type.Simple;
            draw.preserveAspect = true;
            float drawX = left ? GroupW - 20 - DrawW : 20;
            TopLeft(draw.rectTransform, drawX, (BarH - DrawH) / 2f, DrawW, DrawH);

            var callouts = new List<ControlInfo>();
            if (info.controls != null)
                foreach (var c in info.controls)
                    if (c != null && c.inBar && c.hand == hand && !string.IsNullOrEmpty(c.action)) callouts.Add(c);
            callouts.Sort((a, b) => a.anchor.y.CompareTo(b.anchor.y));

            // Spread the labels out vertically so they never overlap.
            const float gapY = 64, minY = 40, maxY = BarH - 40;
            var ys = new float[callouts.Count];
            for (int i = 0; i < callouts.Count; i++)
            {
                float y = UILine.OnDrawing(draw.rectTransform, callouts[i].anchor).y;
                ys[i] = Mathf.Max(y, i > 0 ? ys[i - 1] + gapY : minY);
            }
            if (callouts.Count > 0 && ys[callouts.Count - 1] > maxY)
            {
                float shift = ys[callouts.Count - 1] - maxY;
                for (int i = 0; i < ys.Length; i++) ys[i] = Mathf.Max(minY + i * gapY, ys[i] - shift);
            }

            var ringSprite = LoadSprite("ring");
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            const float labelW = 340, labelH = 60;
            for (int i = 0; i < callouts.Count; i++)
            {
                var c = callouts[i];
                Vector2 p = UILine.OnDrawing(draw.rectTransform, c.anchor);
                float ringPx = c.ringSize * DrawW;

                var ring = Panel(c.id + "_Ring", bg.transform, new Color(1f, 1f, 1f, 0f), null);
                ring.sprite = ringSprite;
                ring.type = Image.Type.Filled;
                ring.fillMethod = Image.FillMethod.Radial360;
                ring.fillOrigin = (int)Image.Origin360.Top;
                ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0f, 1f);
                ring.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                ring.rectTransform.anchoredPosition = new Vector2(p.x, -p.y);
                ring.rectTransform.sizeDelta = new Vector2(ringPx, ringPx);

                float labelX = left ? drawX - 30 - labelW : drawX + DrawW + 30;
                Vector2 end = new Vector2(left ? labelX + labelW + 6 : labelX - 6, ys[i]);
                Vector2 start = p + (end - p).normalized * (ringPx * 0.5f);
                var line = Panel(c.id + "_Line", bg.transform, Color.white, null);
                line.type = Image.Type.Simple;
                line.sprite = null;
                UILine.Set(line.rectTransform, start, end, 3f);
                var dot = Panel(c.id + "_Dot", bg.transform, Color.white, null);
                dot.sprite = knob;
                dot.type = Image.Type.Simple;
                TopLeft(dot.rectTransform, end.x - 5, end.y - 5, 10, 10);

                var label = Label(c.id + "_Label", bg.transform, c.action, 24, Color.white, FontStyles.Normal,
                                  left ? TextAlignmentOptions.Right : TextAlignmentOptions.Left,
                                  labelX, ys[i] - labelH / 2f, labelW, labelH);
                label.richText = true;
                label.overflowMode = TextOverflowModes.Overflow;
                items.Add(new ControlBar.Item { id = c.id, ring = ring, label = label, line = line, dot = dot });
            }
        }

        // ---------- Controls data ----------

        const int ControlsVersion = 3;   // bump when the default control list changes

        static ControlsInfo LoadOrCreateControlsInfo()
        {
            EnsureIconsAreSprites();
            var info = AssetDatabase.LoadAssetAtPath<ControlsInfo>(InfoPath);
            if (info == null)
            {
                info = ScriptableObject.CreateInstance<ControlsInfo>();
                AssetDatabase.CreateAsset(info, InfoPath);
            }
            if (info.version < ControlsVersion || info.controls == null || info.controls.Length == 0)
            {
                info.controls = DefaultControls();   // the real controls (replaces the earlier placeholders)
                info.version = ControlsVersion;
            }
            EditorUtility.SetDirty(info);
            AssetDatabase.SaveAssets();
            return info;
        }

        // Positions are on the 600 x 700 controller drawings (pixels from the top-left).
        static Vector2 At(float x, float y) => new Vector2(x / 600f, y / 700f);

        static ControlInfo[] DefaultControls()
        {
            const string L = "<XRController>{LeftHand}/", R = "<XRController>{RightHand}/";
            const string armAction = "Hold + move your hand: move the tool arm";
            return new[]
            {
                new ControlInfo { id = "left_stick", buttonName = "Left joystick", label = "JOYSTICK", hand = Hand.Left, kind = ControlKind.Stick,
                    binding = L + "{Primary2DAxis}", action = "Move & turn the robot",
                    description = "Push forward or back to drive the robot along the blade, left or right to turn it.",
                    anchor = At(205, 255), ringSize = 0.27f },
                new ControlInfo { id = "a", buttonName = "A button", label = "A", hand = Hand.Right, kind = ControlKind.Button,
                    binding = R + "{PrimaryButton}", action = "Switch Navigation / Tool mode",
                    description = "Switches between Navigation mode (drive the robot) and Tool mode (use the arm). Press A now to go into Tool mode.",
                    anchor = At(320, 385), ringSize = 0.21f },
                new ControlInfo { id = "x", buttonName = "X button", label = "X", hand = Hand.Left, kind = ControlKind.Button,
                    binding = L + "{PrimaryButton}", action = "Switch tool: Scanner / Epoxy filler",
                    description = "Swaps the tool on the arm between the Scanner and the Epoxy filler.",
                    anchor = At(280, 385), ringSize = 0.21f },
                new ControlInfo { id = "b", buttonName = "B button", label = "B", hand = Hand.Right, kind = ControlKind.Hold, holdSeconds = 2f,
                    binding = R + "{SecondaryButton}", action = "Hold 2 s: recenter the view",
                    description = "Hold B for 2 seconds to recenter your view straight ahead.",
                    anchor = At(215, 250), ringSize = 0.21f },
                new ControlInfo { id = "left_grip", buttonName = "Left grip", label = "GRIP", hand = Hand.Left, kind = ControlKind.Hold, holdSeconds = 0f,
                    binding = L + "{GripButton}", action = armAction, toolModeOnly = true, inTutorial = false,
                    anchor = At(375, 555), ringSize = 0.22f },
                new ControlInfo { id = "right_grip", buttonName = "Right grip", label = "GRIP", hand = Hand.Right, kind = ControlKind.Hold, holdSeconds = 0f,
                    binding = R + "{GripButton}", action = armAction, toolModeOnly = true, inTutorial = false,
                    anchor = At(225, 555), ringSize = 0.22f },
            };
        }

        static void EnsureIconsAreSprites()
        {
            if (!AssetDatabase.IsValidFolder(IconFolder)) return;
            foreach (var file in System.IO.Directory.GetFiles(IconFolder, "*.png"))
            {
                string path = file.Replace('\\', '/');
                if (AssetDatabase.LoadAssetAtPath<Sprite>(path) != null) continue;
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
                ti.textureShape = TextureImporterShape.Texture2D;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.npotScale = TextureImporterNPOTScale.None;
                ti.SaveAndReimport();
            }
        }

        static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/{name}.png");

        // ---------- helpers ----------

        // Puts an object and all its children on the HUD layer (drawn on top of the world by the URP Render Objects feature).
        static void SetLayer(GameObject go)
        {
            int layer = LayerMask.NameToLayer(HudLayer);
            if (layer < 0)
            {
                Debug.LogWarning($"ShoreBot: there is no '{HudLayer}' layer. Add it under Project Settings > Tags and Layers, then run the menu again.");
                return;
            }
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }

        static Canvas WorldCanvas(string name, Transform parent, Vector2 size, float scale)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one * scale;
            return canvas;
        }

        static GameObject Group(string name, Transform parent, float left, float top, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TopLeft((RectTransform)go.transform, left, top, width, height);
            return go;
        }

        static Image Panel(string name, Transform parent, Color color, System.Action<RectTransform> layout)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            layout?.Invoke(img.rectTransform);
            return img;
        }

        static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color, FontStyles style,
                                     TextAlignmentOptions align, float left, float top, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (t.font == null) t.font = TMP_Settings.defaultFontAsset;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = align;
            t.raycastTarget = false;
            t.overflowMode = TextOverflowModes.Truncate;
            TopLeft(t.rectTransform, left, top, width, height);
            return t;
        }

        static System.Action<RectTransform> Stretch() => rt =>
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        };

        static void Inset(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        static void TopLeft(RectTransform rt, float left, float top, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(left, -top);
            rt.sizeDelta = new Vector2(width, height);
        }

        static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            c.a = alpha;
            return c;
        }
    }
}