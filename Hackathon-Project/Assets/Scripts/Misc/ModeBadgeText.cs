using ShoreBot;
using TMPro;
using UnityEngine;

//Shows "Navigation Mode", "Tool: Scan" or "Tool: Epoxy" on the ShoreBot mode badge,
//plus a small line below it in Tool mode (e.g. "Navigation locked").
//Runs after ControlBar, so this text always wins.
//The subtitle is created at runtime, so re-running the ShoreBot menu doesn't delete it.
[DefaultExecutionOrder(1000)]
public class ModeBadgeText : MonoBehaviour
{
    [SerializeField] private RobotModeController modeController;
    [SerializeField] private ToolSelector toolSelector;

    [Header("Subtitle in Tool mode")]
    [SerializeField] private string toolModeSubtitle = "Navigation locked";
    [SerializeField] private float subtitleSize = 28f;
    [SerializeField] private Color subtitleColor = new Color(0.66f, 0.65f, 0.83f); //lavender, like the rest of the UI

    private ControlBar _bar;
    private TextMeshProUGUI _subtitle;

    private void Start()
    {
        _bar = FindFirstObjectByType<ControlBar>();
        if (_bar != null && _bar.modeText != null)
            _subtitle = CreateSubtitle();
    }

    private void LateUpdate()
    {
        if (_bar == null || _bar.modeText == null || modeController == null)
            return;

        bool toolMode = modeController.CurrentMode == RobotModeController.RobotMode.Tool;

        string text;
        if (!toolMode)
            text = "Navigation Mode";
        else
            text = toolSelector != null && toolSelector.CurrentTool == ToolSelector.ToolType.Epoxy
                ? "Tool: Epoxy"
                : "Tool: Scan";

        if (_bar.modeText.text != text) //only touch the text when it changes
            _bar.modeText.text = text;

        if (_subtitle != null)
            _subtitle.enabled = toolMode;
    }

    //Plain text (no box) directly below the badge, on the same canvas and layer (HUD → drawn on top)
    private TextMeshProUGUI CreateSubtitle()
    {
        Transform badgeCanvas = _bar.modeText.canvas.transform;

        var go = new GameObject("ModeSubtitle", typeof(RectTransform));
        go.layer = badgeCanvas.gameObject.layer;
        go.transform.SetParent(badgeCanvas, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); //bottom center of the badge
        rt.pivot = new Vector2(0.5f, 1f);                    //hangs below that point
        rt.anchoredPosition = new Vector2(0f, -6f);
        rt.sizeDelta = new Vector2(400f, 36f);

        var subtitle = go.AddComponent<TextMeshProUGUI>();
        subtitle.text = toolModeSubtitle;
        subtitle.fontSize = subtitleSize;
        subtitle.color = subtitleColor;
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.raycastTarget = false;
        subtitle.enabled = false;
        return subtitle;
    }
}