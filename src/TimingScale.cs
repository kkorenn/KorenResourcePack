using System;
using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static float currentMarginScale = 1f;

        private static void UpdateTimingScale()
        {
            try
            {
                scrController ctrl = scrController.instance;
                if (ctrl != null && ctrl.currFloor != null)
                    currentMarginScale = (float)ctrl.currFloor.marginScale;
            }
            catch { }
        }

        private static void DrawTimingScale()
        {
            EnsurePercentStyle();

            int fontSize = ScaledFont(14, 0.022f);
            float shadowOffset = Mathf.Max(1f, Mathf.Round(fontSize * 0.07f));
            percentStyle.fontSize = fontSize;
            percentShadowStyle.fontSize = fontSize;

            string label = "Timing Scale - " + Math.Round(currentMarginScale * 100, 2) + "%";

            GUIContent content = new GUIContent(label);
            float textWidth = percentStyle.CalcSize(content).x;
            float centerX = (Screen.width - textWidth) * 0.5f;

            float baseY = Screen.height
                - Mathf.Max(4f, Screen.height * 0.006f)
                - ScaledFont(20, 0.035f)
                - settings.judgementPositionY
                - fontSize
                - Screen.height * 0.008f
                - 80f
                + settings.TimingScaleOffsetY;

            Rect textRect = new Rect(centerX, baseY, textWidth + 4f, fontSize + 4f);
            GUI.Label(new Rect(textRect.x + shadowOffset, textRect.y + shadowOffset, textRect.width, textRect.height), label, percentShadowStyle);
            GUI.Label(textRect, label, percentStyle);
        }
    }
}
