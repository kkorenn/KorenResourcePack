using UnityEngine;

namespace KorenResourcePack
{
    internal static partial class ProgressTracker
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

        internal static void CaptureRunStart()
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
            AutoDeafen.OnRunReset();
        }
    }
}
