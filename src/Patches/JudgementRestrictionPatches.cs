using HarmonyLib;

namespace KorenResourcePack
{
    internal static partial class JudgementRestrictionPatches
    {
        [HarmonyPatch(typeof(scrMarginTracker), "AddHit", typeof(HitMargin))]
        private static class AddHitPatch
        {
            private static void Postfix(HitMargin hit) => JudgementRestriction.OnAddHitPostfix(hit);
        }
    }
}
