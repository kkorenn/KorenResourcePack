using UnityEngine;

namespace KorenResourcePack
{
    internal static class KeyCodeCompat
    {
        private const int LegacyAsyncKeyOffset = 0x1000;
        private const int LegacyAsyncKeyMax = LegacyAsyncKeyOffset + 0xFF;

        internal static KeyCode NormalizeKey(KeyCode key)
        {
            key = NormalizeLegacyAsyncKey(key);
            return key == KeyCode.AltGr ? KeyCode.RightAlt : key;
        }

        internal static int NormalizeKeyCode(int key)
        {
            return (int)NormalizeKey((KeyCode)key);
        }

        internal static KeyCode NormalizeLegacyAsyncKey(KeyCode key)
        {
            int raw = (int)key;
            if (raw < LegacyAsyncKeyOffset || raw > LegacyAsyncKeyMax)
                return key;

            KeyCode mapped = WindowsVirtualKeyToUnityKey((ushort)(raw - LegacyAsyncKeyOffset));
            return mapped == KeyCode.None ? key : mapped;
        }

        private static KeyCode WindowsVirtualKeyToUnityKey(ushort key)
        {
            switch (key)
            {
                case 0x15:
                case 0xA5:
                    return KeyCode.RightAlt;
                case 0x19:
                case 0xA3:
                    return KeyCode.RightControl;
                case 0x5D:
                    return KeyCode.Menu;
                case 0x08:
                    return KeyCode.Backspace;
                case 0x09:
                    return KeyCode.Tab;
                case 0x0D:
                    return KeyCode.Return;
                case 0x10:
                case 0xA0:
                    return KeyCode.LeftShift;
                case 0x11:
                case 0xA2:
                    return KeyCode.LeftControl;
                case 0x12:
                case 0xA4:
                    return KeyCode.LeftAlt;
                case 0x13:
                    return KeyCode.Pause;
                case 0x14:
                    return KeyCode.CapsLock;
                case 0x1B:
                    return KeyCode.Escape;
                case 0x20:
                    return KeyCode.Space;
                case 0x21:
                    return KeyCode.PageUp;
                case 0x22:
                    return KeyCode.PageDown;
                case 0x23:
                    return KeyCode.End;
                case 0x24:
                    return KeyCode.Home;
                case 0x25:
                    return KeyCode.LeftArrow;
                case 0x26:
                    return KeyCode.UpArrow;
                case 0x27:
                    return KeyCode.RightArrow;
                case 0x28:
                    return KeyCode.DownArrow;
                case 0x2C:
                    return KeyCode.Print;
                case 0x2D:
                    return KeyCode.Insert;
                case 0x2E:
                    return KeyCode.Delete;
                case 0x5B:
                    return KeyCode.LeftWindows;
                case 0x5C:
                    return KeyCode.RightWindows;
                case 0x6A:
                    return KeyCode.KeypadMultiply;
                case 0x6B:
                    return KeyCode.KeypadPlus;
                case 0x6D:
                    return KeyCode.KeypadMinus;
                case 0x6E:
                    return KeyCode.KeypadPeriod;
                case 0x6F:
                    return KeyCode.KeypadDivide;
                case 0x90:
                    return KeyCode.Numlock;
                case 0x91:
                    return KeyCode.ScrollLock;
                case 0xA1:
                    return KeyCode.RightShift;
                case 0xBA:
                    return KeyCode.Semicolon;
                case 0xBB:
                    return KeyCode.Equals;
                case 0xBC:
                    return KeyCode.Comma;
                case 0xBD:
                    return KeyCode.Minus;
                case 0xBE:
                    return KeyCode.Period;
                case 0xBF:
                    return KeyCode.Slash;
                case 0xC0:
                    return KeyCode.BackQuote;
                case 0xDB:
                    return KeyCode.LeftBracket;
                case 0xDC:
                    return KeyCode.Backslash;
                case 0xDD:
                    return KeyCode.RightBracket;
                case 0xDE:
                    return KeyCode.Quote;
            }

            if (key >= 0x30 && key <= 0x39)
                return (KeyCode)((int)KeyCode.Alpha0 + (key - 0x30));
            if (key >= 0x41 && key <= 0x5A)
                return (KeyCode)((int)KeyCode.A + (key - 0x41));
            if (key >= 0x60 && key <= 0x69)
                return (KeyCode)((int)KeyCode.Keypad0 + (key - 0x60));
            if (key >= 0x70 && key <= 0x7E)
                return (KeyCode)((int)KeyCode.F1 + (key - 0x70));

            return KeyCode.None;
        }
    }
}
