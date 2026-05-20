using System.Reflection;
using TMPro;

namespace KorenResourcePack
{
    internal static class TmpCompatibility
    {
        internal static void DisableWordWrapping(TextMeshProUGUI text)
        {
            if (text == null) return;

#if LEGACY
            PropertyInfo property = FindProperty(text.GetType(), "enableWordWrapping");
            if (property != null && property.CanWrite)
            {
                property.SetValue(text, false, null);
                return;
            }

            FieldInfo field = FindField(text.GetType(), "m_enableWordWrapping");
            if (field != null)
                field.SetValue(text, false);
#else
            text.textWrappingMode = TextWrappingModes.NoWrap;
#endif
        }

#if LEGACY
        private static PropertyInfo FindProperty(System.Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            while (type != null)
            {
                PropertyInfo property = type.GetProperty(name, flags);
                if (property != null) return property;
                type = type.BaseType;
            }
            return null;
        }

        private static FieldInfo FindField(System.Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            while (type != null)
            {
                FieldInfo field = type.GetField(name, flags);
                if (field != null) return field;
                type = type.BaseType;
            }
            return null;
        }
#endif
    }
}
