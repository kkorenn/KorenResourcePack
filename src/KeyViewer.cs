using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityFileDialog;

namespace KorenResourcePack
{
    internal static class KeyViewer
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

        internal class KvUiRect
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            // One of `rounded` or `image` is non-null. `rounded` is the procedural
            // (mesh-based) graphic used for note rain and as a fallback when sprite assets
            // are missing. `image` is the Jipper-style sliced UnityEngine.UI.Image, used
            // for key fill/border when KeyBackground/KeyOutline sprites loaded from the
            // bundle.
            public KvRoundedImage rounded;
            public Image image;
        }

        internal class KvRoundedImage : MaskableGraphic
        {
            private readonly List<Vector2> outer = new List<Vector2>(64);
            private readonly List<Vector2> inner = new List<Vector2>(64);
            private float cornerRadius;
            private bool verticalGradient;
            private bool reverseGradient;
            private float ringThickness;
            // When true, OnPopulateMesh skips the 1.25 px anti-aliased outer ring entirely.
            // Used by the fade composite (gradient + solid) so the seam between the two
            // rects sits in fully-opaque interior on both sides — without this the AA bands
            // overlap near the seam and produce a visible horizontal line where the fade
            // ends.
            private bool noEdgeAA;

            public void SetShape(float radius, bool gradient, bool reverse, float borderThickness, bool noAA = false)
            {
                radius = Mathf.Max(0f, radius);
                borderThickness = Mathf.Max(0f, borderThickness);
                if (Mathf.Abs(cornerRadius - radius) < 0.01f &&
                    verticalGradient == gradient &&
                    reverseGradient == reverse &&
                    Mathf.Abs(ringThickness - borderThickness) < 0.01f &&
                    noEdgeAA == noAA)
                    return;

                cornerRadius = radius;
                verticalGradient = gradient;
                reverseGradient = reverse;
                ringThickness = borderThickness;
                noEdgeAA = noAA;
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

                // noEdgeAA collapses the AA ring into the geometry edge so every vertex
                // sits at edgeAlpha=1. Used by fade composite rects.
                float aa = noEdgeAA ? 0f : Mathf.Min(1.25f, rect.width * 0.25f, rect.height * 0.25f);
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
        internal class KvKey
        {
            public string keyName;
            public string countPrefKey;
            public KeyCode keyCode;
            public KeyCode ghostKeyCode;
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
            public int noteAlignmentMode; // -1 left, 0 center, 1 right
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
            public TextAlignmentOptions counterAlignment;
            public bool counterStackTop;
            public bool counterStackBottom;
            public int count;
            public int statValue;
            public List<float> noteStartTimes = new List<float>(); // Time at which key pressed for note rain
            public List<float> noteEndTimes = new List<float>();   // Time at which key released; -1 means still held
            public List<float> ghostNoteStartTimes = new List<float>();
            public List<float> ghostNoteEndTimes = new List<float>();
            public bool wasPressed;
            public bool wasGhostPressed;
            public bool ignoredPress;
            public bool ignoredGhostPress;
            public bool counterEnabled = true;
            public bool hasCustomDisplayText = false;
            public bool isStat;
            public bool isKps;
            public bool isTotal;
            public int lastCounterValue = int.MinValue;

            // Retained-mode text objects
            public TextMeshProUGUI labelTmp;
            public TextMeshProUGUI counterTmp;

            // Retained-mode image objects
            public GameObject visualRoot;
            public KvUiRect borderUi;
            public KvUiRect fillUi;
        }

        internal static List<KvKey> keyViewerKeys;
        private static string lastParsedPresetJson;
        private static string lastParsedTab;
        private static float keyViewerCanvasWidth = 800f;
        private static float keyViewerCanvasHeight = 250f;

        private static int keyViewerTotalPresses;
        private static readonly List<float> keyViewerPressLog = new List<float>();
        private const float KvKpsWindow = 1.0f;

        private static readonly HashSet<KeyCode> kvPressedKeys = new HashSet<KeyCode>();
        private static int[] kvRenderOrder;
        private static int kvRenderOrderCount;
        private static Rewired.Keyboard kvCachedKeyboard;
        private static bool kvKeyboardInitialized;

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

            // Both halves of the composite skip the 1.25 px AA outer ring (noAA=true via
            // PlaceKvGradient/SolidNoAARect) so the seam between them stays fully opaque.
            // The rects share an exact edge — no overlap needed — and the seam is invisible.
            if (!reverse)
            {
                if (gradBot > gradTop)
                    EmitNoteGradientRect(new Rect(nRect.x, gradTop, nRect.width, gradBot - gradTop), noteColor, reverse);

                float solidTop = Mathf.Max(nRect.y, fadeBandEnd);
                if (nRect.yMax > solidTop)
                    EmitNoteSolidNoAA(new Rect(nRect.x, solidTop, nRect.width, nRect.yMax - solidTop), noteColor);
            }
            else
            {
                if (gradBot > gradTop)
                    EmitNoteGradientRect(new Rect(nRect.x, gradTop, nRect.width, gradBot - gradTop), noteColor, reverse);

                float solidBot = Mathf.Min(nRect.yMax, fadeBandStart);
                if (solidBot > nRect.y)
                    EmitNoteSolidNoAA(new Rect(nRect.x, nRect.y, nRect.width, solidBot - nRect.y), noteColor);
            }
        }

        private static bool KvIsModifierKey(KeyCode kc)
        {
            switch (kc)
            {
                case KeyCode.LeftControl: case KeyCode.RightControl:
                case KeyCode.LeftShift:   case KeyCode.RightShift:
                case KeyCode.LeftAlt:     case KeyCode.RightAlt:
                case KeyCode.LeftCommand: case KeyCode.RightCommand:
                    return true;
            }
            return false;
        }

        private static bool KvIsKeyPressed(KeyCode kc)
        {
            if (!kvKeyboardInitialized)
            {
                kvKeyboardInitialized = true;
                try
                {
                    if (Rewired.ReInput.isReady)
                        kvCachedKeyboard = Rewired.ReInput.controllers.Keyboard;
                }
                catch { }
            }
            // For bare modifier keys (Ctrl/Shift/Alt/Cmd), Rewired's KeyCode-overload often
            // returns false because it only resolves keys bound to the player's controller
            // map. Skip straight to UnityEngine.Input which reads the OS state directly —
            // mirroring how OG keyviewers detect modifier presses.
            if (KvIsModifierKey(kc))
            {
                if (Input.GetKey(kc)) return true;
                return kvPressedKeys.Contains(kc);
            }
            if (kvCachedKeyboard != null)
            {
                try
                {
                    if (kvCachedKeyboard.GetKey(kc)) return true;
                }
                catch { kvCachedKeyboard = null; }
            }
            if (Input.GetKey(kc)) return true;
            return kvPressedKeys.Contains(kc);
        }

        private static bool KvApplyInputFilters(KeyCode key, bool rawPressed, bool wasPressed, ref bool ignoredPress)
        {
            if (!rawPressed)
            {
                ignoredPress = false;
                return false;
            }

            if (ignoredPress)
                return false;

            if (KeyLimiter.ShouldBlockKey(key))
            {
                ignoredPress = true;
                return false;
            }

            if (!wasPressed && !ChatterBlocker.AcceptKeyViewerPress(key))
            {
                ignoredPress = true;
                return false;
            }

            return true;
        }

        public static void ResetKeyViewerStats()
        {
            keyViewerPressLog.Clear();
        }

        private static string KvCountKey(string keyName) { return "kvkey_" + (keyName ?? ""); }
        private const string KvTotalPrefKey = "kvtotal";
        private static bool keyViewerTotalLoaded;

        // ---- Counter editing API used by SettingsGui ----
        // Returns the count currently held in PlayerPrefs for a given key name
        // (mirrors what the renderer sees on the next layout rebuild).
        internal static int GetKeyViewerCount(string keyName)
        {
            if (string.IsNullOrEmpty(keyName)) return 0;
            return PlayerPrefs.GetInt(KvCountKey(keyName), 0);
        }

        // Manually overwrite a key's persistent count. Affects the keyName-based
        // bucket so all styles/modes that share that name see the new value.
        internal static void SetKeyViewerCount(string keyName, int value)
        {
            if (string.IsNullOrEmpty(keyName)) return;
            PlayerPrefs.SetInt(KvCountKey(keyName), Mathf.Max(0, value));
            keyViewerKeys = null;
            ScheduleKvSave();
        }

        internal static int GetKeyViewerTotal()
        {
            LoadKeyViewerTotalIfNeeded();
            return keyViewerTotalPresses;
        }

        internal static void SetKeyViewerTotal(int value)
        {
            LoadKeyViewerTotalIfNeeded();
            keyViewerTotalPresses = Mathf.Max(0, value);
            PlayerPrefs.SetInt(KvTotalPrefKey, keyViewerTotalPresses);
            keyViewerKeys = null;
            ScheduleKvSave();
        }

        // Wipes every kvkey_* PlayerPref entry referenced by the current rendered keys
        // (or by the active simple-mode style if dmnote keys haven't loaded yet) and the total.
        internal static void ResetAllKeyViewerCounters()
        {
            if (keyViewerKeys != null)
            {
                foreach (KvKey k in keyViewerKeys)
                {
                    if (k != null && !string.IsNullOrEmpty(k.keyName))
                        PlayerPrefs.DeleteKey(k.countPrefKey ?? KvCountKey(k.keyName));
                }
            }
            // Catch keys not currently rendered in dmnote (other tabs) by also wiping the
            // simple-mode arrays — covers users who haven't opened the dmnote layout yet.
            for (int style = 0; style < 4; style++)
            {
                int[] codes = SimpleStyleCodes(style);
                for (int i = 0; i < codes.Length; i++)
                    PlayerPrefs.DeleteKey(KvCountKey(((KeyCode)codes[i]).ToString().ToUpperInvariant()));
            }
            for (int footStyle = 1; footStyle <= 5; footStyle++)
            {
                int[] codes = SimpleFootStyleCodes(footStyle);
                if (codes == null) continue;
                for (int i = 0; i < codes.Length; i++)
                    PlayerPrefs.DeleteKey(KvCountKey("simple_foot_" + i));
            }
            for (int i = 0; i < 20; i++)
                PlayerPrefs.DeleteKey(KvCountKey("simple_hand_" + i));
            // Same for DM Note preset keys across every tab in the saved JSON.
            string dmRaw = Main.settings.keyViewerPresetJson;
            if (!string.IsNullOrWhiteSpace(dmRaw))
            {
                try
                {
                    JObject root = JObject.Parse(dmRaw);
                    JObject keysTable = root["keys"] as JObject;
                    if (keysTable != null)
                    {
                        foreach (var prop in keysTable.Properties())
                        {
                            JArray arr = prop.Value as JArray;
                            if (arr == null) continue;
                            foreach (JToken t in arr)
                            {
                                if (t == null || t.Type != JTokenType.String) continue;
                                string name = t.ToString();
                                if (string.IsNullOrEmpty(name)) continue;
                                PlayerPrefs.DeleteKey(KvCountKey(name.ToUpperInvariant()));
                            }
                        }
                    }
                }
                catch { }
            }
            PlayerPrefs.DeleteKey(KvTotalPrefKey);
            keyViewerTotalPresses = 0;
            keyViewerTotalLoaded = true;
            keyViewerKeys = null;
            PlayerPrefs.Save();
        }

        // Returns the list of (keyName, count) pairs the user can edit. In dmnote mode
        // this comes from the parsed preset's keys; in simple mode it falls back to the
        // baked layout's KeyCode array so editing works even before the renderer warms up.
        internal static List<KeyValuePair<string, int>> EnumerateKeyViewerCounters()
        {
            List<KeyValuePair<string, int>> result = new List<KeyValuePair<string, int>>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (keyViewerKeys != null)
            {
                foreach (KvKey k in keyViewerKeys)
                {
                    if (k == null || string.IsNullOrEmpty(k.keyName)) continue;
                    if (k.isStat || k.isKps || k.isTotal) continue;
                    if (!k.counterEnabled) continue;
                    if (!seen.Add(k.keyName)) continue;
                    result.Add(new KeyValuePair<string, int>(k.keyName, k.count));
                }
            }
            if (string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase))
            {
                int[] codes = SimpleStyleCodes(Mathf.Clamp(Main.settings.KeyViewerSimpleStyle, 0, 3));
                for (int i = 0; i < codes.Length; i++)
                {
                    string name = ((KeyCode)codes[i]).ToString().ToUpperInvariant();
                    if (!seen.Add(name)) continue;
                    result.Add(new KeyValuePair<string, int>(name, GetKeyViewerCount(name)));
                }
            }
            else
            {
                // DM Note fallback: parse the saved preset directly so the counter editor works
                // even when the renderer hasn't built keyViewerKeys yet (Main.settings page opened
                // before the key viewer is shown).
                string raw = Main.settings.keyViewerPresetJson;
                string tab = string.IsNullOrEmpty(Main.settings.keyViewerSelectedTab) ? "4key" : Main.settings.keyViewerSelectedTab;
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    try
                    {
                        JObject root = JObject.Parse(raw);
                        JObject keysTable = root["keys"] as JObject;
                        JArray keysArr = keysTable != null ? keysTable[tab] as JArray : null;
                        if (keysArr != null)
                        {
                            foreach (JToken t in keysArr)
                            {
                                if (t == null || t.Type != JTokenType.String) continue;
                                string name = t.ToString();
                                if (string.IsNullOrEmpty(name)) continue;
                                name = name.ToUpperInvariant();
                                if (!seen.Add(name)) continue;
                                result.Add(new KeyValuePair<string, int>(name, GetKeyViewerCount(name)));
                            }
                        }
                    }
                    catch { }
                }
            }
            return result;
        }

        private static int[] SimpleStyleCodes(int style)
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

        private static int[] SimpleFootStyleCodes(int style)
        {
            switch (style)
            {
                case 1: return Main.settings.KeyViewerSimpleFootKey2;
                case 2: return Main.settings.KeyViewerSimpleFootKey4;
                case 3: return Main.settings.KeyViewerSimpleFootKey6;
                case 4: return Main.settings.KeyViewerSimpleFootKey8;
                case 5: return Main.settings.KeyViewerSimpleFootKey16;
                default: return null;
            }
        }

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

        private static int PruneKeyViewerPressLog(float now)
        {
            int prune = 0;
            float cutoff = now - KvKpsWindow;
            while (prune < keyViewerPressLog.Count && keyViewerPressLog[prune] < cutoff)
                prune++;

            if (prune > 0)
                keyViewerPressLog.RemoveRange(0, prune);

            return keyViewerPressLog.Count;
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

        private static int KvNoteAlignmentMode(string align)
        {
            if (string.Equals(align, "left", StringComparison.OrdinalIgnoreCase)) return -1;
            if (string.Equals(align, "right", StringComparison.OrdinalIgnoreCase)) return 1;
            return 0;
        }

        private static void CacheKeyLayoutModes(KvKey k)
        {
            if (k == null) return;
            k.noteAlignmentMode = KvNoteAlignmentMode(k.noteAlignment);
            k.counterAlignment = KvCounterAlignment(k.counterAlign);
            k.counterStackTop = string.Equals(k.counterAlign, "top", StringComparison.OrdinalIgnoreCase);
            k.counterStackBottom = string.Equals(k.counterAlign, "bottom", StringComparison.OrdinalIgnoreCase);
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

        // Build a sprite-backed UI rect (Jipper-style: UnityEngine.UI.Image with a sliced sprite
        // from the Koren asset bundle). Falls back to a procedural KvRoundedImage if the sprite is
        // missing (bundle not built / older bundle). Used for key fill and key outline.
        private static KvUiRect NewKvSpriteRect(string name, Transform parent, Sprite sprite)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            KvUiRect ui = new KvUiRect();
            ui.gameObject = go;
            ui.rectTransform = rt;

            if (sprite != null)
            {
                Image image = go.AddComponent<Image>();
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
                image.raycastTarget = false;
                image.enabled = true;
                // The bundled KeyBackground/KeyOutline sprites are 100x100 with an 11px nine-slice
                // border. With Image.Type.Sliced the corner thickness is rendered at the sprite's
                // native pixel size — at typical 60px key sizes that 11px outline reads as ~18%
                // of the key, much thicker than Jipper's preview. Jipper hides this by sizing the
                // RectTransform at 2x and localScale 0.5 so the border visually halves. Achieve
                // the same screen-space effect with pixelsPerUnitMultiplier = 2 (compresses the
                // sliced corners to ~5.5 effective pixels) without touching transform layout.
                image.pixelsPerUnitMultiplier = 2f;
                ui.image = image;
            }
            else
            {
                KvRoundedImage rounded = go.AddComponent<KvRoundedImage>();
                rounded.raycastTarget = false;
                rounded.enabled = true;
                ui.rounded = rounded;
            }
            return ui;
        }

        // The key visuals (fill / outline) are sliced sprites when available; everything else
        // (notes, gradient rain) keeps the procedural mesh path. The "Border"/"Fill" rect names
        // are used to pick the correct sprite.
        private static KvUiRect NewKeyViewerRect(string name, Transform parent)
        {
            BundleLoader.EnsureBundleLoaded();
            bool isBorder = !string.IsNullOrEmpty(name) && name.IndexOf("Border", StringComparison.OrdinalIgnoreCase) >= 0;
            Sprite sprite = isBorder ? BundleLoader.bundleKeyOutline : BundleLoader.bundleKeyBackground;
            return NewKvSpriteRect(name, parent, sprite);
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

        // Variant used for the SOLID half of a fade composite. Disables the rect's outer AA
        // so its top edge stays at full alpha — combined with the matching gradient half,
        // this removes the visible horizontal seam where the fade ends and solid begins.
        private static void EmitNoteSolidNoAA(Rect rect, Color color)
        {
            KvUiRect ui = NextNoteImage();
            PlaceKvSolidNoAARect(ui, rect, color);
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
            if (ui == null || (ui.rounded == null && ui.image == null)) return;
            if (rect.width <= 0f || rect.height <= 0f || color.a <= 0f)
            {
                if (ui.rounded != null) ui.rounded.enabled = false;
                if (ui.image != null) ui.image.enabled = false;
                return;
            }

            ui.rectTransform.anchoredPosition = new Vector2(rect.x, -rect.y);
            ui.rectTransform.sizeDelta = new Vector2(rect.width, rect.height);

            if (ui.image != null)
            {
                // Sliced sprite: tint via Image.color, the 9-slice border supplies the rounded
                // corner from the source asset (KeyBackground / KeyOutline). The radius and
                // borderThickness parameters from the procedural path are ignored — the sprite
                // already encodes both visuals.
                ui.image.color = color;
                ui.image.enabled = true;
                return;
            }

            float maxRadius = Mathf.Min(KvMaxCornerRadiusPx, Mathf.Min(rect.width, rect.height) * 0.25f);
            float effectiveRadius = Mathf.Min(Mathf.Max(0f, radius), maxRadius);

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
            // Pass noAA=true so the gradient's bottom edge keeps its full vertex alpha
            // (no 1.25 px transparent ring multiplying it down to 0). The gradient handles
            // the visible top fade itself; the AA ring would just add a seam at the bottom.
            ui.rounded.SetShape(0f, true, reverse, 0f, noAA: true);
            ui.rounded.enabled = true;
        }

        private static void PlaceKvSolidNoAARect(KvUiRect ui, Rect rect, Color color)
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
            ui.rounded.SetShape(0f, false, false, 0f, noAA: true);
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
            RectTransform rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            return t;
        }

        private static void ApplyFontToKeyViewer()
        {
            if (!kvTextBuilt) return;
            string requested = Main.settings != null ? (Main.settings.fontName ?? "") : "";
            if (requested == kvActiveFontName && kvActiveFont != null) return;

            TMP_FontAsset fa = null;
            try { fa = BundleLoader.GetBundleFont(requested); } catch { }
            if (fa == null) try { fa = BundleLoader.bundleDefaultFont; } catch { }
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
            string raw;
            string tab;
            ResolveActivePreset(out raw, out tab);
            if (string.IsNullOrEmpty(tab)) tab = "4key";
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
                    // In simple mode the preset is generated and "selectedKeyType" is the
                    // baked tab name. Don't write that into the user's saved dmnote setting
                    // — they'd lose their tab choice on the next switch back.
                    if (!string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase))
                        Main.settings.keyViewerSelectedTab = tab;
                }

                JObject keysTable = root["keys"] as JObject;
                JObject posTable = (root["keyPositions"] as JObject) ?? (root["positions"] as JObject);
                if (keysTable == null || posTable == null)
                {
                    Main.mod?.Logger?.Log("[KeyViewer] preset missing 'keys' or 'keyPositions' object at root.");
                    lastParsedPresetJson = raw;
                    lastParsedTab = tab;
                    return;
                }

                JArray keyArr = keysTable[tab] as JArray;
                JArray posArr = posTable[tab] as JArray;
                if (keyArr == null || posArr == null)
                {
                    string availableKeys = "";
                    string availablePos = "";
                    foreach (var prop in keysTable.Properties())
                    {
                        availableKeys += (availableKeys.Length > 0 ? "," : "") + prop.Name;
                    }
                    foreach (var prop in posTable.Properties())
                    {
                        availablePos += (availablePos.Length > 0 ? "," : "") + prop.Name;
                    }
                    Main.mod?.Logger?.Log("[KeyViewer] tab '" + tab + "' missing. Available keys=[" + availableKeys + "] positions=[" + availablePos + "]");
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
                    string countKey = JOptionalString(p, "countKey");
                    k.countPrefKey = KvCountKey(!string.IsNullOrEmpty(countKey) ? countKey : k.keyName);
                    k.keyCode = ResolveKeyCode(k.keyName);
                    string ghostKey = JOptionalString(p, "ghostKey");
                    k.ghostKeyCode = !string.IsNullOrEmpty(ghostKey) ? ResolveKeyCode(ghostKey) : KeyCode.None;
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
                    k.borderWidth = JFloat(p, "borderWidth", 3f);
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

                    k.count = PlayerPrefs.GetInt(k.countPrefKey, JInt(p, "count", 0));
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
                    CacheKeyLayoutModes(k);

                    k.labelTmp = NewKvLabel(k.displayText, TextAlignmentOptions.Center);
                    if (k.counterEnabled)
                        k.counterTmp = NewKvLabel("", TextAlignmentOptions.Bottom);
                    else
                        k.counterTmp = null;

                    k.isStat = false;
                    k.isKps = false;
                    k.isTotal = false;
                    k.lastCounterValue = int.MinValue;

                    k.visualRoot = NewKeyVisualRoot("KVKey_" + i);
                    // Sibling order = draw order. Fill must render below the outline,
                    // so create the fill first, then the border.
                    k.fillUi = NewKeyViewerRect("Fill", k.visualRoot.transform);
                    k.borderUi = NewKeyViewerRect("Border", k.visualRoot.transform);

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
                            CacheKeyLayoutModes(k);

                            string statLabel = k.keyName.Equals("kps", StringComparison.OrdinalIgnoreCase) ? "KPS" :
                                               k.keyName.Equals("total", StringComparison.OrdinalIgnoreCase) ? "Total" : k.keyName.ToUpperInvariant();
                            string jsonDisplay = JOptionalString(p, "displayText");
                            k.hasCustomDisplayText = !string.IsNullOrEmpty(jsonDisplay);
                            if (k.hasCustomDisplayText)
                                k.displayText = jsonDisplay;
                            else
                                k.displayText = k.counterEnabled ? statLabel : "0  " + statLabel;

                            k.count = -1;
                            k.isStat = true;
                            k.isKps = k.keyName.Equals("kps", StringComparison.OrdinalIgnoreCase);
                            k.isTotal = k.keyName.Equals("total", StringComparison.OrdinalIgnoreCase);
                            k.lastCounterValue = int.MinValue;
                            k.labelTmp = NewKvLabel(k.displayText, TextAlignmentOptions.Center);
                            if (k.counterEnabled)
                                k.counterTmp = NewKvLabel("", TextAlignmentOptions.Center);
                            if (kvActiveFont != null) k.labelTmp.font = kvActiveFont;
                            if (kvActiveFont != null && k.counterTmp != null) k.counterTmp.font = kvActiveFont;
                            k.visualRoot = NewKeyVisualRoot("KVStat_" + i);
                            // Sibling order = draw order. Fill must render below the outline.
                            k.fillUi = NewKeyViewerRect("Fill", k.visualRoot.transform);
                            k.borderUi = NewKeyViewerRect("Border", k.visualRoot.transform);
                            keyViewerKeys.Add(k);
                            canvasW = Mathf.Max(canvasW, k.dx + k.width);
                            canvasH = Mathf.Max(canvasH, k.dy + k.height);
                        }
                    }
                }

                if (canvasW > 0f) keyViewerCanvasWidth = canvasW + 40f;
                if (canvasH > 0f) keyViewerCanvasHeight = canvasH + (Main.settings.KeyViewerNoteEffect ? Main.settings.KeyViewerTrackHeight : 0f) + 40f;
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[KeyViewer] Parse failed: " + ex.Message);
                keyViewerKeys = new List<KvKey>();
            }

            ApplyFontToKeyViewer();
            SortKeyViewerVisualLayers();

            lastParsedPresetJson = raw;
            lastParsedTab = tab;
            Main.mod?.Logger?.Log("[KeyViewer] Built " + keyViewerKeys.Count + " items for tab '" + tab + "' canvas=" + keyViewerCanvasWidth + "x" + keyViewerCanvasHeight);
        }

        private struct KvVisualLayerEntry
        {
            public KvKey key;
            public int index;
        }

        private static void SortKeyViewerVisualLayers()
        {
            if (keyViewerKeys == null) return;
            int n = keyViewerKeys.Count;
            if (n <= 1)
            {
                if (kvRenderOrder == null || kvRenderOrder.Length < n)
                    kvRenderOrder = new int[n];
                if (n == 1) kvRenderOrder[0] = 0;
                kvRenderOrderCount = n;
                return;
            }

            // Build a temporary list of valid keys with their original indices
            List<KvVisualLayerEntry> entries = new List<KvVisualLayerEntry>();
            for (int i = 0; i < n; i++)
            {
                KvKey k = keyViewerKeys[i];
                if (k != null && k.visualRoot != null)
                {
                    KvVisualLayerEntry e = new KvVisualLayerEntry();
                    e.key = k;
                    e.index = i;
                    entries.Add(e);
                }
            }

            // Stable sort by dy, then by original index
            entries.Sort((a, b) =>
            {
                int cmp = a.key.dy.CompareTo(b.key.dy);
                return cmp != 0 ? cmp : a.index.CompareTo(b.index);
            });

            if (kvRenderOrder == null || kvRenderOrder.Length < entries.Count)
                kvRenderOrder = new int[entries.Count];
            kvRenderOrderCount = entries.Count;

            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].key.visualRoot.transform.SetSiblingIndex(i);
                kvRenderOrder[i] = entries[i].index;
            }
        }

        private static void EnsureKeyViewerLayout()
        {
            string raw;
            string tab;
            ResolveActivePreset(out raw, out tab);
            if (keyViewerKeys == null || raw != lastParsedPresetJson || tab != lastParsedTab)
            {
                RebuildKeyViewerLayout();
            }
        }

        // When mode == "simple", swap in a generated preset for the chosen Key10/12/16/20
        // layout so the rest of the renderer (parsing, key building, rain, fonts) is unchanged.
        // The user's saved dmnote JSON is left untouched and reappears the moment they switch
        // back. The cache key is the generated string, so style changes invalidate it correctly.
        private static void ResolveActivePreset(out string raw, out string tab)
        {
            if (string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase))
            {
                int style = Mathf.Clamp(Main.settings.KeyViewerSimpleStyle, 0, 3);
                raw = SimplePresets.GetJson(style);
                tab = SimplePresets.TabName;
                return;
            }
            raw = Main.settings.keyViewerPresetJson;
            tab = Main.settings.keyViewerSelectedTab;
        }

        private const int MAX_NOTES_PER_KEY = 256;

        private static void UpdateKeyViewerKeyImages(KvKey k, Rect keyRect, bool pressed, float scale)
        {
            if (k == null || k.fillUi == null) return;

            float scaledRadius = Mathf.Min(Mathf.Max(0f, k.borderRadius * scale), KvMaxCornerRadiusPx);
            bool showBorder = k.borderUi != null && k.borderWidth > 0.5f && Mathf.Max(k.borderColor.a, k.activeBorderColor.a) > 0f;
            // When the bundle provided a sliced KeyBackground/KeyOutline sprite, the rounded edge
            // is encoded in the sprite itself. Both fill and outline must render at the same full
            // keyRect — the outline sprite's transparent center reveals the fill, and its border
            // pixels overlay. Do NOT inset the fill in sprite mode (that produced visible gaps
            // around the fill where the outline showed through, since the outline was drawn at
            // full keyRect but the fill was shrunk by the legacy procedural border thickness).
            bool spriteMode = k.fillUi != null && k.fillUi.image != null;
            if (showBorder)
            {
                float keyMin = Mathf.Min(keyRect.width, keyRect.height);
                float adaptiveBorder = Mathf.Clamp(k.borderWidth * (keyMin / 60f), 1f, keyMin * 0.12f);
                Color borderColor = pressed ? k.activeBorderColor : k.borderColor;
                PlaceKeyRect(k.borderUi, keyRect, borderColor, scaledRadius, adaptiveBorder);

                Rect fillRect;
                float fillRadius;
                if (spriteMode)
                {
                    fillRect = keyRect;
                    fillRadius = scaledRadius; // ignored by sliced Image, but kept for fallback
                }
                else
                {
                    fillRect = new Rect(
                        keyRect.x + adaptiveBorder,
                        keyRect.y + adaptiveBorder,
                        Mathf.Max(0f, keyRect.width - adaptiveBorder * 2f),
                        Mathf.Max(0f, keyRect.height - adaptiveBorder * 2f)
                    );
                    fillRadius = Mathf.Max(0f, scaledRadius - adaptiveBorder);
                }
                PlaceKeyRect(k.fillUi, fillRect, pressed ? k.activeBgColor : k.bgColor, fillRadius);
            }
            else
            {
                if (k.borderUi != null)
                {
                    if (k.borderUi.rounded != null) k.borderUi.rounded.enabled = false;
                    if (k.borderUi.image != null) k.borderUi.image.enabled = false;
                }
                PlaceKeyRect(k.fillUi, keyRect, pressed ? k.activeBgColor : k.bgColor, scaledRadius);
            }
        }

        internal static void DrawKeyViewer()
        {
            LoadKeyViewerTotalIfNeeded();
            EnsureKeyViewerLayout();
            FlushKvSaveIfDue();

            if (keyViewerKeys == null || keyViewerKeys.Count == 0)
            {
                SetActiveIfChanged(kvImageRoot, false);
                SetActiveIfChanged(kvTextRoot, false);
                return;
            }
            SetActiveIfChanged(kvImageRoot, true);
            SetActiveIfChanged(kvTextRoot, true);
            BeginKeyViewerImageFrame();

            // FIX: re-apply font every frame in case bundle loaded after layout built
            ApplyFontToKeyViewer();

            bool simpleMode = string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase);
            float scale = Mathf.Clamp(simpleMode ? Main.settings.KeyViewerSimpleSize : Main.settings.KeyViewerScale, 0.2f, 4f);
            float originX = Main.settings.KeyViewerOffsetX;
            float originY = (Screen.height - keyViewerCanvasHeight * scale) + Main.settings.KeyViewerOffsetY;
            if (simpleMode)
            {
                float yLocation = Main.settings.KeyViewerSimpleYLocation;
                if (Main.settings.KeyViewerSimpleDownLocation && Mathf.Abs(yLocation - 200f) < 0.001f)
                    yLocation = 0f;
                originY += Mathf.Clamp(200f - yLocation, -1000f, 1000f) * scale;
            }

            float now = Time.unscaledTime;
            bool reverse = Main.settings.KeyViewerNoteReverse;
            float noteSpeed = simpleMode ? Main.settings.KeyViewerSimpleRainSpeed : Main.settings.KeyViewerNoteSpeed;
            float trackHeight = simpleMode ? Main.settings.KeyViewerSimpleRainHeight : Main.settings.KeyViewerTrackHeight;
            float speed = Mathf.Max(1f, noteSpeed) * scale;
            float trackH = Mathf.Max(0f, trackHeight) * scale;
            int currentKps = PruneKeyViewerPressLog(now);

            float autoTopY = float.MaxValue;
            float autoBottomY = float.MinValue;

            for (int i = 0; i < keyViewerKeys.Count; i++)
            {
                var k = keyViewerKeys[i];
                if (k.isStat) continue;

                float y = originY + k.dy * scale;
                float yMax = y + k.height * scale;

                if (y < autoTopY) autoTopY = y;
                if (yMax > autoBottomY) autoBottomY = yMax;
            }

            int n = keyViewerKeys.Count;
            if (kvRenderOrder == null || kvRenderOrderCount != n)
                SortKeyViewerVisualLayers();

            for (int oi = 0; oi < kvRenderOrderCount; oi++)
            {
                int i = kvRenderOrder[oi];
                KvKey k = keyViewerKeys[i];
                bool isStat = k.isStat;
                bool rawPressed = !isStat && k.keyCode != KeyCode.None && KvIsKeyPressed(k.keyCode);
                bool rawGhostPressed = !isStat && simpleMode && Main.settings.KeyViewerSimpleUseRain
                                       && Main.settings.KeyViewerSimpleUseGhostRain
                                       && k.ghostKeyCode != KeyCode.None && KvIsKeyPressed(k.ghostKeyCode);
                bool pressed = !isStat && k.keyCode != KeyCode.None
                               && KvApplyInputFilters(k.keyCode, rawPressed, k.wasPressed, ref k.ignoredPress);
                bool ghostPressed = !isStat && simpleMode && Main.settings.KeyViewerSimpleUseRain
                                    && Main.settings.KeyViewerSimpleUseGhostRain
                                    && k.ghostKeyCode != KeyCode.None
                                    && KvApplyInputFilters(k.ghostKeyCode, rawGhostPressed, k.wasGhostPressed, ref k.ignoredGhostPress);

                if (!isStat)
                {
                    if (pressed && !k.wasPressed)
                    {
                        BeginKeyViewerNote(k.noteStartTimes, k.noteEndTimes, now);

                        k.count++;
                        keyViewerTotalPresses++;
                        keyViewerPressLog.Add(now);

                        PlayerPrefs.SetInt(k.countPrefKey ?? KvCountKey(k.keyName), k.count);
                        PlayerPrefs.SetInt(KvTotalPrefKey, keyViewerTotalPresses);
                        ScheduleKvSave();
                    }
                    else if (!pressed && k.wasPressed)
                    {
                        EndKeyViewerNote(k.noteEndTimes, now);
                    }

                    if (ghostPressed && !k.wasGhostPressed)
                        BeginKeyViewerNote(k.ghostNoteStartTimes, k.ghostNoteEndTimes, now);
                    else if (!ghostPressed && k.wasGhostPressed)
                        EndKeyViewerNote(k.ghostNoteEndTimes, now);

                    k.wasPressed = pressed;
                    k.wasGhostPressed = ghostPressed;
                }
                else
                {
                    if (k.isKps)
                        k.statValue = currentKps;
                    else if (k.isTotal)
                        k.statValue = keyViewerTotalPresses;
                    else
                        k.statValue = 0;

                    if (!k.counterEnabled && !k.hasCustomDisplayText)
                    {
                        if (k.isKps)
                            k.displayText = currentKps + "  KPS";
                        else if (k.isTotal)
                            k.displayText = keyViewerTotalPresses + "  Total";
                    }
                }

                Rect keyRect = new Rect(
                    originX + k.dx * scale,
                    originY + k.dy * scale,
                    k.width * scale,
                    k.height * scale
                );

                if (!k.isStat && Main.settings.KeyViewerNoteEffect && k.noteEffectEnabled && trackH > 0f)
                {
                    DrawKeyViewerNotes(k, k.noteStartTimes, k.noteEndTimes, keyRect, scale, now,
                        reverse, speed, trackH, autoTopY, autoBottomY, k.noteColor);

                    if (simpleMode && Main.settings.KeyViewerSimpleUseGhostRain && k.ghostKeyCode != KeyCode.None)
                    {
                        Color ghostColor = k.noteColor;
                        ghostColor.a *= 0.45f;
                        DrawKeyViewerNotes(k, k.ghostNoteStartTimes, k.ghostNoteEndTimes, keyRect, scale, now,
                            reverse, speed, trackH, autoTopY, autoBottomY, ghostColor);
                    }
                }

                UpdateKeyViewerKeyImages(k, keyRect, pressed, scale);

                // ---------- TMP TEXT UPDATE ----------
                int fs = Mathf.Max(8, Mathf.RoundToInt(k.fontSize * scale));

                bool showCounterForThisKey = Main.settings.KeyViewerShowCounter && k.counterEnabled;

                if (k.labelTmp != null)
                {
                    k.labelTmp.color = pressed ? k.activeFontColor : k.fontColor;
                    k.labelTmp.fontSize = fs;
                    if (k.labelTmp.text != k.displayText) k.labelTmp.text = k.displayText;

                    var rt = k.labelTmp.rectTransform;

                    if (isStat && showCounterForThisKey)
                    {
                        // counter.align controls where the counter sits inside the stat box:
                        //   "top"    -> counter on top, label on bottom (DM Note default)
                        //   "bottom" -> counter on bottom, label on top (Simple-mode tall boxes)
                        //   anything else -> inline (label on left, counter on right)
                        bool stackedTop = k.counterStackTop;
                        bool stackedBottom = k.counterStackBottom;
                        // Half-gap on each side of the midline. Total breathing room between
                        // counter and label = 2 * stackGapHalf.
                        float stackGapHalf = Mathf.Max(3f, 4f * scale);
                        // Lift the whole counter+label pair toward the top of the cell so
                        // they read as a tight block rather than centered on the midline.
                        // The bottom of the cell ends up empty — visually the same idea as
                        // hugging the upper border with the rain track underneath.
                        float stackTopBias = Mathf.Max(6f, 12f * scale);
                        if (stackedTop)
                        {
                            // Counter rect sits at the very top edge of the cell; label rect
                            // is shifted up by stackTopBias so both rows ride high.
                            float counterHeight = keyRect.height * 0.5f;
                            k.labelTmp.alignment = TextAlignmentOptions.Top;
                            k.labelTmp.fontSize = Mathf.Max(8, Mathf.RoundToInt(k.fontSize * scale * 1.15f));
                            rt.anchoredPosition = new Vector2(keyRect.x, -(keyRect.y + counterHeight + stackGapHalf - stackTopBias));
                            rt.sizeDelta = new Vector2(keyRect.width, keyRect.height - counterHeight - stackGapHalf);
                        }
                        else if (stackedBottom)
                        {
                            // Label on top, counter on bottom — pull both rects upward.
                            k.labelTmp.alignment = TextAlignmentOptions.Bottom;
                            k.labelTmp.fontSize = Mathf.Max(8, Mathf.RoundToInt(k.fontSize * scale * 1.15f));
                            rt.anchoredPosition = new Vector2(keyRect.x, -(keyRect.y - stackTopBias));
                            rt.sizeDelta = new Vector2(keyRect.width, keyRect.height * 0.5f - stackGapHalf);
                        }
                        else
                        {
                            float pad = Mathf.Min(keyRect.width * 0.08f, Mathf.Max(6f, 12f * scale));
                            float availableWidth = Mathf.Max(0f, keyRect.width - pad * 2f);
                            float labelWidth = availableWidth * 0.42f;
                            k.labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
                            rt.anchoredPosition = new Vector2(keyRect.x + pad, -keyRect.y);
                            rt.sizeDelta = new Vector2(labelWidth, keyRect.height);
                        }
                    }
                    else if (showCounterForThisKey)
                    {
                        // Non-stat key with an inline counter. Same split-and-pull-apart
                        // pattern as stat boxes; label gets the bigger slot (~60%) since the
                        // key glyph carries the visual weight.
                        bool nstackedTop = k.counterStackTop;
                        bool nstackedBottom = k.counterStackBottom;
                        float stackGapHalf = Mathf.Max(3f, 4f * scale);
                        if (nstackedTop)
                        {
                            float counterHeight = keyRect.height * 0.4f;
                            k.labelTmp.alignment = TextAlignmentOptions.Top;
                            rt.anchoredPosition = new Vector2(keyRect.x, -(keyRect.y + counterHeight + stackGapHalf));
                            rt.sizeDelta = new Vector2(keyRect.width, keyRect.height - counterHeight - stackGapHalf);
                        }
                        else if (nstackedBottom)
                        {
                            k.labelTmp.alignment = TextAlignmentOptions.Bottom;
                            rt.anchoredPosition = new Vector2(keyRect.x, -keyRect.y);
                            rt.sizeDelta = new Vector2(keyRect.width, keyRect.height * 0.6f - stackGapHalf);
                        }
                        else
                        {
                            k.labelTmp.alignment = TextAlignmentOptions.Center;
                            rt.anchoredPosition = new Vector2(keyRect.x, -keyRect.y);
                            rt.sizeDelta = new Vector2(keyRect.width, keyRect.height);
                        }
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
                        int curCounter = isStat ? k.statValue : k.count;
                        if (curCounter != k.lastCounterValue)
                        {
                            k.lastCounterValue = curCounter;
                            k.counterTmp.text = curCounter.ToString();
                        }
                        k.counterTmp.alignment = k.counterAlignment;

                        var rt = k.counterTmp.rectTransform;
                        if (isStat)
                        {
                            // Mirror the label-layout decision so label and counter rects agree.
                            bool stackedTop = k.counterStackTop;
                            bool stackedBottom = k.counterStackBottom;
                            float stackGapHalf = Mathf.Max(3f, 4f * scale);
                            float stackTopBias = Mathf.Max(6f, 12f * scale);
                            if (stackedTop)
                            {
                                // Counter on top — shifted upward in lockstep with the label.
                                int baseSize = k.counterFontSize > 0 ? k.counterFontSize : k.fontSize;
                                k.counterTmp.fontSize = Mathf.Max(8, Mathf.RoundToInt(baseSize * scale * 1.15f));
                                k.counterTmp.alignment = TextAlignmentOptions.Bottom;
                                rt.anchoredPosition = new Vector2(keyRect.x, -(keyRect.y - stackTopBias));
                                rt.sizeDelta = new Vector2(keyRect.width, keyRect.height * 0.5f - stackGapHalf);
                            }
                            else if (stackedBottom)
                            {
                                // Counter on bottom — also lifted up so the pair reads tight.
                                int baseSize = k.counterFontSize > 0 ? k.counterFontSize : k.fontSize;
                                k.counterTmp.fontSize = Mathf.Max(8, Mathf.RoundToInt(baseSize * scale * 1.15f));
                                k.counterTmp.alignment = TextAlignmentOptions.Top;
                                float labelHeight = keyRect.height * 0.5f;
                                rt.anchoredPosition = new Vector2(keyRect.x, -(keyRect.y + labelHeight + stackGapHalf - stackTopBias));
                                rt.sizeDelta = new Vector2(keyRect.width, keyRect.height - labelHeight - stackGapHalf);
                            }
                            else
                            {
                                float pad = Mathf.Min(keyRect.width * 0.08f, Mathf.Max(6f, 12f * scale));
                                float availableWidth = Mathf.Max(0f, keyRect.width - pad * 2f);
                                float labelWidth = availableWidth * 0.42f;
                                float gap = Mathf.Max(2f, 4f * scale);
                                rt.anchoredPosition = new Vector2(keyRect.x + pad + labelWidth + gap, -keyRect.y);
                                rt.sizeDelta = new Vector2(Mathf.Max(0f, availableWidth - labelWidth - gap), keyRect.height);
                            }
                        }
                        else
                        {
                            // Non-stat counter mirrors the label's stacked split, including
                            // the midline gap so neither rect overlaps the other.
                            bool nstackedTop = k.counterStackTop;
                            bool nstackedBottom = k.counterStackBottom;
                            float stackGapHalf = Mathf.Max(3f, 4f * scale);
                            if (nstackedTop)
                            {
                                k.counterTmp.alignment = TextAlignmentOptions.Bottom;
                                rt.anchoredPosition = new Vector2(keyRect.x, -keyRect.y);
                                rt.sizeDelta = new Vector2(keyRect.width, keyRect.height * 0.4f - stackGapHalf);
                            }
                            else if (nstackedBottom)
                            {
                                k.counterTmp.alignment = TextAlignmentOptions.Top;
                                float labelHeight = keyRect.height * 0.6f;
                                rt.anchoredPosition = new Vector2(keyRect.x, -(keyRect.y + labelHeight + stackGapHalf));
                                rt.sizeDelta = new Vector2(keyRect.width, keyRect.height - labelHeight - stackGapHalf);
                            }
                            else
                            {
                                rt.anchoredPosition = new Vector2(keyRect.x, -(keyRect.y - 3f * scale));
                                rt.sizeDelta = new Vector2(keyRect.width, keyRect.height);
                }
            }
        }

                }
            }

            EndKeyViewerImageFrame();
        }

        private static void BeginKeyViewerNote(List<float> starts, List<float> ends, float now)
        {
            if (starts.Count > MAX_NOTES_PER_KEY)
            {
                starts.RemoveAt(0);
                ends.RemoveAt(0);
            }

            starts.Add(now);
            ends.Add(-1f);
        }

        private static void EndKeyViewerNote(List<float> ends, float now)
        {
            int last = ends.Count - 1;
            if (last >= 0 && ends[last] < 0f)
                ends[last] = now;
        }

        private static void DrawKeyViewerNotes(KvKey k, List<float> starts, List<float> ends,
                                               Rect keyRect, float scale, float now,
                                               bool reverse, float speed, float trackH,
                                               float autoTopY, float autoBottomY,
                                               Color noteColor)
        {
            if (trackH <= 0f) return;

            float noteWidth = (k.noteWidth > 0f ? k.noteWidth * scale : keyRect.width);
            float noteX = k.noteAlignmentMode < 0
                ? keyRect.x
                : (k.noteAlignmentMode > 0
                    ? keyRect.xMax - noteWidth
                    : keyRect.x + (keyRect.width - noteWidth) * 0.5f);

            float baseY = k.noteAutoYCorrection
                ? (reverse ? autoBottomY : autoTopY)
                : (reverse ? keyRect.yMax : keyRect.y);

            int write = 0;
            int count = starts.Count;

            for (int j = 0; j < count; j++)
            {
                float start = starts[j];
                float end = ends[j];
                float lead = (now - start) * speed;
                float trail = (end < 0f) ? 0f : (now - end) * speed;
                float height = lead - trail;

                if (height <= 0.5f)
                {
                    starts[write] = start;
                    ends[write] = end;
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
                    float effectiveFadePx = Main.settings.KeyViewerFadePx;
                    if (string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase))
                        effectiveFadePx = 0f;

                    if (effectiveFadePx > 0.5f)
                        EmitNoteWithFade(nRect, noteColor, baseY, trackH, effectiveFadePx, reverse);
                    else
                        EmitNoteRect(nRect, noteColor, 2f);
                }

                starts[write] = start;
                ends[write] = end;
                write++;
            }

            if (write < count)
            {
                starts.RemoveRange(write, count - write);
                ends.RemoveRange(write, count - write);
            }
        }

        internal static void ImportKeyViewerPreset()
        {
            string picked = PickPresetJsonFile();
            if (string.IsNullOrEmpty(picked)) return;
            try
            {
                string txt = File.ReadAllText(picked);
                JObject.Parse(txt);
                Main.settings.keyViewerPresetJson = txt;
                keyViewerKeys = null;
                Main.mod?.Logger?.Log("[KeyViewer] Imported preset from " + picked);
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[KeyViewer] Import failed: " + ex.Message);
            }
        }

        private static string PickPresetJsonFile()
        {
            try
            {
#if LEGACY
                 SFB.ExtensionFilter[] filters = new[]
                 {
                     new SFB.ExtensionFilter("JSON Preset", "json"),
                     new SFB.ExtensionFilter("All Files", "*")
                 };
                 string[] picked = SFB.StandaloneFileBrowser.OpenFilePanel("Select DM Note preset", "", filters, false);
                 if (picked == null || picked.Length == 0) return null;
                 string path = picked[0];
                return string.IsNullOrEmpty(path) ? null : path;
#else
                // Game ships UnityFileDialog (native OS picker on Win/Mac/Linux).
                string path = UnityFileDialog.FileBrowser.PickFile(
                    "", "JSON Preset", new[] { "json" }, "Select DM Note preset");
                return string.IsNullOrEmpty(path) ? null : path;
#endif
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[KeyViewer] Picker failed: " + ex.Message);
                return null;
            }
        }

        internal static void HideKeyViewer()
        {
            SetActiveIfChanged(kvImageRoot, false);
            SetActiveIfChanged(kvTextRoot, false);
        }

        internal static void ShowKeyViewer()
        {
            SetActiveIfChanged(kvImageRoot, true);
            SetActiveIfChanged(kvTextRoot, true);
        }

        private static void SetActiveIfChanged(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
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
            kvCachedKeyboard = null;
            kvKeyboardInitialized = false;

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
