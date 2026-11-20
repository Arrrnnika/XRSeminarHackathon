using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ShoreBot
{
    // The start-up tutorial: one page per controller button (from ControlsInfo).
    // Each page waits until you press the button it describes. Press-and-hold buttons (A and B)
    // need both a short press and a hold. After the last page the panel closes and only the
    // bottom control bar stays. Menu: press to replay the tutorial, hold to skip it.
    public class TutorialManager : MonoBehaviour
    {
        public ControlsInfo info;

        [Header("Panel")]
        public GameObject panel;
        public TMP_Text header, counter, titleText, descriptionText, handText, footerText;
        public Image keyCap, bigIcon;
        public GameObject pressRow, holdRow;
        public TMP_Text pressLabel, pressValue, holdValue;
        public Image check1, check2;
        public TMP_Text task1, task2;
        public GameObject task2Group;
        public Image[] dots;

        [Header("Timing")]
        public float holdSeconds = 0.6f;     // how long counts as a "hold"
        public float menuHoldToSkip = 1.5f;
        public float pauseAfterDone = 0.7f;

        [Header("Look")]
        public Color green = new Color32(0x4D, 0xA9, 0x6A, 255);
        public Color yellow = new Color32(0xE5, 0xCA, 0x1B, 255);
        public Color dotTodo = new Color32(0x55, 0x53, 0x7F, 255);
        public Color checkOff = new Color32(0x55, 0x53, 0x7F, 255);
        public UnityEvent onFinished;

        public bool Running { get; private set; }

        readonly List<ControlInfo> pages = new List<ControlInfo>();
        int index;
        InputAction current, menu;
        bool pressDone, holdDone;
        float pressStart = -1f, advanceAt = -1f, menuDown = -1f;

        void OnEnable()
        {
            menu = ShoreBotControls.Button("TutorialMenu", "<XRController>{LeftHand}/{MenuButton}");
        }

        void OnDisable()
        {
            menu?.Dispose();
            current?.Dispose();
        }

        void Start()
        {
            pages.Clear();
            if (info != null && info.controls != null)
                foreach (var c in info.controls)
                    if (c != null && c.inTutorial) pages.Add(c);
            Begin();
        }

        public void Begin()
        {
            if (pages.Count == 0) return;
            StopAllCoroutines();
            Running = true;
            index = 0;
            if (panel) panel.SetActive(true);
            ShowPage();
        }

        public void Close()
        {
            StopAllCoroutines();
            Running = false;
            current?.Dispose();
            current = null;
            if (panel) panel.SetActive(false);
            if (ControlBar.Instance) ControlBar.Instance.Highlight(null);
            onFinished?.Invoke();
        }

        void Update()
        {
            HandleMenu();
            if (!Running || current == null) return;

            // Testing on the computer without a headset: N jumps to the next page.
            if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame) { Next(); return; }

            if (advanceAt > 0f)
            {
                if (Time.time >= advanceAt) Next();
                return;
            }

            var c = pages[index];
            switch (c.kind)
            {
                case ControlKind.Stick:
                    if (current.ReadValue<Vector2>().magnitude > 0.6f) { pressDone = true; PageDone(); }
                    break;

                case ControlKind.Button:
                    if (current.WasPressedThisFrame()) { pressDone = true; PageDone(); }
                    break;

                case ControlKind.PressAndHold:
                    if (current.WasPressedThisFrame()) pressStart = Time.time;
                    if (pressStart > 0f && current.IsPressed() && !holdDone)
                    {
                        float held = Time.time - pressStart;
                        if (held >= holdSeconds)
                        {
                            holdDone = true;
                            Haptics.Pulse(c.hand == Hand.Right, 0.3f, 0.05f);
                        }
                        else if (held > 0.15f && task2) task2.text = $"Hold the {c.buttonName}  {Mathf.RoundToInt(held / holdSeconds * 100)}%";
                    }
                    if (current.WasReleasedThisFrame())
                    {
                        if (pressStart > 0f && Time.time - pressStart < holdSeconds && !pressDone)
                        {
                            pressDone = true;
                            Haptics.Pulse(c.hand == Hand.Right, 0.3f, 0.05f);
                        }
                        pressStart = -1f;
                    }
                    UpdateTasks(c);
                    if (pressDone && holdDone) PageDone();
                    break;
            }
        }

        void HandleMenu()
        {
            if (menu == null) return;
            if (menu.WasPressedThisFrame()) menuDown = Time.time;
            if (menuDown > 0f && menu.IsPressed() && Running && Time.time - menuDown >= menuHoldToSkip)
            {
                menuDown = -1f;
                Close();                                    // hold Menu: skip the tutorial
            }
            if (menu.WasReleasedThisFrame())
            {
                if (menuDown > 0f && !Running && Time.time - menuDown < menuHoldToSkip) Begin(); // press Menu: replay
                menuDown = -1f;
            }
        }

        void ShowPage()
        {
            var c = pages[index];
            current?.Dispose();
            current = ShoreBotControls.For(c);
            pressDone = holdDone = false;
            pressStart = advanceAt = -1f;

            if (header) header.text = "CONTROLS TUTORIAL";
            if (counter) counter.text = $"{index + 1} / {pages.Count}";
            if (titleText) titleText.text = c.buttonName;
            if (descriptionText) descriptionText.text = c.description;
            if (handText) handText.text = c.hand == Hand.Left ? "LEFT CONTROLLER" : "RIGHT CONTROLLER";
            if (bigIcon) { bigIcon.sprite = c.icon; bigIcon.color = Color.white; }
            if (keyCap) keyCap.color = new Color(1f, 1f, 1f, 0.12f);

            bool two = c.kind == ControlKind.PressAndHold;
            if (pressRow) pressRow.SetActive(true);
            if (task1) task1.transform.parent.gameObject.SetActive(true);
            if (pressLabel) pressLabel.text = c.kind == ControlKind.Stick ? "MOVE" : "PRESS";
            if (pressValue) pressValue.text = c.pressAction;
            if (holdRow) holdRow.SetActive(two);
            if (holdValue) holdValue.text = c.holdAction;
            if (task2Group) task2Group.SetActive(two);
            UpdateTasks(c);

            if (footerText) footerText.text = "Hold Menu to skip the tutorial";
            for (int i = 0; i < (dots?.Length ?? 0); i++)
            {
                if (!dots[i]) continue;
                dots[i].gameObject.SetActive(i < pages.Count);
                dots[i].color = i < index ? green : i == index ? yellow : dotTodo;
            }
            if (ControlBar.Instance) ControlBar.Instance.Highlight(c.id);
        }

        void UpdateTasks(ControlInfo c)
        {
            string verb = c.kind == ControlKind.Stick ? "Move" : "Press";
            if (task1) task1.text = $"{verb} the {c.buttonName}";
            if (check1) check1.color = pressDone ? green : checkOff;
            if (c.kind != ControlKind.PressAndHold) return;
            if (task2 && (holdDone || pressStart < 0f)) task2.text = $"Hold the {c.buttonName}";
            if (check2) check2.color = holdDone ? green : checkOff;
        }

        void PageDone()
        {
            var c = pages[index];
            pressDone = true;
            UpdateTasks(c);
            if (keyCap) keyCap.color = green;
            Haptics.Pulse(c.hand == Hand.Right, 0.5f, 0.12f);
            advanceAt = Time.time + pauseAfterDone;
        }

        void Next()
        {
            index++;
            if (index >= pages.Count) StartCoroutine(Finish());
            else ShowPage();
        }

        IEnumerator Finish()
        {
            current?.Dispose();
            current = null;
            if (header) header.text = "TUTORIAL COMPLETE";
            if (counter) counter.text = "";
            if (titleText) titleText.text = "You're ready";
            if (descriptionText) descriptionText.text = "Your controls stay at the bottom of your view. Press Menu any time to replay this tutorial.";
            if (handText) handText.text = "";
            if (bigIcon) { bigIcon.sprite = check1 ? check1.sprite : null; bigIcon.color = Color.white; }
            if (keyCap) keyCap.color = green;
            if (pressRow) pressRow.SetActive(false);
            if (holdRow) holdRow.SetActive(false);
            if (task1) task1.transform.parent.gameObject.SetActive(false);
            if (task2Group) task2Group.SetActive(false);
            if (footerText) footerText.text = "";
            if (dots != null) foreach (var d in dots) if (d) d.color = green;
            if (ControlBar.Instance) ControlBar.Instance.Highlight(null);
            yield return new WaitForSeconds(3f);
            if (pressRow) pressRow.SetActive(true);
            if (task1) task1.transform.parent.gameObject.SetActive(true);
            Close();
        }
    }
}
