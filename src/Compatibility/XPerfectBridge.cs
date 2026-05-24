using System;
using System.Reflection;

namespace KorenResourcePack
{
    
    internal static class XPerfectBridge
    {
        public enum Judge
        {
            None = 0,
            X = 1,
            Plus = 2,
            Minus = 3
        }

        private static bool resolved;
        private static bool installed;

        private static Type accuracyStateType;
        private static MemberInfo lastJudgeMember;
        private static MemberInfo xCountMember;
        private static MemberInfo plusCountMember;
        private static MemberInfo minusCountMember;

        private static Type mainType;
        private static PropertyInfo enabledProp;

        public static bool Installed
        {
            get
            {
                EnsureResolved();
                return installed;
            }
        }

        public static bool Active
        {
            get
            {
                if (!Installed) return false;
                try
                {
                    if (enabledProp == null) return true;
                    object v = enabledProp.GetValue(null, null);
                    return v is bool b && b;
                }
                catch { return false; }
            }
        }

        public static Judge LastJudge()
        {
            if (!Installed || lastJudgeMember == null) return Judge.None;
            try
            {
                object v = ReadStaticMember(lastJudgeMember);
                if (v == null) return Judge.None;
                int i = Convert.ToInt32(v);
                if (i < 0 || i > 3) return Judge.None;
                return (Judge)i;
            }
            catch { return Judge.None; }
        }

        public static int XCount()
        {
            return ReadIntMember(xCountMember);
        }

        public static int PlusCount()
        {
            return ReadIntMember(plusCountMember);
        }

        public static int MinusCount()
        {
            return ReadIntMember(minusCountMember);
        }

        private static int ReadIntMember(MemberInfo member)
        {
            if (!Installed || member == null) return 0;
            try
            {
                object v = ReadStaticMember(member);
                return v == null ? 0 : Convert.ToInt32(v);
            }
            catch { return 0; }
        }

        private static object ReadStaticMember(MemberInfo member)
        {
            PropertyInfo property = member as PropertyInfo;
            if (property != null) return property.GetValue(null, null);

            FieldInfo field = member as FieldInfo;
            return field != null ? field.GetValue(null) : null;
        }

        private static MemberInfo GetStaticReadable(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            PropertyInfo property = type.GetProperty(name, flags);
            if (property != null && property.GetGetMethod(true) != null)
                return property;

            FieldInfo field = type.GetField(name, flags);
            if (field != null)
                return field;

            return type.GetField("<" + name + ">k__BackingField", flags);
        }

        private static void EnsureResolved()
        {
            if (resolved) return;
            resolved = true;
            try
            {
                Assembly xpAsm = null;
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    AssemblyName n = a.GetName();
                    if (n.Name == "XPerfect")
                    {
                        xpAsm = a;
                        break;
                    }
                }
                if (xpAsm == null) return;

                accuracyStateType = xpAsm.GetType("XPerfect.AccuracyState");
                if (accuracyStateType == null) return;

                lastJudgeMember = GetStaticReadable(accuracyStateType, "LastJudge");
                xCountMember = GetStaticReadable(accuracyStateType, "XPerfectCount");
                plusCountMember = GetStaticReadable(accuracyStateType, "PlusPerfectCount");
                minusCountMember = GetStaticReadable(accuracyStateType, "MinusPerfectCount");

                mainType = xpAsm.GetType("XPerfect.Main");
                if (mainType != null)
                {
                    enabledProp = mainType.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Static);
                }

                installed = lastJudgeMember != null;
            }
            catch
            {
                installed = false;
            }
        }
    }
}
