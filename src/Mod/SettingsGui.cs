using System;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;
using static KorenResourcePack.Main;

namespace KorenResourcePack
{
    internal static class SettingsGui
    {
        private static string bpmColorMaxStr;
        private static string comboColorMaxStr;

        private static GUIStyle expandStyle;
        private static GUIStyle enableStyle;
        private static GUIStyle simpleSlotButtonStyle;
        private static bool fontDropdownOpen;
        private static List<string> cachedHudFontChoices;
        private static bool cachedHudFontChoicesFromBundle;
        private static int cachedHudFontBundleCount = -1;

        private static string T(string key) { return Localization.Text(key); }
        private static string Tf(string key, params object[] args) { return Localization.Format(key, args); }

        private static void SetLanguage(string language)
        {
            if (Localization.SetLanguage(language))
                GUI.changed = true;
        }

        private static List<string> GetHudFontChoices()
        {
            BundleLoader.EnsureBundleLoaded();
            bool useBundle = BundleLoader.BundleAvailable && BundleLoader.bundleFonts.Count > 0;
            int bundleCount = useBundle ? BundleLoader.bundleFonts.Count : 0;
            if (cachedHudFontChoices != null
                && cachedHudFontChoicesFromBundle == useBundle
                && cachedHudFontBundleCount == bundleCount)
            {
                return cachedHudFontChoices;
            }
            if (useBundle)
            {
                var list = new List<string>(BundleLoader.bundleFonts.Keys);
                list.Sort(StringComparer.OrdinalIgnoreCase);
                cachedHudFontChoices = list;
                cachedHudFontChoicesFromBundle = true;
                cachedHudFontBundleCount = bundleCount;
                return cachedHudFontChoices;
            }

            cachedHudFontChoices = FontLoader.GetBundledFontNames();
            cachedHudFontChoicesFromBundle = false;
            cachedHudFontBundleCount = 0;
            return cachedHudFontChoices;
        }

        private static readonly Dictionary<string, string> colorBuffers = new Dictionary<string, string>();
        private static readonly HashSet<string> colorExpanded = new HashSet<string>();

        private static bool settingsDirty;
        private static float settingsDirtySince;
        private const float SettingsAutosaveQuietSeconds = 0.6f;

        private static bool pendingResourceChangerOnSet;
        private static bool pendingResourceChangerOnValue;
        private static bool pendingChangeOttoIconSet;
        private static bool pendingChangeOttoIconValue;
        private static bool pendingChangeBallColorSet;
        private static bool pendingChangeBallColorValue;
        private static bool pendingChangeTileColorSet;
        private static bool pendingChangeTileColorValue;
        private static bool pendingResourceChangerFullActionSet;
        private static bool pendingResourceChangerFullActionRefresh;
        private static bool pendingRefreshOttoIcon;
        private static bool pendingRestoreOttoIcon;
        private static bool pendingRefreshPlanetColors;
        private static bool pendingRestorePlanetColors;
        private static bool pendingRefreshTileColors;
        private static bool pendingRestoreTileColors;

        internal static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            
            if (Event.current != null && Event.current.type == EventType.Layout)
                GUI.changed = false;

            Event __krpEv = Event.current;
            if (!Main.settings.KeyLimiterOn && (keyLimiterCapturing || keyLimiterPendingCaptureKey != (int)KeyCode.None))
            {
                StopKeyLimiterCapture();
            }

            if (!keyLimiterCapturing
                && __krpEv != null
                && (__krpEv.type == EventType.KeyDown || __krpEv.type == EventType.KeyUp)
                && (__krpEv.keyCode == KeyCode.LeftShift || __krpEv.keyCode == KeyCode.RightShift))
            {
                __krpEv.Use();
            }

            Localization.SetLanguage(Main.settings.language);
            Main.settings.EnsureColorRanges();
            bool prevFmodEnabled = Main.settings.FmodEnabled;
            GUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.size"), GUILayout.Width(60f));
            Main.settings.size = GUILayout.HorizontalSlider(Main.settings.size, 0.5f, 2.0f, GUILayout.Width(240f));
            string sizeStr = GUILayout.TextField(Main.settings.size.ToString("0.##"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(sizeStr, out parsed)) Main.settings.size = Mathf.Clamp(parsed, 0.5f, 2.0f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.language"), GUILayout.Width(100f));
            bool enSelected = string.Equals(Localization.CurrentLanguage, "en", StringComparison.OrdinalIgnoreCase);
            bool krSelected = string.Equals(Localization.CurrentLanguage, "kr", StringComparison.OrdinalIgnoreCase);
            if (GUILayout.Toggle(enSelected, T("language.en"), GUILayout.Width(120f)) && !enSelected)
                SetLanguage("en");
            if (GUILayout.Toggle(krSelected, T("language.kr"), GUILayout.Width(120f)) && !krSelected)
                SetLanguage("kr");
            GUILayout.EndHorizontal();

            List<string> fontChoices = GetHudFontChoices();
            if (fontChoices.Count > 0 && string.IsNullOrEmpty(Main.settings.fontName))
            {
                Main.settings.fontName = fontChoices[0];
                FontLoader.InvalidatePreferredHudFont();
                Overlay.InvalidateOverlayFontCache();
            }
            else if (BundleLoader.BundleAvailable && fontChoices.Count > 0 && !string.IsNullOrEmpty(Main.settings.fontName))
            {
                if (!BundleLoader.bundleFonts.ContainsKey(Main.settings.fontName))
                {
                    Main.settings.fontName = fontChoices[0];
                    FontLoader.InvalidatePreferredHudFont();
                    Overlay.InvalidateOverlayFontCache();
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.font"), GUILayout.Width(100f));
            string current = string.IsNullOrEmpty(Main.settings.fontName) ? "—" : Main.settings.fontName;
            string arrow = fontDropdownOpen ? " ▲" : " ▼";
            if (GUILayout.Button(current + arrow, GUILayout.Width(280f)))
            {
                fontDropdownOpen = !fontDropdownOpen;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (fontDropdownOpen && fontChoices.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(110f);
                GUILayout.BeginVertical();
                foreach (string name in fontChoices)
                {
                    bool selected = string.Equals(Main.settings.fontName, name, StringComparison.OrdinalIgnoreCase);
                    string label = selected ? "● " + name : "○ " + name;
                    if (GUILayout.Button(label, GUI.skin.label, GUILayout.ExpandWidth(false)))
                    {
                        Main.settings.fontName = name;
                        FontLoader.InvalidatePreferredHudFont();
                        Overlay.InvalidateOverlayFontCache();
                        fontDropdownOpen = false;
                    }
                }
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            DrawExpandable(ref Main.settings.progressBarOn, ref Main.settings.progressBarExpanded, T("feature.progressBar"), DrawProgressBarBody);
            DrawExpandable(ref Main.settings.statusOn, ref Main.settings.statusExpanded, T("feature.status"), DrawStatusBody);
            DrawExpandable(ref Main.settings.bpmOn, ref Main.settings.bpmExpanded, T("feature.bpm"), DrawBpmBody);
            DrawExpandable(ref Main.settings.comboOn, ref Main.settings.comboExpanded, T("feature.combo"), DrawComboBody);
            DrawExpandable(ref Main.settings.judgementOn, ref Main.settings.judgementExpanded, T("feature.judgement"), DrawJudgementBody);
            DrawExpandable(ref Main.settings.holdOn, ref Main.settings.holdExpanded, T("feature.hold"), DrawHoldBody);
            DrawExpandable(ref Main.settings.attemptOn, ref Main.settings.attemptExpanded, T("feature.attempt"), DrawAttemptBody);
            DrawExpandable(ref Main.settings.timingScaleOn, ref Main.settings.timingScaleExpanded, T("feature.timingScale"), DrawTimingScaleBody);
            DrawExpandable(ref Main.settings.keyViewerOn, ref Main.settings.keyViewerExpanded, T("feature.keyViewer"), DrawKeyViewerBody);
            DrawResourceChangerExpandable(T("feature.resourceChanger"));
            DrawTweaksExpandable(T("feature.tweaks"));
            DrawEffectRemoverExpandable(T("feature.effectRemover"));
            DrawExpandable(ref Main.settings.KCBOn, ref Main.settings.KCBExpanded, T("feature.kcb"), DrawKCBBody);
            DrawExpandable(ref Main.settings.KeyLimiterOn, ref Main.settings.KeyLimiterExpanded, T("feature.keyLimiter"), DrawKeyLimiterBody);
            DrawExpandable(ref Main.settings.JRestrictOn, ref Main.settings.JRestrictExpanded, T("feature.jrestrict"), DrawJRestrictBody);
            DrawExpandable(ref Main.settings.FmodEnabled, ref Main.settings.FmodExpanded, T("feature.fmod"), DrawFmodBody);
            GUILayout.EndVertical();

            if (prevFmodEnabled != Main.settings.FmodEnabled)
            {
                KorenResourcePack.Audio.Fmod.SetEnabled(Main.settings.FmodEnabled, modEntry);
                GUI.changed = true;
            }

            AutosaveTick(modEntry);
        }

        private static void DrawStatusBody()
        {
            DrawSubToggle(ref Main.settings.ShowProgress, T("status.showProgress"));
            if (Main.settings.ShowProgress)
                DrawColorRange(ref Main.settings.ProgressColor, T("status.progressColor"), "statusProgressColor", Settings.KorenProgressColor());
            DrawSubToggle(ref Main.settings.ShowAccuracy, T("status.showAccuracy"));
            if (Main.settings.ShowAccuracy)
                DrawColorRange(ref Main.settings.AccuracyColor, T("status.accuracyColor"), "statusAccuracyColor", Settings.KorenAccuracyColor());
            DrawSubToggle(ref Main.settings.ShowXAccuracy, T("status.showXAccuracy"));
            if (Main.settings.ShowXAccuracy)
                DrawColorRange(ref Main.settings.XAccuracyColor, T("status.xAccuracyColor"), "statusXAccuracyColor", Settings.KorenAccuracyColor());
            DrawSubToggle(ref Main.settings.ShowMusicTime, T("status.showMusicTime"));
            if (Main.settings.ShowMusicTime)
                DrawColorRange(ref Main.settings.MusicTimeColor, T("status.musicTimeColor"), "statusMusicTimeColor", Settings.WhiteColorRange());
            DrawSubToggle(ref Main.settings.ShowMapTime, T("status.showMapTime"));
            DrawSubToggle(ref Main.settings.ShowMapTimeIfNotMusic, T("status.useMapTimeNoMusic"));
            if (Main.settings.ShowMapTime)
                DrawColorRange(ref Main.settings.MapTimeColor, T("status.mapTimeColor"), "statusMapTimeColor", Settings.WhiteColorRange());
            DrawSubToggle(ref Main.settings.ShowCheckpoint, T("status.showCheckpoint"));
            DrawSubToggle(ref Main.settings.ShowBest, T("status.showBest"));
            if (Main.settings.ShowBest)
                DrawColorRange(ref Main.settings.BestColor, T("status.bestColor"), "statusBestColor", Settings.KorenProgressColor());
            DrawSubToggle(ref Main.settings.ShowFPS, T("status.showFps"));
            DrawSubToggle(ref Main.settings.HideDebugText, T("status.hideDebug"));
            DrawDecimalPlacesRow(T("status.decimals"));
        }

        private static string decimalPlacesBuf;
        private static void DrawDecimalPlacesRow(string label)
        {
            int prev = Main.settings.DecimalPlaces;
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(label, GUILayout.Width(180f));
            float slid = GUILayout.HorizontalSlider(Main.settings.DecimalPlaces, 0f, 6f, GUILayout.Width(180f));
            int slidI = Mathf.RoundToInt(slid);
            if (slidI != Main.settings.DecimalPlaces)
            {
                Main.settings.DecimalPlaces = Mathf.Clamp(slidI, 0, 6);
                decimalPlacesBuf = Main.settings.DecimalPlaces.ToString();
            }
            decimalPlacesBuf = GUILayout.TextField(decimalPlacesBuf ?? Main.settings.DecimalPlaces.ToString(), GUILayout.Width(40f));
            int parsed;
            if (int.TryParse(decimalPlacesBuf, out parsed)) Main.settings.DecimalPlaces = Mathf.Clamp(parsed, 0, 6);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (Main.settings.DecimalPlaces != prev) Status.InvalidatePercentCaches();
        }

        private static void DrawProgressBarBody()
        {
            DrawColorRange(ref Main.settings.ProgressBarFillColor, T("color.fill"), "pbFillRange", Settings.KorenProgressBarFillColor());
            DrawColorRange(ref Main.settings.ProgressBarBackColor, T("color.background"), "pbBackRange", Settings.KorenProgressBarBackgroundColor());
            DrawColorRange(ref Main.settings.ProgressBarBorderColor, T("color.border"), "pbBorderRange", Settings.KorenProgressBarBorderColor());
        }

        private static void DrawBpmBody()
        {
            DrawSubFloat(ref Main.settings.BpmColorMax, ref bpmColorMaxStr, T("color.bpmMax"), 0f, 100000f);
            DrawColorRange(ref Main.settings.BpmColor, T("color.bpm"), "bpmColor", Settings.KorenBpmColor());
        }

        private static void DrawComboBody()
        {
            DrawSubToggle(ref Main.settings.EnableAutoCombo, T("combo.auto"));
            if (XPerfectBridge.Installed)
            {
                DrawSubToggle(ref Main.settings.XPerfectComboEnabled, T("combo.xperfectOnly"));
            }
            DrawSubInt(ref Main.settings.ComboColorMax, ref comboColorMaxStr, T("color.comboMax"), 0, 1000000);
            DrawColorRange(ref Main.settings.ComboColor, T("color.combo"), "comboColorRange", Settings.KorenComboColor());
            DrawSubToggle(ref Main.settings.ComboMoveUpNoCaption, T("combo.moveUpNoCaption"));
            DrawExpandable(ref Main.settings.CaptionText, ref Main.settings.captionExpanded, T("combo.captionText"), DrawPerfectComboExpanded);
            DrawSubToggle(ref Main.settings.comboFastAnim, T("combo.snappy"));
            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.yOffset"), GUILayout.Width(100f));
            Main.settings.comboY = GUILayout.HorizontalSlider(Main.settings.comboY, -200, 200, GUILayout.Width(240f));
            string comboYStr = GUILayout.TextField(Main.settings.comboY.ToString("0"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(comboYStr, out parsed)) Main.settings.comboY = Mathf.Clamp(parsed, -200, 200);
            GUILayout.EndHorizontal();
        }

        private static void DrawPerfectComboExpanded()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.position"), GUILayout.Width(80f));
            Main.settings.captionY = GUILayout.HorizontalSlider(Main.settings.captionY, -100, 200, GUILayout.Width(240f));
            string perfectComboStr = GUILayout.TextField(Main.settings.captionY.ToString("0"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(perfectComboStr, out parsed)) Main.settings.captionY = Mathf.Clamp(parsed, -100, 200);
            GUILayout.EndHorizontal();
        }

        private static void DrawJudgementBody()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.location"), GUILayout.Width(90f));
            Main.settings.judgementPositionY = GUILayout.HorizontalSlider(Main.settings.judgementPositionY, -100, 200, GUILayout.Width(240f));
            string judgementPositionYStr = GUILayout.TextField(Main.settings.judgementPositionY.ToString("0"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(judgementPositionYStr, out parsed)) Main.settings.judgementPositionY = Mathf.Clamp(parsed, -100, 200);
            GUILayout.EndHorizontal();
        }

        private static void DrawHoldBody()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.xOffsetPx"), GUILayout.Width(140f));
            Main.settings.HoldOffsetX = GUILayout.HorizontalSlider(Main.settings.HoldOffsetX, -200f, 200f, GUILayout.Width(240f));
            string holdOffsetXStr = GUILayout.TextField(Main.settings.HoldOffsetX.ToString("0"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(holdOffsetXStr, out parsed)) Main.settings.HoldOffsetX = Mathf.Clamp(parsed, -200f, 200f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.yOffsetPx"), GUILayout.Width(140f));
            Main.settings.HoldOffsetY = GUILayout.HorizontalSlider(Main.settings.HoldOffsetY, -200f, 200f, GUILayout.Width(240f));
            string holdOffsetYStr = GUILayout.TextField(Main.settings.HoldOffsetY.ToString("0"), GUILayout.Width(60f));
            float parsed2;
            if (float.TryParse(holdOffsetYStr, out parsed2)) Main.settings.HoldOffsetY = Mathf.Clamp(parsed2, -200f, 200f);
            GUILayout.EndHorizontal();
        }

        private static void DrawSubToggle(ref bool on, string name)
        {
            EnsureFeatureStyles();
            on = GUILayout.Toggle(on, name, enableStyle);
        }

        private static string GetBuf(string key, string fallback)
        {
            string v;
            if (colorBuffers.TryGetValue(key, out v) && v != null) return v;
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

            DrawColorEditor(ref r, ref g, ref b, ref a, key);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static void DrawColorRange(ref ColorRange range, string name, string key, ColorRange defaults)
        {
            EnsureFeatureStyles();
            if (range == null) range = defaults != null ? defaults.Clone() : Settings.WhiteColorRange();
            range.EnsureDefault(defaults);

            bool expanded = colorExpanded.Contains(key);
            GUILayout.BeginHorizontal();
            bool newExpanded = GUILayout.Toggle(expanded, expanded ? "◢" : "▶", expandStyle);
            Color old = GUI.color;
            GUI.color = range.GetColor(1f);
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

            if (GUILayout.Button(T("common.addColor"), GUILayout.Width(120f)))
            {
                float p = range.Points != null && range.Points.Count > 0 ? 0.5f : 1f;
                range.AddPoint(p, range.GetColor(p));
            }

            bool shouldSort = false;
            bool deleted = false;
            for (int i = 0; range.Points != null && i < range.Points.Count; i++)
            {
                ColorRangePoint point = range.Points[i];
                if (point == null) continue;
                point.Clamp();

                string pointKey = key + ":point:" + i;
                bool pointExpanded = colorExpanded.Contains(pointKey);

                GUILayout.BeginHorizontal();
                bool newPointExpanded = GUILayout.Toggle(pointExpanded, pointExpanded ? "◢" : "▶", expandStyle);
                old = GUI.color;
                GUI.color = point.ToColor();
                GUILayout.Label("■", GUILayout.Width(20f));
                GUI.color = old;
                string label = (point.Progress * 100f).ToString("0.##") + "%";
                if (GUILayout.Button(label, GUI.skin.label, GUILayout.ExpandWidth(false))) newPointExpanded = !pointExpanded;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (newPointExpanded != pointExpanded)
                {
                    if (newPointExpanded) colorExpanded.Add(pointKey);
                    else colorExpanded.Remove(pointKey);
                }
                if (!newPointExpanded) continue;

                GUILayout.BeginHorizontal();
                GUILayout.Space(24f);
                GUILayout.BeginVertical();

                if (DrawRangeProgress(point, pointKey + ":progress")) shouldSort = true;
                DrawColorEditor(ref point.R, ref point.G, ref point.B, ref point.A, pointKey + ":color");

                if (GUILayout.Button(T("common.delete"), GUILayout.Width(90f)))
                {
                    range.Points.RemoveAt(i);
                    deleted = true;
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.Space(8f);

                if (deleted) break;
            }

            if (shouldSort || deleted) range.Normalize();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static bool DrawRangeProgress(ColorRangePoint point, string key)
        {
            float old = point.Progress;
            GUILayout.BeginHorizontal();
            GUILayout.Label(T("color.percent"), GUILayout.Width(70f));
            float slid = GUILayout.HorizontalSlider(point.Progress, 0f, 1f, GUILayout.Width(180f));
            if (Mathf.Abs(slid - point.Progress) > 0.0001f)
            {
                point.Progress = slid;
                colorBuffers[key] = point.Progress.ToString("0.##");
            }
            string bufVal = colorBuffers.ContainsKey(key) && colorBuffers[key] != null ? colorBuffers[key] : point.Progress.ToString("0.##");
            string newStr = GUILayout.TextField(bufVal, GUILayout.Width(60f));
            colorBuffers[key] = newStr;
            float parsed;
            if (float.TryParse(newStr, out parsed)) point.Progress = Mathf.Clamp01(parsed);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            point.Clamp();
            return Mathf.Abs(old - point.Progress) > 0.0001f;
        }

        private static void DrawColorEditor(ref float r, ref float g, ref float b, ref float a, string key)
        {
            string hexKey = key + ":hex";
            string ctrlName = hexKey + ":ctrl";
            string hex = GetBuf(hexKey, GetHex(r, g, b, a));
            GUILayout.BeginHorizontal();
            GUILayout.Label(T("color.hex"), GUILayout.Width(40f));
            GUI.SetNextControlName(ctrlName);
            string newHex = GUILayout.TextField(hex, GUILayout.Width(100f));
            bool hexFocused = GUI.GetNameOfFocusedControl() == ctrlName;
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

            if (!hexFocused)
                SetBuf(hexKey, GetHex(r, g, b, a));
        }

        private static void DrawSubColorRgb(ref float r, ref float g, ref float b, string name, string key)
        {
            EnsureFeatureStyles();
            bool expanded = colorExpanded.Contains(key);
            GUILayout.BeginHorizontal();
            bool newExpanded = GUILayout.Toggle(expanded, expanded ? "◢" : "▶", expandStyle);
            Color old = GUI.color;
            GUI.color = new Color(r, g, b, 1f);
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
            string ctrlName = hexKey + ":ctrl";
            string hex = GetBuf(hexKey, GetHex(r, g, b, 1f));
            GUILayout.BeginHorizontal();
            GUILayout.Label(T("color.hex"), GUILayout.Width(40f));
            GUI.SetNextControlName(ctrlName);
            string newHex = GUILayout.TextField(hex, GUILayout.Width(100f));
            bool hexFocused = GUI.GetNameOfFocusedControl() == ctrlName;
            if (newHex != hex)
            {
                SetBuf(hexKey, newHex);
                float pr, pg, pb, pa;
                if (ParseHex(newHex, out pr, out pg, out pb, out pa))
                {
                    r = pr; g = pg; b = pb;
                    SetBuf(key + ":r", null); SetBuf(key + ":g", null); SetBuf(key + ":b", null);
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawSubChannel(ref r, "R", key + ":r");
            DrawSubChannel(ref g, "G", key + ":g");
            DrawSubChannel(ref b, "B", key + ":b");

            if (!hexFocused)
                SetBuf(hexKey, GetHex(r, g, b, 1f));

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
            if (simpleSlotButtonStyle == null)
            {
                simpleSlotButtonStyle = new GUIStyle(GUI.skin.button);
                simpleSlotButtonStyle.fixedWidth = 56f;
                simpleSlotButtonStyle.fixedHeight = 24f;
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

        private static void DrawResourceChangerExpandable(string name)
        {
            ApplyPendingResourceChangerGuiChanges();

            EnsureFeatureStyles();
            GUILayout.BeginHorizontal();
            Main.settings.ResourceChangerExpanded = GUILayout.Toggle(
                Main.settings.ResourceChangerExpanded,
                Main.settings.ResourceChangerOn ? (Main.settings.ResourceChangerExpanded ? "◢" : "▶") : "",
                expandStyle
            );

            bool requestedOn = GUILayout.Toggle(Main.settings.ResourceChangerOn, name, enableStyle);
            if (requestedOn != Main.settings.ResourceChangerOn)
                QueueResourceChangerOnChange(requestedOn);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (Main.settings.ResourceChangerExpanded && Main.settings.ResourceChangerOn)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(24f);
                GUILayout.BeginVertical();
                DrawResourceChangerBody();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.Space(12f);
            }
        }

        private static bool IsLayoutEvent()
        {
            Event e = Event.current;
            return e != null && e.type == EventType.Layout;
        }

        private static void QueueResourceChangerOnChange(bool value)
        {
            if (IsLayoutEvent())
            {
                SetResourceChangerOn(value);
                return;
            }

            pendingResourceChangerOnSet = true;
            pendingResourceChangerOnValue = value;
            GUI.changed = true;
        }

        private static void SetResourceChangerOn(bool value)
        {
            if (Main.settings.ResourceChangerOn == value)
                return;

            Main.settings.ResourceChangerOn = value;
            QueueResourceChangerFullAction(value);
            GUI.changed = true;
        }

        private static void QueueResourceChangerFullAction(bool refresh)
        {
            pendingResourceChangerFullActionSet = true;
            pendingResourceChangerFullActionRefresh = refresh;

            pendingRefreshOttoIcon = false;
            pendingRestoreOttoIcon = false;
            pendingRefreshPlanetColors = false;
            pendingRestorePlanetColors = false;
            pendingRefreshTileColors = false;
            pendingRestoreTileColors = false;
        }

        private static void ApplyPendingResourceChangerGuiChanges()
        {
            if (!IsLayoutEvent())
                return;

            if (pendingResourceChangerOnSet)
            {
                bool value = pendingResourceChangerOnValue;
                pendingResourceChangerOnSet = false;
                SetResourceChangerOn(value);
            }

            if (pendingChangeOttoIconSet)
            {
                bool value = pendingChangeOttoIconValue;
                pendingChangeOttoIconSet = false;
                SetChangeOttoIcon(value);
            }

            if (pendingChangeBallColorSet)
            {
                bool value = pendingChangeBallColorValue;
                pendingChangeBallColorSet = false;
                SetChangeBallColor(value);
            }

            if (pendingChangeTileColorSet)
            {
                bool value = pendingChangeTileColorValue;
                pendingChangeTileColorSet = false;
                SetChangeTileColor(value);
            }
        }

        private static void QueueChangeOttoIcon(bool value)
        {
            if (IsLayoutEvent())
            {
                SetChangeOttoIcon(value);
                return;
            }

            pendingChangeOttoIconSet = true;
            pendingChangeOttoIconValue = value;
            GUI.changed = true;
        }

        private static void SetChangeOttoIcon(bool value)
        {
            if (Main.settings.ChangeOttoIcon == value)
                return;

            Main.settings.ChangeOttoIcon = value;
            if (value) pendingRefreshOttoIcon = true;
            else pendingRestoreOttoIcon = true;
            GUI.changed = true;
        }

        private static void QueueChangeBallColor(bool value)
        {
            if (IsLayoutEvent())
            {
                SetChangeBallColor(value);
                return;
            }

            pendingChangeBallColorSet = true;
            pendingChangeBallColorValue = value;
            GUI.changed = true;
        }

        private static void SetChangeBallColor(bool value)
        {
            if (Main.settings.ChangeBallColor == value)
                return;

            Main.settings.ChangeBallColor = value;
            if (value) pendingRefreshPlanetColors = true;
            else pendingRestorePlanetColors = true;
            GUI.changed = true;
        }

        private static void QueueChangeTileColor(bool value)
        {
            if (IsLayoutEvent())
            {
                SetChangeTileColor(value);
                return;
            }

            pendingChangeTileColorSet = true;
            pendingChangeTileColorValue = value;
            GUI.changed = true;
        }

        private static void SetChangeTileColor(bool value)
        {
            if (Main.settings.ChangeTileColor == value)
                return;

            Main.settings.ChangeTileColor = value;
            if (value) pendingRefreshTileColors = true;
            else pendingRestoreTileColors = true;
            GUI.changed = true;
        }

        private static void DrawResourceFeatureToggle(bool on, string name, Action<bool> onRequested)
        {
            EnsureFeatureStyles();
            bool requested = GUILayout.Toggle(on, name, enableStyle);
            if (requested != on && onRequested != null)
                onRequested(requested);
        }

        private static void QueueRefreshOttoIcon()
        {
            if (!pendingResourceChangerFullActionSet)
                pendingRefreshOttoIcon = true;
        }

        private static void QueueRefreshPlanetColors()
        {
            if (!pendingResourceChangerFullActionSet)
                pendingRefreshPlanetColors = true;
        }

        private static void QueueRefreshTileColors()
        {
            if (!pendingResourceChangerFullActionSet)
                pendingRefreshTileColors = true;
        }

        internal static void FlushPendingResourceChangerActions()
        {
            if (pendingResourceChangerFullActionSet)
            {
                bool refresh = pendingResourceChangerFullActionRefresh;
                pendingResourceChangerFullActionSet = false;
                if (refresh) ResourceChanger.RefreshChangedResources();
                else ResourceChanger.RestoreChangedResources();
            }
            else
            {
                if (pendingRestoreOttoIcon) ResourceChanger.RestoreOttoIcon();
                if (pendingRestorePlanetColors) ResourceChanger.RestorePlanetColors();
                if (pendingRestoreTileColors) ResourceChanger.RestoreTileColors();
                if (pendingRefreshOttoIcon) ResourceChanger.RefreshOttoIcon();
                if (pendingRefreshPlanetColors) ResourceChanger.RefreshPlanetColors();
                if (pendingRefreshTileColors) ResourceChanger.RefreshTileColors();
            }

            pendingRefreshOttoIcon = false;
            pendingRestoreOttoIcon = false;
            pendingRefreshPlanetColors = false;
            pendingRestorePlanetColors = false;
            pendingRefreshTileColors = false;
            pendingRestoreTileColors = false;
        }

        private static void DrawTweaksExpandable(string name)
        {
            bool wasOn = Main.settings.TweaksOn;
            DrawExpandable(ref Main.settings.TweaksOn, ref Main.settings.TweaksExpanded, name, DrawTweaksBody);
            if (wasOn != Main.settings.TweaksOn)
                Tweaks.RefreshTweaks();
        }

        private static void DrawEffectRemoverExpandable(string name)
        {
            bool wasOn = Main.settings.EffectRemoverOn;
            bool wasSaveEnabled = Main.settings.EffectRemoverEnableSave;
            DrawExpandable(ref Main.settings.EffectRemoverOn, ref Main.settings.EffectRemoverExpanded, name, DrawEffectRemoverBody);
            if (wasOn != Main.settings.EffectRemoverOn || wasSaveEnabled != Main.settings.EffectRemoverEnableSave)
                EffectRemover.RefreshEditorSaveButtons();
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

        private static void DrawAttemptBody()
        {
            DrawSubToggle(ref Main.settings.ShowAttempt, T("attempt.show"));
            DrawSubToggle(ref Main.settings.ShowFullAttempt, T("attempt.showFull"));

            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.xOffsetPx"), GUILayout.Width(140f));
            Main.settings.AttemptOffsetX = GUILayout.HorizontalSlider(Main.settings.AttemptOffsetX, -400f, 400f, GUILayout.Width(240f));
            string axStr = GUILayout.TextField(Main.settings.AttemptOffsetX.ToString("0"), GUILayout.Width(60f));
            float axP;
            if (float.TryParse(axStr, out axP)) Main.settings.AttemptOffsetX = Mathf.Clamp(axP, -400f, 400f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.yOffsetPx"), GUILayout.Width(140f));
            Main.settings.AttemptOffsetY = GUILayout.HorizontalSlider(Main.settings.AttemptOffsetY, -200f, 400f, GUILayout.Width(240f));
            string ayStr = GUILayout.TextField(Main.settings.AttemptOffsetY.ToString("0"), GUILayout.Width(60f));
            float ayP;
            if (float.TryParse(ayStr, out ayP)) Main.settings.AttemptOffsetY = Mathf.Clamp(ayP, -200f, 400f);
            GUILayout.EndHorizontal();
        }

        private static void DrawTimingScaleBody()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.yOffsetPx"), GUILayout.Width(140f));
            Main.settings.TimingScaleOffsetY = GUILayout.HorizontalSlider(Main.settings.TimingScaleOffsetY, -200f, 200f, GUILayout.Width(240f));
            string yStr = GUILayout.TextField(Main.settings.TimingScaleOffsetY.ToString("0"), GUILayout.Width(60f));
            float yP;
            if (float.TryParse(yStr, out yP)) Main.settings.TimingScaleOffsetY = Mathf.Clamp(yP, -200f, 200f);
            GUILayout.EndHorizontal();
        }

        private static void DrawKeyViewerBody()
        {
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.mode"), GUILayout.Width(80f));
            bool wasSimple = string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase);
            bool simpleSel = GUILayout.Toggle(wasSimple, T("keyviewer.simpleMode"), GUILayout.Width(220f));
            bool dmSel = GUILayout.Toggle(!wasSimple, T("keyviewer.dmMode"), GUILayout.Width(220f));
            if (simpleSel && !wasSimple)
            {
                Main.settings.KeyViewerMode = "simple";
                SyncSimpleKeysToKeyLimiter();
            }
            else if (dmSel && wasSimple) Main.settings.KeyViewerMode = "dmnote";
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);

            if (string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase))
            {
                DrawSimpleKeyViewerBody();
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(T("keyviewer.importPreset"), GUILayout.Width(350f)))
                {
                    KeyViewer.ImportKeyViewerPreset();
                }
                if (GUILayout.Button(T("common.clear"), GUILayout.Width(100f)))
                {
                    Main.settings.keyViewerPresetJson = "";
                    KeyViewer.keyViewerKeys = null;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                string status;
                if (string.IsNullOrEmpty(Main.settings.keyViewerPresetJson)) status = T("keyviewer.noPreset");
                else status = Tf("keyviewer.presetLoaded", Main.settings.keyViewerPresetJson.Length);
                GUILayout.Label(status);

                GUILayout.BeginHorizontal();
                string newTab = GUILayout.TextField(Main.settings.keyViewerSelectedTab ?? "4key", GUILayout.Width(140f));
                if (newTab != Main.settings.keyViewerSelectedTab)
                {
                    Main.settings.keyViewerSelectedTab = newTab;
                    KeyViewer.keyViewerKeys = null;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(T("keyviewer.outOfLimiter"), GUILayout.Width(160f));
                int currentMode = Mathf.Clamp(Main.settings.KeyViewerAdvancedOutOfLimiterMode, 0, 2);
                bool hideSel = GUILayout.Toggle(currentMode == 0, T("keyviewer.hide"), GUILayout.Width(100f));
                bool rainSel = GUILayout.Toggle(currentMode == 1, T("keyviewer.rainOnly"), GUILayout.Width(120f));
                bool fullSel = GUILayout.Toggle(currentMode == 2, T("keyviewer.fullPress"), GUILayout.Width(120f));
                if (hideSel && currentMode != 0) Main.settings.KeyViewerAdvancedOutOfLimiterMode = 0;
                else if (rainSel && currentMode != 1) Main.settings.KeyViewerAdvancedOutOfLimiterMode = 1;
                else if (fullSel && currentMode != 2) Main.settings.KeyViewerAdvancedOutOfLimiterMode = 2;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.xOffset"), GUILayout.Width(100f));
            Main.settings.KeyViewerOffsetX = GUILayout.HorizontalSlider(Main.settings.KeyViewerOffsetX, -2000f, 2000f, GUILayout.Width(240f));
            string xs = GUILayout.TextField(Main.settings.KeyViewerOffsetX.ToString("0"), GUILayout.Width(60f));
            float xp;
            if (float.TryParse(xs, out xp)) Main.settings.KeyViewerOffsetX = Mathf.Clamp(xp, -10000f, 10000f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.yOffset"), GUILayout.Width(100f));
            Main.settings.KeyViewerOffsetY = GUILayout.HorizontalSlider(Main.settings.KeyViewerOffsetY, -2000f, 2000f, GUILayout.Width(240f));
            string ys = GUILayout.TextField(Main.settings.KeyViewerOffsetY.ToString("0"), GUILayout.Width(60f));
            float yp;
            if (float.TryParse(ys, out yp)) Main.settings.KeyViewerOffsetY = Mathf.Clamp(yp, -10000f, 10000f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(T("label.scale"), GUILayout.Width(80f));
            Main.settings.KeyViewerScale = GUILayout.HorizontalSlider(Main.settings.KeyViewerScale, 0.2f, 4f, GUILayout.Width(240f));
            string ss = GUILayout.TextField(Main.settings.KeyViewerScale.ToString("0.##"), GUILayout.Width(60f));
            float sp;
            if (float.TryParse(ss, out sp)) Main.settings.KeyViewerScale = Mathf.Clamp(sp, 0.2f, 4f);
            GUILayout.EndHorizontal();

            DrawSubToggle(ref Main.settings.KeyViewerNoteEffect, T("keyviewer.noteRain"));
            DrawSubToggle(ref Main.settings.KeyViewerNoteReverse, T("keyviewer.reverseRain"));
            DrawSubToggle(ref Main.settings.KeyViewerShowCounter, T("keyviewer.showCounter"));

            bool useSimpleRainSettings = string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase);

            GUILayout.BeginHorizontal();
            GUILayout.Label(T("keyviewer.noteSpeed"), GUILayout.Width(150f));
            float noteSpeed = useSimpleRainSettings ? Main.settings.KeyViewerSimpleRainSpeed : Main.settings.KeyViewerNoteSpeed;
            noteSpeed = GUILayout.HorizontalSlider(noteSpeed, 10f, 1000f, GUILayout.Width(240f));
            string nss = GUILayout.TextField(noteSpeed.ToString("0"), GUILayout.Width(60f));
            float nsp;
            if (float.TryParse(nss, out nsp)) noteSpeed = Mathf.Clamp(nsp, 1f, 5000f);
            if (useSimpleRainSettings) Main.settings.KeyViewerSimpleRainSpeed = noteSpeed;
            else Main.settings.KeyViewerNoteSpeed = noteSpeed;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(T("keyviewer.trackHeight"), GUILayout.Width(150f));
            float trackHeight = useSimpleRainSettings ? Main.settings.KeyViewerSimpleRainHeight : Main.settings.KeyViewerTrackHeight;
            float oldTrackHeight = trackHeight;
            trackHeight = GUILayout.HorizontalSlider(trackHeight, 0f, 1000f, GUILayout.Width(240f));
            string ths = GUILayout.TextField(trackHeight.ToString("0"), GUILayout.Width(60f));
            float thp;
            if (float.TryParse(ths, out thp)) trackHeight = Mathf.Clamp(thp, 0f, 5000f);
            if (useSimpleRainSettings) Main.settings.KeyViewerSimpleRainHeight = trackHeight;
            else Main.settings.KeyViewerTrackHeight = trackHeight;
            if (Mathf.Abs(oldTrackHeight - trackHeight) > 0.001f)
                KeyViewer.keyViewerKeys = null;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(T("keyviewer.fade"), GUILayout.Width(150f));
            Main.settings.KeyViewerFadePx = GUILayout.HorizontalSlider(Main.settings.KeyViewerFadePx, 0f, 500f, GUILayout.Width(240f));
            string fps = GUILayout.TextField(Main.settings.KeyViewerFadePx.ToString("0"), GUILayout.Width(60f));
            float fpp;
            if (float.TryParse(fps, out fpp)) Main.settings.KeyViewerFadePx = Mathf.Clamp(fpp, 0f, 2000f);
            GUILayout.EndHorizontal();

            DrawKeyViewerCountersBody();
        }

        private static readonly Dictionary<string, string> kvCountFieldBuffers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static string kvTotalCountBuffer;
        private static bool kvCountersExpanded;
        private static bool kvCountersConfirmReset;
        private static Vector2 kvCountersScroll;

        private static void DrawKeyViewerCountersBody()
        {
            EnsureFeatureStyles();
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            kvCountersExpanded = GUILayout.Toggle(kvCountersExpanded, kvCountersExpanded ? "◢" : "▶", expandStyle);
            if (GUILayout.Button(T("keyviewer.counters"), GUI.skin.label))
                kvCountersExpanded = !kvCountersExpanded;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (!kvCountersExpanded) return;

            int totalNow = KeyViewer.GetKeyViewerTotal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            GUILayout.Label(T("keyviewer.total"), GUILayout.Width(120f));
            string totalShown = (kvTotalCountBuffer != null) ? kvTotalCountBuffer : totalNow.ToString();
            string totalEdited = GUILayout.TextField(totalShown, GUILayout.Width(120f));
            if (totalEdited != totalShown) kvTotalCountBuffer = totalEdited;
            int totalParsed;
            if (int.TryParse(kvTotalCountBuffer ?? totalEdited, out totalParsed) && totalParsed != totalNow)
                KeyViewer.SetKeyViewerTotal(totalParsed);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            List<KeyValuePair<string, int>> entries = KeyViewer.EnumerateKeyViewerCounters();
            if (entries.Count == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(18f);
                GUILayout.Label(T("keyviewer.noKeys"));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                kvCountersScroll = GUILayout.BeginScrollView(kvCountersScroll, GUILayout.MaxHeight(220f));
                foreach (KeyValuePair<string, int> entry in entries)
                {
                    string name = entry.Key;
                    int current = entry.Value;
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(18f);
                    GUILayout.Label(name, GUILayout.Width(160f));
                    string buf;
                    if (!kvCountFieldBuffers.TryGetValue(name, out buf)) buf = current.ToString();
                    string edited = GUILayout.TextField(buf, GUILayout.Width(120f));
                    if (edited != buf) { kvCountFieldBuffers[name] = edited; buf = edited; }
                    int parsed;
                    if (int.TryParse(buf, out parsed) && parsed != current)
                        KeyViewer.SetKeyViewerCount(name, parsed);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            if (GUILayout.Button(T("keyviewer.resetAllCounts"), GUILayout.Width(200f)))
                kvCountersConfirmReset = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (kvCountersConfirmReset)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                GUILayout.Label("<color=red>" + T("common.reallyResetCounts") + "</color>");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                if (GUILayout.Button(T("common.confirm"), GUILayout.Width(100f)))
                {
                    KeyViewer.ResetAllKeyViewerCounters();
                    kvCountFieldBuffers.Clear();
                    kvTotalCountBuffer = null;
                    kvCountersConfirmReset = false;
                }
                if (GUILayout.Button(T("common.cancel"), GUILayout.Width(100f)))
                    kvCountersConfirmReset = false;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private const int SimpleFootSlotBase = 1000;
        private const int SimpleGhostSlotBase = 2000;
        private static bool simpleKeyShare;
        private static bool simpleKeyChangeExpanded;
        private static bool simpleGhostRainChangeExpanded;
        private static bool simpleTextChangeExpanded;
        private static bool simpleColorExpanded;
        private static bool simpleConfirmReset;
        private static int simpleSelectedSlot = -1;
        private static bool simpleSelectedTextEdit;
        private static int simplePrevStyle = -1;
        private static readonly int[] SimpleKey12BottomOrder = { 9, 8, 10, 11 };
        private static readonly int[] SimpleBackRowOrder = { 12, 13, 9, 8, 10, 11, 14, 15 };
        private static readonly int[] SimpleKey20ExtraOrder = { 17, 16, 18, 19 };

        private static int SimpleSlotCount(int style)
        {
            switch (style) { case 0: return 10; case 1: return 12; case 2: return 16; case 3: return 20; default: return 12; }
        }
        private static float SimpleMaxYForStyle(int style)
        {
            switch (style)
            {
                case 0:
                case 1:
                    return 976f;
                case 3:
                    return 922f;
                default:
                    return 935f;
            }
        }
        private static int[] SimpleCodes(int style)
        {
            switch (style)
            {
                case 0: return Main.settings.KeyViewerSimpleKey10;
                case 1: return Main.settings.KeyViewerSimpleKey12;
                case 2: return Main.settings.KeyViewerSimpleKey16;
                case 3: return Main.settings.KeyViewerSimpleKey20;
                default: return Main.settings.KeyViewerSimpleKey12;
            }
        }
        private static string[] SimpleTexts(int style)
        {
            switch (style)
            {
                case 0: return Main.settings.KeyViewerSimpleKey10Text;
                case 1: return Main.settings.KeyViewerSimpleKey12Text;
                case 2: return Main.settings.KeyViewerSimpleKey16Text;
                case 3: return Main.settings.KeyViewerSimpleKey20Text;
                default: return Main.settings.KeyViewerSimpleKey12Text;
            }
        }

        private static int[] SimpleGhostCodes(int style)
        {
            switch (style)
            {
                case 0: return Main.settings.KeyViewerSimpleGhostKey10;
                case 1: return Main.settings.KeyViewerSimpleGhostKey12;
                case 2: return Main.settings.KeyViewerSimpleGhostKey16;
                case 3: return Main.settings.KeyViewerSimpleGhostKey20;
                default: return Main.settings.KeyViewerSimpleGhostKey12;
            }
        }

        private static int[] SimpleFootCodes(int footStyle)
        {
            switch (footStyle)
            {
                case 1: return Main.settings.KeyViewerSimpleFootKey2;
                case 2: return Main.settings.KeyViewerSimpleFootKey4;
                case 3: return Main.settings.KeyViewerSimpleFootKey6;
                case 4: return Main.settings.KeyViewerSimpleFootKey8;
                case 5: return Main.settings.KeyViewerSimpleFootKey16;
                default: return null;
            }
        }

        private static string SimpleKeyShortLabel(KeyCode kc)
        {
            if (kc == KeyCode.AltGr) return "RAlt";
            string s = kc.ToString();
            if (s.StartsWith("Alpha")) s = s.Substring(5);
            if (s.StartsWith("Keypad")) s = s.Substring(6);
            if (s.StartsWith("Left")) s = "L" + s.Substring(4);
            if (s.StartsWith("Right")) s = "R" + s.Substring(5);
            if (s.EndsWith("Shift")) s = s.Substring(0, s.Length - 5) + "⇧";
            if (s.EndsWith("Control")) s = s.Substring(0, s.Length - 7) + "Ctrl";
            if (s.StartsWith("Mouse")) s = "M" + s.Substring(5);
            switch (s)
            {
                case "PageUp": return "PgUp";
                case "Plus": return "+";
                case "Minus": return "-";
                case "Multiply": return "*";
                case "Divide": return "/";
                case "Enter": return "↵";
                case "Return": return "↵";
                case "Equals": return "=";
                case "Period": return ".";
                case "Comma": return ",";
                case "Tab": return "⇥";
                case "Space": return "␣";
                case "Backslash": return "\\";
                case "Slash": return "/";
                case "Semicolon": return ";";
                case "Quote": return "'";
                case "BackQuote": return "`";
                case "CapsLock": return "⇪";
                case "Backspace": return "Back";
                case "UpArrow": return "↑";
                case "DownArrow": return "↓";
                case "LeftArrow": return "←";
                case "RightArrow": return "→";
                case "LBracket": return "[";
                case "RBracket": return "]";
                case "LeftBracket": return "[";
                case "RightBracket": return "]";
                case "None": return " ";
                default: return s;
            }
        }

        private static void SimpleResetCounts()
        {

            KeyViewer.ResetAllKeyViewerCounters();
        }

        internal static void SyncSimpleKeysToKeyLimiter()
        {
            if (Main.settings == null) return;
            if (!Main.settings.KeyViewerSimpleSyncToKeyLimiter) return;
            if (!string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase)) return;

            int style = Mathf.Clamp(Main.settings.KeyViewerSimpleStyle, 0, 3);
            int footStyle = Mathf.Clamp(Main.settings.KeyViewerSimpleFootStyle, 0, 5);
            int[] handCodes = SimpleCodes(style);
            int[] footCodes = SimpleFootCodes(footStyle);

            HashSet<int> seen = new HashSet<int>();
            List<int> result = new List<int>();
            if (handCodes != null)
            {
                for (int i = 0; i < handCodes.Length; i++)
                {
                    int c = handCodes[i];
                    if (c == 0) continue;
                    if (seen.Add(c)) result.Add(c);
                }
            }
            if (footCodes != null)
            {
                for (int i = 0; i < footCodes.Length; i++)
                {
                    int c = footCodes[i];
                    if (c == 0) continue;
                    if (seen.Add(c)) result.Add(c);
                }
            }

            int[] current = Main.settings.KeyLimiterAllowed;
            if (current != null && current.Length == result.Count)
            {
                bool same = true;
                for (int i = 0; i < current.Length; i++)
                {
                    if (current[i] != result[i]) { same = false; break; }
                }
                if (same) return;
            }

            Main.settings.KeyLimiterAllowed = result.ToArray();
        }

        private static int simplePendingCaptureKey = (int)KeyCode.None;

        private static KeyCode NormalizeSimpleCapturedKey(KeyCode keyCode)
        {
            return keyCode == KeyCode.AltGr ? KeyCode.RightAlt : keyCode;
        }

        private static void DrawSimpleKeyViewerBody()
        {
            EnsureFeatureStyles();
            int style = Mathf.Clamp(Main.settings.KeyViewerSimpleStyle, 0, 3);

            if (Event.current != null && Event.current.type == EventType.Layout
                && simplePendingCaptureKey != (int)KeyCode.None && simpleSelectedSlot >= 0)
            {
                KeyCode pending = (KeyCode)simplePendingCaptureKey;
                simplePendingCaptureKey = (int)KeyCode.None;
                int footStyleNow = Mathf.Clamp(Main.settings.KeyViewerSimpleFootStyle, 0, 5);
                ApplySimpleCapturedKey(pending, SimpleCodes(style), SimpleFootCodes(footStyleNow), SimpleGhostCodes(style));
            }

            DrawSubToggle(ref simpleKeyShare, T("keyviewer.keyShare"));

            bool prevSyncToKeyLimiter = Main.settings.KeyViewerSimpleSyncToKeyLimiter;
            DrawSubToggle(ref Main.settings.KeyViewerSimpleSyncToKeyLimiter, T("keyviewer.syncToKeyLimiter"));
            if (Main.settings.KeyViewerSimpleSyncToKeyLimiter && !prevSyncToKeyLimiter)
                SyncSimpleKeysToKeyLimiter();

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            if (GUILayout.Button(T("keyviewer.resetCount"), GUILayout.Width(180f)))
                simpleConfirmReset = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (simpleConfirmReset)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                GUILayout.Label("<color=red>" + T("keyviewer.reallyResetAll") + "</color>");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                if (GUILayout.Button(T("common.confirm"), GUILayout.Width(100f)))
                {
                    SimpleResetCounts();
                    simpleConfirmReset = false;
                }
                if (GUILayout.Button(T("common.cancel"), GUILayout.Width(100f)))
                    simpleConfirmReset = false;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            DrawSubToggle(ref Main.settings.KeyViewerSimpleUseRain, T("keyviewer.enableRain"));
            if (Main.settings.KeyViewerSimpleUseRain)
            {
                DrawSubToggle(ref Main.settings.KeyViewerSimpleUseGhostRain, T("keyviewer.enableGhostRain"));
                DrawSimpleRainControls();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(T("keyviewer.style"), GUILayout.Width(80f));
            string[] styleNames = { "Key10", "Key12", "Key16", "Key20" };
            for (int i = 0; i < styleNames.Length; i++)
            {
                bool was = style == i;
                bool now = GUILayout.Toggle(was, styleNames[i], GUILayout.Width(70f));
                if (now && !was)
                {
                    if (simpleKeyShare && simplePrevStyle >= 0 && simplePrevStyle != i)
                    {
                        int[] src = SimpleCodes(simplePrevStyle);
                        string[] srcText = SimpleTexts(simplePrevStyle);
                        Main.settings.KeyViewerSimpleStyle = i;
                        int[] dst = SimpleCodes(i);
                        string[] dstText = SimpleTexts(i);
                        int n = Math.Min(src.Length, dst.Length);
                        for (int j = 0; j < n; j++) { dst[j] = src[j]; dstText[j] = srcText[j]; }
                    }
                    else
                    {
                        Main.settings.KeyViewerSimpleStyle = i;
                    }
                    simpleSelectedSlot = -1;
                    KeyViewer.keyViewerKeys = null;
                    SyncSimpleKeysToKeyLimiter();
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            simplePrevStyle = Main.settings.KeyViewerSimpleStyle;
            style = Mathf.Clamp(Main.settings.KeyViewerSimpleStyle, 0, 3);

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(T("keyviewer.footStyle"), GUILayout.Width(80f));
            string[] footNames = { T("keyviewer.footNone"), "Key2", "Key4", "Key6", "Key8", "Key16" };
            int footStyle = Mathf.Clamp(Main.settings.KeyViewerSimpleFootStyle, 0, 5);
            for (int i = 0; i < footNames.Length; i++)
            {
                bool was = footStyle == i;
                bool now = GUILayout.Toggle(was, footNames[i], GUILayout.Width(i == 0 ? 62f : 58f));
                if (now && !was)
                {
                    Main.settings.KeyViewerSimpleFootStyle = i;
                    simpleSelectedSlot = -1;
                    KeyViewer.keyViewerKeys = null;
                    SyncSimpleKeysToKeyLimiter();
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (Main.settings.KeyViewerSimpleFootStyle > 0)
                DrawSimpleFootOffsetRows();

            int slotCount = SimpleSlotCount(style);
            int[] codes = SimpleCodes(style);
            string[] texts = SimpleTexts(style);
            int[] footCodes = SimpleFootCodes(Mathf.Clamp(Main.settings.KeyViewerSimpleFootStyle, 0, 5));
            int[] ghostCodes = SimpleGhostCodes(style);

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            simpleKeyChangeExpanded = GUILayout.Toggle(simpleKeyChangeExpanded, simpleKeyChangeExpanded ? "◢" : "▶", expandStyle);
            if (GUILayout.Button(T("keyviewer.keyChange"), GUI.skin.label)) simpleKeyChangeExpanded = !simpleKeyChangeExpanded;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (simpleKeyChangeExpanded)
            {
                DrawSimpleSlotButtons(style, slotCount, codes, texts, false, 0, false);
                if (footCodes != null && footCodes.Length > 0)
                    DrawSimpleSlotButtons(style, footCodes.Length, footCodes, null, false, SimpleFootSlotBase, false);
                if (simpleSelectedSlot >= 0 && !simpleSelectedTextEdit)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(28f);
                    GUILayout.Label("<b>" + T("keyviewer.pressKey") + "</b>");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    Event ev = Event.current;
                    if (ev != null && ev.isKey && ev.type == EventType.KeyDown && ev.keyCode != KeyCode.None)
                    {
                        simplePendingCaptureKey = (int)ev.keyCode;
                        ev.Use();
                    }
                    else if (Input.anyKeyDown && simplePendingCaptureKey == (int)KeyCode.None)
                    {
                        foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
                        {
                            if (Input.GetKeyDown(kc) && kc != KeyCode.None)
                            {
                                simplePendingCaptureKey = (int)kc;
                                break;
                            }
                        }
                    }
                }
            }

            if (Main.settings.KeyViewerSimpleUseRain && Main.settings.KeyViewerSimpleUseGhostRain)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(14f);
                simpleGhostRainChangeExpanded = GUILayout.Toggle(simpleGhostRainChangeExpanded, simpleGhostRainChangeExpanded ? "◢" : "▶", expandStyle);
                if (GUILayout.Button(T("keyviewer.ghostKeyChange"), GUI.skin.label)) simpleGhostRainChangeExpanded = !simpleGhostRainChangeExpanded;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if (simpleGhostRainChangeExpanded)
                    DrawSimpleSlotButtons(style, slotCount, ghostCodes, null, false, SimpleGhostSlotBase, true);
            }

            if (simpleSelectedSlot >= SimpleGhostSlotBase && !simpleSelectedTextEdit)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                GUILayout.Label("<b>" + T("keyviewer.pressGhostKey") + "</b>");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                HandleSimpleKeyCapture(codes, footCodes, ghostCodes);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            simpleTextChangeExpanded = GUILayout.Toggle(simpleTextChangeExpanded, simpleTextChangeExpanded ? "◢" : "▶", expandStyle);
            if (GUILayout.Button(T("keyviewer.textChange"), GUI.skin.label)) simpleTextChangeExpanded = !simpleTextChangeExpanded;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (simpleTextChangeExpanded)
            {
                DrawSimpleSlotButtons(style, slotCount, codes, texts, true, 0, false);
                if (simpleSelectedSlot >= 0 && simpleSelectedTextEdit && simpleSelectedSlot < texts.Length)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(28f);
                    GUILayout.Label(T("keyviewer.displayText"), GUILayout.Width(110f));
                    string current = texts[simpleSelectedSlot] ?? SimpleKeyShortLabel((KeyCode)codes[simpleSelectedSlot]);
                    string edited = GUILayout.TextField(current, GUILayout.Width(160f));
                    if (edited != current)
                    {
                        texts[simpleSelectedSlot] = edited == SimpleKeyShortLabel((KeyCode)codes[simpleSelectedSlot]) ? null : edited;
                        KeyViewer.keyViewerKeys = null;
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(28f);
                    if (GUILayout.Button(T("common.reset"), GUILayout.Width(100f)))
                    {
                        texts[simpleSelectedSlot] = null;
                        simpleSelectedSlot = -1;
                        KeyViewer.keyViewerKeys = null;
                    }
                    if (GUILayout.Button(T("common.save"), GUILayout.Width(100f)))
                        simpleSelectedSlot = -1;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            simpleColorExpanded = GUILayout.Toggle(simpleColorExpanded, simpleColorExpanded ? "◢" : "▶", expandStyle);
            if (GUILayout.Button(T("keyviewer.color"), GUI.skin.label)) simpleColorExpanded = !simpleColorExpanded;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (simpleColorExpanded)
            {
                DrawSimpleColorRow(ref Main.settings.SKvBgR, ref Main.settings.SKvBgG, ref Main.settings.SKvBgB, ref Main.settings.SKvBgA, T("keyviewer.bg"), "skvBg");
                DrawSimpleColorRow(ref Main.settings.SKvBgcR, ref Main.settings.SKvBgcG, ref Main.settings.SKvBgcB, ref Main.settings.SKvBgcA, T("keyviewer.bgClicked"), "skvBgc");
                DrawSimpleColorRow(ref Main.settings.SKvOutR, ref Main.settings.SKvOutG, ref Main.settings.SKvOutB, ref Main.settings.SKvOutA, T("keyviewer.outline"), "skvOut");
                DrawSimpleColorRow(ref Main.settings.SKvOutcR, ref Main.settings.SKvOutcG, ref Main.settings.SKvOutcB, ref Main.settings.SKvOutcA, T("keyviewer.outlineClicked"), "skvOutc");
                DrawSimpleColorRow(ref Main.settings.SKvTxtR, ref Main.settings.SKvTxtG, ref Main.settings.SKvTxtB, ref Main.settings.SKvTxtA, T("keyviewer.text"), "skvTxt");
                DrawSimpleColorRow(ref Main.settings.SKvTxtcR, ref Main.settings.SKvTxtcG, ref Main.settings.SKvTxtcB, ref Main.settings.SKvTxtcA, T("keyviewer.textClicked"), "skvTxtc");
                DrawSimpleColorRow(ref Main.settings.SKvRainR, ref Main.settings.SKvRainG, ref Main.settings.SKvRainB, ref Main.settings.SKvRainA, T("keyviewer.rainColor"), "skvRain");
                DrawSimpleColorRow(ref Main.settings.SKvRain2R, ref Main.settings.SKvRain2G, ref Main.settings.SKvRain2B, ref Main.settings.SKvRain2A, T("keyviewer.rainColor2"), "skvRain2");
                if (style == 3)
                    DrawSimpleColorRow(ref Main.settings.SKvRain3R, ref Main.settings.SKvRain3G, ref Main.settings.SKvRain3B, ref Main.settings.SKvRain3A, T("keyviewer.rainColor3"), "skvRain3");
                DrawSimpleColorRow(ref Main.settings.SKvGhostRainR, ref Main.settings.SKvGhostRainG, ref Main.settings.SKvGhostRainB, ref Main.settings.SKvGhostRainA, T("keyviewer.ghostRainKey"), "skvGhostRain");
            }
        }

        private static void HandleSimpleKeyCapture(int[] handCodes, int[] footCodes, int[] ghostCodes)
        {
            
            Event ev = Event.current;
            if (ev != null && ev.isKey && ev.type == EventType.KeyDown && ev.keyCode != KeyCode.None)
            {
                simplePendingCaptureKey = (int)ev.keyCode;
                ev.Use();
                return;
            }

            if (!Input.anyKeyDown || simplePendingCaptureKey != (int)KeyCode.None) return;
            foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
            {
                if (!Input.GetKeyDown(kc) || kc == KeyCode.None) continue;
                simplePendingCaptureKey = (int)kc;
                break;
            }
        }

        private static void ApplySimpleCapturedKey(KeyCode keyCode, int[] handCodes, int[] footCodes, int[] ghostCodes)
        {
            keyCode = NormalizeSimpleCapturedKey(keyCode);

            if (simpleSelectedSlot >= SimpleGhostSlotBase)
            {
                int slot = simpleSelectedSlot - SimpleGhostSlotBase;
                if (ghostCodes != null && slot >= 0 && slot < ghostCodes.Length)
                    ghostCodes[slot] = (int)keyCode;
            }
            else if (simpleSelectedSlot >= SimpleFootSlotBase)
            {
                int slot = simpleSelectedSlot - SimpleFootSlotBase;
                if (footCodes != null && slot >= 0 && slot < footCodes.Length)
                    footCodes[slot] = (int)keyCode;
            }
            else if (handCodes != null && simpleSelectedSlot >= 0 && simpleSelectedSlot < handCodes.Length)
            {
                handCodes[simpleSelectedSlot] = (int)keyCode;
            }

            simpleSelectedSlot = -1;
            KeyViewer.keyViewerKeys = null;
            SyncSimpleKeysToKeyLimiter();
        }

        private static int SimpleVisualRowCount(int style, int slotCount, int slotBase)
        {
            if (slotCount <= 0) return 0;
            if (slotBase == SimpleFootSlotBase)
                return slotCount > 10 ? 2 : 1;

            switch (style)
            {
                case 3: return 3;
                case 0:
                case 1:
                case 2:
                    return 2;
                default:
                    return (slotCount + 7) / 8;
            }
        }

        private static int SimpleVisualSlot(int style, int slotCount, int slotBase, int row, int col)
        {
            if (slotBase == SimpleFootSlotBase)
                return SimpleVisualFootSlot(slotCount, row, col);

            if (row == 0)
                return col < Math.Min(8, slotCount) ? col : -1;

            if (style == 1 && row == 1)
                return col < SimpleKey12BottomOrder.Length ? SimpleKey12BottomOrder[col] : -1;

            if ((style == 2 || style == 3) && row == 1)
                return col < SimpleBackRowOrder.Length ? SimpleBackRowOrder[col] : -1;

            if (style == 3 && row == 2)
                return col < SimpleKey20ExtraOrder.Length ? SimpleKey20ExtraOrder[col] : -1;

            int slot = row * 8 + col;
            return slot < slotCount ? slot : -1;
        }

        private static int SimpleVisualFootSlot(int slotCount, int row, int col)
        {
            bool twoLine = slotCount > 10;
            int rowSize = twoLine ? slotCount / 2 : slotCount;
            if (row >= (twoLine ? 2 : 1) || col >= rowSize) return -1;

            int evenCount = (rowSize + 1) / 2;
            int slotInRow = col < evenCount ? col * 2 : (col - evenCount) * 2 + 1;
            int slot = row * rowSize + slotInRow;
            return slot < slotCount ? slot : -1;
        }

        private static void DrawSimpleRainControls()
        {
            DrawSimpleFloatRow(ref Main.settings.KeyViewerSimpleRainWidth, T("keyviewer.rainWidth"), 0f, 2000f, 0f, 10000f);
            DrawSimpleFloatRow(ref Main.settings.KeyViewerSimpleRain2Width, T("keyviewer.rainWidth2"), 0f, 2000f, 0f, 10000f);
            DrawSimpleFloatRow(ref Main.settings.KeyViewerSimpleRainOffsetY, T("keyviewer.rainYOffset"), -2000f, 2000f, -10000f, 10000f);
            DrawSimpleFloatRow(ref Main.settings.KeyViewerSimpleRain2OffsetY, T("keyviewer.rain2YOffset"), -2000f, 2000f, -10000f, 10000f);
        }

        private static void DrawSimpleFloatRow(ref float value, string label, float sliderMin, float sliderMax, float clampMin, float clampMax)
        {
            float old = value;
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(label, GUILayout.Width(120f));
            value = GUILayout.HorizontalSlider(value, sliderMin, sliderMax, GUILayout.Width(240f));
            string text = GUILayout.TextField(value.ToString("0"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(text, out parsed)) value = Mathf.Clamp(parsed, clampMin, clampMax);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (old != value)
                KeyViewer.keyViewerKeys = null;
        }

        private static void DrawSimpleFootOffsetRows()
        {
            float oldX = Main.settings.KeyViewerSimpleFootOffsetX;
            float oldY = Main.settings.KeyViewerSimpleFootOffsetY;

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(T("keyviewer.footXOffset"), GUILayout.Width(120f));
            Main.settings.KeyViewerSimpleFootOffsetX = GUILayout.HorizontalSlider(Main.settings.KeyViewerSimpleFootOffsetX, -2000f, 2000f, GUILayout.Width(240f));
            string xs = GUILayout.TextField(Main.settings.KeyViewerSimpleFootOffsetX.ToString("0"), GUILayout.Width(60f));
            float xp;
            if (float.TryParse(xs, out xp)) Main.settings.KeyViewerSimpleFootOffsetX = Mathf.Clamp(xp, -10000f, 10000f);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(T("keyviewer.footYOffset"), GUILayout.Width(120f));
            Main.settings.KeyViewerSimpleFootOffsetY = GUILayout.HorizontalSlider(Main.settings.KeyViewerSimpleFootOffsetY, -2000f, 2000f, GUILayout.Width(240f));
            string ys = GUILayout.TextField(Main.settings.KeyViewerSimpleFootOffsetY.ToString("0"), GUILayout.Width(60f));
            float yp;
            if (float.TryParse(ys, out yp)) Main.settings.KeyViewerSimpleFootOffsetY = Mathf.Clamp(yp, -10000f, 10000f);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (oldX != Main.settings.KeyViewerSimpleFootOffsetX || oldY != Main.settings.KeyViewerSimpleFootOffsetY)
                KeyViewer.keyViewerKeys = null;
        }

        private static void DrawSimpleSlotButtons(int style, int slotCount, int[] codes, string[] texts,
                                                  bool textMode, int slotBase, bool clearNonNoneOnClick)
        {
            EnsureFeatureStyles();
            int perRow = 8;
            int rows = SimpleVisualRowCount(style, slotCount, slotBase);
            for (int row = 0; row < rows; row++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                for (int col = 0; col < perRow; col++)
                {
                    int slot = SimpleVisualSlot(style, slotCount, slotBase, row, col);
                    if (slot < 0 || slot >= slotCount) break;
                    if (codes == null || slot >= codes.Length) break;
                    string label = textMode
                        ? (texts != null && slot < texts.Length && !string.IsNullOrEmpty(texts[slot]) ? texts[slot] : SimpleKeyShortLabel((KeyCode)codes[slot]))
                        : SimpleKeyShortLabel((KeyCode)codes[slot]);
                    int slotId = slotBase + slot;
                    if (slotId == simpleSelectedSlot && textMode == simpleSelectedTextEdit) label = "<b>" + label + "</b>";
                    if (GUILayout.Button(label, simpleSlotButtonStyle))
                    {
                        if (clearNonNoneOnClick && codes[slot] != (int)KeyCode.None)
                        {
                            codes[slot] = (int)KeyCode.None;
                            simpleSelectedSlot = -1;
                            KeyViewer.keyViewerKeys = null;
                            continue;
                        }
                        simpleSelectedSlot = slotId;
                        simpleSelectedTextEdit = textMode;
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawSimpleColorRow(ref float r, ref float g, ref float b, ref float a, string name, string key)
        {
            float oldR = r, oldG = g, oldB = b, oldA = a;
            DrawSubColor(ref r, ref g, ref b, ref a, name, key);
            if (oldR != r || oldG != g || oldB != b || oldA != a) KeyViewer.keyViewerKeys = null;
        }

        private static void DrawResourceChangerBody()
        {
            ApplyPendingResourceChangerGuiChanges();

            DrawResourceFeatureToggle(Main.settings.ChangeOttoIcon, T("resource.ottoIcon"), QueueChangeOttoIcon);
            DrawResourceFeatureToggle(Main.settings.ChangeBallColor, T("resource.ballColor"), QueueChangeBallColor);
            DrawResourceFeatureToggle(Main.settings.ChangeTileColor, T("resource.tileColor"), QueueChangeTileColor);

            if (Main.settings.ChangeOttoIcon)
            {
                DrawResourceColor(ref Main.settings.OttoR, ref Main.settings.OttoG, ref Main.settings.OttoB, ref Main.settings.OttoA, T("resource.ottoColor"), "otto", QueueRefreshOttoIcon);
                if (DrawOttoOffsetRow(ref Main.settings.OttoOffsetX, T("resource.ottoXOffset")))
                    QueueRefreshOttoIcon();
                if (DrawOttoOffsetRow(ref Main.settings.OttoOffsetY, T("resource.ottoYOffset")))
                    QueueRefreshOttoIcon();
            }

            if (Main.settings.ChangeBallColor)
            {
                ResourceChanger.NormalizeBallOpacitySettings();
                DrawBallPlanetResource(
                    ref Main.settings.BallPlanet1R,
                    ref Main.settings.BallPlanet1G,
                    ref Main.settings.BallPlanet1B,
                    ref Main.settings.BallPlanet1Opacity,
                    ref Main.settings.TailPlanet1Opacity,
                    T("resource.planet1Color"),
                    T("resource.planet1BallOpacity"),
                    T("resource.planet1TailOpacity"),
                    "resourceBall1"
                );
                DrawBallPlanetResource(
                    ref Main.settings.BallPlanet2R,
                    ref Main.settings.BallPlanet2G,
                    ref Main.settings.BallPlanet2B,
                    ref Main.settings.BallPlanet2Opacity,
                    ref Main.settings.TailPlanet2Opacity,
                    T("resource.planet2Color"),
                    T("resource.planet2BallOpacity"),
                    T("resource.planet2TailOpacity"),
                    "resourceBall2"
                );
                DrawBallPlanetResource(
                    ref Main.settings.BallPlanet3R,
                    ref Main.settings.BallPlanet3G,
                    ref Main.settings.BallPlanet3B,
                    ref Main.settings.BallPlanet3Opacity,
                    ref Main.settings.TailPlanet3Opacity,
                    T("resource.planet3Color"),
                    T("resource.planet3BallOpacity"),
                    T("resource.planet3TailOpacity"),
                    "resourceBall3"
                );
            }

            if (Main.settings.ChangeTileColor)
                DrawResourceColor(ref Main.settings.TileR, ref Main.settings.TileG, ref Main.settings.TileB, ref Main.settings.TileA, T("resource.tileColor"), "resourceTile", QueueRefreshTileColors);
        }

        private static void DrawTweaksBody()
        {
            bool prevRemoveCheckpoints = Main.settings.RemoveAllCheckpoints;
            bool prevRemoveBallCoreParticles = Main.settings.RemoveBallCoreParticles;
            bool prevDisableTileHitGlow = Main.settings.DisableTileHitGlow;
            bool prevRemovePlanetGlow = Main.settings.RemovePlanetGlow;
            DrawSubToggle(ref Main.settings.RemoveAllCheckpoints, T("tweaks.removeCheckpoints"));
            DrawSubToggle(ref Main.settings.RemoveBallCoreParticles, T("tweaks.removeBallCore"));
            if (Main.settings.RemoveBallCoreParticles)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20f);
                GUILayout.Label(T("tweaks.stationaryTailOpacity"), GUILayout.Width(180f));
                float prevOp = Main.settings.StationaryTailOpacity;
                Main.settings.StationaryTailOpacity = GUILayout.HorizontalSlider(Main.settings.StationaryTailOpacity, 0f, 1f, GUILayout.Width(180f));
                GUILayout.Label((Main.settings.StationaryTailOpacity * 100f).ToString("0") + "%", GUILayout.Width(50f));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if (prevOp != Main.settings.StationaryTailOpacity)
                    Tweaks.RefreshBallCoreParticlesTweak();
            }
            DrawSubToggle(ref Main.settings.DisableTileHitGlow, T("tweaks.disableTileHitGlow"));
            DrawSubToggle(ref Main.settings.RemovePlanetGlow, T("tweaks.removePlanetGlow"));
            DrawSubToggle(ref Main.settings.HideJudgementPopups, T("tweaks.hideJudgementPopups"));
            if (Main.settings.HideJudgementPopups)
                DrawHiddenJudgementPopupMask();
            if (prevRemoveCheckpoints != Main.settings.RemoveAllCheckpoints)
                Tweaks.RefreshCheckpointTweak();
            if (prevRemoveBallCoreParticles != Main.settings.RemoveBallCoreParticles)
                Tweaks.RefreshBallCoreParticlesTweak();
            if (prevDisableTileHitGlow != Main.settings.DisableTileHitGlow)
                Tweaks.RefreshTileHitGlowTweak();
            if (prevRemovePlanetGlow != Main.settings.RemovePlanetGlow)
                Tweaks.RefreshPlanetGlowTweak();
        }

        private static void DrawEffectRemoverBody()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(T("effectRemover.save") + ": " + (Main.settings.EffectRemoverEnableSave ? T("common.on") : T("common.off")));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            if (GUILayout.Button(T("effectRemover.toggleSave"), GUILayout.Width(150f), GUILayout.Height(28f)))
            {
                Main.settings.EffectRemoverEnableSave = !Main.settings.EffectRemoverEnableSave;
                EffectRemover.RefreshEditorSaveButtons();
                GUI.changed = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label(T("effectRemover.nonDlc"));
            DrawSubToggle(ref Main.settings.EffectRemoverFilters, T("effectRemover.filter"));
            DrawSubToggle(ref Main.settings.EffectRemoverAdvancedFilters, T("effectRemover.advancedFilter"));
            DrawSubToggle(ref Main.settings.EffectRemoverParticles, T("effectRemover.particles"));
            DrawSubToggle(ref Main.settings.EffectRemoverDecorations, T("effectRemover.decorations"));
            DrawSubToggle(ref Main.settings.EffectRemoverBackgrounds, T("effectRemover.backgrounds"));
            DrawSubToggle(ref Main.settings.EffectRemoverCameras, T("effectRemover.cameras"));
            DrawSubToggle(ref Main.settings.EffectRemoverRepeatEvents, T("effectRemover.repeatEvents"));
            DrawSubToggle(ref Main.settings.EffectRemoverFrameRate, T("effectRemover.frameRate"));
            DrawSubToggle(ref Main.settings.EffectRemoverHitSounds, T("effectRemover.hitSounds"));

            DrawEffectRemoverPlanetPanel();
            DrawEffectRemoverTrackPanel();

            GUILayout.Space(6f);
            GUILayout.Label(T("effectRemover.dlc"));
            DrawSubToggle(ref Main.settings.EffectRemoverHoldSounds, T("effectRemover.holdSounds"));
            DrawSubToggle(ref Main.settings.EffectRemoverHideIcons, T("effectRemover.hideIcons"));

            GUILayout.Space(6f);
            GUILayout.Label(T("effectRemover.misc"));
            if (Main.settings.EffectRemoverDecorations)
                DrawSubToggle(ref Main.settings.EffectRemoverRemoveAllDecorations, T("effectRemover.removeAllDecorations"));
            DrawSubToggle(ref Main.settings.EffectRemoverResetTrackOpacity, T("effectRemover.resetTrackOpacity"));
            if (Main.settings.EffectRemoverCameras)
                DrawEffectRemoverCameraZoom();
            if (Main.settings.EffectRemoverTrackAnimations)
                DrawSubToggle(ref Main.settings.EffectRemoverResetTrackAnimation, T("effectRemover.resetTrackAnimation"));
            if (Main.settings.EffectRemoverTrackColors)
                DrawSubToggle(ref Main.settings.EffectRemoverResetTrackColor, T("effectRemover.resetTrackColor"));
        }

        private static void DrawEffectRemoverPlanetPanel()
        {
            int count = (Main.settings.EffectRemoverPlanetOrbit ? 1 : 0)
                + (Main.settings.EffectRemoverPlanetScale ? 1 : 0)
                + (Main.settings.EffectRemoverPlanetRadius ? 1 : 0);

            Main.settings.EffectRemoverPlanetPanel = GUILayout.Toggle(
                Main.settings.EffectRemoverPlanetPanel,
                Tf("effectRemover.planetEvents", count),
                enableStyle);

            if (!Main.settings.EffectRemoverPlanetPanel) return;

            GUILayout.BeginHorizontal();
            GUILayout.Space(30f);
            if (GUILayout.Button(T("effectRemover.toggleAll"), GUILayout.Width(100f), GUILayout.Height(26f)))
            {
                bool value = count == 0;
                Main.settings.EffectRemoverPlanetOrbit = value;
                Main.settings.EffectRemoverPlanetScale = value;
                Main.settings.EffectRemoverPlanetRadius = value;
                GUI.changed = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawIndentedToggle(ref Main.settings.EffectRemoverPlanetOrbit, T("effectRemover.planetOrbit"));
            DrawIndentedToggle(ref Main.settings.EffectRemoverPlanetScale, T("effectRemover.planetScale"));
            DrawIndentedToggle(ref Main.settings.EffectRemoverPlanetRadius, T("effectRemover.planetRadius"));
        }

        private static void DrawEffectRemoverTrackPanel()
        {
            int count = (Main.settings.EffectRemoverTrackAnimations ? 1 : 0)
                + (Main.settings.EffectRemoverTrackPositions ? 1 : 0)
                + (Main.settings.EffectRemoverTrackMoves ? 1 : 0)
                + (Main.settings.EffectRemoverTrackColors ? 1 : 0);

            Main.settings.EffectRemoverTrackPanel = GUILayout.Toggle(
                Main.settings.EffectRemoverTrackPanel,
                Tf("effectRemover.trackEvents", count),
                enableStyle);

            if (!Main.settings.EffectRemoverTrackPanel) return;

            GUILayout.BeginHorizontal();
            GUILayout.Space(30f);
            if (GUILayout.Button(T("effectRemover.toggleAll"), GUILayout.Width(100f), GUILayout.Height(26f)))
            {
                bool value = count == 0;
                Main.settings.EffectRemoverTrackAnimations = value;
                Main.settings.EffectRemoverTrackPositions = value;
                Main.settings.EffectRemoverTrackMoves = value;
                Main.settings.EffectRemoverTrackColors = value;
                GUI.changed = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawIndentedToggle(ref Main.settings.EffectRemoverTrackAnimations, T("effectRemover.trackAnimations"));
            DrawIndentedToggle(ref Main.settings.EffectRemoverTrackMoves, T("effectRemover.trackMoves"));
            DrawIndentedToggle(ref Main.settings.EffectRemoverTrackPositions, T("effectRemover.trackPositions"));
            DrawIndentedToggle(ref Main.settings.EffectRemoverTrackColors, T("effectRemover.trackColors"));
        }

        private static void DrawEffectRemoverCameraZoom()
        {
            DrawSubToggle(ref Main.settings.EffectRemoverSetCameraZoom, T("effectRemover.setCameraZoom"));
            if (!Main.settings.EffectRemoverSetCameraZoom) return;

            GUILayout.BeginHorizontal();
            GUILayout.Space(28f);
            Main.settings.EffectRemoverCameraZoomScale = GUILayout.HorizontalSlider(
                Main.settings.EffectRemoverCameraZoomScale,
                100.0f,
                1000.0f,
                GUILayout.Width(260f));

            string inputZoom = GUILayout.TextField(Main.settings.EffectRemoverCameraZoomScale.ToString("0.##"), GUILayout.Width(70f));
            float parsedZoomScale;
            if (float.TryParse(inputZoom, out parsedZoomScale))
                Main.settings.EffectRemoverCameraZoomScale = Mathf.Clamp(parsedZoomScale, 100.0f, 1000.0f);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void DrawIndentedToggle(ref bool on, string name)
        {
            EnsureFeatureStyles();
            GUILayout.BeginHorizontal();
            GUILayout.Space(30f);
            on = GUILayout.Toggle(on, name, enableStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void DrawHiddenJudgementPopupMask()
        {
            int[] bits =
            {
                (int)HitMargin.TooEarly,
                (int)HitMargin.VeryEarly,
                (int)HitMargin.EarlyPerfect,
                (int)HitMargin.Perfect,
                (int)HitMargin.LatePerfect,
                (int)HitMargin.VeryLate,
                (int)HitMargin.TooLate,
                (int)HitMargin.Multipress,
                (int)HitMargin.FailMiss,
                (int)HitMargin.FailOverload,
                (int)HitMargin.Auto,
                (int)HitMargin.OverPress
            };
            string[] names =
            {
                T("judgement.tooEarly"),
                T("judgement.veryEarly"),
                T("judgement.earlyPerfect"),
                T("judgement.perfect"),
                T("judgement.latePerfect"),
                T("judgement.veryLate"),
                T("judgement.tooLate"),
                T("judgement.multipress"),
                T("judgement.failMiss"),
                T("judgement.failOverload"),
                T("judgement.auto"),
                T("judgement.overPress")
            };

            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            GUILayout.Label(T("tweaks.hiddenJudgements"));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawJudgementMaskRows(bits, names);

            if (XPerfectBridge.Installed)
            {
                int[] xpBits =
                {
                    Tweaks.XPerfectJudgementPopupBit,
                    Tweaks.PlusPerfectJudgementPopupBit,
                    Tweaks.MinusPerfectJudgementPopupBit
                };
                string[] xpNames =
                {
                    T("judgement.xperfect"),
                    T("judgement.plusPerfect"),
                    T("judgement.minusPerfect")
                };
                DrawJudgementMaskRows(xpBits, xpNames);
            }
        }

        private static void DrawJudgementMaskRows(int[] bits, string[] names)
        {
            for (int row = 0; row < (names.Length + 3) / 4; row++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(34f);
                for (int col = 0; col < 4; col++)
                {
                    int idx = row * 4 + col;
                    if (idx >= names.Length) break;
                    int bit = 1 << bits[idx];
                    bool was = (Main.settings.HiddenJudgementPopupMask & bit) != 0;
                    bool now = GUILayout.Toggle(was, names[idx], GUILayout.Width(140f));
                    if (now != was)
                    {
                        if (now) Main.settings.HiddenJudgementPopupMask |= bit;
                        else Main.settings.HiddenJudgementPopupMask &= ~bit;
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawResourceColor(ref float r, ref float g, ref float b, ref float a, string name, string key, Action onChanged)
        {
            float oldR = r, oldG = g, oldB = b, oldA = a;
            DrawSubColor(ref r, ref g, ref b, ref a, name, key);
            if (oldR != r || oldG != g || oldB != b || oldA != a)
            {
                if (onChanged != null) onChanged();
            }
        }

        private static void DrawResourceColorRgb(ref float r, ref float g, ref float b, string name, string key, Action onChanged)
        {
            float oldR = r, oldG = g, oldB = b;
            DrawSubColorRgb(ref r, ref g, ref b, name, key);
            if (oldR != r || oldG != g || oldB != b)
            {
                if (onChanged != null) onChanged();
            }
        }

        private static void DrawResourceOpacity(ref float val, string name, Action onChanged)
        {
            float old = val;
            DrawSubFloat01(ref val, name);
            if (Mathf.Abs(old - val) > 0.0001f)
            {
                if (onChanged != null) onChanged();
            }
        }

        private static void DrawBallPlanetResource(
            ref float r,
            ref float g,
            ref float b,
            ref float ballOpacity,
            ref float tailOpacity,
            string colorName,
            string ballOpacityName,
            string tailOpacityName,
            string key
        )
        {
            DrawResourceColorRgb(ref r, ref g, ref b, colorName, key + ":color", QueueRefreshPlanetColors);
            DrawResourceOpacity(ref ballOpacity, ballOpacityName, QueueRefreshPlanetColors);
            DrawResourceOpacity(ref tailOpacity, tailOpacityName, QueueRefreshPlanetColors);
        }

        private static void DrawKCBBody()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(T("kcb.threshold"), GUILayout.Width(180f));
            Main.settings.KCBThresholdMs = GUILayout.HorizontalSlider(Main.settings.KCBThresholdMs, 0f, 1000f, GUILayout.Width(180f));
            string s = GUILayout.TextField(Main.settings.KCBThresholdMs.ToString("0"), GUILayout.Width(50f));
            float p;
            if (float.TryParse(s, out p)) Main.settings.KCBThresholdMs = Mathf.Max(0f, p);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static bool DrawOttoOffsetRow(ref float val, string name)
        {
            float old = val;
            GUILayout.BeginHorizontal();
            GUILayout.Space(28f);
            GUILayout.Label(name, GUILayout.Width(180f));
            float slid = GUILayout.HorizontalSlider(val, -500f, 500f, GUILayout.Width(220f));
            if (slid != val) val = slid;
            string s = GUILayout.TextField(val.ToString("0.##"), GUILayout.Width(60f));
            float p;
            if (float.TryParse(s, out p)) val = Mathf.Clamp(p, -5000f, 5000f);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return Mathf.Abs(old - val) > 0.0001f;
        }

        private static void DrawSubFloat01(ref float val, string name)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(28f);
            GUILayout.Label(name, GUILayout.Width(220f));
            float slid = GUILayout.HorizontalSlider(val, 0f, 1f, GUILayout.Width(180f));
            if (slid != val) val = slid;
            string s = GUILayout.TextField(val.ToString("0.##"), GUILayout.Width(50f));
            float p;
            if (float.TryParse(s, out p)) val = Mathf.Clamp01(p);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        internal static bool keyLimiterCapturing;
        private static int keyLimiterPendingCaptureKey = (int)KeyCode.None;
        private static readonly KeyCode[] KeyLimiterCaptureKeyCodes = BuildKeyLimiterCaptureKeyCodes();
        private static readonly HashSet<KeyCode> keyLimiterCaptureHeld = new HashSet<KeyCode>();
        private static KeyCode keyLimiterLastCapturedKey = KeyCode.None;
        private static float keyLimiterLastCapturedAt = -1000f;
        private const float KeyLimiterCaptureRepeatGuardSeconds = 0.12f;

        private static KeyCode[] BuildKeyLimiterCaptureKeyCodes()
        {
            Array values = Enum.GetValues(typeof(KeyCode));
            List<KeyCode> keys = new List<KeyCode>();

            for (int i = 0; i < values.Length; i++)
            {
                KeyCode key = (KeyCode)values.GetValue(i);

                if (key == KeyCode.None)
                    continue;

                string name = key.ToString();

                if (name.StartsWith("Mouse"))
                    continue;

                if (name.StartsWith("Joystick"))
                    continue;

                keys.Add(key);
            }

            return keys.ToArray();
        }

        private static bool IsKeyLimiterCaptureKeyDown(KeyCode key)
        {
            try { return Input.GetKey(key); }
            catch { return false; }
        }

        private static void SeedKeyLimiterCaptureHeldKeys()
        {
            keyLimiterCaptureHeld.Clear();

            for (int i = 0; i < KeyLimiterCaptureKeyCodes.Length; i++)
            {
                KeyCode key = KeyLimiterCaptureKeyCodes[i];
                if (IsKeyLimiterCaptureKeyDown(key))
                    keyLimiterCaptureHeld.Add(key);
            }
        }

        private static void RefreshKeyLimiterCaptureHeldKeys()
        {
            for (int i = 0; i < KeyLimiterCaptureKeyCodes.Length; i++)
            {
                KeyCode key = KeyLimiterCaptureKeyCodes[i];
                if (!IsKeyLimiterCaptureKeyDown(key))
                    keyLimiterCaptureHeld.Remove(key);
            }
        }

        private static void StartKeyLimiterCapture()
        {
            keyLimiterCapturing = true;
            keyLimiterPendingCaptureKey = (int)KeyCode.None;
            keyLimiterLastCapturedKey = KeyCode.None;
            keyLimiterLastCapturedAt = -1000f;
            SeedKeyLimiterCaptureHeldKeys();
        }

        private static void StopKeyLimiterCapture()
        {
            keyLimiterCapturing = false;
            keyLimiterPendingCaptureKey = (int)KeyCode.None;
            keyLimiterCaptureHeld.Clear();
            keyLimiterLastCapturedKey = KeyCode.None;
            keyLimiterLastCapturedAt = -1000f;
        }

        private static KeyCode ClaimKeyLimiterCapture(KeyCode key)
        {
            if (key == KeyCode.None)
                return KeyCode.None;

            if (keyLimiterCaptureHeld.Contains(key))
                return KeyCode.None;

            float now = Time.realtimeSinceStartup;
            if (key == keyLimiterLastCapturedKey
                && now - keyLimiterLastCapturedAt < KeyLimiterCaptureRepeatGuardSeconds)
            {
                keyLimiterCaptureHeld.Add(key);
                return KeyCode.None;
            }

            keyLimiterCaptureHeld.Add(key);
            keyLimiterLastCapturedKey = key;
            keyLimiterLastCapturedAt = now;
            return key;
        }

        private static KeyCode CharacterToKeyCode(char c)
        {
            switch (c)
            {
                case 'a':
                case 'A': return KeyCode.A;
                case 'b':
                case 'B': return KeyCode.B;
                case 'c':
                case 'C': return KeyCode.C;
                case 'd':
                case 'D': return KeyCode.D;
                case 'e':
                case 'E': return KeyCode.E;
                case 'f':
                case 'F': return KeyCode.F;
                case 'g':
                case 'G': return KeyCode.G;
                case 'h':
                case 'H': return KeyCode.H;
                case 'i':
                case 'I': return KeyCode.I;
                case 'j':
                case 'J': return KeyCode.J;
                case 'k':
                case 'K': return KeyCode.K;
                case 'l':
                case 'L': return KeyCode.L;
                case 'm':
                case 'M': return KeyCode.M;
                case 'n':
                case 'N': return KeyCode.N;
                case 'o':
                case 'O': return KeyCode.O;
                case 'p':
                case 'P': return KeyCode.P;
                case 'q':
                case 'Q': return KeyCode.Q;
                case 'r':
                case 'R': return KeyCode.R;
                case 's':
                case 'S': return KeyCode.S;
                case 't':
                case 'T': return KeyCode.T;
                case 'u':
                case 'U': return KeyCode.U;
                case 'v':
                case 'V': return KeyCode.V;
                case 'w':
                case 'W': return KeyCode.W;
                case 'x':
                case 'X': return KeyCode.X;
                case 'y':
                case 'Y': return KeyCode.Y;
                case 'z':
                case 'Z': return KeyCode.Z;

                case '0':
                case ')': return KeyCode.Alpha0;
                case '1':
                case '!': return KeyCode.Alpha1;
                case '2':
                case '@': return KeyCode.Alpha2;
                case '3':
                case '#': return KeyCode.Alpha3;
                case '4':
                case '$': return KeyCode.Alpha4;
                case '5':
                case '%': return KeyCode.Alpha5;
                case '6':
                case '^': return KeyCode.Alpha6;
                case '7':
                case '&': return KeyCode.Alpha7;
                case '8':
                case '*': return KeyCode.Alpha8;
                case '9':
                case '(': return KeyCode.Alpha9;

                case ' ': return KeyCode.Space;

                case '`':
                case '~': return KeyCode.BackQuote;

                case '-':
                case '_': return KeyCode.Minus;

                case '=':
                case '+': return KeyCode.Equals;

                case '[':
                case '{': return KeyCode.LeftBracket;

                case ']':
                case '}': return KeyCode.RightBracket;

                case '\\':
                case '|': return KeyCode.Backslash;

                case ';':
                case ':': return KeyCode.Semicolon;

                case '\'':
                case '"': return KeyCode.Quote;

                case ',':
                case '<': return KeyCode.Comma;

                case '.':
                case '>': return KeyCode.Period;

                case '/':
                case '?': return KeyCode.Slash;
            }

            return KeyCode.None;
        }

        private static KeyCode CaptureAnyKeyLimiterKey(Event e)
        {
            if (e != null && e.type == EventType.KeyDown)
            {
                if (e.keyCode != KeyCode.None)
                    return ClaimKeyLimiterCapture(e.keyCode);

                if (e.character != '\0')
                {
                    KeyCode fromChar = CharacterToKeyCode(e.character);

                    if (fromChar != KeyCode.None)
                        return ClaimKeyLimiterCapture(fromChar);
                }
            }

            for (int i = 0; i < KeyLimiterCaptureKeyCodes.Length; i++)
            {
                KeyCode key = KeyLimiterCaptureKeyCodes[i];

                if (Input.GetKeyDown(key))
                    return ClaimKeyLimiterCapture(key);
            }

            return KeyCode.None;
        }

        private static void ToggleKeyLimiterKey(int captured)
        {
            int[] arr = Main.settings.KeyLimiterAllowed ?? new int[0];

            int existing = -1;

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == captured)
                {
                    existing = i;
                    break;
                }
            }

            if (existing >= 0)
            {
                int[] shrunk = new int[arr.Length - 1];

                Array.Copy(arr, 0, shrunk, 0, existing);
                Array.Copy(arr, existing + 1, shrunk, existing, arr.Length - existing - 1);

                Main.settings.KeyLimiterAllowed = shrunk;
            }
            else
            {
                int[] grown = new int[arr.Length + 1];

                Array.Copy(arr, grown, arr.Length);
                grown[arr.Length] = captured;

                Main.settings.KeyLimiterAllowed = grown;
            }
        }

        private static bool IsKeyLimiterLockedBySync()
        {
            return Main.settings != null
                && Main.settings.keyViewerOn
                && Main.settings.KeyViewerSimpleSyncToKeyLimiter
                && string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase);
        }

        private static void DrawKeyLimiterBody()
        {
            SyncSimpleKeysToKeyLimiter();
            bool locked = IsKeyLimiterLockedBySync();
            if (locked && keyLimiterCapturing)
                StopKeyLimiterCapture();

            int[] arr = Main.settings.KeyLimiterAllowed ?? new int[0];
            Event e = Event.current;

            if (!locked
                && e != null
                && e.type == EventType.Layout
                && keyLimiterPendingCaptureKey != (int)KeyCode.None)
            {
                ToggleKeyLimiterKey(keyLimiterPendingCaptureKey);
                GUI.changed = true;
                keyLimiterPendingCaptureKey = (int)KeyCode.None;
                arr = Main.settings.KeyLimiterAllowed ?? new int[0];
            }

            if (keyLimiterCapturing)
            {
                RefreshKeyLimiterCaptureHeldKeys();

                KeyCode capturedKey = keyLimiterPendingCaptureKey == (int)KeyCode.None
                    ? CaptureAnyKeyLimiterKey(e)
                    : KeyCode.None;

                if (e != null && (e.type == EventType.KeyDown || e.type == EventType.KeyUp))
                {
                    e.Use();
                }

                if (capturedKey != KeyCode.None)
                {
                    keyLimiterPendingCaptureKey = (int)capturedKey;
                }
            }

            GUILayout.BeginVertical("box");

            if (locked)
            {
                GUILayout.Label(T("keyLimiter.lockedBySync"));
                GUILayout.Space(6);
            }

            bool prevEnabled = GUI.enabled;
            GUI.enabled = prevEnabled && !locked;

            GUILayout.BeginHorizontal();

            string captureLabel = keyLimiterCapturing
                ? T("keyLimiter.capturing")
                : T("keyLimiter.addRemove");
            if (GUILayout.Button(captureLabel, GUILayout.ExpandWidth(false)))
            {
                if (keyLimiterCapturing)
                    StopKeyLimiterCapture();
                else
                    StartKeyLimiterCapture();
            }

            if (GUILayout.Button(T("common.clearAll"), GUILayout.Width(100)))
            {
                Main.settings.KeyLimiterAllowed = new int[0];
                arr = Main.settings.KeyLimiterAllowed;
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            if (keyLimiterCapturing)
            {
                GUILayout.Label(T("keyLimiter.hint"));
            }

            GUILayout.Space(6);

            arr = Main.settings.KeyLimiterAllowed ?? new int[0];

            if (arr.Length == 0)
            {
                GUILayout.Label(T("keyLimiter.none"));
            }
            else
            {
                GUILayout.Label(T("keyLimiter.allowed"));

                for (int i = 0; i < arr.Length; i++)
                {
                    KeyCode key = (KeyCode)arr[i];

                    GUILayout.BeginHorizontal("box");

                    GUILayout.Label(key.ToString());

                    if (GUILayout.Button(T("common.remove"), GUILayout.Width(80)))
                    {
                        ToggleKeyLimiterKey((int)key);
                        arr = Main.settings.KeyLimiterAllowed ?? new int[0];
                        GUILayout.EndHorizontal();
                        break;
                    }

                    GUILayout.EndHorizontal();
                }
            }

            GUI.enabled = prevEnabled;

            GUILayout.EndVertical();
        }

        private static string jrestrictAccBuf;
        private static void DrawJRestrictBody()
        {
            
            bool xpAvail = XPerfectBridge.Installed;
            int[] modeIndices = xpAvail ? new[] { 0, 4, 1, 2, 3 } : new[] { 0, 4, 1, 3 };
            string[] modeLabels =
            {
                T("jrestrict.mode.accuracy"),
                T("jrestrict.mode.pure"),
                T("jrestrict.mode.xpure"),
                T("jrestrict.mode.custom"),
                T("jrestrict.mode.nomiss")
            };
            
            if (!xpAvail && Main.settings.JRestrictMode == 2) Main.settings.JRestrictMode = 1;
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(T("label.mode"), GUILayout.Width(80f));
            for (int idx = 0; idx < modeIndices.Length; idx++)
            {
                int modeI = modeIndices[idx];
                bool was = Main.settings.JRestrictMode == modeI;
                bool now = GUILayout.Toggle(was, modeLabels[modeI], GUILayout.Width(150f));
                if (now && !was) Main.settings.JRestrictMode = modeI;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (Main.settings.JRestrictMode == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(14f);
                GUILayout.Label(T("jrestrict.minAccuracy"), GUILayout.Width(180f));
                Main.settings.JRestrictAccuracy = GUILayout.HorizontalSlider(Main.settings.JRestrictAccuracy, 0f, 100f, GUILayout.Width(220f));
                jrestrictAccBuf = GUILayout.TextField(jrestrictAccBuf ?? Main.settings.JRestrictAccuracy.ToString("0.##"), GUILayout.Width(60f));
                float p;
                if (float.TryParse(jrestrictAccBuf, out p)) Main.settings.JRestrictAccuracy = Mathf.Clamp(p, 0f, 100f);
                else jrestrictAccBuf = Main.settings.JRestrictAccuracy.ToString("0.##");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else if (Main.settings.JRestrictMode == 3)
            {
                
                string[] names =
                {
                    T("judgement.tooEarly"),
                    T("judgement.veryEarly"),
                    T("judgement.earlyPerfect"),
                    T("judgement.perfect"),
                    T("judgement.latePerfect"),
                    T("judgement.veryLate"),
                    T("judgement.tooLate")
                };
                GUILayout.BeginHorizontal();
                GUILayout.Space(14f);
                GUILayout.Label(T("jrestrict.allowed"));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                for (int row = 0; row < (names.Length + 3) / 4; row++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(28f);
                    for (int col = 0; col < 4; col++)
                    {
                        int idx = row * 4 + col;
                        if (idx >= names.Length) break;
                        int bit = 1 << idx;
                        bool was = (Main.settings.JRestrictAllowedMask & bit) != 0;
                        bool now = GUILayout.Toggle(was, names[idx], GUILayout.Width(140f));
                        if (now != was)
                        {
                            if (now) Main.settings.JRestrictAllowedMask |= bit;
                            else Main.settings.JRestrictAllowedMask &= ~bit;
                        }
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }
        }

        private static void AutosaveTick(UnityModManager.ModEntry modEntry)
        {
            
            if (GUI.changed)
            {
                settingsDirty = true;
                settingsDirtySince = Time.realtimeSinceStartup;
            }

            FlushAutosaveIfDue(modEntry);
        }

        internal static void FlushAutosaveIfDue(UnityModManager.ModEntry modEntry)
        {
            if (!settingsDirty) return;
            if (Time.realtimeSinceStartup - settingsDirtySince < SettingsAutosaveQuietSeconds) return;
            if (Main.settings == null) return;

            try
            {
                Main.settings.Save(modEntry);
                settingsDirty = false;
            }
            catch (Exception ex)
            {
                modEntry?.Logger?.Log("[Settings] autosave failed: " + ex.Message);
                
                settingsDirtySince = Time.realtimeSinceStartup;
            }
        }

        internal static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settingsDirty = false;
            Main.settings.Save(modEntry);
            KorenResourcePack.Audio.Fmod.SaveRuntimePrefs();
        }

        private static void DrawFmodBody()
        {
            KorenResourcePack.Audio.Fmod.DrawFmodLogo();
            GUILayout.Label(T("fmod.attribution"));

            GUILayout.Space(6f);

            if (KorenResourcePack.Audio.Fmod.Initialized)
            {
                GUILayout.Space(6f);
                int driverCount = KorenResourcePack.Audio.Fmod.GetDriverCount();
                GUILayout.Label(T("fmod.outputDevice"));
                int sel = Main.settings.FmodSelectedDriver;
                for (int i = 0; i < driverCount; i++)
                {
                    string name = KorenResourcePack.Audio.Fmod.GetDriverName(i);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(14f);
                    bool isSel = i == sel;
                    bool nowSel = GUILayout.Toggle(isSel, (isSel ? "● " : "○ ") + name);
                    if (nowSel && !isSel)
                    {
                        Main.settings.FmodSelectedDriver = i;
                        KorenResourcePack.Audio.Fmod.SelectedDriver = i;
                        KorenResourcePack.Audio.Fmod.ApplySelectedDriver();
                        GUI.changed = true;
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(6f);
                bool prevAsio = Main.settings.FmodUseASIO;
                Main.settings.FmodUseASIO = GUILayout.Toggle(Main.settings.FmodUseASIO, T("fmod.useAsio"));
                if (prevAsio != Main.settings.FmodUseASIO)
                {
                    KorenResourcePack.Audio.Fmod.SetASIO(Main.settings.FmodUseASIO, Main.mod);
                    GUI.changed = true;
                }
            }
            else if (Main.settings.FmodEnabled)
            {
                GUILayout.Space(6f);
                GUILayout.Label(T("fmod.notInitialized"));
            }
        }
    }
}
