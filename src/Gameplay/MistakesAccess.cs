using System.Reflection;

namespace KorenResourcePack
{
    // Access scrController.mistakesManager and its percentAcc/percentXAcc fields.
    // Legacy builds run against game versions that lack these members on scrController,
    // so direct access throws MissingMethodException at JIT — try/catch in the caller
    // cannot catch it because the failure occurs on method entry, before the try block.
    internal static class MistakesAccess
    {
#if LEGACY
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static MemberInfo _managerMember;
        private static bool _managerLookupAttempted;

        private static MemberInfo _accMember;
        private static bool _accLookupAttempted;

        private static MemberInfo _xAccMember;
        private static bool _xAccLookupAttempted;
#endif

        internal static scrMistakesManager Get()
        {
            scrController ctrl = scrController.instance;
            if (ctrl == null) return null;
#if LEGACY
            try
            {
                if (!_managerLookupAttempted)
                {
                    _managerLookupAttempted = true;
                    System.Type t = ctrl.GetType();
                    _managerMember = (MemberInfo)t.GetField("mistakesManager", Flags)
                                     ?? t.GetProperty("mistakesManager", Flags)
                                     ?? (MemberInfo)t.GetField("mistakeManager", Flags)
                                     ?? t.GetProperty("mistakeManager", Flags);
                }

                FieldInfo f = _managerMember as FieldInfo;
                if (f != null) return f.GetValue(ctrl) as scrMistakesManager;
                PropertyInfo p = _managerMember as PropertyInfo;
                if (p != null) return p.GetValue(ctrl, null) as scrMistakesManager;
                return null;
            }
            catch { return null; }
#else
            return ctrl.mistakesManager;
#endif
        }

        internal static float PercentAcc(scrMistakesManager m)
        {
            if (m == null) return 1f;
#if LEGACY
            return ReadFloat(m, "percentAcc", ref _accMember, ref _accLookupAttempted);
#else
            return m.percentAcc;
#endif
        }

        internal static float PercentXAcc(scrMistakesManager m)
        {
            if (m == null) return 1f;
#if LEGACY
            return ReadFloat(m, "percentXAcc", ref _xAccMember, ref _xAccLookupAttempted);
#else
            return m.percentXAcc;
#endif
        }

#if LEGACY
        private static float ReadFloat(scrMistakesManager target, string name, ref MemberInfo cached, ref bool attempted)
        {
            try
            {
                if (!attempted)
                {
                    attempted = true;
                    System.Type t = target.GetType();
                    cached = (MemberInfo)t.GetField(name, Flags) ?? t.GetProperty(name, Flags);
                }

                object value = null;
                FieldInfo f = cached as FieldInfo;
                if (f != null) value = f.GetValue(target);
                else
                {
                    PropertyInfo p = cached as PropertyInfo;
                    if (p != null) value = p.GetValue(target, null);
                }

                if (value == null) return 1f;
                return System.Convert.ToSingle(value);
            }
            catch { return 1f; }
        }
#endif
    }
}
