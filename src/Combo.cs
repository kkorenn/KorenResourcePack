using UnityEngine;

namespace KorenResourcePack
{
    // Perfect-combo HUD readout. Owns its own pulse animation timer + scale curve and the
    // RegisterComboHit hook called from GamePatches. Reads runVisible/perfectCombo from Main.
    internal static class Combo
    {
        // Pulse animation parameters — tunables read by EvaluateComboScale.
        internal static float comboPulseStartTime = -1f;
        internal static float comboPulsePeakScale = 1.24f;
        internal static float comboPulseOutDuration = 0.075f;
        internal static float comboPulseSettleDuration = 0.18f;

        private static int kComboCachedValue = -1;
        private static string kComboCachedText = "0";
        private static GUIStyle kComboCaptionStyle;
        private static GUIStyle kComboCaptionShadowStyle;
        private static int kComboCaptionFontSize = -1;

        internal static void DrawPerfectCombo()
        {
            Styles.EnsurePercentStyle();
            float scale = EvaluateComboScale();
            int valueBaseSize = Styles.ScaledFont(56, 0.075f);
            int valueSize = Mathf.RoundToInt(valueBaseSize * scale);
            int captionSize = Mathf.RoundToInt(valueSize * 0.35f);

            float shadowOffset = Mathf.Max(2f, Mathf.Round(valueSize * 0.05f));
            float centerX = Screen.width * 0.5f;

            float heightScale = Screen.height / ProgressBar.ProgressBarReferenceHeight;
            float barTop = ProgressBar.ProgressBarTargetTopOffset * heightScale;
            float barHeight = ProgressBar.ProgressBarTargetHeight * heightScale;

            float verticalOffset = Screen.height * 0.030f;
            if (Main.settings.ComboMoveUpNoCaption && LevelName.IsSongCaptionEmpty())
                verticalOffset -= Screen.height * 0.040f;

            float topY = Mathf.Max(0f, barTop + barHeight + verticalOffset + Main.settings.comboY);

            Styles.comboValueStyle.fontSize = valueSize;
            Styles.comboValueShadowStyle.fontSize = valueSize;

            if (kComboCaptionStyle == null)
            {
                kComboCaptionStyle = new GUIStyle(Styles.comboValueStyle);
                kComboCaptionShadowStyle = new GUIStyle(Styles.comboValueShadowStyle);
            }
            if (kComboCaptionFontSize != captionSize)
            {
                kComboCaptionStyle.fontSize = captionSize;
                kComboCaptionShadowStyle.fontSize = captionSize;
                kComboCaptionFontSize = captionSize;
            }

            float rectWidth = Screen.width * 0.4f;
            Rect valueRect = new Rect(centerX - rectWidth * 0.5f, topY, rectWidth, valueSize + Screen.height * 0.016f);

            if (Main.perfectCombo != kComboCachedValue)
            {
                kComboCachedValue = Main.perfectCombo;
                kComboCachedText = Main.perfectCombo.ToString();
            }
            string text = kComboCachedText;

            Color saved = Styles.comboValueStyle.normal.textColor;

            if (Main.settings.ComboColorMax > 0)
            {
                float t = Mathf.Clamp01((float)Main.perfectCombo / Main.settings.ComboColorMax);
                Color comboLow = new Color(Main.settings.ComboColorLowR, Main.settings.ComboColorLowG, Main.settings.ComboColorLowB, Main.settings.ComboColorLowA);
                Color comboHigh = new Color(Main.settings.ComboColorHighR, Main.settings.ComboColorHighG, Main.settings.ComboColorHighB, Main.settings.ComboColorHighA);
                Styles.comboValueStyle.normal.textColor = Color.Lerp(comboLow, comboHigh, t);
            }
            else
            {
                Styles.comboValueStyle.normal.textColor = new Color(Main.settings.ComboColorLowR, Main.settings.ComboColorLowG, Main.settings.ComboColorLowB, Main.settings.ComboColorLowA);
            }

            GUI.Label(new Rect(valueRect.x + shadowOffset, valueRect.y + shadowOffset, valueRect.width, valueRect.height), text, Styles.comboValueShadowStyle);
            GUI.Label(valueRect, text, Styles.comboValueStyle);

            if (Main.settings.CaptionText)
            {
                float spacing = Screen.height * 0.03f;
                Rect captionRect = new Rect(
                    valueRect.x,
                    valueRect.y + valueRect.height - spacing - Main.settings.captionY,
                    valueRect.width,
                    captionSize
                );

                string caption = (Main.settings.XPerfectComboEnabled && XPerfectBridge.Active)
                    ? "XPerfect Combo"
                    : "Perfect Combo";

                GUI.Label(
                    new Rect(captionRect.x + shadowOffset, captionRect.y + shadowOffset, captionRect.width, captionRect.height),
                    caption,
                    kComboCaptionShadowStyle
                );

                GUI.Label(captionRect, caption, kComboCaptionStyle);
            }

            Styles.comboValueStyle.normal.textColor = saved;
        }

        internal static float EvaluateComboScale()
        {
            if (comboPulseStartTime < 0f)
            {
                return 1f;
            }

            float snap = (Main.settings != null && Main.settings.comboFastAnim) ? 0.35f : 1f;
            float outDur = comboPulseOutDuration * snap;
            float settleDur = comboPulseSettleDuration * snap;
            float peak = comboPulsePeakScale;

            float elapsed = Time.realtimeSinceStartup - comboPulseStartTime;
            if (elapsed <= outDur)
            {
                float t = elapsed / outDur;
                float eased = t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                return Mathf.LerpUnclamped(1f, peak, eased);
            }

            float settleElapsed = elapsed - outDur;
            if (settleElapsed >= settleDur)
            {
                comboPulseStartTime = -1f;
                return 1f;
            }
            return Mathf.Lerp(peak, 1f, settleElapsed / settleDur);
        }

        internal static void RegisterComboHit(HitMargin hit)
        {
            if (!Main.modEnabled || !Main.runVisible)
            {
                return;
            }

            bool xpComboMode = Main.settings.XPerfectComboEnabled && XPerfectBridge.Active;
            bool incPerfect;
            if (xpComboMode && hit == HitMargin.Perfect)
            {
                incPerfect = XPerfectBridge.LastJudge() == XPerfectBridge.Judge.X;
            }
            else
            {
                incPerfect = hit == HitMargin.Perfect;
            }
            bool incAuto = Main.settings.EnableAutoCombo && hit == HitMargin.Auto;
            if (incPerfect || incAuto)
            {
                Main.perfectCombo++;
                comboPulseStartTime = Time.realtimeSinceStartup;
            }
            else if (Main.settings.EnableAutoCombo || hit != HitMargin.Auto)
            {
                Main.perfectCombo = 0;
                comboPulseStartTime = -1f;
            }
        }
    }
}
