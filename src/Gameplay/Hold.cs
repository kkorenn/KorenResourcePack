using System;

namespace KorenResourcePack
{
    internal static class Hold
    {
        internal static string GetHoldBehaviorLabel()
        {
            try
            {
                HoldBehavior behavior = Persistence.holdBehavior;
                switch (behavior)
                {
                    case HoldBehavior.Normal:
                        return "Holds: Normal";
                    case HoldBehavior.CanHitEnd:
                        return "Holds: Hold Tap";
                    case HoldBehavior.NoHoldNeeded:
                        return "Holds: No Holding Required";
                    default:
                        return "Holds: " + behavior;
                }
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[Warning] Hold behavior read failed: " + ex.Message);
                return null;
            }
        }
    }
}
