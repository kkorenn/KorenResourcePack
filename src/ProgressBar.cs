using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private const float ProgressBarReferenceWidth = 1920f;
        private const float ProgressBarReferenceHeight = 1080f;
        private const float ProgressBarTargetWidth = 720f;
        private const float ProgressBarTargetHeight = 9f;
        private const float ProgressBarTargetTopOffset = 10f;

        private static Texture2D ringTex;

        private static void DrawTopProgressBar(float progress)
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
            float fillWidth = Mathf.Clamp(innerTrackRect.width * progress, 0f, innerTrackRect.width);

            DrawRoundedRing(borderRect, new Color(settings.ProgressBarBorderR, settings.ProgressBarBorderG, settings.ProgressBarBorderB, settings.ProgressBarBorderA), outerRadius, 2f);
            DrawRoundedRect(trackRect, new Color(settings.ProgressBarBackR, settings.ProgressBarBackG, settings.ProgressBarBackB, settings.ProgressBarBackA), innerRadius);

            if (fillWidth > 1f)
            {
                Rect clipRect = new Rect(innerTrackRect.x, innerTrackRect.y, fillWidth, innerTrackRect.height);
                GUI.BeginGroup(clipRect);
                DrawRoundedRect(new Rect(0f, 0f, innerTrackRect.width, innerTrackRect.height), new Color(settings.ProgressBarFillR, settings.ProgressBarFillG, settings.ProgressBarFillB, settings.ProgressBarFillA), fillRadius);
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
