using UnityEngine;

namespace KorenResourcePack
{
    internal static class Combo
    {
        internal static float comboPulseStartTime = -1f;
        internal static float comboPulsePeakScale = 1.24f;
        internal static float comboPulseOutDuration = 0.075f;
        internal static float comboPulseSettleDuration = 0.18f;

        internal static Color GetComboColor(int combo)
        {
            if (Main.settings == null) return Color.white;
            Main.settings.EnsureColorRanges();
            float t = Main.settings.ComboColorMax <= 0 ? 0f : (float)combo / Main.settings.ComboColorMax;
            return Main.settings.ComboColor.GetColor(t);
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
