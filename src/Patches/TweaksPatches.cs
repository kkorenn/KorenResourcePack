using HarmonyLib;
using UnityEngine;

namespace KorenResourcePack
{
    internal static partial class Tweaks
    {
        [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
        private static class HitTextMeshShowPatch
        {
            private static void Prefix(scrHitTextMesh __instance, ref Vector3 position, ref Vector3 borderOffset, ref float scale)
            {
                if (!ShouldHideJudgementPopup(__instance)) return;

                position = HiddenJudgementPopupPosition;
                borderOffset = Vector3.zero;
                scale = 0f;
            }

            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            [HarmonyAfter(new[] { "XPerfect" })]
            private static void Postfix(scrHitTextMesh __instance)
            {
                if (!ShouldHideJudgementPopupAfterText(__instance)) return;

                HideJudgementPopupInstance(__instance);
            }
        }

        [HarmonyPatch(typeof(ffxCheckpoint), "get_runOnHit")]
        private static class CheckpointRunOnHitPatch
        {
            private static bool Prefix(ref bool __result)
            {
                if (!ShouldRemoveCheckpoints) return true;
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(ffxCheckpoint), "Awake")]
        private static class CheckpointAwakePatch
        {
            private static void Postfix(ffxCheckpoint __instance)
            {
                InvalidateCheckpointCache();
                if (ShouldRemoveCheckpoints)
                    RemoveCheckpointVisual(__instance);
            }
        }

        [HarmonyPatch(typeof(ffxCheckpoint), "Decode")]
        private static class CheckpointDecodePatch
        {
            private static void Postfix(ffxCheckpoint __instance)
            {
                InvalidateCheckpointCache();
                if (ShouldRemoveCheckpoints)
                    RemoveCheckpointVisual(__instance);
            }
        }

        [HarmonyPatch(typeof(ffxCheckpoint), "StartEffect")]
        private static class CheckpointStartEffectPatch
        {
            private static bool Prefix(ffxCheckpoint __instance)
            {
                if (!ShouldRemoveCheckpoints) return true;
                RemoveCheckpointVisual(__instance);
                return false;
            }
        }

        [HarmonyPatch(typeof(scrMistakesManager), "MarkCheckpoint")]
        private static class MistakesMarkCheckpointPatch
        {
            private static bool Prefix()
            {
                return !ShouldRemoveCheckpoints;
            }
        }

        [HarmonyPatch(typeof(scrFloor), "LightUp")]
        private static class FloorLightUpPatch
        {
            private static void Prefix(scrFloor __instance)
            {
                if (!ShouldDisableTileHitGlow || __instance == null) return;

                lightUpDepth++;
                try
                {
                    int id = __instance.GetInstanceID();
                    if (!lightUpDisableGlowStates.ContainsKey(id))
                        lightUpDisableGlowStates[id] = __instance.disableGlow;
                    __instance.disableGlow = true;
                }
                catch
                {
                }
            }

            private static void Postfix(scrFloor __instance)
            {
                if (__instance == null) return;

                if (lightUpDepth > 0)
                    lightUpDepth--;

                int id;
                try { id = __instance.GetInstanceID(); }
                catch { return; }

                try
                {
                    bool wasDisabled;
                    if (lightUpDisableGlowStates.TryGetValue(id, out wasDisabled))
                    {
                        __instance.disableGlow = wasDisabled;
                        lightUpDisableGlowStates.Remove(id);
                    }
                }
                catch
                {
                }

                if (!ShouldDisableTileHitGlow) return;

                suppressNextRandomColorFloorIds.Add(id);
                SuppressFloorHitGlow(__instance);
            }
        }

        [HarmonyPatch(typeof(scrFloor), "SetToRandomColor")]
        private static class FloorSetToRandomColorPatch
        {
            private static bool Prefix(scrFloor __instance)
            {
                if (!ShouldDisableTileHitGlow || __instance == null) return true;

                int id;
                try { id = __instance.GetInstanceID(); }
                catch { return true; }

                if (lightUpDepth <= 0 && !suppressNextRandomColorFloorIds.Remove(id))
                    return true;

                SuppressFloorHitGlow(__instance);
                return false;
            }
        }

        [HarmonyPatch(typeof(PlanetRenderer), "Awake")]
        private static class PlanetRendererAwakePatch
        {
            private static void Postfix(PlanetRenderer __instance)
            {
                InvalidateRendererCache();
                ApplyBallCoreParticlesTweak(__instance);
                ApplyStationaryTailTweak(__instance);
            }
        }

        [HarmonyPatch(typeof(PlanetRenderer), "Revive")]
        private static class PlanetRendererRevivePatch
        {
            private static void Postfix(PlanetRenderer __instance)
            {
                InvalidateRendererCache();
                ApplyBallCoreParticlesTweak(__instance);
                ApplyStationaryTailTweak(__instance);
            }
        }

        [HarmonyPatch(typeof(PlanetRenderer), "PlayParticles")]
        private static class PlanetRendererPlayParticlesPatch
        {
            private static void Postfix(PlanetRenderer __instance)
            {
                ApplyBallCoreParticlesTweak(__instance);
                ApplyStationaryTailTweak(__instance);
            }
        }

        [HarmonyPatch(typeof(PlanetRenderer), "LateUpdate")]
        private static class PlanetRendererLateUpdatePatch
        {
            private static void Postfix(PlanetRenderer __instance)
            {
                if (ShouldRemoveBallCoreParticles)
                    ApplyBallCoreParticlesTweak(__instance);
                if (ShouldForcePlanetRingInvisible)
                    ForcePlanetRingInvisible(__instance);
            }
        }

        [HarmonyPatch(typeof(PlanetRenderer), "SetTailColor")]
        private static class PlanetRendererSetTailColorPatch
        {
            private static void Postfix(PlanetRenderer __instance)
            {
                ApplyStationaryTailTweak(__instance);
            }
        }

        [HarmonyPatch(typeof(PlanetRenderer), "SetCoreColor")]
        private static class PlanetRendererSetCoreColorPatch
        {
            private static bool Prefix(PlanetRenderer __instance)
            {
                if (!ShouldRemoveBallCoreParticles) return true;
                ApplyBallCoreParticlesTweak(__instance);
                return false;
            }
        }

        [HarmonyPatch(typeof(PlanetRenderer), "SetParticleSystemColor")]
        private static class PlanetRendererSetParticleSystemColorPatch
        {
            private static bool Prefix(PlanetRenderer __instance, ParticleSystem particleSystem)
            {
                if (!ShouldRemoveBallCoreParticles || !IsRemovedPlanetParticle(__instance, particleSystem))
                    return true;

                ApplyPlanetParticleTweak(particleSystem, false);
                return false;
            }
        }

        [HarmonyPatch(typeof(scrPlanet), "Start")]
        private static class PlanetStartPatch
        {
            private static void Postfix(scrPlanet __instance)
            {
                InvalidatePlanetCache();
                InvalidateRendererCache();
                try { ApplyBallCoreParticlesTweak(__instance.planetRenderer); } catch { }
                ApplyStationaryTailTweak(__instance);
                try { ApplyPlanetGlowTweak(__instance.planetRenderer); } catch { }
                if (ShouldForcePlanetRingInvisible)
                {
                    try { ForcePlanetRingInvisible(__instance.planetRenderer); } catch { }
                }
            }
        }

        [HarmonyPatch(typeof(scrPlanet), "LateUpdate")]
        private static class PlanetLateUpdatePatch
        {
            private static void Postfix(scrPlanet __instance)
            {
                if (!ShouldRemoveBallCoreParticles) return;
                ApplyStationaryTailTweak(__instance);
            }
        }

        [HarmonyPatch(typeof(scrController), "TogglePauseGame")]
        private static class DisableAutoPauseTogglePatch
        {
            private static bool Prefix(scrController __instance, ref bool __result)
            {
                if (!ShouldDisableAutoPause || __instance == null) return true;

                bool autoOn;
                try { autoOn = RDC.auto; }
                catch { return true; }
                if (!autoOn) return true;

                bool currentlyPaused;
                try { currentlyPaused = __instance.paused; }
                catch { return true; }
                if (currentlyPaused) return true;

                if (IsSafePauseCallSite()) return true;

                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(RDInput), "get_mouseScrollDelta")]
        private static class BlockMouseWheelScrollPatch
        {
            private static void Postfix(ref Vector2 __result)
            {
                if (ShouldBlockMouseWheelScroll)
                    __result = Vector2.zero;
            }
        }
    }
}
