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
        private static bool fontDropdownOpen;

        /// <summary>
        /// Names shown in the Font dropdown. When a TMP AssetBundle is loaded, these are the bundle font keys
        /// (TMP asset names without " SDF" etc.); otherwise TTF names from the mod Fonts/ folder (IMGUI path).
        /// </summary>
        private static List<string> GetHudFontChoices()
        {
            BundleLoader.EnsureBundleLoaded();
            if (BundleLoader.BundleAvailable && BundleLoader.bundleFonts.Count > 0)
            {
                var list = new List<string>(BundleLoader.bundleFonts.Keys);
                list.Sort(StringComparer.OrdinalIgnoreCase);
                return list;
            }

            return FontLoader.GetBundledFontNames();
        }

        private static readonly Dictionary<string, string> colorBuffers = new Dictionary<string, string>();
        private static readonly HashSet<string> colorExpanded = new HashSet<string>();

        internal static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            // While the UMM settings panel is open, bare Shift presses (LShift/RShift)
            // propagate into IMGUI focus handling and collapse the surrounding
            // GUILayout.Toggle / foldout that has IMGUI focus — looks like the menu
            // is closing. Swallow them so the menu stays open while the user holds
            // a play key that happens to be Shift (common with KeyLimiter setups).
            Event __krpEv = Event.current;
            if (__krpEv != null
                && (__krpEv.type == EventType.KeyDown || __krpEv.type == EventType.KeyUp)
                && (__krpEv.keyCode == KeyCode.LeftShift || __krpEv.keyCode == KeyCode.RightShift))
            {
                __krpEv.Use();
            }

            Main.settings.EnsureColorRanges();
            GUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            if (Main.settings.language == "en"){GUILayout.Label("Size", GUILayout.Width(60f));} else {GUILayout.Label("크기", GUILayout.Width(60f));}
            Main.settings.size = GUILayout.HorizontalSlider(Main.settings.size, 0.5f, 2.0f, GUILayout.Width(240f));
            string sizeStr = GUILayout.TextField(Main.settings.size.ToString("0.##"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(sizeStr, out parsed)) Main.settings.size = Mathf.Clamp(parsed, 0.5f, 2.0f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (Main.settings.language == "en"){GUILayout.Label("Language", GUILayout.Width(100f));} else {GUILayout.Label("언어", GUILayout.Width(60f));}
            if (GUILayout.Button("English", GUILayout.Width(100f)))
            {
                Main.settings.language = "en";
            }
            if (GUILayout.Button("한국어", GUILayout.Width(100f)))
            {
                Main.settings.language = "kr";
            }
            GUILayout.EndHorizontal();

            List<string> fontChoices = GetHudFontChoices();
            if (fontChoices.Count > 0 && string.IsNullOrEmpty(Main.settings.fontName))
            {
                Main.settings.fontName = fontChoices[0];
                preferredHudFont = null;
                Overlay.InvalidateOverlayFontCache();
            }
            else if (BundleLoader.BundleAvailable && fontChoices.Count > 0 && !string.IsNullOrEmpty(Main.settings.fontName))
            {
                if (!BundleLoader.bundleFonts.ContainsKey(Main.settings.fontName))
                {
                    Main.settings.fontName = fontChoices[0];
                    preferredHudFont = null;
                    Overlay.InvalidateOverlayFontCache();
                }
            }

            GUILayout.BeginHorizontal();
            if (Main.settings.language == "en") { GUILayout.Label("Font", GUILayout.Width(100f)); } else { GUILayout.Label("폰트", GUILayout.Width(60f)); }
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
                        preferredHudFont = null;
                        Overlay.InvalidateOverlayFontCache();
                        fontDropdownOpen = false;
                    }
                }
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            if (Main.settings.language == "en")
            {
                DrawExpandable(ref Main.settings.progressBarOn, ref Main.settings.progressBarExpanded, "ProgressBar", DrawProgressBarBody);
                DrawExpandable(ref Main.settings.statusOn, ref Main.settings.statusExpanded, "Status", DrawStatusBody);
                DrawExpandable(ref Main.settings.bpmOn, ref Main.settings.bpmExpanded, "BPM", DrawBpmBody);
                DrawExpandable(ref Main.settings.comboOn, ref Main.settings.comboExpanded, "Combo", DrawComboBody);
                DrawExpandable(ref Main.settings.judgementOn, ref Main.settings.judgementExpanded, "Judgement", DrawJudgementBody);
                DrawExpandable(ref Main.settings.holdOn, ref Main.settings.holdExpanded, "Hold", DrawHoldBody);
                DrawExpandable(ref Main.settings.attemptOn, ref Main.settings.attemptExpanded, "Attempt", DrawAttemptBody);
                DrawExpandable(ref Main.settings.timingScaleOn, ref Main.settings.timingScaleExpanded, "TimingScale", DrawTimingScaleBody);
                DrawExpandable(ref Main.settings.keyViewerOn, ref Main.settings.keyViewerExpanded, "KeyViewer", DrawKeyViewerBody);
                DrawResourceChangerExpandable("Resource Changer");
                DrawTweaksExpandable("Tweaks");
                DrawExpandable(ref Main.settings.KCBOn, ref Main.settings.KCBExpanded, "Keyboard Chatter Blocker", DrawKCBBody);
                DrawExpandable(ref Main.settings.KeyLimiterOn, ref Main.settings.KeyLimiterExpanded, "Key Limiter", DrawKeyLimiterBody);
                DrawExpandable(ref Main.settings.JRestrictOn, ref Main.settings.JRestrictExpanded, "Judgement Restriction", DrawJRestrictBody);
            } else
            {
                DrawExpandable(ref Main.settings.progressBarOn, ref Main.settings.progressBarExpanded, "프로그레스바", DrawProgressBarBody);
                DrawExpandable(ref Main.settings.statusOn, ref Main.settings.statusExpanded, "표시 설정", DrawStatusBody);
                DrawExpandable(ref Main.settings.bpmOn, ref Main.settings.bpmExpanded, "브픔", DrawBpmBody);
                DrawExpandable(ref Main.settings.comboOn, ref Main.settings.comboExpanded, "콤보", DrawComboBody);
                DrawExpandable(ref Main.settings.judgementOn, ref Main.settings.judgementExpanded, "판정", DrawJudgementBody);
                DrawExpandable(ref Main.settings.holdOn, ref Main.settings.holdExpanded, "홀드", DrawHoldBody);
                DrawExpandable(ref Main.settings.attemptOn, ref Main.settings.attemptExpanded, "시도", DrawAttemptBody);
                DrawExpandable(ref Main.settings.timingScaleOn, ref Main.settings.timingScaleExpanded, "타이밍 스케일", DrawTimingScaleBody);
                DrawExpandable(ref Main.settings.keyViewerOn, ref Main.settings.keyViewerExpanded, "키뷰어", DrawKeyViewerBody);
                DrawResourceChangerExpandable("리소스 체인저");
                DrawTweaksExpandable("트윅");
                DrawExpandable(ref Main.settings.KCBOn, ref Main.settings.KCBExpanded, "키보드 채터 블로커", DrawKCBBody);
                DrawExpandable(ref Main.settings.KeyLimiterOn, ref Main.settings.KeyLimiterExpanded, "키 리미터", DrawKeyLimiterBody);
                DrawExpandable(ref Main.settings.JRestrictOn, ref Main.settings.JRestrictExpanded, "판정 제한", DrawJRestrictBody);
            }
            GUILayout.EndVertical();
        }

        private static void DrawStatusBody()
        {
            if (Main.settings.language == "en")
            {
                DrawSubToggle(ref Main.settings.ShowProgress, "Show progress");
                if (Main.settings.ShowProgress)
                    DrawColorRange(ref Main.settings.ProgressColor, "Progress color", "statusProgressColor", Settings.JipperProgressColor());
                DrawSubToggle(ref Main.settings.ShowAccuracy, "Show accuracy");
                if (Main.settings.ShowAccuracy)
                    DrawColorRange(ref Main.settings.AccuracyColor, "Accuracy color", "statusAccuracyColor", Settings.JipperAccuracyColor());
                DrawSubToggle(ref Main.settings.ShowXAccuracy, "Show X-accuracy");
                if (Main.settings.ShowXAccuracy)
                    DrawColorRange(ref Main.settings.XAccuracyColor, "X-accuracy color", "statusXAccuracyColor", Settings.JipperAccuracyColor());
                DrawSubToggle(ref Main.settings.ShowMusicTime, "Show music time");
                if (Main.settings.ShowMusicTime)
                    DrawColorRange(ref Main.settings.MusicTimeColor, "Music time color", "statusMusicTimeColor", Settings.WhiteColorRange());
                DrawSubToggle(ref Main.settings.ShowMapTime, "Show map time");
                DrawSubToggle(ref Main.settings.ShowMapTimeIfNotMusic, "Use map time when music is missing");
                if (Main.settings.ShowMapTime)
                    DrawColorRange(ref Main.settings.MapTimeColor, "Map time color", "statusMapTimeColor", Settings.WhiteColorRange());
                DrawSubToggle(ref Main.settings.ShowCheckpoint, "Show checkpoint");
                DrawSubToggle(ref Main.settings.ShowBest, "Show best");
                if (Main.settings.ShowBest)
                    DrawColorRange(ref Main.settings.BestColor, "Best color", "statusBestColor", Settings.JipperProgressColor());
                DrawSubToggle(ref Main.settings.ShowFPS, "Show FPS");
                DrawSubToggle(ref Main.settings.HideDebugText, "Hide debug text");
                DrawDecimalPlacesRow("Decimal places");
            } else
            {
                DrawSubToggle(ref Main.settings.ShowProgress, "프로그레스 퍼센트 표시");
                if (Main.settings.ShowProgress)
                    DrawColorRange(ref Main.settings.ProgressColor, "프로그레스 색상", "statusProgressColor", Settings.JipperProgressColor());
                DrawSubToggle(ref Main.settings.ShowAccuracy, "정확도 표시");
                if (Main.settings.ShowAccuracy)
                    DrawColorRange(ref Main.settings.AccuracyColor, "정확도 색상", "statusAccuracyColor", Settings.JipperAccuracyColor());
                DrawSubToggle(ref Main.settings.ShowXAccuracy, "절대 정확도 표시");
                if (Main.settings.ShowXAccuracy)
                    DrawColorRange(ref Main.settings.XAccuracyColor, "절대 정확도 색상", "statusXAccuracyColor", Settings.JipperAccuracyColor());
                DrawSubToggle(ref Main.settings.ShowMusicTime, "음악 시간 표시");
                if (Main.settings.ShowMusicTime)
                    DrawColorRange(ref Main.settings.MusicTimeColor, "음악 시간 색상", "statusMusicTimeColor", Settings.WhiteColorRange());
                DrawSubToggle(ref Main.settings.ShowMapTime, "맵 시간 표시");
                DrawSubToggle(ref Main.settings.ShowMapTimeIfNotMusic, "음악이 없으면 맵 시간 사용");
                if (Main.settings.ShowMapTime)
                    DrawColorRange(ref Main.settings.MapTimeColor, "맵 시간 색상", "statusMapTimeColor", Settings.WhiteColorRange());
                DrawSubToggle(ref Main.settings.ShowCheckpoint, "체크포인트 표시");
                DrawSubToggle(ref Main.settings.ShowBest, "최고 표시");
                if (Main.settings.ShowBest)
                    DrawColorRange(ref Main.settings.BestColor, "최고 색상", "statusBestColor", Settings.JipperProgressColor());
                DrawSubToggle(ref Main.settings.ShowFPS, "프레임 표시");
                DrawSubToggle(ref Main.settings.HideDebugText, "디버그 텍스트 숨기기");
                DrawDecimalPlacesRow("소수점 자리수");
            }
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
            if (Main.settings.language == "en")
            {
                DrawColorRange(ref Main.settings.ProgressBarFillColor, "Fill color", "pbFillRange", Settings.JipperProgressBarFillColor());
                DrawColorRange(ref Main.settings.ProgressBarBackColor, "Background color", "pbBackRange", Settings.JipperProgressBarBackgroundColor());
                DrawColorRange(ref Main.settings.ProgressBarBorderColor, "Border color", "pbBorderRange", Settings.JipperProgressBarBorderColor());
            } else
            {
                DrawColorRange(ref Main.settings.ProgressBarFillColor, "채움 색상", "pbFillRange", Settings.JipperProgressBarFillColor());
                DrawColorRange(ref Main.settings.ProgressBarBackColor, "배경 색상", "pbBackRange", Settings.JipperProgressBarBackgroundColor());
                DrawColorRange(ref Main.settings.ProgressBarBorderColor, "테두리 색상", "pbBorderRange", Settings.JipperProgressBarBorderColor());
            }
        }

        private static void DrawBpmBody()
        {
            if (Main.settings.language == "en")
            {
                DrawSubFloat(ref Main.settings.BpmColorMax, ref bpmColorMaxStr, "BPM color max", 0f, 100000f);
                DrawColorRange(ref Main.settings.BpmColor, "BPM color", "bpmColor", Settings.JipperBpmColor());
            }
            else
            {
                DrawSubFloat(ref Main.settings.BpmColorMax, ref bpmColorMaxStr, "최대 브픔 색깔", 0f, 100000f);
                DrawColorRange(ref Main.settings.BpmColor, "브픔 색상", "bpmColor", Settings.JipperBpmColor());
            }
        }

        private static void DrawComboBody()
        {
            if (Main.settings.language == "en") {DrawSubToggle(ref Main.settings.EnableAutoCombo, "Enable auto combo");} else {DrawSubToggle(ref Main.settings.EnableAutoCombo, "오토 콤보");}
            if (XPerfectBridge.Installed)
            {
                if (Main.settings.language == "en") {DrawSubToggle(ref Main.settings.XPerfectComboEnabled, "XPerfect-only combo (break on +/-Perfect)");} else {DrawSubToggle(ref Main.settings.XPerfectComboEnabled, "XPerfect 전용 콤보 (+/-Perfect에서 끊김)");}
            }
            if (Main.settings.language == "en")
            {
                DrawSubInt(ref Main.settings.ComboColorMax, ref comboColorMaxStr, "Combo color max", 0, 1000000);
                DrawColorRange(ref Main.settings.ComboColor, "Combo color", "comboColorRange", Settings.JipperComboColor());
            }
            else
            {
                DrawSubInt(ref Main.settings.ComboColorMax, ref comboColorMaxStr, "최대 콤보 색깔", 0, 1000000);
                DrawColorRange(ref Main.settings.ComboColor, "콤보 색상", "comboColorRange", Settings.JipperComboColor());
            }
            if (Main.settings.language == "en") {DrawSubToggle(ref Main.settings.ComboMoveUpNoCaption, "Move up when no title/artist");} else {DrawSubToggle(ref Main.settings.ComboMoveUpNoCaption, "제목/작가가 없을 때 위로 올리기");}
            if (Main.settings.language == "en") {DrawExpandable(ref Main.settings.CaptionText, ref Main.settings.captionExpanded, "Show Perfect Combo Text", DrawPerfectComboExpanded);} else {DrawExpandable(ref Main.settings.CaptionText, ref Main.settings.captionExpanded, "Perfect Combo 글자 표시", DrawPerfectComboExpanded);}
            if (Main.settings.language == "en") {DrawSubToggle(ref Main.settings.comboFastAnim, "Make Animation More Snappy");} else {{DrawSubToggle(ref Main.settings.comboFastAnim, "콤보 에니매이션 더 빠르게 하기");}}
            GUILayout.BeginHorizontal();
            if (Main.settings.language == "en"){GUILayout.Label("Y offset", GUILayout.Width(100f));} else {GUILayout.Label("Y 오프셋", GUILayout.Width(100f));}
            Main.settings.comboY = GUILayout.HorizontalSlider(Main.settings.comboY, -200, 200, GUILayout.Width(240f));
            string comboYStr = GUILayout.TextField(Main.settings.comboY.ToString("0"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(comboYStr, out parsed)) Main.settings.comboY = Mathf.Clamp(parsed, -200, 200);
            GUILayout.EndHorizontal();
        }

        private static void DrawPerfectComboExpanded()
        {
            GUILayout.BeginHorizontal();
            if (Main.settings.language == "en"){GUILayout.Label("Position", GUILayout.Width(80f));} else {GUILayout.Label("위치", GUILayout.Width(80f));}
            Main.settings.captionY = GUILayout.HorizontalSlider(Main.settings.captionY, -100, 200, GUILayout.Width(240f));
            string perfectComboStr = GUILayout.TextField(Main.settings.captionY.ToString("0"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(perfectComboStr, out parsed)) Main.settings.captionY = Mathf.Clamp(parsed, -100, 200);
            GUILayout.EndHorizontal();
        }

        private static void DrawJudgementBody()
        {
            GUILayout.BeginHorizontal();
            if (Main.settings.language == "en") {GUILayout.Label("Location", GUILayout.Width(90f));} else {GUILayout.Label("위치", GUILayout.Width(80f));}
            Main.settings.judgementPositionY = GUILayout.HorizontalSlider(Main.settings.judgementPositionY, -100, 200, GUILayout.Width(240f));
            string judgementPositionYStr = GUILayout.TextField(Main.settings.judgementPositionY.ToString("0"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(judgementPositionYStr, out parsed)) Main.settings.judgementPositionY = Mathf.Clamp(parsed, -100, 200);
            GUILayout.EndHorizontal();
        }

        private static void DrawHoldBody()
        {
            GUILayout.BeginHorizontal();
            if (Main.settings.language == "en") {GUILayout.Label("X offset (px)", GUILayout.Width(140f));} else {GUILayout.Label("X 오프셋 (px)", GUILayout.Width(140f));}
            Main.settings.HoldOffsetX = GUILayout.HorizontalSlider(Main.settings.HoldOffsetX, -200f, 200f, GUILayout.Width(240f));
            string holdOffsetXStr = GUILayout.TextField(Main.settings.HoldOffsetX.ToString("0"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(holdOffsetXStr, out parsed)) Main.settings.HoldOffsetX = Mathf.Clamp(parsed, -200f, 200f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (Main.settings.language == "en") {GUILayout.Label("Y offset (px)", GUILayout.Width(140f));} else {GUILayout.Label("Y 오프셋 (px)", GUILayout.Width(140f));}
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

            if (GUILayout.Button(Main.settings.language == "kr" ? "색상 추가" : "Add color", GUILayout.Width(120f)))
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

                if (GUILayout.Button(Main.settings.language == "kr" ? "삭제" : "Delete", GUILayout.Width(90f)))
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
            GUILayout.Label("Percent", GUILayout.Width(70f));
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
            string hex = GetBuf(hexKey, GetHex(r, g, b, a));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hex", GUILayout.Width(40f));
            string newHex = GUILayout.TextField(hex, GUILayout.Width(100f));
            bool userTypedHex = newHex != hex;
            if (userTypedHex)
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

            // Only resync the hex buffer to the current RGBA when the user is not actively
            // typing into the hex field; otherwise intermediate invalid strings get clobbered.
            if (!userTypedHex)
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
            string hex = GetBuf(hexKey, GetHex(r, g, b, 1f));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hex", GUILayout.Width(40f));
            string newHex = GUILayout.TextField(hex, GUILayout.Width(100f));
            bool userTypedHex = newHex != hex;
            if (userTypedHex)
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

            if (!userTypedHex)
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
            bool wasOn = Main.settings.ResourceChangerOn;
            DrawExpandable(ref Main.settings.ResourceChangerOn, ref Main.settings.ResourceChangerExpanded, name, DrawResourceChangerBody);
            if (wasOn == Main.settings.ResourceChangerOn) return;

            if (Main.settings.ResourceChangerOn) ResourceChanger.RefreshChangedResources();
            else ResourceChanger.RestoreChangedResources();
        }

        private static void DrawTweaksExpandable(string name)
        {
            bool wasOn = Main.settings.TweaksOn;
            DrawExpandable(ref Main.settings.TweaksOn, ref Main.settings.TweaksExpanded, name, DrawTweaksBody);
            if (wasOn != Main.settings.TweaksOn)
                Tweaks.RefreshTweaks();
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
            if (Main.settings.language == "en")
            {
                DrawSubToggle(ref Main.settings.ShowAttempt, "Show attempt count");
                DrawSubToggle(ref Main.settings.ShowFullAttempt, "Show full attempt total");
            }
            else
            {
                DrawSubToggle(ref Main.settings.ShowAttempt, "시도 횟수 표시");
                DrawSubToggle(ref Main.settings.ShowFullAttempt, "전체 시도 합계 표시");
            }

            GUILayout.BeginHorizontal();
            if (Main.settings.language == "en") { GUILayout.Label("X offset (px)", GUILayout.Width(140f)); } else { GUILayout.Label("X 오프셋 (px)", GUILayout.Width(140f)); }
            Main.settings.AttemptOffsetX = GUILayout.HorizontalSlider(Main.settings.AttemptOffsetX, -400f, 400f, GUILayout.Width(240f));
            string axStr = GUILayout.TextField(Main.settings.AttemptOffsetX.ToString("0"), GUILayout.Width(60f));
            float axP;
            if (float.TryParse(axStr, out axP)) Main.settings.AttemptOffsetX = Mathf.Clamp(axP, -400f, 400f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (Main.settings.language == "en") { GUILayout.Label("Y offset (px)", GUILayout.Width(140f)); } else { GUILayout.Label("Y 오프셋 (px)", GUILayout.Width(140f)); }
            Main.settings.AttemptOffsetY = GUILayout.HorizontalSlider(Main.settings.AttemptOffsetY, -200f, 400f, GUILayout.Width(240f));
            string ayStr = GUILayout.TextField(Main.settings.AttemptOffsetY.ToString("0"), GUILayout.Width(60f));
            float ayP;
            if (float.TryParse(ayStr, out ayP)) Main.settings.AttemptOffsetY = Mathf.Clamp(ayP, -200f, 400f);
            GUILayout.EndHorizontal();
        }

        private static void DrawTimingScaleBody()
        {
            GUILayout.BeginHorizontal();
            if (Main.settings.language == "en") { GUILayout.Label("Y offset (px)", GUILayout.Width(140f)); } else { GUILayout.Label("Y 오프셋 (px)", GUILayout.Width(140f)); }
            Main.settings.TimingScaleOffsetY = GUILayout.HorizontalSlider(Main.settings.TimingScaleOffsetY, -200f, 200f, GUILayout.Width(240f));
            string yStr = GUILayout.TextField(Main.settings.TimingScaleOffsetY.ToString("0"), GUILayout.Width(60f));
            float yP;
            if (float.TryParse(yStr, out yP)) Main.settings.TimingScaleOffsetY = Mathf.Clamp(yP, -200f, 200f);
            GUILayout.EndHorizontal();
        }

        private static void DrawKeyViewerBody()
        {
            bool ko = Main.settings.language == "kr";

            // ---- Mode selector ----
            // "simple" = hardcoded Key10/12/16/20 layouts, "dmnote" = JSON preset.
            GUILayout.BeginHorizontal();
            GUILayout.Label(ko ? "모드" : "Mode", GUILayout.Width(80f));
            bool wasSimple = string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase);
            bool simpleSel = GUILayout.Toggle(wasSimple, ko ? "심플 (프리셋 없음)" : "Simple (no preset)", GUILayout.Width(220f));
            bool dmSel = GUILayout.Toggle(!wasSimple, ko ? "DM Note (고급)" : "DM Note (advanced)", GUILayout.Width(220f));
            if (simpleSel && !wasSimple) Main.settings.KeyViewerMode = "simple";
            else if (dmSel && wasSimple) Main.settings.KeyViewerMode = "dmnote";
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);

            // Mode-specific layout source. Shared rain/offset/scale controls render below.
            if (string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase))
            {
                DrawSimpleKeyViewerBody(ko);
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(ko ? "프리셋 가져오기 (DM Note JSON)" : "Import preset (DM Note JSON)", GUILayout.Width(350f)))
                {
                    KeyViewer.ImportKeyViewerPreset();
                }
                if (GUILayout.Button(ko ? "초기화" : "Clear", GUILayout.Width(100f)))
                {
                    Main.settings.keyViewerPresetJson = "";
                    KeyViewer.keyViewerKeys = null;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                string status;
                if (string.IsNullOrEmpty(Main.settings.keyViewerPresetJson)) status = ko ? "프리셋 없음" : "No preset loaded";
                else status = ko ? ("프리셋 로드됨 (" + Main.settings.keyViewerPresetJson.Length + " 문자)") : ("Preset loaded (" + Main.settings.keyViewerPresetJson.Length + " chars)");
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
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(ko ? "X 오프셋" : "X offset", GUILayout.Width(100f));
            Main.settings.KeyViewerOffsetX = GUILayout.HorizontalSlider(Main.settings.KeyViewerOffsetX, -2000f, 2000f, GUILayout.Width(240f));
            string xs = GUILayout.TextField(Main.settings.KeyViewerOffsetX.ToString("0"), GUILayout.Width(60f));
            float xp;
            if (float.TryParse(xs, out xp)) Main.settings.KeyViewerOffsetX = Mathf.Clamp(xp, -10000f, 10000f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(ko ? "Y 오프셋" : "Y offset", GUILayout.Width(100f));
            Main.settings.KeyViewerOffsetY = GUILayout.HorizontalSlider(Main.settings.KeyViewerOffsetY, -2000f, 2000f, GUILayout.Width(240f));
            string ys = GUILayout.TextField(Main.settings.KeyViewerOffsetY.ToString("0"), GUILayout.Width(60f));
            float yp;
            if (float.TryParse(ys, out yp)) Main.settings.KeyViewerOffsetY = Mathf.Clamp(yp, -10000f, 10000f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(ko ? "크기" : "Scale", GUILayout.Width(80f));
            Main.settings.KeyViewerScale = GUILayout.HorizontalSlider(Main.settings.KeyViewerScale, 0.2f, 4f, GUILayout.Width(240f));
            string ss = GUILayout.TextField(Main.settings.KeyViewerScale.ToString("0.##"), GUILayout.Width(60f));
            float sp;
            if (float.TryParse(ss, out sp)) Main.settings.KeyViewerScale = Mathf.Clamp(sp, 0.2f, 4f);
            GUILayout.EndHorizontal();

            DrawSubToggle(ref Main.settings.KeyViewerNoteEffect, ko ? "노트 비 효과" : "Note rain effect");
            DrawSubToggle(ref Main.settings.KeyViewerNoteReverse, ko ? "노트 반전 (아래로)" : "Reverse rain (downward)");
            DrawSubToggle(ref Main.settings.KeyViewerShowCounter, ko ? "카운터 표시" : "Show counter");

            GUILayout.BeginHorizontal();
            GUILayout.Label(ko ? "노트 속도" : "Note speed", GUILayout.Width(150f));
            Main.settings.KeyViewerNoteSpeed = GUILayout.HorizontalSlider(Main.settings.KeyViewerNoteSpeed, 10f, 1000f, GUILayout.Width(240f));
            string nss = GUILayout.TextField(Main.settings.KeyViewerNoteSpeed.ToString("0"), GUILayout.Width(60f));
            float nsp;
            if (float.TryParse(nss, out nsp)) Main.settings.KeyViewerNoteSpeed = Mathf.Clamp(nsp, 1f, 5000f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(ko ? "트랙 높이" : "Track height", GUILayout.Width(150f));
            Main.settings.KeyViewerTrackHeight = GUILayout.HorizontalSlider(Main.settings.KeyViewerTrackHeight, 0f, 1000f, GUILayout.Width(240f));
            string ths = GUILayout.TextField(Main.settings.KeyViewerTrackHeight.ToString("0"), GUILayout.Width(60f));
            float thp;
            if (float.TryParse(ths, out thp)) Main.settings.KeyViewerTrackHeight = Mathf.Clamp(thp, 0f, 5000f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(ko ? "페이드 (px)" : "Fade (px)", GUILayout.Width(150f));
            Main.settings.KeyViewerFadePx = GUILayout.HorizontalSlider(Main.settings.KeyViewerFadePx, 0f, 500f, GUILayout.Width(240f));
            string fps = GUILayout.TextField(Main.settings.KeyViewerFadePx.ToString("0"), GUILayout.Width(60f));
            float fpp;
            if (float.TryParse(fps, out fpp)) Main.settings.KeyViewerFadePx = Mathf.Clamp(fpp, 0f, 2000f);
            GUILayout.EndHorizontal();

            // ---- Shared Counters expandable (dmnote + simple) ----
            DrawKeyViewerCountersBody(ko);
        }

        // Buffered text for each key's count field so the user can type intermediate values
        // without the field bouncing back to the persisted number on every keystroke.
        private static readonly Dictionary<string, string> kvCountFieldBuffers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static string kvTotalCountBuffer;
        private static bool kvCountersExpanded;
        private static bool kvCountersConfirmReset;
        private static Vector2 kvCountersScroll;

        private static void DrawKeyViewerCountersBody(bool ko)
        {
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            kvCountersExpanded = GUILayout.Toggle(kvCountersExpanded, kvCountersExpanded ? "◢" : "▶", GUILayout.Width(20f));
            if (GUILayout.Button(ko ? "키 카운터 (수동 편집)" : "Key counters (manual edit)", GUI.skin.label))
                kvCountersExpanded = !kvCountersExpanded;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (!kvCountersExpanded) return;

            // ---- Total count field ----
            int totalNow = KeyViewer.GetKeyViewerTotal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            GUILayout.Label(ko ? "전체 합계" : "Total", GUILayout.Width(120f));
            string totalShown = (kvTotalCountBuffer != null) ? kvTotalCountBuffer : totalNow.ToString();
            string totalEdited = GUILayout.TextField(totalShown, GUILayout.Width(120f));
            if (totalEdited != totalShown) kvTotalCountBuffer = totalEdited;
            int totalParsed;
            if (int.TryParse(kvTotalCountBuffer ?? totalEdited, out totalParsed) && totalParsed != totalNow)
                KeyViewer.SetKeyViewerTotal(totalParsed);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            // ---- Per-key count fields ----
            List<KeyValuePair<string, int>> entries = KeyViewer.EnumerateKeyViewerCounters();
            if (entries.Count == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(18f);
                GUILayout.Label(ko ? "표시할 키가 없습니다 (프리셋이나 스타일을 먼저 로드하세요)" : "No keys to show — load a preset or pick a style first.");
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

            // ---- Reset button with confirm ----
            GUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            if (GUILayout.Button(ko ? "모든 카운트 초기화" : "Reset all counts", GUILayout.Width(200f)))
                kvCountersConfirmReset = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (kvCountersConfirmReset)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                GUILayout.Label("<color=red>" + (ko ? "정말 초기화할까요?" : "Really reset every count?") + "</color>");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                if (GUILayout.Button(ko ? "확인" : "Confirm", GUILayout.Width(100f)))
                {
                    KeyViewer.ResetAllKeyViewerCounters();
                    kvCountFieldBuffers.Clear();
                    kvTotalCountBuffer = null;
                    kvCountersConfirmReset = false;
                }
                if (GUILayout.Button(ko ? "취소" : "Cancel", GUILayout.Width(100f)))
                    kvCountersConfirmReset = false;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        // ---------- Simple-mode UI state (transient; not saved) ----------
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
        private static string simpleSpeedStr, simpleHeightStr, simpleSizeStr, simpleYLocationStr;
        private static int simplePrevStyle = -1;

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
            // Defer to the shared API so we wipe both PlayerPrefs and the in-memory
            // totals counter. Calling DeleteKey alone left keyViewerTotalPresses stale.
            KeyViewer.ResetAllKeyViewerCounters();
        }

        private static void DrawSimpleKeyViewerBody(bool ko)
        {
            int style = Mathf.Clamp(Main.settings.KeyViewerSimpleStyle, 0, 3);
            if (Main.settings.KeyViewerSimpleDownLocation && Mathf.Abs(Main.settings.KeyViewerSimpleYLocation - 200f) < 0.001f)
            {
                Main.settings.KeyViewerSimpleYLocation = 0f;
                Main.settings.KeyViewerSimpleDownLocation = false;
            }

            // ----- Top toggles & sliders -----
            DrawSubToggle(ref simpleKeyShare, ko ? "키 공유 (스타일 변경 시 키 복사)" : "Key share (copy keys when changing style)");

            float oldY = Main.settings.KeyViewerSimpleYLocation;
            DrawSubFloat(ref Main.settings.KeyViewerSimpleYLocation, ref simpleYLocationStr, ko ? "Y 위치" : "Y location", 0f, SimpleMaxYForStyle(style));
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            if (GUILayout.Button(ko ? "초기화" : "Reset", GUILayout.Width(90f)))
            {
                Main.settings.KeyViewerSimpleYLocation = 200f;
                simpleYLocationStr = null;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (Mathf.Abs(oldY - Main.settings.KeyViewerSimpleYLocation) > 0.001f)
                KeyViewer.keyViewerKeys = null;

            // Reset count with confirm dialog (mirrors Jipper's UX).
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            if (GUILayout.Button(ko ? "카운트 초기화" : "Reset count", GUILayout.Width(180f)))
                simpleConfirmReset = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (simpleConfirmReset)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                GUILayout.Label("<color=red>" + (ko ? "정말 모든 키 카운트를 초기화할까요?" : "Really reset all key counts?") + "</color>");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                if (GUILayout.Button(ko ? "확인" : "Confirm", GUILayout.Width(100f)))
                {
                    SimpleResetCounts();
                    simpleConfirmReset = false;
                }
                if (GUILayout.Button(ko ? "취소" : "Cancel", GUILayout.Width(100f)))
                    simpleConfirmReset = false;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            DrawSubToggle(ref Main.settings.KeyViewerSimpleUseRain, ko ? "비 효과 사용" : "Enable rain effect");
            if (Main.settings.KeyViewerSimpleUseRain)
                DrawSubToggle(ref Main.settings.KeyViewerSimpleUseGhostRain, ko ? "고스트 비 사용" : "Enable ghost rain");
            DrawSubFloat(ref Main.settings.KeyViewerSimpleRainSpeed, ref simpleSpeedStr, ko ? "비 속도" : "Rain speed", 1f, 800f);
            DrawSubFloat(ref Main.settings.KeyViewerSimpleRainHeight, ref simpleHeightStr, ko ? "비 높이" : "Rain height", 1f, 1000f);

            // Style picker (Key10/12/16/20). When KeyShare is on, copy current keys/text into
            // the new style up to the shorter of the two arrays — Jipper's "keyShare" behavior.
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(ko ? "스타일" : "Style", GUILayout.Width(80f));
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
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            simplePrevStyle = Main.settings.KeyViewerSimpleStyle;
            style = Mathf.Clamp(Main.settings.KeyViewerSimpleStyle, 0, 3);

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(ko ? "발 스타일" : "Foot style", GUILayout.Width(80f));
            string[] footNames = { "None", "Key2", "Key4", "Key6", "Key8", "Key16" };
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
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawSubFloat(ref Main.settings.KeyViewerSimpleSize, ref simpleSizeStr, ko ? "크기" : "Size", 0f, 2f);

            int slotCount = SimpleSlotCount(style);
            int[] codes = SimpleCodes(style);
            string[] texts = SimpleTexts(style);
            int[] footCodes = SimpleFootCodes(Mathf.Clamp(Main.settings.KeyViewerSimpleFootStyle, 0, 5));
            int[] ghostCodes = SimpleGhostCodes(style);

            // ----- Key change expandable (rebind keys per slot) -----
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            simpleKeyChangeExpanded = GUILayout.Toggle(simpleKeyChangeExpanded, simpleKeyChangeExpanded ? "◢" : "▶", GUILayout.Width(20f));
            if (GUILayout.Button(ko ? "키 변경" : "Key change", GUI.skin.label)) simpleKeyChangeExpanded = !simpleKeyChangeExpanded;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (simpleKeyChangeExpanded)
            {
                DrawSimpleSlotButtons(ko, slotCount, codes, texts, false, 0, false);
                if (footCodes != null && footCodes.Length > 0)
                    DrawSimpleSlotButtons(ko, footCodes.Length, footCodes, null, false, SimpleFootSlotBase, false);
                if (simpleSelectedSlot >= 0 && !simpleSelectedTextEdit)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(28f);
                    GUILayout.Label("<b>" + (ko ? "키 입력 대기 중…" : "Press a key…") + "</b>");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    // Listen for any keypress and assign to the slot. Event-driven via
                    // OnGUI's Event.current; works without a separate input thread.
                    Event ev = Event.current;
                    if (ev != null && ev.isKey && ev.type == EventType.KeyDown && ev.keyCode != KeyCode.None)
                    {
                        ApplySimpleCapturedKey(ev.keyCode, codes, footCodes, ghostCodes);
                        ev.Use();
                    }
                    else if (Input.anyKeyDown)
                    {
                        foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
                        {
                            if (Input.GetKeyDown(kc) && kc != KeyCode.None)
                            {
                                ApplySimpleCapturedKey(kc, codes, footCodes, ghostCodes);
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
                simpleGhostRainChangeExpanded = GUILayout.Toggle(simpleGhostRainChangeExpanded, simpleGhostRainChangeExpanded ? "◢" : "▶", GUILayout.Width(20f));
                if (GUILayout.Button(ko ? "고스트 비 키 변경" : "Ghost rain key change", GUI.skin.label)) simpleGhostRainChangeExpanded = !simpleGhostRainChangeExpanded;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if (simpleGhostRainChangeExpanded)
                    DrawSimpleSlotButtons(ko, slotCount, ghostCodes, null, false, SimpleGhostSlotBase, true);
            }

            if (simpleSelectedSlot >= SimpleGhostSlotBase && !simpleSelectedTextEdit)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                GUILayout.Label("<b>" + (ko ? "고스트 비 키 입력 대기 중…" : "Press a ghost rain key…") + "</b>");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                HandleSimpleKeyCapture(codes, footCodes, ghostCodes);
            }

            // ----- Text change expandable -----
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            simpleTextChangeExpanded = GUILayout.Toggle(simpleTextChangeExpanded, simpleTextChangeExpanded ? "◢" : "▶", GUILayout.Width(20f));
            if (GUILayout.Button(ko ? "텍스트 변경" : "Text change", GUI.skin.label)) simpleTextChangeExpanded = !simpleTextChangeExpanded;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (simpleTextChangeExpanded)
            {
                DrawSimpleSlotButtons(ko, slotCount, codes, texts, true, 0, false);
                if (simpleSelectedSlot >= 0 && simpleSelectedTextEdit && simpleSelectedSlot < texts.Length)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(28f);
                    GUILayout.Label(ko ? "표시 텍스트:" : "Display text:", GUILayout.Width(110f));
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
                    if (GUILayout.Button(ko ? "초기화" : "Reset", GUILayout.Width(100f)))
                    {
                        texts[simpleSelectedSlot] = null;
                        simpleSelectedSlot = -1;
                        KeyViewer.keyViewerKeys = null;
                    }
                    if (GUILayout.Button(ko ? "저장" : "Save", GUILayout.Width(100f)))
                        simpleSelectedSlot = -1;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }

            // ----- Color expandable (9 slots, 3rd rain shown only on Key20) -----
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            simpleColorExpanded = GUILayout.Toggle(simpleColorExpanded, simpleColorExpanded ? "◢" : "▶", GUILayout.Width(20f));
            if (GUILayout.Button(ko ? "색상" : "Color", GUI.skin.label)) simpleColorExpanded = !simpleColorExpanded;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (simpleColorExpanded)
            {
                DrawSimpleColorRow(ref Main.settings.SKvBgR, ref Main.settings.SKvBgG, ref Main.settings.SKvBgB, ref Main.settings.SKvBgA, ko ? "배경" : "Background", "skvBg");
                DrawSimpleColorRow(ref Main.settings.SKvBgcR, ref Main.settings.SKvBgcG, ref Main.settings.SKvBgcB, ref Main.settings.SKvBgcA, ko ? "배경 (눌림)" : "Background (clicked)", "skvBgc");
                DrawSimpleColorRow(ref Main.settings.SKvOutR, ref Main.settings.SKvOutG, ref Main.settings.SKvOutB, ref Main.settings.SKvOutA, ko ? "테두리" : "Outline", "skvOut");
                DrawSimpleColorRow(ref Main.settings.SKvOutcR, ref Main.settings.SKvOutcG, ref Main.settings.SKvOutcB, ref Main.settings.SKvOutcA, ko ? "테두리 (눌림)" : "Outline (clicked)", "skvOutc");
                DrawSimpleColorRow(ref Main.settings.SKvTxtR, ref Main.settings.SKvTxtG, ref Main.settings.SKvTxtB, ref Main.settings.SKvTxtA, ko ? "글자" : "Text", "skvTxt");
                DrawSimpleColorRow(ref Main.settings.SKvTxtcR, ref Main.settings.SKvTxtcG, ref Main.settings.SKvTxtcB, ref Main.settings.SKvTxtcA, ko ? "글자 (눌림)" : "Text (clicked)", "skvTxtc");
                DrawSimpleColorRow(ref Main.settings.SKvRainR, ref Main.settings.SKvRainG, ref Main.settings.SKvRainB, ref Main.settings.SKvRainA, ko ? "비 색상" : "Rain color", "skvRain");
                DrawSimpleColorRow(ref Main.settings.SKvRain2R, ref Main.settings.SKvRain2G, ref Main.settings.SKvRain2B, ref Main.settings.SKvRain2A, ko ? "비 색상 2" : "Rain color 2", "skvRain2");
                if (style == 3)
                    DrawSimpleColorRow(ref Main.settings.SKvRain3R, ref Main.settings.SKvRain3G, ref Main.settings.SKvRain3B, ref Main.settings.SKvRain3A, ko ? "비 색상 3" : "Rain color 3", "skvRain3");
            }
        }

        // Render slot buttons for Key change / Text change. The button label is the current
        // KeyCode short name (or the text override if shown in text-change mode). Click = select.
        private static void HandleSimpleKeyCapture(int[] handCodes, int[] footCodes, int[] ghostCodes)
        {
            Event ev = Event.current;
            if (ev != null && ev.isKey && ev.type == EventType.KeyDown && ev.keyCode != KeyCode.None)
            {
                ApplySimpleCapturedKey(ev.keyCode, handCodes, footCodes, ghostCodes);
                ev.Use();
                return;
            }

            if (!Input.anyKeyDown) return;
            foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
            {
                if (!Input.GetKeyDown(kc) || kc == KeyCode.None) continue;
                ApplySimpleCapturedKey(kc, handCodes, footCodes, ghostCodes);
                break;
            }
        }

        private static void ApplySimpleCapturedKey(KeyCode keyCode, int[] handCodes, int[] footCodes, int[] ghostCodes)
        {
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
        }

        private static void DrawSimpleSlotButtons(bool ko, int slotCount, int[] codes, string[] texts,
                                                  bool textMode, int slotBase, bool clearNonNoneOnClick)
        {
            GUIStyle btn = new GUIStyle(GUI.skin.button) { fixedWidth = 56f, fixedHeight = 24f };
            int perRow = 8;
            for (int row = 0; row < (slotCount + perRow - 1) / perRow; row++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                for (int col = 0; col < perRow; col++)
                {
                    int slot = row * perRow + col;
                    if (slot >= slotCount) break;
                    string label = textMode
                        ? (texts != null && slot < texts.Length && !string.IsNullOrEmpty(texts[slot]) ? texts[slot] : SimpleKeyShortLabel((KeyCode)codes[slot]))
                        : SimpleKeyShortLabel((KeyCode)codes[slot]);
                    int slotId = slotBase + slot;
                    if (slotId == simpleSelectedSlot && textMode == simpleSelectedTextEdit) label = "<b>" + label + "</b>";
                    if (GUILayout.Button(label, btn))
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
            bool ko = Main.settings.language == "kr";
            bool prevOtto = Main.settings.ChangeOttoIcon;
            bool prevBall = Main.settings.ChangeBallColor;
            bool prevTile = Main.settings.ChangeTileColor;

            DrawSubToggle(ref Main.settings.ChangeOttoIcon, ko ? "오토 (자동 모드) 아이콘 변경" : "Change Otto (auto-mode) icon");
            DrawSubToggle(ref Main.settings.ChangeBallColor, ko ? "공 색상 변경" : "Change ball color");
            DrawSubToggle(ref Main.settings.ChangeTileColor, ko ? "비트 타일 색상" : "Beat tile color");

            if (prevOtto != Main.settings.ChangeOttoIcon)
            {
                if (Main.settings.ChangeOttoIcon) ResourceChanger.RefreshOttoIcon();
                else ResourceChanger.RestoreOttoIcon();
            }
            if (prevBall != Main.settings.ChangeBallColor)
            {
                if (Main.settings.ChangeBallColor) ResourceChanger.RefreshPlanetColors();
                else ResourceChanger.RestorePlanetColors();
            }
            if (prevTile != Main.settings.ChangeTileColor)
            {
                if (Main.settings.ChangeTileColor) ResourceChanger.RefreshTileColors();
                else ResourceChanger.RestoreTileColors();
            }

            if (Main.settings.ChangeOttoIcon)
            {
                DrawResourceColor(ref Main.settings.OttoR, ref Main.settings.OttoG, ref Main.settings.OttoB, ref Main.settings.OttoA, ko ? "오토 색상" : "Otto color", "otto", ResourceChanger.RefreshOttoIcon);
                DrawOttoOffsetRow(ref Main.settings.OttoOffsetX, ko ? "오토 X 오프셋" : "Otto X offset");
                DrawOttoOffsetRow(ref Main.settings.OttoOffsetY, ko ? "오토 Y 오프셋" : "Otto Y offset");
                ResourceChanger.RefreshOttoIcon();
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
                    ko ? "1번 공 색상" : "Planet 1 color",
                    ko ? "1번 공 불투명도" : "Planet 1 ball opacity",
                    ko ? "1번 꼬리 불투명도" : "Planet 1 tail opacity",
                    "resourceBall1"
                );
                DrawBallPlanetResource(
                    ref Main.settings.BallPlanet2R,
                    ref Main.settings.BallPlanet2G,
                    ref Main.settings.BallPlanet2B,
                    ref Main.settings.BallPlanet2Opacity,
                    ref Main.settings.TailPlanet2Opacity,
                    ko ? "2번 공 색상" : "Planet 2 color",
                    ko ? "2번 공 불투명도" : "Planet 2 ball opacity",
                    ko ? "2번 꼬리 불투명도" : "Planet 2 tail opacity",
                    "resourceBall2"
                );
                DrawBallPlanetResource(
                    ref Main.settings.BallPlanet3R,
                    ref Main.settings.BallPlanet3G,
                    ref Main.settings.BallPlanet3B,
                    ref Main.settings.BallPlanet3Opacity,
                    ref Main.settings.TailPlanet3Opacity,
                    ko ? "3번 공 색상" : "Planet 3 color",
                    ko ? "3번 공 불투명도" : "Planet 3 ball opacity",
                    ko ? "3번 꼬리 불투명도" : "Planet 3 tail opacity",
                    "resourceBall3"
                );
            }

            if (Main.settings.ChangeTileColor)
                DrawResourceColor(ref Main.settings.TileR, ref Main.settings.TileG, ref Main.settings.TileB, ref Main.settings.TileA, ko ? "비트 타일 색상" : "Beat tile color", "resourceTile", ResourceChanger.RefreshTileColors);
        }

        private static void DrawTweaksBody()
        {
            bool ko = Main.settings.language == "kr";
            bool prevRemoveCheckpoints = Main.settings.RemoveAllCheckpoints;
            bool prevRemoveBallCoreParticles = Main.settings.RemoveBallCoreParticles;
            bool prevDisableTileHitGlow = Main.settings.DisableTileHitGlow;
            DrawSubToggle(ref Main.settings.RemoveAllCheckpoints, ko ? "모든 체크포인트 제거" : "Remove all checkpoints");
            DrawSubToggle(ref Main.settings.RemoveBallCoreParticles, ko ? "공 내부 효과 제거" : "Remove ball inner effects");
            DrawSubToggle(ref Main.settings.DisableTileHitGlow, ko ? "타일 히트 발광 제거" : "Disable tile hit glow");
            if (prevRemoveCheckpoints != Main.settings.RemoveAllCheckpoints)
                Tweaks.RefreshCheckpointTweak();
            if (prevRemoveBallCoreParticles != Main.settings.RemoveBallCoreParticles)
                Tweaks.RefreshBallCoreParticlesTweak();
            if (prevDisableTileHitGlow != Main.settings.DisableTileHitGlow)
                Tweaks.RefreshTileHitGlowTweak();
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
            DrawResourceColorRgb(ref r, ref g, ref b, colorName, key + ":color", ResourceChanger.RefreshPlanetColors);
            DrawResourceOpacity(ref ballOpacity, ballOpacityName, ResourceChanger.RefreshPlanetColors);
            DrawResourceOpacity(ref tailOpacity, tailOpacityName, ResourceChanger.RefreshPlanetColors);
        }

        private static void DrawKCBBody()
        {
            bool ko = Main.settings.language == "kr";
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(ko ? "임계값 (ms)" : "Threshold (ms)", GUILayout.Width(180f));
            Main.settings.KCBThresholdMs = GUILayout.HorizontalSlider(Main.settings.KCBThresholdMs, 0f, 1000f, GUILayout.Width(180f));
            string s = GUILayout.TextField(Main.settings.KCBThresholdMs.ToString("0"), GUILayout.Width(50f));
            float p;
            if (float.TryParse(s, out p)) Main.settings.KCBThresholdMs = Mathf.Max(0f, p);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void DrawOttoOffsetRow(ref float val, string name)
        {
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

        // AdofaiTweaks-style: one capture toggle. While armed, the next pressed key is
        // added to the active list (or removed if it's already in the list). Existing keys
        // render as buttons — click any one to remove it directly.
        // Internal so the KeyLimiter patch can bypass its own filter while the user is
        // actively binding a key — otherwise pressing a not-yet-allowed modifier (Shift,
        // Ctrl) would be swallowed by the same patch and never reach the capture poll.
        internal static bool keyLimiterCapturing;
        private static void DrawKeyLimiterBody()
        {
            bool ko = Main.settings.language == "kr";
            int[] arr = Main.settings.KeyLimiterAllowed ?? new int[0];

            // Capture: toggle armed by pressing "Add key". Next KeyDown mutates the list.
            // IMGUI's KeyDown event reports keyCode=None for bare modifier presses (Shift,
            // Ctrl, Alt, Cmd) — those go through Event.modifiers instead. To capture them
            // reliably we additionally poll Input.GetKeyDown for the known modifier set
            // during Repaint passes.
            if (keyLimiterCapturing)
            {
                int captured = (int)KeyCode.None;
                Event e = Event.current;
                // Always swallow KeyDown / KeyUp while capturing — otherwise pressing Shift,
                // Tab, Space etc. propagates into IMGUI and collapses the surrounding toggle
                // (the GUILayout.Toggle for "Key Limiter on" treats Space as an activation).
                if (e != null && (e.type == EventType.KeyDown || e.type == EventType.KeyUp))
                {
                    if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
                        captured = (int)e.keyCode;
                    e.Use();
                }
                else if (e != null && e.type == EventType.Repaint)
                {
                    // Bare modifier KeyDowns arrive with keyCode == None in IMGUI. Poll Input
                    // directly during Repaint to capture them. Our KeyLimiter patch bypasses
                    // its own filter while capturing, so these polls aren't suppressed.
                    KeyCode[] modifiers = {
                        KeyCode.LeftShift, KeyCode.RightShift,
                        KeyCode.LeftControl, KeyCode.RightControl,
                        KeyCode.LeftAlt, KeyCode.RightAlt,
                        KeyCode.LeftCommand, KeyCode.RightCommand,
                    };
                    for (int i = 0; i < modifiers.Length; i++)
                    {
                        if (Input.GetKeyDown(modifiers[i])) { captured = (int)modifiers[i]; break; }
                    }
                }

                if (captured != (int)KeyCode.None)
                {
                    int existing = -1;
                    for (int i = 0; i < arr.Length; i++) { if (arr[i] == captured) { existing = i; break; } }
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
                    // Stay armed: user can keep pressing keys to add/remove until they
                    // click the toggle again (matches AdofaiTweaks' continuous-capture UX).
                }
            }

            arr = Main.settings.KeyLimiterAllowed ?? new int[0];

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(ko ? "활성 키" : "Active keys", GUILayout.Width(120f));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            int perRow = 6;
            int rows = Mathf.Max(1, (arr.Length + perRow - 1) / perRow);
            for (int row = 0; row < rows; row++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(28f);
                for (int col = 0; col < perRow; col++)
                {
                    int slot = row * perRow + col;
                    if (slot >= arr.Length) break;
                    KeyCode kc = (KeyCode)arr[slot];
                    if (GUILayout.Button(kc.ToString(), GUILayout.Width(110f)))
                    {
                        // Remove on click — same UX as AdofaiTweaks.
                        int[] shrunk = new int[arr.Length - 1];
                        Array.Copy(arr, 0, shrunk, 0, slot);
                        Array.Copy(arr, slot + 1, shrunk, slot, arr.Length - slot - 1);
                        Main.settings.KeyLimiterAllowed = shrunk;
                        break;
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            string addLabel = keyLimiterCapturing
                ? (ko ? "키를 누르세요..." : "Press any key...")
                : (ko ? "키 추가" : "Add key");
            if (GUILayout.Button(addLabel, GUILayout.Width(180f)))
                keyLimiterCapturing = !keyLimiterCapturing;
            if (arr.Length > 0 && GUILayout.Button(ko ? "모두 지우기" : "Clear all", GUILayout.Width(120f)))
                Main.settings.KeyLimiterAllowed = new int[0];
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(ko
                ? "활성 키 목록에 있는 키만 입력으로 등록됩니다. 키를 클릭하면 제거됩니다."
                : "Only the keys above register as input. Click any key to remove it.");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static string jrestrictAccBuf;
        private static void DrawJRestrictBody()
        {
            bool ko = Main.settings.language == "kr";
            // Hide the XPure Perfect mode (index 2) entirely when XPerfect isn't installed —
            // there's no sensible way to satisfy it without the source-of-truth enum.
            bool xpAvail = XPerfectBridge.Installed;
            int[] modeIndices = xpAvail ? new[] { 0, 4, 1, 2, 3 } : new[] { 0, 4, 1, 3 };
            string[] modeLabels = ko
                ? new[] { "정확도 임계값", "퓨어 퍼펙트만", "X-퓨어 퍼펙트만", "사용자 지정", "노 미스" }
                : new[] { "% accuracy", "Pure Perfect only", "XPure Perfect only", "Custom", "No Miss" };
            // If user previously selected mode 2 but XPerfect was uninstalled, fall back to mode 1.
            if (!xpAvail && Main.settings.JRestrictMode == 2) Main.settings.JRestrictMode = 1;
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(ko ? "모드" : "Mode", GUILayout.Width(80f));
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
                GUILayout.Label(ko ? "최소 정확도 (%)" : "Min accuracy (%)", GUILayout.Width(180f));
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
                // Custom bitmask. Show toggles for each HitMargin the player can hit.
                string[] names = ko
                    ? new[] { "너무 빠름", "매우 빠름", "이른 퍼펙트", "퍼펙트", "늦은 퍼펙트", "매우 늦음", "너무 늦음" }
                    : new[] { "Too Early", "Very Early", "Early Perfect", "Perfect", "Late Perfect", "Very Late", "Too Late" };
                GUILayout.BeginHorizontal();
                GUILayout.Space(14f);
                GUILayout.Label(ko ? "허용된 판정" : "Allowed judgements:");
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

        internal static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Main.settings.Save(modEntry);
        }
    }
}
