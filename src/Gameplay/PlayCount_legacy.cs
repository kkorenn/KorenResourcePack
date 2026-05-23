#if LEGACY
namespace KorenResourcePack
{
    internal static partial class PlayCount
    {
        private static double GetCurrentControllerSpeed()
        {
            return LegacyReflection.GetDouble(ADOBase.controller, "speed", 1.0);
        }
    }
}
#endif
