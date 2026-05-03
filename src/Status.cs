using System;
using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
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
                if (settings.ShowProgress)
                {
                    if (Mathf.Abs(progress - kStatusCachedProgress) > 0.0001f)
                    {
                        kStatusCachedProgress = progress;
                        kStatusProgressText = "Progress | " + Math.Round(progress * 100f, 2) + "%";
                    }
                }
                if (settings.ShowAccuracy) kStatusAccuracyText = "Accuracy | " + GetAccuracyText();
                if (settings.ShowXAccuracy) kStatusXAccuracyText = "XAccuracy | " + GetXAccuracyText();
                if (settings.ShowMusicTime)
                {
                    kStatusMusicTimeText = "Music/Map | " + (IsMusicPlaying() ? GetMusicTimeText() : GetMapTimeText());
                }
                if (settings.ShowCheckpoint)
                {
                    int cp = GetCheckpointCount();
                    if (cp != kStatusCachedCp)
                    {
                        kStatusCachedCp = cp;
                        kStatusCheckpointText = "Checkpoints | " + cp;
                    }
                }
                if (settings.ShowBest) kStatusBestText = "Best | " + GetBestText();
                if (settings.ShowFPS)
                {
                    int fps = Mathf.RoundToInt(GetSmoothedFps());
                    if (fps != kStatusCachedFps)
                    {
                        kStatusCachedFps = fps;
                        kStatusFpsText = "FPS | " + fps;
                    }
                }
            }

            if (drawBpm)
            {
                float tileBpm; float actualBpm;
                GetBpmValues(out tileBpm, out actualBpm);

                if (Mathf.Abs(tileBpm - kStatusCachedTBpmRaw) > 0.005f)
                {
                    kStatusCachedTBpmRaw = tileBpm;
                    kStatusTbpmText = "TBPM | " + Math.Round(tileBpm, 2);
                    kStatusCachedTColor = LerpBpmColor(tileBpm);
                }
                if (Mathf.Abs(actualBpm - kStatusCachedCBpmRaw) > 0.005f)
                {
                    kStatusCachedCBpmRaw = actualBpm;
                    kStatusCbpmText = "CBPM | " + Math.Round(actualBpm, 2);
                    kStatusCachedCColor = LerpBpmColor(actualBpm);
                }
            }
        }

        private static void DrawStatusText(float progress, bool drawStatus, bool drawBpm)
        {
            EnsurePercentStyle();

            RefreshStatusCacheIfDue(progress, drawStatus, drawBpm);

            int fontSize = ScaledFont(18, 0.030f);
            float shadowOffset = Mathf.Max(2f, Mathf.Round(fontSize * 0.08f));
            float lineHeight = fontSize + Screen.height * 0.006f;
            percentStyle.fontSize = fontSize;
            percentShadowStyle.fontSize = fontSize;
            rightStatusStyle.fontSize = fontSize;
            rightStatusShadowStyle.fontSize = fontSize;

            float screenW = Screen.width;
            float leftX = screenW * 0.012f;
            float topY = Screen.height * 0.013f;
            float blockWidth = screenW * 0.33f;
            float rightX = screenW - blockWidth - leftX;

            if (drawStatus)
            {
                int row = 0;
                if (settings.ShowProgress)
                    DrawStatusLine(kStatusProgressText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (settings.ShowAccuracy)
                    DrawStatusLine(kStatusAccuracyText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (settings.ShowXAccuracy)
                    DrawStatusLine(kStatusXAccuracyText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (settings.ShowMusicTime)
                    DrawStatusLine(kStatusMusicTimeText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (settings.ShowCheckpoint)
                    DrawStatusLine(kStatusCheckpointText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (settings.ShowBest)
                    DrawStatusLine(kStatusBestText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (settings.ShowFPS)
                    DrawStatusLine(kStatusFpsText, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
            }

            if (drawBpm)
            {
                Color old = rightStatusStyle.normal.textColor;
                rightStatusStyle.normal.textColor = kStatusCachedTColor;
                DrawStatusLine(kStatusTbpmText, rightX, topY, blockWidth, lineHeight, shadowOffset, true);
                rightStatusStyle.normal.textColor = kStatusCachedCColor;
                DrawStatusLine(kStatusCbpmText, rightX, topY + lineHeight, blockWidth, lineHeight, shadowOffset, true);
                rightStatusStyle.normal.textColor = old;
            }
        }

        private static string GetAccuracyText()
        {
            try
            {
                scrMistakesManager m = scrController.instance != null ? scrController.instance.mistakesManager : null;
                float a = m != null ? m.percentAcc : 1f;
                return Math.Round(a * 100f, 2) + "%";
            }
            catch { return "100%"; }
        }

        private static string GetXAccuracyText()
        {
            try
            {
                scrMistakesManager mistakesManager = scrController.instance != null ? scrController.instance.mistakesManager : null;
                float xAccuracy = mistakesManager != null ? mistakesManager.percentXAcc : 1f;
                return Math.Round(xAccuracy * 100f, 2) + "%";
            }
            catch
            {
                return "100%";
            }
        }

        private static bool IsMusicPlaying()
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

        private static string GetMusicTimeText()
        {
            try
            {
                AudioSource a = scrConductor.instance != null ? scrConductor.instance.song : null;
                if (a == null || a.clip == null) return "0:00 / 0:00";
                return FormatTime(a.time) + " / " + FormatTime(a.clip.length);
            }
            catch { return "0:00 / 0:00"; }
        }

        private static string GetMapTimeText()
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

        private static int GetCheckpointCount()
        {
            try { return scnGame.instance != null ? scnGame.instance.checkpointsUsed : 0; }
            catch { return 0; }
        }

        private static string GetBestText()
        {
            try
            {
                scnGame g = scnGame.instance;
                if (g == null || g.levelData == null) return "0%";
                string key = "best_" + (g.levelData.song ?? "") + "_" + (g.levelData.artist ?? "");
                float best = PlayerPrefs.GetFloat(key, 0f);
                return Math.Round(best * 100f, 2) + "%";
            }
            catch { return "0%"; }
        }

        private static float GetSmoothedFps()
        {
            float fps = 1f / Time.unscaledDeltaTime;
            smoothedFps = Mathf.Lerp(smoothedFps, fps, 0.1f);
            return smoothedFps;
        }

        private static void DrawStatusLine(string label, float x, float y, float width, float height, float shadowOffset, bool rightAligned)
        {
            GUIStyle shadowStyle = rightAligned ? rightStatusShadowStyle : percentShadowStyle;
            GUIStyle mainStyle = rightAligned ? rightStatusStyle : percentStyle;
            GUI.Label(new Rect(x + shadowOffset, y + shadowOffset, width, height), label, shadowStyle);
            GUI.Label(new Rect(x, y, width, height), label, mainStyle);
        }
    }
}
