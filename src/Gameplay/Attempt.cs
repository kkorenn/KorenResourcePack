using UnityEngine;

namespace KorenResourcePack
{
    // Attempt counter HUD line. Reads PlayCount state and renders the "Attempt N" /
    // "Full Attempt N" labels under the judgement strip.
    internal static class Attempt
    {
        private static readonly GUIContent cachedContent = new GUIContent();

        // Track last raw values
        internal static int lastAttemptRaw = -1;
        internal static int lastFullAttemptRaw = -1;

        // Display values (stable)
        internal static int displayAttempt = 1;
        internal static int displayFullAttempt = 1;

        internal static void DrawAttempt()
        {
            if (PlayCount.playDatas == null) return;

            Styles.EnsurePercentStyle();

            int fontSize = Styles.ScaledFont(14, 0.022f);
            float shadowOffset = Mathf.Max(2f, Mathf.Round(fontSize * 0.08f));

            Styles.percentStyle.fontSize = fontSize;
            Styles.percentShadowStyle.fontSize = fontSize;

            float baseY =
                Screen.height
                - Mathf.Max(4f, Screen.height * 0.006f)
                - fontSize
                - Main.settings.judgementPositionY
                - 80f;

            PlayCount.PlayData data;
            PlayCount.TryGetPlayData(PlayCount.lastMapHash, out data);

            string line1 = null;
            string line2 = null;
            int lineCount = 0;

            // --- Attempt ---
            if (Main.settings.ShowAttempt)
            {
                if (data != null)
                {
                    lastAttemptRaw = data.GetAttempts(PlayCount.startProgress, PlayCount.GetCurrentMultiplier());
                    displayAttempt = PlayCount.GetSessionAttemptDisplay();
                }

                line1 = $"Attempt {displayAttempt}";
                lineCount++;
            }

            // --- Full Attempt ---
            if (Main.settings.ShowFullAttempt)
            {
                if (data != null)
                {
                    lastFullAttemptRaw = data.GetAllAttempts();
                    displayFullAttempt = lastFullAttemptRaw;
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

            float attemptX = judgementRight + fontSize * 0.8f + Main.settings.AttemptOffsetX;
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

            float y = baseY + index * lineHeight + Main.settings.AttemptOffsetY;

            cachedContent.text = text;
            float textWidth = Styles.percentStyle.CalcSize(cachedContent).x;

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
                Styles.percentShadowStyle
            );

            GUI.Label(rect, text, Styles.percentStyle);
        }
    }
}
