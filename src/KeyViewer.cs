using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
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
            public int count;
            public List<float> noteStartTimes = new List<float>(); // Time at which key pressed for note rain
            public List<float> noteEndTimes = new List<float>();   // Time at which key released; -1 means still held
            public bool wasPressed;
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

        private static Texture2D fadeTexNonReverse;
        private static Texture2D fadeTexReverse;

        private static Texture2D GetFadeTex(bool reverse)
        {
            if (!reverse && fadeTexNonReverse != null) return fadeTexNonReverse;
            if (reverse && fadeTexReverse != null) return fadeTexReverse;

            int N = 64;
            Texture2D t = new Texture2D(1, N, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            Color32[] px = new Color32[N];
            for (int i = 0; i < N; i++)
            {
                // Texture index 0 = bottom row, N-1 = top row.
                // GUI.DrawTexture maps texture top to rect top.
                // Non-reverse: rect TOP = transparent → texture top row alpha 0 → index N-1 alpha=0; bottom row alpha 1 → index 0 alpha 255
                // Reverse: rect BOTTOM transparent → texture bottom row alpha 0 → index 0 alpha 0; top row alpha 1 → index N-1 alpha 255
                byte a;
                if (!reverse) a = (byte)(255 - (i * 255) / (N - 1));
                else a = (byte)((i * 255) / (N - 1));
                px[i] = new Color32(255, 255, 255, a);
            }
            t.SetPixels32(px);
            t.Apply();
            if (!reverse) { fadeTexNonReverse = t; return fadeTexNonReverse; }
            fadeTexReverse = t;
            return fadeTexReverse;
        }

        private static void DrawNoteWithFade(Rect nRect, Color noteColor, float noteBaseY, float trackH, float fadePx, bool reverse)
        {
            if (fadePx <= 0f)
            {
                DrawRoundedRect(nRect, noteColor, 0f);
                return;
            }

            // Track bounds
            float trackTop = reverse ? noteBaseY : (noteBaseY - trackH);
            float trackBot = reverse ? (noteBaseY + trackH) : noteBaseY;

            // Fade band (leading edge)
            float fadeBandStart = reverse ? (trackBot - fadePx) : trackTop;
            float fadeBandEnd   = reverse ? trackBot : (trackTop + fadePx);

            // Clamp fade band to track
            fadeBandStart = Mathf.Clamp(fadeBandStart, trackTop, trackBot);
            fadeBandEnd   = Mathf.Clamp(fadeBandEnd, trackTop, trackBot);

            // Intersection with note rect
            float gradTop = Mathf.Max(nRect.y, fadeBandStart);
            float gradBot = Mathf.Min(nRect.yMax, fadeBandEnd);

            Texture2D tex = GetFadeTex(reverse);

            // --- DRAW GRADIENT PART ---
            if (gradBot > gradTop)
            {
                float a1 = Mathf.Clamp01((gradTop - fadeBandStart) / fadePx);
                float a2 = Mathf.Clamp01((gradBot - fadeBandStart) / fadePx);

                // Convert to UV (Unity textures are bottom-left origin)
                Rect uv = new Rect(0f, 1f - a2, 1f, a2 - a1);

                Color old = GUI.color;
                GUI.color = noteColor;

                GUI.DrawTextureWithTexCoords(
                    new Rect(nRect.x, gradTop, nRect.width, gradBot - gradTop),
                    tex,
                    uv
                );

                GUI.color = old;
            }

            // --- DRAW SOLID PART ---
            if (!reverse)
            {
                float solidTop = Mathf.Max(nRect.y, fadeBandEnd);
                if (nRect.yMax > solidTop)
                {
                    DrawRoundedRect(
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
                    DrawRoundedRect(
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
            // Persist total survives reset; only clear KPS rolling window
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
            // Letters
            for (char c = 'A'; c <= 'Z'; c++)
            {
                KeyCode kc;
                if (Enum.TryParse<KeyCode>(c.ToString(), true, out kc))
                    m[c.ToString()] = kc;
            }
            // Digits 0-9
            for (int i = 0; i <= 9; i++) m[i.ToString()] = (KeyCode)Enum.Parse(typeof(KeyCode), "Alpha" + i);
            // Common modifiers
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
            // DM Note Mac virtual keycodes (numeric) → approximate
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

        private static Color HexToColor(string hex, float alpha)
        {
            if (string.IsNullOrEmpty(hex)) return new Color(1f, 1f, 1f, alpha);
            string s = hex.Trim();
            try
            {
                // CSS keyword
                if (string.Equals(s, "transparent", StringComparison.OrdinalIgnoreCase))
                {
                    return new Color(0f, 0f, 0f, 0f);
                }

                // CSS rgb()/rgba() — alpha in source is verbatim, ignores `alpha` arg
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

                // Hex with or without leading '#'
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
                    // Explicit RGBA in hex overrides the default alpha verbatim
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
                {
                    return Mathf.Clamp01(pct / 100f);
                }
                return 1f;
            }
            float v;
            if (float.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v))
            {
                return Mathf.Clamp01(v / scale);
            }
            return 1f;
        }

        private static float ParseAlphaComponent(string s)
        {
            string t = s.Trim();
            if (t.EndsWith("%"))
            {
                float pct;
                if (float.TryParse(t.TrimEnd('%').Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pct))
                {
                    return Mathf.Clamp01(pct / 100f);
                }
                return 1f;
            }
            float v;
            if (float.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v))
            {
                // CSS spec: alpha is 0-1; tolerate 0-255 fallback
                return v <= 1f ? Mathf.Clamp01(v) : Mathf.Clamp01(v / 255f);
            }
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

        private static void RebuildKeyViewerLayout()
        {
            keyViewerKeys = new List<KvKey>();
            string raw = settings.keyViewerPresetJson;
            string tab = string.IsNullOrEmpty(settings.keyViewerSelectedTab) ? "4key" : settings.keyViewerSelectedTab;
            if (string.IsNullOrWhiteSpace(raw)) return;

            try
            {
                JObject root = JObject.Parse(raw);

                JObject noteSettings = root["noteSettings"] as JObject;
                if (noteSettings != null)
                {
                    settings.KeyViewerNoteSpeed = JFloat(noteSettings, "speed", settings.KeyViewerNoteSpeed);
                    settings.KeyViewerTrackHeight = JFloat(noteSettings, "trackHeight", settings.KeyViewerTrackHeight);
                    settings.KeyViewerNoteReverse = JBool(noteSettings, "reverse", settings.KeyViewerNoteReverse);
                }
                settings.KeyViewerNoteEffect = JBool(root, "noteEffect", settings.KeyViewerNoteEffect);

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
                    // DM Note: activeBackgroundColor falls back to backgroundColor when null (NOT noteColor)
                    k.activeBgColor = HexToColor(JStr(p, "activeBackgroundColor", bgHex), 0.5f);
                    k.borderColor = HexToColor(JStr(p, "borderColor", "#FFFFFF"), 0.4f);
                    k.borderWidth = JFloat(p, "borderWidth", 2f);
                    k.borderRadius = JFloat(p, "borderRadius", 10f);
                    k.displayText = JStr(p, "displayText", DefaultDisplayFor(k.keyName));
                    k.noteWidth = JFloat(p, "noteWidth", 0f);
                    k.noteAlignment = JStr(p, "noteAlignment", "center");
                    k.noteEffectEnabled = JBool(p, "noteEffectEnabled", true);
                    k.noteGlowEnabled = JBool(p, "noteGlowEnabled", false);
                    k.noteGlowSize = JFloat(p, "noteGlowSize", 20f);
                    k.noteGlowOpacity = JFloat(p, "noteGlowOpacity", 70f) / 100f;
                    string glowHex = JStr(p, "noteGlowColor", noteHex);
                    k.noteGlowColor = HexToColor(glowHex, k.noteGlowOpacity);
                    k.noteAutoYCorrection = JBool(p, "noteAutoYCorrection", true);

                    // Persistent count load
                    k.count = PlayerPrefs.GetInt(KvCountKey(k.keyName), JInt(p, "count", 0));
                    string fontHex = JStr(p, "fontColor", "#FFFFFF");
                    k.fontColor = HexToColor(fontHex, 1f);
                    // DM Note: activeFontColor falls back to fontColor when null
                    k.activeFontColor = HexToColor(JStr(p, "activeFontColor", fontHex), 1f);
                    k.fontSize = JInt(p, "fontSize", 18);
                    keyViewerKeys.Add(k);

                    canvasW = Mathf.Max(canvasW, k.dx + k.width);
                    canvasH = Mathf.Max(canvasH, k.dy + k.height);
                }

                // Stat positions (KPS / Total) — render as static labels
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
                            k.borderColor = HexToColor(JStr(p, "borderColor", "#FFFFFF"), 0.4f);
                            k.borderWidth = JFloat(p, "borderWidth", 2f);
                            k.borderRadius = JFloat(p, "borderRadius", 10f);
                            string statLabel = k.keyName.Equals("kps", StringComparison.OrdinalIgnoreCase) ? "KPS" :
                                               k.keyName.Equals("total", StringComparison.OrdinalIgnoreCase) ? "Total" : k.keyName.ToUpperInvariant();
                            k.displayText = "0  " + statLabel;
                            k.fontColor = HexToColor(JStr(p, "fontColor", "#FFFFFF"), 1f);
                            k.activeFontColor = k.fontColor;
                            k.fontSize = JInt(p, "fontSize", 16);
                            k.count = -1; // marker for stat row, draw differently
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

            lastParsedPresetJson = raw;
            lastParsedTab = tab;
            mod?.Logger?.Log("[KeyViewer] Built " + keyViewerKeys.Count + " items for tab '" + tab + "' canvas=" + keyViewerCanvasWidth + "x" + keyViewerCanvasHeight);
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

        private static void DrawKeyViewer()
        {
            LoadKeyViewerTotalIfNeeded();
            EnsureKeyViewerLayout();
            FlushKvSaveIfDue();
            if (keyViewerKeys == null || keyViewerKeys.Count == 0) return;

            EnsurePercentStyle();

            float scale = Mathf.Clamp(settings.KeyViewerScale, 0.2f, 4f);
            float originX = settings.KeyViewerOffsetX;
            float originY = (Screen.height - keyViewerCanvasHeight * scale) + settings.KeyViewerOffsetY;

            float now = Time.unscaledTime;
            bool reverse = settings.KeyViewerNoteReverse;
            float speed = Mathf.Max(1f, settings.KeyViewerNoteSpeed) * scale;
            float trackH = Mathf.Max(0f, settings.KeyViewerTrackHeight) * scale;

            // --- AUTO Y ALIGN ---
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

            int oldDepth = GUI.depth;
            GUI.depth = -10000;

            // Render order: ascending dy so bottom-row keys (and their notes) draw last,
            // appearing above top-row keys when notes cross row boundaries.
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

                // --- INPUT ---
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
                    // --- STAT UPDATE ---
                    int prune = 0;
                    while (prune < keyViewerPressLog.Count && keyViewerPressLog[prune] < now - KvKpsWindow)
                        prune++;

                    if (prune > 0)
                        keyViewerPressLog.RemoveRange(0, prune);

                    int kps = keyViewerPressLog.Count;

                    if (k.keyName.Equals("kps", StringComparison.OrdinalIgnoreCase))
                        k.displayText = kps + "  KPS";
                    else if (k.keyName.Equals("total", StringComparison.OrdinalIgnoreCase))
                        k.displayText = keyViewerTotalPresses + "  Total";
                }

                Rect keyRect = new Rect(
                    originX + k.dx * scale,
                    originY + k.dy * scale,
                    k.width * scale,
                    k.height * scale
                );

                // --- NOTES ---
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
                                DrawNoteWithFade(nRect, k.noteColor, baseY, trackH, settings.KeyViewerFadePx, reverse);
                            else
                                DrawRoundedRect(nRect, k.noteColor, 2f);
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

                // Base key
                DrawRoundedRect(keyRect, pressed ? k.activeBgColor : k.bgColor, k.borderRadius);

                // WHITE OVERLAY (this is the glow you’re missing)
                if (pressed && !isStat)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.18f);
                    GUI.DrawTexture(keyRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                // Border AFTER overlay
                if (k.borderWidth > 0.5f)
                {
                    float keyMin = Mathf.Min(keyRect.width, keyRect.height);
                    float adaptiveBorder = Mathf.Clamp(k.borderWidth * (keyMin / 60f), 1f, keyMin * 0.12f);
                    DrawRoundedRing(keyRect, k.borderColor, k.borderRadius, adaptiveBorder);
                }

                // --- TEXT ---
                int fs = Mathf.Max(8, Mathf.RoundToInt(k.fontSize * scale));

                Color savedColor = percentStyle.normal.textColor;
                int savedSize = percentStyle.fontSize;
                TextAnchor savedAlign = percentStyle.alignment;

                percentStyle.fontSize = fs;
                percentStyle.wordWrap = false;
                percentStyle.clipping = TextClipping.Clip;

                percentStyle.alignment = settings.KeyViewerShowCounter
                    ? TextAnchor.UpperCenter
                    : TextAnchor.MiddleCenter;

                percentStyle.normal.textColor = pressed ? k.activeFontColor : k.fontColor;

                Rect labelRect = settings.KeyViewerShowCounter
                    ? new Rect(keyRect.x, keyRect.y + 4f, keyRect.width, keyRect.height)
                    : keyRect;

                GUI.Label(labelRect, k.displayText, percentStyle);

                // --- COUNTER ---
                if (settings.KeyViewerShowCounter && !isStat)
                {
                    int csize = Mathf.Max(8, Mathf.RoundToInt(k.fontSize * scale * 0.85f));

                    percentStyle.fontSize = csize;
                    percentStyle.alignment = TextAnchor.LowerCenter;

                    Rect cRect = new Rect(keyRect.x, keyRect.y, keyRect.width, keyRect.height - 4f);
                    GUI.Label(cRect, k.count.ToString(), percentStyle);
                }

                percentStyle.normal.textColor = savedColor;
                percentStyle.fontSize = savedSize;
                percentStyle.alignment = savedAlign;
            }

            GUI.depth = oldDepth;
        }
        private static void ImportKeyViewerPreset()
        {
            string picked = PickPresetJsonFile();
            if (string.IsNullOrEmpty(picked)) return;
            try
            {
                string txt = File.ReadAllText(picked);
                // Validate JSON parse
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
                RuntimePlatform p = Application.platform;
                if (p == RuntimePlatform.OSXPlayer || p == RuntimePlatform.OSXEditor)
                {
                    return RunPickerProcess("osascript",
                        "-e 'POSIX path of (choose file with prompt \"Select DM Note preset\" of type {\"public.json\", \"public.text\", \"public.plain-text\"})'");
                }
                if (p == RuntimePlatform.WindowsPlayer || p == RuntimePlatform.WindowsEditor)
                {
                    return RunPickerProcess("powershell.exe",
                        "-NoProfile -ExecutionPolicy Bypass -Command \"" +
                        "Add-Type -AssemblyName System.Windows.Forms; " +
                        "$f = New-Object System.Windows.Forms.OpenFileDialog; " +
                        "$f.Title = 'Select DM Note preset'; " +
                        "$f.Filter = 'JSON (*.json)|*.json'; " +
                        "if ($f.ShowDialog() -eq 'OK') { Write-Output $f.FileName }\"");
                }
                return RunPickerProcess("zenity", "--file-selection --title=\"Select preset\" --file-filter=\"JSON | *.json\"");
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[KeyViewer] Picker failed: " + ex.Message);
                return null;
            }
        }

        private static string RunPickerProcess(string fileName, string args)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit(120000);
                    return string.IsNullOrEmpty(output) ? null : output;
                }
            }
            catch { return null; }
        }
    }
}
