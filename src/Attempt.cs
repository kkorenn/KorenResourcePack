using System;
using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static readonly GUIContent cachedContent = new GUIContent();

        // Track last raw values
        private static int lastAttemptRaw = -1;
        private static int lastFullAttemptRaw = -1;

        // Display values (stable)
        private static int displayAttempt = 1;
        private static int displayFullAttempt = 1;

        private static void DrawAttempt()
        {
            if (playDatas == null) return;

            EnsurePercentStyle();

            int fontSize = ScaledFont(14, 0.022f);
            float shadowOffset = Mathf.Max(2f, Mathf.Round(fontSize * 0.08f));

            percentStyle.fontSize = fontSize;
            percentShadowStyle.fontSize = fontSize;

            float baseY =
                Screen.height
                - Mathf.Max(4f, Screen.height * 0.006f)
                - fontSize
                - settings.judgementPositionY
                - 80f;

            PlayData data = GetPlayData(lastMapHash);

            string line1 = null;
            string line2 = null;
            int lineCount = 0;

            // --- Attempt ---
            if (settings.ShowAttempt)
            {
                if (data != null)
                {
                    int newRaw = data.GetAttempts(startProgress, GetCurrentMultiplier());

                    // ONLY update when it actually increases (retry)
                    if (newRaw > lastAttemptRaw)
                    {
                        lastAttemptRaw = newRaw;
                        displayAttempt = newRaw + 1;
                    }
                }

                line1 = $"Attempt {displayAttempt}";
                lineCount++;
            }

            // --- Full Attempt ---
            if (settings.ShowFullAttempt)
            {
                if (data != null)
                {
                    int newRaw = data.GetAllAttempts();

                    if (newRaw > lastFullAttemptRaw)
                    {
                        lastFullAttemptRaw = newRaw;
                        displayFullAttempt = newRaw + 1;
                    }
                }

                if (line1 == null)
                    line1 = $"Full Attempt {displayFullAttempt}";
                else
                    line2 = $"Full Attempt {displayFullAttempt}";

                lineCount++;
            }

            if (lineCount == 0) return;

            float judgementWidth = Mathf.Max(180f, Screen.width * 0.13f);
            float judgementRight = Screen.width * 0.5f + judgementWidth * 0.5f;

            float attemptX = judgementRight + fontSize * 0.8f + settings.AttemptOffsetX;
            float lineHeight = fontSize + Screen.height * 0.004f;

            DrawLine(line1, 0, baseY, lineHeight, attemptX, shadowOffset);
            DrawLine(line2, 1, baseY, lineHeight, attemptX, shadowOffset);
        }

        private static void DrawLine(
            string text,
            int index,
            float baseY,
            float lineHeight,
            float x,
            float shadowOffset)
        {
            if (string.IsNullOrEmpty(text)) return;

            float y = baseY + index * lineHeight + settings.AttemptOffsetY;

            cachedContent.text = text;
            float textWidth = percentStyle.CalcSize(cachedContent).x;

            Rect rect = new Rect(
                x,
                y,
                textWidth + 16f,
                lineHeight + 8f
            );

            GUI.Label(
                new Rect(
                    rect.x + shadowOffset,
                    rect.y + shadowOffset,
                    rect.width,
                    rect.height
                ),
                text,
                percentShadowStyle
            );

            GUI.Label(rect, text, percentStyle);
        }
    }
}