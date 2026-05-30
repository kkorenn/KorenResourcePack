using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityModManagerNet;

namespace KorenResourcePack
{
    internal static class Localization
    {
        private const string DefaultLanguage = "en";
        private static readonly Dictionary<string, Dictionary<string, string>> tables =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private static UnityModManager.ModEntry entry;

        internal static string CurrentLanguage { get; private set; } = DefaultLanguage;

        internal static void Initialize(UnityModManager.ModEntry modEntry, string language)
        {
            entry = modEntry;
            LoadLanguage(DefaultLanguage);
            LoadLanguage("kr");
            LoadLanguage("cn");

            // First run: default mod language from ADOFAI's current language.
            // Korean game -> kr; Chinese game -> cn; anything else -> en.
            if (Main.settings != null && !Main.settings.languageInitialized)
            {
                language = DefaultFromGameLanguage();
                Main.settings.languageInitialized = true;
            }
            SetLanguage(language);
        }

        /// <summary>Map ADOFAI's current language to the mod's supported set (kr, cn, or en).</summary>
        private static string DefaultFromGameLanguage()
        {
            try
            {
                switch (RDString.language)
                {
                    case UnityEngine.SystemLanguage.Korean:
                        return "kr";
                    case UnityEngine.SystemLanguage.Chinese:
                    case UnityEngine.SystemLanguage.ChineseSimplified:
                    case UnityEngine.SystemLanguage.ChineseTraditional:
                        return "cn";
                    default:
                        return DefaultLanguage;
                }
            }
            catch
            {
                return DefaultLanguage;
            }
        }

        internal static bool SetLanguage(string language)
        {
            string normalized = NormalizeLanguage(language);
            if (!tables.ContainsKey(normalized))
                LoadLanguage(normalized);
            if (!tables.ContainsKey(normalized))
                normalized = DefaultLanguage;

            bool changed = !string.Equals(CurrentLanguage, normalized, StringComparison.OrdinalIgnoreCase);
            CurrentLanguage = normalized;
            if (Main.settings != null)
                Main.settings.language = normalized;
            return changed;
        }

        internal static string Text(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";

            Dictionary<string, string> table;
            string value;
            if (tables.TryGetValue(CurrentLanguage, out table) && table.TryGetValue(key, out value))
                return value;
            if (tables.TryGetValue(DefaultLanguage, out table) && table.TryGetValue(key, out value))
                return value;
            return key;
        }

        internal static string Format(string key, params object[] args)
        {
            try { return string.Format(Text(key), args); }
            catch { return Text(key); }
        }

        private static string NormalizeLanguage(string language)
        {
            if (string.Equals(language, "ko", StringComparison.OrdinalIgnoreCase)) return "kr";
            if (string.Equals(language, "ko-KR", StringComparison.OrdinalIgnoreCase)) return "kr";
            if (string.Equals(language, "korean", StringComparison.OrdinalIgnoreCase)) return "kr";
            if (string.Equals(language, "zh", StringComparison.OrdinalIgnoreCase)) return "cn";
            if (string.Equals(language, "zh-CN", StringComparison.OrdinalIgnoreCase)) return "cn";
            if (string.Equals(language, "chinese", StringComparison.OrdinalIgnoreCase)) return "cn";
            if (string.Equals(language, "english", StringComparison.OrdinalIgnoreCase)) return "en";
            return string.IsNullOrEmpty(language) ? DefaultLanguage : language;
        }

        private static void LoadLanguage(string language)
        {
            string normalized = NormalizeLanguage(language);
            if (tables.ContainsKey(normalized)) return;

            string path = GetLocalizationPath(normalized);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                entry?.Logger?.Log("[Localization] missing " + normalized + ".json");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var table = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (table == null) table = new Dictionary<string, string>();
                tables[normalized] = new Dictionary<string, string>(table, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                entry?.Logger?.Log("[Localization] failed to load " + path + ": " + ex.Message);
            }
        }

        private static string GetLocalizationPath(string language)
        {
            string fileName = language + ".json";
            if (entry != null && !string.IsNullOrEmpty(entry.Path))
            {
                string packaged = Path.Combine(entry.Path, "localization", fileName);
                if (File.Exists(packaged)) return packaged;
            }

            string cwd = Directory.GetCurrentDirectory();
            string dev = Path.Combine(cwd, "localization", fileName);
            if (File.Exists(dev)) return dev;

            string parentDev = Path.Combine(cwd, "Mods", "KorenResourcePack", "localization", fileName);
            return parentDev;
        }
    }
}
