using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static string lastFontName;
        private static bool fontDropdownOpen;
        private static Dictionary<string, string> bundledFontFiles;
        private static List<string> bundledFontNames;

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

                if (!File.Exists(dest) || File.GetLastWriteTimeUtc(srcPath) > File.GetLastWriteTimeUtc(dest))
                {
                    File.Copy(srcPath, dest, true);
                }

                return dest;
            }
            catch
            {
                return null;
            }
        }

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
                    string display = names.Length > 0
                        ? names[0]
                        : Path.GetFileNameWithoutExtension(installedPath);

                    if (!bundledFontFiles.ContainsKey(display))
                    {
                        bundledFontFiles[display] = installedPath;
                        bundledFontNames.Add(display);
                        RegisterFontWithOS(installedPath);
                    }
                }

                mod?.Logger?.Log("[Font] Bundled fonts loaded: " + string.Join(", ", bundledFontNames));
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

            // Reset cache if font changed
            if (requested != lastFontName)
            {
                preferredHudFont = null;
                lastFontName = requested;
            }

            if (preferredHudFont != null)
                return preferredHudFont;

            // --- TRY CUSTOM FONT ---
            if (!string.IsNullOrEmpty(requested) &&
                bundledFontFiles != null &&
                bundledFontFiles.ContainsKey(requested))
            {
                try
                {
                    string path = bundledFontFiles[requested];

                    RegisterFontWithOS(path);

                    string[] names = ExtractFontNames(path);
                    string fileName = Path.GetFileNameWithoutExtension(path);

                    // Combine ALL possible names
                    List<string> tryNames = new List<string>();
                    tryNames.Add(requested);
                    tryNames.Add(fileName);
                    tryNames.AddRange(names);

                    // Remove duplicates / empty
                    tryNames = tryNames
                        .Where(n => !string.IsNullOrEmpty(n))
                        .Distinct()
                        .ToList();

                    mod?.Logger?.Log("[Font] Trying names: " + string.Join(", ", tryNames));

                    Font f = Font.CreateDynamicFontFromOSFont(tryNames.ToArray(), 28);

                    if (f != null)
                    {
                        try
                        {
                            f.RequestCharactersInTexture(
                                "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,:;!?@#$%&*()-_=+/\\|<>[]{}'\" ↑↓←→",
                                28);
                        }
                        catch { }

                        preferredHudFont = f;

                        mod?.Logger?.Log("[Font] SUCCESS -> " + f.name);

                        return preferredHudFont;
                    }
                }
                catch (Exception ex)
                {
                    mod?.Logger?.Log("[Font] Custom load failed: " + ex.Message);
                }
            }

            // --- FALLBACK ---
            try
            {
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
            catch (Exception ex)
            {
                mod?.Logger?.Log("[Font] Fallback failed: " + ex.Message);
            }

            if (preferredHudFont == null)
                preferredHudFont = GUI.skin.label.font;

            return preferredHudFont;
        }

        // =========================
        // OS FONT REGISTRATION
        // =========================

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern IntPtr CFURLCreateFromFileSystemRepresentation(IntPtr alloc, byte[] buffer, long bufLen, bool isDirectory);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRelease(IntPtr cf);

        [DllImport("/System/Library/Frameworks/CoreText.framework/CoreText")]
        private static extern bool CTFontManagerRegisterFontsForURL(IntPtr fontUrl, int scope, IntPtr error);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "AddFontResourceExW")]
        private static extern int AddFontResourceExW(string lpFileName, uint fl, IntPtr pdv);

        private static bool RegisterFontWithOS(string path)
        {
            try
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer ||
                    Application.platform == RuntimePlatform.WindowsEditor)
                {
                    return AddFontResourceExW(path, 0x10, IntPtr.Zero) > 0;
                }

                if (Application.platform == RuntimePlatform.OSXPlayer ||
                    Application.platform == RuntimePlatform.OSXEditor)
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(path);
                    IntPtr url = CFURLCreateFromFileSystemRepresentation(IntPtr.Zero, bytes, bytes.Length, false);

                    if (url == IntPtr.Zero) return false;

                    try
                    {
                        return CTFontManagerRegisterFontsForURL(url, 1, IntPtr.Zero);
                    }
                    finally { CFRelease(url); }
                }
            }
            catch { }

            return false;
        }

        // =========================
        // FONT NAME EXTRACTION
        // =========================

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
                    if (b[rec] == 'n' && b[rec + 1] == 'a' && b[rec + 2] == 'm' && b[rec + 3] == 'e')
                    {
                        nameOffset = (b[rec + 8] << 24) | (b[rec + 9] << 16) | (b[rec + 10] << 8) | b[rec + 11];
                        break;
                    }
                }

                if (nameOffset < 0) return result.ToArray();

                int count = (b[nameOffset + 2] << 8) | b[nameOffset + 3];
                int stringOffset = (b[nameOffset + 4] << 8) | b[nameOffset + 5];

                for (int i = 0; i < count; i++)
                {
                    int rec = nameOffset + 6 + i * 12;
                    int nameID = (b[rec + 6] << 8) | b[rec + 7];

                    if (nameID != 1 && nameID != 4 && nameID != 6) continue;

                    int length = (b[rec + 8] << 8) | b[rec + 9];
                    int offset = (b[rec + 10] << 8) | b[rec + 11];

                    int strStart = nameOffset + stringOffset + offset;

                    if (strStart < 0 || strStart + length > b.Length) continue;

                    string s = System.Text.Encoding.BigEndianUnicode.GetString(b, strStart, length);

                    if (!string.IsNullOrEmpty(s))
                        result.Add(s);
                }
            }
            catch { }

            result.Add(Path.GetFileNameWithoutExtension(path));

            return result.Distinct().ToArray();
        }
    }
}