using System;
using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static float smoothedFps;
        private static float updateTimer;
        private static int frameCount;

        private static void DrawStatusText(float progress, bool drawStatus, bool drawBpm)
        {
            EnsurePercentStyle();

            int fontSize = ScaledFont(18, 0.030f);
            float shadowOffset = Mathf.Max(2f, Mathf.Round(fontSize * 0.08f));
            float lineHeight = fontSize + Screen.height * 0.006f;
            percentStyle.fontSize = fontSize;
            percentShadowStyle.fontSize = fontSize;
            rightStatusStyle.fontSize = fontSize;
            rightStatusShadowStyle.fontSize = fontSize;

            float leftX = Screen.width * 0.012f;
            float topY = Screen.height * 0.013f;
            float blockWidth = Screen.width * 0.33f;
            float rightX = Screen.width - blockWidth - leftX;

            if (drawStatus)
            {
                int row = 0;
                if (settings.ShowProgress)
                    DrawStatusLine("Progress | " + Math.Round(progress * 100f, 2) + "%", leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (settings.ShowAccuracy)
                    DrawStatusLine("Accuracy | " + GetAccuracyText(), leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (settings.ShowXAccuracy)
                    DrawStatusLine("XAccuracy | " + GetXAccuracyText(), leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);

                if (settings.ShowMusicTime)
                {
                    string txt = IsMusicPlaying() ? GetMusicTimeText() : GetMapTimeText();
                    DrawStatusLine("Music/Map | " + txt, leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                }

                if (settings.ShowCheckpoint)
                    DrawStatusLine("Checkpoints | " + GetCheckpointCount(), leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (settings.ShowBest)
                    DrawStatusLine("Best | " + GetBestText(), leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
                if (settings.ShowFPS)
                    DrawStatusLine("FPS | " + Mathf.RoundToInt(GetSmoothedFps()), leftX, topY + lineHeight * row++, blockWidth, lineHeight, shadowOffset, false);
            }

            if (drawBpm)
            {
                float tileBpm;
                float actualBpm;
                GetBpmValues(out tileBpm, out actualBpm);
                Color tColor = LerpBpmColor(tileBpm);
                Color cColor = LerpBpmColor(actualBpm);
                Color old = rightStatusStyle.normal.textColor;
                rightStatusStyle.normal.textColor = tColor;
                DrawStatusLine("TBPM | " + Math.Round(tileBpm, 2), rightX, topY, blockWidth, lineHeight, shadowOffset, true);
                rightStatusStyle.normal.textColor = cColor;
                DrawStatusLine("CBPM | " + Math.Round(actualBpm, 2), rightX, topY + lineHeight, blockWidth, lineHeight, shadowOffset, true);
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
            updateTimer += Time.unscaledDeltaTime;
            frameCount++;

            float interval = settings.updInterval / 1000f;

            if (updateTimer >= interval)
            {
                smoothedFps = frameCount / updateTimer;
                updateTimer = 0f;
                frameCount = 0;
            }
            return smoothedFps;
        }

        private static void DrawStatusLine(string label, float x, float y, float width, float height, float shadowOffset, bool rightAligned)
        {
            Rect textRect = new Rect(x, y, width, height);
            GUIStyle shadowStyle = rightAligned ? rightStatusShadowStyle : percentShadowStyle;
            GUIStyle mainStyle = rightAligned ? rightStatusStyle : percentStyle;
            GUI.Label(new Rect(textRect.x + shadowOffset, textRect.y + shadowOffset, textRect.width, textRect.height), label, shadowStyle);
            GUI.Label(textRect, label, mainStyle);
        }
    }
}
