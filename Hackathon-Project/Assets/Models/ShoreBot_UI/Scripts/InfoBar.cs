using TMPro;
using UnityEngine;

namespace ShoreBot
{
    // The small display on your left wrist: current tool, arm state, task progress and a hint.
    public class InfoBar : MonoBehaviour
    {
        public static InfoBar Instance { get; private set; }

        public TMP_Text toolText, armText, progressLabel, hintText;
        public RectTransform progressFill;   // stretched from 0 to 100% of its parent's width
        public GameObject progressGroup;
        public Color armFollowing = new Color32(0x4D, 0xA9, 0x6A, 255);
        public Color armStopped = new Color32(0xA9, 0xA7, 0xD3, 255);

        void Awake()
        {
            Instance = this;
            SetArm(false);
            ClearProgress();
        }

        public void SetTool(string toolName)
        {
            if (toolText) toolText.text = "TOOL  " + toolName;
        }

        public void SetArm(bool following)
        {
            if (!armText) return;
            armText.text = following ? "ARM FOLLOWING YOUR HAND" : "ARM STOPPED";
            armText.color = following ? armFollowing : armStopped;
        }

        public void SetProgress(string label, float value01)
        {
            value01 = Mathf.Clamp01(value01);
            if (progressGroup) progressGroup.SetActive(true);
            if (progressLabel) progressLabel.text = $"{label} {Mathf.RoundToInt(value01 * 100)}%";
            if (progressFill) progressFill.anchorMax = new Vector2(value01, 1f);
        }

        public void ClearProgress()
        {
            if (progressGroup) progressGroup.SetActive(false);
        }

        public void SetHint(string text)
        {
            if (hintText) hintText.text = text;
        }
    }
}
