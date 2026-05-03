using System;
using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static void DrawHoldBehaviorLabel()
        {
            EnsurePercentStyle();

            string label = GetHoldBehaviorLabel();
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            int fontSize = ScaledFont(16, 0.026f);
            float shadowOffset = Mathf.Max(1f, Mathf.Round(fontSize * 0.05f));
            float width = Mathf.Max(200f, Screen.width * 0.18f);
            float x = (Screen.width - width) * 0.87f + settings.HoldOffsetX;
            float y = Screen.height - Mathf.Max(28f, Screen.height * 0.05f) + settings.HoldOffsetY;

            judgementStyle.fontSize = fontSize;
            judgementShadowStyle.fontSize = fontSize;
            judgementStyle.normal.textColor = new Color(1f, 1f, 1f, 0.92f);

            Rect rect = new Rect(x, y, width, fontSize + 8f);

            int oldDepth = GUI.depth;
            GUI.depth = -10000;
            GUI.Label(new Rect(rect.x + shadowOffset, rect.y + shadowOffset, rect.width, rect.height), label, judgementShadowStyle);
            GUI.Label(rect, label, judgementStyle);
            GUI.depth = oldDepth;
        }

        private static string GetHoldBehaviorLabel()
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
                mod?.Logger?.Log("[Warning] Hold behavior read failed: " + ex.Message);
                return null;
            }
        }
    }
}
