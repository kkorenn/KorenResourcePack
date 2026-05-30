using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KorenResourcePack
{
    internal static partial class ResourceChanger
    {
        [HarmonyPatch(typeof(scnEditor), "OttoUpdate")]
        private static class OttoUpdatePatch
        {
            private static void Postfix()
            {
                ApplyOttoIcon();
            }
        }

        [HarmonyPatch(typeof(scnEditor), "Update")]
        private static class OttoUpdateForcePatch
        {
            private static void Postfix()
            {
                ApplyOttoIcon();
            }
        }

        [HarmonyPatch(typeof(scnEditor), "OttoBlink")]
        private static class OttoBlinkPatch
        {
            private static void Postfix()
            {
                ApplyOttoIcon();
            }
        }

        [HarmonyPatch(typeof(scrPlanet), "Start")]
        private static class PlanetStartPatch
        {
            private static void Postfix(scrPlanet __instance)
            {
                InvalidatePlanetCache();
                if (ShouldChangeBall) ApplyPlanetColor(__instance);
            }
        }

        [HarmonyPatch(typeof(PlanetRenderer), "Awake")]
        private static class PlanetRendererAwakeResourceColorPatch
        {
            private static void Postfix(PlanetRenderer __instance)
            {
                InvalidatePlanetCache();
                if (ShouldChangeBall) ApplyPlanetRendererColor(__instance);
            }
        }

        [HarmonyPatch(typeof(PlanetRenderer), "Revive")]
        private static class PlanetRendererReviveResourceColorPatch
        {
            private static void Postfix(PlanetRenderer __instance)
            {
                if (ShouldChangeBall) ApplyPlanetRendererColor(__instance);
            }
        }

        [HarmonyPatch(typeof(PlanetRenderer), "PlayParticles")]
        private static class PlanetRendererPlayParticlesResourceColorPatch
        {
            private static void Postfix(PlanetRenderer __instance)
            {
                if (!ShouldChangeBall) return;
                NormalizeBallOpacitySettings();
                ApplyTailParticleColor(__instance, TailColor(GetPlanetSlot(__instance)));
            }
        }

        [HarmonyPatch]
        private static class PlanetRendererColorBlockPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return ExistingMethods(
                    typeof(PlanetRenderer),
                    "SetRainbow",
                    "LoadPlanetColor",
                    "SetColor"
                );
            }

            private static bool Prefix(PlanetRenderer __instance)
            {
                if (applyingPlanetColor) return true;
                if (!ShouldChangeBall) return true;
                ApplyPlanetRendererColor(__instance);
                ApplyLogoColor(scrLogoText.instance);
                return false;
            }
        }

        [HarmonyPatch]
        private static class PlanetRendererBallForceColorPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return ExistingMethods(
                    typeof(PlanetRenderer),
                    "SetPlanetColor",
                    "SetCoreColor",
                    "SetTailColor",
                    "SetFaceColor"
                );
            }

            private static void Prefix(PlanetRenderer __instance, MethodBase __originalMethod, ref Color __0)
            {
                if (applyingPlanetColor) return;
                if (ShouldChangeBall)
                {
                    NormalizeBallOpacitySettings();
                    int slot = GetPlanetSlot(__instance);
                    __0 = __originalMethod != null && __originalMethod.Name == "SetTailColor"
                        ? TailColor(slot)
                        : BallColor(slot);
                }
            }

            private static void Postfix(PlanetRenderer __instance, MethodBase __originalMethod)
            {
                if (applyingPlanetColor) return;
                if (!ShouldChangeBall || __originalMethod == null || __originalMethod.Name != "SetTailColor") return;

                NormalizeBallOpacitySettings();
                ApplyTailParticleColor(__instance, TailColor(GetPlanetSlot(__instance)));
            }
        }

        [HarmonyPatch]
        private static class PlanetRendererRingForceColorPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return ExistingMethods(
                    typeof(PlanetRenderer),
                    "SetRingColor"
                );
            }

            private static void Prefix(PlanetRenderer __instance, ref Color __0)
            {
                if (applyingPlanetColor) return;
                if (ShouldChangeBall)
                {
                    NormalizeBallOpacitySettings();
                    __0 = RingColor(GetPlanetSlot(__instance));
                }
            }
        }

        [HarmonyPatch(typeof(scrRing), "set_color")]
        private static class ScrRingSetColorPatch
        {
            private static void Prefix(ref Color __0)
            {
                if (ShouldChangeBall) __0 = RingColor(0);
            }
        }

        [HarmonyPatch(typeof(scrRing), "DOColor")]
        private static class ScrRingDoColorPatch
        {
            private static void Prefix(ref Color __0)
            {
                if (ShouldChangeBall) __0 = RingColor(0);
            }
        }

        [HarmonyPatch(typeof(scrRing), "DOFade")]
        private static class ScrRingDoFadePatch
        {
            private static void Prefix(ref float __0)
            {
                if (ShouldChangeBall) __0 = 0f;
            }
        }

        [HarmonyPatch(typeof(PlanetarySystem), "RainbowMode")]
        private static class LevelSelectRainbowPatch
        {
            private static bool Prefix()
            {
                return !ShouldChangeBall;
            }
        }

        [HarmonyPatch(typeof(PlanetarySystem), "EnbyMode")]
        private static class LevelSelectEnbyPatch
        {
            private static bool Prefix()
            {
                return !ShouldChangeBall;
            }
        }

        [HarmonyPatch(typeof(scrLogoText), "Awake")]
        private static class LogoAwakePatch
        {
            private static void Postfix(scrLogoText __instance)
            {
                if (ShouldChangeBall) ApplyLogoColor(__instance);
            }
        }

        [HarmonyPatch(typeof(scrLogoText), "UpdateColors")]
        private static class LogoUpdateColorsPatch
        {
            private static bool Prefix(scrLogoText __instance)
            {
                if (!ShouldChangeBall) return true;
                ApplyLogoColor(__instance);
                return false;
            }
        }

        [HarmonyPatch(typeof(scrLogoText), "LateUpdate")]
        private static class LogoLateUpdatePatch
        {
            private static bool Prefix()
            {
                return !ShouldChangeBall;
            }
        }

        [HarmonyPatch(typeof(scrFloor), "Start")]
        private static class FloorStartPatch
        {
            private static void Postfix(scrFloor __instance)
            {
                InvalidateFloorCache();
                if (ShouldChangeTile) ApplyTileColor(__instance);
            }
        }

        [HarmonyPatch(typeof(scrFloor), "SetTileColor")]
        private static class FloorSetTileColorPatch
        {
            private static bool Prefix(scrFloor __instance)
            {
                return !ShouldChangeTile || __instance.tag != "Beat";
            }
        }
    }
}
