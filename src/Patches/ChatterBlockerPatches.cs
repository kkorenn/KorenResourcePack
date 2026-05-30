using HarmonyLib;
using SkyHook;

namespace KorenResourcePack
{
    internal static class ChatterBlockerPatches
    {
        [HarmonyPatch(typeof(scrPlayer), "CountValidKeysPressed")]
        private static class ScrPlayerCountValidKeysPressedPatch
        {
            private static bool Prefix(ref int __result) => ChatterBlocker.OnCountValidKeysPressedPrefix(ref __result);
        }

        [HarmonyPatch(typeof(SkyHookManager), "HookCallback")]
        private static class SkyHookManagerHookCallbackPatch
        {
            private static bool Prefix(SkyHookEvent __0) => ChatterBlocker.OnSkyHookEvent(__0);
        }
    }
}
