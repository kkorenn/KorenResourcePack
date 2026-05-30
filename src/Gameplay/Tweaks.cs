using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KorenResourcePack
{
    internal static partial class Tweaks
    {
        internal const int VanillaJudgementPopupCount = 12;
        internal const int XPerfectJudgementPopupBit = VanillaJudgementPopupCount;
        internal const int PlusPerfectJudgementPopupBit = VanillaJudgementPopupCount + 1;
        internal const int MinusPerfectJudgementPopupBit = VanillaJudgementPopupCount + 2;
        internal const int AllJudgementPopupMask = (1 << (VanillaJudgementPopupCount + 3)) - 1;

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

        private static bool ShouldRemovePlanetGlow =>
            Main.modEnabled &&
            Main.settings != null &&
            Main.settings.TweaksOn &&
            Main.settings.RemovePlanetGlow;

        private static bool ShouldHideJudgementPopups =>
            Main.modEnabled &&
            Main.settings != null &&
            Main.settings.TweaksOn &&
            Main.settings.HideJudgementPopups;

        private static bool ShouldDisableAutoPause =>
            Main.modEnabled &&
            Main.settings != null &&
            Main.settings.TweaksOn &&
            Main.settings.DisableAutoPause;

        private static bool ShouldForcePlanetRingInvisible =>
            Main.modEnabled &&
            Main.settings != null &&
            Main.settings.ResourceChangerOn &&
            Main.settings.ChangeBallColor;

        private static readonly Dictionary<int, bool> particleActiveStates = new Dictionary<int, bool>();
        private static readonly Dictionary<int, bool> particleRendererEnabledStates = new Dictionary<int, bool>();
        private static readonly Dictionary<int, bool> particleEmissionEnabledStates = new Dictionary<int, bool>();
        private static readonly Dictionary<int, ParticleSystem.MinMaxCurve> particleEmissionRateStates = new Dictionary<int, ParticleSystem.MinMaxCurve>();
        private static readonly Dictionary<int, int> particleMaxParticleStates = new Dictionary<int, int>();
        private static readonly Dictionary<int, bool> tailRendererEnabledStates = new Dictionary<int, bool>();
        private static readonly Dictionary<int, ParticleSystem.MinMaxGradient> tailStartColorStates = new Dictionary<int, ParticleSystem.MinMaxGradient>();
        private static readonly Dictionary<int, Renderer[]> tailRendererCache = new Dictionary<int, Renderer[]>();
        private static readonly Dictionary<int, bool> lightUpDisableGlowStates = new Dictionary<int, bool>();
        private static readonly Dictionary<int, bool> planetGlowEnabledStates = new Dictionary<int, bool>();
        private static readonly Dictionary<int, scrPlanet> rendererPlanetCache = new Dictionary<int, scrPlanet>();
        private static readonly Dictionary<string, MemberInfo> planetRendererMemberCache = new Dictionary<string, MemberInfo>();
        private static readonly HashSet<int> suppressNextRandomColorFloorIds = new HashSet<int>();
        private static readonly ffxCheckpoint[] EmptyCheckpoints = new ffxCheckpoint[0];
        private static readonly PlanetRenderer[] EmptyRenderers = new PlanetRenderer[0];
        private static readonly scrFloor[] EmptyFloors = new scrFloor[0];
        private static readonly scrPlanet[] EmptyPlanets = new scrPlanet[0];
        private static readonly Vector3 HiddenJudgementPopupPosition = new Vector3(123456f, 123456f, 123456f);
        private static ffxCheckpoint[] cachedCheckpoints;
        private static PlanetRenderer[] cachedRenderers;
        private static scrFloor[] cachedFloors;
        private static scrPlanet[] cachedPlanets;
        private static int lightUpDepth;
        private const BindingFlags PlanetRendererMemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static T[] FindObjectsCompat<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        }

        internal static void ClearSceneCaches()
        {
            InvalidateCheckpointCache();
            InvalidateRendererCache();
            InvalidateFloorCache();
            InvalidatePlanetCache();
            rendererPlanetCache.Clear();
            tailRendererCache.Clear();
            suppressNextRandomColorFloorIds.Clear();
        }

        private static void InvalidateCheckpointCache() { cachedCheckpoints = null; }
        private static void InvalidateRendererCache() { cachedRenderers = null; }
        private static void InvalidateFloorCache() { cachedFloors = null; }
        private static void InvalidatePlanetCache() { cachedPlanets = null; }

        private static ffxCheckpoint[] GetCheckpoints()
        {
            if (cachedCheckpoints != null) return cachedCheckpoints;
            try { cachedCheckpoints = FindObjectsCompat<ffxCheckpoint>(); }
            catch { cachedCheckpoints = EmptyCheckpoints; }
            return cachedCheckpoints ?? EmptyCheckpoints;
        }

        private static PlanetRenderer[] GetPlanetRenderers()
        {
            if (cachedRenderers != null) return cachedRenderers;
            try { cachedRenderers = FindObjectsCompat<PlanetRenderer>(); }
            catch { cachedRenderers = EmptyRenderers; }
            return cachedRenderers ?? EmptyRenderers;
        }

        private static scrFloor[] GetFloors()
        {
            if (cachedFloors != null) return cachedFloors;
            try { cachedFloors = FindObjectsCompat<scrFloor>(); }
            catch { cachedFloors = EmptyFloors; }
            return cachedFloors ?? EmptyFloors;
        }

        private static scrPlanet[] GetPlanets()
        {
            if (cachedPlanets != null) return cachedPlanets;
            try { cachedPlanets = FindObjectsCompat<scrPlanet>(); }
            catch { cachedPlanets = EmptyPlanets; }
            return cachedPlanets ?? EmptyPlanets;
        }

        internal static void RefreshTweaks()
        {
            RefreshCheckpointTweak();
            RefreshBallCoreParticlesTweak();
            RefreshTileHitGlowTweak();
            RefreshPlanetGlowTweak();
        }

        internal static void RestoreTweaks()
        {
            RefreshBallCoreParticlesTweak(true);
            RefreshPlanetGlowTweak(true);
            rendererPlanetCache.Clear();
            tailRendererCache.Clear();
            tailStartColorStates.Clear();
        }

        internal static void RefreshCheckpointTweak()
        {
            if (!ShouldRemoveCheckpoints)
                return;

            ffxCheckpoint[] checkpoints = GetCheckpoints();

            for (int i = 0; i < checkpoints.Length; i++)
                RemoveCheckpointVisual(checkpoints[i]);
        }

        internal static void RefreshPlanetGlowTweak()
        {
            RefreshPlanetGlowTweak(false);
        }

        private static void RefreshPlanetGlowTweak(bool forceRestore)
        {
            PlanetRenderer[] renderers = GetPlanetRenderers();

            for (int i = 0; i < renderers.Length; i++)
                ApplyPlanetGlowTweak(renderers[i], forceRestore);
        }

        private static void ApplyPlanetGlowTweak(PlanetRenderer renderer, bool forceRestore = false)
        {
            if (renderer == null) return;

            SpriteRenderer glow;
            try { glow = renderer.glow; } catch { return; }
            if (glow == null) return;

            int id = glow.GetInstanceID();
            if (ShouldRemovePlanetGlow && !forceRestore)
            {
                if (!planetGlowEnabledStates.ContainsKey(id))
                    planetGlowEnabledStates[id] = glow.enabled;
                glow.enabled = false;
            }
            else
            {
                bool wasEnabled;
                if (planetGlowEnabledStates.TryGetValue(id, out wasEnabled))
                {
                    glow.enabled = wasEnabled;
                    planetGlowEnabledStates.Remove(id);
                }
            }
        }

        private static void ForcePlanetRingInvisible(PlanetRenderer renderer)
        {
            if (!ShouldForcePlanetRingInvisible || renderer == null) return;

            LineRenderer ring = GetPlanetRing(renderer);
            if (ring == null) return;

            if (GetPlanetOnlyRing(renderer)) return;

            try
            {
                Color s = ring.startColor;
                if (s.a != 0f) { s.a = 0f; ring.startColor = s; }
                Color e = ring.endColor;
                if (e.a != 0f) { e.a = 0f; ring.endColor = e; }
            }
            catch { }
        }

        private static LineRenderer GetPlanetRing(PlanetRenderer renderer)
        {
            object value;
            return TryGetPlanetRendererMemberValue(renderer, "ring", out value) ? value as LineRenderer : null;
        }

        private static bool GetPlanetOnlyRing(PlanetRenderer renderer)
        {
            object value;
            return TryGetPlanetRendererMemberValue(renderer, "onlyRing", out value) && value is bool && (bool)value;
        }

        internal static void RefreshTileHitGlowTweak()
        {
            if (!ShouldDisableTileHitGlow) return;

            scrFloor[] floors = GetFloors();

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
            PlanetRenderer[] renderers = GetPlanetRenderers();

            for (int i = 0; i < renderers.Length; i++)
                ApplyBallCoreParticlesTweak(renderers[i], forceRestore);

            scrPlanet[] planets = GetPlanets();

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
            RememberPlanetRenderer(planet, renderer);

            bool hideTail = ShouldRemoveBallCoreParticles && !forceRestore && IsStationaryPlanet(planet);
            ApplyTailOpacityTweak(GetTailParticles(renderer), hideTail);
            ApplyTailOpacityTweak(GetTailParticlesCoop(renderer), hideTail);
        }

        private static void ApplyStationaryTailTweak(PlanetRenderer renderer, bool forceRestore = false)
        {
            
            if (!forceRestore && !ShouldRemoveBallCoreParticles) return;
            scrPlanet planet = FindPlanetForRenderer(renderer);
            if (planet != null)
                ApplyStationaryTailTweak(planet, forceRestore);
        }

        private static ParticleSystem GetCoreParticles(PlanetRenderer renderer)
        {
            return GetPlanetRendererParticle(renderer, "coreParticles");
        }

        private static ParticleSystem GetSparks(PlanetRenderer renderer)
        {
            return GetPlanetRendererParticle(renderer, "sparks");
        }

        private static ParticleSystem GetTailParticles(PlanetRenderer renderer)
        {
            return GetPlanetRendererParticle(renderer, "tailParticles");
        }

        private static ParticleSystem GetTailParticlesCoop(PlanetRenderer renderer)
        {
            return GetPlanetRendererParticle(renderer, "tailParticlesCoop");
        }

        private static ParticleSystem GetPlanetRendererParticle(PlanetRenderer renderer, string name)
        {
            object value;
            return TryGetPlanetRendererMemberValue(renderer, name, out value) ? value as ParticleSystem : null;
        }

        private static bool TryGetPlanetRendererMemberValue(PlanetRenderer renderer, string name, out object value)
        {
            value = null;
            if (renderer == null || string.IsNullOrEmpty(name)) return false;

            MemberInfo member = GetPlanetRendererMember(renderer.GetType(), name);
            if (member == null) return false;

            try
            {
                FieldInfo field = member as FieldInfo;
                if (field != null)
                {
                    value = field.GetValue(renderer);
                    return true;
                }

                PropertyInfo property = member as PropertyInfo;
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    value = property.GetValue(renderer, null);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static MemberInfo GetPlanetRendererMember(System.Type type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name)) return null;
            string key = type.FullName + "." + name;
            MemberInfo cached;
            if (planetRendererMemberCache.TryGetValue(key, out cached)) return cached;

            for (System.Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo field = t.GetField(name, PlanetRendererMemberFlags);
                if (field != null)
                {
                    planetRendererMemberCache[key] = field;
                    return field;
                }

                PropertyInfo property = t.GetProperty(name, PlanetRendererMemberFlags);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    planetRendererMemberCache[key] = property;
                    return property;
                }
            }

            planetRendererMemberCache[key] = null;
            return null;
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
            int rendererId = renderer.GetInstanceID();
            scrPlanet cached;
            if (rendererPlanetCache.TryGetValue(rendererId, out cached))
            {
                try
                {
                    if (cached != null && cached.planetRenderer == renderer)
                        return cached;
                }
                catch
                {
                }

                rendererPlanetCache.Remove(rendererId);
            }

            scrPlanet[] planets = GetPlanets();

            for (int i = 0; i < planets.Length; i++)
            {
                scrPlanet planet = planets[i];
                if (planet == null) continue;
                try
                {
                    if (planet.planetRenderer == renderer)
                    {
                        RememberPlanetRenderer(planet, renderer);
                        return planet;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static void RememberPlanetRenderer(scrPlanet planet, PlanetRenderer renderer)
        {
            if (planet == null || renderer == null) return;
            try { rendererPlanetCache[renderer.GetInstanceID()] = planet; }
            catch { }
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
                int rootId;
                try
                {
                    rootId = particleObject.GetInstanceID();
                    if (particleActiveStates.ContainsKey(rootId) && !particleObject.activeSelf)
                        return;
                }
                catch
                {
                }
                DisableParticleSystemTree(particles, particleObject);
                return;
            }

            RestoreParticleSystemTree(particleObject);
        }

        private static void ApplyTailOpacityTweak(ParticleSystem particles, bool hideTail)
        {
            if (particles == null) return;

            float opacity = hideTail
                ? (Main.settings != null ? Mathf.Clamp01(Main.settings.StationaryTailOpacity) : 0f)
                : 1f;

            try
            {
                Renderer[] renderers = GetTailRenderers(particles);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null) continue;
                    int rendererId = renderer.GetInstanceID();

                    if (opacity <= 0f)
                    {
                        if (!tailRendererEnabledStates.ContainsKey(rendererId))
                            tailRendererEnabledStates[rendererId] = renderer.enabled;
                        renderer.enabled = false;
                    }
                    else
                    {
                        bool wasEnabled;
                        if (tailRendererEnabledStates.TryGetValue(rendererId, out wasEnabled))
                        {
                            renderer.enabled = wasEnabled;
                            tailRendererEnabledStates.Remove(rendererId);
                        }
                    }
                }

                if (hideTail && opacity > 0f && opacity < 1f)
                {
                    int id = particles.GetInstanceID();
                    ParticleSystem.MainModule main = particles.main;
                    if (!tailStartColorStates.ContainsKey(id))
                        tailStartColorStates[id] = main.startColor;
                    ParticleSystem.MinMaxGradient original;
                    if (!tailStartColorStates.TryGetValue(id, out original))
                        original = main.startColor;
                    main.startColor = ScaleGradientAlpha(original, opacity);
                }
                else
                {
                    RestoreTailStartColor(particles);
                }
            }
            catch
            {
            }
        }

        private static Renderer[] GetTailRenderers(ParticleSystem particles)
        {
            if (particles == null || particles.gameObject == null) return new Renderer[0];
            int id = particles.GetInstanceID();
            Renderer[] cached;
            if (tailRendererCache.TryGetValue(id, out cached) && cached != null)
                return cached;

            cached = particles.gameObject.GetComponentsInChildren<Renderer>(true);
            tailRendererCache[id] = cached ?? new Renderer[0];
            return tailRendererCache[id];
        }

        private static ParticleSystem.MinMaxGradient ScaleGradientAlpha(ParticleSystem.MinMaxGradient source, float opacity)
        {
            opacity = Mathf.Clamp01(opacity);
            try
            {
                switch (source.mode)
                {
                    case ParticleSystemGradientMode.Color:
                    {
                        Color c = source.color;
                        c.a *= opacity;
                        return new ParticleSystem.MinMaxGradient(c);
                    }
                    case ParticleSystemGradientMode.TwoColors:
                    {
                        Color min = source.colorMin;
                        Color max = source.colorMax;
                        min.a *= opacity;
                        max.a *= opacity;
                        return new ParticleSystem.MinMaxGradient(min, max);
                    }
                    case ParticleSystemGradientMode.Gradient:
                    {
                        Gradient g = CloneGradientWithAlpha(source.gradient, opacity);
                        return g != null ? new ParticleSystem.MinMaxGradient(g) : source;
                    }
                    case ParticleSystemGradientMode.TwoGradients:
                    {
                        Gradient min = CloneGradientWithAlpha(source.gradientMin, opacity);
                        Gradient max = CloneGradientWithAlpha(source.gradientMax, opacity);
                        return min != null && max != null ? new ParticleSystem.MinMaxGradient(min, max) : source;
                    }
                    case ParticleSystemGradientMode.RandomColor:
                    {
                        Gradient g = CloneGradientWithAlpha(source.gradient, opacity);
                        return g != null ? new ParticleSystem.MinMaxGradient(g) : source;
                    }
                }
            }
            catch
            {
            }
            return source;
        }

        private static Gradient CloneGradientWithAlpha(Gradient source, float opacity)
        {
            if (source == null) return null;
            try
            {
                Gradient clone = new Gradient();
                clone.mode = source.mode;
                GradientColorKey[] colorKeys = source.colorKeys;
                GradientAlphaKey[] alphaKeys = source.alphaKeys;
                if (alphaKeys != null)
                {
                    for (int i = 0; i < alphaKeys.Length; i++)
                        alphaKeys[i].alpha *= opacity;
                }
                clone.SetKeys(colorKeys, alphaKeys);
                return clone;
            }
            catch
            {
                return null;
            }
        }

        private static void RestoreTailStartColor(ParticleSystem particles)
        {
            if (particles == null) return;
            int id;
            try { id = particles.GetInstanceID(); }
            catch { return; }

            ParticleSystem.MinMaxGradient original;
            if (!tailStartColorStates.TryGetValue(id, out original)) return;
            try
            {
                ParticleSystem.MainModule main = particles.main;
                main.startColor = original;
            }
            catch
            {
            }
            tailStartColorStates.Remove(id);
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

        private static bool IsJudgementPopupBitHidden(int bit)
        {
            return bit >= 0 &&
                bit < 31 &&
                (Main.settings.HiddenJudgementPopupMask & (1 << bit)) != 0;
        }

        private static bool ShouldHideJudgementPopup(scrHitTextMesh hitText)
        {
            if (!ShouldHideJudgementPopups || hitText == null) return false;

            HitMargin hitMargin;
            try { hitMargin = hitText.hitMargin; }
            catch { return false; }

            if (hitMargin == HitMargin.Perfect && XPerfectBridge.Active)
            {
                switch (XPerfectBridge.LastJudge())
                {
                    case XPerfectBridge.Judge.X:
                        return IsJudgementPopupBitHidden(XPerfectJudgementPopupBit);
                    case XPerfectBridge.Judge.Plus:
                        return IsJudgementPopupBitHidden(PlusPerfectJudgementPopupBit);
                    case XPerfectBridge.Judge.Minus:
                        return IsJudgementPopupBitHidden(MinusPerfectJudgementPopupBit);
                }
            }

            int bit = (int)hitMargin;
            return bit >= 0 &&
                bit < VanillaJudgementPopupCount &&
                IsJudgementPopupBitHidden(bit);
        }

        private static bool IsSafePauseCallSite()
        {
            try
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(2, false);
                for (int i = 0; i < st.FrameCount; i++)
                {
                    System.Reflection.MethodBase m = st.GetFrame(i).GetMethod();
                    if (m == null) continue;
                    System.Type dt = m.DeclaringType;
                    if (dt == null) continue;
                    string name = m.Name;
                    if (dt == typeof(scnGame) && name == "ResetScene") return true;
                    if (dt == typeof(scnEditor))
                    {
                        if (name == "SwitchToEditMode" || name == "TogglePause" ||
                            name == "ResetScene" || name == "SwitchToPlayMode" ||
                            name == "PauseIfUnpaused")
                            return true;
                    }
                    if (dt == typeof(PauseMenu)) return true;
                }
            }
            catch { }
            return false;
        }

    }
}
