using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KorenResourcePack
{
    /// <summary>
    /// Builds the JSON preset for KeyViewer "Simple" mode at runtime, reading from the
    /// user's Simple-mode settings (KeyCode arrays, color slots, displayText overrides).
    /// The dmnote-style renderer in KeyViewer.cs is unchanged — Simple mode is just a
    /// generated preset that happens to honor the user's customizations.
    ///
        /// JSON is rebuilt only when the generated layout settings change. The renderer's
        /// parse cache (lastParsedPresetJson) keys on the raw string, so any mutation
        /// (style switch, key rebind, color change) flips the cache and triggers a clean
        /// rebuild on the next draw.
    /// </summary>
    internal static class SimplePresets
    {
        public const string TabName = "simple";

        // Visual constants. ~50px keys with row spacing matching Jipper's 54px.
        private const float KeyW = 50f;
        private const float KeyH = 50f;
        private const float WideW = 77f;
        private const float RowGap = 54f;

        // Slot indices per Jipper BackSequence. The "back" rows render in this visual order.
        private static readonly byte[] BackSeq16 = { 12, 13, 9, 8, 10, 11, 14, 15 };
        private static int cachedStyle = -1;
        private static int cachedHash;
        private static string cachedJson;

        public static string GetJson(int style)
        {
            if (style < 0) style = 0; else if (style > 3) style = 3;
            int hash = ComputeSettingsHash(style);
            if (cachedJson != null && cachedStyle == style && cachedHash == hash)
                return cachedJson;

            JArray keyArr = new JArray();
            JArray posArr = new JArray();
            JArray statArr = new JArray();

            switch (style)
            {
                case 0: BuildKey10(keyArr, posArr, statArr); break;
                case 1: BuildKey12(keyArr, posArr, statArr); break;
                case 2: BuildKey16(keyArr, posArr, statArr); break;
                case 3: BuildKey20(keyArr, posArr, statArr); break;
            }
            BuildFootKeys(style, keyArr, posArr);

            JObject keys = new JObject(); keys[TabName] = keyArr;
            JObject pos = new JObject(); pos[TabName] = posArr;
            JObject stat = new JObject(); stat[TabName] = statArr;

            JObject root = new JObject();
            root["keys"] = keys;
            root["keyPositions"] = pos;
            root["statPositions"] = stat;
            root["selectedKeyType"] = TabName;
            cachedStyle = style;
            cachedHash = hash;
            cachedJson = root.ToString(Newtonsoft.Json.Formatting.None);
            return cachedJson;
        }

        private static int ComputeSettingsHash(int style)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + style;
                hash = hash * 31 + (Main.SettingsRef.KeyViewerSimpleUseRain ? 1 : 0);
                hash = hash * 31 + (Main.SettingsRef.KeyViewerSimpleUseGhostRain ? 1 : 0);
                hash = hash * 31 + Main.SettingsRef.KeyViewerSimpleFootStyle;
                AddArrayHash(ref hash, CodesFor(style));
                AddArrayHash(ref hash, LabelsFor(style));
                AddArrayHash(ref hash, GhostCodesFor(style));
                AddArrayHash(ref hash, FootCodesFor(Main.SettingsRef.KeyViewerSimpleFootStyle));
                AddFloatHash(ref hash, Main.SettingsRef.SKvBgR);    AddFloatHash(ref hash, Main.SettingsRef.SKvBgG);
                AddFloatHash(ref hash, Main.SettingsRef.SKvBgB);    AddFloatHash(ref hash, Main.SettingsRef.SKvBgA);
                AddFloatHash(ref hash, Main.SettingsRef.SKvBgcR);   AddFloatHash(ref hash, Main.SettingsRef.SKvBgcG);
                AddFloatHash(ref hash, Main.SettingsRef.SKvBgcB);   AddFloatHash(ref hash, Main.SettingsRef.SKvBgcA);
                AddFloatHash(ref hash, Main.SettingsRef.SKvOutR);   AddFloatHash(ref hash, Main.SettingsRef.SKvOutG);
                AddFloatHash(ref hash, Main.SettingsRef.SKvOutB);   AddFloatHash(ref hash, Main.SettingsRef.SKvOutA);
                AddFloatHash(ref hash, Main.SettingsRef.SKvOutcR);  AddFloatHash(ref hash, Main.SettingsRef.SKvOutcG);
                AddFloatHash(ref hash, Main.SettingsRef.SKvOutcB);  AddFloatHash(ref hash, Main.SettingsRef.SKvOutcA);
                AddFloatHash(ref hash, Main.SettingsRef.SKvTxtR);   AddFloatHash(ref hash, Main.SettingsRef.SKvTxtG);
                AddFloatHash(ref hash, Main.SettingsRef.SKvTxtB);   AddFloatHash(ref hash, Main.SettingsRef.SKvTxtA);
                AddFloatHash(ref hash, Main.SettingsRef.SKvTxtcR);  AddFloatHash(ref hash, Main.SettingsRef.SKvTxtcG);
                AddFloatHash(ref hash, Main.SettingsRef.SKvTxtcB);  AddFloatHash(ref hash, Main.SettingsRef.SKvTxtcA);
                AddFloatHash(ref hash, Main.SettingsRef.SKvRainR);  AddFloatHash(ref hash, Main.SettingsRef.SKvRainG);
                AddFloatHash(ref hash, Main.SettingsRef.SKvRainB);  AddFloatHash(ref hash, Main.SettingsRef.SKvRainA);
                AddFloatHash(ref hash, Main.SettingsRef.SKvRain2R); AddFloatHash(ref hash, Main.SettingsRef.SKvRain2G);
                AddFloatHash(ref hash, Main.SettingsRef.SKvRain2B); AddFloatHash(ref hash, Main.SettingsRef.SKvRain2A);
                AddFloatHash(ref hash, Main.SettingsRef.SKvRain3R); AddFloatHash(ref hash, Main.SettingsRef.SKvRain3G);
                AddFloatHash(ref hash, Main.SettingsRef.SKvRain3B); AddFloatHash(ref hash, Main.SettingsRef.SKvRain3A);
                return hash;
            }
        }

        private static void AddArrayHash(ref int hash, int[] values)
        {
            if (values == null) return;
            for (int i = 0; i < values.Length; i++)
                hash = hash * 31 + values[i];
        }

        private static void AddArrayHash(ref int hash, string[] values)
        {
            if (values == null) return;
            for (int i = 0; i < values.Length; i++)
                hash = hash * 31 + (values[i] == null ? 0 : values[i].GetHashCode());
        }

        private static void AddFloatHash(ref int hash, float value)
        {
            hash = hash * 31 + value.GetHashCode();
        }

        private static int[] CodesFor(int style)
        {
            switch (style)
            {
                case 0: return Main.SettingsRef.KeyViewerSimpleKey10;
                case 1: return Main.SettingsRef.KeyViewerSimpleKey12;
                case 2: return Main.SettingsRef.KeyViewerSimpleKey16;
                case 3: return Main.SettingsRef.KeyViewerSimpleKey20;
                default: return Main.SettingsRef.KeyViewerSimpleKey12;
            }
        }

        private static string[] LabelsFor(int style)
        {
            switch (style)
            {
                case 0: return Main.SettingsRef.KeyViewerSimpleKey10Text;
                case 1: return Main.SettingsRef.KeyViewerSimpleKey12Text;
                case 2: return Main.SettingsRef.KeyViewerSimpleKey16Text;
                case 3: return Main.SettingsRef.KeyViewerSimpleKey20Text;
                default: return Main.SettingsRef.KeyViewerSimpleKey12Text;
            }
        }

        private static int[] GhostCodesFor(int style)
        {
            switch (style)
            {
                case 0: return Main.SettingsRef.KeyViewerSimpleGhostKey10;
                case 1: return Main.SettingsRef.KeyViewerSimpleGhostKey12;
                case 2: return Main.SettingsRef.KeyViewerSimpleGhostKey16;
                case 3: return Main.SettingsRef.KeyViewerSimpleGhostKey20;
                default: return Main.SettingsRef.KeyViewerSimpleGhostKey12;
            }
        }

        private static int[] FootCodesFor(int footStyle)
        {
            switch (footStyle)
            {
                case 1: return Main.SettingsRef.KeyViewerSimpleFootKey2;
                case 2: return Main.SettingsRef.KeyViewerSimpleFootKey4;
                case 3: return Main.SettingsRef.KeyViewerSimpleFootKey6;
                case 4: return Main.SettingsRef.KeyViewerSimpleFootKey8;
                case 5: return Main.SettingsRef.KeyViewerSimpleFootKey16;
                default: return null;
            }
        }

        private static string SlotLabel(int style, int slot)
        {
            string[] over = LabelsFor(style);
            if (slot >= 0 && slot < over.Length && !string.IsNullOrEmpty(over[slot])) return over[slot];
            int[] codes = CodesFor(style);
            if (slot < 0 || slot >= codes.Length) return "";
            return KeyCodeShortLabel((KeyCode)codes[slot]);
        }

        // Trim KeyCode names to short labels.
        private static string KeyCodeShortLabel(KeyCode kc)
        {
            string s = kc.ToString();
            if (s.StartsWith("Alpha")) s = s.Substring(5);
            if (s.StartsWith("Keypad")) s = "N" + s.Substring(6);
            if (s.StartsWith("Left")) s = "L" + s.Substring(4);
            if (s.StartsWith("Right")) s = "R" + s.Substring(5);
            if (s.EndsWith("Shift")) s = s.Substring(0, s.Length - 5) + "⇧";
            if (s.EndsWith("Control")) s = s.Substring(0, s.Length - 7) + "Ctrl";
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
                case "None": return "";
                default: return s;
            }
        }

        private static void BuildKey10(JArray keyArr, JArray posArr, JArray statArr)
        {
            for (int i = 0; i < 8; i++) AppendKey(keyArr, posArr, 0, i, 54f * i, 0f, KeyW, KeyH);
            AppendKey(keyArr, posArr, 0, 8, 81f, RowGap, 131f, KeyH);
            AppendKey(keyArr, posArr, 0, 9, 54f * 4f, RowGap, 131f, KeyH);
            AppendStat(statArr, "kps", 0f, RowGap, WideW, KeyH);
            AppendStat(statArr, "total", 81f + 54f * 5f, RowGap, WideW, KeyH);
        }

        private static void BuildKey12(JArray keyArr, JArray posArr, JArray statArr)
        {
            for (int i = 0; i < 8; i++) AppendKey(keyArr, posArr, 1, i, 54f * i, 0f, KeyW, KeyH);
            AppendKey(keyArr, posArr, 1, 9, 81f, RowGap, KeyW, KeyH);
            AppendKey(keyArr, posArr, 1, 8, 81f + 54f, RowGap, WideW, KeyH);
            AppendKey(keyArr, posArr, 1, 10, 54f * 4f, RowGap, WideW, KeyH);
            AppendKey(keyArr, posArr, 1, 11, 54f * 4f + 81f, RowGap, KeyW, KeyH);
            AppendStat(statArr, "kps", 0f, RowGap, WideW, KeyH);
            AppendStat(statArr, "total", 81f + 54f * 5f, RowGap, WideW, KeyH);
        }

        private static void BuildKey16(JArray keyArr, JArray posArr, JArray statArr)
        {
            for (int i = 0; i < 8; i++) AppendKey(keyArr, posArr, 2, i, 54f * i, 0f, KeyW, KeyH);
            for (int i = 0; i < 8; i++)
            {
                int slot = BackSeq16[i];
                AppendKey(keyArr, posArr, 2, slot, 54f * i, RowGap, KeyW, KeyH);
            }
            AppendStat(statArr, "kps", 0f, 120f, 212f, 30f);
            AppendStat(statArr, "total", 216f, 120f, 212f, 30f);
        }

        private static void BuildKey20(JArray keyArr, JArray posArr, JArray statArr)
        {
            for (int i = 0; i < 8; i++) AppendKey(keyArr, posArr, 3, i, 54f * i, 0f, KeyW, KeyH);
            for (int i = 0; i < 8; i++)
            {
                int slot = BackSeq16[i];
                AppendKey(keyArr, posArr, 3, slot, 54f * i, RowGap, KeyW, KeyH);
            }
            AppendKey(keyArr, posArr, 3, 17, 81f, RowGap * 2f, KeyW, KeyH);
            AppendKey(keyArr, posArr, 3, 16, 81f + 54f, RowGap * 2f, WideW, KeyH);
            AppendKey(keyArr, posArr, 3, 18, 54f * 4f, RowGap * 2f, WideW, KeyH);
            AppendKey(keyArr, posArr, 3, 19, 54f * 4f + 81f, RowGap * 2f, KeyW, KeyH);
            AppendStat(statArr, "kps", 0f, RowGap * 2f, WideW, KeyH);
            AppendStat(statArr, "total", 81f + 54f * 5f, RowGap * 2f, WideW, KeyH);
        }

        private static void AppendKey(JArray keyArr, JArray posArr, int style, int slot,
                                      float dx, float dy, float w, float h)
        {
            int[] codes = CodesFor(style);
            string keyName = slot >= 0 && slot < codes.Length ? ((KeyCode)codes[slot]).ToString().ToUpperInvariant() : "";

            // Some KeyCode names don't map to KRP's KeyNameMap; include displayText so the
            // user-facing label still renders correctly even if KeyCode resolution fails.
            keyArr.Add(keyName);

            JObject p = new JObject();
            p["dx"] = dx;
            p["dy"] = dy;
            p["width"] = w;
            p["height"] = h;
            p["hidden"] = false;
            p["countKey"] = "simple_hand_" + slot;
            if (Main.SettingsRef.KeyViewerSimpleUseGhostRain)
            {
                int[] ghostCodes = GhostCodesFor(style);
                if (slot >= 0 && slot < ghostCodes.Length)
                    p["ghostKey"] = ((KeyCode)ghostCodes[slot]).ToString().ToUpperInvariant();
            }
            p["displayText"] = SlotLabel(style, slot);
            p["fontSize"] = 18;
            p["backgroundColor"] = ColRgba(Main.SettingsRef.SKvBgR, Main.SettingsRef.SKvBgG, Main.SettingsRef.SKvBgB, Main.SettingsRef.SKvBgA);
            p["activeBackgroundColor"] = ColRgba(Main.SettingsRef.SKvBgcR, Main.SettingsRef.SKvBgcG, Main.SettingsRef.SKvBgcB, Main.SettingsRef.SKvBgcA);
            p["borderColor"] = ColRgba(Main.SettingsRef.SKvOutR, Main.SettingsRef.SKvOutG, Main.SettingsRef.SKvOutB, Main.SettingsRef.SKvOutA);
            p["activeBorderColor"] = ColRgba(Main.SettingsRef.SKvOutcR, Main.SettingsRef.SKvOutcG, Main.SettingsRef.SKvOutcB, Main.SettingsRef.SKvOutcA);
            p["borderWidth"] = 2;
            p["borderRadius"] = 8;
            p["fontColor"] = ColRgba(Main.SettingsRef.SKvTxtR, Main.SettingsRef.SKvTxtG, Main.SettingsRef.SKvTxtB, Main.SettingsRef.SKvTxtA);
            p["activeFontColor"] = ColRgba(Main.SettingsRef.SKvTxtcR, Main.SettingsRef.SKvTxtcG, Main.SettingsRef.SKvTxtcB, Main.SettingsRef.SKvTxtcA);
            // Pick rain color group: extras row (style==3, slot 16-19) -> Rain3, top row -> Rain1, others -> Rain2.
            // The visual mapping mirrors Jipper's `color` field per slot.
            float rR, rG, rB, rA;
            int colorGroup = SlotRainGroup(style, slot);
            switch (colorGroup)
            {
                case 1:
                    rR = Main.SettingsRef.SKvRainR; rG = Main.SettingsRef.SKvRainG;
                    rB = Main.SettingsRef.SKvRainB; rA = Main.SettingsRef.SKvRainA; break;
                case 3:
                    rR = Main.SettingsRef.SKvRain3R; rG = Main.SettingsRef.SKvRain3G;
                    rB = Main.SettingsRef.SKvRain3B; rA = Main.SettingsRef.SKvRain3A; break;
                default:
                    rR = Main.SettingsRef.SKvRain2R; rG = Main.SettingsRef.SKvRain2G;
                    rB = Main.SettingsRef.SKvRain2B; rA = Main.SettingsRef.SKvRain2A; break;
            }
            p["noteColor"] = ColRgba(rR, rG, rB, rA);
            p["noteOpacity"] = 100;
            p["noteAlignment"] = "center";
            p["noteEffectEnabled"] = Main.SettingsRef.KeyViewerSimpleUseRain;
            JObject counter = new JObject();
            counter["enabled"] = true;
            counter["align"] = "bottom";
            counter["fontSize"] = 14;
            JObject counterFill = new JObject();
            counterFill["idle"] = ColRgba(Main.SettingsRef.SKvTxtR, Main.SettingsRef.SKvTxtG, Main.SettingsRef.SKvTxtB, Main.SettingsRef.SKvTxtA);
            counterFill["active"] = ColRgba(Main.SettingsRef.SKvTxtcR, Main.SettingsRef.SKvTxtcG, Main.SettingsRef.SKvTxtcB, Main.SettingsRef.SKvTxtcA);
            counter["fill"] = counterFill;
            p["counter"] = counter;
            posArr.Add(p);
        }

        private static void BuildFootKeys(int style, JArray keyArr, JArray posArr)
        {
            int[] footCodes = FootCodesFor(Main.SettingsRef.KeyViewerSimpleFootStyle);
            if (footCodes == null || footCodes.Length == 0) return;

            float y = FootYForStyle(style);
            bool twoLine = footCodes.Length > 10;
            int rowSize = twoLine ? footCodes.Length / 2 : footCodes.Length;
            for (int line = 0; line < (twoLine ? 2 : 1); line++)
            {
                float x = 432f;
                int baseSlot = line * rowSize;
                for (int start = 0; start < 2; start++)
                {
                    for (int i = start; i < rowSize; i += 2)
                    {
                        AppendFootKey(keyArr, posArr, footCodes, baseSlot + i, x, y + line * 30f);
                        x += 34f;
                    }
                }
            }
        }

        private static float FootYForStyle(int style)
        {
            switch (style)
            {
                case 2: return 125f;
                case 3: return 138f;
                default: return 84f;
            }
        }

        private static void AppendFootKey(JArray keyArr, JArray posArr, int[] footCodes, int slot,
                                          float dx, float dy)
        {
            string keyName = slot >= 0 && slot < footCodes.Length
                ? ((KeyCode)footCodes[slot]).ToString().ToUpperInvariant()
                : "";
            keyArr.Add(keyName);

            JObject p = new JObject();
            p["dx"] = dx;
            p["dy"] = dy;
            p["width"] = 30f;
            p["height"] = 30f;
            p["hidden"] = false;
            p["countKey"] = "simple_foot_" + slot;
            p["displayText"] = KeyCodeShortLabel((KeyCode)footCodes[slot]);
            p["fontSize"] = 13;
            p["backgroundColor"] = ColRgba(Main.SettingsRef.SKvBgR, Main.SettingsRef.SKvBgG, Main.SettingsRef.SKvBgB, Main.SettingsRef.SKvBgA);
            p["activeBackgroundColor"] = ColRgba(Main.SettingsRef.SKvBgcR, Main.SettingsRef.SKvBgcG, Main.SettingsRef.SKvBgcB, Main.SettingsRef.SKvBgcA);
            p["borderColor"] = ColRgba(Main.SettingsRef.SKvOutR, Main.SettingsRef.SKvOutG, Main.SettingsRef.SKvOutB, Main.SettingsRef.SKvOutA);
            p["activeBorderColor"] = ColRgba(Main.SettingsRef.SKvOutcR, Main.SettingsRef.SKvOutcG, Main.SettingsRef.SKvOutcB, Main.SettingsRef.SKvOutcA);
            p["borderWidth"] = 2;
            p["borderRadius"] = 8;
            p["fontColor"] = ColRgba(Main.SettingsRef.SKvTxtR, Main.SettingsRef.SKvTxtG, Main.SettingsRef.SKvTxtB, Main.SettingsRef.SKvTxtA);
            p["activeFontColor"] = ColRgba(Main.SettingsRef.SKvTxtcR, Main.SettingsRef.SKvTxtcG, Main.SettingsRef.SKvTxtcB, Main.SettingsRef.SKvTxtcA);
            p["noteEffectEnabled"] = false;
            JObject counter = new JObject();
            counter["enabled"] = false;
            p["counter"] = counter;
            posArr.Add(p);
        }

        // Maps style + slot to Jipper's three-color rain grouping (1 top row, 2 second row, 3 extra row).
        private static int SlotRainGroup(int style, int slot)
        {
            if (slot < 8) return 1;
            if (style == 3 && slot >= 16) return 3;
            return 2;
        }

        private static void AppendStat(JArray statArr, string statType, float dx, float dy, float w, float h)
        {
            JObject p = new JObject();
            p["statType"] = statType;
            p["dx"] = dx;
            p["dy"] = dy;
            p["width"] = w;
            p["height"] = h;
            p["hidden"] = false;
            p["fontColor"] = ColRgba(Main.SettingsRef.SKvTxtR, Main.SettingsRef.SKvTxtG, Main.SettingsRef.SKvTxtB, Main.SettingsRef.SKvTxtA);
            p["activeFontColor"] = ColRgba(Main.SettingsRef.SKvTxtcR, Main.SettingsRef.SKvTxtcG, Main.SettingsRef.SKvTxtcB, Main.SettingsRef.SKvTxtcA);
            p["backgroundColor"] = ColRgba(Main.SettingsRef.SKvBgR, Main.SettingsRef.SKvBgG, Main.SettingsRef.SKvBgB, Main.SettingsRef.SKvBgA);
            p["activeBackgroundColor"] = ColRgba(Main.SettingsRef.SKvBgcR, Main.SettingsRef.SKvBgcG, Main.SettingsRef.SKvBgcB, Main.SettingsRef.SKvBgcA);
            p["borderColor"] = ColRgba(Main.SettingsRef.SKvOutR, Main.SettingsRef.SKvOutG, Main.SettingsRef.SKvOutB, Main.SettingsRef.SKvOutA);
            p["activeBorderColor"] = ColRgba(Main.SettingsRef.SKvOutcR, Main.SettingsRef.SKvOutcG, Main.SettingsRef.SKvOutcB, Main.SettingsRef.SKvOutcA);
            p["borderWidth"] = 2;
            p["borderRadius"] = 8;
            JObject counter = new JObject();
            counter["enabled"] = true;
            // Tall stat boxes (Key10/12 KPS/Total ~50px) use "bottom" -> stacked layout.
            // Slim stat boxes (Key16/20 KPS/Total ~30px) use "right" -> inline layout.
            counter["align"] = h >= 40f ? "bottom" : "right";
            counter["fontSize"] = 16;
            JObject counterFill = new JObject();
            counterFill["idle"] = ColRgba(Main.SettingsRef.SKvTxtR, Main.SettingsRef.SKvTxtG, Main.SettingsRef.SKvTxtB, Main.SettingsRef.SKvTxtA);
            counterFill["active"] = ColRgba(Main.SettingsRef.SKvTxtcR, Main.SettingsRef.SKvTxtcG, Main.SettingsRef.SKvTxtcB, Main.SettingsRef.SKvTxtcA);
            counter["fill"] = counterFill;
            p["counter"] = counter;
            statArr.Add(p);
        }

        private static string ColRgba(float r, float g, float b, float a)
        {
            int ri = Mathf.Clamp(Mathf.RoundToInt(r * 255f), 0, 255);
            int gi = Mathf.Clamp(Mathf.RoundToInt(g * 255f), 0, 255);
            int bi = Mathf.Clamp(Mathf.RoundToInt(b * 255f), 0, 255);
            string aStr = Mathf.Clamp01(a).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            return "rgba(" + ri + ", " + gi + ", " + bi + ", " + aStr + ")";
        }

    }
}
