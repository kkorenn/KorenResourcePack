using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static KorenResourcePack.Main;

namespace KorenResourcePack
{
    internal static class Status
    {
        private static float smoothedFps;

        // Cached status text — refreshed at most every kStatusRefreshInterval seconds
        private const float kStatusRefreshInterval = 0.1f;
        private static float kStatusLastRefreshTime = -1f;
        private static string kStatusProgressText = "Progress | 0%";
        private static string kStatusAccuracyText = "Accuracy | 100%";
        private static string kStatusXAccuracyText = "XAccuracy | 100%";
        private static string kStatusMusicTimeText = "Music Time | 0:00 / 0:00";
        private static string kStatusMapTimeText = "Map Time | 0:00 / 0:00";
        private static string kStatusCheckpointText = "Checkpoints | 0";
        private static string kStatusBestText = "Best | 0%";
        private static string kStatusFpsText = "FPS | 0";
        private static string kStatusTbpmText = "TBPM | 0";
        private static string kStatusCbpmText = "CBPM | 0";
        private static string kStatusKpsText = "KPS | 0";
        private static float kStatusCachedProgress = -1f;
        private static int kStatusCachedCp = -1;

        private static Color kStatusCachedTColor;
        private static Color kStatusCachedCColor;
        private static float kStatusCachedTBpmRaw = -1f;
        private static float kStatusCachedCBpmRaw = -1f;
        private static float kStatusCachedKpsRaw = -1f;

        private static void RefreshStatusCacheIfDue(float progress, bool drawStatus, bool drawBpm)
        {
            float now = Time.unscaledTime;
            if (now - kStatusLastRefreshTime < kStatusRefreshInterval) return;
            kStatusLastRefreshTime = now;

            if (drawStatus)
            {
                if (Main.settings.ShowProgress)
                {
                    if (Mathf.Abs(progress - kStatusCachedProgress) > 0.0001f)
                    {
                        kStatusCachedProgress = progress;
                        kStatusProgressText = FormatStatusLine("Progress", FormatProgressRange(progress));
                    }
                }
                if (Main.settings.ShowAccuracy) kStatusAccuracyText = FormatStatusLine("Accuracy", GetAccuracyText());
                if (Main.settings.ShowXAccuracy) kStatusXAccuracyText = FormatStatusLine("XAccuracy", GetXAccuracyText());
                if (Main.settings.ShowMusicTime)
                {
                    kStatusMusicTimeText = FormatExistingStatusLine(GetPrimaryTimeStatusText());
                }
                if (Main.settings.ShowMapTime) kStatusMapTimeText = FormatExistingStatusLine(GetMapTimeStatusText());
                if (Main.settings.ShowCheckpoint)
                {
                    int cp = GetCheckpointCount();
                    if (cp != kStatusCachedCp)
                    {
                        kStatusCachedCp = cp;
                        kStatusCheckpointText = FormatStatusLine("Checkpoints", cp.ToString());
                    }
                }
                if (Main.settings.ShowBest) kStatusBestText = FormatStatusLine("Best", GetBestText());
                if (Main.settings.ShowFPS)
                {
                    kStatusFpsText = FormatExistingStatusLine(GetFpsText());
                }
            }

            if (drawBpm)
            {
                float tileBpm; float actualBpm;
                Bpm.GetBpmValues(out tileBpm, out actualBpm);

                if (Mathf.Abs(tileBpm - kStatusCachedTBpmRaw) > 0.005f)
                {
                    kStatusCachedTBpmRaw = tileBpm;
                    kStatusTbpmText = FormatStatusLine("TBPM", Math.Round(tileBpm, 2).ToString());
                }
                if (Mathf.Abs(actualBpm - kStatusCachedCBpmRaw) > 0.005f)
                {
                    kStatusCachedCBpmRaw = actualBpm;
                    kStatusCbpmText = FormatStatusLine("CBPM", Math.Round(actualBpm, 2).ToString());
                }
                float kps = actualBpm / 60f;
                if (Mathf.Abs(kps - kStatusCachedKpsRaw) > 0.005f)
                {
                    kStatusCachedKpsRaw = kps;
                    kStatusKpsText = FormatStatusLine("KPS", Math.Round(kps, 2).ToString());
                }
                kStatusCachedTColor = Bpm.LerpBpmColor(tileBpm);
                kStatusCachedCColor = Bpm.LerpBpmColor(actualBpm);
            }
        }

        internal static void DrawStatusText(float progress, bool drawStatus, bool drawBpm)
        {
            Styles.EnsurePercentStyle();

            RefreshStatusCacheIfDue(progress, drawStatus, drawBpm);

            int fontSize = Styles.ScaledFont(18, 0.030f);
            float shadowOffset = Mathf.Max(2f, Mathf.Round(fontSize * 0.08f));
            float lineHeight = fontSize + Screen.height * 0.006f;
            Styles.percentStyle.fontSize = fontSize;
            Styles.percentShadowStyle.fontSize = fontSize;
            Styles.rightStatusStyle.fontSize = fontSize;
            Styles.rightStatusShadowStyle.fontSize = fontSize;

            float screenW = Screen.width;
            float leftX = screenW * 0.012f;
            float topY = Screen.height * 0.013f;
            float blockWidth = screenW * 0.33f;
            float rightX = screenW - blockWidth - leftX;

            if (drawStatus)
            {
                int row = 0;
                if (Main.settings.ShowProgress)
                    DrawStatusLine(kStatusProgressText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false, GetProgressColor(progress));
                if (Main.settings.ShowAccuracy)
                    DrawStatusLine(kStatusAccuracyText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false, GetAccuracyColor());
                if (Main.settings.ShowXAccuracy)
                    DrawStatusLine(kStatusXAccuracyText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false, GetXAccuracyColor());
                if (Main.settings.ShowMusicTime)
                    DrawStatusLine(kStatusMusicTimeText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false, GetMusicTimeColor());
                if (Main.settings.ShowMapTime)
                    DrawStatusLine(kStatusMapTimeText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false, GetMapTimeColor());
                if (Main.settings.ShowCheckpoint)
                    DrawStatusLine(kStatusCheckpointText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false, Color.white);
                if (Main.settings.ShowBest)
                    DrawStatusLine(kStatusBestText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false, GetBestColor());
                if (Main.settings.ShowFPS)
                    DrawStatusLine(kStatusFpsText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false, Color.white);
            }

            if (drawBpm)
            {
                DrawStatusLine(kStatusTbpmText, rightX, topY, blockWidth, lineHeight, shadowOffset, true, kStatusCachedTColor);
                DrawStatusLine(kStatusCbpmText, rightX, topY + lineHeight, blockWidth, lineHeight, shadowOffset, true, kStatusCachedCColor);
                DrawStatusLine(kStatusKpsText, rightX, topY + lineHeight * 2f, blockWidth, lineHeight, shadowOffset, true, kStatusCachedCColor);
            }
        }

        // Gold color when at exactly 100% accuracy.
        private const string AccuracyGoldHex = "#FFDA00";
        private static int cachedPercentDecimals = -1;
        private static string cachedPercentFormat = "0.##";

        // Forces every percent-cache sentinel out of its "matches last frame" range so the
        // next Status/Overlay tick rebuilds the displayed strings. Called when the user moves
        // the DecimalPlaces slider — without this, cached strings (Progress, Timing Scale)
        // stay rendered at the old precision until their underlying value happens to change.
        internal static void InvalidatePercentCaches()
        {
            // Use a sentinel safely outside the [0,1] progress range. Avoid NaN: the cache
            // checks use Mathf.Abs(a - cached) > eps, and any subtraction with NaN yields NaN
            // which never compares > eps, so NaN would silently keep the cache stale.
            kStatusCachedProgress = -999f;
            Overlay.InvalidateOverlayPercentCaches();
        }

        // Centralized percent formatter — every HUD readout (Progress, Accuracy, XAccuracy,
        // Best, Timing Scale) routes through here so Main.settings.DecimalPlaces controls them all.
        // NaN/Infinity collapse to a clean perfect-run readout instead of "NaN%".
        internal static string FormatPercent(float ratio, bool goldAtPerfect = false)
        {
            if (float.IsNaN(ratio) || float.IsInfinity(ratio)) ratio = 1f;
            int decimals = Main.settings != null ? Mathf.Clamp(Main.settings.DecimalPlaces, 0, 6) : 2;
            float pct = ratio * 100f;
            string body = pct.ToString(GetPercentFormat(decimals), System.Globalization.CultureInfo.InvariantCulture) + "%";
            // Half-of-the-last-digit tolerance keeps 99.99999... from showing as non-perfect
            // even though it would round up to 100 at every decimal precision.
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

        // Progress text variant that shows "start% - now%" when the run began mid-level.
        // Fresh first-tile starts can report a tiny non-zero percentComplete, so the range
        // decision uses the captured run-start state instead of guessing from the percentage.
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
                scrMistakesManager m = scrController.instance != null ? scrController.instance.mistakesManager : null;
                float a = m != null ? m.percentAcc : 1f;
                if (float.IsNaN(a) || float.IsInfinity(a)) return 1f;
                return Mathf.Clamp01(a);
            }
            catch { return 1f; }
        }

        internal static float GetXAccuracyRatio()
        {
            try
            {
                scrMistakesManager mistakesManager = scrController.instance != null ? scrController.instance.mistakesManager : null;
                float xAccuracy = mistakesManager != null ? mistakesManager.percentXAcc : 1f;
                if (float.IsNaN(xAccuracy) || float.IsInfinity(xAccuracy)) return 1f;
                return Mathf.Clamp01(xAccuracy);
            }
            catch { return 1f; }
        }

        internal static Color GetAccuracyColor()
        {
            if (Main.settings == null) return Color.white;
            Main.settings.EnsureColorRanges();
            return Main.settings.AccuracyColor.GetColor(GetAccuracyRatio());
        }

        internal static Color GetXAccuracyColor()
        {
            if (Main.settings == null) return Color.white;
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
                scrMistakesManager m = scrController.instance != null ? scrController.instance.mistakesManager : null;
                float a = m != null ? m.percentAcc : 1f;
                return FormatAccuracyPercent(a);
            }
            catch { return FormatAccuracyPercent(1f); }
        }

        internal static string GetXAccuracyText()
        {
            try
            {
                scrMistakesManager mistakesManager = scrController.instance != null ? scrController.instance.mistakesManager : null;
                float xAccuracy = mistakesManager != null ? mistakesManager.percentXAcc : 1f;
                return FormatAccuracyPercent(xAccuracy);
            }
            catch { return FormatAccuracyPercent(1f); }
        }

        internal static bool IsMusicPlaying()
        {
            try { return scrConductor.instance != null && scrConductor.instance.song != null && scrConductor.instance.song.isPlaying; }
            catch { return false; }
        }

        internal static bool HasMusicClip()
        {
            try
            {
                AudioSource a = scrConductor.instance != null ? scrConductor.instance.song : null;
                return a != null && a.clip != null;
            }
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
            if (Main.settings != null && Main.settings.ShowMapTimeIfNotMusic && !HasMusicClip())
                return GetMapTimeStatusText();
            return "Music Time | " + GetMusicTimeText();
        }

        internal static float GetPrimaryTimeRatio()
        {
            if (Main.settings != null && Main.settings.ShowMapTimeIfNotMusic && !HasMusicClip())
                return GetMapTimeRatio();

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
        private static float fpsUpdateTimer;
        private static int displayedFps;
        private static string displayedFpsText = "FPS | 0";
        private const float updateInterval = 0.2f;
        private const float minSmooth = 2f;
        private const float maxSmooth = 12f;  
        private const float sensitivity = 0.08f;
        internal static string GetFpsText()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return "FPS | --";

            float fps = 1f / dt;

            // --- adaptive exponential smoothing ---
            float diff = Mathf.Abs(fps - smoothedFps);
            float t = Mathf.Clamp01(diff * sensitivity);
            float smooth = Mathf.Lerp(minSmooth, maxSmooth, t);

            // framerate-independent smoothing
            float factor = 1f - Mathf.Exp(-smooth * dt);
            smoothedFps += (fps - smoothedFps) * factor;

            // --- update display at fixed interval ---
            fpsUpdateTimer += dt;
            if (fpsUpdateTimer >= updateInterval)
            {
                fpsUpdateTimer = 0f;
                int nextFps = Mathf.RoundToInt(smoothedFps);
                if (nextFps != displayedFps)
                {
                    displayedFps = nextFps;
                    displayedFpsText = "FPS | " + displayedFps;
                }
            }

            return displayedFpsText;
        }

        [HarmonyLib.HarmonyPatch(typeof(scrShowIfDebug), "Update")]
        private static class HideDebugTextPatch
        {
            private static bool Prefix(Text ___txt)
            {
                if (!Main.modEnabled || Main.settings == null || !Main.settings.statusOn || !Main.settings.HideDebugText)
                    return true;

                if (___txt != null) ___txt.enabled = false;
                return false;
            }
        }

        private static void DrawStatusLine(string label, float x, float y, float width, float height, float shadowOffset, bool rightAligned, Color color)
        {
            GUIStyle shadowStyle = rightAligned ? Styles.rightStatusShadowStyle : Styles.percentShadowStyle;
            GUIStyle mainStyle = rightAligned ? Styles.rightStatusStyle : Styles.percentStyle;
            Color old = mainStyle.normal.textColor;
            mainStyle.normal.textColor = color;
            GUI.Label(new Rect(x + shadowOffset, y + shadowOffset, width, height), label, shadowStyle);
            GUI.Label(new Rect(x, y, width, height), label, mainStyle);
            mainStyle.normal.textColor = old;
        }
    }
}
