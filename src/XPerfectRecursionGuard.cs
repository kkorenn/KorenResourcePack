using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace KorenResourcePack
{
    /// <summary>
    /// Defensive Harmony guard against an XPerfect + TUFCourse interaction crash.
    ///
    /// Bug chain (TUFCourse + XPerfect, with or without KRP):
    ///   1. TUFCourse forces scrController.noFail = true via its Awake patch.
    ///   2. Player misses a hit -> scrPlanet.SwitchChosen() -> scrMisc.GetHitMargin().
    ///   3. XPerfect.HitMarginPatch.Postfix sees judge != XPerfect and calls scrController.FailAction().
    ///   4. scrController.FailAction(noFail==true) skips ChangeState(Fail2) and instead calls Hit(false).
    ///   5. Hit -> SwitchChosen -> GetHitMargin -> XPerfect.Postfix again -> step 3 -> infinite recursion.
    ///   6. Stack overflow crashes the player.
    ///
    /// XPerfect's own state guard (`(States) != PlayerControl`) never trips because the state never
    /// changes when noFail is on, so the bail-out path is unreachable.
    ///
    /// This guard patches XPerfect's Postfix with a thread-local depth counter and short-circuits any
    /// nested invocation (returns false from the prefix on reentry, skipping the original Postfix body).
    /// The first invocation runs normally, so single-shot fail behavior is preserved.
    /// </summary>
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
                MethodInfo postfix = typeof(XPerfectRecursionGuard).GetMethod(
                    nameof(GuardPostfix), BindingFlags.Static | BindingFlags.NonPublic);

                harmony.Patch(target,
                    prefix: new HarmonyMethod(prefix),
                    postfix: new HarmonyMethod(postfix));

                applied = true;
                modEntry?.Logger?.Log("[XPerfectGuard] Installed reentry guard on XPerfect.HitMarginPatch.Postfix.");
            }
            catch (Exception ex)
            {
                modEntry?.Logger?.Log("[XPerfectGuard] Install failed: " + ex.Message);
            }
        }

        // Returning false from a Harmony prefix skips the original method body
        // (in this case, XPerfect's own Postfix). The first call increments depth and proceeds;
        // any nested call (depth > 0) is short-circuited, breaking the recursion.
        private static bool GuardPrefix()
        {
            if (depth > 0)
            {
                return false;
            }

            depth++;
            return true;
        }

        private static void GuardPostfix()
        {
            if (depth > 0)
            {
                depth--;
            }
        }
    }
}
