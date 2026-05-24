using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace KorenResourcePack
{
    
    internal static class XPerfectRecursionGuard
    {
        [ThreadStatic] private static int depth;

        private static bool applied;

        public static void TryApply(Harmony harmony, UnityModManager.ModEntry modEntry)
        {
            if (applied) return;
            try
            {
                Type patchType = AccessTools.TypeByName("XPerfect.HitMarginPatch");
                if (patchType == null)
                {
                    return;
                }

                MethodInfo target = AccessTools.Method(patchType, "Postfix");
                if (target == null)
                {
                    modEntry?.Logger?.Log("[XPerfectGuard] XPerfect.HitMarginPatch.Postfix not found; guard not installed.");
                    return;
                }

                MethodInfo prefix = typeof(XPerfectRecursionGuard).GetMethod(
                    nameof(GuardPrefix), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo finalizer = typeof(XPerfectRecursionGuard).GetMethod(
                    nameof(GuardFinalizer), BindingFlags.Static | BindingFlags.NonPublic);

                harmony.Patch(target,
                    prefix: new HarmonyMethod(prefix),
                    finalizer: new HarmonyMethod(finalizer));

                applied = true;
                modEntry?.Logger?.Log("[XPerfectGuard] Installed reentry guard on XPerfect.HitMarginPatch.Postfix.");
            }
            catch (Exception ex)
            {
                modEntry?.Logger?.Log("[XPerfectGuard] Install failed: " + ex.Message);
            }
        }

        private static bool GuardPrefix(ref bool __state)
        {
            __state = false;
            if (depth > 0)
            {
                return false;
            }

            depth++;
            __state = true;
            return true;
        }

        private static Exception GuardFinalizer(bool __state, Exception __exception)
        {
            if (__state && depth > 0)
            {
                depth--;
            }
            return __exception;
        }
    }
}
