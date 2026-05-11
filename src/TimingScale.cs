using System;
using UnityEngine;

namespace KorenResourcePack
{
    // Timing-scale HUD readout. Tracks marginScale of the current floor and renders the
    // "Timing Scale - X%" label in the IMGUI fallback path. The TMP overlay path lives in
    // Overlay.cs and reads CurrentMarginScale directly.
    internal static class TimingScale
    {
        internal static float CurrentMarginScale = 1f;

        internal static void UpdateTimingScale()
        {
            try
            {
                scrController ctrl = scrController.instance;
                if (ctrl != null && ctrl.currFloor != null)
                    CurrentMarginScale = (float)ctrl.currFloor.marginScale;
            }
            catch { }
        }

        internal static void DrawTimingScale()
        {
            Styles.EnsurePercentStyle();

            int fontSize = Styles.ScaledFont(14, 0.022f);
            float shadowOffset = Mathf.Max(1f, Mathf.Round(fontSize * 0.07f));
            Styles.percentStyle.fontSize = fontSize;
            Styles.percentShadowStyle.fontSize = fontSize;

            string label = "Timing Scale - " + Math.Round(CurrentMarginScale * 100, 2) + "%";

            GUIContent content = new GUIContent(label);
            float textWidth = Styles.percentStyle.CalcSize(content).x;
            float centerX = (Screen.width - textWidth) * 0.5f;

            float baseY = Screen.height
                - Mathf.Max(4f, Screen.height * 0.006f)
                - Styles.ScaledFont(20, 0.035f)
                - Main.settings.judgementPositionY
                - fontSize
                - Screen.height * 0.008f
                - 80f
                + Main.settings.TimingScaleOffsetY;

            Rect textRect = new Rect(centerX - 4f, baseY, textWidth + 16f, fontSize + 12f);
            GUI.Label(new Rect(textRect.x + shadowOffset, textRect.y + shadowOffset, textRect.width, textRect.height), label, Styles.percentShadowStyle);
            GUI.Label(textRect, label, Styles.percentStyle);
        }
    }
}
