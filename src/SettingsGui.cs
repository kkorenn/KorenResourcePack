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

        private static GUIStyle expandStyle;
        private static GUIStyle enableStyle;

        private static readonly Dictionary<string, string> colorBuffers = new Dictionary<string, string>();
        private static readonly HashSet<string> colorExpanded = new HashSet<string>();

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
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

            EnsureBundledFontsLoaded();
            if (bundledFontNames != null && bundledFontNames.Count > 0 && string.IsNullOrEmpty(settings.fontName))
            {
                settings.fontName = bundledFontNames[0];
                preferredHudFont = null;
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

            if (fontDropdownOpen && bundledFontNames != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(110f);
                GUILayout.BeginVertical();
                foreach (string name in bundledFontNames)
                {
                    string label = settings.fontName == name ? "● " + name : "○ " + name;
                    if (GUILayout.Button(label, GUI.skin.label, GUILayout.ExpandWidth(false)))
                    {
                        settings.fontName = name;
                        preferredHudFont = null;
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
                DrawExpandable(ref settings.ShowFPS, ref settings.fpsExpanded, "Show FPS", DrawFPSBody);
            } else
            {
                DrawSubToggle(ref settings.ShowProgress, "프로그레스 퍼센트 표시");
                DrawSubToggle(ref settings.ShowAccuracy, "정확도 표시");
                DrawSubToggle(ref settings.ShowXAccuracy, "절대 정확도 표시");
                DrawSubToggle(ref settings.ShowMusicTime, "음악/맵 시간 표시");
                DrawSubToggle(ref settings.ShowCheckpoint, "체크포인트 표시");
                DrawSubToggle(ref settings.ShowBest, "최고 표시");
                DrawExpandable(ref settings.ShowFPS, ref settings.fpsExpanded, "프레임 표시", DrawFPSBody);
            }
        }

        private static void DrawFPSBody()
        {
            GUILayout.BeginHorizontal();
            if (settings.language == "en"){GUILayout.Label("Interval", GUILayout.Width(80f));} else {GUILayout.Label("간격", GUILayout.Width(80f));}
            settings.updInterval = GUILayout.HorizontalSlider(settings.updInterval, 1, 1000, GUILayout.Width(240f));
            string sizeStr = GUILayout.TextField(settings.updInterval.ToString("#"), GUILayout.Width(60f));
            float parsed;
            if (float.TryParse(sizeStr, out parsed)) settings.updInterval = Mathf.Clamp(parsed, 1, 1000);
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
            if (settings.language == "en") {DrawSubInt(ref settings.ComboColorMax, ref comboColorMaxStr, "Combo color max", 0, 1000000);} else {DrawSubInt(ref settings.ComboColorMax, ref comboColorMaxStr, "최대 콤보 색깔", 0, 1000000);}
            DrawSubColor(ref settings.ComboColorLowR, ref settings.ComboColorLowG, ref settings.ComboColorLowB, ref settings.ComboColorLowA, "0%", "comboLow");
            DrawSubColor(ref settings.ComboColorHighR, ref settings.ComboColorHighG, ref settings.ComboColorHighB, ref settings.ComboColorHighA, "100%", "comboHigh");
            if (settings.language == "en") {DrawSubToggle(ref settings.ComboMoveUpNoCaption, "Move up when no title/artist");} else {DrawSubToggle(ref settings.ComboMoveUpNoCaption, "제목/작가가 없을 때 위로 올리기");}
            if (settings.language == "en") {DrawExpandable(ref settings.CaptionText, ref settings.captionExpanded, "Show Perfect Combo Text", DrawPerfectComboExpanded);} else {DrawExpandable(ref settings.CaptionText, ref settings.captionExpanded, "Perfect Combo 글자 표시", DrawPerfectComboExpanded);}
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

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    }
}
