using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KorenResourcePack
{
    
    internal static partial class JudgementRestriction
    {
        
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
                    
                    m.Invoke(c, new object[] { false, false, reason ?? "", false });
                    return;
                }
                
                MethodInfo restart = typeof(scrController).GetMethod("Restart",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(bool) }, null);
                if (restart != null) restart.Invoke(c, new object[] { false });
            }
            catch { }
        }

        private static bool ShouldFailFor(HitMargin margin)
        {
            int marginInt = (int)margin;
            switch (Main.settings.JRestrictMode)
            {
                case 1: 
                    return marginInt != (int)HitMargin.Perfect;
                case 2: 
                {
                    
                    if (marginInt != (int)HitMargin.Perfect) return true;
                    
                    XPerfectBridge.Judge xj = XPerfectBridge.LastJudge();
                    if (xj == XPerfectBridge.Judge.None) return false; 
                    return xj != XPerfectBridge.Judge.X;
                }
                case 4: 
                    return margin == HitMargin.TooEarly;
                case 3: 
                {
                    int mask = Main.settings.JRestrictAllowedMask;
                    if (mask == 0) return false; 
                    int bit = 1 << marginInt;
                    return (mask & bit) == 0;
                }
                case 0:
                default: 
                {
                    try
                    {
                        scrMistakesManager m = MistakesAccess.Get();
                        if (m == null) return false;
                        float acc = MistakesAccess.PercentAcc(m);
                        if (float.IsNaN(acc) || float.IsInfinity(acc)) return false;
                        return acc * 100f < Main.settings.JRestrictAccuracy;
                    }
                    catch { return false; }
                }
            }
        }

        private static void AfterAddHit(HitMargin hit)
        {
            if (!Main.modEnabled || Main.settings == null || !Main.settings.JRestrictOn) return;
            
            if (hit == HitMargin.Auto) return;
            if (ShouldFailFor(hit))
            {
                TriggerFail("KRP: judgement restriction");
            }
        }

#if !LEGACY
        [HarmonyPatch(typeof(scrMarginTracker), "AddHit", typeof(HitMargin))]
        private static class AddHitPatch
        {
            private static void Postfix(HitMargin hit)
            {
                AfterAddHit(hit);
            }
        }
#endif
    }
}
