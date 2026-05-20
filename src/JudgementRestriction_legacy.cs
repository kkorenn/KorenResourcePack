#if LEGACY
using HarmonyLib;

namespace KorenResourcePack
{
    internal static partial class JudgementRestriction
    {
        [HarmonyPatch(typeof(scrMistakesManager), "AddHit", typeof(HitMargin))]
        private static class AddHitPatch
        {
            private static void Postfix(HitMargin hit)
            {
                AfterAddHit(hit);
            }
        }
    }
}
#endif
