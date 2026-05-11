using HarmonyLib;
using UnityEngine;

namespace KorenResourcePack
{
    // Tracks the percentComplete value at the moment the player started the current run.
    // 0 for a fresh restart from the beginning; the checkpoint % when resumed mid-level.
    // Read by ProgressBar (fill starts here) and the Progress status text ("X% - Y%").
    internal static class ProgressTracker
    {
        internal static float RunStartProgress;

        private static void CaptureRunStart()
        {
            try
            {
                scrController c = scrController.instance;
                RunStartProgress = c != null ? Mathf.Clamp01(c.percentComplete) : 0f;
            }
            catch { RunStartProgress = 0f; }
            // The cached progress strings key off `progress` only; bust them so the new
            // start% takes effect on the next Status/Overlay tick.
            Main.InvalidatePercentCaches();
            // Restart points (fresh attempt, checkpoint revert, full restart) must also
            // wipe the judgement-count display. Without this, slot counters from the prior
            // attempt linger on the screen until the player triggers another reset path.
            Judgement.ResetJudgementDisplay();
        }

        // RestartProgress fires for both fresh-attempt restarts and the post-revert path,
        // so a single Postfix here covers both cases.
        [HarmonyPatch(typeof(scrController), "RestartProgress")]
        private static class RestartProgressPatch
        {
            private static void Postfix() => CaptureRunStart();
        }

        // Restart(bool) is the public entry point — patches stack so the start point is
        // refreshed even when other mods invoke Restart directly.
        [HarmonyPatch(typeof(scrController), "Restart", typeof(bool))]
        private static class RestartPatch
        {
            private static void Postfix() => CaptureRunStart();
        }

        // Checkpoint revert specifically — covers the path where Restart isn't called but
        // currentSeqID jumps backward.
        [HarmonyPatch(typeof(scrMistakesManager), "RevertToLastCheckpoint")]
        private static class RevertCheckpointPatch
        {
            private static void Postfix() => CaptureRunStart();
        }
    }
}
