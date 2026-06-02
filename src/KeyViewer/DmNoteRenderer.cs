using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JipperKeyViewer.KeyViewer
{
    public partial class KeyViewer : MonoBehaviour
    {
        private sealed class DmNoteSpec
        {
            public string keyName;
            public KeyCode keyCode;
            public KeyCode ghostKeyCode;
            public string countPrefKey;
            public float x, y, w, h;
            public string displayText;
            public Color bg, activeBg, outline, activeOutline, text, activeText, counterText, activeCounterText, rain, ghostRain;
            public int fontSize, counterFontSize;
            public string counterAlign;
            public string counterAlignMode;
            public float counterGap;
            public bool counterEnabled;
            public bool inlineStatCounter;
            public bool noteEnabled;
            public bool isStat, isKps, isTotal;
        }

        private DmNoteSpec[] dmNoteRuntimeKeys;
        private bool dmNoteLayoutActive;
        private string dmNoteLayoutSignature;
        private float dmNoteCanvasHeight;
        private float dmNoteBottomRowDy;
        private bool dmNoteHasBottomRow;

        private static readonly Dictionary<string, string> DmNoteDisplayOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "SECTION", "`" },
            { "INS", "Ins" },
            { "CONTEXT MENU", "Menu" },
            { "19", "Pause" },
            { "SCROLL LOCK", "ScrLk" },
            { "SCROLLLOCK", "ScrLk" },
        };

        private static bool IsDmNoteMode()
        {
            var s = global::KorenResourcePack.Main.settings;
            return s != null && string.Equals(s.KeyViewerMode, "dmnote", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HostKeyViewerEnabled()
        {
            var s = global::KorenResourcePack.Main.settings;
            if (s == null) return Settings != null && Settings.Enabled;
            if (IsDmNoteMode()) return s.keyViewerOn;
            return s.keyViewerOn && Settings != null && Settings.Enabled;
        }

        private static float GetDmNoteScale()
        {
            var s = global::KorenResourcePack.Main.settings;
            return Mathf.Clamp(s != null ? s.KeyViewerScale : 1f, 0.2f, 4f);
        }

        private static float GetDmNoteTrackHeight()
        {
            var s = global::KorenResourcePack.Main.settings;
            return Mathf.Max(0f, s != null ? s.KeyViewerTrackHeight : 200f);
        }

        private static string DmNoteSignature()
        {
            var s = global::KorenResourcePack.Main.settings;
            if (s == null) return "";
            return (s.keyViewerSelectedTab ?? "") + "\n" +
                   (s.KeyViewerShowCounter ? "counter:1" : "counter:0") + "\n" +
                   (s.KeyViewerNoteEffect ? "rain:1:" + s.KeyViewerTrackHeight.ToString("0.###") : "rain:0") + "\n" +
                   (s.keyViewerPresetJson ?? "");
        }

        private bool IsDmNoteLayoutDirty()
        {
            return dmNoteLayoutActive && dmNoteLayoutSignature != DmNoteSignature();
        }

        private bool DmNoteRainEnabled()
        {
            var s = global::KorenResourcePack.Main.settings;
            return s != null && s.KeyViewerNoteEffect;
        }

        private void UpdateDmNoteTransform()
        {
            if (KeyViewerSizeObject == null) return;
            var s = global::KorenResourcePack.Main.settings;
            if (s == null) return;

            RectTransform rt = (RectTransform)KeyViewerSizeObject.transform;
            float scale = GetDmNoteScale();
            Vector3 desiredScale = new Vector3(scale, scale, 1f);
            if (rt.localScale != desiredScale)
                rt.localScale = desiredScale;

            Vector2 desiredPos = new Vector2(s.KeyViewerOffsetX, -s.KeyViewerOffsetY);
            if (rt.anchoredPosition != desiredPos)
                rt.anchoredPosition = desiredPos;
        }

        private void InitializeDmNoteKeyViewer()
        {
            dmNoteLayoutSignature = DmNoteSignature();
            UpdateDmNoteTransform();

            var specs = ParseDmNoteSpecs();
            int size = Mathf.Max(36, specs.Count);
            Keys = new Key[size];
            dmNoteRuntimeKeys = new DmNoteSpec[size];
            PressTimes = PressTimes ?? new Queue<long>();

            for (int i = 0; i < specs.Count; i++)
            {
                DmNoteSpec spec = specs[i];
                Key key = CreateDmNoteKey(i, spec);
                Keys[i] = key;
                dmNoteRuntimeKeys[i] = spec;
                if (spec.isKps) Kps = key;
                if (spec.isTotal) Total = key;
            }
        }

        private List<DmNoteSpec> ParseDmNoteSpecs()
        {
            List<DmNoteSpec> result = new List<DmNoteSpec>();
            dmNoteHasBottomRow = false;
            dmNoteBottomRowDy = 0f;
            var ms = global::KorenResourcePack.Main.settings;
            if (ms == null || string.IsNullOrWhiteSpace(ms.keyViewerPresetJson))
            {
                dmNoteCanvasHeight = 250f;
                return result;
            }

            try
            {
                JObject root = JObject.Parse(ms.keyViewerPresetJson);
                string tab = ms.keyViewerSelectedTab;
                JToken selected = root["selectedKeyType"];
                if (selected != null && selected.Type == JTokenType.String)
                    tab = selected.ToString();
                if (string.IsNullOrEmpty(tab)) tab = "4key";
                ms.keyViewerSelectedTab = tab;

                JObject keysTable = root["keys"] as JObject;
                JObject posTable = (root["keyPositions"] as JObject) ?? (root["positions"] as JObject);
                JArray keyArr = keysTable != null ? keysTable[tab] as JArray : null;
                JArray posArr = posTable != null ? posTable[tab] as JArray : null;
                if (keyArr == null || posArr == null)
                {
                    dmNoteCanvasHeight = 250f;
                    return result;
                }

                float maxY = 0f;
                float minKeyDy = float.PositiveInfinity;
                float maxKeyDy = float.NegativeInfinity;
                int n = Mathf.Min(keyArr.Count, posArr.Count);
                for (int i = 0; i < n; i++)
                {
                    JObject p = posArr[i] as JObject;
                    if (p == null || JBool(p, "hidden", false)) continue;
                    DmNoteSpec spec = ParseDmNoteKeySpec(keyArr[i] != null ? keyArr[i].ToString() : "", p, false);
                    result.Add(spec);
                    maxY = Mathf.Max(maxY, spec.y + spec.h);
                    minKeyDy = Mathf.Min(minKeyDy, spec.y);
                    maxKeyDy = Mathf.Max(maxKeyDy, spec.y);
                }
                dmNoteBottomRowDy = maxKeyDy;
                dmNoteHasBottomRow = maxKeyDy - minKeyDy > 1f;

                JObject statTable = root["statPositions"] as JObject;
                JArray statArr = statTable != null ? statTable[tab] as JArray : null;
                if (statArr != null)
                {
                    for (int i = 0; i < statArr.Count; i++)
                    {
                        JObject p = statArr[i] as JObject;
                        if (p == null || JBool(p, "hidden", false)) continue;
                        string name = JStr(p, "statType", "stat");
                        DmNoteSpec spec = ParseDmNoteKeySpec(name, p, true);
                        result.Add(spec);
                        maxY = Mathf.Max(maxY, spec.y + spec.h);
                    }
                }

                float trackHeight = global::KorenResourcePack.Main.settings != null
                    && global::KorenResourcePack.Main.settings.KeyViewerNoteEffect ? GetDmNoteTrackHeight() : 0f;
                dmNoteCanvasHeight = Mathf.Max(60f, maxY) + trackHeight + 40f;
            }
            catch (Exception ex)
            {
                global::KorenResourcePack.Main.mod?.Logger?.Log("[KeyViewer] JKV DM Note parse failed: " + ex.Message);
                dmNoteCanvasHeight = 250f;
                result.Clear();
            }
            return result;
        }

        private DmNoteSpec ParseDmNoteKeySpec(string keyName, JObject p, bool stat)
        {
            string fontHex = JStr(p, "fontColor", "#FFFFFF");
            string bgHex = JStr(p, "backgroundColor", "#3C3C3C");
            string borderHex = JStr(p, "borderColor", "#FFFFFF");
            string noteHex = JStr(p, "noteColor", "#FFFFFF");
            JObject counter = p["counter"] as JObject;
            JObject counterFill = counter != null ? counter["fill"] as JObject : null;

            DmNoteSpec spec = new DmNoteSpec();
            spec.keyName = keyName ?? "";
            spec.keyCode = stat ? KeyCode.None : ResolveDmNoteKeyCode(spec.keyName);
            string ghostName = JOptionalString(p, "ghostKey");
            spec.ghostKeyCode = string.IsNullOrEmpty(ghostName) ? KeyCode.None : ResolveDmNoteKeyCode(ghostName);
            string countKey = JOptionalString(p, "countKey");
            spec.countPrefKey = string.IsNullOrEmpty(countKey) ? spec.keyName : countKey;
            spec.x = JFloat(p, "dx", 0f);
            spec.y = JFloat(p, "dy", 0f);
            spec.w = Mathf.Max(1f, JFloat(p, "width", stat ? 100f : 60f));
            spec.h = Mathf.Max(1f, JFloat(p, "height", stat ? 30f : 60f));
            spec.bg = HexToColor(bgHex, 0.5f);
            spec.activeBg = HexToColor(JStr(p, "activeBackgroundColor", bgHex), 0.5f);
            if (JBool(p, "idleTransparent", false)) spec.bg.a = 0f;
            if (JBool(p, "activeTransparent", false)) spec.activeBg.a = 0f;
            spec.outline = HexToColor(borderHex, 0.4f);
            spec.activeOutline = HexToColor(JStr(p, "activeBorderColor", borderHex), spec.outline.a);
            string activeFontHex = JStr(p, "activeFontColor", fontHex);
            spec.text = HexToColor(fontHex, 1f);
            spec.activeText = HexToColor(activeFontHex, 1f);
            spec.rain = HexToColor(noteHex, JFloat(p, "noteOpacity", 80f) / 100f);
            string ghostNoteHex = JOptionalString(p, "ghostNoteColor");
            spec.ghostRain = string.IsNullOrEmpty(ghostNoteHex)
                ? new Color(spec.rain.r, spec.rain.g, spec.rain.b, spec.rain.a * 0.45f)
                : HexToColor(ghostNoteHex, JFloat(p, "ghostNoteOpacity", 45f) / 100f);
            spec.fontSize = JInt(p, "fontSize", stat ? 16 : 18);
            bool showCounter = global::KorenResourcePack.Main.settings == null || global::KorenResourcePack.Main.settings.KeyViewerShowCounter;
            spec.counterEnabled = showCounter && (counter != null ? JBool(counter, "enabled", true) : !stat);
            spec.counterFontSize = counter != null ? JInt(counter, "fontSize", Mathf.Max(8, Mathf.RoundToInt(spec.fontSize * 0.85f))) : Mathf.Max(8, Mathf.RoundToInt(spec.fontSize * 0.85f));
            spec.counterAlign = counter != null ? JStr(counter, "align", stat ? "center" : "bottom") : (stat ? "center" : "bottom");
            spec.counterAlignMode = counter != null ? JStr(counter, "alignMode", "") : "";
            spec.counterGap = counter != null ? JFloat(counter, "gap", Mathf.Clamp(Settings.CounterTextSpacing, -12f, 32f)) : Mathf.Clamp(Settings.CounterTextSpacing, -12f, 32f);
            string counterIdle = counterFill != null ? JStr(counterFill, "idle", fontHex) : fontHex;
            string counterActive = counterFill != null ? JStr(counterFill, "active", activeFontHex) : activeFontHex;
            spec.counterText = HexToColor(counterIdle, 1f);
            spec.activeCounterText = HexToColor(counterActive, 1f);
            spec.noteEnabled = JBool(p, "noteEffectEnabled", true);
            spec.isStat = stat;
            spec.isKps = stat && spec.keyName.Equals("kps", StringComparison.OrdinalIgnoreCase);
            spec.isTotal = stat && spec.keyName.Equals("total", StringComparison.OrdinalIgnoreCase);
            spec.inlineStatCounter = stat && spec.counterEnabled && string.Equals(spec.counterAlignMode, "center", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(spec.counterAlign, "top", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(spec.counterAlign, "bottom", StringComparison.OrdinalIgnoreCase);
            string display = JOptionalString(p, "displayText");
            spec.displayText = !string.IsNullOrEmpty(display) ? display : DefaultDmNoteDisplay(spec.keyName, stat, spec.counterEnabled);
            return spec;
        }

        private Key CreateDmNoteKey(int index, DmNoteSpec spec)
        {
            float y = dmNoteCanvasHeight - spec.y - spec.h;
            bool bottomRowKey = !spec.isStat && dmNoteHasBottomRow && spec.y >= dmNoteBottomRowDy - 1f;
            GameObject obj = new GameObject("DMNote Key " + index);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.SetParent(KeyViewerSizeObject.transform);
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(spec.x, y);
            rt.sizeDelta = new Vector2(spec.w, spec.h);
            rt.localScale = Vector3.one;

            Key key = obj.AddComponent<Key>();
            key.keyName = spec.keyName;
            key.keyCode = spec.keyCode;
            key.ghostKeyCode = spec.ghostKeyCode;
            key.countPrefKey = spec.countPrefKey;
            key.isStat = spec.isStat;
            key.isKps = spec.isKps;
            key.isTotal = spec.isTotal;
            key.count = spec.isStat ? 0 : global::KorenResourcePack.KeyViewer.GetKeyViewerCount(spec.countPrefKey);
            key.color = (byte)(index < 8 ? 0 : (index < 16 ? 1 : 3));
            key.rainColor = spec.rain;

            key.background = AddDmNoteImage(obj.transform, "Background", spec.w, spec.h, spec.bg, keyBackgroundSprite);
            key.outline = AddDmNoteImage(obj.transform, "Outline", spec.w, spec.h, spec.outline, keyOutlineSprite);
            key.text = AddDmNoteLabel(obj.transform, "KeyText", spec.displayText, spec.fontSize, spec.text);

            if (spec.inlineStatCounter)
            {
                key.text.text = DmNoteInlineStatText(spec, spec.isTotal ? global::KorenResourcePack.KeyViewer.GetKeyViewerTotal() : 0);
                LayoutDmNoteText(key.text.rectTransform, spec, false);
                key.text.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                LayoutDmNoteText(key.text.rectTransform, spec, false);
                key.text.alignment = DmNoteCounterAlignment(spec, false);
            }
            if (spec.counterEnabled && !spec.inlineStatCounter)
            {
                key.value = AddDmNoteLabel(obj.transform, "CountText", "0", spec.counterFontSize, spec.counterText);
                LayoutDmNoteText(key.value.rectTransform, spec, true);
                key.value.alignment = DmNoteCounterAlignment(spec, true);
            }

            if (!spec.isStat && spec.noteEnabled)
            {
                key.rain = new GameObject("RainLine");
                RectTransform rainRt = key.rain.AddComponent<RectTransform>();
                rainRt.SetParent(obj.transform);
                float track = GetDmNoteTrackHeight();
                rainRt.sizeDelta = new Vector2(spec.w, track);
                rainRt.anchorMin = rainRt.anchorMax = rainRt.pivot = Vector2.zero;
                float rainLift = bottomRowKey ? spec.h : 0f;
                rainRt.anchoredPosition = new Vector2(0, spec.h - track + rainLift);
                rainRt.localScale = Vector3.one;
                key.rain.AddComponent<Canvas>();
                key.rain.AddComponent<GraphicRaycaster>();
            }

            return key;
        }

        private Image AddDmNoteImage(Transform parent, string name, float w, float h, Color color, Sprite sprite)
        {
            GameObject go = new GameObject(name);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w * 2f, h * 2f);
            rt.localScale = new Vector3(0.5f, 0.5f);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        private TextMeshProUGUI AddDmNoteLabel(Transform parent, string name, string text, int fontSize, Color color)
        {
            GameObject go = new GameObject(name);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent);
            rt.localScale = Vector3.one;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            ApplyKrpFont(tmp);
            tmp.fontStyle = (FontStyles)Settings.FontStyleFlags;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0;
            tmp.fontSizeMax = Mathf.Max(8, fontSize);
            tmp.raycastTarget = false;
            tmp.color = color;
            tmp.text = text ?? "";
            return tmp;
        }

        private void LayoutDmNoteText(RectTransform rt, DmNoteSpec spec, bool counter)
        {
            bool top = string.Equals(spec.counterAlign, "top", StringComparison.OrdinalIgnoreCase);
            bool bottom = string.Equals(spec.counterAlign, "bottom", StringComparison.OrdinalIgnoreCase);
            bool left = string.Equals(spec.counterAlign, "left", StringComparison.OrdinalIgnoreCase);
            bool right = string.Equals(spec.counterAlign, "right", StringComparison.OrdinalIgnoreCase);
            bool between = string.Equals(spec.counterAlignMode, "between", StringComparison.OrdinalIgnoreCase);
            float gap = Mathf.Clamp(spec.counterGap, -64f, 64f);

            if (!spec.counterEnabled || spec.inlineStatCounter)
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(spec.w - 4f, spec.h - 4f);
                return;
            }

            if (top || bottom)
            {
                float pad = Mathf.Clamp(gap, 0f, Mathf.Max(0f, (spec.h - 2f) * 0.5f));
                float itemGap = between ? 0f : Mathf.Max(0f, gap);
                float labelH = Mathf.Clamp(spec.fontSize + 8f, 1f, Mathf.Max(1f, spec.h - 4f));
                float counterH = Mathf.Clamp(spec.counterFontSize + 8f, 1f, Mathf.Max(1f, spec.h - 4f));
                rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
                if (between)
                {
                    if (bottom)
                    {
                        rt.anchoredPosition = counter
                            ? new Vector2(2f, pad)
                            : new Vector2(2f, Mathf.Max(pad, spec.h - pad - labelH));
                        rt.sizeDelta = new Vector2(spec.w - 4f, counter ? counterH : labelH);
                    }
                    else
                    {
                        rt.anchoredPosition = counter
                            ? new Vector2(2f, Mathf.Max(pad, spec.h - pad - counterH))
                            : new Vector2(2f, pad);
                        rt.sizeDelta = new Vector2(spec.w - 4f, counter ? counterH : labelH);
                    }
                }
                else
                {
                    float groupH = Mathf.Min(spec.h - 4f, labelH + counterH + itemGap);
                    float y0 = Mathf.Max(2f, (spec.h - groupH) * 0.5f);
                    if (counter)
                    {
                        rt.anchoredPosition = bottom
                            ? new Vector2(2f, y0)
                            : new Vector2(2f, y0 + labelH + itemGap);
                        rt.sizeDelta = new Vector2(spec.w - 4f, counterH);
                    }
                    else
                    {
                        rt.anchoredPosition = bottom
                            ? new Vector2(2f, y0 + counterH + itemGap)
                            : new Vector2(2f, y0);
                        rt.sizeDelta = new Vector2(spec.w - 4f, labelH);
                    }
                }
                return;
            }

            if (left || right)
            {
                float pad = Mathf.Clamp(gap, 0f, Mathf.Max(0f, (spec.w - 2f) * 0.5f));
                float itemGap = between ? 0f : Mathf.Max(0f, gap);
                float labelW = Mathf.Clamp(spec.fontSize * Mathf.Max(1f, (spec.displayText ?? "").Length) * 0.58f + 4f, 1f, Mathf.Max(1f, spec.w - 4f));
                float counterW = Mathf.Clamp(spec.counterFontSize * 3f, 1f, Mathf.Max(1f, spec.w - 4f));
                rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
                if (between)
                {
                    if (right)
                    {
                        rt.anchoredPosition = counter
                            ? new Vector2(Mathf.Max(pad, spec.w - pad - counterW), 2f)
                            : new Vector2(pad, 2f);
                        rt.sizeDelta = new Vector2(counter ? counterW : labelW, spec.h - 4f);
                    }
                    else
                    {
                        rt.anchoredPosition = counter
                            ? new Vector2(pad, 2f)
                            : new Vector2(Mathf.Max(pad, spec.w - pad - labelW), 2f);
                        rt.sizeDelta = new Vector2(counter ? counterW : labelW, spec.h - 4f);
                    }
                }
                else
                {
                    float groupW = Mathf.Min(spec.w - 4f, labelW + counterW + itemGap);
                    float x0 = Mathf.Max(2f, (spec.w - groupW) * 0.5f);
                    if (right)
                    {
                        rt.anchoredPosition = counter
                            ? new Vector2(x0 + labelW + itemGap, 2f)
                            : new Vector2(x0, 2f);
                        rt.sizeDelta = new Vector2(counter ? counterW : labelW, spec.h - 4f);
                    }
                    else
                    {
                        rt.anchoredPosition = counter
                            ? new Vector2(x0, 2f)
                            : new Vector2(x0 + counterW + itemGap, 2f);
                        rt.sizeDelta = new Vector2(counter ? counterW : labelW, spec.h - 4f);
                    }
                }
                return;
            }

            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(spec.w - 4f, spec.h - 4f);
        }

        private static TextAlignmentOptions DmNoteCounterAlignment(DmNoteSpec spec, bool counter)
        {
            string align = spec.counterAlign;
            bool between = string.Equals(spec.counterAlignMode, "between", StringComparison.OrdinalIgnoreCase);
            if (!between && (string.Equals(align, "top", StringComparison.OrdinalIgnoreCase)
                || string.Equals(align, "bottom", StringComparison.OrdinalIgnoreCase)))
                return TextAlignmentOptions.Center;
            if (string.Equals(align, "top", StringComparison.OrdinalIgnoreCase))
                return counter ? TextAlignmentOptions.Bottom : TextAlignmentOptions.Top;
            if (string.Equals(align, "bottom", StringComparison.OrdinalIgnoreCase))
                return counter ? TextAlignmentOptions.Top : TextAlignmentOptions.Bottom;
            if (!between && (string.Equals(align, "left", StringComparison.OrdinalIgnoreCase)
                || string.Equals(align, "right", StringComparison.OrdinalIgnoreCase)))
                return TextAlignmentOptions.Center;
            if (string.Equals(align, "left", StringComparison.OrdinalIgnoreCase))
                return counter ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
            if (string.Equals(align, "right", StringComparison.OrdinalIgnoreCase))
                return counter ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            return TextAlignmentOptions.Center;
        }

        private void ProcessDmNoteKeysInUpdate(long now)
        {
            if (Keys == null || dmNoteRuntimeKeys == null) return;
            int currentKps = PressTimes != null ? PruneDmNoteKps(now) : 0;
            int total = global::KorenResourcePack.KeyViewer.GetKeyViewerTotal();
            int limiterMode = Mathf.Clamp(global::KorenResourcePack.Main.settings.KeyViewerAdvancedOutOfLimiterMode, 0, 2);

            for (int i = 0; i < Keys.Length && i < dmNoteRuntimeKeys.Length; i++)
            {
                Key key = Keys[i];
                DmNoteSpec spec = dmNoteRuntimeKeys[i];
                if (key == null || spec == null) continue;

                if (spec.isStat)
                {
                    int value = spec.isKps ? currentKps : (spec.isTotal ? total : 0);
                    if (key.value != null)
                        key.value.text = FormatCount(value);
                    else if (key.text != null && key.lastCounterValue != value)
                        key.text.text = spec.inlineStatCounter ? DmNoteInlineStatText(spec, value) : spec.displayText;
                    key.lastCounterValue = value;
                    continue;
                }

                bool rawPressed = spec.keyCode != KeyCode.None && IsConfiguredDmNoteKeyDown(spec.keyCode);
                bool rawGhostPressed = spec.ghostKeyCode != KeyCode.None && IsConfiguredDmNoteKeyDown(spec.ghostKeyCode);
                bool limiterBlocked = spec.keyCode != KeyCode.None && rawPressed && global::KorenResourcePack.KeyLimiter.ShouldBlockKey(spec.keyCode);
                bool limiterHidden = limiterBlocked && limiterMode == 0;
                bool limiterGhostPressed = limiterBlocked && limiterMode == 1;
                bool limiterFullPressed = limiterBlocked && limiterMode == 2;
                bool pressed = !limiterHidden
                    && !limiterGhostPressed
                    && ApplyDmNoteInputFilters(spec.keyCode, rawPressed, key.isPressed, ref key.ignoredPress);
                bool ghostPressed = limiterGhostPressed || rawGhostPressed;
                bool visualPressed = pressed;

                if (pressed != key.isPressed)
                {
                    ApplyDmNoteVisualState(key, spec, visualPressed);
                    key.isPressed = pressed;
                    if (pressed)
                    {
                        if (DmNoteRainEnabled())
                            rainSystem.TriggerRainEffect(i, key, now);
                        if (!limiterFullPressed)
                        {
                            key.count++;
                            if (PressTimes != null) PressTimes.Enqueue(now);
                            global::KorenResourcePack.KeyViewer.SetKeyViewerCount(spec.countPrefKey, key.count);
                        }
                        global::KorenResourcePack.KeyViewer.SetKeyViewerTotal(total + 1);
                        total++;
                    }
                    else
                    {
                        rainSystem.ReleaseRainEffect(i, key, now);
                    }
                }
                else if (visualPressed != (key.background != null && key.background.color == spec.activeBg))
                {
                    ApplyDmNoteVisualState(key, spec, visualPressed);
                }

                if (ghostPressed && !key.wasGhostPressed && DmNoteRainEnabled())
                    rainSystem.TriggerGhostRain(i, key, now);
                else if (!ghostPressed && key.wasGhostPressed)
                    rainSystem.ReleaseGhostRain(i, key, now);
                key.wasGhostPressed = ghostPressed;

                if (limiterGhostPressed && !key.wasLimiterGhostPressed)
                {
                    global::KorenResourcePack.KeyViewer.SetKeyViewerTotal(total + 1);
                    total++;
                }
                key.wasLimiterGhostPressed = limiterGhostPressed;

                if (key.value != null && key.lastCounterValue != key.count)
                {
                    key.lastCounterValue = key.count;
                    key.value.text = FormatCount(key.count);
                }
            }
        }

        private int PruneDmNoteKps(long now)
        {
            while (PressTimes.Count > 0 && now - PressTimes.Peek() > 1000)
                PressTimes.Dequeue();
            return PressTimes.Count;
        }

        private void RefreshDmNoteCountDisplay()
        {
            if (Keys == null || dmNoteRuntimeKeys == null) return;
            int total = global::KorenResourcePack.KeyViewer.GetKeyViewerTotal();
            for (int i = 0; i < Keys.Length && i < dmNoteRuntimeKeys.Length; i++)
            {
                Key key = Keys[i];
                DmNoteSpec spec = dmNoteRuntimeKeys[i];
                if (key == null || spec == null) continue;
                if (spec.isTotal && key.value != null) key.value.text = FormatCount(total);
                else if (spec.isTotal && spec.inlineStatCounter && key.text != null) key.text.text = DmNoteInlineStatText(spec, total);
                else if (spec.isKps && spec.inlineStatCounter && key.text != null) key.text.text = DmNoteInlineStatText(spec, 0);
                if (!spec.isStat && key.value != null) key.value.text = FormatCount(key.count);
            }
        }

        private static string DmNoteInlineStatText(DmNoteSpec spec, int value)
        {
            return (spec.displayText ?? "") + "  " + FormatCount(value);
        }

        private static bool IsConfiguredDmNoteKeyDown(KeyCode key)
        {
            key = global::KorenResourcePack.KeyCodeCompat.NormalizeKey(key);
            try { if (Input.GetKey(key)) return true; } catch { }
            return global::KorenResourcePack.KeyViewer.IsRawKeyDown(key);
        }

        private static bool ApplyDmNoteInputFilters(KeyCode key, bool rawPressed, bool wasPressed, ref bool ignoredPress)
        {
            if (!rawPressed)
            {
                ignoredPress = false;
                return false;
            }
            if (ignoredPress) return false;
            if (!wasPressed && !global::KorenResourcePack.ChatterBlocker.AcceptKeyViewerPress(key))
            {
                ignoredPress = true;
                return false;
            }
            return true;
        }

        private static void ApplyDmNoteVisualState(Key key, DmNoteSpec spec, bool pressed)
        {
            if (key.background != null) key.background.color = pressed ? spec.activeBg : spec.bg;
            if (key.outline != null) key.outline.color = pressed ? spec.activeOutline : spec.outline;
            if (key.text != null) key.text.color = pressed ? spec.activeText : spec.text;
            if (key.value != null) key.value.color = pressed ? spec.activeCounterText : spec.counterText;
        }

        private static KeyCode ResolveDmNoteKeyCode(string name)
        {
            if (string.IsNullOrEmpty(name)) return KeyCode.None;
            int numeric;
            if (int.TryParse(name, out numeric))
                return global::KorenResourcePack.KeyCodeCompat.NormalizeKey((KeyCode)numeric);

            KeyCode shared = global::KorenResourcePack.KeyViewer.ResolveKeyCode(name);
            if (shared != KeyCode.None)
                return global::KorenResourcePack.KeyCodeCompat.NormalizeKey(shared);

            string normalized = name.Replace(" ", "").Replace("_", "");
            KeyCode key;
            if (Enum.TryParse(normalized, true, out key))
                return global::KorenResourcePack.KeyCodeCompat.NormalizeKey(key);

            switch (normalized.ToUpperInvariant())
            {
                case "DOT": return KeyCode.Period;
                case "PERIOD": return KeyCode.Period;
                case "ENTER": return KeyCode.Return;
                case "ESC": return KeyCode.Escape;
                case "LCONTROL":
                case "LCTRL": return KeyCode.LeftControl;
                case "RCONTROL":
                case "RCTRL":
                case "HANJA": return KeyCode.RightControl;
                case "RALT":
                case "ALTGR":
                case "HANGUL": return KeyCode.RightAlt;
                case "LALT": return KeyCode.LeftAlt;
                case "INS": return KeyCode.Insert;
                case "CONTEXTMENU": return KeyCode.Menu;
                case "BACKSLASH": return KeyCode.Backslash;
                case "SLASH": return KeyCode.Slash;
                case "CAPSLOCK": return KeyCode.CapsLock;
                case "SPACE": return KeyCode.Space;
            }

            if (normalized.Length == 1)
            {
                char c = char.ToUpperInvariant(normalized[0]);
                if (c >= 'A' && c <= 'Z') return (KeyCode)((int)KeyCode.A + (c - 'A'));
                if (c >= '0' && c <= '9') return (KeyCode)((int)KeyCode.Alpha0 + (c - '0'));
            }
            return KeyCode.None;
        }

        private static string DefaultDmNoteDisplay(string keyName, bool stat, bool counterEnabled)
        {
            if (stat)
            {
                if (keyName.Equals("kps", StringComparison.OrdinalIgnoreCase)) return "KPS";
                if (keyName.Equals("total", StringComparison.OrdinalIgnoreCase)) return "Total";
                return keyName.ToUpperInvariant();
            }
            if (string.IsNullOrEmpty(keyName)) return "";
            string display;
            if (TryDmNoteDisplayName(keyName, out display)) return display;
            return keyName;
        }

        private static bool TryDmNoteDisplayName(string keyName, out string display)
        {
            display = null;
            if (string.IsNullOrEmpty(keyName)) return false;
            if (DmNoteDisplayOverrides.TryGetValue(keyName, out display)) return true;

            string shared = global::KorenResourcePack.KeyViewer.DefaultDisplayFor(keyName);
            if (!string.Equals(shared, keyName, StringComparison.Ordinal))
            {
                display = shared;
                return true;
            }

            string normalized = NormalizeDmNoteDisplayKey(keyName);
            if (!string.Equals(normalized, keyName, StringComparison.OrdinalIgnoreCase))
            {
                if (DmNoteDisplayOverrides.TryGetValue(normalized, out display)) return true;
                shared = global::KorenResourcePack.KeyViewer.DefaultDisplayFor(normalized);
                if (!string.Equals(shared, normalized, StringComparison.Ordinal))
                {
                    display = shared;
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeDmNoteDisplayKey(string keyName)
        {
            string s = (keyName ?? "").Replace("_", " ").Trim();
            while (s.IndexOf("  ", StringComparison.Ordinal) >= 0)
                s = s.Replace("  ", " ");
            return s.ToUpperInvariant();
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
                            float r = ParseDmNoteColorComponent(parts[0], 255f);
                            float g = ParseDmNoteColorComponent(parts[1], 255f);
                            float b = ParseDmNoteColorComponent(parts[2], 255f);
                            float a = parts.Length >= 4 ? ParseDmNoteAlphaComponent(parts[3]) : alpha;
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

        private static float ParseDmNoteColorComponent(string s, float scale)
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

        private static float ParseDmNoteAlphaComponent(string s)
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
                return Mathf.Clamp01(v <= 1f ? v : v / 255f);
            return 1f;
        }

        private static string JStr(JObject p, string key, string def)
        {
            JToken t = p != null ? p[key] : null;
            return t == null || t.Type == JTokenType.Null ? def : t.ToString();
        }

        private static string JOptionalString(JObject p, string key)
        {
            JToken t = p != null ? p[key] : null;
            return t == null || t.Type == JTokenType.Null ? null : t.ToString();
        }

        private static float JFloat(JObject p, string key, float def)
        {
            JToken t = p != null ? p[key] : null;
            if (t == null || t.Type == JTokenType.Null) return def;
            try { return t.ToObject<float>(); } catch { return def; }
        }

        private static int JInt(JObject p, string key, int def)
        {
            JToken t = p != null ? p[key] : null;
            if (t == null || t.Type == JTokenType.Null) return def;
            try { return t.ToObject<int>(); } catch { return def; }
        }

        private static bool JBool(JObject p, string key, bool def)
        {
            JToken t = p != null ? p[key] : null;
            if (t == null || t.Type == JTokenType.Null) return def;
            try { return t.ToObject<bool>(); } catch { return def; }
        }
    }
}
