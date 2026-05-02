using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
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

            float topY = Mathf.Max(0f, barTop + barHeight + verticalOffset);

            comboValueStyle.fontSize = valueSize;
            comboValueShadowStyle.fontSize = valueSize;

            GUIStyle captionStyle = new GUIStyle(comboValueStyle);
            GUIStyle captionShadowStyle = new GUIStyle(comboValueShadowStyle);
            captionStyle.fontSize = captionSize;
            captionShadowStyle.fontSize = captionSize;

            float rectWidth = Screen.width * 0.4f;
            Rect valueRect = new Rect(centerX - rectWidth * 0.5f, topY, rectWidth, valueSize + Screen.height * 0.016f);

            string text = perfectCombo.ToString();

            Color saved = comboValueStyle.normal.textColor;
            Color comboLow = new Color(settings.ComboColorLowR, settings.ComboColorLowG, settings.ComboColorLowB, settings.ComboColorLowA);

            if (settings.ComboColorMax > 0)
            {
                float t = Mathf.Clamp01((float)perfectCombo / settings.ComboColorMax);
                Color comboHigh = new Color(settings.ComboColorHighR, settings.ComboColorHighG, settings.ComboColorHighB, settings.ComboColorHighA);
                comboValueStyle.normal.textColor = Color.Lerp(comboLow, comboHigh, t);
            }
            else
            {
                comboValueStyle.normal.textColor = comboLow;
            }

            GUI.Label(new Rect(valueRect.x + shadowOffset, valueRect.y + shadowOffset, valueRect.width, valueRect.height), text, comboValueShadowStyle);
            GUI.Label(valueRect, text, comboValueStyle);

            float spacing = Screen.height * 0.03f;
            Rect captionRect = new Rect(
                valueRect.x,
                valueRect.y + valueRect.height - spacing - settings.captionY,
                valueRect.width,
                captionSize
            );

            string caption = "Perfect Combo";
            if (settings.CaptionText)
            {
                GUI.Label(new Rect(captionRect.x + shadowOffset, captionRect.y + shadowOffset, captionRect.width, captionRect.height), caption, captionShadowStyle);
                GUI.Label(captionRect, caption, captionStyle);
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

            bool incPerfect = hit == HitMargin.Perfect;
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
