using System;
using UnityEngine;

namespace KorenResourcePack
{
    // Hold-behavior label. Renders the current Persistence.holdBehavior in the bottom-right
    // (IMGUI fallback only — TMP overlay path is in Overlay.cs).
    internal static class Hold
    {
        internal static void DrawHoldBehaviorLabel()
        {
            Styles.EnsurePercentStyle();

            string label = GetHoldBehaviorLabel();
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            int fontSize = Styles.ScaledFont(16, 0.026f);
            float shadowOffset = Mathf.Max(1f, Mathf.Round(fontSize * 0.05f));
            float width = Mathf.Max(200f, Screen.width * 0.18f);
            float x = (Screen.width - width) * 0.87f + Main.settings.HoldOffsetX;
            float y = Screen.height - Mathf.Max(28f, Screen.height * 0.05f) + Main.settings.HoldOffsetY;

            Styles.judgementStyle.fontSize = fontSize;
            Styles.judgementShadowStyle.fontSize = fontSize;
            Styles.judgementStyle.normal.textColor = new Color(1f, 1f, 1f, 0.92f);

            Rect rect = new Rect(x, y, width, fontSize + 8f);

            int oldDepth = GUI.depth;
            GUI.depth = -10000;
            GUI.Label(new Rect(rect.x + shadowOffset, rect.y + shadowOffset, rect.width, rect.height), label, Styles.judgementShadowStyle);
            GUI.Label(rect, label, Styles.judgementStyle);
            GUI.depth = oldDepth;
        }

        internal static string GetHoldBehaviorLabel()
        {
            try
            {
                HoldBehavior behavior = Persistence.holdBehavior;
                switch (behavior)
                {
                    case HoldBehavior.Normal:
                        return "Holds: Normal";
                    case HoldBehavior.CanHitEnd:
                        return "Holds: Hold Tap";
                    case HoldBehavior.NoHoldNeeded:
                        return "Holds: No Holding Required";
                    default:
                        return "Holds: " + behavior;
                }
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[Warning] Hold behavior read failed: " + ex.Message);
                return null;
            }
        }
    }
}
