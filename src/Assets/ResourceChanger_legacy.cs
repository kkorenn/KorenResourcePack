#if LEGACY
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KorenResourcePack
{
    internal static partial class ResourceChanger
    {
        private static MethodBase FirstScrRingMethod(string name, Type firstParameterType)
        {
            MethodInfo[] methods = typeof(scrRing).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != name) continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 0 && parameters[0].ParameterType == firstParameterType)
                    return method;
            }

            return null;
        }

        [HarmonyPatch]
        private static class ScrRingSetColorPatch
        {
            [HarmonyPrepare]
            private static bool Prepare()
            {
                return TargetMethod() != null;
            }

            private static MethodBase TargetMethod()
            {
                return FirstScrRingMethod("set_color", typeof(Color));
            }

            private static void Prefix(ref Color __0)
            {
                if (ShouldChangeBall) __0 = RingColor(0);
            }
        }

        [HarmonyPatch]
        private static class ScrRingDoColorPatch
        {
            [HarmonyPrepare]
            private static bool Prepare()
            {
                return TargetMethod() != null;
            }

            private static MethodBase TargetMethod()
            {
                return FirstScrRingMethod("DOColor", typeof(Color));
            }

            private static void Prefix(ref Color __0)
            {
                if (ShouldChangeBall) __0 = RingColor(0);
            }
        }

        [HarmonyPatch]
        private static class ScrRingDoFadePatch
        {
            [HarmonyPrepare]
            private static bool Prepare()
            {
                return TargetMethod() != null;
            }

            private static MethodBase TargetMethod()
            {
                return FirstScrRingMethod("DOFade", typeof(float));
            }

            private static void Prefix(ref float __0)
            {
                if (ShouldChangeBall) __0 = 0f;
            }
        }

        [HarmonyPatch(typeof(scnLevelSelect), "RainbowMode")]
        private static class LevelSelectRainbowPatch
        {
            private static bool Prefix()
            {
                return !ShouldChangeBall;
            }
        }

        [HarmonyPatch(typeof(scnLevelSelect), "EnbyMode")]
        private static class LevelSelectEnbyPatch
        {
            private static bool Prefix()
            {
                return !ShouldChangeBall;
            }
        }
    }
}
#endif
