using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static string lastFontName;
        private static bool fontDropdownOpen;
        private static Dictionary<string, string> bundledFontFiles;
        private static List<string> bundledFontNames;

        private static void EnsureBundledFontsLoaded()
        {
            if (bundledFontFiles != null) return;
            bundledFontFiles = new Dictionary<string, string>();
            bundledFontNames = new List<string>();
            try
            {
                string fontsDir = Path.Combine(mod.Path, "Fonts");
                if (!Directory.Exists(fontsDir)) return;
                foreach (string path in Directory.GetFiles(fontsDir))
                {
                    string ext = Path.GetExtension(path).ToLowerInvariant();
                    if (ext != ".ttf" && ext != ".ttc") continue;
                    string installedPath = InstallFontPersistent(path) ?? path;
                    string[] names = ExtractFontNames(installedPath);
                    string display = names.Length > 0 ? names[0] : Path.GetFileNameWithoutExtension(installedPath);
                    if (!bundledFontFiles.ContainsKey(display))
                    {
                        bundledFontFiles[display] = installedPath;
                        bundledFontNames.Add(display);
                        RegisterFontWithOS(installedPath);
                    }
                }
                mod?.Logger?.Log("[Font] Bundled fonts loaded: " + string.Join(", ", bundledFontNames.ToArray()));
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[Font] Bundle scan failed: " + ex.Message);
            }
        }

        private static Font GetPreferredHudFont()
        {
            EnsureBundledFontsLoaded();

            string requested = settings != null ? (settings.fontName ?? "") : "";
            if (requested != lastFontName)
            {
                if (preferredHudFont != null && preferredHudFont.dynamic && !ReferenceEquals(preferredHudFont, GUI.skin.label.font))
                {
                    try { UnityEngine.Object.Destroy(preferredHudFont); } catch { }
                }
                preferredHudFont = null;
                lastFontName = requested;
            }

            if (preferredHudFont != null) return preferredHudFont;

            if (!string.IsNullOrEmpty(requested) && bundledFontFiles != null && bundledFontFiles.ContainsKey(requested))
            {
                try
                {
                    string path = bundledFontFiles[requested];
                    bool reg = RegisterFontWithOS(path);
                    string[] names = ExtractFontNames(path);
                    mod?.Logger?.Log("[Font] '" + requested + "' register=" + reg + " names=[" + string.Join(", ", names) + "]");

                    HashSet<string> osFonts = null;
                    try
                    {
                        string[] osList = Font.GetOSInstalledFontNames();
                        if (osList != null)
                        {
                            osFonts = new HashSet<string>(osList, StringComparer.OrdinalIgnoreCase);
                        }
                    }
                    catch (Exception ex) { mod?.Logger?.Log("[Font] GetOSInstalledFontNames failed: " + ex.Message); }

                    string matched = null;
                    if (osFonts != null)
                    {
                        foreach (string n in names)
                        {
                            if (!string.IsNullOrEmpty(n) && osFonts.Contains(n))
                            {
                                matched = n;
                                break;
                            }
                        }
                        if (matched != null)
                        {
                            mod?.Logger?.Log("[Font] OS visible match: '" + matched + "'");
                        }
                        else
                        {
                            mod?.Logger?.Log("[Font] WARNING: none of [" + string.Join(", ", names) + "] visible to Unity OS font enum. Likely needs game restart for newly installed fonts to register. Trying anyway.");
                        }
                    }

                    List<string> tryOrder = new List<string>();
                    if (matched != null) tryOrder.Add(matched);
                    foreach (string n in names)
                    {
                        if (!string.IsNullOrEmpty(n) && !tryOrder.Contains(n)) tryOrder.Add(n);
                    }

                    foreach (string n in tryOrder)
                    {
                        Font f = Font.CreateDynamicFontFromOSFont(n, 28);
                        string fontResolvedName = (f != null && f.fontNames != null && f.fontNames.Length > 0) ? string.Join("|", f.fontNames) : "(null)";
                        bool dyn = f != null ? f.dynamic : false;
                        string mat = (f != null && f.material != null) ? f.material.name : "(no mat)";
                        bool osVisible = osFonts != null && osFonts.Contains(n);
                        mod?.Logger?.Log("[Font] try '" + n + "' -> " + fontResolvedName + " dyn=" + dyn + " mat=" + mat + " osVisible=" + osVisible);
                        if (f != null && f.fontNames != null && f.fontNames.Length > 0)
                        {
                            try
                            {
                                f.RequestCharactersInTexture(
                                    "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,:;!?@#$%&*()-_=+/\\|<>[]{}'\" \tABCDEFGHIJKLMNOPQRSTUVWXYZ",
                                    28);
                            }
                            catch { }
                            preferredHudFont = f;
                            return preferredHudFont;
                        }
                    }
                }
                catch (Exception ex) { mod?.Logger?.Log("[Font] Custom load failed: " + ex.Message); }
            }

            try
            {
                Text gameHudText = scrController.instance != null ? (scrController.instance.txtPercent ?? scrController.instance.txtLevelName) : null;
                if (gameHudText != null && gameHudText.font != null)
                {
                    preferredHudFont = gameHudText.font;
                    return preferredHudFont;
                }

                preferredHudFont = Font.CreateDynamicFontFromOSFont(
                    new[]
                    {
                        "DIN Alternate Bold",
                        "DIN Condensed",
                        "Avenir Next Demi Bold",
                        "Helvetica Neue",
                        "Trebuchet MS",
                        "Arial"
                    },
                    28);
            }
            catch (Exception ex) { mod?.Logger?.Log("[Warning] Font fallback used: " + ex.Message); }

            if (preferredHudFont == null) preferredHudFont = GUI.skin.label.font;
            return preferredHudFont;
        }

        private static string InstallFontPersistent(string srcPath)
        {
            try
            {
                RuntimePlatform p = Application.platform;
                string targetDir = null;
                if (p == RuntimePlatform.OSXPlayer || p == RuntimePlatform.OSXEditor)
                {
                    string home = Environment.GetEnvironmentVariable("HOME");
                    if (string.IsNullOrEmpty(home)) return null;
                    targetDir = Path.Combine(home, "Library/Fonts");
                }
                else if (p == RuntimePlatform.WindowsPlayer || p == RuntimePlatform.WindowsEditor)
                {
                    string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    if (string.IsNullOrEmpty(local)) return null;
                    targetDir = Path.Combine(local, "Microsoft\\Windows\\Fonts");
                }
                else
                {
                    return null;
                }
                Directory.CreateDirectory(targetDir);
                string dest = Path.Combine(targetDir, Path.GetFileName(srcPath));
                if (string.Equals(Path.GetFullPath(srcPath), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase)) return dest;
                if (!File.Exists(dest) || File.GetLastWriteTimeUtc(srcPath) > File.GetLastWriteTimeUtc(dest))
                {
                    try
                    {
                        File.Copy(srcPath, dest, true);
                        mod?.Logger?.Log("[Font] Installed persistently: " + dest);
                    }
                    catch (IOException ioex)
                    {
                        if (File.Exists(dest))
                        {
                            mod?.Logger?.Log("[Font] Reusing existing install (file in use): " + dest);
                            return dest;
                        }
                        mod?.Logger?.Log("[Font] Persistent install failed: " + ioex.Message);
                        return null;
                    }
                    catch (UnauthorizedAccessException uaex)
                    {
                        if (File.Exists(dest))
                        {
                            mod?.Logger?.Log("[Font] Reusing existing install (access denied on copy): " + dest);
                            return dest;
                        }
                        mod?.Logger?.Log("[Font] Persistent install failed: " + uaex.Message);
                        return null;
                    }
                }
                return dest;
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[Font] Persistent install failed: " + ex.Message);
                return null;
            }
        }

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern IntPtr CFURLCreateFromFileSystemRepresentation(IntPtr alloc, byte[] buffer, long bufLen, bool isDirectory);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRelease(IntPtr cf);

        [DllImport("/System/Library/Frameworks/CoreText.framework/CoreText")]
        private static extern bool CTFontManagerRegisterFontsForURL(IntPtr fontUrl, int scope, IntPtr error);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "AddFontResourceExW")]
        private static extern int AddFontResourceExW(string lpFileName, uint fl, IntPtr pdv);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        private const int HWND_BROADCAST = 0xffff;
        private const uint WM_FONTCHANGE = 0x001D;

        private static bool RegisterFontWithOS(string path)
        {
            try
            {
                RuntimePlatform p = Application.platform;
                if (p == RuntimePlatform.OSXPlayer || p == RuntimePlatform.OSXEditor)
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(path);
                    IntPtr url = CFURLCreateFromFileSystemRepresentation(IntPtr.Zero, bytes, bytes.Length, false);
                    if (url == IntPtr.Zero) return false;
                    try
                    {
                        if (CTFontManagerRegisterFontsForURL(url, 1, IntPtr.Zero)) return true;
                        return CTFontManagerRegisterFontsForURL(url, 3, IntPtr.Zero);
                    }
                    finally { CFRelease(url); }
                }
                if (p == RuntimePlatform.WindowsPlayer || p == RuntimePlatform.WindowsEditor)
                {
                    bool ok = AddFontResourceExW(path, 0x10, IntPtr.Zero) > 0;
                    RegisterFontWindowsRegistry(path);
                    BroadcastFontChange();
                    return ok;
                }
            }
            catch (Exception ex) { mod?.Logger?.Log("[Font] OS register error: " + ex.Message); }
            return false;
        }

        private static void RegisterFontWindowsRegistry(string installedPath)
        {
            try
            {
                string[] names = ExtractFontNames(installedPath);
                if (names == null || names.Length == 0) return;
                string family = names[0];
                if (string.IsNullOrEmpty(family)) return;

                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows NT\CurrentVersion\Fonts", true))
                {
                    if (key == null) return;
                    string regName = family + " (TrueType)";
                    object existing = key.GetValue(regName);
                    if (existing == null || !string.Equals(existing.ToString(), installedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        key.SetValue(regName, installedPath);
                        mod?.Logger?.Log("[Font] HKCU Fonts registered: " + regName + " -> " + installedPath);
                    }
                }
            }
            catch (Exception ex) { mod?.Logger?.Log("[Font] Win registry register failed: " + ex.Message); }
        }

        private static void BroadcastFontChange()
        {
            try
            {
                IntPtr res;
                SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero, 0x0002, 1000, out res);
            }
            catch { }
        }

        private static string[] ExtractFontNames(string path)
        {
            List<string> result = new List<string>();
            try
            {
                byte[] b = File.ReadAllBytes(path);
                int numTables = (b[4] << 8) | b[5];
                int nameOffset = -1;
                for (int i = 0; i < numTables; i++)
                {
                    int rec = 12 + i * 16;
                    if (b[rec] == (byte)'n' && b[rec + 1] == (byte)'a' && b[rec + 2] == (byte)'m' && b[rec + 3] == (byte)'e')
                    {
                        nameOffset = (b[rec + 8] << 24) | (b[rec + 9] << 16) | (b[rec + 10] << 8) | b[rec + 11];
                        break;
                    }
                }
                if (nameOffset < 0) throw new Exception("name table missing");

                int count = (b[nameOffset + 2] << 8) | b[nameOffset + 3];
                int stringOffset = (b[nameOffset + 4] << 8) | b[nameOffset + 5];
                Dictionary<int, string> picks = new Dictionary<int, string>();
                for (int i = 0; i < count; i++)
                {
                    int rec = nameOffset + 6 + i * 12;
                    int platformID = (b[rec] << 8) | b[rec + 1];
                    int encodingID = (b[rec + 2] << 8) | b[rec + 3];
                    int nameID = (b[rec + 6] << 8) | b[rec + 7];
                    int length = (b[rec + 8] << 8) | b[rec + 9];
                    int offset = (b[rec + 10] << 8) | b[rec + 11];
                    if (nameID != 1 && nameID != 4 && nameID != 6) continue;
                    if (picks.ContainsKey(nameID)) continue;
                    int strStart = nameOffset + stringOffset + offset;
                    if (strStart < 0 || strStart + length > b.Length) continue;
                    string s = null;
                    if (platformID == 3 || platformID == 0)
                        s = System.Text.Encoding.BigEndianUnicode.GetString(b, strStart, length);
                    else if (platformID == 1 && encodingID == 0)
                        s = System.Text.Encoding.ASCII.GetString(b, strStart, length);
                    if (!string.IsNullOrEmpty(s)) picks[nameID] = s;
                }
                if (picks.ContainsKey(1)) result.Add(picks[1]);
                if (picks.ContainsKey(4)) result.Add(picks[4]);
                if (picks.ContainsKey(6)) result.Add(picks[6]);
            }
            catch (Exception ex) { mod?.Logger?.Log("[Font] TTF parse error: " + ex.Message); }
            result.Add(Path.GetFileNameWithoutExtension(path));
            return result.ToArray();
        }
    }
}
