using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KorenResourcePack
{
    public static partial class Main
    {
        // ========================================================
        // Retained-mode KeyViewer canvases. Images stay below TMP text.
        // ========================================================
        private const int KvImageSortingOrder = 32701;
        private const int KvTextSortingOrder = 32702;
        private const float KvMaxCornerRadiusPx = 8f;

        private static GameObject kvImageRoot;
        private static Canvas kvImageCanvas;
        private static RectTransform kvNotesLayer;
        private static RectTransform kvKeysLayer;
        private static bool kvImageBuilt;

        private static GameObject kvTextRoot;
        private static Canvas kvTextCanvas;
        private static bool kvTextBuilt;
        private static TMP_FontAsset kvActiveFont;
        private static string kvActiveFontName;
        private static readonly Color KvShadowColor = new Color(0f, 0f, 0f, 0.55f);

        private class KvUiRect
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public KvRoundedImage rounded;
        }

        private class KvRoundedImage : MaskableGraphic
        {
            private readonly List<Vector2> outer = new List<Vector2>(64);
            private readonly List<Vector2> inner = new List<Vector2>(64);
            private float cornerRadius;
            private bool verticalGradient;
            private bool reverseGradient;
            private float ringThickness;

            public void SetShape(float radius, bool gradient, bool reverse, float borderThickness)
            {
                radius = Mathf.Max(0f, radius);
                borderThickness = Mathf.Max(0f, borderThickness);
                if (Mathf.Abs(cornerRadius - radius) < 0.01f &&
                    verticalGradient == gradient &&
                    reverseGradient == reverse &&
                    Mathf.Abs(ringThickness - borderThickness) < 0.01f)
                    return;

                cornerRadius = radius;
                verticalGradient = gradient;
                reverseGradient = reverse;
                ringThickness = borderThickness;
                SetVerticesDirty();
            }

            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();

                Rect rect = GetPixelAdjustedRect();
                if (rect.width <= 0f || rect.height <= 0f)
                    return;

                float radius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(rect.width, rect.height) * 0.25f);
                if (ringThickness > 0.01f)
                {
                    PopulateRing(vh, rect, radius, ringThickness);
                    return;
                }

                float aa = Mathf.Min(1.25f, rect.width * 0.25f, rect.height * 0.25f);
                Rect innerRect = new Rect(rect.xMin + aa, rect.yMin + aa, Mathf.Max(0f, rect.width - aa * 2f), Mathf.Max(0f, rect.height - aa * 2f));
                float innerRadius = Mathf.Max(0f, radius - aa);
                int segments = Mathf.Clamp(Mathf.CeilToInt(radius * 0.9f), 4, 12);

                outer.Clear();
                inner.Clear();
                AddRoundedRectPoints(outer, rect, radius, segments);
                AddRoundedRectPoints(inner, innerRect, innerRadius, segments);
                if (inner.Count < 3 || outer.Count != inner.Count)
                    return;

                int centerIndex = vh.currentVertCount;
                Vector2 center = innerRect.center;
                vh.AddVert(center, VertexColor(center, rect, 1f), Vector2.zero);

                int innerStart = vh.currentVertCount;
                for (int i = 0; i < inner.Count; i++)
                    vh.AddVert(inner[i], VertexColor(inner[i], rect, 1f), Vector2.zero);

                int outerStart = vh.currentVertCount;
                for (int i = 0; i < outer.Count; i++)
                    vh.AddVert(outer[i], VertexColor(outer[i], rect, 0f), Vector2.zero);

                for (int i = 0; i < inner.Count; i++)
                {
                    int next = (i + 1) % inner.Count;
                    vh.AddTriangle(centerIndex, innerStart + i, innerStart + next);
                    vh.AddTriangle(innerStart + i, outerStart + i, outerStart + next);
                    vh.AddTriangle(innerStart + i, outerStart + next, innerStart + next);
                }
            }

            private void PopulateRing(VertexHelper vh, Rect rect, float radius, float thickness)
            {
                float maxThickness = Mathf.Min(rect.width, rect.height) * 0.45f;
                float t = Mathf.Clamp(thickness, 0f, maxThickness);
                if (t <= 0.01f) return;

                Rect innerRect = new Rect(rect.xMin + t, rect.yMin + t, Mathf.Max(0f, rect.width - t * 2f), Mathf.Max(0f, rect.height - t * 2f));
                if (innerRect.width <= 0f || innerRect.height <= 0f) return;

                int segments = Mathf.Clamp(Mathf.CeilToInt(radius * 0.9f), 4, 12);
                outer.Clear();
                inner.Clear();
                AddRoundedRectPoints(outer, rect, radius, segments);
                AddRoundedRectPoints(inner, innerRect, Mathf.Max(0f, radius - t), segments);
                if (outer.Count != inner.Count) return;

                Color32 c = color;
                int outerStart = vh.currentVertCount;
                for (int i = 0; i < outer.Count; i++)
                    vh.AddVert(outer[i], c, Vector2.zero);

                int innerStart = vh.currentVertCount;
                for (int i = 0; i < inner.Count; i++)
                    vh.AddVert(inner[i], c, Vector2.zero);

                for (int i = 0; i < outer.Count; i++)
                {
                    int next = (i + 1) % outer.Count;
                    vh.AddTriangle(outerStart + i, outerStart + next, innerStart + next);
                    vh.AddTriangle(outerStart + i, innerStart + next, innerStart + i);
                }
            }

            private Color32 VertexColor(Vector2 p, Rect rect, float edgeAlpha)
            {
                Color c = color;
                if (verticalGradient)
                {
                    float t = Mathf.InverseLerp(rect.yMin, rect.yMax, p.y);
                    c.a *= reverseGradient ? t : (1f - t);
                }
                c.a *= edgeAlpha;
                return c;
            }

            private static void AddRoundedRectPoints(List<Vector2> points, Rect rect, float radius, int segments)
            {
                points.Clear();
                if (radius <= 0.01f)
                {
                    points.Add(new Vector2(rect.xMax, rect.yMin));
                    points.Add(new Vector2(rect.xMax, rect.yMax));
                    points.Add(new Vector2(rect.xMin, rect.yMax));
                    points.Add(new Vector2(rect.xMin, rect.yMin));
                    return;
                }

                AddArc(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, segments);
                AddArc(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, segments);
                AddArc(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, segments);
                AddArc(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f, segments);
            }

            private static void AddArc(List<Vector2> points, Vector2 center, float radius, float fromDeg, float toDeg, int segments)
            {
                for (int i = 0; i <= segments; i++)
                {
                    if (points.Count > 0 && i == 0) continue;
                    float a = Mathf.Lerp(fromDeg, toDeg, i / (float)segments) * Mathf.Deg2Rad;
                    points.Add(new Vector2(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius));
                }
            }
        }

        private static readonly List<KvUiRect> kvNoteImagePool = new List<KvUiRect>();
        private static int kvNoteImageCursor;

        // Compiled per-tab layout from preset JSON
        private class KvKey
        {
            public string keyName;
            public KeyCode keyCode;
            public float dx, dy, width, height;
            public Color noteColor;
            public Color bgColor;
            public Color activeBgColor;
            public Color borderColor;
            public Color activeBorderColor;
            public float borderWidth;
            public float borderRadius;
            public string displayText;
            public float noteWidth;     // 0 = match key width
            public string noteAlignment; // left/center/right
            public bool noteEffectEnabled = true;
            public bool noteGlowEnabled;
            public float noteGlowSize;
            public float noteGlowOpacity;
            public Color noteGlowColor;
            public bool noteAutoYCorrection = true;
            public Color fontColor;
            public Color activeFontColor;
            public int fontSize;
            public Color counterColor;
            public Color activeCounterColor;
            public int counterFontSize;
            public string counterAlign;
            public int count;
            public int statValue;
            public List<float> noteStartTimes = new List<float>(); // Time at which key pressed for note rain
            public List<float> noteEndTimes = new List<float>();   // Time at which key released; -1 means still held
            public bool wasPressed;
            public bool counterEnabled = true;
            public bool hasCustomDisplayText = false;

            // Retained-mode text objects
            public TextMeshProUGUI labelTmp;
            public TextMeshProUGUI counterTmp;

            // Retained-mode image objects
            public GameObject visualRoot;
            public KvUiRect borderUi;
            public KvUiRect fillUi;
        }

        private static List<KvKey> keyViewerKeys;
        private static string lastParsedPresetJson;
        private static string lastParsedTab;
        private static float keyViewerCanvasWidth = 800f;
        private static float keyViewerCanvasHeight = 250f;

        private static int keyViewerTotalPresses;
        private static readonly List<float> keyViewerPressLog = new List<float>();
        private const float KvKpsWindow = 1.0f;

        private static readonly HashSet<KeyCode> kvPressedKeys = new HashSet<KeyCode>();

        public static void KeyViewerPollEvent()
        {
            Event e = Event.current;
            if (e == null) return;
            if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
                kvPressedKeys.Add(e.keyCode);
            else if (e.type == EventType.KeyUp && e.keyCode != KeyCode.None)
                kvPressedKeys.Remove(e.keyCode);
        }

        private static void EmitNoteWithFade(Rect nRect, Color noteColor, float noteBaseY, float trackH, float fadePx, bool reverse)
        {
            if (fadePx <= 0f)
            {
                EmitNoteRect(nRect, noteColor, 0f);
                return;
            }

            float trackTop = reverse ? noteBaseY : (noteBaseY - trackH);
            float trackBot = reverse ? (noteBaseY + trackH) : noteBaseY;

            float fadeBandStart = reverse ? (trackBot - fadePx) : trackTop;
            float fadeBandEnd   = reverse ? trackBot : (trackTop + fadePx);

            fadeBandStart = Mathf.Clamp(fadeBandStart, trackTop, trackBot);
            fadeBandEnd   = Mathf.Clamp(fadeBandEnd, trackTop, trackBot);

            float gradTop = Mathf.Max(nRect.y, fadeBandStart);
            float gradBot = Mathf.Min(nRect.yMax, fadeBandEnd);

            if (gradBot > gradTop)
            {
                EmitNoteGradientRect(new Rect(nRect.x, gradTop, nRect.width, gradBot - gradTop), noteColor, reverse);
            }

            if (!reverse)
            {
                float solidTop = Mathf.Max(nRect.y, fadeBandEnd);
                if (nRect.yMax > solidTop)
                {
                    EmitNoteRect(
                        new Rect(nRect.x, solidTop, nRect.width, nRect.yMax - solidTop),
                        noteColor,
                        0f
                    );
                }
            }
            else
            {
                float solidBot = Mathf.Min(nRect.yMax, fadeBandStart);
                if (solidBot > nRect.y)
                {
                    EmitNoteRect(
                        new Rect(nRect.x, nRect.y, nRect.width, solidBot - nRect.y),
                        noteColor,
                        0f
                    );
                }
            }
        }

        private static bool KvIsKeyPressed(KeyCode kc)
        {
            try
            {
                if (Rewired.ReInput.isReady)
                {
                    var kb = Rewired.ReInput.controllers.Keyboard;
                    if (kb != null && kb.GetKey(kc)) return true;
                }
            }
            catch { }
            if (Input.GetKey(kc)) return true;
            return kvPressedKeys.Contains(kc);
        }

        public static void ResetKeyViewerStats()
        {
            keyViewerPressLog.Clear();
        }

        private static string KvCountKey(string keyName) { return "kvkey_" + (keyName ?? ""); }
        private const string KvTotalPrefKey = "kvtotal";
        private static bool keyViewerTotalLoaded;

        private static void LoadKeyViewerTotalIfNeeded()
        {
            if (keyViewerTotalLoaded) return;
            keyViewerTotalPresses = PlayerPrefs.GetInt(KvTotalPrefKey, 0);
            keyViewerTotalLoaded = true;
        }

        private static float kvSavePending;
        private static void ScheduleKvSave()
        {
            kvSavePending = Time.unscaledTime + 1.0f;
        }
        private static void FlushKvSaveIfDue()
        {
            if (kvSavePending > 0f && Time.unscaledTime >= kvSavePending)
            {
                PlayerPrefs.Save();
                kvSavePending = 0f;
            }
        }

        private static readonly Dictionary<string, KeyCode> KeyNameMap = BuildKeyNameMap();

        private static Dictionary<string, KeyCode> BuildKeyNameMap()
        {
            Dictionary<string, KeyCode> m = new Dictionary<string, KeyCode>(StringComparer.OrdinalIgnoreCase);
            for (char c = 'A'; c <= 'Z'; c++)
            {
                KeyCode kc;
                if (Enum.TryParse<KeyCode>(c.ToString(), true, out kc))
                    m[c.ToString()] = kc;
            }
            for (int i = 0; i <= 9; i++) m[i.ToString()] = (KeyCode)Enum.Parse(typeof(KeyCode), "Alpha" + i);
            m["LEFT SHIFT"] = KeyCode.LeftShift;
            m["RIGHT SHIFT"] = KeyCode.RightShift;
            m["LEFT CTRL"] = KeyCode.LeftControl;
            m["RIGHT CTRL"] = KeyCode.RightControl;
            m["LEFT ALT"] = KeyCode.LeftAlt;
            m["RIGHT ALT"] = KeyCode.RightAlt;
            m["SPACE"] = KeyCode.Space;
            m["TAB"] = KeyCode.Tab;
            m["RETURN"] = KeyCode.Return;
            m["ENTER"] = KeyCode.Return;
            m["BACKSPACE"] = KeyCode.Backspace;
            m["ESCAPE"] = KeyCode.Escape;
            m["UP"] = KeyCode.UpArrow;
            m["DOWN"] = KeyCode.DownArrow;
            m["LEFT"] = KeyCode.LeftArrow;
            m["RIGHT"] = KeyCode.RightArrow;
            m["COMMA"] = KeyCode.Comma;
            m["DOT"] = KeyCode.Period;
            m["PERIOD"] = KeyCode.Period;
            m["FORWARD SLASH"] = KeyCode.Slash;
            m["SLASH"] = KeyCode.Slash;
            m["BACK SLASH"] = KeyCode.Backslash;
            m["BACKSLASH"] = KeyCode.Backslash;
            m["SEMICOLON"] = KeyCode.Semicolon;
            m["APOSTROPHE"] = KeyCode.Quote;
            m["QUOTE"] = KeyCode.Quote;
            m["LEFT BRACKET"] = KeyCode.LeftBracket;
            m["RIGHT BRACKET"] = KeyCode.RightBracket;
            m["MINUS"] = KeyCode.Minus;
            m["EQUALS"] = KeyCode.Equals;
            m["GRAVE"] = KeyCode.BackQuote;
            m["SECTION"] = KeyCode.BackQuote;
            m["BACKQUOTE"] = KeyCode.BackQuote;
            m["BACK QUOTE"] = KeyCode.BackQuote;
            m["SQUARE BRACKET OPEN"] = KeyCode.LeftBracket;
            m["SQUARE BRACKET CLOSE"] = KeyCode.RightBracket;
            m["LEFT BRACKET"] = KeyCode.LeftBracket;
            m["RIGHT BRACKET"] = KeyCode.RightBracket;
            m["LBRACKET"] = KeyCode.LeftBracket;
            m["RBRACKET"] = KeyCode.RightBracket;
            m["OPEN BRACKET"] = KeyCode.LeftBracket;
            m["CLOSE BRACKET"] = KeyCode.RightBracket;
            m["["] = KeyCode.LeftBracket;
            m["]"] = KeyCode.RightBracket;
            m["UP ARROW"] = KeyCode.UpArrow;
            m["DOWN ARROW"] = KeyCode.DownArrow;
            m["LEFT ARROW"] = KeyCode.LeftArrow;
            m["RIGHT ARROW"] = KeyCode.RightArrow;
            m["CAPS LOCK"] = KeyCode.CapsLock;
            m["NUMPAD 0"] = KeyCode.Keypad0;
            m["NUMPAD 1"] = KeyCode.Keypad1;
            m["NUMPAD 2"] = KeyCode.Keypad2;
            m["NUMPAD 3"] = KeyCode.Keypad3;
            m["NUMPAD 4"] = KeyCode.Keypad4;
            m["NUMPAD 5"] = KeyCode.Keypad5;
            m["NUMPAD 6"] = KeyCode.Keypad6;
            m["NUMPAD 7"] = KeyCode.Keypad7;
            m["NUMPAD 8"] = KeyCode.Keypad8;
            m["NUMPAD 9"] = KeyCode.Keypad9;
            m["NUMPAD MULTIPLY"] = KeyCode.KeypadMultiply;
            m["NUMPAD PLUS"] = KeyCode.KeypadPlus;
            m["NUMPAD MINUS"] = KeyCode.KeypadMinus;
            m["NUMPAD DELETE"] = KeyCode.KeypadPeriod;
            m["NUMPAD DIVIDE"] = KeyCode.KeypadDivide;
            m["NUMPAD RETURN"] = KeyCode.KeypadEnter;
            m["NUMPAD ENTER"] = KeyCode.KeypadEnter;
            m["25"] = KeyCode.RightControl;
            m["21"] = KeyCode.RightAlt;
            m["91"] = KeyCode.LeftCommand;
            m["92"] = KeyCode.RightCommand;
            for (int i = 1; i <= 12; i++)
            {
                KeyCode fk;
                if (Enum.TryParse<KeyCode>("F" + i, true, out fk)) m["F" + i] = fk;
            }
            return m;
        }

        private static KeyCode ResolveKeyCode(string name)
        {
            if (string.IsNullOrEmpty(name)) return KeyCode.None;
            KeyCode kc;
            if (KeyNameMap.TryGetValue(name, out kc)) return kc;
            if (Enum.TryParse<KeyCode>(name.Replace(" ", ""), true, out kc)) return kc;
            return KeyCode.None;
        }

        private static readonly Dictionary<string, string> KeyDisplayMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "LEFT SHIFT", "LShift" },
            { "RIGHT SHIFT", "RShift" },
            { "LEFT CTRL", "LCtrl" },
            { "RIGHT CTRL", "RCtrl" },
            { "LEFT ALT", "LAlt" },
            { "RIGHT ALT", "RAlt" },
            { "LEFT WIN", "LWin" },
            { "RIGHT WIN", "RWin" },
            { "BACKSPACE", "Back" },
            { "RETURN", "Enter" },
            { "ENTER", "Enter" },
            { "ESCAPE", "Esc" },
            { "BACKSLASH", "\\" },
            { "BACK SLASH", "\\" },
            { "FORWARD SLASH", "/" },
            { "SLASH", "/" },
            { "EQUALS", "=" },
            { "MINUS", "-" },
            { "DOT", "." },
            { "PERIOD", "." },
            { "COMMA", "," },
            { "SEMICOLON", ";" },
            { "APOSTROPHE", "'" },
            { "QUOTE", "'" },
            { "LEFT BRACKET", "[" },
            { "RIGHT BRACKET", "]" },
            { "LBRACKET", "[" },
            { "RBRACKET", "]" },
            { "OPEN BRACKET", "[" },
            { "CLOSE BRACKET", "]" },
            { "SQUARE BRACKET OPEN", "[" },
            { "SQUARE BRACKET CLOSE", "]" },
            { "UP ARROW", "↑" },
            { "DOWN ARROW", "↓" },
            { "LEFT ARROW", "←" },
            { "RIGHT ARROW", "→" },
            { "CAPS LOCK", "Caps" },
            { "NUMPAD 0", "Num0" },
            { "NUMPAD 1", "Num1" },
            { "NUMPAD 2", "Num2" },
            { "NUMPAD 3", "Num3" },
            { "NUMPAD 4", "Num4" },
            { "NUMPAD 5", "Num5" },
            { "NUMPAD 6", "Num6" },
            { "NUMPAD 7", "Num7" },
            { "NUMPAD 8", "Num8" },
            { "NUMPAD 9", "Num9" },
            { "NUMPAD MULTIPLY", "Num*" },
            { "NUMPAD PLUS", "Num+" },
            { "NUMPAD MINUS", "Num-" },
            { "NUMPAD DELETE", "Num." },
            { "NUMPAD DIVIDE", "Num/" },
            { "NUMPAD RETURN", "NEnt" },
            { "25", "RCtrl" },
            { "21", "RAlt" },
            { "91", "LWin" },
            { "92", "RWin" },
            { "GRAVE", "`" },
            { "SECTION", "§" },
            { "BACKQUOTE", "`" },
            { "BACK QUOTE", "`" },
            { "TAB", "Tab" },
            { "SPACE", "Space" },
            { "CAPSLOCK", "Caps" },
            { "UP", "↑" },
            { "DOWN", "↓" },
            { "LEFT", "←" },
            { "RIGHT", "→" },
        };

        private static string DefaultDisplayFor(string keyName)
        {
            if (string.IsNullOrEmpty(keyName)) return "";
            string s;
            if (KeyDisplayMap.TryGetValue(keyName, out s)) return s;
            return keyName;
        }

        private static TextAlignmentOptions KvCounterAlignment(string align)
        {
            if (string.Equals(align, "top", StringComparison.OrdinalIgnoreCase))
                return TextAlignmentOptions.Top;
            if (string.Equals(align, "bottom", StringComparison.OrdinalIgnoreCase))
                return TextAlignmentOptions.Bottom;
            if (string.Equals(align, "right", StringComparison.OrdinalIgnoreCase))
                return TextAlignmentOptions.MidlineRight;
            if (string.Equals(align, "left", StringComparison.OrdinalIgnoreCase))
                return TextAlignmentOptions.MidlineLeft;
            return TextAlignmentOptions.Center;
        }

        private static Color HexToColor(string hex, float alpha)
        {
            if (string.IsNullOrEmpty(hex)) return new Color(1f, 1f, 1f, alpha);
            string s = hex.Trim();
            try
            {
                if (string.Equals(s, "transparent", StringComparison.OrdinalIgnoreCase))
                    return new Color(0f, 0f, 0f, 0f);

                if (s.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
                {
                    int lp = s.IndexOf('(');
                    int rp = s.IndexOf(')');
                    if (lp > 0 && rp > lp)
                    {
                        string inner = s.Substring(lp + 1, rp - lp - 1);
                        string[] parts = inner.Split(new[] { ',', '/' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            float r = ParseColorComponent(parts[0], 255f);
                            float g = ParseColorComponent(parts[1], 255f);
                            float b = ParseColorComponent(parts[2], 255f);
                            float a = parts.Length >= 4 ? ParseAlphaComponent(parts[3]) : 1f;
                            return new Color(r, g, b, a);
                        }
                    }
                    return new Color(1f, 1f, 1f, alpha);
                }

                string h = s.TrimStart('#');
                if (h.Length == 3)
                {
                    int r = Convert.ToInt32(new string(h[0], 2), 16);
                    int g = Convert.ToInt32(new string(h[1], 2), 16);
                    int b = Convert.ToInt32(new string(h[2], 2), 16);
                    return new Color(r / 255f, g / 255f, b / 255f, alpha);
                }
                if (h.Length == 4)
                {
                    int r = Convert.ToInt32(new string(h[0], 2), 16);
                    int g = Convert.ToInt32(new string(h[1], 2), 16);
                    int b = Convert.ToInt32(new string(h[2], 2), 16);
                    int a = Convert.ToInt32(new string(h[3], 2), 16);
                    return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                }
                if (h.Length == 6)
                {
                    int r = Convert.ToInt32(h.Substring(0, 2), 16);
                    int g = Convert.ToInt32(h.Substring(2, 2), 16);
                    int b = Convert.ToInt32(h.Substring(4, 2), 16);
                    return new Color(r / 255f, g / 255f, b / 255f, alpha);
                }
                if (h.Length == 8)
                {
                    int r = Convert.ToInt32(h.Substring(0, 2), 16);
                    int g = Convert.ToInt32(h.Substring(2, 2), 16);
                    int b = Convert.ToInt32(h.Substring(4, 2), 16);
                    int a = Convert.ToInt32(h.Substring(6, 2), 16);
                    return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                }
            }
            catch { }
            return new Color(1f, 1f, 1f, alpha);
        }

        private static float ParseColorComponent(string s, float scale)
        {
            string t = s.Trim();
            if (t.EndsWith("%"))
            {
                float pct;
                if (float.TryParse(t.TrimEnd('%').Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pct))
                    return Mathf.Clamp01(pct / 100f);
                return 1f;
            }
            float v;
            if (float.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v))
                return Mathf.Clamp01(v / scale);
            return 1f;
        }

        private static float ParseAlphaComponent(string s)
        {
            string t = s.Trim();
            if (t.EndsWith("%"))
            {
                float pct;
                if (float.TryParse(t.TrimEnd('%').Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pct))
                    return Mathf.Clamp01(pct / 100f);
                return 1f;
            }
            float v;
            if (float.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v))
                return v <= 1f ? Mathf.Clamp01(v) : Mathf.Clamp01(v / 255f);
            return 1f;
        }

        private static bool JNotNull(JToken t)
        {
            return t != null && t.Type != JTokenType.Null;
        }

        private static string JStr(JObject p, string key, string def)
        {
            JToken t = p[key];
            if (!JNotNull(t)) return def;
            string s = t.ToString();
            return string.IsNullOrEmpty(s) ? def : s;
        }

        private static string JOptionalString(JObject p, string key)
        {
            JToken t = p[key];
            if (!JNotNull(t)) return null;
            return t.ToString();
        }

        private static float JFloat(JObject p, string key, float def)
        {
            JToken t = p[key];
            if (!JNotNull(t)) return def;
            try { return t.ToObject<float>(); } catch { return def; }
        }

        private static int JInt(JObject p, string key, int def)
        {
            JToken t = p[key];
            if (!JNotNull(t)) return def;
            try { return t.ToObject<int>(); } catch { return def; }
        }

        private static bool JBool(JObject p, string key, bool def)
        {
            JToken t = p[key];
            if (!JNotNull(t)) return def;
            try { return t.ToObject<bool>(); } catch { return def; }
        }

        // ----------------- RETAINED IMAGE CANVAS -----------------

        private static void BuildKeyViewerImageOverlayIfNeeded()
        {
            if (kvImageBuilt && kvImageRoot != null)
            {
                if (kvImageCanvas != null) kvImageCanvas.sortingOrder = KvImageSortingOrder;
                return;
            }

            kvImageRoot = new GameObject("KorenResourcePack.KeyViewer.Images");
            UnityEngine.Object.DontDestroyOnLoad(kvImageRoot);

            kvImageCanvas = kvImageRoot.AddComponent<Canvas>();
            kvImageCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            kvImageCanvas.sortingOrder = KvImageSortingOrder;

            var scaler = kvImageRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            kvImageRoot.AddComponent<GraphicRaycaster>().enabled = false;
            kvNotesLayer = NewKvLayer("Notes");
            kvKeysLayer = NewKvLayer("Keys");
            kvImageBuilt = true;
        }

        private static RectTransform NewKvLayer(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(kvImageRoot.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static void DestroyKvImageChildren()
        {
            DestroyKvChildren(kvNotesLayer);
            DestroyKvChildren(kvKeysLayer);
            kvNoteImagePool.Clear();
            kvNoteImageCursor = 0;
        }

        private static void DestroyKvChildren(Transform parent)
        {
            if (parent == null) return;
            foreach (Transform child in parent)
            {
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private static KvUiRect NewKvUiRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            KvRoundedImage rounded = go.AddComponent<KvRoundedImage>();
            rounded.raycastTarget = false;
            rounded.enabled = true;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            KvUiRect ui = new KvUiRect();
            ui.gameObject = go;
            ui.rectTransform = rt;
            ui.rounded = rounded;
            return ui;
        }

        private static KvUiRect NewKeyViewerRect(string name, Transform parent)
        {
            return NewKvUiRect(name, parent);
        }

        private static GameObject NewKeyVisualRoot(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(kvKeysLayer, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            return go;
        }

        private static void BeginKeyViewerImageFrame()
        {
            kvNoteImageCursor = 0;
        }

        private static void EndKeyViewerImageFrame()
        {
            for (int i = kvNoteImageCursor; i < kvNoteImagePool.Count; i++)
            {
                if (kvNoteImagePool[i] != null && kvNoteImagePool[i].rounded != null)
                    kvNoteImagePool[i].rounded.enabled = false;
            }
        }

        private static KvUiRect NextNoteImage()
        {
            if (kvNotesLayer == null) return null;
            KvUiRect ui;
            if (kvNoteImageCursor < kvNoteImagePool.Count)
            {
                ui = kvNoteImagePool[kvNoteImageCursor];
            }
            else
            {
                ui = NewKvUiRect("KVNote", kvNotesLayer);
                kvNoteImagePool.Add(ui);
            }

            kvNoteImageCursor++;
            return ui;
        }

        private static void EmitNoteRect(Rect rect, Color color, float radius)
        {
            KvUiRect ui = NextNoteImage();
            PlaceKvUiRect(ui, rect, color, radius, 0f);
        }

        private static void EmitNoteGradientRect(Rect rect, Color color, bool reverse)
        {
            KvUiRect ui = NextNoteImage();
            PlaceKvGradientRect(ui, rect, color, reverse);
        }

        private static void PlaceKeyRect(KvUiRect ui, Rect rect, Color color, float radius, float borderThickness)
        {
            PlaceKvUiRect(ui, rect, color, radius, borderThickness);
        }

        private static void PlaceKeyRect(KvUiRect ui, Rect rect, Color color, float radius)
        {
            PlaceKvUiRect(ui, rect, color, radius, 0f);
        }

        private static void PlaceKvUiRect(KvUiRect ui, Rect rect, Color color, float radius, float borderThickness)
        {
            if (ui == null || ui.rounded == null) return;
            if (rect.width <= 0f || rect.height <= 0f || color.a <= 0f)
            {
                ui.rounded.enabled = false;
                return;
            }

            float maxRadius = Mathf.Min(KvMaxCornerRadiusPx, Mathf.Min(rect.width, rect.height) * 0.25f);
            float effectiveRadius = Mathf.Min(Mathf.Max(0f, radius), maxRadius);

            ui.rectTransform.anchoredPosition = new Vector2(rect.x, -rect.y);
            ui.rectTransform.sizeDelta = new Vector2(rect.width, rect.height);
            ui.rounded.color = color;
            ui.rounded.SetShape(effectiveRadius, false, false, borderThickness);
            ui.rounded.enabled = true;
        }

        private static void PlaceKvGradientRect(KvUiRect ui, Rect rect, Color color, bool reverse)
        {
            if (ui == null || ui.rounded == null) return;
            if (rect.width <= 0f || rect.height <= 0f || color.a <= 0f)
            {
                ui.rounded.enabled = false;
                return;
            }

            ui.rectTransform.anchoredPosition = new Vector2(rect.x, -rect.y);
            ui.rectTransform.sizeDelta = new Vector2(rect.width, rect.height);
            ui.rounded.color = color;
            ui.rounded.SetShape(0f, true, reverse, 0f);
            ui.rounded.enabled = true;
        }

        // ----------------- RETAINED TEXT CANVAS -----------------

        private static void BuildKeyViewerTextOverlayIfNeeded()
        {
            if (kvTextBuilt && kvTextRoot != null)
            {
                if (kvTextCanvas != null) kvTextCanvas.sortingOrder = KvTextSortingOrder;
                return;
            }

            kvTextRoot = new GameObject("KorenResourcePack.KeyViewer.Text");
            UnityEngine.Object.DontDestroyOnLoad(kvTextRoot);

            kvTextCanvas = kvTextRoot.AddComponent<Canvas>();
            kvTextCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            kvTextCanvas.sortingOrder = KvTextSortingOrder;

            var scaler = kvTextRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            kvTextRoot.AddComponent<GraphicRaycaster>().enabled = false;
            kvTextBuilt = true;
        }

        private static void DestroyKvTextChildren()
        {
            if (kvTextRoot == null) return;
            foreach (Transform child in kvTextRoot.transform)
            {
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private static TextMeshProUGUI NewKvLabel(string text, TextAlignmentOptions align)
        {
            GameObject go = new GameObject("KVLabel", typeof(RectTransform));
            go.transform.SetParent(kvTextRoot.transform, false);
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.alignment = align;
            t.color = Color.white;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.text = text ?? "";
            t.outlineColor = KvShadowColor;
            t.outlineWidth = 0.18f;
            return t;
        }

        private static void ApplyFontToKeyViewer()
        {
            if (!kvTextBuilt) return;
            string requested = settings != null ? (settings.fontName ?? "") : "";
            if (requested == kvActiveFontName && kvActiveFont != null) return;

            TMP_FontAsset fa = null;
            try { fa = GetBundleFont(requested); } catch { }
            if (fa == null) try { fa = bundleDefaultFont; } catch { }
            if (fa == null) return;

            kvActiveFont = fa;
            kvActiveFontName = requested;

            if (keyViewerKeys == null) return;
            foreach (var k in keyViewerKeys)
            {
                if (k.labelTmp != null) k.labelTmp.font = kvActiveFont;
                if (k.counterTmp != null) k.counterTmp.font = kvActiveFont;
            }
        }

        // ----------------- LAYOUT -----------------

        private static void RebuildKeyViewerLayout()
        {
            keyViewerKeys = new List<KvKey>();
            string raw = settings.keyViewerPresetJson;
            string tab = string.IsNullOrEmpty(settings.keyViewerSelectedTab) ? "4key" : settings.keyViewerSelectedTab;
            if (string.IsNullOrWhiteSpace(raw)) return;

            BuildKeyViewerImageOverlayIfNeeded();
            BuildKeyViewerTextOverlayIfNeeded();
            DestroyKvImageChildren();
            DestroyKvTextChildren();

            try
            {
                JObject root = JObject.Parse(raw);

                JToken sel = root["selectedKeyType"];
                if (JNotNull(sel) && sel.Type == JTokenType.String)
                {
                    tab = sel.ToString();
                    settings.keyViewerSelectedTab = tab;
                }

                JObject keysTable = root["keys"] as JObject;
                JObject posTable = (root["keyPositions"] as JObject) ?? (root["positions"] as JObject);
                if (keysTable == null || posTable == null)
                {
                    mod?.Logger?.Log("[KeyViewer] preset missing 'keys' or 'keyPositions' object at root.");
                    lastParsedPresetJson = raw;
                    lastParsedTab = tab;
                    return;
                }

                JArray keyArr = keysTable[tab] as JArray;
                JArray posArr = posTable[tab] as JArray;
                if (keyArr == null || posArr == null)
                {
                    string availableKeys = string.Join(",", keysTable.Properties().Select(pp => pp.Name).ToArray());
                    string availablePos = string.Join(",", posTable.Properties().Select(pp => pp.Name).ToArray());
                    mod?.Logger?.Log("[KeyViewer] tab '" + tab + "' missing. Available keys=[" + availableKeys + "] positions=[" + availablePos + "]");
                    lastParsedPresetJson = raw;
                    lastParsedTab = tab;
                    return;
                }

                int n = Mathf.Min(keyArr.Count, posArr.Count);
                float canvasW = 0f, canvasH = 0f;
                for (int i = 0; i < n; i++)
                {
                    JObject p = posArr[i] as JObject;
                    if (p == null) continue;
                    if (JBool(p, "hidden", false)) continue;
                    KvKey k = new KvKey();
                    k.keyName = keyArr[i].ToString();
                    k.keyCode = ResolveKeyCode(k.keyName);
                    k.dx = JFloat(p, "dx", 0f);
                    k.dy = JFloat(p, "dy", 0f);
                    k.width = JFloat(p, "width", 60f);
                    k.height = JFloat(p, "height", 60f);
                    string noteHex = JStr(p, "noteColor", "#FFFFFF");
                    float noteOp = JFloat(p, "noteOpacity", 80f) / 100f;
                    k.noteColor = HexToColor(noteHex, noteOp);
                    string bgHex = JStr(p, "backgroundColor", "#3C3C3C");
                    k.bgColor = HexToColor(bgHex, 0.5f);
                    k.activeBgColor = HexToColor(JStr(p, "activeBackgroundColor", bgHex), 0.5f);
                    if (JBool(p, "idleTransparent", false)) k.bgColor.a = 0f;
                    if (JBool(p, "activeTransparent", false)) k.activeBgColor.a = 0f;
                    k.borderColor = HexToColor(JStr(p, "borderColor", "#FFFFFF"), 0.4f);
                    k.activeBorderColor = HexToColor(JStr(p, "activeBorderColor", JStr(p, "borderColor", "#FFFFFF")), k.borderColor.a);
                    k.borderWidth = JFloat(p, "borderWidth", 4f);
                    k.borderRadius = JFloat(p, "borderRadius", 10f);

                    string dt = JOptionalString(p, "displayText");
                    k.displayText = !string.IsNullOrEmpty(dt) ? dt : DefaultDisplayFor(k.keyName);

                    k.noteWidth = JFloat(p, "noteWidth", 0f);
                    k.noteAlignment = JStr(p, "noteAlignment", "center");
                    k.noteEffectEnabled = JBool(p, "noteEffectEnabled", true);
                    k.noteGlowEnabled = JBool(p, "noteGlowEnabled", false);
                    k.noteGlowSize = JFloat(p, "noteGlowSize", 20f);
                    k.noteGlowOpacity = JFloat(p, "noteGlowOpacity", 70f) / 100f;
                    string glowHex = JStr(p, "noteGlowColor", noteHex);
                    k.noteGlowColor = HexToColor(glowHex, k.noteGlowOpacity);
                    k.noteAutoYCorrection = JBool(p, "noteAutoYCorrection", true);

                    k.count = PlayerPrefs.GetInt(KvCountKey(k.keyName), JInt(p, "count", 0));
                    string fontHex = JStr(p, "fontColor", "#FFFFFF");
                    k.fontColor = HexToColor(fontHex, 1f);
                    k.activeFontColor = HexToColor(JStr(p, "activeFontColor", fontHex), 1f);
                    k.fontSize = JInt(p, "fontSize", 18);

                    JObject counterObj = p["counter"] as JObject;
                    k.counterEnabled = counterObj != null ? JBool(counterObj, "enabled", true) : true;
                    k.counterFontSize = counterObj != null ? JInt(counterObj, "fontSize", Mathf.Max(8, Mathf.RoundToInt(k.fontSize * 0.85f))) : Mathf.Max(8, Mathf.RoundToInt(k.fontSize * 0.85f));
                    k.counterAlign = counterObj != null ? JStr(counterObj, "align", "bottom") : "bottom";
                    JObject counterFill = counterObj != null ? counterObj["fill"] as JObject : null;
                    string counterIdleHex = counterFill != null ? JStr(counterFill, "idle", fontHex) : fontHex;
                    string counterActiveHex = counterFill != null ? JStr(counterFill, "active", JStr(p, "activeFontColor", fontHex)) : JStr(p, "activeFontColor", fontHex);
                    k.counterColor = HexToColor(counterIdleHex, 1f);
                    k.activeCounterColor = HexToColor(counterActiveHex, 1f);

                    k.labelTmp = NewKvLabel(k.displayText, TextAlignmentOptions.Center);
                    if (k.counterEnabled)
                        k.counterTmp = NewKvLabel("", TextAlignmentOptions.Bottom);
                    else
                        k.counterTmp = null;

                    k.visualRoot = NewKeyVisualRoot("KVKey_" + i);
                    k.borderUi = NewKeyViewerRect("Border", k.visualRoot.transform);
                    k.fillUi = NewKeyViewerRect("Fill", k.visualRoot.transform);

                    // FIX: apply font immediately so labels never have null font
                    if (kvActiveFont != null)
                    {
                        k.labelTmp.font = kvActiveFont;
                        if (k.counterTmp != null) k.counterTmp.font = kvActiveFont;
                    }

                    keyViewerKeys.Add(k);

                    canvasW = Mathf.Max(canvasW, k.dx + k.width);
                    canvasH = Mathf.Max(canvasH, k.dy + k.height);
                }

                JObject statTable = root["statPositions"] as JObject;
                if (statTable != null)
                {
                    JArray statArr = statTable[tab] as JArray;
                    if (statArr != null)
                    {
                        for (int i = 0; i < statArr.Count; i++)
                        {
                            JObject p = statArr[i] as JObject;
                            if (p == null) continue;
                            if (JBool(p, "hidden", false)) continue;
                            KvKey k = new KvKey();
                            k.keyName = JStr(p, "statType", "stat");
                            k.keyCode = KeyCode.None;
                            k.dx = JFloat(p, "dx", 0f);
                            k.dy = JFloat(p, "dy", 0f);
                            k.width = JFloat(p, "width", 100f);
                            k.height = JFloat(p, "height", 30f);
                            k.noteColor = new Color(1f, 1f, 1f, 0f);
                            k.bgColor = HexToColor(JStr(p, "backgroundColor", "#3C3C3C"), 0.5f);
                            k.activeBgColor = k.bgColor;
                            if (JBool(p, "idleTransparent", false)) k.bgColor.a = 0f;
                            k.borderColor = HexToColor(JStr(p, "borderColor", "#FFFFFF"), 0.4f);
                            k.activeBorderColor = k.borderColor;
                            k.borderWidth = JFloat(p, "borderWidth", 4f);
                            k.borderRadius = JFloat(p, "borderRadius", 10f);

                            k.fontColor = HexToColor(JStr(p, "fontColor", "#FFFFFF"), 1f);
                            k.activeFontColor = k.fontColor;
                            k.fontSize = JInt(p, "fontSize", 16);
                            JObject counterObj = p["counter"] as JObject;
                            k.counterEnabled = counterObj != null ? JBool(counterObj, "enabled", true) : false;
                            k.counterFontSize = counterObj != null ? JInt(counterObj, "fontSize", k.fontSize) : k.fontSize;
                            k.counterAlign = counterObj != null ? JStr(counterObj, "align", "center") : "center";
                            JObject counterFill = counterObj != null ? counterObj["fill"] as JObject : null;
                            string counterIdleHex = counterFill != null ? JStr(counterFill, "idle", JStr(p, "fontColor", "#FFFFFF")) : JStr(p, "fontColor", "#FFFFFF");
                            string counterActiveHex = counterFill != null ? JStr(counterFill, "active", counterIdleHex) : counterIdleHex;
                            k.counterColor = HexToColor(counterIdleHex, 1f);
                            k.activeCounterColor = HexToColor(counterActiveHex, 1f);

                            string statLabel = k.keyName.Equals("kps", StringComparison.OrdinalIgnoreCase) ? "KPS" :
                                               k.keyName.Equals("total", StringComparison.OrdinalIgnoreCase) ? "Total" : k.keyName.ToUpperInvariant();
                            string jsonDisplay = JOptionalString(p, "displayText");
                            k.hasCustomDisplayText = !string.IsNullOrEmpty(jsonDisplay);
                            if (k.hasCustomDisplayText)
                                k.displayText = jsonDisplay;
                            else
                                k.displayText = k.counterEnabled ? statLabel : "0  " + statLabel;

                            k.count = -1;
                            k.labelTmp = NewKvLabel(k.displayText, TextAlignmentOptions.Center);
                            if (k.counterEnabled)
                                k.counterTmp = NewKvLabel("", TextAlignmentOptions.Center);
                            if (kvActiveFont != null) k.labelTmp.font = kvActiveFont;
                            if (kvActiveFont != null && k.counterTmp != null) k.counterTmp.font = kvActiveFont;
                            k.visualRoot = NewKeyVisualRoot("KVStat_" + i);
                            k.borderUi = NewKeyViewerRect("Border", k.visualRoot.transform);
                            k.fillUi = NewKeyViewerRect("Fill", k.visualRoot.transform);
                            keyViewerKeys.Add(k);
                            canvasW = Mathf.Max(canvasW, k.dx + k.width);
                            canvasH = Mathf.Max(canvasH, k.dy + k.height);
                        }
                    }
                }

                if (canvasW > 0f) keyViewerCanvasWidth = canvasW + 40f;
                if (canvasH > 0f) keyViewerCanvasHeight = canvasH + (settings.KeyViewerNoteEffect ? settings.KeyViewerTrackHeight : 0f) + 40f;
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[KeyViewer] Parse failed: " + ex.Message);
                keyViewerKeys = new List<KvKey>();
            }

            ApplyFontToKeyViewer();
            SortKeyViewerVisualLayers();

            lastParsedPresetJson = raw;
            lastParsedTab = tab;
            mod?.Logger?.Log("[KeyViewer] Built " + keyViewerKeys.Count + " items for tab '" + tab + "' canvas=" + keyViewerCanvasWidth + "x" + keyViewerCanvasHeight);
        }

        private static void SortKeyViewerVisualLayers()
        {
            if (keyViewerKeys == null) return;
            KvKey[] ordered = keyViewerKeys
                .Where(k => k != null && k.visualRoot != null)
                .OrderBy(k => k.dy)
                .ThenBy(k => keyViewerKeys.IndexOf(k))
                .ToArray();

            for (int i = 0; i < ordered.Length; i++)
                ordered[i].visualRoot.transform.SetSiblingIndex(i);
        }

        private static void EnsureKeyViewerLayout()
        {
            string raw = settings.keyViewerPresetJson;
            string tab = settings.keyViewerSelectedTab;
            if (keyViewerKeys == null || raw != lastParsedPresetJson || tab != lastParsedTab)
            {
                RebuildKeyViewerLayout();
            }
        }

        private const int MAX_NOTES_PER_KEY = 256;

        private static void UpdateKeyViewerKeyImages(KvKey k, Rect keyRect, bool pressed, float scale)
        {
            if (k == null || k.fillUi == null) return;

            float scaledRadius = Mathf.Min(Mathf.Max(0f, k.borderRadius * scale), KvMaxCornerRadiusPx);
            bool showBorder = k.borderUi != null && k.borderWidth > 0.5f && Mathf.Max(k.borderColor.a, k.activeBorderColor.a) > 0f;
            if (showBorder)
            {
                float keyMin = Mathf.Min(keyRect.width, keyRect.height);
                float adaptiveBorder = Mathf.Clamp(k.borderWidth * (keyMin / 60f), 1f, keyMin * 0.12f);
                Color borderColor = pressed ? k.activeBorderColor : k.borderColor;
                PlaceKeyRect(k.borderUi, keyRect, borderColor, scaledRadius, adaptiveBorder);

                Rect fillRect = new Rect(
                    keyRect.x + adaptiveBorder,
                    keyRect.y + adaptiveBorder,
                    Mathf.Max(0f, keyRect.width - adaptiveBorder * 2f),
                    Mathf.Max(0f, keyRect.height - adaptiveBorder * 2f)
                );
                PlaceKeyRect(k.fillUi, fillRect, pressed ? k.activeBgColor : k.bgColor, Mathf.Max(0f, scaledRadius - adaptiveBorder));
            }
            else
            {
                if (k.borderUi != null && k.borderUi.rounded != null)
                    k.borderUi.rounded.enabled = false;
                PlaceKeyRect(k.fillUi, keyRect, pressed ? k.activeBgColor : k.bgColor, scaledRadius);
            }
        }

        private static void DrawKeyViewer()
        {
            LoadKeyViewerTotalIfNeeded();
            EnsureKeyViewerLayout();
            FlushKvSaveIfDue();

            if (keyViewerKeys == null || keyViewerKeys.Count == 0)
            {
                if (kvImageRoot != null) kvImageRoot.SetActive(false);
                if (kvTextRoot != null) kvTextRoot.SetActive(false);
                return;
            }
            if (kvImageRoot != null) kvImageRoot.SetActive(true);
            if (kvTextRoot != null) kvTextRoot.SetActive(true);
            BeginKeyViewerImageFrame();

            // FIX: re-apply font every frame in case bundle loaded after layout built
            ApplyFontToKeyViewer();

            float scale = Mathf.Clamp(settings.KeyViewerScale, 0.2f, 4f);
            float originX = settings.KeyViewerOffsetX;
            float originY = (Screen.height - keyViewerCanvasHeight * scale) + settings.KeyViewerOffsetY;

            float now = Time.unscaledTime;
            bool reverse = settings.KeyViewerNoteReverse;
            float speed = Mathf.Max(1f, settings.KeyViewerNoteSpeed) * scale;
            float trackH = Mathf.Max(0f, settings.KeyViewerTrackHeight) * scale;

            float autoTopY = float.MaxValue;
            float autoBottomY = float.MinValue;

            for (int i = 0; i < keyViewerKeys.Count; i++)
            {
                var k = keyViewerKeys[i];
                if (k.count == -1) continue;

                float y = originY + k.dy * scale;
                float yMax = y + k.height * scale;

                if (y < autoTopY) autoTopY = y;
                if (yMax > autoBottomY) autoBottomY = yMax;
            }

            int n = keyViewerKeys.Count;
            int[] renderOrder = new int[n];
            for (int i = 0; i < n; i++) renderOrder[i] = i;
            Array.Sort(renderOrder, (a, b) =>
            {
                float da = keyViewerKeys[a].dy;
                float db = keyViewerKeys[b].dy;
                if (da < db) return -1;
                if (da > db) return 1;
                return a - b;
            });

            for (int oi = 0; oi < n; oi++)
            {
                int i = renderOrder[oi];
                KvKey k = keyViewerKeys[i];
                bool isStat = k.count == -1;
                bool pressed = !isStat && k.keyCode != KeyCode.None && KvIsKeyPressed(k.keyCode);

                if (!isStat)
                {
                    if (pressed && !k.wasPressed)
                    {
                        if (k.noteStartTimes.Count > MAX_NOTES_PER_KEY)
                        {
                            k.noteStartTimes.RemoveAt(0);
                            k.noteEndTimes.RemoveAt(0);
                        }

                        k.noteStartTimes.Add(now);
                        k.noteEndTimes.Add(-1f);

                        k.count++;
                        keyViewerTotalPresses++;
                        keyViewerPressLog.Add(now);

                        PlayerPrefs.SetInt(KvCountKey(k.keyName), k.count);
                        PlayerPrefs.SetInt(KvTotalPrefKey, keyViewerTotalPresses);
                        ScheduleKvSave();
                    }
                    else if (!pressed && k.wasPressed)
                    {
                        int last = k.noteEndTimes.Count - 1;
                        if (last >= 0 && k.noteEndTimes[last] < 0f)
                            k.noteEndTimes[last] = now;
                    }

                    k.wasPressed = pressed;
                }
                else
                {
                    int prune = 0;
                    while (prune < keyViewerPressLog.Count && keyViewerPressLog[prune] < now - KvKpsWindow)
                        prune++;

                    if (prune > 0)
                        keyViewerPressLog.RemoveRange(0, prune);

                    int kps = keyViewerPressLog.Count;

                    if (k.keyName.Equals("kps", StringComparison.OrdinalIgnoreCase))
                        k.statValue = kps;
                    else if (k.keyName.Equals("total", StringComparison.OrdinalIgnoreCase))
                        k.statValue = keyViewerTotalPresses;
                    else
                        k.statValue = 0;

                    if (!k.counterEnabled && !k.hasCustomDisplayText)
                    {
                        if (k.keyName.Equals("kps", StringComparison.OrdinalIgnoreCase))
                            k.displayText = kps + "  KPS";
                        else if (k.keyName.Equals("total", StringComparison.OrdinalIgnoreCase))
                            k.displayText = keyViewerTotalPresses + "  Total";
                    }
                }

                Rect keyRect = new Rect(
                    originX + k.dx * scale,
                    originY + k.dy * scale,
                    k.width * scale,
                    k.height * scale
                );

                if (!isStat && settings.KeyViewerNoteEffect && k.noteEffectEnabled && trackH > 0f)
                {
                    float noteWidth = (k.noteWidth > 0f ? k.noteWidth * scale : keyRect.width);

                    float noteX =
                        k.noteAlignment.Equals("left", StringComparison.OrdinalIgnoreCase) ? keyRect.x :
                        k.noteAlignment.Equals("right", StringComparison.OrdinalIgnoreCase) ? keyRect.xMax - noteWidth :
                        keyRect.x + (keyRect.width - noteWidth) * 0.5f;

                    float baseY = k.noteAutoYCorrection
                        ? (reverse ? autoBottomY : autoTopY)
                        : (reverse ? keyRect.yMax : keyRect.y);

                    int write = 0;
                    int count = k.noteStartTimes.Count;

                    for (int j = 0; j < count; j++)
                    {
                        float start = k.noteStartTimes[j];
                        float end = k.noteEndTimes[j];

                        float lead = (now - start) * speed;
                        float trail = (end < 0f) ? 0f : (now - end) * speed;

                        float height = lead - trail;

                        if (height <= 0.5f)
                        {
                            k.noteStartTimes[write] = start;
                            k.noteEndTimes[write] = end;
                            write++;
                            continue;
                        }

                        if (trail > trackH + 8f)
                            continue;

                        float drawH = Mathf.Min(height, trackH);

                        Rect nRect = reverse
                            ? new Rect(noteX, baseY + trail, noteWidth, drawH)
                            : new Rect(noteX, baseY - drawH - trail, noteWidth, drawH);

                        if (nRect.height > 0.5f)
                        {
                            if (settings.KeyViewerFadePx > 0.5f)
                                EmitNoteWithFade(nRect, k.noteColor, baseY, trackH, settings.KeyViewerFadePx, reverse);
                            else
                                EmitNoteRect(nRect, k.noteColor, 2f);
                        }

                        k.noteStartTimes[write] = start;
                        k.noteEndTimes[write] = end;
                        write++;
                    }

                    if (write < count)
                    {
                        k.noteStartTimes.RemoveRange(write, count - write);
                        k.noteEndTimes.RemoveRange(write, count - write);
                    }
                }

                UpdateKeyViewerKeyImages(k, keyRect, pressed, scale);

                // ---------- TMP TEXT UPDATE ----------
                int fs = Mathf.Max(8, Mathf.RoundToInt(k.fontSize * scale));

                bool showCounterForThisKey = settings.KeyViewerShowCounter && k.counterEnabled;

                if (k.labelTmp != null)
                {
                    k.labelTmp.color = pressed ? k.activeFontColor : k.fontColor;
                    k.labelTmp.fontSize = fs;
                    k.labelTmp.text = k.displayText;

                    var rt = k.labelTmp.rectTransform;
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);

                    if (isStat && showCounterForThisKey)
                    {
                        float pad = Mathf.Min(keyRect.width * 0.12f, Mathf.Max(8f, 16f * scale));
                        k.labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
                        rt.anchoredPosition = new Vector2(keyRect.x + pad, -keyRect.y);
                        rt.sizeDelta = new Vector2(Mathf.Max(0f, keyRect.width - pad * 2f), keyRect.height);
                    }
                    else if (showCounterForThisKey)
                    {
                        k.labelTmp.alignment = TextAlignmentOptions.Top;
                        rt.anchoredPosition = new Vector2(keyRect.x, -(keyRect.y + 4f * scale));
                        rt.sizeDelta = new Vector2(keyRect.width, keyRect.height - 4f * scale);
                    }
                    else
                    {
                        k.labelTmp.alignment = TextAlignmentOptions.Center;
                        rt.anchoredPosition = new Vector2(keyRect.x, -keyRect.y);
                        rt.sizeDelta = new Vector2(keyRect.width, keyRect.height);
                    }
                    k.labelTmp.enabled = true;
                }

                if (k.counterTmp != null)
                {
                    k.counterTmp.enabled = showCounterForThisKey;
                    if (showCounterForThisKey)
                    {
                        int csize = Mathf.Max(8, Mathf.RoundToInt((k.counterFontSize > 0 ? k.counterFontSize : k.fontSize) * scale));
                        k.counterTmp.fontSize = csize;
                        k.counterTmp.color = pressed ? k.activeCounterColor : k.counterColor;
                        k.counterTmp.text = isStat ? k.statValue.ToString() : k.count.ToString();
                        k.counterTmp.alignment = KvCounterAlignment(k.counterAlign);

                        var rt = k.counterTmp.rectTransform;
                        rt.anchorMin = new Vector2(0f, 1f);
                        rt.anchorMax = new Vector2(0f, 1f);
                        rt.pivot = new Vector2(0f, 1f);
                        if (isStat)
                        {
                            float pad = Mathf.Min(keyRect.width * 0.12f, Mathf.Max(8f, 16f * scale));
                            rt.anchoredPosition = new Vector2(keyRect.x + pad, -keyRect.y);
                            rt.sizeDelta = new Vector2(Mathf.Max(0f, keyRect.width - pad * 2f), keyRect.height);
                        }
                        else
                        {
                            rt.anchoredPosition = new Vector2(keyRect.x, -keyRect.y);
                            rt.sizeDelta = new Vector2(keyRect.width, keyRect.height);
                        }
                    }
                }
            }

            EndKeyViewerImageFrame();
        }

        private static void ImportKeyViewerPreset()
        {
            string picked = PickPresetJsonFile();
            if (string.IsNullOrEmpty(picked)) return;
            try
            {
                string txt = File.ReadAllText(picked);
                JObject.Parse(txt);
                settings.keyViewerPresetJson = txt;
                keyViewerKeys = null;
                mod?.Logger?.Log("[KeyViewer] Imported preset from " + picked);
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[KeyViewer] Import failed: " + ex.Message);
            }
        }

        private static string PickPresetJsonFile()
        {
            try
            {
                SFB.ExtensionFilter[] filters = new[]
                {
                    new SFB.ExtensionFilter("JSON Preset", "json"),
                    new SFB.ExtensionFilter("All Files", "*")
                };
                string[] picked = SFB.StandaloneFileBrowser.OpenFilePanel("Select DM Note preset", "", filters, false);
                if (picked == null || picked.Length == 0) return null;
                string path = picked[0];
                return string.IsNullOrEmpty(path) ? null : path;
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[KeyViewer] Picker failed: " + ex.Message);
                return null;
            }
        }

        internal static void HideKeyViewer()
        {
            if (kvImageRoot != null) kvImageRoot.SetActive(false);
            if (kvTextRoot != null) kvTextRoot.SetActive(false);
        }

        internal static void ShowKeyViewer()
        {
            if (kvImageRoot != null) kvImageRoot.SetActive(true);
            if (kvTextRoot != null) kvTextRoot.SetActive(true);
        }

        internal static void DestroyKeyViewer()
        {
            try
            {
                if (kvImageRoot != null) UnityEngine.Object.Destroy(kvImageRoot);
                if (kvTextRoot != null) UnityEngine.Object.Destroy(kvTextRoot);
            }
            catch { }

            kvImageRoot = null;
            kvImageCanvas = null;
            kvNotesLayer = null;
            kvKeysLayer = null;
            kvImageBuilt = false;
            kvNoteImagePool.Clear();
            kvNoteImageCursor = 0;

            kvTextRoot = null;
            kvTextCanvas = null;
            kvTextBuilt = false;
            kvActiveFont = null;
            kvActiveFontName = null;

            if (keyViewerKeys != null)
            {
                foreach (var k in keyViewerKeys)
                {
                    k.labelTmp = null;
                    k.counterTmp = null;
                    k.visualRoot = null;
                    k.borderUi = null;
                    k.fillUi = null;
                }
            }
            keyViewerKeys = null;
            lastParsedPresetJson = null;
            lastParsedTab = null;
        }
    }
}
