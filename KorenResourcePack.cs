using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace KorenResourcePack
{
    public static class Main
    {
        private const string HarmonyId = "koren.koren_resource_pack";

        private static Settings settings;
        private static UnityModManager.ModEntry mod;
        private static Harmony harmony;
        private static GUIStyle percentStyle;
        private static GUIStyle percentShadowStyle;
        private static GUIStyle rightStatusStyle;
        private static GUIStyle rightStatusShadowStyle;
        private static GUIStyle comboValueStyle;
        private static GUIStyle comboValueShadowStyle;
        private static GUIStyle judgementStyle;
        private static GUIStyle judgementShadowStyle;
        private static Font preferredHudFont;
        private static bool modEnabled = true;
        private static bool runVisible;
        private static int perfectCombo;
        private static readonly int[] judgementCounts = new int[12];
        private static int lastJudgementSlot = 4;
        private static float comboPulseStartTime = -1f;
        private static float comboPulsePeakScale = 1.24f;
        private static float comboPulseOutDuration = 0.075f;
        private static float comboPulseSettleDuration = 0.18f;
        private static Text trackedLevelNameText;
        private static Vector2 trackedLevelNameOriginalPosition;
        private static int trackedLevelNameOriginalFontSize;
        private static readonly float[] JudgementSlotWeights = { 0.85f, 1f, 1.1f, 1.2f, 1.7f, 1.2f, 1.1f, 1f, 0.85f };
        private static readonly Color[] JudgementSlotColors =
        {
            new Color(0.78f, 0.35f, 1f, 1f),
            new Color(1f, 0.22f, 0.22f, 1f),
            new Color(1f, 0.44f, 0.31f, 1f),
            new Color(0.63f, 1f, 0.31f, 1f),
            new Color(0.38f, 1f, 0.31f, 1f),
            new Color(0.63f, 1f, 0.31f, 1f),
            new Color(1f, 0.44f, 0.31f, 1f),
            new Color(1f, 0.22f, 0.22f, 1f),
            new Color(0.78f, 0.35f, 1f, 1f)
        };

        public class Settings : UnityModManager.ModSettings
        {
            public float size = 1f;

            public bool progressBarOn = true;
            public bool progressBarExpanded = false;
            public float ProgressBarFillR = 0.97f, ProgressBarFillG = 0.99f, ProgressBarFillB = 1.00f, ProgressBarFillA = 0.96f;
            public float ProgressBarBackR = 0.05f, ProgressBarBackG = 0.05f, ProgressBarBackB = 0.06f, ProgressBarBackA = 0.80f;
            public float ProgressBarBorderR = 0.98f, ProgressBarBorderG = 0.99f, ProgressBarBorderB = 1.00f, ProgressBarBorderA = 0.68f;

            public bool statusOn = true;
            public bool statusExpanded = false;
            public bool ShowProgress = true;
            public bool ShowAccuracy = false;
            public bool ShowXAccuracy = true;
            public bool ShowMusicTime = true;
            public bool ShowCheckpoint = false;
            public bool ShowBest = false;

            public bool bpmOn = true;
            public bool bpmExpanded = false;
            public float BpmColorMax = 8000f;
            public float BpmColorLowR = 1f, BpmColorLowG = 1f, BpmColorLowB = 1f, BpmColorLowA = 1f;
            public float BpmColorHighR = 1f, BpmColorHighG = 0f, BpmColorHighB = 1f, BpmColorHighA = 1f;

            public bool comboOn = true;
            public bool comboExpanded = false;
            public bool EnableAutoCombo = true;
            public int ComboColorMax = 1000;
            public float ComboColorLowR = 1f, ComboColorLowG = 1f, ComboColorLowB = 1f, ComboColorLowA = 1f;
            public float ComboColorHighR = 0.72f, ComboColorHighG = 0.35f, ComboColorHighB = 1f, ComboColorHighA = 1f;
            public bool ComboMoveUpNoCaption = false;

            public bool judgementOn = true;
            public bool judgementExpanded = false;
            public bool LocationUp = false;

            public bool holdOn = true;
            public bool holdExpanded = false;
            public float HoldOffsetX = 0f;
            public float HoldOffsetY = 0f;

            public override void Save(UnityModManager.ModEntry modEntry)
            {
                Save(this, modEntry);
            }
        }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            modEnabled = true;
            runVisible = false;
            perfectCombo = 0;
            ResetJudgementDisplay();
            comboPulseStartTime = -1f;

            try
            {
                settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();
            }
            catch (Exception ex)
            {
                modEntry.Logger.Log("[Warning] Settings load failed, using defaults: " + ex.Message);
                settings = new Settings();
            }

            modEntry.OnToggle = OnToggle;
            modEntry.OnFixedGUI = OnFixedGUI;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnUnload = OnUnload;

            harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(Main).Assembly);
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            modEntry.Logger.Log("koren resource pack loaded.");

            try
            {
                Thread updateThread = new Thread(() => CheckForUpdates(modEntry));
                updateThread.IsBackground = true;
                updateThread.Start();
            }
            catch (Exception ex)
            {
                modEntry.Logger.Log("[Warning] Update check failed to start: " + ex.Message);
            }

            return true;
        }

        private const string UpdateApiUrl = "https://api.github.com/repos/kkorenn/KorenResourcePack/releases/latest";

        private static void CheckForUpdates(UnityModManager.ModEntry modEntry)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(UpdateApiUrl);
                req.UserAgent = "KorenResourcePack-Updater";
                req.Accept = "application/vnd.github+json";
                req.Timeout = 8000;

                string json;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                {
                    json = r.ReadToEnd();
                }

                string latestTag = ExtractJsonString(json, "tag_name");
                if (string.IsNullOrEmpty(latestTag))
                {
                    modEntry.Logger.Log("[Update] No tag_name in release JSON.");
                    return;
                }

                string current = modEntry.Info.Version;
                if (!IsNewerVersion(current, latestTag))
                {
                    modEntry.Logger.Log("[Update] Up to date (" + current + ").");
                    return;
                }

                string zipUrl = ExtractAssetZipUrl(json);
                if (string.IsNullOrEmpty(zipUrl))
                {
                    modEntry.Logger.Log("[Update] " + latestTag + " available but no .zip asset found.");
                    return;
                }

                modEntry.Logger.Log("[Update] New version " + latestTag + " found. Downloading...");
                string tmpZip = Path.Combine(Path.GetTempPath(), "KorenResourcePack_update_" + Guid.NewGuid().ToString("N") + ".zip");
                HttpWebRequest dl = (HttpWebRequest)WebRequest.Create(zipUrl);
                dl.UserAgent = "KorenResourcePack-Updater";
                dl.Timeout = 30000;
                using (HttpWebResponse dlResp = (HttpWebResponse)dl.GetResponse())
                using (Stream s = dlResp.GetResponseStream())
                using (FileStream fs = File.Create(tmpZip))
                {
                    s.CopyTo(fs);
                }

                string tmpExtract = Path.Combine(Path.GetTempPath(), "KorenResourcePack_extract_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmpExtract);
                ZipFile.ExtractToDirectory(tmpZip, tmpExtract);

                string srcDll = FindFile(tmpExtract, "KorenResourcePack.dll");
                string srcInfo = FindFile(tmpExtract, "Info.json");
                if (srcDll == null || srcInfo == null)
                {
                    modEntry.Logger.Log("[Update] Zip missing dll or Info.json.");
                    return;
                }

                string modDir = modEntry.Path;
                File.Copy(srcDll, Path.Combine(modDir, "KorenResourcePack.dll"), true);
                File.Copy(srcInfo, Path.Combine(modDir, "Info.json"), true);

                try { File.Delete(tmpZip); } catch { }
                try { Directory.Delete(tmpExtract, true); } catch { }

                modEntry.Logger.Log("[Update] Installed " + latestTag + ". Restart game to apply.");
            }
            catch (Exception ex)
            {
                modEntry.Logger.Log("[Update] Failed: " + ex.Message);
            }
        }

        private static string ExtractJsonString(string json, string key)
        {
            Match m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string ExtractAssetZipUrl(string json)
        {
            foreach (Match m in Regex.Matches(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+)\""))
            {
                string url = m.Groups[1].Value;
                if (url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    return url;
                }
            }
            return null;
        }

        private static bool IsNewerVersion(string current, string latestTag)
        {
            try
            {
                Version cur = ParseVersion(current);
                Version lat = ParseVersion(latestTag);
                return lat > cur;
            }
            catch
            {
                return !string.Equals(current, latestTag.TrimStart('v', 'V'), StringComparison.OrdinalIgnoreCase);
            }
        }

        private static Version ParseVersion(string v)
        {
            string s = (v ?? "").TrimStart('v', 'V');
            int dash = s.IndexOf('-');
            if (dash >= 0) s = s.Substring(0, dash);
            string[] parts = s.Split('.');
            int[] nums = { 0, 0, 0, 0 };
            for (int i = 0; i < parts.Length && i < 4; i++)
            {
                int.TryParse(parts[i], out nums[i]);
            }
            return new Version(nums[0], nums[1], nums[2], nums[3]);
        }

        private static string FindFile(string root, string name)
        {
            foreach (string path in Directory.GetFiles(root, name, SearchOption.AllDirectories))
            {
                return path;
            }
            return null;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            modEnabled = value;

            if (!value)
            {
                DisableRuntimeState();
                modEntry.Logger.Log("koren resource pack disabled at runtime.");
                return true;
            }

            perfectCombo = 0;
            ResetJudgementDisplay();
            comboPulseStartTime = -1f;
            runVisible = DetectActiveRun();
            if (runVisible)
            {
                AdjustLevelNameUi();
            }

            modEntry.Logger.Log("koren resource pack enabled at runtime.");
            return true;
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            harmony?.UnpatchAll(HarmonyId);
            RestoreLevelNameUi();
            modEntry.Logger.Log("koren resource pack unloaded.");
            return true;
        }

        private static void OnSceneUnloaded(Scene _)
        {
            SetRunVisible(false, "sceneUnloaded");
        }

        private static void OnFixedGUI(UnityModManager.ModEntry modEntry)
        {
            if (settings == null || !modEnabled)
            {
                return;
            }

            AdjustLevelNameUi();

            float progress = GetLevelProgress();
            if (progress < 0f)
            {
                return;
            }

            if (settings.progressBarOn) DrawTopProgressBar(progress);
            if (settings.statusOn || settings.bpmOn)
            {
                DrawStatusText(progress, settings.statusOn, settings.bpmOn);
            }
            if (settings.comboOn)
            {
                DrawPerfectCombo();
            }
            if (settings.judgementOn)
            {
                DrawJudgementDisplay();
            }
            if (settings.holdOn) DrawHoldBehaviorLabel();
        }

        private static float GetLevelProgress()
        {
            try
            {
                if (!modEnabled)
                {
                    return -1f;
                }

                scrController controller = scrController.instance;

                if (!runVisible || controller == null || scrLevelMaker.instance == null || scrLevelMaker.instance.listFloors == null)
                {
                    return -1f;
                }

                if (controller.paused)
                {
                    return -1f;
                }

                if (scrLevelMaker.instance.listFloors.Count <= 1)
                {
                    return -1f;
                }

                return Mathf.Clamp01(controller.percentComplete);
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[Warning] Progress read failed: " + ex.Message);
                return -1f;
            }
        }

        private static void SetRunVisible(bool visible, string reason)
        {
            if (!modEnabled)
            {
                return;
            }

            if (runVisible == visible)
            {
                return;
            }

            runVisible = visible;
            mod?.Logger?.Log("[State] " + (visible ? "Show" : "Hide") + " via " + reason);
        }

        private static void ResetRunData(string reason)
        {
            perfectCombo = 0;
            ResetJudgementDisplay();
            comboPulseStartTime = -1f;
            mod?.Logger?.Log("[State] Reset run data via " + reason);
        }

        private static void DisableRuntimeState()
        {
            runVisible = false;
            perfectCombo = 0;
            ResetJudgementDisplay();
            comboPulseStartTime = -1f;
            RestoreLevelNameUi();
        }

        private static bool DetectActiveRun()
        {
            try
            {
                scrController controller = scrController.instance;
                return controller != null
                       && !controller.paused
                       && scrLevelMaker.instance != null
                       && scrLevelMaker.instance.listFloors != null
                       && scrLevelMaker.instance.listFloors.Count > 1;
            }
            catch
            {
                return false;
            }
        }

        private const float ProgressBarReferenceWidth = 1920f;
        private const float ProgressBarReferenceHeight = 1080f;
        private const float ProgressBarTargetWidth = 720f;
        private const float ProgressBarTargetHeight = 9f;
        private const float ProgressBarTargetTopOffset = 10f;

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

        private static Color LerpBpmColor(float bpm)
        {
            Color low = new Color(settings.BpmColorLowR, settings.BpmColorLowG, settings.BpmColorLowB, settings.BpmColorLowA);
            if (settings.BpmColorMax <= 0f) return low;
            float t = Mathf.Clamp01(bpm / settings.BpmColorMax);
            Color high = new Color(settings.BpmColorHighR, settings.BpmColorHighG, settings.BpmColorHighB, settings.BpmColorHighA);
            return Color.Lerp(low, high, t);
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

        private static void DrawPerfectCombo()
        {
            EnsurePercentStyle();

            float scale = EvaluateComboScale();
            int valueBaseSize = ScaledFont(56, 0.075f);
            int valueSize = Mathf.RoundToInt(valueBaseSize * scale);
            float shadowOffset = Mathf.Max(2f, Mathf.Round(valueSize * 0.05f));
            float centerX = Screen.width * 0.5f;
            float heightScale = Screen.height / ProgressBarReferenceHeight;
            float barTop = ProgressBarTargetTopOffset * heightScale;
            float barHeight = ProgressBarTargetHeight * heightScale;
            float verticalOffset = Screen.height * 0.030f;
            if (settings.ComboMoveUpNoCaption && IsSongCaptionEmpty()) verticalOffset -= Screen.height * 0.040f;
            float topY = Mathf.Max(0f, barTop + barHeight + verticalOffset);

            comboValueStyle.fontSize = valueSize;
            comboValueShadowStyle.fontSize = valueSize;

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

            comboValueStyle.normal.textColor = saved;
        }

        private static void DrawJudgementDisplay()
        {
            EnsurePercentStyle();

            int fontSize = ScaledFont(20, 0.035f);
            float shadowOffset = Mathf.Max(2f, Mathf.Round(fontSize * 0.08f));
            float baseY = Screen.height - Mathf.Max(4f, Screen.height * 0.006f) - fontSize;
            if (settings.LocationUp) baseY -= fontSize * 1.4f;

            judgementStyle.fontSize = fontSize;
            judgementShadowStyle.fontSize = fontSize;

            float totalWeight = 0f;
            for (int i = 0; i < JudgementSlotWeights.Length; i++)
            {
                totalWeight += JudgementSlotWeights[i];
            }

            float configuredWidth = Mathf.Max(180f, Screen.width * 0.13f);
            float gap = Mathf.Max(4f, fontSize * 0.18f);
            string[] values = new string[JudgementSlotWeights.Length];
            float[] textWidths = new float[JudgementSlotWeights.Length];
            float[] slotWidths = new float[JudgementSlotWeights.Length];
            float sumText = 0f;

            for (int i = 0; i < JudgementSlotWeights.Length; i++)
            {
                values[i] = GetJudgementSlotCount(i).ToString();
                textWidths[i] = judgementStyle.CalcSize(new GUIContent(values[i])).x;
                sumText += textWidths[i];
            }

            float requiredWidth = sumText + gap * (JudgementSlotWeights.Length - 1);
            float totalWidth = Mathf.Max(configuredWidth, requiredWidth);
            float extra = totalWidth - sumText - gap * (JudgementSlotWeights.Length - 1);

            for (int i = 0; i < JudgementSlotWeights.Length; i++)
            {
                float share = extra * (JudgementSlotWeights[i] / totalWeight);
                slotWidths[i] = textWidths[i] + share;
            }

            float startX = (Screen.width - totalWidth) * 0.5f;
            float[] centers = new float[JudgementSlotWeights.Length];
            float cursor = startX;
            for (int i = 0; i < JudgementSlotWeights.Length; i++)
            {
                centers[i] = cursor + slotWidths[i] * 0.5f;
                cursor += slotWidths[i] + gap;
            }

            Color oldColor = GUI.color;
            int oldDepth = GUI.depth;
            GUI.depth = -10000;

            for (int i = 0; i < JudgementSlotWeights.Length; i++)
            {
                float halfRectWidth = Mathf.Max(textWidths[i], slotWidths[i]) * 0.5f + 2f;
                Rect textRect = new Rect(centers[i] - halfRectWidth, baseY, halfRectWidth * 2f, fontSize + Screen.height * 0.009f);
                judgementStyle.normal.textColor = JudgementSlotColors[i];
                GUI.Label(new Rect(textRect.x + shadowOffset, textRect.y + shadowOffset, textRect.width, textRect.height), values[i], judgementShadowStyle);
                GUI.Label(textRect, values[i], judgementStyle);
            }

            GUI.depth = oldDepth;
            GUI.color = oldColor;
        }

        private static void DrawHoldBehaviorLabel()
        {
            EnsurePercentStyle();

            string label = GetHoldBehaviorLabel();
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            int fontSize = ScaledFont(16, 0.026f);
            float shadowOffset = Mathf.Max(1f, Mathf.Round(fontSize * 0.05f));
            float width = Mathf.Max(180f, Screen.width * 0.18f);
            float x = (Screen.width - width) * 0.87f + settings.HoldOffsetX;
            float y = Screen.height - Mathf.Max(28f, Screen.height * 0.05f) + settings.HoldOffsetY;

            judgementStyle.fontSize = fontSize;
            judgementShadowStyle.fontSize = fontSize;
            judgementStyle.normal.textColor = new Color(1f, 1f, 1f, 0.92f);

            Rect rect = new Rect(x, y, width, fontSize + 8f);

            int oldDepth = GUI.depth;
            GUI.depth = -10000;
            GUI.Label(new Rect(rect.x + shadowOffset, rect.y + shadowOffset, rect.width, rect.height), label, judgementShadowStyle);
            GUI.Label(rect, label, judgementStyle);
            GUI.depth = oldDepth;
        }

        private static string GetHoldBehaviorLabel()
        {
            try
            {
                HoldBehavior behavior = Persistence.holdBehavior;
                switch (behavior)
                {
                    case HoldBehavior.Normal:
                        return "Holds: Normal";
                    case HoldBehavior.CanHitEnd:
                        return "Holds: Hold Tap";
                    case HoldBehavior.NoHoldNeeded:
                        return "Holds: No Holding Required";
                    default:
                        return "Holds: " + behavior;
                }
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[Warning] Hold behavior read failed: " + ex.Message);
                return null;
            }
        }

        private static void AdjustLevelNameUi()
        {
            try
            {
                Text levelNameText = scrController.instance != null ? scrController.instance.txtLevelName : null;
                if (levelNameText == null)
                {
                    return;
                }

                if (trackedLevelNameText != levelNameText)
                {
                    trackedLevelNameText = levelNameText;
                    trackedLevelNameOriginalPosition = levelNameText.rectTransform.anchoredPosition;
                    trackedLevelNameOriginalFontSize = levelNameText.fontSize;
                }

                float yOffset = Mathf.Clamp(Screen.height * 0.072f, 44f, 88f);
                int fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.036f), 20, 40);

                levelNameText.resizeTextForBestFit = false;
                levelNameText.alignment = TextAnchor.MiddleCenter;
                levelNameText.fontSize = fontSize;
                levelNameText.rectTransform.anchoredPosition = trackedLevelNameOriginalPosition + new Vector2(0f, yOffset);
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[Warning] Level name UI adjust failed: " + ex.Message);
            }
        }

        private static void RestoreLevelNameUi()
        {
            if (trackedLevelNameText == null)
            {
                return;
            }

            try
            {
                trackedLevelNameText.fontSize = trackedLevelNameOriginalFontSize;
                trackedLevelNameText.rectTransform.anchoredPosition = trackedLevelNameOriginalPosition;
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[Warning] Level name UI restore failed: " + ex.Message);
            }
            finally
            {
                trackedLevelNameText = null;
            }
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

        private static bool IsSongCaptionEmpty()
        {
            try
            {
                scnGame g = scnGame.instance;
                if (g == null || g.levelData == null) return false;
                return string.IsNullOrWhiteSpace(g.levelData.song) && string.IsNullOrWhiteSpace(g.levelData.artist);
            }
            catch { return false; }
        }

        private static float smoothedFps = 60f;

        private static float GetSmoothedFps()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f)
            {
                float instant = 1f / dt;
                smoothedFps = Mathf.Lerp(smoothedFps, instant, 0.1f);
            }
            return smoothedFps;
        }

        private static void GetBpmValues(out float tileBpm, out float actualBpm)
        {
            tileBpm = 0f;
            actualBpm = 0f;

            try
            {
                scrController controller = scrController.instance;
                scrConductor conductor = scrConductor.instance;
                scrFloor floor = controller != null ? (controller.currFloor ?? controller.firstFloor) : null;

                if (controller == null || conductor == null || floor == null || conductor.song == null)
                {
                    return;
                }

                tileBpm = (float)(conductor.bpm * conductor.song.pitch * controller.speed);
                actualBpm = floor.nextfloor ? (float)(60.0 / (floor.nextfloor.entryTime - floor.entryTime) * conductor.song.pitch) : tileBpm;
            }
            catch
            {
                tileBpm = 0f;
                actualBpm = 0f;
            }
        }

        private static void DrawStatusLine(string label, float x, float y, float width, float height, float shadowOffset, bool rightAligned)
        {
            Rect textRect = new Rect(x, y, width, height);
            GUIStyle shadowStyle = rightAligned ? rightStatusShadowStyle : percentShadowStyle;
            GUIStyle mainStyle = rightAligned ? rightStatusStyle : percentStyle;
            GUI.Label(new Rect(textRect.x + shadowOffset, textRect.y + shadowOffset, textRect.width, textRect.height), label, shadowStyle);
            GUI.Label(textRect, label, mainStyle);
        }

        private static void DrawRoundedRect(Rect rect, Color color, float radius)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 1f, color, 0f, radius);
        }

        private static Texture2D ringTex;

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
            GUI.color = color;
            GUI.DrawTexture(rect, GetRingTexture(), ScaleMode.StretchToFill, true, 1f, color,
                new Vector4(thickness, thickness, thickness, thickness),
                new Vector4(radius, radius, radius, radius));
        }

        private static void EnsurePercentStyle()
        {
            if (percentStyle != null)
            {
                return;
            }

            Font hudFont = GetPreferredHudFont();
            percentStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                font = hudFont,
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 1f, 1f, 0.9f) }
            };

            percentShadowStyle = new GUIStyle(percentStyle);
            percentShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.28f);

            rightStatusStyle = new GUIStyle(percentStyle);
            rightStatusStyle.alignment = TextAnchor.UpperRight;

            rightStatusShadowStyle = new GUIStyle(percentShadowStyle);
            rightStatusShadowStyle.alignment = TextAnchor.UpperRight;

            comboValueStyle = new GUIStyle(percentStyle);
            comboValueStyle.alignment = TextAnchor.UpperCenter;
            comboValueStyle.fontStyle = FontStyle.Bold;
            comboValueStyle.normal.textColor = new Color(1f, 1f, 1f, 1f);

            comboValueShadowStyle = new GUIStyle(percentShadowStyle);
            comboValueShadowStyle.alignment = TextAnchor.UpperCenter;

            judgementStyle = new GUIStyle(percentStyle);
            judgementStyle.alignment = TextAnchor.UpperCenter;
            judgementStyle.fontStyle = FontStyle.Bold;

            judgementShadowStyle = new GUIStyle(percentShadowStyle);
            judgementShadowStyle.alignment = TextAnchor.UpperCenter;
        }

        private static float EvaluateComboScale()
        {
            if (comboPulseStartTime < 0f)
            {
                return 1f;
            }

            float elapsed = Time.realtimeSinceStartup - comboPulseStartTime;
            if (elapsed <= comboPulseOutDuration)
            {
                float t = elapsed / comboPulseOutDuration;
                float eased = t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                return Mathf.LerpUnclamped(1f, comboPulsePeakScale, eased);
            }

            float settleElapsed = elapsed - comboPulseOutDuration;
            if (settleElapsed >= comboPulseSettleDuration)
            {
                comboPulseStartTime = -1f;
                return 1f;
            }

            return Mathf.Lerp(comboPulsePeakScale, 1f, settleElapsed / comboPulseSettleDuration);
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

        private static void RegisterJudgementHit(HitMargin hit)
        {
            if (!modEnabled || !runVisible)
            {
                return;
            }

            int hitIndex = (int)hit;
            if (hitIndex >= 0 && hitIndex < judgementCounts.Length)
            {
                judgementCounts[hitIndex]++;
            }

            int slot = GetJudgementSlotForHit(hit);
            if (slot >= 0)
            {
                lastJudgementSlot = slot;
            }
        }

        private static void ResetJudgementDisplay()
        {
            Array.Clear(judgementCounts, 0, judgementCounts.Length);
            lastJudgementSlot = 4;
        }

        private static int GetJudgementSlotForHit(HitMargin hit)
        {
            switch (hit)
            {
                case HitMargin.FailOverload:
                    return 0;
                case HitMargin.TooEarly:
                    return 1;
                case HitMargin.VeryEarly:
                    return 2;
                case HitMargin.EarlyPerfect:
                    return 3;
                case HitMargin.Perfect:
                case HitMargin.Auto:
                    return 4;
                case HitMargin.LatePerfect:
                    return 5;
                case HitMargin.VeryLate:
                    return 6;
                case HitMargin.TooLate:
                    return 7;
                case HitMargin.FailMiss:
                    return 8;
                default:
                    return -1;
            }
        }

        private static int GetJudgementSlotCount(int slot)
        {
            switch (slot)
            {
                case 0:
                    return judgementCounts[(int)HitMargin.FailOverload];
                case 1:
                    return judgementCounts[(int)HitMargin.TooEarly];
                case 2:
                    return judgementCounts[(int)HitMargin.VeryEarly];
                case 3:
                    return judgementCounts[(int)HitMargin.EarlyPerfect];
                case 4:
                    return judgementCounts[(int)HitMargin.Perfect] + judgementCounts[(int)HitMargin.Auto];
                case 5:
                    return judgementCounts[(int)HitMargin.LatePerfect];
                case 6:
                    return judgementCounts[(int)HitMargin.VeryLate];
                case 7:
                    return judgementCounts[(int)HitMargin.TooLate];
                case 8:
                    return judgementCounts[(int)HitMargin.FailMiss];
                default:
                    return 0;
            }
        }

        private static int ScaledFont(int floor, float ratio)
        {
            float mult = (settings != null) ? Mathf.Clamp(settings.size, 0.3f, 3f) : 1f;
            return Mathf.Max(floor, Mathf.RoundToInt(Screen.height * ratio * mult));
        }

        private static Font GetPreferredHudFont()
        {
            if (preferredHudFont != null)
            {
                return preferredHudFont;
            }

            try
            {
                Text gameHudText = scrController.instance != null ? (scrController.instance.txtPercent ?? scrController.instance.txtLevelName) : null;
                if (gameHudText != null && gameHudText.font != null)
                {
                    preferredHudFont = gameHudText.font;
                    return preferredHudFont;
                }

                preferredHudFont = Font.CreateDynamicFontFromOSFont(
                    new[]
                    {
                        "DIN Alternate Bold",
                        "DIN Condensed",
                        "Avenir Next Demi Bold",
                        "Helvetica Neue",
                        "Trebuchet MS",
                        "Arial"
                    },
                    28);
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[Warning] Font fallback used: " + ex.Message);
            }

            if (preferredHudFont == null)
            {
                preferredHudFont = GUI.skin.label.font;
            }

            return preferredHudFont;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Size", GUILayout.Width(60f));
            settings.size = GUILayout.HorizontalSlider(settings.size, 0.5f, 2.0f, GUILayout.Width(240f));
            string sizeStr = GUILayout.TextField(settings.size.ToString("0.##"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(sizeStr, out parsed)) settings.size = Mathf.Clamp(parsed, 0.5f, 2.0f);
            GUILayout.EndHorizontal();

            DrawExpandable(ref settings.progressBarOn, ref settings.progressBarExpanded, "ProgressBar", DrawProgressBarBody);
            DrawExpandable(ref settings.statusOn, ref settings.statusExpanded, "Status", DrawStatusBody);
            DrawExpandable(ref settings.bpmOn, ref settings.bpmExpanded, "BPM", DrawBpmBody);
            DrawExpandable(ref settings.comboOn, ref settings.comboExpanded, "Combo", DrawComboBody);
            DrawExpandable(ref settings.judgementOn, ref settings.judgementExpanded, "Judgement", DrawJudgementBody);
            DrawExpandable(ref settings.holdOn, ref settings.holdExpanded, "Hold", DrawHoldBody);

            GUILayout.EndVertical();
        }

        private static string bpmColorMaxStr;
        private static string comboColorMaxStr;

        private static void DrawStatusBody()
        {
            DrawSubToggle(ref settings.ShowProgress, "Show progress");
            DrawSubToggle(ref settings.ShowAccuracy, "Show accuracy");
            DrawSubToggle(ref settings.ShowXAccuracy, "Show X-accuracy");
            DrawSubToggle(ref settings.ShowMusicTime, "Show music/map time");
            DrawSubToggle(ref settings.ShowCheckpoint, "Show checkpoint");
            DrawSubToggle(ref settings.ShowBest, "Show best");
        }

        private static void DrawProgressBarBody()
        {
            DrawSubColor(ref settings.ProgressBarFillR, ref settings.ProgressBarFillG, ref settings.ProgressBarFillB, ref settings.ProgressBarFillA, "Fill color", "pbFill");
            DrawSubColor(ref settings.ProgressBarBackR, ref settings.ProgressBarBackG, ref settings.ProgressBarBackB, ref settings.ProgressBarBackA, "Background color", "pbBack");
            DrawSubColor(ref settings.ProgressBarBorderR, ref settings.ProgressBarBorderG, ref settings.ProgressBarBorderB, ref settings.ProgressBarBorderA, "Border color", "pbBorder");
        }

        private static void DrawBpmBody()
        {
            DrawSubFloat(ref settings.BpmColorMax, ref bpmColorMaxStr, "BPM color max", 0f, 100000f);
            DrawSubColor(ref settings.BpmColorLowR, ref settings.BpmColorLowG, ref settings.BpmColorLowB, ref settings.BpmColorLowA, "Color (low)", "bpmLow");
            DrawSubColor(ref settings.BpmColorHighR, ref settings.BpmColorHighG, ref settings.BpmColorHighB, ref settings.BpmColorHighA, "Color (high)", "bpmHigh");
        }

        private static void DrawComboBody()
        {
            DrawSubToggle(ref settings.EnableAutoCombo, "Enable auto combo");
            DrawSubInt(ref settings.ComboColorMax, ref comboColorMaxStr, "Combo color max", 0, 1000000);
            DrawSubColor(ref settings.ComboColorLowR, ref settings.ComboColorLowG, ref settings.ComboColorLowB, ref settings.ComboColorLowA, "Color (low)", "comboLow");
            DrawSubColor(ref settings.ComboColorHighR, ref settings.ComboColorHighG, ref settings.ComboColorHighB, ref settings.ComboColorHighA, "Color (high)", "comboHigh");
            DrawSubToggle(ref settings.ComboMoveUpNoCaption, "Move up when no title/artist");
        }

        private static void DrawJudgementBody()
        {
            DrawSubToggle(ref settings.LocationUp, "Location up");
        }

        private static void DrawHoldBody()
        {
            DrawSubFloat(ref settings.HoldOffsetX, ref holdOffsetXStr, "X offset (px)", -4000f, 4000f);
            DrawSubFloat(ref settings.HoldOffsetY, ref holdOffsetYStr, "Y offset (px)", -4000f, 4000f);
        }

        private static string holdOffsetXStr;
        private static string holdOffsetYStr;

        private static void DrawSubToggle(ref bool on, string name)
        {
            EnsureFeatureStyles();
            on = GUILayout.Toggle(on, name, enableStyle);
        }

        private static readonly Dictionary<string, string> colorBuffers = new Dictionary<string, string>();
        private static readonly HashSet<string> colorExpanded = new HashSet<string>();

        private static string GetBuf(string key, string fallback)
        {
            string v;
            if (colorBuffers.TryGetValue(key, out v)) return v;
            colorBuffers[key] = fallback;
            return fallback;
        }

        private static void SetBuf(string key, string val) { colorBuffers[key] = val; }

        private static int Norm(float v) { return v <= 0f ? 0 : (v >= 1f ? 255 : Mathf.RoundToInt(v * 255f)); }

        private static string GetHex(float r, float g, float b, float a)
        {
            string s = Norm(r).ToString("X2") + Norm(g).ToString("X2") + Norm(b).ToString("X2");
            if (a < 1f) s += Norm(a).ToString("X2");
            return s;
        }

        private static bool ParseHex(string hex, out float r, out float g, out float b, out float a)
        {
            r = g = b = 0f; a = 1f;
            if (string.IsNullOrEmpty(hex)) return false;
            string h = hex.Trim().TrimStart('#');
            try
            {
                if (h.Length == 3 || h.Length == 4)
                {
                    r = Convert.ToInt32(h.Substring(0, 1), 16) / 15f;
                    g = Convert.ToInt32(h.Substring(1, 1), 16) / 15f;
                    b = Convert.ToInt32(h.Substring(2, 1), 16) / 15f;
                    if (h.Length == 4) a = Convert.ToInt32(h.Substring(3, 1), 16) / 15f;
                    return true;
                }
                if (h.Length == 6 || h.Length == 8)
                {
                    r = Convert.ToInt32(h.Substring(0, 2), 16) / 255f;
                    g = Convert.ToInt32(h.Substring(2, 2), 16) / 255f;
                    b = Convert.ToInt32(h.Substring(4, 2), 16) / 255f;
                    if (h.Length == 8) a = Convert.ToInt32(h.Substring(6, 2), 16) / 255f;
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static void DrawSubColor(ref float r, ref float g, ref float b, ref float a, string name, string key)
        {
            EnsureFeatureStyles();
            bool expanded = colorExpanded.Contains(key);
            GUILayout.BeginHorizontal();
            bool newExpanded = GUILayout.Toggle(expanded, expanded ? "◢" : "▶", expandStyle);
            Color old = GUI.color;
            GUI.color = new Color(r, g, b, a);
            GUILayout.Label("■", GUILayout.Width(20f));
            GUI.color = old;
            if (GUILayout.Button(name, GUI.skin.label, GUILayout.ExpandWidth(false))) newExpanded = !expanded;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (newExpanded != expanded) { if (newExpanded) colorExpanded.Add(key); else colorExpanded.Remove(key); }
            if (!newExpanded) return;

            GUILayout.BeginHorizontal();
            GUILayout.Space(24f);
            GUILayout.BeginVertical();

            string hexKey = key + ":hex";
            string hex = GetBuf(hexKey, GetHex(r, g, b, a));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hex", GUILayout.Width(40f));
            string newHex = GUILayout.TextField(hex, GUILayout.Width(100f));
            if (newHex != hex)
            {
                SetBuf(hexKey, newHex);
                float pr, pg, pb, pa;
                if (ParseHex(newHex, out pr, out pg, out pb, out pa))
                {
                    r = pr; g = pg; b = pb; a = pa;
                    SetBuf(key + ":r", null); SetBuf(key + ":g", null); SetBuf(key + ":b", null); SetBuf(key + ":a", null);
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawSubChannel(ref r, "R", key + ":r");
            DrawSubChannel(ref g, "G", key + ":g");
            DrawSubChannel(ref b, "B", key + ":b");
            DrawSubChannel(ref a, "A", key + ":a");

            SetBuf(hexKey, GetHex(r, g, b, a));

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static void DrawSubChannel(ref float val, string label, string bufKey)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(20f));
            float slid = GUILayout.HorizontalSlider(val, 0f, 1f, GUILayout.Width(180f));
            if (slid != val)
            {
                val = slid;
                colorBuffers[bufKey] = val.ToString("0.##");
            }
            string bufVal = colorBuffers.ContainsKey(bufKey) && colorBuffers[bufKey] != null ? colorBuffers[bufKey] : val.ToString("0.##");
            string newStr = GUILayout.TextField(bufVal, GUILayout.Width(60f));
            colorBuffers[bufKey] = newStr;
            float p;
            if (float.TryParse(newStr, out p))
            {
                float clamped = Mathf.Clamp01(p);
                if (clamped != val) val = clamped;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void DrawSubFloat(ref float val, ref string str, string name, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(180f));
            str = GUILayout.TextField(str ?? val.ToString("0.##"), GUILayout.Width(80f));
            float p;
            if (float.TryParse(str, out p)) val = Mathf.Clamp(p, min, max);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void DrawSubInt(ref int val, ref string str, string name, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(180f));
            str = GUILayout.TextField(str ?? val.ToString(), GUILayout.Width(80f));
            int p;
            if (int.TryParse(str, out p)) val = Mathf.Clamp(p, min, max);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static GUIStyle expandStyle;
        private static GUIStyle enableStyle;

        private static void EnsureFeatureStyles()
        {
            if (expandStyle == null)
            {
                expandStyle = new GUIStyle();
                expandStyle.fixedWidth = 10f;
                expandStyle.fontSize = 15;
                expandStyle.normal.textColor = Color.white;
                expandStyle.margin = new RectOffset(4, 2, 6, 6);
            }
            if (enableStyle == null)
            {
                enableStyle = new GUIStyle(GUI.skin.toggle);
                enableStyle.fontStyle = FontStyle.Normal;
                enableStyle.margin = new RectOffset(0, 4, 4, 4);
            }
        }

        private static void DrawExpandable(ref bool on, ref bool expanded, string name, Action body)
        {
            EnsureFeatureStyles();
            GUILayout.BeginHorizontal();
            expanded = GUILayout.Toggle(expanded, on ? (expanded ? "◢" : "▶") : "", expandStyle);
            on = GUILayout.Toggle(on, name, enableStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (expanded && on)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(24f);
                GUILayout.BeginVertical();
                if (body != null) body();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.Space(12f);
            }
        }

        private static void DrawSimpleToggle(ref bool on, string name)
        {
            EnsureFeatureStyles();
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            on = GUILayout.Toggle(on, name, enableStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        [HarmonyPatch(typeof(scnGame), "Play")]
        private static class ScnGamePlayPatch
        {
            private static void Postfix()
            {
                ResetRunData("scnGame.Play");
                SetRunVisible(true, "scnGame.Play");
            }
        }

        [HarmonyPatch(typeof(scrPressToStart), "ShowText")]
        private static class PressToStartShowTextPatch
        {
            private static void Postfix()
            {
                SetRunVisible(true, "scrPressToStart.ShowText");
            }
        }

        [HarmonyPatch(typeof(scnEditor), "ResetScene")]
        private static class EditorResetScenePatch
        {
            private static void Postfix()
            {
                ResetRunData("scnEditor.ResetScene");
                SetRunVisible(true, "scnEditor.ResetScene");
            }
        }

        [HarmonyPatch(typeof(scrController), "StartLoadingScene")]
        private static class ControllerStartLoadingScenePatch
        {
            private static void Postfix()
            {
                ResetRunData("scrController.StartLoadingScene");
                SetRunVisible(false, "scrController.StartLoadingScene");
            }
        }

        [HarmonyPatch(typeof(scrMistakesManager), "AddHit")]
        private static class MistakesManagerAddHitPatch
        {
            private static void Postfix(HitMargin hit)
            {
                RegisterComboHit(hit);
                RegisterJudgementHit(hit);
            }
        }
    }
}
