using HarmonyLib;
using UnityEngine;

namespace KorenResourcePack
{
    
    internal static class ProgressTracker
    {
        internal static float RunStartProgress;
        internal static bool RunStartedFromFirstTile = true;

        internal static bool IsFirstTileRunStart()
        {
            try
            {
                if (scnGame.instance != null)
                    return scnGame.instance.checkpointsUsed == 0;
            }
            catch { }

            try { return scrController.checkpointsUsed == 0; }
            catch { return false; }
        }

        internal static float NormalizeRunStartProgress(float progress)
        {
            return IsFirstTileRunStart() ? 0f : Mathf.Clamp01(progress);
        }

        private static void CaptureRunStart()
        {
            try
            {
                scrController c = scrController.instance;
                float progress = c != null ? c.percentComplete : 0f;
                RunStartedFromFirstTile = IsFirstTileRunStart();
                RunStartProgress = RunStartedFromFirstTile ? 0f : Mathf.Clamp01(progress);
            }
            catch
            {
                RunStartedFromFirstTile = true;
                RunStartProgress = 0f;
            }
            
            Main.InvalidatePercentCaches();
            
            Judgement.ResetJudgementDisplay();
        }

        [HarmonyPatch(typeof(scrController), "RestartProgress")]
        private static class RestartProgressPatch
        {
            private static void Postfix() => CaptureRunStart();
        }

        [HarmonyPatch(typeof(scrController), "Restart", typeof(bool))]
        private static class RestartPatch
        {
            private static void Postfix() => CaptureRunStart();
        }

        [HarmonyPatch(typeof(scrMistakesManager), "RevertToLastCheckpoint")]
        private static class RevertCheckpointPatch
        {
            private static void Postfix() => CaptureRunStart();
        }
    }
}
