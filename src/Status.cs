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
        private static float updateTimer;
        private static int frameCount;

        // Cached status text — refreshed at most every kStatusRefreshInterval seconds
        private const float kStatusRefreshInterval = 0.1f;
        private static float kStatusLastRefreshTime = -1f;
        private static string kStatusProgressText = "Progress | 0%";
        private static string kStatusAccuracyText = "Accuracy | 100%";
        private static string kStatusXAccuracyText = "XAccuracy | 100%";
        private static string kStatusMusicTimeText = "Music/Map | 0:00";
        private static string kStatusCheckpointText = "Checkpoints | 0";
        private static string kStatusBestText = "Best | 0%";
        private static string kStatusFpsText = "FPS | 0";
        private static string kStatusTbpmText = "TBPM | 0";
        private static string kStatusCbpmText = "CBPM | 0";
        private static int kStatusCachedTbpm = int.MinValue;
        private static int kStatusCachedCbpm = int.MinValue;
        private static int kStatusCachedFps = -1;
        private static float kStatusCachedProgress = -1f;
        private static int kStatusCachedCp = -1;

        private static Color kStatusCachedTColor;
        private static Color kStatusCachedCColor;
        private static float kStatusCachedTBpmRaw = -1f;
        private static float kStatusCachedCBpmRaw = -1f;

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
                        kStatusProgressText = "Progress | " + FormatProgressRange(progress);
                    }
                }
                if (Main.settings.ShowAccuracy) kStatusAccuracyText = "Accuracy | " + GetAccuracyText();
                if (Main.settings.ShowXAccuracy) kStatusXAccuracyText = "XAccuracy | " + GetXAccuracyText();
                if (Main.settings.ShowMusicTime)
                {
                    kStatusMusicTimeText = "Music/Map | " + (IsMusicPlaying() ? GetMusicTimeText() : GetMapTimeText());
                }
                if (Main.settings.ShowCheckpoint)
                {
                    int cp = GetCheckpointCount();
                    if (cp != kStatusCachedCp)
                    {
                        kStatusCachedCp = cp;
                        kStatusCheckpointText = "Checkpoints | " + cp;
                    }
                }
                if (Main.settings.ShowBest) kStatusBestText = "Best | " + GetBestText();
                if (Main.settings.ShowFPS)
                {
                    kStatusFpsText = GetFpsText();
                }
            }

            if (drawBpm)
            {
                float tileBpm; float actualBpm;
                Bpm.GetBpmValues(out tileBpm, out actualBpm);

                if (Mathf.Abs(tileBpm - kStatusCachedTBpmRaw) > 0.005f)
                {
                    kStatusCachedTBpmRaw = tileBpm;
                    kStatusTbpmText = "TBPM | " + Math.Round(tileBpm, 2);
                    kStatusCachedTColor = Bpm.LerpBpmColor(tileBpm);
                }
                if (Mathf.Abs(actualBpm - kStatusCachedCBpmRaw) > 0.005f)
                {
                    kStatusCachedCBpmRaw = actualBpm;
                    kStatusCbpmText = "CBPM | " + Math.Round(actualBpm, 2);
                    kStatusCachedCColor = Bpm.LerpBpmColor(actualBpm);
                }
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
                    DrawStatusLine(kStatusProgressText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (Main.settings.ShowAccuracy)
                    DrawStatusLine(kStatusAccuracyText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (Main.settings.ShowXAccuracy)
                    DrawStatusLine(kStatusXAccuracyText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (Main.settings.ShowMusicTime)
                    DrawStatusLine(kStatusMusicTimeText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (Main.settings.ShowCheckpoint)
                    DrawStatusLine(kStatusCheckpointText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (Main.settings.ShowBest)
                    DrawStatusLine(kStatusBestText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (Main.settings.ShowFPS)
                    DrawStatusLine(kStatusFpsText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
            }

            if (drawBpm)
            {
                Color old = Styles.rightStatusStyle.normal.textColor;
                Styles.rightStatusStyle.normal.textColor = kStatusCachedTColor;
                DrawStatusLine(kStatusTbpmText, rightX, topY, blockWidth, lineHeight, shadowOffset, true);
                Styles.rightStatusStyle.normal.textColor = kStatusCachedCColor;
                DrawStatusLine(kStatusCbpmText, rightX, topY + lineHeight, blockWidth, lineHeight, shadowOffset, true);
                Styles.rightStatusStyle.normal.textColor = old;
            }
        }

        // Gold color when at exactly 100% accuracy.
        private const string AccuracyGoldHex = "#FFD700";

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
            hudCachedProgress = -999f;
        }

        // Centralized percent formatter — every HUD readout (Progress, Accuracy, XAccuracy,
        // Best, Timing Scale) routes through here so Main.settings.DecimalPlaces controls them all.
        // NaN/Infinity collapse to a clean perfect-run readout instead of "NaN%".
        internal static string FormatPercent(float ratio, bool goldAtPerfect = false)
        {
            if (float.IsNaN(ratio) || float.IsInfinity(ratio)) ratio = 1f;
            int decimals = Main.settings != null ? Mathf.Clamp(Main.settings.DecimalPlaces, 0, 6) : 2;
            float pct = ratio * 100f;
            string fmt = decimals == 0 ? "0" : "0." + new string('0', decimals);
            string body = pct.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture) + "%";
            // Half-of-the-last-digit tolerance keeps 99.99999... from showing as non-perfect
            // even though it would round up to 100 at every decimal precision.
            float perfectThreshold = 100f - 0.5f * Mathf.Pow(10f, -decimals);
            if (goldAtPerfect && pct >= perfectThreshold)
                return "<color=" + AccuracyGoldHex + ">" + body + "</color>";
            return body;
        }

        private static string FormatAccuracyPercent(float ratio) => FormatPercent(ratio, goldAtPerfect: true);

        // Progress text variant that shows "start% - now%" when the run began mid-level.
        // Threshold of 0.5 percentage point keeps the regular single-number readout for runs
        // that effectively start at 0 (rounding-error or tiny offsets shouldn't trigger the
        // range form).
        internal static string FormatProgressRange(float now)
        {
            if (ProgressTracker.RunStartProgress > 0.005f)
                return FormatPercent(ProgressTracker.RunStartProgress) + " - " + FormatPercent(now);
            return FormatPercent(now);
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

        private static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int m = (int)(seconds / 60f);
            int s = (int)(seconds % 60f);
            return m + ":" + s.ToString("00");
        }

        internal static string GetMusicTimeText()
        {
            try
            {
                AudioSource a = scrConductor.instance != null ? scrConductor.instance.song : null;
                if (a == null || a.clip == null) return "0:00 / 0:00";
                return FormatTime(a.time) + " / " + FormatTime(a.clip.length);
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
                float t = (float)cd.songposition_minusi;
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
                scnGame g = scnGame.instance;
                if (g == null || g.levelData == null) return FormatPercent(0f);
                string key = "best_" + (g.levelData.song ?? "") + "_" + (g.levelData.artist ?? "");
                float best = PlayerPrefs.GetFloat(key, 0f);
                return FormatPercent(best);
            }
            catch { return FormatPercent(0f); }
        }
        private static float fpsUpdateTimer;
        private static int displayedFps;
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
                displayedFps = Mathf.RoundToInt(smoothedFps);
            }

            return $"FPS | {displayedFps}";
        }

        private static void DrawStatusLine(string label, float x, float y, float width, float height, float shadowOffset, bool rightAligned)
        {
            GUIStyle shadowStyle = rightAligned ? Styles.rightStatusShadowStyle : Styles.percentShadowStyle;
            GUIStyle mainStyle = rightAligned ? Styles.rightStatusStyle : Styles.percentStyle;
            GUI.Label(new Rect(x + shadowOffset, y + shadowOffset, width, height), label, shadowStyle);
            GUI.Label(new Rect(x, y, width, height), label, mainStyle);
        }
    }
}
