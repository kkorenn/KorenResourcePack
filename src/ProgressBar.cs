using UnityEngine;

namespace KorenResourcePack
{
    internal static class ProgressBar
    {
        internal const float ProgressBarReferenceWidth = 1920f;
        internal const float ProgressBarReferenceHeight = 1080f;
        internal const float ProgressBarTargetWidth = 720f;
        internal const float ProgressBarTargetHeight = 9f;
        internal const float ProgressBarTargetTopOffset = 10f;

        private static Texture2D ringTex;

        internal static void DrawTopProgressBar(float progress)
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float widthScale = screenWidth / ProgressBarReferenceWidth;
            float heightScale = screenHeight / ProgressBarReferenceHeight;
            float width = Mathf.Clamp(ProgressBarTargetWidth * widthScale, 260f, screenWidth - 32f);
            float height = Mathf.Max(8f, ProgressBarTargetHeight * heightScale);
            float x = (screenWidth - width) * 0.5f;
            float y = Mathf.Max(6f, ProgressBarTargetTopOffset * heightScale);
            y = Mathf.Min(y, screenHeight - height - 4f);
            float outerRadius = Mathf.Min((height + 4f) * 0.5f, 14f);
            float innerRadius = Mathf.Max(1f, outerRadius - 2f);
            float fillRadius = Mathf.Max(1f, innerRadius - 1f);

            Color oldColor = GUI.color;
            int oldDepth = GUI.depth;
            GUI.depth = -10000;

            Rect borderRect = new Rect(x - 2f, y - 2f, width + 4f, height + 4f);
            Rect trackRect = new Rect(x, y, width, height);
            Rect innerTrackRect = new Rect(x + 1f, y + 1f, width - 2f, height - 2f);

            // Fill spans [runStartProgress, progress] instead of [0, progress] so a
            // checkpoint-resumed run only highlights the segment the player has actually
            // traveled this attempt — the part before the start sits as empty track.
            float startPct = Mathf.Clamp01(ProgressTracker.RunStartProgress);
            float endPct = Mathf.Clamp01(progress);
            if (endPct < startPct) endPct = startPct;
            float fillStartX = innerTrackRect.x + innerTrackRect.width * startPct;
            float fillWidth = Mathf.Clamp(innerTrackRect.width * (endPct - startPct), 0f, innerTrackRect.width);

            DrawRoundedRing(borderRect, new Color(Main.settings.ProgressBarBorderR, Main.settings.ProgressBarBorderG, Main.settings.ProgressBarBorderB, Main.settings.ProgressBarBorderA), outerRadius, 2f);
            DrawRoundedRect(trackRect, new Color(Main.settings.ProgressBarBackR, Main.settings.ProgressBarBackG, Main.settings.ProgressBarBackB, Main.settings.ProgressBarBackA), innerRadius);

            if (fillWidth > 1f)
            {
                Rect clipRect = new Rect(fillStartX, innerTrackRect.y, fillWidth, innerTrackRect.height);
                GUI.BeginGroup(clipRect);
                // Draw the rounded rect at full track width and clip to the segment so the
                // corners stay round at both ends of the fill region.
                DrawRoundedRect(new Rect(innerTrackRect.x - fillStartX, 0f, innerTrackRect.width, innerTrackRect.height), new Color(Main.settings.ProgressBarFillR, Main.settings.ProgressBarFillG, Main.settings.ProgressBarFillB, Main.settings.ProgressBarFillA), fillRadius);
                GUI.EndGroup();
            }

            GUI.depth = oldDepth;
            GUI.color = oldColor;
        }

        private static void DrawRoundedRect(Rect rect, Color color, float radius)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Color savedGuiColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 1f, color, 0f, radius);
            GUI.color = savedGuiColor;
        }

        private static Texture2D GetRingTexture()
        {
            if (ringTex == null)
            {
                ringTex = new Texture2D(3, 3, TextureFormat.RGBA32, false);
                ringTex.filterMode = FilterMode.Bilinear;
                ringTex.wrapMode = TextureWrapMode.Clamp;
                Color32 white = new Color32(255, 255, 255, 255);
                Color32 clear = new Color32(255, 255, 255, 0);
                Color32[] px = new Color32[9];
                for (int i = 0; i < 9; i++) px[i] = (i == 4) ? clear : white;
                ringTex.SetPixels32(px);
                ringTex.Apply();
            }
            return ringTex;
        }

        private static void DrawRoundedRing(Rect rect, Color color, float radius, float thickness)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;
            Color savedGuiColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, GetRingTexture(), ScaleMode.StretchToFill, true, 1f, color,
                new Vector4(thickness, thickness, thickness, thickness),
                new Vector4(radius, radius, radius, radius));
            GUI.color = savedGuiColor;
        }
    }
}
