using System.Reflection;
using TMPro;
using UnityEngine;

namespace KorenResourcePack
{
    internal static class TmpCompatibility
    {
        internal static void DisableWordWrapping(TextMeshProUGUI text)
        {
            if (text == null) return;

            PropertyInfo property = FindProperty(text.GetType(), "enableWordWrapping");
            if (property != null && property.CanWrite)
            {
                property.SetValue(text, false, null);
                return;
            }

            FieldInfo field = FindField(text.GetType(), "m_enableWordWrapping");
            if (field != null)
                field.SetValue(text, false);
        }

        internal static Material GetFontMaterial(TextMeshProUGUI text)
        {
            string[] materialMembers =
            {
                "fontMaterial",
                "fontSharedMaterial",
                "m_fontMaterial",
                "m_currentMaterial",
                "m_sharedMaterial",
                "m_material"
            };

            for (int i = 0; i < materialMembers.Length; i++)
            {
                object value;
                if (!TryGetMemberValue(text, materialMembers[i], out value))
                    continue;

                Material material = value as Material;
                if (material != null) return material;

                Material[] materials = value as Material[];
                if (materials != null && materials.Length > 0 && materials[0] != null)
                    return materials[0];
            }

            return null;
        }

        internal static void SetFontSharedMaterial(TextMeshProUGUI text, Material material)
        {
            if (text == null || material == null) return;
            TrySetMemberValue(text, "fontSharedMaterial", material);
            TrySetMaterialArrayMember(text, "fontSharedMaterials", material);
            TrySetMaterialArrayMember(text, "m_fontSharedMaterials", material);
        }

        internal static void RefreshTextRendering(TextMeshProUGUI text)
        {
            if (text == null) return;
            TryInvoke(text, "UpdateMeshPadding");
            TryInvoke(text, "SetVerticesDirty");
            TryInvoke(text, "SetMaterialDirty");
            TryInvoke(text, "SetLayoutDirty");
        }

        // Safe wrapper around outlineColor/outlineWidth setters. TMP's outlineColor
        // setter calls CreateMaterialInstance(fontSharedMaterial) under the hood; on
        // legacy ADOFAI and some Windows builds, TMP_Settings.defaultFontAsset is
        // null, so a freshly AddComponent'd TextMeshProUGUI has no fontSharedMaterial
        // and the setter throws ArgumentNullException. Skip when no material yet —
        // call this again after the bundle font is bound.
        internal static bool TrySetOutline(TextMeshProUGUI text, Color color, float width)
        {
            if (text == null) return false;
            if (GetFontMaterial(text) == null) return false;
            try
            {
                text.outlineColor = color;
                text.outlineWidth = width;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetMemberValue(object target, string name, out object value)
        {
            value = null;
            if (target == null || string.IsNullOrEmpty(name)) return false;

            try
            {
                PropertyInfo property = FindProperty(target.GetType(), name);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    value = property.GetValue(target, null);
                    return true;
                }

                FieldInfo field = FindField(target.GetType(), name);
                if (field != null)
                {
                    value = field.GetValue(target);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool TrySetMemberValue(object target, string name, object value)
        {
            if (target == null || string.IsNullOrEmpty(name)) return false;

            try
            {
                PropertyInfo property = FindProperty(target.GetType(), name);
                if (property != null && property.CanWrite && property.GetIndexParameters().Length == 0)
                {
                    property.SetValue(target, value, null);
                    return true;
                }

                FieldInfo field = FindField(target.GetType(), name);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool TrySetMaterialArrayMember(object target, string name, Material material)
        {
            if (target == null || material == null || string.IsNullOrEmpty(name)) return false;

            try
            {
                Material[] materials = new[] { material };
                PropertyInfo property = FindProperty(target.GetType(), name);
                if (property != null && property.CanWrite && property.GetIndexParameters().Length == 0
                    && property.PropertyType == typeof(Material[]))
                {
                    property.SetValue(target, materials, null);
                    return true;
                }

                FieldInfo field = FindField(target.GetType(), name);
                if (field != null && field.FieldType == typeof(Material[]))
                {
                    field.SetValue(target, materials);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static void TryInvoke(object target, string name)
        {
            if (target == null || string.IsNullOrEmpty(name)) return;

            try
            {
                MethodInfo method = FindMethod(target.GetType(), name);
                if (method != null)
                    method.Invoke(target, null);
            }
            catch
            {
            }
        }

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

        private static MethodInfo FindMethod(System.Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            while (type != null)
            {
                MethodInfo method = type.GetMethod(name, flags, null, System.Type.EmptyTypes, null);
                if (method != null) return method;
                type = type.BaseType;
            }
            return null;
        }
    }
}
