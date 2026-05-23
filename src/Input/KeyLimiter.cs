using HarmonyLib;
using MonsterLove.StateMachine;
using SkyHook;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace KorenResourcePack
{
    // KeyboardChatterBlocker's key limiter filters only while the controller is in
    // PlayerControl. Normal key filtering happens in ChatterBlocker's CountValidKeysPressed
    // replacement; async key filtering happens in the SkyHook callback.
    internal static class KeyLimiter
    {
        private static readonly HashSet<int> cachedAllowedKeys = new HashSet<int>();
        private static int[] cachedAllowedSource;
        private static int cachedAllowedLength = -1;
        private static int cachedPlayerControlFrame = -1;
        private static bool cachedPlayerControl;
        private static int cachedPlayerControlForHooks;

#if !LEGACY
        // ADOFAI 3.1+ ships with a built-in key limiter (Persistence.keyLimiterKeys) that
        // runs inside RDInputType_Keyboard/RDInputType_AsyncKeyboard before
        // RDInput.GetMainPressKeys returns. Any key not in the player's saved limiter set is
        // dropped upstream, so the mod never sees presses for keys like '=' or '-' even if
        // they're in our allowlist. When the mod's KeyLimiter is enabled we force the game's
        // gate off so the mod's allowlist becomes the sole filter.
        [HarmonyPatch(typeof(RDInput), "get_useKeyLimiter")]
        private static class RDInputUseKeyLimiterPatch
        {
            private static void Postfix(ref bool __result)
            {
                if (__result && IsEnabled())
                    __result = false;
            }
        }
#endif

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
                    cachedAllowedKeys.Add(allowed[i]);

                cachedAllowedSource = allowed;
                cachedAllowedLength = allowed.Length;
            }

            return cachedAllowedKeys.Contains((int)key);
        }

        private static bool IsAllowedKeyDirect(KeyCode key)
        {
            int[] allowed = Main.settings != null ? Main.settings.KeyLimiterAllowed : null;
            if (allowed == null) return false;

            int target = (int)key;
            for (int i = 0; i < allowed.Length; i++)
            {
                if (allowed[i] == target)
                    return true;
            }

            return false;
        }

        internal static bool ShouldBlockKey(KeyCode key)
        {
            return IsActive() && InPlayerControl() && !IsAllowedKey(key);
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
                case "LAlt":            return KeyCode.LeftAlt;
                case "Space":           return KeyCode.Space;
                case "RAlt":            return KeyCode.RightAlt;
                case "RControl":        return KeyCode.RightControl;
                case "RCtrl":           return KeyCode.RightControl;
                case "RightControl":    return KeyCode.RightControl;
                case "RightCtrl":       return KeyCode.RightControl;
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
                case "MouseLeft":       return KeyCode.Mouse0;
                case "MouseRight":      return KeyCode.Mouse1;
                case "MouseMiddle":     return KeyCode.Mouse2;
                case "MouseX1":         return KeyCode.Mouse3;
                case "MouseX2":         return KeyCode.Mouse4;
            }

            return AsyncKeyMapper.AsyncKeyToUnityKey(label);
        }

        internal static bool ShouldBlockAsyncKey(ushort key, KeyLabel label)
        {
            return ShouldBlockAsyncKey(label, InPlayerControl(), false);
        }

        internal static bool ShouldBlockAsyncKeyFromHook(ushort key, KeyLabel label)
        {
            return ShouldBlockAsyncKey(label, InPlayerControlCached(), true);
        }

        private static bool ShouldBlockAsyncKey(KeyLabel label, bool inPlayerControl, bool useDirectAllowedKeys)
        {
            if (!IsActive() || !inPlayerControl) return false;
            KeyCode unityKey = AsyncLabelToPhysicalUnityKey(label);
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
