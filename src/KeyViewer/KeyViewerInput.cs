
using System.Collections.Generic;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    public partial class KeyViewer : MonoBehaviour
    {
        private void ProcessKeySelection()
        {
            if (SelectedKey == -1 || changeState == 1 || !Application.isFocused) return;
            if (!Input.anyKeyDown) return;

            foreach (KeyCode keyCode in AllKeyCodes)
            {
                if (Input.GetKeyDown(keyCode))
                {
                    SetupKey(keyCode);
                    return;
                }
            }
        }

        private void SetupKey(KeyCode keyCode)
        {
            keyCode = global::KorenResourcePack.KeyCodeCompat.NormalizeKey(keyCode);
            if (changeState == 2)
            {
                KeyCode[] ghostKeyCodes = GetGhostKeyCode();
                if (SelectedKey < ghostKeyCodes.Length)
                {
                    ghostKeyCodes[SelectedKey] = keyCode;
                    if (SelectedKey < ghostKeyStates.Length)
                        ghostKeyStates[SelectedKey] = false;
                }
                SelectedKey = -1;
                SaveSettings();
                return;
            }
            KeyCode[] keyCodes = GetKeyCode();
            KeyCode[] footKeyCodes = GetFootKeyCode();
            string[] keyTexts = GetKeyText();
            if (SelectedKey < 20)
            {
                keyCodes[SelectedKey] = keyCode;
            }
            else if (footKeyCodes != null && SelectedKey - 20 < footKeyCodes.Length)
            {
                footKeyCodes[SelectedKey - 20] = keyCode;
            }
            else
            {
                SelectedKey = -1;
                return;
            }
            if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
            {
                string displayText;
                if (SelectedKey < 20 && !string.IsNullOrEmpty(keyTexts[SelectedKey]))
                    displayText = keyTexts[SelectedKey];
                else if (SelectedKey >= 20)
                {
                    string[] footTexts = GetFootKeyText();
                    int footIndex = SelectedKey - 20;
                    displayText = footTexts != null && footIndex < footTexts.Length && !string.IsNullOrEmpty(footTexts[footIndex])
                        ? footTexts[footIndex] : KeyToString(keyCode);
                }
                else
                    displayText = KeyToString(keyCode);
                Keys[SelectedKey].text.text = displayText;
            }
            SelectedKey = -1;
            SaveSettings();
        }

        static readonly Dictionary<KeyCode, string> KeyDisplayNames = new Dictionary<KeyCode, string>();

        public static string KeyToString(KeyCode keyCode)
        {
            KeyCode normalized = global::KorenResourcePack.KeyCodeCompat.NormalizeKey(keyCode);
            if (normalized != keyCode)
                keyCode = normalized;
            if (KeyDisplayNames.Count == 0 && AllKeyCodes != null)
                BuildKeyDisplayNames();
            return KeyDisplayNames.TryGetValue(keyCode, out var name) ? name : keyCode.ToString();
        }

        static void BuildKeyDisplayNames()
        {
            foreach (KeyCode k in AllKeyCodes)
            {
                string s = k.ToString();
                if (s.StartsWith("Alpha")) s = s.Substring(5);
                else if (s.StartsWith("Keypad")) s = s.Substring(6);
                else if (s.StartsWith("Left")) s = 'L' + s.Substring(4);
                else if (s.StartsWith("Right")) s = 'R' + s.Substring(5);
                else if (s.StartsWith("Mouse")) s = "M" + s.Substring(5);
                if (s.EndsWith("Shift")) s = s.Substring(0, s.Length - 5) + "\u21E7";
                else if (s.EndsWith("Control")) s = s.Substring(0, s.Length - 7) + "Ctrl";
                s = s switch
                {
                    "Plus" => "+", "Minus" => "-", "Multiply" => "*", "Divide" => "/",
                    "Enter" => "\u21B5", "Equals" => "=", "Period" => ".", "Return" => "\u21B5",
                    "None" => " ", "Tab" => "\u21E5", "Backslash" => "\\", "Backspace" => "Back",
                    "Slash" => "/", "LBracket" => "[", "RBracket" => "]", "Semicolon" => ";",
                    "Comma" => ",", "Quote" => "'", "UpArrow" => "\u2191", "DownArrow" => "\u2193",
                    "LArrow" => "\u2190", "RArrow" => "\u2192", "Space" => "\u2423",
                    "BackQuote" => "`", "PageDown" => "Pg\u2193", "PageUp" => "Pg\u2191",
                    "CapsLock" => "\u21EA", "Insert" => "Ins",
                    _ => s
                };
                KeyDisplayNames[k] = s;
            }
        }

        private static GUILayoutOption FloatFieldWidth(string text) => GUILayout.Width(Mathf.Max(30, text.Length * 9));


        private void ProcessMainAndFootKeysInUpdate(long elapsedMilliseconds)
        {
            if (cachedKeyStyle != Settings.KeyViewerStyle)
            {
                cachedMainKeys = GetKeyCode();
                cachedGhostKeys = GetGhostKeyCode();
                cachedKeyStyle = Settings.KeyViewerStyle;
                ghostKeyStates = new bool[cachedGhostKeys.Length];
            }
            else if (cachedGhostKeys == null)
            {
                cachedGhostKeys = GetGhostKeyCode();
                ghostKeyStates = new bool[cachedGhostKeys.Length];
            }
            if (cachedFootStyle != Settings.FootKeyViewerStyle)
            {
                cachedFootKeys = GetFootKeyCode();
                cachedFootStyle = Settings.FootKeyViewerStyle;
            }
            ProcessKeyGroup(cachedMainKeys, 0, elapsedMilliseconds);
            if (cachedFootKeys != null)
                ProcessKeyGroup(cachedFootKeys, 20, elapsedMilliseconds);
            if (Total != null && Total.value != null && lastTotal != Settings.TotalCount)
            {
                lastTotal = Settings.TotalCount;
                Total.value.text = FormatCount(lastTotal);
            }
        }

        private void ProcessKeyGroup(KeyCode[] keyCodes, int baseIndex, long elapsedMs)
        {
            int[] countArr = Settings.Count;
            bool rainEnabled = Settings.EnableRainEffect;
            for (int i = 0; i < keyCodes.Length; i++)
            {
                int idx = baseIndex + i;
                if (idx >= Keys.Length) continue;
                Key key = Keys[idx];
                if (key == null) continue;
                bool current = IsConfiguredKeyDown(keyCodes[i]);
                if (current != key.isPressed)
                {
                    UpdateKeyColors(idx, current);
                    key.isPressed = current;
                    if (current)
                    {
                        countArr[idx]++;
                        Settings.TotalCount++;
                        if (key.value != null && !Settings.EnablePerKeyKps)
                            key.value.text = FormatCount(countArr[idx]);
                        PressTimes.Enqueue(elapsedMs);
                        if (keyPressTimes != null && idx < keyPressTimes.Length)
                        {
                            if (keyPressTimes[idx] == null) keyPressTimes[idx] = new Queue<long>();
                            keyPressTimes[idx].Enqueue(elapsedMs);
                        }
                        if (rainEnabled) rainSystem.TriggerRainEffect(idx, key, elapsedMs);
                    }
                    else
                    {
                        if (rainEnabled) rainSystem.ReleaseRainEffect(idx, key, elapsedMs);
                    }
                }
            }
        }

        private void ProcessKpsInUpdate(long elapsedMilliseconds)
        {
            if (PressTimes == null) return;
            while (PressTimes.Count > 0 && elapsedMilliseconds - PressTimes.Peek() > 1000)
                PressTimes.Dequeue();
            int currentKps = PressTimes.Count;
            if (lastKps != currentKps)
            {
                lastKps = currentKps;
                if (Kps != null && Kps.value != null) Kps.value.text = currentKps.ToString();
            }
        }

        private void ProcessPerKeyKpsInUpdate(long elapsedMilliseconds)
        {
            if (!Settings.EnablePerKeyKps || keyPressTimes == null || Keys == null) return;
            for (int i = 0; i < Keys.Length && i < keyPressTimes.Length; i++)
            {
                var q = keyPressTimes[i];
                if (q == null) continue;
                while (q.Count > 0 && elapsedMilliseconds - q.Peek() > 1000)
                    q.Dequeue();
                int kps = q.Count;
                if (lastPerKeyKps != null && i < lastPerKeyKps.Length && lastPerKeyKps[i] != kps)
                {
                    lastPerKeyKps[i] = kps;
                    if (Keys[i] != null && Keys[i].value != null)
                        Keys[i].value.text = kps.ToString();
                }
            }
        }

        private void ProcessGhostKeysInUpdate(long now)
        {
            if (cachedGhostKeys == null) return;
            bool rainEnabled = Settings.EnableRainEffect;
            bool ghostRainEnabled = Settings.EnableGhostRain;
            if (!rainEnabled || !ghostRainEnabled) return;

            KeyCode[] ghosts = cachedGhostKeys;
            for (int i = 0; i < ghosts.Length; i++)
            {
                if (ghosts[i] == KeyCode.None) continue;

                bool current = IsConfiguredKeyDown(ghosts[i]);
                if (current != ghostKeyStates[i])
                {
                    ghostKeyStates[i] = current;
                    if (current)
                        rainSystem.TriggerGhostRain(i, Keys[i], now);
                    else
                        rainSystem.ReleaseGhostRain(i, Keys[i], now);
                }
            }
        }

        private void UpdateKeyColors(int i, bool pressed)
        {
            if (Keys == null || i >= Keys.Length) return;
            Key key = Keys[i];
            if (key == null) return;
            if (Settings.EnablePerKeyColors && i < 36)
            {
                key.background.color = pressed ? Settings.PerKeyBackgroundClicked[i] : Settings.PerKeyBackground[i];
                key.outline.color = pressed ? Settings.PerKeyOutlineClicked[i] : Settings.PerKeyOutline[i];
                key.text.color = pressed ? Settings.PerKeyTextClicked[i] : Settings.PerKeyText[i];
            }
            else
            {
                key.background.color = pressed ? Settings.BackgroundClicked : Settings.Background;
                key.outline.color = pressed ? Settings.OutlineClicked : Settings.Outline;
                key.text.color = pressed ? Settings.TextClicked : Settings.Text;
            }
            if (key.value != null) key.value.color = key.text.color;
        }

        private static bool IsConfiguredKeyDown(KeyCode keyCode)
        {
            KeyCode normalized = global::KorenResourcePack.KeyCodeCompat.NormalizeKey(keyCode);
            return SafeInputGetKey(normalized) || global::KorenResourcePack.KeyViewer.IsRawKeyDown(normalized);
        }

        private static bool SafeInputGetKey(KeyCode keyCode)
        {
            if (keyCode == KeyCode.None) return false;
            try { return Input.GetKey(keyCode); }
            catch { return false; }
        }
    }
}
