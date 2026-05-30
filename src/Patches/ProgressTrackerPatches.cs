using HarmonyLib;

namespace KorenResourcePack
{
    internal static class ProgressTrackerPatches
    {
        [HarmonyPatch(typeof(scrController), "RestartProgress")]
        private static class RestartProgressPatch
        {
            private static void Postfix() => ProgressTracker.CaptureRunStart();
        }

        [HarmonyPatch(typeof(scrController), "Restart", typeof(bool))]
        private static class RestartPatch
        {
            private static void Postfix() => ProgressTracker.CaptureRunStart();
        }

        [HarmonyPatch(typeof(scrMistakesManager), "RevertToLastCheckpoint")]
        private static class RevertCheckpointPatch
        {
            private static void Postfix() => ProgressTracker.CaptureRunStart();
        }
    }
}
