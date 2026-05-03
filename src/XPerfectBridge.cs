using System;
using System.Reflection;

namespace KorenResourcePack
{
    // Soft (reflection-based) bridge to the optional XPerfect mod.
    // No compile-time reference: KorenResourcePack still works with or without XPerfect installed.
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
        private static FieldInfo lastJudgeField;
        private static FieldInfo xCountField;
        private static FieldInfo plusCountField;
        private static FieldInfo minusCountField;

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
            if (!Installed || lastJudgeField == null) return Judge.None;
            try
            {
                object v = lastJudgeField.GetValue(null);
                if (v == null) return Judge.None;
                int i = Convert.ToInt32(v);
                if (i < 0 || i > 3) return Judge.None;
                return (Judge)i;
            }
            catch { return Judge.None; }
        }

        public static int XCount()
        {
            return ReadIntField(xCountField);
        }

        public static int PlusCount()
        {
            return ReadIntField(plusCountField);
        }

        public static int MinusCount()
        {
            return ReadIntField(minusCountField);
        }

        private static int ReadIntField(FieldInfo f)
        {
            if (!Installed || f == null) return 0;
            try
            {
                object v = f.GetValue(null);
                return v == null ? 0 : Convert.ToInt32(v);
            }
            catch { return 0; }
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

                lastJudgeField = accuracyStateType.GetField("LastJudge", BindingFlags.Public | BindingFlags.Static);
                xCountField = accuracyStateType.GetField("XPerfectCount", BindingFlags.Public | BindingFlags.Static);
                plusCountField = accuracyStateType.GetField("PlusPerfectCount", BindingFlags.Public | BindingFlags.Static);
                minusCountField = accuracyStateType.GetField("MinusPerfectCount", BindingFlags.Public | BindingFlags.Static);

                mainType = xpAsm.GetType("XPerfect.Main");
                if (mainType != null)
                {
                    enabledProp = mainType.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Static);
                }

                installed = lastJudgeField != null;
            }
            catch
            {
                installed = false;
            }
        }
    }
}
