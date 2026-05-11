using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KorenResourcePack
{
    // Judgement restriction. After every AddHit() the game registers, we check whether
    // the player still satisfies the configured rule. If not, fail the level immediately.
    //
    // Modes:
    //   0 = accuracy threshold (current accuracy must stay >= JRestrictAccuracy %)
    //   1 = Pure Perfect only  (every hit must be HitMargin.Perfect)
    //   2 = XPure Perfect only (uses XPerfectBridge.LastJudge if available)
    //   3 = Custom             (every hit must be in JRestrictAllowedMask bitmask)
    internal static class JudgementRestriction
    {
        // ---- Reflection cache for scrController.FailAction(bool, bool, string, bool) ----
        private static MethodInfo failActionMethod;
        private static bool failActionLookupAttempted;

        private static MethodInfo GetFailAction()
        {
            if (failActionLookupAttempted) return failActionMethod;
            failActionLookupAttempted = true;
            try
            {
                failActionMethod = typeof(scrController).GetMethod(
                    "FailAction",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(bool), typeof(bool), typeof(string), typeof(bool) },
                    null);
            }
            catch { failActionMethod = null; }
            return failActionMethod;
        }

        private static void TriggerFail(string reason)
        {
            try
            {
                scrController c = scrController.instance;
                if (c == null) return;
                MethodInfo m = GetFailAction();
                if (m != null)
                {
                    // Args inferred from disassembly: (a, b, msg, c) — most callers pass
                    // (true, false, "", false). Mirroring that keeps the engine's failure
                    // animation consistent with a normal hitbox fail.
                    m.Invoke(c, new object[] { true, false, reason ?? "", false });
                    return;
                }
                // Fallback: trigger a restart if FailAction isn't reachable for any reason.
                MethodInfo restart = typeof(scrController).GetMethod("Restart",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(bool) }, null);
                if (restart != null) restart.Invoke(c, new object[] { false });
            }
            catch { }
        }

        // HitMargin enum values from disassembly:
        //   0 TooEarly, 1 VeryEarly, 2 EarlyPerfect, 3 Perfect, 4 LatePerfect,
        //   5 VeryLate, 6 TooLate, 7 Multipress, 8 FailMiss, 9 FailOverload,
        //  10 Auto, 11 OverPress
        private static bool ShouldFailFor(HitMargin margin)
        {
            int marginInt = (int)margin;
            switch (Main.settings.JRestrictMode)
            {
                case 1: // Pure Perfect only — strict Perfect (slot 3)
                    return marginInt != (int)HitMargin.Perfect;
                case 2: // XPure Perfect only — relies on XPerfect mod's LastJudge.
                {
                    // First gate: must be a regular Perfect from the engine.
                    if (marginInt != (int)HitMargin.Perfect) return true;
                    // Then ask XPerfect what kind of Perfect this was. Judge.X = the XPure
                    // tier; anything else (Plus / Minus) fails the restriction.
                    XPerfectBridge.Judge xj = XPerfectBridge.LastJudge();
                    if (xj == XPerfectBridge.Judge.None) return false; // XPerfect not running -> trust the engine
                    return xj != XPerfectBridge.Judge.X;
                }
                case 3: // Custom bitmask
                {
                    int mask = Main.settings.JRestrictAllowedMask;
                    if (mask == 0) return false; // empty mask = nothing fails
                    int bit = 1 << marginInt;
                    return (mask & bit) == 0;
                }
                case 0:
                default: // Accuracy threshold
                {
                    try
                    {
                        scrMistakesManager m = scrController.instance != null ? scrController.instance.mistakesManager : null;
                        if (m == null) return false;
                        float acc = m.percentAcc;
                        if (float.IsNaN(acc) || float.IsInfinity(acc)) return false;
                        return acc * 100f < Main.settings.JRestrictAccuracy;
                    }
                    catch { return false; }
                }
            }
        }

        [HarmonyPatch(typeof(scrMistakesManager), "AddHit", typeof(HitMargin))]
        private static class AddHitPatch
        {
            private static void Postfix(HitMargin hit)
            {
                if (!Main.modEnabled || Main.settings == null || !Main.settings.JRestrictOn) return;
                // AddHit fires for Auto and FailOverload too; ignore those system hits.
                if (hit == HitMargin.Auto) return;
                if (ShouldFailFor(hit))
                {
                    TriggerFail("KRP: judgement restriction");
                }
            }
        }
    }
}
