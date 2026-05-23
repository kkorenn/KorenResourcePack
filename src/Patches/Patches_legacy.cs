#if LEGACY
using HarmonyLib;

namespace KorenResourcePack
{
    internal static partial class GamePatches
    {
        [HarmonyPatch(typeof(scrMistakesManager), "AddHit")]
        private static class MistakesManagerAddHitPatch
        {
            private static void Postfix(HitMargin hit)
            {
                RegisterHit(hit);
            }
        }
    }
}
#endif
