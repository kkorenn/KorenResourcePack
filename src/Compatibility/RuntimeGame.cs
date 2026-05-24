using System;
using System.Reflection;

namespace KorenResourcePack
{
    internal static class RuntimeGame
    {
        private static bool? cachedLegacyGame;
        private static bool loggedLegacyHudFallback;

        internal static bool IsLegacyBuild
        {
            get
            {
#if LEGACY
                return true;
#else
                return false;
#endif
            }
        }

        internal static bool IsLegacyGame()
        {
            if (cachedLegacyGame.HasValue) return cachedLegacyGame.Value;
            cachedLegacyGame = DetectLegacyGame();
            return cachedLegacyGame.Value;
        }

        internal static bool UseImguiHudText()
        {
            return IsLegacyGame();
        }

        internal static void LogLegacyHudFallbackOnce()
        {
            if (loggedLegacyHudFallback) return;
            loggedLegacyHudFallback = true;
            Main.mod?.Logger?.Log("[Overlay] Legacy game detected; using IMGUI HUD text fallback.");
        }

        private static bool DetectLegacyGame()
        {
            try
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                bool controllerCountsKeys = typeof(scrController).GetMethod("CountValidKeysPressed", flags, null, Type.EmptyTypes, null) != null;
                Type playerType = ResolveType("scrPlayer");
                bool playerCountsKeys = playerType != null && playerType.GetMethod("CountValidKeysPressed", flags, null, Type.EmptyTypes, null) != null;

                if (controllerCountsKeys && !playerCountsKeys)
                    return true;
                if (playerCountsKeys)
                    return false;
            }
            catch
            {
            }

            return IsLegacyBuild;
        }

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            Type type = Type.GetType(typeName);
            if (type != null) return type;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    type = assemblies[i].GetType(typeName, false);
                    if (type != null) return type;
                }
                catch
                {
                }
            }

            return null;
        }
    }
}
