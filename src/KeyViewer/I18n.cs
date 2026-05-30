// Localization shim. JipperKeyViewer originally had its own i18n + lang.json; in KorenResourcePack
// all strings instead live in KRP's localization/*.json under the "jkv." prefix and are resolved
// through KorenResourcePack.Localization. / 本地化垫片：所有字符串改用 KRP 的本地化系统（"jkv." 前缀）。
using KorenResourcePack;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Thin adapter over <see cref="KorenResourcePack.Localization"/>. Keeps the original
    /// I18n.Tr(key) call sites working without a parallel translation system.
    /// </summary>
    public static class I18n
    {
        /// <summary>Mirrors KRP's current language; setter is ignored (KRP owns language).</summary>
        public static string Lang
        {
            get { return Localization.CurrentLanguage; }
            set { /* language is controlled by KorenResourcePack */ }
        }

        /// <summary>No-op: translations are loaded by KorenResourcePack.Localization.</summary>
        public static void Load() { }

        /// <summary>Resolve a JKV key through KRP localization (keys stored as "jkv.&lt;key&gt;").</summary>
        public static string Tr(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            return Localization.Text("jkv." + key);
        }
    }
}
