using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace KorenResourcePack
{
    // Injects a system-wide key chord (modifiers + key) so external apps such as
    // Discord can pick it up via their own global shortcut. One SendChord call is a
    // single tap (press + release of the whole chord), which toggles Discord's deafen.
    internal static class NativeKeySender
    {
        private static bool unsupportedLogged;

        internal static void SendChord(bool ctrl, bool shift, bool alt, bool meta, KeyCode key)
        {
            key = KeyCodeCompat.NormalizeKey(key);
            if (key == KeyCode.None) return;

            try
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                        SendWindows(ctrl, shift, alt, meta, key);
                        break;
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.OSXEditor:
                        SendMac(ctrl, shift, alt, meta, key);
                        break;
                    case RuntimePlatform.LinuxPlayer:
                    case RuntimePlatform.LinuxEditor:
                        SendLinux(ctrl, shift, alt, meta, key);
                        break;
                    default:
                        LogUnsupported("platform " + Application.platform);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogUnsupported(ex.Message);
            }
        }

        private static void LogUnsupported(string detail)
        {
            if (unsupportedLogged) return;
            unsupportedLogged = true;
            Main.mod?.Logger?.Log("[AutoDeafen] key send unavailable (" + detail + ").");
        }

        // ---------------- Windows ----------------

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_SHIFT = 0x10;
        private const byte VK_MENU = 0x12;   // Alt
        private const byte VK_LWIN = 0x5B;

        private static void SendWindows(bool ctrl, bool shift, bool alt, bool meta, KeyCode key)
        {
            byte vk = WinVk(key);
            if (vk == 0) { LogUnsupported("no VK for " + key); return; }

            if (ctrl) keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            if (shift) keybd_event(VK_SHIFT, 0, 0, UIntPtr.Zero);
            if (alt) keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
            if (meta) keybd_event(VK_LWIN, 0, 0, UIntPtr.Zero);

            keybd_event(vk, 0, 0, UIntPtr.Zero);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            if (meta) keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (alt) keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (shift) keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (ctrl) keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private static byte WinVk(KeyCode k)
        {
            if (k >= KeyCode.A && k <= KeyCode.Z) return (byte)(0x41 + (k - KeyCode.A));
            if (k >= KeyCode.Alpha0 && k <= KeyCode.Alpha9) return (byte)(0x30 + (k - KeyCode.Alpha0));
            if (k >= KeyCode.Keypad0 && k <= KeyCode.Keypad9) return (byte)(0x60 + (k - KeyCode.Keypad0));
            if (k >= KeyCode.F1 && k <= KeyCode.F15) return (byte)(0x70 + (k - KeyCode.F1));
            switch (k)
            {
                case KeyCode.Space: return 0x20;
                case KeyCode.Return: return 0x0D;
                case KeyCode.Tab: return 0x09;
                case KeyCode.Backspace: return 0x08;
                case KeyCode.Escape: return 0x1B;
                case KeyCode.Insert: return 0x2D;
                case KeyCode.Delete: return 0x2E;
                case KeyCode.Home: return 0x24;
                case KeyCode.End: return 0x23;
                case KeyCode.PageUp: return 0x21;
                case KeyCode.PageDown: return 0x22;
                case KeyCode.UpArrow: return 0x26;
                case KeyCode.DownArrow: return 0x28;
                case KeyCode.LeftArrow: return 0x25;
                case KeyCode.RightArrow: return 0x27;
                case KeyCode.BackQuote: return 0xC0;
                case KeyCode.Minus: return 0xBD;
                case KeyCode.Equals: return 0xBB;
                case KeyCode.LeftBracket: return 0xDB;
                case KeyCode.RightBracket: return 0xDD;
                case KeyCode.Semicolon: return 0xBA;
                case KeyCode.Quote: return 0xDE;
                case KeyCode.Comma: return 0xBC;
                case KeyCode.Period: return 0xBE;
                case KeyCode.Slash: return 0xBF;
                case KeyCode.Backslash: return 0xDC;
                case KeyCode.KeypadPlus: return 0x6B;
                case KeyCode.KeypadMinus: return 0x6D;
                case KeyCode.KeypadMultiply: return 0x6A;
                case KeyCode.KeypadDivide: return 0x6F;
                case KeyCode.KeypadPeriod: return 0x6E;
            }
            return 0;
        }

        // ---------------- macOS ----------------
        // Inject the chord at the HID event tap (where physical keys enter), because
        // Discord's global shortcut listens there — System Events / session-level posts
        // don't reach it. The mod's own SkyHook tap sits at HID too and would suppress
        // the key during play, so open an inject-bypass window first.

        private const string AppServices = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
        private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        [DllImport(AppServices)]
        private static extern IntPtr CGEventSourceCreate(int stateID);
        [DllImport(AppServices)]
        private static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort virtualKey, bool keyDown);
        [DllImport(AppServices)]
        private static extern void CGEventSetFlags(IntPtr evt, ulong flags);
        [DllImport(AppServices)]
        private static extern void CGEventPost(uint tap, IntPtr evt);
        [DllImport(CoreFoundation)]
        private static extern void CFRelease(IntPtr cf);

        private const ulong kCGEventFlagMaskShift = 0x00020000;
        private const ulong kCGEventFlagMaskControl = 0x00040000;
        private const ulong kCGEventFlagMaskAlternate = 0x00080000;
        private const ulong kCGEventFlagMaskCommand = 0x00100000;
        private const uint kCGHIDEventTap = 0;
        private const int kCGEventSourceStateHIDSystemState = 1;

        private const ushort kVK_Command = 55;
        private const ushort kVK_Shift = 56;
        private const ushort kVK_Option = 58;
        private const ushort kVK_Control = 59;

        private static IntPtr macSource = IntPtr.Zero;
        private static bool macSourceTried;

        private static void SendMac(bool ctrl, bool shift, bool alt, bool meta, KeyCode key)
        {
            ushort code;
            if (!MacKeyCode(key, out code)) { LogUnsupported("no mac code for " + key); return; }

            if (!macSourceTried)
            {
                macSourceTried = true;
                macSource = CGEventSourceCreate(kCGEventSourceStateHIDSystemState);
            }

            // Tell the mod's own keyboard hook to let this keystroke pass through.
            ChatterBlocker.BeginInjectBypass(150);

            ulong flags = 0;
            if (ctrl) { flags |= kCGEventFlagMaskControl; PostMacKey(kVK_Control, true, flags); }
            if (shift) { flags |= kCGEventFlagMaskShift; PostMacKey(kVK_Shift, true, flags); }
            if (alt) { flags |= kCGEventFlagMaskAlternate; PostMacKey(kVK_Option, true, flags); }
            if (meta) { flags |= kCGEventFlagMaskCommand; PostMacKey(kVK_Command, true, flags); }

            PostMacKey(code, true, flags);
            PostMacKey(code, false, flags);

            if (meta) { PostMacKey(kVK_Command, false, flags); flags &= ~kCGEventFlagMaskCommand; }
            if (alt) { PostMacKey(kVK_Option, false, flags); flags &= ~kCGEventFlagMaskAlternate; }
            if (shift) { PostMacKey(kVK_Shift, false, flags); flags &= ~kCGEventFlagMaskShift; }
            if (ctrl) { PostMacKey(kVK_Control, false, flags); flags &= ~kCGEventFlagMaskControl; }
        }

        private static void PostMacKey(ushort code, bool down, ulong flags)
        {
            IntPtr evt = CGEventCreateKeyboardEvent(macSource, code, down);
            if (evt == IntPtr.Zero) return;
            CGEventSetFlags(evt, flags);
            CGEventPost(kCGHIDEventTap, evt);
            CFRelease(evt);
        }

        private static bool MacKeyCode(KeyCode k, out ushort code)
        {
            switch (k)
            {
                case KeyCode.A: code = 0; return true;
                case KeyCode.B: code = 11; return true;
                case KeyCode.C: code = 8; return true;
                case KeyCode.D: code = 2; return true;
                case KeyCode.E: code = 14; return true;
                case KeyCode.F: code = 3; return true;
                case KeyCode.G: code = 5; return true;
                case KeyCode.H: code = 4; return true;
                case KeyCode.I: code = 34; return true;
                case KeyCode.J: code = 38; return true;
                case KeyCode.K: code = 40; return true;
                case KeyCode.L: code = 37; return true;
                case KeyCode.M: code = 46; return true;
                case KeyCode.N: code = 45; return true;
                case KeyCode.O: code = 31; return true;
                case KeyCode.P: code = 35; return true;
                case KeyCode.Q: code = 12; return true;
                case KeyCode.R: code = 15; return true;
                case KeyCode.S: code = 1; return true;
                case KeyCode.T: code = 17; return true;
                case KeyCode.U: code = 32; return true;
                case KeyCode.V: code = 9; return true;
                case KeyCode.W: code = 13; return true;
                case KeyCode.X: code = 7; return true;
                case KeyCode.Y: code = 16; return true;
                case KeyCode.Z: code = 6; return true;
                case KeyCode.Alpha1: code = 18; return true;
                case KeyCode.Alpha2: code = 19; return true;
                case KeyCode.Alpha3: code = 20; return true;
                case KeyCode.Alpha4: code = 21; return true;
                case KeyCode.Alpha5: code = 23; return true;
                case KeyCode.Alpha6: code = 22; return true;
                case KeyCode.Alpha7: code = 26; return true;
                case KeyCode.Alpha8: code = 28; return true;
                case KeyCode.Alpha9: code = 25; return true;
                case KeyCode.Alpha0: code = 29; return true;
                case KeyCode.Space: code = 49; return true;
                case KeyCode.Return: code = 36; return true;
                case KeyCode.Tab: code = 48; return true;
                case KeyCode.Backspace: code = 51; return true;
                case KeyCode.Escape: code = 53; return true;
                case KeyCode.Minus: code = 27; return true;
                case KeyCode.Equals: code = 24; return true;
                case KeyCode.LeftBracket: code = 33; return true;
                case KeyCode.RightBracket: code = 30; return true;
                case KeyCode.Semicolon: code = 41; return true;
                case KeyCode.Quote: code = 39; return true;
                case KeyCode.Comma: code = 43; return true;
                case KeyCode.Period: code = 47; return true;
                case KeyCode.Slash: code = 44; return true;
                case KeyCode.Backslash: code = 42; return true;
                case KeyCode.BackQuote: code = 50; return true;
                case KeyCode.LeftArrow: code = 123; return true;
                case KeyCode.RightArrow: code = 124; return true;
                case KeyCode.DownArrow: code = 125; return true;
                case KeyCode.UpArrow: code = 126; return true;
                case KeyCode.F1: code = 122; return true;
                case KeyCode.F2: code = 120; return true;
                case KeyCode.F3: code = 99; return true;
                case KeyCode.F4: code = 118; return true;
                case KeyCode.F5: code = 96; return true;
                case KeyCode.F6: code = 97; return true;
                case KeyCode.F7: code = 98; return true;
                case KeyCode.F8: code = 100; return true;
                case KeyCode.F9: code = 101; return true;
                case KeyCode.F10: code = 109; return true;
                case KeyCode.F11: code = 103; return true;
                case KeyCode.F12: code = 111; return true;
            }
            code = 0;
            return false;
        }

        // ---------------- Linux (X11 / XTest) ----------------

        [DllImport("libX11.so.6")]
        private static extern IntPtr XOpenDisplay(string display);
        [DllImport("libX11.so.6")]
        private static extern byte XKeysymToKeycode(IntPtr display, ulong keysym);
        [DllImport("libX11.so.6")]
        private static extern int XFlush(IntPtr display);
        [DllImport("libXtst.so.6")]
        private static extern int XTestFakeKeyEvent(IntPtr display, uint keycode, bool isPress, ulong delay);

        private const ulong XK_Shift_L = 0xffe1;
        private const ulong XK_Control_L = 0xffe3;
        private const ulong XK_Alt_L = 0xffe9;
        private const ulong XK_Super_L = 0xffeb;

        private static IntPtr linuxDisplay = IntPtr.Zero;
        private static bool linuxDisplayTried;

        private static void SendLinux(bool ctrl, bool shift, bool alt, bool meta, KeyCode key)
        {
            if (!linuxDisplayTried)
            {
                linuxDisplayTried = true;
                linuxDisplay = XOpenDisplay(null);
            }
            if (linuxDisplay == IntPtr.Zero) { LogUnsupported("no X11 display (Wayland?)"); return; }

            ulong sym = LinuxKeysym(key);
            if (sym == 0) { LogUnsupported("no keysym for " + key); return; }
            byte kc = XKeysymToKeycode(linuxDisplay, sym);
            if (kc == 0) { LogUnsupported("unmapped keysym for " + key); return; }

            byte ctrlKc = ctrl ? XKeysymToKeycode(linuxDisplay, XK_Control_L) : (byte)0;
            byte shiftKc = shift ? XKeysymToKeycode(linuxDisplay, XK_Shift_L) : (byte)0;
            byte altKc = alt ? XKeysymToKeycode(linuxDisplay, XK_Alt_L) : (byte)0;
            byte metaKc = meta ? XKeysymToKeycode(linuxDisplay, XK_Super_L) : (byte)0;

            if (ctrlKc != 0) XTestFakeKeyEvent(linuxDisplay, ctrlKc, true, 0);
            if (shiftKc != 0) XTestFakeKeyEvent(linuxDisplay, shiftKc, true, 0);
            if (altKc != 0) XTestFakeKeyEvent(linuxDisplay, altKc, true, 0);
            if (metaKc != 0) XTestFakeKeyEvent(linuxDisplay, metaKc, true, 0);

            XTestFakeKeyEvent(linuxDisplay, kc, true, 0);
            XTestFakeKeyEvent(linuxDisplay, kc, false, 0);

            if (metaKc != 0) XTestFakeKeyEvent(linuxDisplay, metaKc, false, 0);
            if (altKc != 0) XTestFakeKeyEvent(linuxDisplay, altKc, false, 0);
            if (shiftKc != 0) XTestFakeKeyEvent(linuxDisplay, shiftKc, false, 0);
            if (ctrlKc != 0) XTestFakeKeyEvent(linuxDisplay, ctrlKc, false, 0);

            XFlush(linuxDisplay);
        }

        private static ulong LinuxKeysym(KeyCode k)
        {
            if (k >= KeyCode.A && k <= KeyCode.Z) return (ulong)('a' + (k - KeyCode.A));
            if (k >= KeyCode.Alpha0 && k <= KeyCode.Alpha9) return (ulong)('0' + (k - KeyCode.Alpha0));
            if (k >= KeyCode.F1 && k <= KeyCode.F12) return 0xffbe + (ulong)(k - KeyCode.F1);
            switch (k)
            {
                case KeyCode.Space: return 0x0020;
                case KeyCode.Return: return 0xff0d;
                case KeyCode.Tab: return 0xff09;
                case KeyCode.Backspace: return 0xff08;
                case KeyCode.Escape: return 0xff1b;
                case KeyCode.Insert: return 0xff63;
                case KeyCode.Delete: return 0xffff;
                case KeyCode.Home: return 0xff50;
                case KeyCode.End: return 0xff57;
                case KeyCode.PageUp: return 0xff55;
                case KeyCode.PageDown: return 0xff56;
                case KeyCode.UpArrow: return 0xff52;
                case KeyCode.DownArrow: return 0xff54;
                case KeyCode.LeftArrow: return 0xff51;
                case KeyCode.RightArrow: return 0xff53;
                case KeyCode.Minus: return 0x002d;
                case KeyCode.Equals: return 0x003d;
                case KeyCode.LeftBracket: return 0x005b;
                case KeyCode.RightBracket: return 0x005d;
                case KeyCode.Semicolon: return 0x003b;
                case KeyCode.Quote: return 0x0027;
                case KeyCode.Comma: return 0x002c;
                case KeyCode.Period: return 0x002e;
                case KeyCode.Slash: return 0x002f;
                case KeyCode.Backslash: return 0x005c;
                case KeyCode.BackQuote: return 0x0060;
            }
            return 0;
        }
    }
}
