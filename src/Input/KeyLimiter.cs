using HarmonyLib;
using MonsterLove.StateMachine;
using SkyHook;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace KorenResourcePack
{
    
    internal static class KeyLimiter
    {
        private static readonly HashSet<int> cachedAllowedKeys = new HashSet<int>();
        private static int[] cachedAllowedSource;
        private static int cachedAllowedLength = -1;
        private static int cachedPlayerControlFrame = -1;
        private static bool cachedPlayerControl;
        private static int cachedPlayerControlForHooks;

        internal static bool IsEnabled()
        {
            return Main.modEnabled && Main.settings != null && Main.settings.KeyLimiterOn;
        }

        internal static bool IsActive()
        {
            return IsEnabled() && !SettingsGui.keyLimiterCapturing;
        }

        internal static bool InPlayerControl()
        {
            int frame = Time.frameCount;
            if (cachedPlayerControlFrame == frame)
                return cachedPlayerControl;

            cachedPlayerControlFrame = frame;
            SetCachedPlayerControl(false);
            try
            {
                scrController controller = scrController.instance;
                if (controller == null) return false;
                if (controller.paused || !controller.gameworld) return false;
                SetCachedPlayerControl(((StateBehaviour)controller).stateMachine.GetState() is States state
                                      && state == States.PlayerControl);
                return cachedPlayerControl;
            }
            catch
            {
                SetCachedPlayerControl(false);
                return false;
            }
        }

        internal static void RefreshPlayerControlState()
        {
            InPlayerControl();
        }

        internal static void ResetPlayerControlState()
        {
            cachedPlayerControlFrame = -1;
            SetCachedPlayerControl(false);
        }

        internal static bool InPlayerControlCached()
        {
            return Volatile.Read(ref cachedPlayerControlForHooks) != 0;
        }

        private static void SetCachedPlayerControl(bool value)
        {
            cachedPlayerControl = value;
            Volatile.Write(ref cachedPlayerControlForHooks, value ? 1 : 0);
        }

        internal static bool IsAllowedKey(KeyCode key)
        {
            int[] allowed = Main.settings != null ? Main.settings.KeyLimiterAllowed : null;
            if (allowed == null) return false;

            if (!ReferenceEquals(allowed, cachedAllowedSource) || allowed.Length != cachedAllowedLength)
            {
                cachedAllowedKeys.Clear();
                for (int i = 0; i < allowed.Length; i++)
                    cachedAllowedKeys.Add((int)NormalizeKey((KeyCode)allowed[i]));

                cachedAllowedSource = allowed;
                cachedAllowedLength = allowed.Length;
            }

            return cachedAllowedKeys.Contains((int)NormalizeKey(key));
        }

        internal static bool IsMouseKey(KeyCode key)
        {
            return key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6;
        }

        // Right Alt on non-US Windows layouts is AltGr: Unity/Rewired surfaces the press as
        // KeyCode.AltGr while the SkyHook VK path resolves it to KeyCode.RightAlt. Collapse the
        // two so a key bound as RightAlt still matches an AltGr press and vice versa. (Capture
        // already normalizes AltGr->RightAlt when storing; this covers the gameplay allow-check,
        // which previously missed it and made RAlt look broken while LAlt worked.)
        private static KeyCode NormalizeKey(KeyCode key)
        {
            return key == KeyCode.AltGr ? KeyCode.RightAlt : key;
        }

        internal static KeyCode NormalizeKeyForComparison(KeyCode key)
        {
            return NormalizeKey(key);
        }

        internal static bool IsMouseLabel(KeyLabel label)
        {
            switch (label.ToString())
            {
                case "MouseLeft":
                case "MouseRight":
                case "MouseMiddle":
                case "MouseX1":
                case "MouseX2":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsAllowedKeyDirect(KeyCode key)
        {
            int[] allowed = Main.settings != null ? Main.settings.KeyLimiterAllowed : null;
            if (allowed == null) return false;

            int target = (int)NormalizeKey(key);
            for (int i = 0; i < allowed.Length; i++)
            {
                if ((int)NormalizeKey((KeyCode)allowed[i]) == target)
                    return true;
            }

            return false;
        }

        internal static bool ShouldBlockKey(KeyCode key)
        {
            return IsActive() && InPlayerControl() && !IsMouseKey(key) && !IsAllowedKey(key);
        }

        internal static KeyCode AsyncLabelToPhysicalUnityKey(KeyLabel label)
        {
            string name = label.ToString();

            if (name.Length == 1 && name[0] >= 'A' && name[0] <= 'Z')
                return (KeyCode)((int)KeyCode.A + (name[0] - 'A'));

            if (name.Length == 6 && name.StartsWith("Alpha") && name[5] >= '0' && name[5] <= '9')
                return (KeyCode)((int)KeyCode.Alpha0 + (name[5] - '0'));

            if (name.Length >= 2 && name[0] == 'F')
            {
                int functionKey;
                if (int.TryParse(name.Substring(1), out functionKey) && functionKey >= 1 && functionKey <= 15)
                    return (KeyCode)((int)KeyCode.F1 + (functionKey - 1));
            }

            if (name.Length == 7 && name.StartsWith("Keypad") && name[6] >= '0' && name[6] <= '9')
                return (KeyCode)((int)KeyCode.Keypad0 + (name[6] - '0'));

            switch (name)
            {
                case "Escape":          return KeyCode.Escape;
                case "Grave":           return KeyCode.BackQuote;
                case "Minus":           return KeyCode.Minus;
                case "Equal":           return KeyCode.Equals;
                case "Backspace":       return KeyCode.Backspace;
                case "Tab":             return KeyCode.Tab;
                case "LeftBrace":       return KeyCode.LeftBracket;
                case "RightBrace":      return KeyCode.RightBracket;
                case "BackSlash":       return KeyCode.Backslash;
                case "CapsLock":        return KeyCode.CapsLock;
                case "Semicolon":       return KeyCode.Semicolon;
                case "Apostrophe":      return KeyCode.Quote;
                case "Enter":           return KeyCode.Return;
                case "LShift":          return KeyCode.LeftShift;
                case "LeftShift":       return KeyCode.LeftShift;
                case "Comma":           return KeyCode.Comma;
                case "Dot":             return KeyCode.Period;
                case "Slash":           return KeyCode.Slash;
                case "RShift":          return KeyCode.RightShift;
                case "RightShift":      return KeyCode.RightShift;
                case "LControl":        return KeyCode.LeftControl;
                case "LCtrl":           return KeyCode.LeftControl;
                case "LeftControl":     return KeyCode.LeftControl;
                case "LeftCtrl":        return KeyCode.LeftControl;
                case "Super":           return KeyCode.LeftCommand;
                case "LWin":            return KeyCode.LeftWindows;
                case "LeftWin":         return KeyCode.LeftWindows;
                case "LeftWindows":     return KeyCode.LeftWindows;
                case "LAlt":            return KeyCode.LeftAlt;
                case "Space":           return KeyCode.Space;
                case "RAlt":            return KeyCode.RightAlt;
                case "AltGr":           return KeyCode.RightAlt;
                case "Hangul":          return KeyCode.RightAlt;
                case "RControl":        return KeyCode.RightControl;
                case "RCtrl":           return KeyCode.RightControl;
                case "RightControl":    return KeyCode.RightControl;
                case "RightCtrl":       return KeyCode.RightControl;
                case "Hanja":           return KeyCode.RightControl;
                case "RWin":            return KeyCode.RightWindows;
                case "RightWin":        return KeyCode.RightWindows;
                case "RightWindows":    return KeyCode.RightWindows;
                case "PrintScreen":     return KeyCode.Print;
                case "ScrollLock":      return KeyCode.ScrollLock;
                case "PauseBreak":      return KeyCode.Pause;
                case "Insert":          return KeyCode.Insert;
                case "Home":            return KeyCode.Home;
                case "PageUp":          return KeyCode.PageUp;
                case "Delete":          return KeyCode.Delete;
                case "End":             return KeyCode.End;
                case "PageDown":        return KeyCode.PageDown;
                case "ArrowUp":         return KeyCode.UpArrow;
                case "ArrowLeft":       return KeyCode.LeftArrow;
                case "ArrowDown":       return KeyCode.DownArrow;
                case "ArrowRight":      return KeyCode.RightArrow;
                case "NumLock":         return KeyCode.Numlock;
                case "KeypadSlash":     return KeyCode.KeypadDivide;
                case "KeypadAsterisk":  return KeyCode.KeypadMultiply;
                case "KeypadMinus":     return KeyCode.KeypadMinus;
                case "KeypadDot":       return KeyCode.KeypadPeriod;
                case "KeypadPlus":      return KeyCode.KeypadPlus;
                case "KeypadEnter":     return KeyCode.KeypadEnter;
                case "Application":     return KeyCode.Menu;
                case "Apps":            return KeyCode.Menu;
                case "Menu":            return KeyCode.Menu;
                case "MouseLeft":       return KeyCode.Mouse0;
                case "MouseRight":      return KeyCode.Mouse1;
                case "MouseMiddle":     return KeyCode.Mouse2;
                case "MouseX1":         return KeyCode.Mouse3;
                case "MouseX2":         return KeyCode.Mouse4;
            }

            return AsyncKeyMapper.AsyncKeyToUnityKey(label);
        }

        internal static KeyCode HookKeyToPhysicalUnityKey(ushort key, KeyLabel label)
        {
            // Numpad and arrow/navigation keys share virtual-key codes on Windows
            // (e.g. Numpad8 and Up both arrive as VK 0x26 until the extended-key flag
            // is applied), so the raw-VK path below can't tell them apart and would let
            // a numpad press through as the allowed arrow, or vice versa. SkyHook's
            // KeyLabel already carries the extended-flag disambiguation, so for that key
            // family trust the label. Mirrors the RAlt/Hangul special-case where a
            // physical key needs explicit handling to be recognized across layouts.
            KeyCode labelKey = AsyncKeyMapper.AsyncKeyToUnityKey(label);
            if (IsNumpadOrArrowKey(labelKey))
                return labelKey;

            if (IsWindowsRuntime())
            {
                KeyCode hookKey = WindowsHookKeyToUnityKey(key);
                if (hookKey != KeyCode.None)
                    return hookKey;
            }

            KeyCode mapped = AsyncLabelToPhysicalUnityKey(label);
            if (mapped != KeyCode.None)
                return mapped;

            return KeyCode.None;
        }

        // Keys whose physical identity can only be resolved with the extended-key flag:
        // the numpad cluster vs the arrow/navigation cluster that shares its VK codes.
        // For these we trust the SkyHook label rather than the raw virtual-key code.
        private static bool IsNumpadOrArrowKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.UpArrow:
                case KeyCode.DownArrow:
                case KeyCode.LeftArrow:
                case KeyCode.RightArrow:
                case KeyCode.Keypad0:
                case KeyCode.Keypad1:
                case KeyCode.Keypad2:
                case KeyCode.Keypad3:
                case KeyCode.Keypad4:
                case KeyCode.Keypad5:
                case KeyCode.Keypad6:
                case KeyCode.Keypad7:
                case KeyCode.Keypad8:
                case KeyCode.Keypad9:
                case KeyCode.KeypadPeriod:
                case KeyCode.KeypadDivide:
                case KeyCode.KeypadMultiply:
                case KeyCode.KeypadMinus:
                case KeyCode.KeypadPlus:
                case KeyCode.KeypadEnter:
                    return true;
                default:
                    return false;
            }
        }

        private static KeyCode WindowsHookKeyToUnityKey(ushort key)
        {
            switch (key)
            {
                case 0x15: // VK_HANGUL, same physical key DM Note exports as "21".
                case 0xA5: // VK_RMENU
                    return KeyCode.RightAlt;
                case 0x19: // VK_HANJA, same physical key DM Note exports as "25".
                case 0xA3: // VK_RCONTROL
                    return KeyCode.RightControl;
                case 0x5D: // VK_APPS (context-menu / application key) -> Unity KeyCode.Menu.
                    return KeyCode.Menu;
                case 0: return KeyCode.Mouse0;
                case 1: return KeyCode.Mouse1;
                case 2: return KeyCode.Mouse2;
                case 3: return KeyCode.Mouse3;
                case 4: return KeyCode.Mouse4;
                case 8: return KeyCode.Backspace;
                case 9: return KeyCode.Tab;
                case 13: return KeyCode.Return;
                case 19: return KeyCode.Pause;
                case 20: return KeyCode.CapsLock;
                case 27: return KeyCode.Escape;
                case 32: return KeyCode.Space;
                case 33: return KeyCode.PageUp;
                case 34: return KeyCode.PageDown;
                case 35: return KeyCode.End;
                case 36: return KeyCode.Home;
                case 37: return KeyCode.LeftArrow;
                case 38: return KeyCode.UpArrow;
                case 39: return KeyCode.RightArrow;
                case 40: return KeyCode.DownArrow;
                case 44: return KeyCode.Print;
                case 45: return KeyCode.Insert;
                case 46: return KeyCode.Delete;
                case 91: return KeyCode.LeftWindows;
                case 92: return KeyCode.RightWindows;
                case 106: return KeyCode.KeypadMultiply;
                case 107: return KeyCode.KeypadPlus;
                case 109: return KeyCode.KeypadMinus;
                case 110: return KeyCode.KeypadPeriod;
                case 111: return KeyCode.KeypadDivide;
                case 144: return KeyCode.Numlock;
                case 145: return KeyCode.ScrollLock;
                case 160: return KeyCode.LeftShift;
                case 161: return KeyCode.RightShift;
                case 162: return KeyCode.LeftControl;
                case 164: return KeyCode.LeftAlt;
                case 186: return KeyCode.Semicolon;
                case 187: return KeyCode.Equals;
                case 188: return KeyCode.Comma;
                case 189: return KeyCode.Minus;
                case 190: return KeyCode.Period;
                case 191: return KeyCode.Slash;
                case 192: return KeyCode.BackQuote;
                case 219: return KeyCode.LeftBracket;
                case 220: return KeyCode.Backslash;
                case 221: return KeyCode.RightBracket;
                case 222: return KeyCode.Quote;
            }

            if (key >= 48 && key <= 57)
                return (KeyCode)((int)KeyCode.Alpha0 + (key - 48));
            if (key >= 65 && key <= 90)
                return (KeyCode)((int)KeyCode.A + (key - 65));
            if (key >= 96 && key <= 105)
                return (KeyCode)((int)KeyCode.Keypad0 + (key - 96));
            if (key >= 112 && key <= 126)
                return (KeyCode)((int)KeyCode.F1 + (key - 112));

            return KeyCode.None;
        }

        private static bool IsWindowsRuntime()
        {
            RuntimePlatform platform = Application.platform;
            return platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.WindowsEditor;
        }

        internal static bool ShouldBlockAsyncKey(ushort key, KeyLabel label)
        {
            return ShouldBlockAsyncKey(key, label, InPlayerControl(), false);
        }

        internal static bool ShouldBlockAsyncKeyFromHook(ushort key, KeyLabel label)
        {
            return ShouldBlockAsyncKey(key, label, InPlayerControlCached(), true);
        }

        private static bool ShouldBlockAsyncKey(ushort key, KeyLabel label, bool inPlayerControl, bool useDirectAllowedKeys)
        {
            if (!IsActive() || !inPlayerControl) return false;
            if (IsMouseLabel(label)) return false;

            KeyCode unityKey = useDirectAllowedKeys
                ? HookKeyToPhysicalUnityKey(key, label)
                : AsyncLabelToPhysicalUnityKey(label);
            if (IsMouseKey(unityKey)) return false;
            if (unityKey != KeyCode.None && IsAllowedKey(unityKey, useDirectAllowedKeys)) return false;

            KeyCode mappedKey = AsyncKeyMapper.AsyncKeyToUnityKey(label);
            return mappedKey == KeyCode.None || !IsAllowedKey(mappedKey, useDirectAllowedKeys);
        }

        private static bool IsAllowedKey(KeyCode key, bool useDirectAllowedKeys)
        {
            return useDirectAllowedKeys ? IsAllowedKeyDirect(key) : IsAllowedKey(key);
        }
    }
}
