using System;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static string bpmColorMaxStr;
        private static string comboColorMaxStr;
        private static string judgementPositionYStr;
        private static string perfectComboStr;
        private static string holdOffsetXStr;
        private static string holdOffsetYStr;
        private static string comboYStr;

        private static GUIStyle expandStyle;
        private static GUIStyle enableStyle;

        /// <summary>
        /// Names shown in the Font dropdown. When a TMP AssetBundle is loaded, these are the bundle font keys
        /// (TMP asset names without " SDF" etc.); otherwise TTF names from the mod Fonts/ folder (IMGUI path).
        /// </summary>
        private static List<string> GetHudFontChoices()
        {
            EnsureBundleLoaded();
            if (BundleAvailable && bundleFonts.Count > 0)
            {
                var list = new List<string>(bundleFonts.Keys);
                list.Sort(StringComparer.OrdinalIgnoreCase);
                return list;
            }

            EnsureBundledFontsLoaded();
            if (bundledFontNames != null && bundledFontNames.Count > 0)
                return new List<string>(bundledFontNames);
            return new List<string>();
        }

        private static readonly Dictionary<string, string> colorBuffers = new Dictionary<string, string>();
        private static readonly HashSet<string> colorExpanded = new HashSet<string>();

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            DrawUpdatePopup(modEntry);
            GUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            if (settings.language == "en"){GUILayout.Label("Size", GUILayout.Width(60f));} else {GUILayout.Label("크기", GUILayout.Width(60f));}
            settings.size = GUILayout.HorizontalSlider(settings.size, 0.5f, 2.0f, GUILayout.Width(240f));
            string sizeStr = GUILayout.TextField(settings.size.ToString("0.##"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(sizeStr, out parsed)) settings.size = Mathf.Clamp(parsed, 0.5f, 2.0f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (settings.language == "en"){GUILayout.Label("Language", GUILayout.Width(100f));} else {GUILayout.Label("언어", GUILayout.Width(60f));}
            if (GUILayout.Button("English", GUILayout.Width(100f)))
            {
                settings.language = "en";
            }
            if (GUILayout.Button("한국어", GUILayout.Width(100f)))
            {
                settings.language = "kr";
            }
            GUILayout.EndHorizontal();

            List<string> fontChoices = GetHudFontChoices();
            if (fontChoices.Count > 0 && string.IsNullOrEmpty(settings.fontName))
            {
                settings.fontName = fontChoices[0];
                preferredHudFont = null;
                InvalidateOverlayFontCache();
            }
            else if (BundleAvailable && fontChoices.Count > 0 && !string.IsNullOrEmpty(settings.fontName))
            {
                if (!bundleFonts.ContainsKey(settings.fontName))
                {
                    settings.fontName = fontChoices[0];
                    preferredHudFont = null;
                    InvalidateOverlayFontCache();
                }
            }

            GUILayout.BeginHorizontal();
            if (settings.language == "en") { GUILayout.Label("Font", GUILayout.Width(100f)); } else { GUILayout.Label("폰트", GUILayout.Width(60f)); }
            string current = string.IsNullOrEmpty(settings.fontName) ? "—" : settings.fontName;
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
                    bool selected = string.Equals(settings.fontName, name, StringComparison.OrdinalIgnoreCase);
                    string label = selected ? "● " + name : "○ " + name;
                    if (GUILayout.Button(label, GUI.skin.label, GUILayout.ExpandWidth(false)))
                    {
                        settings.fontName = name;
                        preferredHudFont = null;
                        InvalidateOverlayFontCache();
                        fontDropdownOpen = false;
                    }
                }
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            if (settings.language == "en")
            {
                DrawExpandable(ref settings.progressBarOn, ref settings.progressBarExpanded, "ProgressBar", DrawProgressBarBody);
                DrawExpandable(ref settings.statusOn, ref settings.statusExpanded, "Status", DrawStatusBody);
                DrawExpandable(ref settings.bpmOn, ref settings.bpmExpanded, "BPM", DrawBpmBody);
                DrawExpandable(ref settings.comboOn, ref settings.comboExpanded, "Combo", DrawComboBody);
                DrawExpandable(ref settings.judgementOn, ref settings.judgementExpanded, "Judgement", DrawJudgementBody);
                DrawExpandable(ref settings.holdOn, ref settings.holdExpanded, "Hold", DrawHoldBody);
                DrawExpandable(ref settings.attemptOn, ref settings.attemptExpanded, "Attempt", DrawAttemptBody);
                DrawExpandable(ref settings.timingScaleOn, ref settings.timingScaleExpanded, "TimingScale", DrawTimingScaleBody);
                DrawExpandable(ref settings.keyViewerOn, ref settings.keyViewerExpanded, "KeyViewer", DrawKeyViewerBody);
                DrawExpandable(ref settings.ResourceChangerOn, ref settings.ResourceChangerExpanded, "Resource Changer", DrawResourceChangerBody);
            } else
            {
                DrawExpandable(ref settings.progressBarOn, ref settings.progressBarExpanded, "프로그레스바", DrawProgressBarBody);
                DrawExpandable(ref settings.statusOn, ref settings.statusExpanded, "표시 설정", DrawStatusBody);
                DrawExpandable(ref settings.bpmOn, ref settings.bpmExpanded, "브픔", DrawBpmBody);
                DrawExpandable(ref settings.comboOn, ref settings.comboExpanded, "콤보", DrawComboBody);
                DrawExpandable(ref settings.judgementOn, ref settings.judgementExpanded, "판정", DrawJudgementBody);
                DrawExpandable(ref settings.holdOn, ref settings.holdExpanded, "홀드", DrawHoldBody);
                DrawExpandable(ref settings.attemptOn, ref settings.attemptExpanded, "시도", DrawAttemptBody);
                DrawExpandable(ref settings.timingScaleOn, ref settings.timingScaleExpanded, "타이밍 스케일", DrawTimingScaleBody);
                DrawExpandable(ref settings.keyViewerOn, ref settings.keyViewerExpanded, "키뷰어", DrawKeyViewerBody);
                DrawExpandable(ref settings.ResourceChangerOn, ref settings.ResourceChangerExpanded, "리소스 체인저", DrawResourceChangerBody);
            }
            GUILayout.EndVertical();
        }

        private static void DrawStatusBody()
        {
            if (settings.language == "en")
            {
                DrawSubToggle(ref settings.ShowProgress, "Show progress");
                DrawSubToggle(ref settings.ShowAccuracy, "Show accuracy");
                DrawSubToggle(ref settings.ShowXAccuracy, "Show X-accuracy");
                DrawSubToggle(ref settings.ShowMusicTime, "Show music/map time");
                DrawSubToggle(ref settings.ShowCheckpoint, "Show checkpoint");
                DrawSubToggle(ref settings.ShowBest, "Show best");
                DrawSubToggle(ref settings.ShowFPS, "Show FPS");
                DrawDecimalPlacesRow("Decimal places");
            } else
            {
                DrawSubToggle(ref settings.ShowProgress, "프로그레스 퍼센트 표시");
                DrawSubToggle(ref settings.ShowAccuracy, "정확도 표시");
                DrawSubToggle(ref settings.ShowXAccuracy, "절대 정확도 표시");
                DrawSubToggle(ref settings.ShowMusicTime, "음악/맵 시간 표시");
                DrawSubToggle(ref settings.ShowCheckpoint, "체크포인트 표시");
                DrawSubToggle(ref settings.ShowBest, "최고 표시");
                DrawSubToggle(ref settings.ShowFPS, "프레임 표시");
                DrawDecimalPlacesRow("소수점 자리수");
            }
        }

        private static string decimalPlacesBuf;
        private static void DrawDecimalPlacesRow(string label)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            GUILayout.Label(label, GUILayout.Width(180f));
            float slid = GUILayout.HorizontalSlider(settings.DecimalPlaces, 0f, 6f, GUILayout.Width(180f));
            int slidI = Mathf.RoundToInt(slid);
            if (slidI != settings.DecimalPlaces)
            {
                settings.DecimalPlaces = Mathf.Clamp(slidI, 0, 6);
                decimalPlacesBuf = settings.DecimalPlaces.ToString();
            }
            decimalPlacesBuf = GUILayout.TextField(decimalPlacesBuf ?? settings.DecimalPlaces.ToString(), GUILayout.Width(40f));
            int parsed;
            if (int.TryParse(decimalPlacesBuf, out parsed)) settings.DecimalPlaces = Mathf.Clamp(parsed, 0, 6);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void DrawProgressBarBody()
        {
            if (settings.language == "en")
            {
                DrawSubColor(ref settings.ProgressBarFillR, ref settings.ProgressBarFillG, ref settings.ProgressBarFillB, ref settings.ProgressBarFillA, "Fill color", "pbFill");
                DrawSubColor(ref settings.ProgressBarBackR, ref settings.ProgressBarBackG, ref settings.ProgressBarBackB, ref settings.ProgressBarBackA, "Background color", "pbBack");
                DrawSubColor(ref settings.ProgressBarBorderR, ref settings.ProgressBarBorderG, ref settings.ProgressBarBorderB, ref settings.ProgressBarBorderA, "Border color", "pbBorder");
            } else
            {
                DrawSubColor(ref settings.ProgressBarFillR, ref settings.ProgressBarFillG, ref settings.ProgressBarFillB, ref settings.ProgressBarFillA, "채움 색상", "pbFill");
                DrawSubColor(ref settings.ProgressBarBackR, ref settings.ProgressBarBackG, ref settings.ProgressBarBackB, ref settings.ProgressBarBackA, "배경 색상", "pbBack");
                DrawSubColor(ref settings.ProgressBarBorderR, ref settings.ProgressBarBorderG, ref settings.ProgressBarBorderB, ref settings.ProgressBarBorderA, "테두리 색상", "pbBorder");
            }
        }

        private static void DrawBpmBody()
        {
            if (settings.language == "en") {DrawSubFloat(ref settings.BpmColorMax, ref bpmColorMaxStr, "BPM color max", 0f, 100000f);} else {DrawSubFloat(ref settings.BpmColorMax, ref bpmColorMaxStr, "최대 브픔 색깔", 0f, 100000f);}
            DrawSubColor(ref settings.BpmColorLowR, ref settings.BpmColorLowG, ref settings.BpmColorLowB, ref settings.BpmColorLowA, "0%", "bpmLow");
            DrawSubColor(ref settings.BpmColorHighR, ref settings.BpmColorHighG, ref settings.BpmColorHighB, ref settings.BpmColorHighA, "100%", "bpmHigh");
        }

        private static void DrawComboBody()
        {
            if (settings.language == "en") {DrawSubToggle(ref settings.EnableAutoCombo, "Enable auto combo");} else {DrawSubToggle(ref settings.EnableAutoCombo, "오토 콤보");}
            if (XPerfectBridge.Installed)
            {
                if (settings.language == "en") {DrawSubToggle(ref settings.XPerfectComboEnabled, "XPerfect-only combo (break on +/-Perfect)");} else {DrawSubToggle(ref settings.XPerfectComboEnabled, "XPerfect 전용 콤보 (+/-Perfect에서 끊김)");}
            }
            if (settings.language == "en") {DrawSubInt(ref settings.ComboColorMax, ref comboColorMaxStr, "Combo color max", 0, 1000000);} else {DrawSubInt(ref settings.ComboColorMax, ref comboColorMaxStr, "최대 콤보 색깔", 0, 1000000);}
            DrawSubColor(ref settings.ComboColorLowR, ref settings.ComboColorLowG, ref settings.ComboColorLowB, ref settings.ComboColorLowA, "0%", "comboLow");
            DrawSubColor(ref settings.ComboColorHighR, ref settings.ComboColorHighG, ref settings.ComboColorHighB, ref settings.ComboColorHighA, "100%", "comboHigh");
            if (settings.language == "en") {DrawSubToggle(ref settings.ComboMoveUpNoCaption, "Move up when no title/artist");} else {DrawSubToggle(ref settings.ComboMoveUpNoCaption, "제목/작가가 없을 때 위로 올리기");}
            if (settings.language == "en") {DrawExpandable(ref settings.CaptionText, ref settings.captionExpanded, "Show Perfect Combo Text", DrawPerfectComboExpanded);} else {DrawExpandable(ref settings.CaptionText, ref settings.captionExpanded, "Perfect Combo 글자 표시", DrawPerfectComboExpanded);}
            if (settings.language == "en") {DrawSubToggle(ref settings.comboFastAnim, "Make Animation More Snappy");} else {{DrawSubToggle(ref settings.comboFastAnim, "콤보 에니매이션 더 빠르게 하기");}}
            GUILayout.BeginHorizontal();
            if (settings.language == "en"){GUILayout.Label("Y offset", GUILayout.Width(100f));} else {GUILayout.Label("Y 오프셋", GUILayout.Width(100f));}
            settings.comboY = GUILayout.HorizontalSlider(settings.comboY, -200, 200, GUILayout.Width(240f));
            string comboYStr = GUILayout.TextField(settings.comboY.ToString("0"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(comboYStr, out parsed)) settings.comboY = Mathf.Clamp(parsed, -200, 200);
            GUILayout.EndHorizontal();
        }

        private static void DrawPerfectComboExpanded()
        {
            GUILayout.BeginHorizontal();
            if (settings.language == "en"){GUILayout.Label("Position", GUILayout.Width(80f));} else {GUILayout.Label("위치", GUILayout.Width(80f));}
            settings.captionY = GUILayout.HorizontalSlider(settings.captionY, -100, 200, GUILayout.Width(240f));
            string perfectComboStr = GUILayout.TextField(settings.captionY.ToString("0"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(perfectComboStr, out parsed)) settings.captionY = Mathf.Clamp(parsed, -100, 200);
            GUILayout.EndHorizontal();
        }

        private static void DrawJudgementBody()
        {
            GUILayout.BeginHorizontal();
            if (settings.language == "en") {GUILayout.Label("Location", GUILayout.Width(90f));} else {GUILayout.Label("위치", GUILayout.Width(80f));}
            settings.judgementPositionY = GUILayout.HorizontalSlider(settings.judgementPositionY, -100, 200, GUILayout.Width(240f));
            string judgementPositionYStr = GUILayout.TextField(settings.judgementPositionY.ToString("0"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(judgementPositionYStr, out parsed)) settings.judgementPositionY = Mathf.Clamp(parsed, -100, 200);
            GUILayout.EndHorizontal();
        }

        private static void DrawHoldBody()
        {
            GUILayout.BeginHorizontal();
            if (settings.language == "en") {GUILayout.Label("X offset (px)", GUILayout.Width(140f));} else {GUILayout.Label("X 오프셋 (px)", GUILayout.Width(140f));}
            settings.HoldOffsetX = GUILayout.HorizontalSlider(settings.HoldOffsetX, -200f, 200f, GUILayout.Width(240f));
            string holdOffsetXStr = GUILayout.TextField(settings.HoldOffsetX.ToString("0"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(holdOffsetXStr, out parsed)) settings.HoldOffsetX = Mathf.Clamp(parsed, -200f, 200f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (settings.language == "en") {GUILayout.Label("Y offset (px)", GUILayout.Width(140f));} else {GUILayout.Label("Y 오프셋 (px)", GUILayout.Width(140f));}
            settings.HoldOffsetY = GUILayout.HorizontalSlider(settings.HoldOffsetY, -200f, 200f, GUILayout.Width(240f));
            string holdOffsetYStr = GUILayout.TextField(settings.HoldOffsetY.ToString("0"), GUILayout.Width(60f));
            float parsed2;
            if (float.TryParse(holdOffsetYStr, out parsed2)) settings.HoldOffsetY = Mathf.Clamp(parsed2, -200f, 200f);
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

            // Only resync the hex buffer to the current RGBA when the user is NOT actively
            // typing into the hex field. Otherwise an intermediate invalid string ("FFAA",
            // mid-edit) parses as a no-op and we'd clobber the user's keystroke on the next
            // frame, making it feel like typing is impossible (forces them to use sliders).
            if (!userTypedHex)
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

        private static void DrawAttemptBody()
        {
            if (settings.language == "en")
            {
                DrawSubToggle(ref settings.ShowAttempt, "Show attempt count");
                DrawSubToggle(ref settings.ShowFullAttempt, "Show session total (N / total)");
            }
            else
            {
                DrawSubToggle(ref settings.ShowAttempt, "시도 횟수 표시");
                DrawSubToggle(ref settings.ShowFullAttempt, "세션 합계 표시 (N / 합계)");
            }

            GUILayout.BeginHorizontal();
            if (settings.language == "en") { GUILayout.Label("X offset (px)", GUILayout.Width(140f)); } else { GUILayout.Label("X 오프셋 (px)", GUILayout.Width(140f)); }
            settings.AttemptOffsetX = GUILayout.HorizontalSlider(settings.AttemptOffsetX, -400f, 400f, GUILayout.Width(240f));
            string axStr = GUILayout.TextField(settings.AttemptOffsetX.ToString("0"), GUILayout.Width(60f));
            float axP;
            if (float.TryParse(axStr, out axP)) settings.AttemptOffsetX = Mathf.Clamp(axP, -400f, 400f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (settings.language == "en") { GUILayout.Label("Y offset (px)", GUILayout.Width(140f)); } else { GUILayout.Label("Y 오프셋 (px)", GUILayout.Width(140f)); }
            settings.AttemptOffsetY = GUILayout.HorizontalSlider(settings.AttemptOffsetY, -200f, 400f, GUILayout.Width(240f));
            string ayStr = GUILayout.TextField(settings.AttemptOffsetY.ToString("0"), GUILayout.Width(60f));
            float ayP;
            if (float.TryParse(ayStr, out ayP)) settings.AttemptOffsetY = Mathf.Clamp(ayP, -200f, 400f);
            GUILayout.EndHorizontal();
        }

        private static void DrawTimingScaleBody()
        {
            GUILayout.BeginHorizontal();
            if (settings.language == "en") { GUILayout.Label("Y offset (px)", GUILayout.Width(140f)); } else { GUILayout.Label("Y 오프셋 (px)", GUILayout.Width(140f)); }
            settings.TimingScaleOffsetY = GUILayout.HorizontalSlider(settings.TimingScaleOffsetY, -200f, 200f, GUILayout.Width(240f));
            string yStr = GUILayout.TextField(settings.TimingScaleOffsetY.ToString("0"), GUILayout.Width(60f));
            float yP;
            if (float.TryParse(yStr, out yP)) settings.TimingScaleOffsetY = Mathf.Clamp(yP, -200f, 200f);
            GUILayout.EndHorizontal();
        }

        private static void DrawKeyViewerBody()
        {
            bool ko = settings.language == "kr";

            // ---- Mode selector ----
            // "simple" = hardcoded Key10/12/16/20 layouts, "dmnote" = JSON preset.
            GUILayout.BeginHorizontal();
            GUILayout.Label(ko ? "모드" : "Mode", GUILayout.Width(80f));
            bool wasSimple = string.Equals(settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase);
            bool simpleSel = GUILayout.Toggle(wasSimple, ko ? "심플 (프리셋 없음)" : "Simple (no preset)", GUILayout.Width(220f));
            bool dmSel = GUILayout.Toggle(!wasSimple, ko ? "DM Note (고급)" : "DM Note (advanced)", GUILayout.Width(220f));
            if (simpleSel && !wasSimple) settings.KeyViewerMode = "simple";
            else if (dmSel && wasSimple) settings.KeyViewerMode = "dmnote";
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);

            // Mode-specific layout source. Shared rain/offset/scale controls render below.
            if (string.Equals(settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase))
            {
                DrawSimpleKeyViewerBody(ko);
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(ko ? "프리셋 가져오기 (DM Note JSON)" : "Import preset (DM Note JSON)", GUILayout.Width(350f)))
                {
                    ImportKeyViewerPreset();
                }
                if (GUILayout.Button(ko ? "초기화" : "Clear", GUILayout.Width(100f)))
                {
                    settings.keyViewerPresetJson = "";
                    keyViewerKeys = null;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                string status;
                if (string.IsNullOrEmpty(settings.keyViewerPresetJson)) status = ko ? "프리셋 없음" : "No preset loaded";
                else status = ko ? ("프리셋 로드됨 (" + settings.keyViewerPresetJson.Length + " 문자)") : ("Preset loaded (" + settings.keyViewerPresetJson.Length + " chars)");
                GUILayout.Label(status);

                GUILayout.BeginHorizontal();
                string newTab = GUILayout.TextField(settings.keyViewerSelectedTab ?? "4key", GUILayout.Width(140f));
                if (newTab != settings.keyViewerSelectedTab)
                {
                    settings.keyViewerSelectedTab = newTab;
                    keyViewerKeys = null;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(ko ? "X 오프셋" : "X offset", GUILayout.Width(100f));
            settings.KeyViewerOffsetX = GUILayout.HorizontalSlider(settings.KeyViewerOffsetX, -2000f, 2000f, GUILayout.Width(240f));
            string xs = GUILayout.TextField(settings.KeyViewerOffsetX.ToString("0"), GUILayout.Width(60f));
            float xp;
            if (float.TryParse(xs, out xp)) settings.KeyViewerOffsetX = Mathf.Clamp(xp, -10000f, 10000f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(ko ? "Y 오프셋" : "Y offset", GUILayout.Width(100f));
            settings.KeyViewerOffsetY = GUILayout.HorizontalSlider(settings.KeyViewerOffsetY, -2000f, 2000f, GUILayout.Width(240f));
            string ys = GUILayout.TextField(settings.KeyViewerOffsetY.ToString("0"), GUILayout.Width(60f));
            float yp;
            if (float.TryParse(ys, out yp)) settings.KeyViewerOffsetY = Mathf.Clamp(yp, -10000f, 10000f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(ko ? "크기" : "Scale", GUILayout.Width(80f));
            settings.KeyViewerScale = GUILayout.HorizontalSlider(settings.KeyViewerScale, 0.2f, 4f, GUILayout.Width(240f));
            string ss = GUILayout.TextField(settings.KeyViewerScale.ToString("0.##"), GUILayout.Width(60f));
            float sp;
            if (float.TryParse(ss, out sp)) settings.KeyViewerScale = Mathf.Clamp(sp, 0.2f, 4f);
            GUILayout.EndHorizontal();

            DrawSubToggle(ref settings.KeyViewerNoteEffect, ko ? "노트 비 효과" : "Note rain effect");
            DrawSubToggle(ref settings.KeyViewerNoteReverse, ko ? "노트 반전 (아래로)" : "Reverse rain (downward)");
            DrawSubToggle(ref settings.KeyViewerShowCounter, ko ? "카운터 표시" : "Show counter");

            GUILayout.BeginHorizontal();
            GUILayout.Label(ko ? "노트 속도" : "Note speed", GUILayout.Width(150f));
            settings.KeyViewerNoteSpeed = GUILayout.HorizontalSlider(settings.KeyViewerNoteSpeed, 10f, 1000f, GUILayout.Width(240f));
            string nss = GUILayout.TextField(settings.KeyViewerNoteSpeed.ToString("0"), GUILayout.Width(60f));
            float nsp;
            if (float.TryParse(nss, out nsp)) settings.KeyViewerNoteSpeed = Mathf.Clamp(nsp, 1f, 5000f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(ko ? "트랙 높이" : "Track height", GUILayout.Width(150f));
            settings.KeyViewerTrackHeight = GUILayout.HorizontalSlider(settings.KeyViewerTrackHeight, 0f, 1000f, GUILayout.Width(240f));
            string ths = GUILayout.TextField(settings.KeyViewerTrackHeight.ToString("0"), GUILayout.Width(60f));
            float thp;
            if (float.TryParse(ths, out thp)) settings.KeyViewerTrackHeight = Mathf.Clamp(thp, 0f, 5000f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(ko ? "페이드 (px)" : "Fade (px)", GUILayout.Width(150f));
            settings.KeyViewerFadePx = GUILayout.HorizontalSlider(settings.KeyViewerFadePx, 0f, 500f, GUILayout.Width(240f));
            string fps = GUILayout.TextField(settings.KeyViewerFadePx.ToString("0"), GUILayout.Width(60f));
            float fpp;
            if (float.TryParse(fps, out fpp)) settings.KeyViewerFadePx = Mathf.Clamp(fpp, 0f, 2000f);
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
            int totalNow = GetKeyViewerTotal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(18f);
            GUILayout.Label(ko ? "전체 합계" : "Total", GUILayout.Width(120f));
            string totalShown = (kvTotalCountBuffer != null) ? kvTotalCountBuffer : totalNow.ToString();
            string totalEdited = GUILayout.TextField(totalShown, GUILayout.Width(120f));
            if (totalEdited != totalShown) kvTotalCountBuffer = totalEdited;
            int totalParsed;
            if (int.TryParse(kvTotalCountBuffer ?? totalEdited, out totalParsed) && totalParsed != totalNow)
                SetKeyViewerTotal(totalParsed);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            // ---- Per-key count fields ----
            List<KeyValuePair<string, int>> entries = EnumerateKeyViewerCounters();
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
                        SetKeyViewerCount(name, parsed);
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
                    ResetAllKeyViewerCounters();
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
        private static bool simpleKeyShare;
        private static bool simpleKeyChangeExpanded;
        private static bool simpleTextChangeExpanded;
        private static bool simpleColorExpanded;
        private static bool simpleConfirmReset;
        private static int simpleSelectedSlot = -1;
        private static bool simpleSelectedTextEdit;
        private static string simpleSpeedStr, simpleHeightStr, simpleSizeStr;
        private static int simplePrevStyle = -1;

        private static int SimpleSlotCount(int style)
        {
            switch (style) { case 0: return 10; case 1: return 12; case 2: return 16; case 3: return 20; default: return 12; }
        }
        private static int[] SimpleCodes(int style)
        {
            switch (style)
            {
                case 0: return settings.KeyViewerSimpleKey10;
                case 1: return settings.KeyViewerSimpleKey12;
                case 2: return settings.KeyViewerSimpleKey16;
                case 3: return settings.KeyViewerSimpleKey20;
                default: return settings.KeyViewerSimpleKey12;
            }
        }
        private static string[] SimpleTexts(int style)
        {
            switch (style)
            {
                case 0: return settings.KeyViewerSimpleKey10Text;
                case 1: return settings.KeyViewerSimpleKey12Text;
                case 2: return settings.KeyViewerSimpleKey16Text;
                case 3: return settings.KeyViewerSimpleKey20Text;
                default: return settings.KeyViewerSimpleKey12Text;
            }
        }

        private static string SimpleKeyShortLabel(KeyCode kc)
        {
            string s = kc.ToString();
            if (s.StartsWith("Alpha")) s = s.Substring(5);
            if (s.StartsWith("Keypad")) s = "N" + s.Substring(6);
            if (s.StartsWith("Left")) s = "L" + s.Substring(4);
            if (s.StartsWith("Right")) s = "R" + s.Substring(5);
            return s;
        }

        private static void SimpleResetCounts()
        {
            // Defer to the shared API so we wipe both PlayerPrefs and the in-memory
            // totals counter. Calling DeleteKey alone left keyViewerTotalPresses stale.
            ResetAllKeyViewerCounters();
        }

        private static void DrawSimpleKeyViewerBody(bool ko)
        {
            int style = Mathf.Clamp(settings.KeyViewerSimpleStyle, 0, 3);

            // ----- Top toggles & sliders -----
            DrawSubToggle(ref simpleKeyShare, ko ? "키 공유 (스타일 변경 시 키 복사)" : "Key share (copy keys when changing style)");

            bool prevDown = settings.KeyViewerSimpleDownLocation;
            DrawSubToggle(ref settings.KeyViewerSimpleDownLocation, ko ? "아래쪽 위치" : "Down location");
            if (prevDown != settings.KeyViewerSimpleDownLocation) keyViewerKeys = null;

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

            DrawSubToggle(ref settings.KeyViewerSimpleUseRain, ko ? "비 효과 사용" : "Enable rain effect");
            DrawSubFloat(ref settings.KeyViewerSimpleRainSpeed, ref simpleSpeedStr, ko ? "비 속도" : "Rain speed", 1f, 800f);
            DrawSubFloat(ref settings.KeyViewerSimpleRainHeight, ref simpleHeightStr, ko ? "비 높이" : "Rain height", 1f, 1000f);

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
                        settings.KeyViewerSimpleStyle = i;
                        int[] dst = SimpleCodes(i);
                        string[] dstText = SimpleTexts(i);
                        int n = Math.Min(src.Length, dst.Length);
                        for (int j = 0; j < n; j++) { dst[j] = src[j]; dstText[j] = srcText[j]; }
                    }
                    else
                    {
                        settings.KeyViewerSimpleStyle = i;
                    }
                    simpleSelectedSlot = -1;
                    keyViewerKeys = null;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            simplePrevStyle = settings.KeyViewerSimpleStyle;

            DrawSubFloat(ref settings.KeyViewerSimpleSize, ref simpleSizeStr, ko ? "크기" : "Size", 0f, 2f);

            int slotCount = SimpleSlotCount(style);
            int[] codes = SimpleCodes(style);
            string[] texts = SimpleTexts(style);

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
                DrawSimpleSlotButtons(ko, slotCount, codes, texts, false);
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
                        codes[simpleSelectedSlot] = (int)ev.keyCode;
                        simpleSelectedSlot = -1;
                        keyViewerKeys = null;
                        ev.Use();
                    }
                    else if (Input.anyKeyDown)
                    {
                        foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
                        {
                            if (Input.GetKeyDown(kc) && kc != KeyCode.None)
                            {
                                codes[simpleSelectedSlot] = (int)kc;
                                simpleSelectedSlot = -1;
                                keyViewerKeys = null;
                                break;
                            }
                        }
                    }
                }
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
                DrawSimpleSlotButtons(ko, slotCount, codes, texts, true);
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
                        keyViewerKeys = null;
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(28f);
                    if (GUILayout.Button(ko ? "초기화" : "Reset", GUILayout.Width(100f)))
                    {
                        texts[simpleSelectedSlot] = null;
                        simpleSelectedSlot = -1;
                        keyViewerKeys = null;
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
                DrawSimpleColorRow(ref settings.SKvBgR, ref settings.SKvBgG, ref settings.SKvBgB, ref settings.SKvBgA, ko ? "배경" : "Background", "skvBg");
                DrawSimpleColorRow(ref settings.SKvBgcR, ref settings.SKvBgcG, ref settings.SKvBgcB, ref settings.SKvBgcA, ko ? "배경 (눌림)" : "Background (clicked)", "skvBgc");
                DrawSimpleColorRow(ref settings.SKvOutR, ref settings.SKvOutG, ref settings.SKvOutB, ref settings.SKvOutA, ko ? "테두리" : "Outline", "skvOut");
                DrawSimpleColorRow(ref settings.SKvOutcR, ref settings.SKvOutcG, ref settings.SKvOutcB, ref settings.SKvOutcA, ko ? "테두리 (눌림)" : "Outline (clicked)", "skvOutc");
                DrawSimpleColorRow(ref settings.SKvTxtR, ref settings.SKvTxtG, ref settings.SKvTxtB, ref settings.SKvTxtA, ko ? "글자" : "Text", "skvTxt");
                DrawSimpleColorRow(ref settings.SKvTxtcR, ref settings.SKvTxtcG, ref settings.SKvTxtcB, ref settings.SKvTxtcA, ko ? "글자 (눌림)" : "Text (clicked)", "skvTxtc");
                DrawSimpleColorRow(ref settings.SKvRainR, ref settings.SKvRainG, ref settings.SKvRainB, ref settings.SKvRainA, ko ? "비 색상" : "Rain color", "skvRain");
                DrawSimpleColorRow(ref settings.SKvRain2R, ref settings.SKvRain2G, ref settings.SKvRain2B, ref settings.SKvRain2A, ko ? "비 색상 2" : "Rain color 2", "skvRain2");
                if (style == 3)
                    DrawSimpleColorRow(ref settings.SKvRain3R, ref settings.SKvRain3G, ref settings.SKvRain3B, ref settings.SKvRain3A, ko ? "비 색상 3" : "Rain color 3", "skvRain3");
            }
        }

        // Render slot buttons for Key change / Text change. The button label is the current
        // KeyCode short name (or the text override if shown in text-change mode). Click = select.
        private static void DrawSimpleSlotButtons(bool ko, int slotCount, int[] codes, string[] texts, bool textMode)
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
                        ? (slot < texts.Length && !string.IsNullOrEmpty(texts[slot]) ? texts[slot] : SimpleKeyShortLabel((KeyCode)codes[slot]))
                        : SimpleKeyShortLabel((KeyCode)codes[slot]);
                    if (slot == simpleSelectedSlot && textMode == simpleSelectedTextEdit) label = "<b>" + label + "</b>";
                    if (GUILayout.Button(label, btn))
                    {
                        simpleSelectedSlot = slot;
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
            if (oldR != r || oldG != g || oldB != b || oldA != a) keyViewerKeys = null;
        }

        private static void DrawResourceChangerBody()
        {
            bool prev = settings.ChangeOttoIcon;
            if (settings.language == "en")
            {
                DrawSubToggle(ref settings.ChangeOttoIcon, "Change Otto (auto-mode) icon");
            }
            else
            {
                DrawSubToggle(ref settings.ChangeOttoIcon, "오토 (자동 모드) 아이콘 변경");
            }
            // Apply immediately on toggle so the user sees the swap without leaving the editor.
            if (prev != settings.ChangeOttoIcon)
            {
                if (settings.ChangeOttoIcon) RefreshOttoIcon();
                else RestoreOttoIcon();
            }

            if (settings.ChangeOttoIcon)
            {
                if (settings.language == "en")
                    DrawSubColor(ref settings.OttoR, ref settings.OttoG, ref settings.OttoB, ref settings.OttoA, "Otto color", "otto");
                else
                    DrawSubColor(ref settings.OttoR, ref settings.OttoG, ref settings.OttoB, ref settings.OttoA, "오토 색상", "otto");
                // Push the new tint to the live editor icon every GUI repaint so
                // sliders update visually as the user drags. ResourceChanger derives
                // the dim "auto off" variant automatically.
                RefreshOttoIcon();
            }
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    }
}
