using UnityEngine;

namespace ShoreBot
{
    // Draws a straight UI line (a thin Image) between two points given in "top-left" panel coordinates
    // (x to the right, y downwards, in canvas pixels).
    public static class UILine
    {
        public static void Set(RectTransform line, Vector2 from, Vector2 to, float thickness = 3f)
        {
            Vector2 d = to - from;
            line.anchorMin = line.anchorMax = new Vector2(0f, 1f);
            line.pivot = new Vector2(0f, 0.5f);
            line.anchoredPosition = new Vector2(from.x, -from.y);
            line.sizeDelta = new Vector2(d.magnitude, thickness);
            line.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        // Point on a controller drawing (anchor 0..1 from top-left) in the drawing's parent coordinates.
        public static Vector2 OnDrawing(RectTransform drawing, Vector2 anchor)
        {
            float left = drawing.anchoredPosition.x, top = -drawing.anchoredPosition.y;
            return new Vector2(left + anchor.x * drawing.sizeDelta.x, top + anchor.y * drawing.sizeDelta.y);
        }
    }
}
