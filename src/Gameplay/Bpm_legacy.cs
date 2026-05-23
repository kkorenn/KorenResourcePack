#if LEGACY
namespace KorenResourcePack
{
    internal static partial class Bpm
    {
        private static double GetControllerSpeed(scrController controller)
        {
            return LegacyReflection.GetDouble(controller, "speed", 1.0);
        }
    }
}
#endif
