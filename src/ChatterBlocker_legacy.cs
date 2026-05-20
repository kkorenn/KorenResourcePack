#if LEGACY
using System.Collections;
using HarmonyLib;
using SkyHook;

namespace KorenResourcePack
{
    internal static partial class ChatterBlocker
    {
        private static void RecordKeyStatsImpl(scrController controller, object key)
        {
            if (controller == null || key == null) return;

            IDictionary keyFrequency = LegacyReflection.GetDictionary(controller, "keyFrequency");
            if (keyFrequency != null)
            {
                object current = keyFrequency.Contains(key) ? keyFrequency[key] : null;
                int next = current is int ? (int)current + 1 : 0;
                keyFrequency[key] = next;
            }

            LegacyReflection.IncrementNumericMember(controller, "keyTotal");
        }

        private static void ResetKeyLimiterOverCounter(scrController controller)
        {
            LegacyReflection.SetMemberValue(controller, "keyLimiterOverCounter", 0);
        }

        [HarmonyPatch(typeof(scrController), "CountValidKeysPressed")]
        private static class ScrControllerCountValidKeysPressedPatch
        {
            private static bool Prefix(ref int __result)
            {
                return CountValidKeysPressedPrefix(ref __result);
            }
        }

        [HarmonyPatch(typeof(SkyHookManager), "HookCallback")]
        private static class SkyHookManagerHookCallbackPatch
        {
            private static bool Prefix(SkyHookEvent ev)
            {
                return HandleSkyHookEvent(ev);
            }
        }
    }
}
#endif
