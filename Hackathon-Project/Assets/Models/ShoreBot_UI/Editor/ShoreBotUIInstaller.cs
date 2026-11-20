using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ShoreBot.EditorTools
{
    // Adds the ShoreBot user-experience layer to the open scene:
    //   - the start-up controls tutorial (one page per Quest button),
    //   - the control bar fixed to the bottom of the view (button icons + what they do),
    //   - the wrist display on the left controller (tool, arm status, progress, hint).
    // Needs an XR Origin in the scene (Main Camera, Left Controller, Right Controller).
    // Safe to run again: it replaces its own old UI objects. Button texts live in ControlsInfo.
    public static class ShoreBotUIInstaller
    {
        const string Root = "Assets/ShoreBot_UI";
        const string InfoPath = Root + "/ControlsInfo.asset";
        const string IconFolder = Root + "/Icons";
        static readonly string[] UIObjects = { "ShoreBot_ControlBar", "ShoreBot_TutorialPanel", "ShoreBot_WristInfo", "ShoreBot_Systems", "ShoreBot_UISystems" };

        static readonly Color Navy = Hex("1B1A33", 1f);
        static readonly Color NavyGlass = Hex("1B1A33", 0.82f);
        static readonly Color NavySolid = Hex("2B2950");
        static readonly Color Yellow = Hex("E5CA1B");
        static readonly Color Lavender = Hex("A9A7D3");
        static readonly Color Green = Hex("4DA96A");
        static readonly Color DotTodo = Hex("55537F");
        static readonly Color CapNormal = new Color(1f, 1f, 1f, 0.12f);

        [MenuItem("ShoreBot/Add Tutorial and Control Bar to Scene")]
        public static void Build()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                EditorUtility.DisplayDialog("ShoreBot",
                    "TextMeshPro needs its basic files first. In the window that opens, click 'Import TMP Essentials', " +
                    "then run ShoreBot > Add Tutorial and Control Bar to Scene again.", "OK");
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
                return;
            }

            Camera cam = Camera.main;
            Transform left = FindInScene("Left Controller");
            if (cam == null || left == null)
            {
                EditorUtility.DisplayDialog("ShoreBot",
                    "Couldn't find the XR Origin (Main Camera and Left Controller) in the open scene. " +
                    "Add the XR Origin (XR Rig) from the XR Interaction Toolkit Starter Assets first.", "OK");
                return;
            }

            RemoveOldUI();
            var info = LoadOrCreateControlsInfo();

            // ---------- Wrist display (left controller) ----------
            var wrist = WorldCanvas("ShoreBot_WristInfo", left, new Vector2(380, 200), 0.0005f);
            wrist.transform.localPosition = new Vector3(0f, 0.06f, -0.09f);
            wrist.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            Panel("Background", wrist.transform, Navy, Stretch());
            var infoBar = wrist.gameObject.AddComponent<InfoBar>();
            infoBar.toolText = Label("Tool", wrist.transform, "TOOL  [placeholder]", 30, Yellow, FontStyles.Bold, TextAlignmentOptions.Left, 20, 14, 340, 40);
            infoBar.armText = Label("Arm", wrist.transform, "ARM STOPPED", 22, Lavender, FontStyles.Bold, TextAlignmentOptions.Left, 20, 58, 340, 30);
            var progress = Group("Progress", wrist.transform, 20, 94, 340, 50);
            infoBar.progressGroup = progress;
            infoBar.progressLabel = Label("Label", progress.transform, "Progress 0%", 20, Color.white, FontStyles.Normal, TextAlignmentOptions.Left, 0, 0, 340, 26);
            var barBg = Panel("Bar", progress.transform, NavySolid, null);
            TopLeft(barBg.rectTransform, 0, 30, 340, 14);
            var fill = Panel("Fill", barBg.transform, Green, null);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            infoBar.progressFill = fill.rectTransform;
            infoBar.hintText = Label("Hint", wrist.transform, "", 20, Lavender, FontStyles.Normal, TextAlignmentOptions.Left, 20, 158, 340, 30);
            infoBar.armFollowing = Green;
            infoBar.armStopped = Lavender;


            // ---------- Control bar fixed to the bottom of the view ----------
            BuildControlBar(cam.transform, info);

            // ---------- Tutorial panel ----------
            var sys = new GameObject("ShoreBot_UISystems");
            var tutorial = sys.AddComponent<TutorialManager>();
            tutorial.info = info;
            BuildTutorialPanel(cam, tutorial, info);

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
            Selection.activeObject = info;
            Debug.Log("ShoreBot: tutorial, bottom control bar and wrist display added. " +
                      "Edit button texts in Assets/ShoreBot_UI/ControlsInfo. Press Play to try it (N = next page).");
        }

        static void RemoveOldUI()
        {
            var old = new List<GameObject>();
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (System.Array.IndexOf(UIObjects, t.name) >= 0) old.Add(t.gameObject);
            foreach (var go in old) if (go != null) Object.DestroyImmediate(go);
        }

        // ---------- Control bar ----------

        const float CellW = 290, CellH = 72, Gap = 8, Pad = 12, BarW = 1500;

        static void BuildControlBar(Transform head, ControlsInfo info)
        {
            float groupW = Pad * 2 + CellW * 2 + Gap;
            float groupH = Pad * 2 + CellH * 3 + Gap * 2;
            var bar = WorldCanvas("ShoreBot_ControlBar", head, new Vector2(BarW, groupH), 0.0007f);
            bar.transform.localPosition = new Vector3(0f, -0.37f, 0.9f);   // 0.9 m ahead, at the bottom of your view
            bar.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);   // tilted up towards your eyes
            bar.sortingOrder = 10;

            var cb = bar.gameObject.AddComponent<ControlBar>();
            cb.info = info;
            cb.capNormal = CapNormal;
            cb.capPressed = Green;
            cb.capHighlight = Yellow;
            cb.iconDark = Navy;
            cb.labelColor = Lavender;

            var items = new List<ControlBar.Item>();
            BuildGroup(bar.transform, "LeftHandControls", 0, groupW, groupH, false, info, items,
                "left_stick", "left_trigger", "left_grip", "x", "y", "menu");
            BuildGroup(bar.transform, "RightHandControls", BarW - groupW, groupW, groupH, true, info, items,
                "right_stick", "right_trigger", "right_grip", "a", "b");
            cb.items = items.ToArray();
        }

        static void BuildGroup(Transform parent, string name, float x, float w, float h, bool mirrored,
                               ControlsInfo info, List<ControlBar.Item> items, params string[] ids)
        {
            var bg = Panel(name, parent, NavyGlass, null);
            TopLeft(bg.rectTransform, x, 0, w, h);
            for (int i = 0; i < ids.Length; i++)
            {
                var c = info.Find(ids[i]);
                if (c == null) continue;
                int col = mirrored ? 1 - i % 2 : i % 2;
                int row = i / 2;
                var cell = Group(ids[i], bg.transform, Pad + col * (CellW + Gap), Pad + row * (CellH + Gap), CellW, CellH);

                var cap = Panel("KeyCap", cell.transform, CapNormal, null);
                TopLeft(cap.rectTransform, 0, 6, 60, 60);
                var icon = Panel("Icon", cap.transform, Color.white, null);
                icon.sprite = c.icon;
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                Inset(icon.rectTransform, 8);

                bool two = c.kind == ControlKind.PressAndHold;
                var line1 = Label("Line1", cell.transform, c.pressAction, 22, Color.white, FontStyles.Normal,
                                  TextAlignmentOptions.Left, 72, two ? 6 : 0, CellW - 76, two ? 30 : CellH);
                var line2 = Label("Line2", cell.transform, c.holdAction, 22, Color.white, FontStyles.Normal,
                                  TextAlignmentOptions.Left, 72, 36, CellW - 76, 30);
                line1.richText = line2.richText = true;
                line2.gameObject.SetActive(two);

                items.Add(new ControlBar.Item { id = c.id, keyCap = cap, icon = icon, line1 = line1, line2 = line2 });
            }
        }

        // ---------- Tutorial panel ----------

        static void BuildTutorialPanel(Camera cam, TutorialManager t, ControlsInfo info)
        {
            const float W = 900, H = 440;
            var canvas = WorldCanvas("ShoreBot_TutorialPanel", null, new Vector2(W, H), 0.0007f);
            Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd = fwd.sqrMagnitude < 0.01f ? Vector3.forward : fwd.normalized;
            canvas.transform.position = cam.transform.position + fwd * 1.0f + Vector3.up * 0.03f;
            canvas.transform.rotation = Quaternion.LookRotation(fwd);
            var follow = canvas.gameObject.AddComponent<SmoothFollow>();
            follow.head = cam.transform;
            follow.distance = 1.0f;
            follow.heightOffset = 0.03f;
            Panel("Background", canvas.transform, Navy, Stretch());
            var p = canvas.transform;
            t.panel = canvas.gameObject;

            t.header = Label("Header", p, "CONTROLS TUTORIAL", 22, Yellow, FontStyles.Bold, TextAlignmentOptions.Left, 40, 26, 500, 32);
            t.counter = Label("Counter", p, "1 / 10", 22, Lavender, FontStyles.Bold, TextAlignmentOptions.Right, W - 240, 26, 200, 32);

            t.keyCap = Panel("KeyCap", p, CapNormal, null);
            TopLeft(t.keyCap.rectTransform, 40, 80, 190, 190);
            t.bigIcon = Panel("Icon", t.keyCap.transform, Color.white, null);
            t.bigIcon.type = Image.Type.Simple;
            t.bigIcon.preserveAspect = true;
            Inset(t.bigIcon.rectTransform, 28);
            var first = info.controls != null && info.controls.Length > 0 ? info.controls[0] : null;
            if (first != null) t.bigIcon.sprite = first.icon;
            t.handText = Label("Hand", p, "LEFT CONTROLLER", 18, Lavender, FontStyles.Bold, TextAlignmentOptions.Center, 40, 280, 190, 28);

            t.titleText = Label("Title", p, first != null ? first.buttonName : "", 46, Color.white, FontStyles.Bold, TextAlignmentOptions.Left, 260, 74, 600, 60);
            t.descriptionText = Label("Description", p, first != null ? first.description : "", 24, Hex("DDDCF0"), FontStyles.Normal, TextAlignmentOptions.TopLeft, 260, 138, 600, 72);

            var pressRow = Group("PressRow", p, 260, 216, 600, 34);
            t.pressRow = pressRow;
            t.pressLabel = Label("Label", pressRow.transform, "PRESS", 20, Yellow, FontStyles.Bold, TextAlignmentOptions.Left, 0, 0, 90, 34);
            t.pressValue = Label("Value", pressRow.transform, "[Press action]", 26, Color.white, FontStyles.Normal, TextAlignmentOptions.Left, 95, 0, 505, 34);
            var holdRow = Group("HoldRow", p, 260, 254, 600, 34);
            t.holdRow = holdRow;
            Label("Label", holdRow.transform, "HOLD", 20, Yellow, FontStyles.Bold, TextAlignmentOptions.Left, 0, 0, 90, 34);
            t.holdValue = Label("Value", holdRow.transform, "[Hold action]", 26, Color.white, FontStyles.Normal, TextAlignmentOptions.Left, 95, 0, 505, 34);

            var taskBg = Panel("Tasks", p, NavySolid, null);
            TopLeft(taskBg.rectTransform, 40, 316, W - 80, 64);
            var check = LoadSprite("check");
            var task1 = Group("Task1", taskBg.transform, 18, 13, 400, 38);
            t.check1 = Panel("Check", task1.transform, DotTodo, null);
            t.check1.sprite = check; t.check1.type = Image.Type.Simple; t.check1.preserveAspect = true;
            TopLeft(t.check1.rectTransform, 0, 3, 32, 32);
            t.task1 = Label("Text", task1.transform, "Move the Left stick", 22, Color.white, FontStyles.Normal, TextAlignmentOptions.Left, 44, 0, 356, 38);
            var task2 = Group("Task2", taskBg.transform, 420, 13, 380, 38);
            t.task2Group = task2;
            t.check2 = Panel("Check", task2.transform, DotTodo, null);
            t.check2.sprite = check; t.check2.type = Image.Type.Simple; t.check2.preserveAspect = true;
            TopLeft(t.check2.rectTransform, 0, 3, 32, 32);
            t.task2 = Label("Text", task2.transform, "Hold the button", 22, Color.white, FontStyles.Normal, TextAlignmentOptions.Left, 44, 0, 336, 38);

            t.footerText = Label("Footer", p, "Hold Menu to skip the tutorial", 18, Lavender, FontStyles.Normal, TextAlignmentOptions.Left, 40, 398, 450, 28);

            int n = 0;
            if (info.controls != null) foreach (var c in info.controls) if (c != null && c.inTutorial) n++;
            var dots = new Image[n];
            const float dot = 14, gap = 10;
            float start = W - 40 - (n * dot + (n - 1) * gap);
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            for (int i = 0; i < n; i++)
            {
                dots[i] = Panel("Dot" + (i + 1), p, i == 0 ? Yellow : DotTodo, null);
                dots[i].sprite = knob;
                dots[i].type = Image.Type.Simple;
                TopLeft(dots[i].rectTransform, start + i * (dot + gap), 405, dot, dot);
            }
            t.dots = dots;
            t.green = Green; t.yellow = Yellow; t.dotTodo = DotTodo; t.checkOff = DotTodo;
        }

        // ---------- Controls data ----------

        static ControlsInfo LoadOrCreateControlsInfo()
        {
            EnsureIconsAreSprites();
            var info = AssetDatabase.LoadAssetAtPath<ControlsInfo>(InfoPath);
            if (info == null)
            {
                info = ScriptableObject.CreateInstance<ControlsInfo>();
                info.controls = DefaultControls();
                AssetDatabase.CreateAsset(info, InfoPath);
            }
            else
            {
                // Keep the texts already typed in; just fill in missing icons.
                foreach (var d in DefaultControls())
                {
                    var c = info.Find(d.id);
                    if (c != null && c.icon == null) c.icon = d.icon;
                }
            }
            EditorUtility.SetDirty(info);
            AssetDatabase.SaveAssets();
            return info;
        }

        static ControlInfo[] DefaultControls()
        {
            const string L = "<XRController>{LeftHand}/", R = "<XRController>{RightHand}/";
            return new[]
            {
                C("left_stick", "Left stick", Hand.Left, ControlKind.Stick, "stick_l", L + "{Primary2DAxis}"),
                C("right_stick", "Right stick", Hand.Right, ControlKind.Stick, "stick_r", R + "{Primary2DAxis}"),
                C("left_trigger", "Left trigger", Hand.Left, ControlKind.Button, "trigger_l", L + "{TriggerButton}"),
                C("right_trigger", "Right trigger", Hand.Right, ControlKind.Button, "trigger_r", R + "{TriggerButton}"),
                C("left_grip", "Left grip", Hand.Left, ControlKind.Button, "grip_l", L + "{GripButton}"),
                C("right_grip", "Right grip", Hand.Right, ControlKind.Button, "grip_r", R + "{GripButton}"),
                C("a", "A button", Hand.Right, ControlKind.PressAndHold, "btn_a", R + "{PrimaryButton}"),
                C("b", "B button", Hand.Right, ControlKind.PressAndHold, "btn_b", R + "{SecondaryButton}"),
                C("x", "X button", Hand.Left, ControlKind.Button, "btn_x", L + "{PrimaryButton}"),
                C("y", "Y button", Hand.Left, ControlKind.Button, "btn_y", L + "{SecondaryButton}"),
                new ControlInfo
                {
                    id = "menu", buttonName = "Menu button", hand = Hand.Left, kind = ControlKind.PressAndHold,
                    icon = LoadSprite("btn_menu"), binding = L + "{MenuButton}",
                    pressAction = "Replay tutorial", holdAction = "Skip tutorial",
                    description = "Press to replay the controls tutorial, hold to skip it.", inTutorial = false,
                },
            };
        }

        static ControlInfo C(string id, string name, Hand hand, ControlKind kind, string icon, string binding) => new ControlInfo
        {
            id = id, buttonName = name, hand = hand, kind = kind, icon = LoadSprite(icon), binding = binding,
            pressAction = kind == ControlKind.Stick ? "[Move action]" : "[Press action]",
            holdAction = "[Hold action]",
            description = $"Placeholder: what the {name.ToLower()} does will go here once the controls are finished.",
            inTutorial = true,
        };

        // Makes sure every PNG in the Icons folder is imported as a flat 2D sprite.
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


        static Transform FindInScene(string name)
        {
            Transform loose = null;
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name) return t;
                    if (loose == null && t.name.Contains(name)) loose = t;
                }
            return loose;
        }


        static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            c.a = alpha;
            return c;
        }
    }
}
