using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace KorenResourcePack
{
    internal static class Tweaks
    {
        private static bool ShouldRemoveCheckpoints =>
            Main.modEnabled &&
            Main.settings != null &&
            Main.settings.TweaksOn &&
            Main.settings.RemoveAllCheckpoints;

        private static bool ShouldRemoveBallCoreParticles =>
            Main.modEnabled &&
            Main.settings != null &&
            Main.settings.TweaksOn &&
            Main.settings.RemoveBallCoreParticles;

        private static bool ShouldDisableTileHitGlow =>
            Main.modEnabled &&
            Main.settings != null &&
            Main.settings.TweaksOn &&
            Main.settings.DisableTileHitGlow;

        private static readonly Dictionary<int, bool> particleActiveStates = new Dictionary<int, bool>();
        private static readonly Dictionary<int, bool> particleRendererEnabledStates = new Dictionary<int, bool>();
        private static readonly Dictionary<int, bool> particleEmissionEnabledStates = new Dictionary<int, bool>();
        private static readonly Dictionary<int, ParticleSystem.MinMaxCurve> particleEmissionRateStates = new Dictionary<int, ParticleSystem.MinMaxCurve>();
        private static readonly Dictionary<int, int> particleMaxParticleStates = new Dictionary<int, int>();
        private static readonly Dictionary<int, bool> tailRendererEnabledStates = new Dictionary<int, bool>();
        private static readonly Dictionary<int, bool> lightUpDisableGlowStates = new Dictionary<int, bool>();
        private static readonly HashSet<int> suppressNextRandomColorFloorIds = new HashSet<int>();
        private static int lightUpDepth;

        internal static void RefreshTweaks()
        {
            RefreshCheckpointTweak();
            RefreshBallCoreParticlesTweak();
            RefreshTileHitGlowTweak();
        }

        internal static void RestoreTweaks()
        {
            RefreshBallCoreParticlesTweak(true);
        }

        internal static void RefreshCheckpointTweak()
        {
            if (Main.settings == null || !Main.settings.TweaksOn || !Main.settings.RemoveAllCheckpoints)
                return;

            ffxCheckpoint[] checkpoints;
            try { checkpoints = Object.FindObjectsOfType<ffxCheckpoint>(); }
            catch { return; }

            for (int i = 0; i < checkpoints.Length; i++)
                RemoveCheckpointVisual(checkpoints[i]);
        }

        internal static void RefreshTileHitGlowTweak()
        {
            if (!ShouldDisableTileHitGlow) return;

            scrFloor[] floors;
            try { floors = Object.FindObjectsOfType<scrFloor>(); }
            catch { return; }

            for (int i = 0; i < floors.Length; i++)
                SuppressFloorHitGlow(floors[i]);
        }

        private static void RemoveCheckpointVisual(ffxCheckpoint checkpoint)
        {
            if (checkpoint == null) return;

            scrFloor floor = null;
            try { floor = checkpoint.floor; } catch { }
            if (floor == null)
            {
                try { floor = checkpoint.GetComponent<scrFloor>(); } catch { }
            }
            if (floor == null) return;

            try
            {
                if (floor.floorIcon == FloorIcon.Checkpoint)
                {
                    floor.floorIcon = FloorIcon.None;
                    floor.UpdateIconSprite(true);
                }
            }
            catch
            {
            }
        }

        internal static void RefreshBallCoreParticlesTweak()
        {
            RefreshBallCoreParticlesTweak(false);
        }

        private static void RefreshBallCoreParticlesTweak(bool forceRestore)
        {
            PlanetRenderer[] renderers = new PlanetRenderer[0];
            try { renderers = Object.FindObjectsOfType<PlanetRenderer>(); }
            catch { }

            for (int i = 0; i < renderers.Length; i++)
                ApplyBallCoreParticlesTweak(renderers[i], forceRestore);

            scrPlanet[] planets = new scrPlanet[0];
            try { planets = Object.FindObjectsOfType<scrPlanet>(); }
            catch { }

            for (int i = 0; i < planets.Length; i++)
                ApplyStationaryTailTweak(planets[i], forceRestore);
        }

        private static void ApplyBallCoreParticlesTweak(PlanetRenderer renderer, bool forceRestore = false)
        {
            if (renderer == null) return;
            ApplyPlanetParticleTweak(GetCoreParticles(renderer), forceRestore);
            ApplyPlanetParticleTweak(GetSparks(renderer), forceRestore);
        }

        private static void ApplyStationaryTailTweak(scrPlanet planet, bool forceRestore = false)
        {
            if (planet == null) return;

            PlanetRenderer renderer = null;
            try { renderer = planet.planetRenderer; } catch { }
            if (renderer == null) return;

            bool hideTail = ShouldRemoveBallCoreParticles && !forceRestore && IsStationaryPlanet(planet);
            ApplyTailOpacityTweak(GetTailParticles(renderer), hideTail);
            ApplyTailOpacityTweak(GetTailParticlesCoop(renderer), hideTail);
        }

        private static void ApplyStationaryTailTweak(PlanetRenderer renderer, bool forceRestore = false)
        {
            scrPlanet planet = FindPlanetForRenderer(renderer);
            if (planet != null)
                ApplyStationaryTailTweak(planet, forceRestore);
        }

        private static ParticleSystem GetCoreParticles(PlanetRenderer renderer)
        {
            if (renderer == null) return null;
            try { return renderer.coreParticles; } catch { return null; }
        }

        private static ParticleSystem GetSparks(PlanetRenderer renderer)
        {
            if (renderer == null) return null;
            try { return renderer.sparks; } catch { return null; }
        }

        private static ParticleSystem GetTailParticles(PlanetRenderer renderer)
        {
            if (renderer == null) return null;
            try { return renderer.tailParticles; } catch { return null; }
        }

        private static ParticleSystem GetTailParticlesCoop(PlanetRenderer renderer)
        {
            if (renderer == null) return null;
            try { return renderer.tailParticlesCoop; } catch { return null; }
        }

        private static bool IsStationaryPlanet(scrPlanet planet)
        {
            if (planet == null) return false;
            try { return planet.planetarySystem != null && planet.planetarySystem.chosenPlanet == planet; }
            catch { return false; }
        }

        private static scrPlanet FindPlanetForRenderer(PlanetRenderer renderer)
        {
            if (renderer == null) return null;

            scrPlanet[] planets;
            try { planets = Object.FindObjectsOfType<scrPlanet>(); }
            catch { return null; }

            for (int i = 0; i < planets.Length; i++)
            {
                scrPlanet planet = planets[i];
                if (planet == null) continue;
                try
                {
                    if (planet.planetRenderer == renderer)
                        return planet;
                }
                catch
                {
                }
            }

            return null;
        }

        private static bool IsRemovedPlanetParticle(PlanetRenderer renderer, ParticleSystem particles)
        {
            if (renderer == null || particles == null) return false;
            return particles == GetCoreParticles(renderer) || particles == GetSparks(renderer);
        }

        private static void ApplyPlanetParticleTweak(ParticleSystem particles, bool forceRestore)
        {
            if (particles == null) return;
            GameObject particleObject = particles.gameObject;
            if (particleObject == null) return;

            if (ShouldRemoveBallCoreParticles && !forceRestore)
            {
                DisableParticleSystemTree(particles, particleObject);
                return;
            }

            RestoreParticleSystemTree(particleObject);
        }

        private static void ApplyTailOpacityTweak(ParticleSystem particles, bool hideTail)
        {
            if (particles == null) return;

            if (hideTail)
            {
                HideTailParticles(particles);
                return;
            }

            RestoreTailParticles(particles);
        }

        private static void HideTailParticles(ParticleSystem particles)
        {
            if (particles == null) return;

            try
            {
                Renderer[] renderers = particles.gameObject.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null) continue;
                    int rendererId = renderer.GetInstanceID();
                    if (!tailRendererEnabledStates.ContainsKey(rendererId))
                        tailRendererEnabledStates[rendererId] = renderer.enabled;
                    renderer.enabled = false;
                }
            }
            catch
            {
            }
        }

        private static void RestoreTailParticles(ParticleSystem particles)
        {
            if (particles == null) return;

            try
            {
                Renderer[] renderers = particles.gameObject.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null) continue;
                    int rendererId = renderer.GetInstanceID();
                    bool wasEnabled;
                    if (!tailRendererEnabledStates.TryGetValue(rendererId, out wasEnabled)) continue;
                    renderer.enabled = wasEnabled;
                    tailRendererEnabledStates.Remove(rendererId);
                }
            }
            catch
            {
            }
        }

        private static void SuppressFloorHitGlow(scrFloor floor)
        {
            if (floor == null) return;

            HideFloorGlowObject(floor.topGlow);
            HideFloorGlowObject(floor.bottomGlow);
            RestoreFloorHitColor(floor);
        }

        private static void HideFloorGlowObject(SpriteRenderer glow)
        {
            if (glow == null) return;
            try { glow.gameObject.SetActive(false); } catch { }
        }

        private static void RestoreFloorHitColor(scrFloor floor)
        {
            if (floor == null) return;
            if (ResourceChanger.TryApplyTileColor(floor)) return;

            try
            {
                if (floor.floorRenderer == null) return;
                Color color = floor.floorRenderer.deselectedColor;
                if (color.a <= 0.001f && floor.floorRenderer.cachedColor.a > 0.001f)
                    color = floor.floorRenderer.cachedColor;
                floor.floorRenderer.color = color;
            }
            catch
            {
            }
        }

        private static void DisableParticleSystemTree(ParticleSystem particles, GameObject particleObject)
        {
            RememberActiveState(particleObject);
            try { particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); } catch { }
            try { particles.Clear(true); } catch { }
            try
            {
                int id = particles.GetInstanceID();
                ParticleSystem.EmissionModule emission = particles.emission;
                if (!particleEmissionEnabledStates.ContainsKey(id))
                    particleEmissionEnabledStates[id] = emission.enabled;
                if (!particleEmissionRateStates.ContainsKey(id))
                    particleEmissionRateStates[id] = emission.rateOverTime;
                emission.enabled = false;
                emission.rateOverTime = 0f;
                ParticleSystem.MainModule main = particles.main;
                if (!particleMaxParticleStates.ContainsKey(id))
                    particleMaxParticleStates[id] = main.maxParticles;
                main.maxParticles = 0;
            }
            catch
            {
            }

            DisableRenderers(particleObject);

            try
            {
                ParticleSystem[] children = particleObject.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    ParticleSystem child = children[i];
                    if (child == null) continue;
                    RememberActiveState(child.gameObject);
                    try { child.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); } catch { }
                    try { child.Clear(true); } catch { }
                    try
                    {
                        int childId = child.GetInstanceID();
                        ParticleSystem.EmissionModule childEmission = child.emission;
                        if (!particleEmissionEnabledStates.ContainsKey(childId))
                            particleEmissionEnabledStates[childId] = childEmission.enabled;
                        if (!particleEmissionRateStates.ContainsKey(childId))
                            particleEmissionRateStates[childId] = childEmission.rateOverTime;
                        childEmission.enabled = false;
                        childEmission.rateOverTime = 0f;
                        ParticleSystem.MainModule childMain = child.main;
                        if (!particleMaxParticleStates.ContainsKey(childId))
                            particleMaxParticleStates[childId] = childMain.maxParticles;
                        childMain.maxParticles = 0;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }

            try { particleObject.SetActive(false); } catch { }
        }

        private static void DisableRenderers(GameObject root)
        {
            if (root == null) return;
            try
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null) continue;
                    int id = renderer.GetInstanceID();
                    if (!particleRendererEnabledStates.ContainsKey(id))
                        particleRendererEnabledStates[id] = renderer.enabled;
                    renderer.enabled = false;
                }
            }
            catch
            {
            }
        }

        private static void RestoreParticleSystemTree(GameObject particleObject)
        {
            if (particleObject == null) return;

            try
            {
                GameObject[] objects = CollectGameObjects(particleObject);
                for (int i = 0; i < objects.Length; i++)
                    RestoreActiveState(objects[i]);
            }
            catch
            {
                RestoreActiveState(particleObject);
            }

            try
            {
                ParticleSystem[] particles = particleObject.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < particles.Length; i++)
                    RestoreParticleSystemSettings(particles[i]);
            }
            catch
            {
            }

            try
            {
                Renderer[] renderers = particleObject.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null) continue;
                    int id = renderer.GetInstanceID();
                    bool wasEnabled;
                    if (!particleRendererEnabledStates.TryGetValue(id, out wasEnabled)) continue;
                    renderer.enabled = wasEnabled;
                    particleRendererEnabledStates.Remove(id);
                }
            }
            catch
            {
            }
        }

        private static void RestoreParticleSystemSettings(ParticleSystem particles)
        {
            if (particles == null) return;
            int id = particles.GetInstanceID();

            try
            {
                bool wasEmissionEnabled;
                ParticleSystem.MinMaxCurve rate;
                ParticleSystem.EmissionModule emission = particles.emission;
                if (particleEmissionEnabledStates.TryGetValue(id, out wasEmissionEnabled))
                {
                    emission.enabled = wasEmissionEnabled;
                    particleEmissionEnabledStates.Remove(id);
                }
                if (particleEmissionRateStates.TryGetValue(id, out rate))
                {
                    emission.rateOverTime = rate;
                    particleEmissionRateStates.Remove(id);
                }
            }
            catch
            {
            }

            try
            {
                int maxParticles;
                if (particleMaxParticleStates.TryGetValue(id, out maxParticles))
                {
                    ParticleSystem.MainModule main = particles.main;
                    main.maxParticles = maxParticles;
                    particleMaxParticleStates.Remove(id);
                }
            }
            catch
            {
            }
        }

        private static GameObject[] CollectGameObjects(GameObject root)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            GameObject[] objects = new GameObject[transforms.Length];
            for (int i = 0; i < transforms.Length; i++)
                objects[i] = transforms[i].gameObject;
            return objects;
        }

        private static void RememberActiveState(GameObject obj)
        {
            if (obj == null) return;
            int id = obj.GetInstanceID();
            if (!particleActiveStates.ContainsKey(id))
                particleActiveStates[id] = obj.activeSelf;
        }

        private static void RestoreActiveState(GameObject obj)
        {
            if (obj == null) return;
            int id = obj.GetInstanceID();
            bool wasActive;
            if (!particleActiveStates.TryGetValue(id, out wasActive)) return;
            try { obj.SetActive(wasActive); } catch { }
            particleActiveStates.Remove(id);
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
                if (ShouldRemoveCheckpoints)
                    RemoveCheckpointVisual(__instance);
            }
        }

        [HarmonyPatch(typeof(ffxCheckpoint), "Decode")]
        private static class CheckpointDecodePatch
        {
            private static void Postfix(ffxCheckpoint __instance)
            {
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
                ApplyBallCoreParticlesTweak(__instance);
                ApplyStationaryTailTweak(__instance);
            }
        }

        [HarmonyPatch(typeof(PlanetRenderer), "Revive")]
        private static class PlanetRendererRevivePatch
        {
            private static void Postfix(PlanetRenderer __instance)
            {
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
                ApplyBallCoreParticlesTweak(__instance);
                ApplyStationaryTailTweak(__instance);
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
                try { ApplyBallCoreParticlesTweak(__instance.planetRenderer); } catch { }
                ApplyStationaryTailTweak(__instance);
            }
        }

        [HarmonyPatch(typeof(scrPlanet), "LateUpdate")]
        private static class PlanetLateUpdatePatch
        {
            private static void Postfix(scrPlanet __instance)
            {
                ApplyStationaryTailTweak(__instance);
            }
        }
    }
}
