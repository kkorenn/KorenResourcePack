using KorenResourcePack;

namespace JipperKeyViewer.KeyViewer
{
    public static class I18n
    {
        public static string Lang
        {
            get { return Localization.CurrentLanguage; }
            set {   }
        }

        public static void Load() { }

        public static string Tr(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            return Localization.Text("jkv." + key);
        }
    }
}
