#if LEGACY
using System;
using System.Collections;
using System.Reflection;

namespace KorenResourcePack
{
    internal static class LegacyReflection
    {
        private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        internal static Type ResolveType(params string[] typeNames)
        {
            for (int i = 0; i < typeNames.Length; i++)
            {
                Type t = Type.GetType(typeNames[i], false);
                if (t != null) return t;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int ai = 0; ai < assemblies.Length; ai++)
            {
                Assembly asm = assemblies[ai];
                for (int ti = 0; ti < typeNames.Length; ti++)
                {
                    Type t = asm.GetType(typeNames[ti], false);
                    if (t != null) return t;
                }
            }

            return null;
        }

        internal static bool TryGetMemberValue(object target, string name, out object value)
        {
            value = null;
            if (target == null || string.IsNullOrEmpty(name)) return false;

            Type type = target.GetType();
            FieldInfo field = type.GetField(name, InstanceFlags);
            if (field != null)
            {
                value = field.GetValue(target);
                return true;
            }

            PropertyInfo property = type.GetProperty(name, InstanceFlags);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                value = property.GetValue(target, null);
                return true;
            }

            return false;
        }

        internal static void SetMemberValue(object target, string name, object value)
        {
            if (target == null || string.IsNullOrEmpty(name)) return;

            Type type = target.GetType();
            FieldInfo field = type.GetField(name, InstanceFlags);
            if (field != null)
            {
                field.SetValue(target, ConvertValue(value, field.FieldType));
                return;
            }

            PropertyInfo property = type.GetProperty(name, InstanceFlags);
            if (property != null && property.CanWrite && property.GetIndexParameters().Length == 0)
                property.SetValue(target, ConvertValue(value, property.PropertyType), null);
        }

        internal static double GetDouble(object target, string name, double fallback)
        {
            object value;
            if (!TryGetMemberValue(target, name, out value) || value == null) return fallback;
            try { return Convert.ToDouble(value); }
            catch { return fallback; }
        }

        internal static IDictionary GetDictionary(object target, string name)
        {
            object value;
            return TryGetMemberValue(target, name, out value) ? value as IDictionary : null;
        }

        internal static void IncrementNumericMember(object target, string name)
        {
            object value;
            if (!TryGetMemberValue(target, name, out value)) return;

            Type valueType = value != null ? value.GetType() : typeof(int);
            try { SetMemberValue(target, name, Convert.ToInt64(value) + 1L); }
            catch { SetMemberValue(target, name, ConvertValue(1, valueType)); }
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (targetType == null || value == null) return value;
            Type nullableType = Nullable.GetUnderlyingType(targetType);
            if (nullableType != null) targetType = nullableType;
            if (targetType.IsInstanceOfType(value)) return value;
            return Convert.ChangeType(value, targetType);
        }
    }
}
#endif
