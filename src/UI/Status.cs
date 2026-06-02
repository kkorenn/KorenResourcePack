using System;
using UnityEngine;
using static KorenResourcePack.Main;

namespace KorenResourcePack
{
    internal static class Status
    {
        private static float smoothedFps;

        private const string AccuracyGoldHex = "#FFDA00";
        private static int cachedPercentDecimals = -1;
        private static string cachedPercentFormat = "0.##";

        internal static void InvalidatePercentCaches()
        {
            Overlay.InvalidateOverlayPercentCaches();
        }

        internal static string FormatPercent(float ratio, bool goldAtPerfect = false)
        {
            if (float.IsNaN(ratio) || float.IsInfinity(ratio)) ratio = 1f;
            int decimals = Main.settings != null ? Mathf.Clamp(Main.settings.DecimalPlaces, 0, 6) : 2;
            float pct = ratio * 100f;
            string body = pct.ToString(GetPercentFormat(decimals), System.Globalization.CultureInfo.InvariantCulture) + "%";

            float perfectThreshold = 100f - 0.5f * Mathf.Pow(10f, -decimals);
            if (goldAtPerfect && pct >= perfectThreshold)
                return "<color=" + AccuracyGoldHex + ">" + body + "</color>";
            return body;
        }

        private static string GetPercentFormat(int decimals)
        {
            if (decimals != cachedPercentDecimals)
            {
                cachedPercentDecimals = decimals;
                cachedPercentFormat = decimals == 0 ? "0" : "0." + new string('0', decimals);
            }
            return cachedPercentFormat;
        }

        private static string FormatAccuracyPercent(float ratio) => FormatPercent(ratio, goldAtPerfect: true);

        internal static string FormatProgressRange(float now)
        {
            if (!ProgressTracker.RunStartedFromFirstTile && ProgressTracker.RunStartProgress > 0f)
                return FormatPercent(ProgressTracker.RunStartProgress) + " - " + FormatPercent(now);
            return FormatPercent(now);
        }

        internal static string FormatStatusLine(string label, string value)
        {
            return "<color=white>" + label + " |</color> " + value;
        }

        internal static string FormatExistingStatusLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            int pipe = text.IndexOf('|');
            if (pipe < 0) return text;
            return "<color=white>" + text.Substring(0, pipe + 1) + "</color>" + text.Substring(pipe + 1);
        }

        private static string BuildSplit(Func<int, float> ratioOf, Func<float, Color> gradeOf, bool goldAtPerfect)
        {
            if (Main.settings != null) Main.settings.EnsureColorRanges();
            int count = MistakesAccess.PlayerCount();
            if (count < 1) count = 1;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
            {
                float r = ratioOf(i);
                if (float.IsNaN(r) || float.IsInfinity(r)) r = 1f;
                r = Mathf.Clamp01(r);
                if (i > 0) sb.Append(" <color=white>|</color> ");
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(gradeOf(r))).Append('>')
                  .Append(FormatPercent(r, goldAtPerfect)).Append("</color>");
            }
            return sb.ToString();
        }

        private static float MaxAccuracyRatioFor(int playerID)
        {
            float acc = MistakesAccess.PercentAcc(playerID);
            float prog = MistakesAccess.PercentComplete(playerID);
            return acc * prog + (1f - prog);
        }

        internal static Color GetProgressColor(float progress)
        {
            if (Main.settings == null) return Color.white;
            Main.settings.EnsureColorRanges();
            return Main.settings.ProgressColor.GetColor(progress);
        }

        internal static float GetAccuracyRatio()
        {
            try
            {
                float a = MistakesAccess.PercentAcc(MistakesAccess.Get());
                if (float.IsNaN(a) || float.IsInfinity(a)) return 1f;
                return Mathf.Clamp01(a);
            }
            catch { return 1f; }
        }

        internal static float GetXAccuracyRatio()
        {
            try
            {
                float xAccuracy = MistakesAccess.PercentXAcc(MistakesAccess.Get());
                if (float.IsNaN(xAccuracy) || float.IsInfinity(xAccuracy)) return 1f;
                return Mathf.Clamp01(xAccuracy);
            }
            catch { return 1f; }
        }

        internal static Color GetAccuracyColor()
        {
            if (Main.settings == null) return Color.white;
            if (MistakesAccess.CoopMode()) return Color.white;
            Main.settings.EnsureColorRanges();
            return Main.settings.AccuracyColor.GetColor(GetAccuracyRatio());
        }

        internal static Color GetXAccuracyColor()
        {
            if (Main.settings == null) return Color.white;
            if (MistakesAccess.CoopMode()) return Color.white;
            Main.settings.EnsureColorRanges();
            return Main.settings.XAccuracyColor.GetColor(GetXAccuracyRatio());
        }

        internal static Color GetMusicTimeColor()
        {
            if (Main.settings == null) return Color.white;
            Main.settings.EnsureColorRanges();
            return Main.settings.MusicTimeColor.GetColor(GetPrimaryTimeRatio());
        }

        internal static Color GetMapTimeColor()
        {
            if (Main.settings == null) return Color.white;
            Main.settings.EnsureColorRanges();
            return Main.settings.MapTimeColor.GetColor(GetMapTimeRatio());
        }

        internal static Color GetBestColor()
        {
            if (Main.settings == null) return Color.white;
            Main.settings.EnsureColorRanges();
            float bestStart, bestEnd;
            float best = PlayCount.TryGetBestRange(out bestStart, out bestEnd) ? bestEnd : 0f;
            return Main.settings.BestColor.GetColor(best);
        }

        internal static string GetAccuracyText()
        {
            try
            {
                if (MistakesAccess.CoopMode())
                    return BuildSplit(MistakesAccess.PercentAcc, Main.settings.AccuracyColor.GetColor, true);
                float a = MistakesAccess.PercentAcc(MistakesAccess.Get());
                return FormatAccuracyPercent(a);
            }
            catch { return FormatAccuracyPercent(1f); }
        }

        internal static string GetXAccuracyText()
        {
            try
            {
                if (MistakesAccess.CoopMode())
                    return BuildSplit(MistakesAccess.PercentXAcc, Main.settings.XAccuracyColor.GetColor, true);
                float xAccuracy = MistakesAccess.PercentXAcc(MistakesAccess.Get());
                return FormatAccuracyPercent(xAccuracy);
            }
            catch { return FormatAccuracyPercent(1f); }
        }

        internal static float GetMaxAccuracyRatio()
        {
            try
            {
                scrMistakesManager m = MistakesAccess.Get();
                float acc = MistakesAccess.PercentAcc(m);
                float prog = MistakesAccess.PercentComplete(m);
                float r = acc * prog + (1f - prog);
                if (float.IsNaN(r) || float.IsInfinity(r)) return 1f;
                return Mathf.Clamp01(r);
            }
            catch { return 1f; }
        }

        internal static float GetMaxXAccuracyRatio()
        {
            return XAccuracy.MaxRatio();
        }

        internal static Color GetMaxAccuracyColor()
        {
            if (Main.settings == null) return Color.white;
            if (MistakesAccess.CoopMode()) return Color.white;
            Main.settings.EnsureColorRanges();
            return Main.settings.AccuracyColor.GetColor(GetMaxAccuracyRatio());
        }

        internal static Color GetMaxXAccuracyColor()
        {
            if (Main.settings == null) return Color.white;
            if (MistakesAccess.CoopMode()) return Color.white;
            Main.settings.EnsureColorRanges();
            return Main.settings.XAccuracyColor.GetColor(GetMaxXAccuracyRatio());
        }

        internal static string GetMaxAccuracyText()
        {
            try
            {
                if (MistakesAccess.CoopMode())
                    return BuildSplit(MaxAccuracyRatioFor, Main.settings.AccuracyColor.GetColor, true);
            }
            catch {   }
            return FormatAccuracyPercent(GetMaxAccuracyRatio());
        }

        internal static string GetMaxXAccuracyText()
        {
            try
            {
                if (MistakesAccess.CoopMode())
                    return BuildSplit(XAccuracy.MaxRatio, Main.settings.XAccuracyColor.GetColor, true);
            }
            catch {   }
            return FormatAccuracyPercent(GetMaxXAccuracyRatio());
        }

        internal static bool IsMusicPlaying()
        {
            try { return scrConductor.instance != null && scrConductor.instance.song != null && scrConductor.instance.song.isPlaying; }
            catch { return false; }
        }

        private static string FormatTime(float seconds, bool forceHour = false)
        {
            if (seconds < 0f) seconds = 0f;
            int total = (int)seconds;
            if (forceHour || total >= 3600)
                return (total / 3600) + ":" + ((total % 3600) / 60).ToString("00") + ":" + (total % 60).ToString("00");

            int m = total / 60;
            int s = total % 60;
            return m + ":" + s.ToString("00");
        }

        internal static string GetPrimaryTimeStatusText()
        {
            return "Music Time | " + GetMusicTimeText();
        }

        internal static float GetPrimaryTimeRatio()
        {
            try
            {
                AudioSource song = scrConductor.instance != null ? scrConductor.instance.song : null;
                if (song == null || song.clip == null || song.clip.length <= 0f) return 0f;
                return Mathf.Clamp01(song.time / song.clip.length);
            }
            catch { return 0f; }
        }

        internal static string GetMapTimeStatusText()
        {
            return "Map Time | " + GetMapTimeText();
        }

        internal static float GetMapTimeRatio()
        {
            try
            {
                scrConductor cd = scrConductor.instance;
                if (cd == null) return 0f;
                float time = (float)(cd.addoffset + cd.songposition_minusi);
                float total = 0f;
                if (scrLevelMaker.instance != null && scrLevelMaker.instance.listFloors != null && scrLevelMaker.instance.listFloors.Count > 0)
                {
                    scrFloor last = scrLevelMaker.instance.listFloors[scrLevelMaker.instance.listFloors.Count - 1];
                    if (last != null) total = (float)last.entryTime;
                }
                if (total <= 0f) return 0f;
                return Mathf.Clamp01(time / total);
            }
            catch { return 0f; }
        }

        internal static string GetMusicTimeText()
        {
            try
            {
                AudioSource a = scrConductor.instance != null ? scrConductor.instance.song : null;
                if (a == null || a.clip == null) return "0:00 / 0:00";
                bool hour = a.clip.length >= 3600f;
                return FormatTime(a.time, hour) + " / " + FormatTime(a.clip.length, hour);
            }
            catch { return "0:00 / 0:00"; }
        }

        internal static string GetMapTimeText()
        {
            try
            {
                scrController c = scrController.instance;
                scrConductor cd = scrConductor.instance;
                if (c == null || cd == null) return "0:00";
                float t = (float)(cd.addoffset + cd.songposition_minusi);
                float total = 0f;
                try
                {
                    if (scrLevelMaker.instance != null && scrLevelMaker.instance.listFloors != null && scrLevelMaker.instance.listFloors.Count > 0)
                    {
                        scrFloor last = scrLevelMaker.instance.listFloors[scrLevelMaker.instance.listFloors.Count - 1];
                        if (last != null) total = (float)last.entryTime;
                    }
                }
                catch
                {
                }

                if (t < 0f) t = 0f;
                if (total > 0f)
                {
                    if (t > total) t = total;
                    bool hour = total >= 3600f;
                    return FormatTime(t, hour) + " / " + FormatTime(total, hour);
                }

                return FormatTime(t);
            }
            catch { return "0:00"; }
        }

        internal static int GetCheckpointCount()
        {
            try { return scnGame.instance != null ? scnGame.instance.checkpointsUsed : 0; }
            catch { return 0; }
        }

        internal static string GetBestText()
        {
            try
            {
                float start, end;
                if (!PlayCount.TryGetBestRange(out start, out end))
                    return FormatPercent(0f);
                if (start > 0f)
                    return FormatPercent(start) + " - " + FormatPercent(end);
                return FormatPercent(end);
            }
            catch { return FormatPercent(0f); }
        }

        private static int displayedFps;
        private static string displayedFpsText = "FPS | 0";
        private const float minSmooth = 2f;
        private const float maxSmooth = 12f;
        private const float sensitivity = 0.08f;

        internal static string GetFpsText()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return "FPS | --";

            float fps = 1f / dt;

            float diff = Mathf.Abs(fps - smoothedFps);
            float t = Mathf.Clamp01(diff * sensitivity);
            float smooth = Mathf.Lerp(minSmooth, maxSmooth, t);

            float factor = 1f - Mathf.Exp(-smooth * dt);
            smoothedFps += (fps - smoothedFps) * factor;

            int nextFps = Mathf.RoundToInt(smoothedFps);
            if (nextFps != displayedFps)
            {
                displayedFps = nextFps;
                displayedFpsText = "FPS | " + displayedFps;
            }

            return displayedFpsText;
        }
    }
}
