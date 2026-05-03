using System;
using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static void DrawAttempt()
        {
            if (playDatas == null) return;

            EnsurePercentStyle();

            int fontSize = ScaledFont(14, 0.022f);
            float shadowOffset = Mathf.Max(2f, Mathf.Round(fontSize * 0.08f));
            percentStyle.fontSize = fontSize;
            percentShadowStyle.fontSize = fontSize;

            float baseY = Screen.height - Mathf.Max(4f, Screen.height * 0.006f) - fontSize - settings.judgementPositionY - 80f;

            PlayData data = null;
            try { data = GetPlayData(lastMapHash); } catch { }

            string[] lines = new string[2];
            int lineCount = 0;

            if (settings.ShowAttempt)
            {
                int count = 0;
                try { count = data != null ? data.GetAttempts(startProgress, GetCurrentMultiplier()) : 0; } catch { }
                lines[lineCount++] = "Attempt " + count;
            }
            if (settings.ShowFullAttempt)
            {
                int count = 0;
                try { count = data != null ? data.GetAllAttempts() : 0; } catch { }
                lines[lineCount++] = "Full Attempt " + count;
            }

            if (lineCount == 0) return;

            // Position to the right of the judgement display
            float judgementWidth = Mathf.Max(180f, Screen.width * 0.13f);
            float judgementRight = Screen.width * 0.5f + judgementWidth * 0.5f;
            float attemptX = judgementRight + fontSize * 0.8f + settings.AttemptOffsetX;
            float lineHeight = fontSize + Screen.height * 0.004f;

            for (int i = 0; i < lineCount; i++)
            {
                float y = baseY + i * lineHeight + settings.AttemptOffsetY;
                GUIContent content = new GUIContent(lines[i]);
                float textWidth = percentStyle.CalcSize(content).x;
                Rect textRect = new Rect(attemptX, y, textWidth + 16f, lineHeight + 8f);
                GUI.Label(new Rect(textRect.x + shadowOffset, textRect.y + shadowOffset, textRect.width, textRect.height), lines[i], percentShadowStyle);
                GUI.Label(textRect, lines[i], percentStyle);
            }
        }
    }
}
