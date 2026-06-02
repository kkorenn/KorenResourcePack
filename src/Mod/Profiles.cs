using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace KorenResourcePack
{
    internal static class Profiles
    {
        private const string DefaultName = "Default";
        private static readonly char[] InvalidNameChars = Path.GetInvalidFileNameChars();

        private static string ProfilesDir
        {
            get
            {
                string root = Main.mod != null ? Main.mod.Path : "";
                return Path.Combine(root, "Profiles");
            }
        }

        private static string PathFor(string name)
        {
            return Path.Combine(ProfilesDir, name + ".xml");
        }

        private static void EnsureDir()
        {
            try { Directory.CreateDirectory(ProfilesDir); } catch { }
        }

        internal static List<string> List()
        {
            List<string> names = new List<string>();
            try
            {
                EnsureDir();
                foreach (string f in Directory.GetFiles(ProfilesDir, "*.xml"))
                    names.Add(Path.GetFileNameWithoutExtension(f));
                names.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex) { Main.mod?.Logger?.Log("[Profile] list failed: " + ex.Message); }
            return names;
        }

        internal static bool Exists(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            try { return File.Exists(PathFor(name)); } catch { return false; }
        }

        internal static void Initialize()
        {
            Settings s = Main.settings;
            if (s == null) return;
            EnsureDir();
            List<string> names = List();
            if (names.Count == 0)
            {
                string seed = string.IsNullOrEmpty(s.ActiveProfileName) ? DefaultName : s.ActiveProfileName;
                seed = SanitizeName(seed);
                if (string.IsNullOrEmpty(seed)) seed = DefaultName;
                s.ActiveProfileName = seed;
                SaveProfile(seed);
                return;
            }
            if (string.IsNullOrEmpty(s.ActiveProfileName) || !ContainsIgnoreCase(names, s.ActiveProfileName))
                s.ActiveProfileName = names[0];
        }

        internal static void SyncActive()
        {
            Settings s = Main.settings;
            if (s == null || string.IsNullOrEmpty(s.ActiveProfileName)) return;
            SaveProfile(s.ActiveProfileName);
        }

        internal static void SaveProfile(string name)
        {
            if (string.IsNullOrEmpty(name) || Main.settings == null) return;
            try
            {
                EnsureDir();
                Main.settings.NormalizeSavedKeyCodes();
                XmlSerializer ser = new XmlSerializer(typeof(Settings));
                string tmp = PathFor(name) + ".tmp";
                using (FileStream fs = File.Create(tmp))
                    ser.Serialize(fs, Main.settings);
                string final = PathFor(name);
                if (File.Exists(final)) File.Delete(final);
                File.Move(tmp, final);
            }
            catch (Exception ex) { Main.mod?.Logger?.Log("[Profile] save '" + name + "' failed: " + ex.Message); }
        }

        private static Settings LoadProfile(string name)
        {
            string p = PathFor(name);
            if (!File.Exists(p)) return null;
            try
            {
                XmlSerializer ser = new XmlSerializer(typeof(Settings));
                using (FileStream fs = File.OpenRead(p))
                    return (Settings)ser.Deserialize(fs);
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[Profile] load '" + name + "' failed: " + ex.Message);
                return null;
            }
        }

        internal static void Switch(string name)
        {
            Settings cur = Main.settings;
            if (cur == null || string.IsNullOrEmpty(name)) return;
            if (string.Equals(name, cur.ActiveProfileName, StringComparison.OrdinalIgnoreCase)) return;
            if (!Exists(name)) return;

            SyncActive();
            Settings loaded = LoadProfile(name);
            if (loaded == null) return;

            CopyFields(loaded, cur);
            cur.ActiveProfileName = name;
            RefreshAfterSwitch();
        }

        internal static void CreateNew(string name)
        {
            Settings s = Main.settings;
            if (s == null) return;
            name = UniqueName(SanitizeName(name));
            if (string.IsNullOrEmpty(name)) return;
            SyncActive();
            s.ActiveProfileName = name;
            SaveProfile(name);
        }

        internal static void Duplicate(string sourceName, string newName)
        {
            Settings s = Main.settings;
            if (s == null || string.IsNullOrEmpty(sourceName)) return;
            if (!Exists(sourceName)) return;

            newName = UniqueName(SanitizeName(string.IsNullOrEmpty(newName) ? sourceName + " copy" : newName));
            if (string.IsNullOrEmpty(newName)) return;

            try
            {
                if (string.Equals(sourceName, s.ActiveProfileName, StringComparison.OrdinalIgnoreCase))
                {
                    SyncActive();
                }
                EnsureDir();
                File.Copy(PathFor(sourceName), PathFor(newName), false);
            }
            catch (Exception ex) { Main.mod?.Logger?.Log("[Profile] duplicate failed: " + ex.Message); }
        }

        internal static void Delete(string name)
        {
            Settings s = Main.settings;
            if (s == null || string.IsNullOrEmpty(name)) return;
            List<string> names = List();
            if (names.Count <= 1) return;
            if (!Exists(name)) return;

            bool wasActive = string.Equals(name, s.ActiveProfileName, StringComparison.OrdinalIgnoreCase);
            try { File.Delete(PathFor(name)); }
            catch (Exception ex) { Main.mod?.Logger?.Log("[Profile] delete failed: " + ex.Message); return; }

            if (wasActive)
            {
                names = List();
                if (names.Count == 0)
                {
                    s.ActiveProfileName = DefaultName;
                    SaveProfile(DefaultName);
                    return;
                }
                string next = names[0];
                Settings loaded = LoadProfile(next);
                if (loaded != null) CopyFields(loaded, s);
                s.ActiveProfileName = next;
                RefreshAfterSwitch();
            }
        }

        internal static void Rename(string oldName, string newName)
        {
            Settings s = Main.settings;
            if (s == null || string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName)) return;
            if (string.Equals(oldName, newName, StringComparison.Ordinal)) return;
            if (!Exists(oldName)) return;

            newName = UniqueName(SanitizeName(newName));
            if (string.IsNullOrEmpty(newName)) return;
            try { File.Move(PathFor(oldName), PathFor(newName)); }
            catch (Exception ex) { Main.mod?.Logger?.Log("[Profile] rename failed: " + ex.Message); return; }

            if (string.Equals(s.ActiveProfileName, oldName, StringComparison.OrdinalIgnoreCase))
                s.ActiveProfileName = newName;
        }

        private static void CopyFields(Settings src, Settings dst)
        {
            if (src == null || dst == null) return;
            FieldInfo[] fields = typeof(Settings).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo f in fields)
            {
                if (f.Name == "ActiveProfileName") continue;
                try { f.SetValue(dst, f.GetValue(src)); }
                catch (Exception ex) { Main.mod?.Logger?.Log("[Profile] copy '" + f.Name + "' failed: " + ex.Message); }
            }
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            char[] chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                for (int j = 0; j < InvalidNameChars.Length; j++)
                {
                    if (chars[i] == InvalidNameChars[j]) { chars[i] = '_'; break; }
                }
            }
            string s = new string(chars).Trim().Trim('.');
            return s;
        }

        private static string UniqueName(string baseName)
        {
            if (string.IsNullOrEmpty(baseName)) return "";
            if (!Exists(baseName)) return baseName;
            for (int i = 2; i < 1000; i++)
            {
                string candidate = baseName + " " + i;
                if (!Exists(candidate)) return candidate;
            }
            return baseName + " " + Guid.NewGuid().ToString("N").Substring(0, 6);
        }

        private static bool ContainsIgnoreCase(List<string> list, string item)
        {
            for (int i = 0; i < list.Count; i++)
                if (string.Equals(list[i], item, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void RefreshAfterSwitch()
        {
            Settings s = Main.settings;
            if (s == null) return;

            s.InvalidateColorRangeCache();
            s.EnsureColorRanges();

            try { Localization.SetLanguage(s.language); } catch { }
            try { FontLoader.InvalidatePreferredHudFont(); } catch { }
            try { Overlay.InvalidateOverlayFontCache(); } catch { }
            try { Status.InvalidatePercentCaches(); } catch { }
            KeyViewer.keyViewerKeys = null;
            try { SettingsGui.SyncSimpleKeysToKeyLimiter(); } catch { }
            try { ResourceChanger.RefreshChangedResources(); } catch { }
            try { Tweaks.RefreshTweaks(); } catch { }
            try { EffectRemover.RefreshEditorSaveButtons(); } catch { }
        }
    }
}
