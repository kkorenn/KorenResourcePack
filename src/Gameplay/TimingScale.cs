namespace KorenResourcePack
{
    internal static class TimingScale
    {
        internal static float CurrentMarginScale = 1f;

        internal static void UpdateTimingScale()
        {
            try
            {
                scrController ctrl = scrController.instance;
                if (ctrl != null && ctrl.currFloor != null)
                    CurrentMarginScale = (float)ctrl.currFloor.marginScale;
            }
            catch { }
        }
    }
}
