using System.ComponentModel;
using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static int kComboCachedValue = -1;
        private static string kComboCachedText = "0";
        private static GUIStyle kComboCaptionStyle;
        private static GUIStyle kComboCaptionShadowStyle;
        private static int kComboCaptionFontSize = -1;

        private static void DrawPerfectCombo()
        {
            EnsurePercentStyle();
            float scale = EvaluateComboScale();
            int valueBaseSize = ScaledFont(56, 0.075f);
            int valueSize = Mathf.RoundToInt(valueBaseSize * scale);
            int captionSize = Mathf.RoundToInt(valueSize * 0.35f);

            float shadowOffset = Mathf.Max(2f, Mathf.Round(valueSize * 0.05f));
            float centerX = Screen.width * 0.5f;

            float heightScale = Screen.height / ProgressBarReferenceHeight;
            float barTop = ProgressBarTargetTopOffset * heightScale;
            float barHeight = ProgressBarTargetHeight * heightScale;

            float verticalOffset = Screen.height * 0.030f;
            if (settings.ComboMoveUpNoCaption && IsSongCaptionEmpty())
                verticalOffset -= Screen.height * 0.040f;

            float topY = Mathf.Max(0f, barTop + barHeight + verticalOffset + settings.comboY);

            comboValueStyle.fontSize = valueSize;
            comboValueShadowStyle.fontSize = valueSize;

            // Build cached caption styles only when font/parent identity changes.
            if (kComboCaptionStyle == null)
            {
                kComboCaptionStyle = new GUIStyle(comboValueStyle);
                kComboCaptionShadowStyle = new GUIStyle(comboValueShadowStyle);
            }
            if (kComboCaptionFontSize != captionSize)
            {
                kComboCaptionStyle.fontSize = captionSize;
                kComboCaptionShadowStyle.fontSize = captionSize;
                kComboCaptionFontSize = captionSize;
            }

            float rectWidth = Screen.width * 0.4f;
            Rect valueRect = new Rect(centerX - rectWidth * 0.5f, topY, rectWidth, valueSize + Screen.height * 0.016f);

            // Cache combo text — only realloc on count change
            if (perfectCombo != kComboCachedValue)
            {
                kComboCachedValue = perfectCombo;
                kComboCachedText = perfectCombo.ToString();
            }
            string text = kComboCachedText;

            Color saved = comboValueStyle.normal.textColor;

            if (settings.ComboColorMax > 0)
            {
                float t = Mathf.Clamp01((float)perfectCombo / settings.ComboColorMax);
                Color comboLow = new Color(settings.ComboColorLowR, settings.ComboColorLowG, settings.ComboColorLowB, settings.ComboColorLowA);
                Color comboHigh = new Color(settings.ComboColorHighR, settings.ComboColorHighG, settings.ComboColorHighB, settings.ComboColorHighA);
                comboValueStyle.normal.textColor = Color.Lerp(comboLow, comboHigh, t);
            }
            else
            {
                comboValueStyle.normal.textColor = new Color(settings.ComboColorLowR, settings.ComboColorLowG, settings.ComboColorLowB, settings.ComboColorLowA);
            }

            GUI.Label(new Rect(valueRect.x + shadowOffset, valueRect.y + shadowOffset, valueRect.width, valueRect.height), text, comboValueShadowStyle);
            GUI.Label(valueRect, text, comboValueStyle);

            if (settings.CaptionText)
            {
                float spacing = Screen.height * 0.03f;
                Rect captionRect = new Rect(
                    valueRect.x,
                    valueRect.y + valueRect.height - spacing - settings.captionY,
                    valueRect.width,
                    captionSize
                );

                string caption = (settings.XPerfectComboEnabled && XPerfectBridge.Active)
                    ? "XPerfect Combo"
                    : "Perfect Combo";

                GUI.Label(
                    new Rect(captionRect.x + shadowOffset, captionRect.y + shadowOffset, captionRect.width, captionRect.height),
                    caption,
                    kComboCaptionShadowStyle
                );

                GUI.Label(captionRect, caption, kComboCaptionStyle);
            }

            comboValueStyle.normal.textColor = saved;
        }

        private static float EvaluateComboScale()
        {
            if (comboPulseStartTime < 0f)
            {
                return 1f;
            }

            float snap = (settings != null && settings.comboFastAnim) ? 0.35f : 1f;
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

        private static void RegisterComboHit(HitMargin hit)
        {
            if (!modEnabled || !runVisible)
            {
                return;
            }

            bool xpComboMode = settings.XPerfectComboEnabled && XPerfectBridge.Active;
            bool incPerfect;
            if (xpComboMode && hit == HitMargin.Perfect)
            {
                // Only XPerfect-grade hits keep the combo alive when XPerfect combo mode is on
                incPerfect = XPerfectBridge.LastJudge() == XPerfectBridge.Judge.X;
            }
            else
            {
                incPerfect = hit == HitMargin.Perfect;
            }
            bool incAuto = settings.EnableAutoCombo && hit == HitMargin.Auto;
            if (incPerfect || incAuto)
            {
                perfectCombo++;
                comboPulseStartTime = Time.realtimeSinceStartup;
            }
            else if (settings.EnableAutoCombo || hit != HitMargin.Auto)
            {
                perfectCombo = 0;
                comboPulseStartTime = -1f;
            }
        }
    }
}
