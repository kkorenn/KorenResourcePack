using HarmonyLib;
using UnityEngine.UI;

namespace KorenResourcePack
{
    internal static class StatusPatches
    {
        [HarmonyPatch(typeof(scrShowIfDebug), "Update")]
        private static class HideDebugTextPatch
        {
            private static bool Prefix(Text ___txt)
            {
                if (!Main.modEnabled || Main.settings == null || !Main.settings.statusOn || !Main.settings.HideDebugText)
                    return true;

                if (___txt != null) ___txt.enabled = false;
                return false;
            }
        }
    }
}
