using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace KorenResourcePack
{
    internal static partial class ResourceChanger
    {
        private static Sprite ottoOriginalSprite;
        private static bool applyingPlanetColor;
        private static Sprite[] ottoOriginalAutoSprites;
        private static Sprite[] ottoTrackedAutoSprites;
        private static SpriteState ottoOriginalSpriteState;
        private static bool hasOttoOriginalSpriteState;
        private static Button ottoSpriteStateButton;
        private static Image ottoSpriteStateImage;
        private static bool hasOttoOriginalTransform;
        private static Vector2 ottoOriginalAnchoredPosition;
        private static Vector3 ottoOriginalLocalScale;
        private static Color ottoOriginalColor;
        private static Image ottoTrackedTransformImage;
        private static bool ottoApplyStateValid;
        private static scnEditor ottoCachedEditor;
        private static Image ottoCachedImage;
        private static Sprite ottoCachedReplacement;
        private static Sprite[] ottoCachedAutoSprites;
        private static bool ottoCachedAutoState;
        private static Color ottoCachedTargetColor;
        private static Vector2 ottoCachedPosition;
        private static Vector3 ottoCachedScale;
        private static MethodInfo ottoUpdateMethod;
        private static readonly Dictionary<int, int> rendererPlanetSlots = new Dictionary<int, int>();
        private static readonly Dictionary<string, MemberInfo> rendererMemberCache = new Dictionary<string, MemberInfo>();
        private static readonly Color DefaultBeatTileColor = new Color(0.675f, 0.675f, 0.766f, 1f);
        private static readonly Color TailStartColorMultiplier = new Color(0.5f, 0.5f, 0.5f, 1f);
        private static readonly scrFloor[] EmptyFloors = new scrFloor[0];
        private static PlanetarySystem cachedPlanetSystem;
        private static int cachedPlanetSystemCount = -1;
        private static scrPlanet[] cachedSystemPlanets;
        private static scrFloor[] cachedFloors;
        private static MethodInfo setParticleSystemColorMethod;
        private static readonly object[] particleSystemColorInvokeArgs = new object[3];
        private static MethodInfo logoColorMethod;
        private static bool logoColorMethodResolved;
        private static readonly object[] logoColorInvokeArgs = new object[2];
        private const BindingFlags RendererMemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static void OverrideAutoButtonSpriteState(Image autoImage, Sprite replacement)
        {
            if (autoImage == null || replacement == null) return;
            Button btn = null;
            if (ottoSpriteStateImage == autoImage && ottoSpriteStateButton != null)
                btn = ottoSpriteStateButton;
            if (btn == null)
            {
                btn = autoImage.GetComponent<Button>();
                if (btn == null) btn = autoImage.GetComponentInParent<Button>();
                if (btn != null) ottoSpriteStateImage = autoImage;
            }
            if (btn == null) return;
            if (!hasOttoOriginalSpriteState || ottoSpriteStateButton != btn)
            {
                ottoOriginalSpriteState = btn.spriteState;
                hasOttoOriginalSpriteState = true;
                ottoSpriteStateButton = btn;
            }
            SpriteState ss = btn.spriteState;
            bool dirty = false;
            if (ss.highlightedSprite != replacement) { ss.highlightedSprite = replacement; dirty = true; }
            if (ss.pressedSprite     != replacement) { ss.pressedSprite     = replacement; dirty = true; }
            if (ss.selectedSprite    != replacement) { ss.selectedSprite    = replacement; dirty = true; }
            if (ss.disabledSprite    != replacement) { ss.disabledSprite    = replacement; dirty = true; }
            if (dirty) btn.spriteState = ss;
        }

        private static void InvalidateOttoIconCache()
        {
            ottoApplyStateValid = false;
            ottoCachedEditor = null;
            ottoCachedImage = null;
            ottoCachedReplacement = null;
            ottoCachedAutoSprites = null;
        }

        private static bool AutoSpritesNeedOverride(scnEditor editor, Sprite replacement)
        {
            if (editor == null || editor.autoSprites == null || replacement == null) return false;
            for (int i = 0; i < editor.autoSprites.Length; i++)
                if (editor.autoSprites[i] != replacement)
                    return true;
            return false;
        }

        private static bool AutoButtonSpriteStateNeedsOverride(Image autoImage, Sprite replacement)
        {
            if (autoImage == null || replacement == null) return false;
            Button btn = null;
            if (ottoSpriteStateImage == autoImage && ottoSpriteStateButton != null)
                btn = ottoSpriteStateButton;
            if (btn == null) return true;

            SpriteState ss = btn.spriteState;
            return ss.highlightedSprite != replacement ||
                   ss.pressedSprite     != replacement ||
                   ss.selectedSprite    != replacement ||
                   ss.disabledSprite    != replacement;
        }

        private static bool OttoApplyStateMatches(
            scnEditor editor,
            Image autoImage,
            Sprite replacement,
            bool autoState,
            Color targetColor,
            Vector2 targetPosition,
            Vector3 targetScale)
        {
            if (!ottoApplyStateValid) return false;
            if (ottoCachedEditor != editor ||
                ottoCachedImage != autoImage ||
                ottoCachedReplacement != replacement ||
                ottoCachedAutoSprites != editor.autoSprites ||
                ottoCachedAutoState != autoState ||
                ottoCachedTargetColor != targetColor ||
                ottoCachedPosition != targetPosition ||
                ottoCachedScale != targetScale)
                return false;

            RectTransform rt = autoImage.rectTransform;
            return autoImage.sprite == replacement &&
                   autoImage.color == targetColor &&
                   rt.anchoredPosition == targetPosition &&
                   rt.localScale == targetScale &&
                   !AutoSpritesNeedOverride(editor, replacement) &&
                   !AutoButtonSpriteStateNeedsOverride(autoImage, replacement);
        }

        private static void RememberOttoApplyState(
            scnEditor editor,
            Image autoImage,
            Sprite replacement,
            bool autoState,
            Color targetColor,
            Vector2 targetPosition,
            Vector3 targetScale)
        {
            ottoApplyStateValid = true;
            ottoCachedEditor = editor;
            ottoCachedImage = autoImage;
            ottoCachedReplacement = replacement;
            ottoCachedAutoSprites = editor != null ? editor.autoSprites : null;
            ottoCachedAutoState = autoState;
            ottoCachedTargetColor = targetColor;
            ottoCachedPosition = targetPosition;
            ottoCachedScale = targetScale;
        }

        private const float OttoScale = 0.85f;

        private const float OttoIdleDimFactor = 0.343f;

        private static Color OttoActiveColor =>
            new Color(
                Main.settings.OttoR,
                Main.settings.OttoG,
                Main.settings.OttoB,
                Main.settings.OttoA
            );

        private static Color OttoIdleColor =>
            new Color(
                Main.settings.OttoR * OttoIdleDimFactor,
                Main.settings.OttoG * OttoIdleDimFactor,
                Main.settings.OttoB * OttoIdleDimFactor,
                Main.settings.OttoA
            );

        private static Color BallColor(int slot)
        {
            float r, g, b;
            GetPlanetRgb(slot, out r, out g, out b);
            return new Color(r, g, b, GetBallOpacity(slot));
        }

        private static Color TailColor(int slot)
        {
            float r, g, b;
            if (Main.settings != null && Main.settings.SeparateTailColor)
                GetTailRgb(slot, out r, out g, out b);
            else
                GetPlanetRgb(slot, out r, out g, out b);
            return new Color(r, g, b, GetTailOpacity(slot));
        }

        private static Color RingColor(int slot)
        {
            if (Main.settings == null) return Color.white;
            return new Color(
                Main.settings.RingR,
                Main.settings.RingG,
                Main.settings.RingB,
                Main.settings.RingA
            );
        }

        private static Color LogoColor
        {
            get
            {
                float r, g, b;
                GetPlanetRgb(0, out r, out g, out b);
                return new Color(r, g, b, 1f);
            }
        }

        private static Color TileColor =>
            new Color(
                Main.settings.TileR,
                Main.settings.TileG,
                Main.settings.TileB,
                Main.settings.TileA
            );

        private static bool ShouldChangeBall =>
            Main.modEnabled &&
            Main.settings != null &&
            Main.settings.ResourceChangerOn &&
            Main.settings.ChangeBallColor;

        private static bool ShouldChangeRing =>
            Main.modEnabled &&
            Main.settings != null &&
            Main.settings.ResourceChangerOn &&
            Main.settings.ChangeRingColor;

        private static bool ShouldChangeTile =>
            Main.modEnabled &&
            Main.settings != null &&
            Main.settings.ResourceChangerOn &&
            Main.settings.ChangeTileColor;

        private static IEnumerable<MethodBase> ExistingMethods(Type type, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                MethodInfo method = AccessTools.Method(type, names[i]);
                if (method != null) yield return method;
            }
        }

        private static readonly scrPlanet[] EmptyPlanets = new scrPlanet[0];

        internal static void ClearSceneCaches()
        {
            InvalidatePlanetCache();
            InvalidateFloorCache();
            rendererPlanetSlots.Clear();
            InvalidateOttoIconCache();
        }

        private static void InvalidatePlanetCache()
        {
            cachedPlanetSystem = null;
            cachedPlanetSystemCount = -1;
            cachedSystemPlanets = null;
        }

        private static void InvalidateFloorCache()
        {
            cachedFloors = null;
        }

        private static T[] FindObjectsCompat<T>() where T : UnityEngine.Object
        {
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        }

        private static PlanetarySystem GetControllerPlanetarySystem(scrController controller)
        {
            return controller != null ? controller.planetarySystem : null;
        }

        private static PlanetarySystem GetPlanetPlanetarySystem(scrPlanet planet)
        {
            return planet != null ? planet.planetarySystem : null;
        }

        private static scrPlanet GetSystemPlanet(PlanetarySystem system, string name)
        {
            if (system == null) return null;
            switch (name)
            {
                case "planetRed": return system.planetRed;
                case "planetBlue": return system.planetBlue;
                default: return null;
            }
        }

        private static scrPlanet[] GetSystemPlanets(PlanetarySystem system)
        {
            if (system == null) return EmptyPlanets;
            int count = system.allPlanets != null ? system.allPlanets.Count : 0;
            if (cachedSystemPlanets != null &&
                cachedPlanetSystem == system &&
                cachedPlanetSystemCount == count)
                return cachedSystemPlanets;

            cachedPlanetSystem = system;
            cachedPlanetSystemCount = count;
            cachedSystemPlanets = system.allPlanets != null && count > 0
                ? system.allPlanets.ToArray()
                : EmptyPlanets;
            return cachedSystemPlanets;
        }

        private static int GetSystemPlanetIndex(PlanetarySystem system, scrPlanet planet)
        {
            if (system == null || planet == null) return -1;
            return system.allPlanets != null ? system.allPlanets.IndexOf(planet) : -1;
        }

        private static scrPlanet[] GetPlanets()
        {
            try
            {
                PlanetarySystem system = GetControllerPlanetarySystem(ADOBase.controller);
                scrPlanet[] planets = GetSystemPlanets(system);
                if (planets.Length > 0) return planets;
            }
            catch
            {
            }

            try { return FindObjectsCompat<scrPlanet>(); }
            catch { return EmptyPlanets; }
        }

        private static scrFloor[] GetFloors()
        {
            if (cachedFloors != null) return cachedFloors;
            try { cachedFloors = FindObjectsCompat<scrFloor>(); }
            catch { cachedFloors = EmptyFloors; }
            return cachedFloors ?? EmptyFloors;
        }

        private static bool IsRedPlanet(scrPlanet planet)
        {
            try
            {
                PlanetarySystem system = GetPlanetPlanetarySystem(planet);
                if (system == null)
                    system = GetControllerPlanetarySystem(ADOBase.controller);

                if (system != null)
                {
                    if (GetSystemPlanet(system, "planetRed") == planet) return true;
                    if (GetSystemPlanet(system, "planetBlue") == planet) return false;
                }
            }
            catch
            {
            }

            try { return planet != null && planet.planetIndex == 0; }
            catch { return true; }
        }

        private static int GetPlanetSlot(scrPlanet planet)
        {
            if (planet == null) return 0;

            try
            {
                PlanetarySystem system = GetPlanetPlanetarySystem(planet);
                if (system == null)
                    system = GetControllerPlanetarySystem(ADOBase.controller);

                if (system != null)
                {
                    if (GetSystemPlanet(system, "planetRed") == planet) return 0;
                    if (GetSystemPlanet(system, "planetBlue") == planet) return 1;
                    int index = GetSystemPlanetIndex(system, planet);
                    if (index >= 0) return Mathf.Clamp(index, 0, 2);
                }
            }
            catch
            {
            }

            try { return Mathf.Clamp(planet.planetIndex, 0, 2); }
            catch { return 0; }
        }

        private static int GetPlanetSlot(PlanetRenderer renderer)
        {
            if (renderer == null) return 0;

            int rendererId = renderer.GetInstanceID();
            int slot;
            if (rendererPlanetSlots.TryGetValue(rendererId, out slot))
                return Mathf.Clamp(slot, 0, 2);

            scrPlanet planet = FindPlanetForRenderer(renderer);
            if (planet == null) return 0;

            slot = GetPlanetSlot(planet);
            rendererPlanetSlots[rendererId] = slot;
            return slot;
        }

        private static scrPlanet FindPlanetForRenderer(PlanetRenderer renderer)
        {
            if (renderer == null) return null;

            scrPlanet[] planets = GetPlanets();
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

        private static void RememberPlanetRendererSlot(scrPlanet planet)
        {
            if (planet == null) return;
            try
            {
                if (planet.planetRenderer != null)
                    rendererPlanetSlots[planet.planetRenderer.GetInstanceID()] = GetPlanetSlot(planet);
            }
            catch
            {
            }
        }

        private static void GetPlanetRgb(int slot, out float r, out float g, out float b)
        {
            if (Main.settings == null)
            {
                r = g = b = 1f;
                return;
            }

            switch (Mathf.Clamp(slot, 0, 2))
            {
                case 1:
                    r = Main.settings.BallPlanet2R;
                    g = Main.settings.BallPlanet2G;
                    b = Main.settings.BallPlanet2B;
                    break;
                case 2:
                    r = Main.settings.BallPlanet3R;
                    g = Main.settings.BallPlanet3G;
                    b = Main.settings.BallPlanet3B;
                    break;
                default:
                    r = Main.settings.BallPlanet1R;
                    g = Main.settings.BallPlanet1G;
                    b = Main.settings.BallPlanet1B;
                    break;
            }
        }

        private static void GetTailRgb(int slot, out float r, out float g, out float b)
        {
            if (Main.settings == null)
            {
                r = g = b = 1f;
                return;
            }

            switch (Mathf.Clamp(slot, 0, 2))
            {
                case 1:
                    r = Main.settings.TailPlanet2R;
                    g = Main.settings.TailPlanet2G;
                    b = Main.settings.TailPlanet2B;
                    break;
                case 2:
                    r = Main.settings.TailPlanet3R;
                    g = Main.settings.TailPlanet3G;
                    b = Main.settings.TailPlanet3B;
                    break;
                default:
                    r = Main.settings.TailPlanet1R;
                    g = Main.settings.TailPlanet1G;
                    b = Main.settings.TailPlanet1B;
                    break;
            }
        }

        private static float GetBallOpacity(int slot)
        {
            if (Main.settings == null) return 1f;
            switch (Mathf.Clamp(slot, 0, 2))
            {
                case 1: return Main.settings.BallPlanet2Opacity;
                case 2: return Main.settings.BallPlanet3Opacity;
                default: return Main.settings.BallPlanet1Opacity;
            }
        }

        private static float GetTailOpacity(int slot)
        {
            if (Main.settings == null) return 1f;
            switch (Mathf.Clamp(slot, 0, 2))
            {
                case 1: return Main.settings.TailPlanet2Opacity;
                case 2: return Main.settings.TailPlanet3Opacity;
                default: return Main.settings.TailPlanet1Opacity;
            }
        }

        internal static void NormalizeBallOpacitySettings()
        {
            if (Main.settings == null) return;

            float ballOpacity = Mathf.Clamp01(Main.settings.BallOpacity);
            if (Mathf.Abs(ballOpacity - 1f) < 0.001f && Main.settings.BallA < 0.999f)
                ballOpacity = Mathf.Clamp01(Main.settings.BallA);

            if (!Main.settings.BallPlanetSettingsMigrated)
            {
                Main.settings.BallPlanet1R = Main.settings.BallR;
                Main.settings.BallPlanet1G = Main.settings.BallG;
                Main.settings.BallPlanet1B = Main.settings.BallB;
                Main.settings.BallPlanet2R = Main.settings.BallR;
                Main.settings.BallPlanet2G = Main.settings.BallG;
                Main.settings.BallPlanet2B = Main.settings.BallB;
                Main.settings.BallPlanet3R = Main.settings.BallR;
                Main.settings.BallPlanet3G = Main.settings.BallG;
                Main.settings.BallPlanet3B = Main.settings.BallB;
                Main.settings.BallPlanet1Opacity = ballOpacity;
                Main.settings.BallPlanet2Opacity = ballOpacity;
                Main.settings.BallPlanet3Opacity = ballOpacity;
                Main.settings.TailPlanet1Opacity = ballOpacity;
                Main.settings.TailPlanet2Opacity = ballOpacity;
                Main.settings.TailPlanet3Opacity = ballOpacity;
                Main.settings.BallPlanetSettingsMigrated = true;
            }

            if (!Main.settings.TailColorSettingsMigrated)
            {
                Main.settings.TailPlanet1R = Main.settings.BallPlanet1R;
                Main.settings.TailPlanet1G = Main.settings.BallPlanet1G;
                Main.settings.TailPlanet1B = Main.settings.BallPlanet1B;
                Main.settings.TailPlanet2R = Main.settings.BallPlanet2R;
                Main.settings.TailPlanet2G = Main.settings.BallPlanet2G;
                Main.settings.TailPlanet2B = Main.settings.BallPlanet2B;
                Main.settings.TailPlanet3R = Main.settings.BallPlanet3R;
                Main.settings.TailPlanet3G = Main.settings.BallPlanet3G;
                Main.settings.TailPlanet3B = Main.settings.BallPlanet3B;
                Main.settings.TailColorSettingsMigrated = true;
            }

            Main.settings.BallPlanet1R = Mathf.Clamp01(Main.settings.BallPlanet1R);
            Main.settings.BallPlanet1G = Mathf.Clamp01(Main.settings.BallPlanet1G);
            Main.settings.BallPlanet1B = Mathf.Clamp01(Main.settings.BallPlanet1B);
            Main.settings.BallPlanet2R = Mathf.Clamp01(Main.settings.BallPlanet2R);
            Main.settings.BallPlanet2G = Mathf.Clamp01(Main.settings.BallPlanet2G);
            Main.settings.BallPlanet2B = Mathf.Clamp01(Main.settings.BallPlanet2B);
            Main.settings.BallPlanet3R = Mathf.Clamp01(Main.settings.BallPlanet3R);
            Main.settings.BallPlanet3G = Mathf.Clamp01(Main.settings.BallPlanet3G);
            Main.settings.BallPlanet3B = Mathf.Clamp01(Main.settings.BallPlanet3B);
            Main.settings.BallPlanet1Opacity = Mathf.Clamp01(Main.settings.BallPlanet1Opacity);
            Main.settings.BallPlanet2Opacity = Mathf.Clamp01(Main.settings.BallPlanet2Opacity);
            Main.settings.BallPlanet3Opacity = Mathf.Clamp01(Main.settings.BallPlanet3Opacity);
            Main.settings.TailPlanet1Opacity = Mathf.Clamp01(Main.settings.TailPlanet1Opacity);
            Main.settings.TailPlanet2Opacity = Mathf.Clamp01(Main.settings.TailPlanet2Opacity);
            Main.settings.TailPlanet3Opacity = Mathf.Clamp01(Main.settings.TailPlanet3Opacity);
            Main.settings.TailPlanet1R = Mathf.Clamp01(Main.settings.TailPlanet1R);
            Main.settings.TailPlanet1G = Mathf.Clamp01(Main.settings.TailPlanet1G);
            Main.settings.TailPlanet1B = Mathf.Clamp01(Main.settings.TailPlanet1B);
            Main.settings.TailPlanet2R = Mathf.Clamp01(Main.settings.TailPlanet2R);
            Main.settings.TailPlanet2G = Mathf.Clamp01(Main.settings.TailPlanet2G);
            Main.settings.TailPlanet2B = Mathf.Clamp01(Main.settings.TailPlanet2B);
            Main.settings.TailPlanet3R = Mathf.Clamp01(Main.settings.TailPlanet3R);
            Main.settings.TailPlanet3G = Mathf.Clamp01(Main.settings.TailPlanet3G);
            Main.settings.TailPlanet3B = Mathf.Clamp01(Main.settings.TailPlanet3B);
            Main.settings.RingR = Mathf.Clamp01(Main.settings.RingR);
            Main.settings.RingG = Mathf.Clamp01(Main.settings.RingG);
            Main.settings.RingB = Mathf.Clamp01(Main.settings.RingB);
            Main.settings.RingA = Mathf.Clamp01(Main.settings.RingA);
            Main.settings.BallOpacity = ballOpacity;
            Main.settings.BallA = 1f;
        }

        private static void ApplyTileColor(scrFloor floor)
        {
            if (!ShouldChangeTile || floor == null || floor.tag != "Beat") return;
            floor.floorRenderer.color = TileColor;
        }

        internal static bool TryApplyTileColor(scrFloor floor)
        {
            if (!ShouldChangeTile || floor == null || floor.tag != "Beat") return false;
            ApplyTileColor(floor);
            return true;
        }

        private static void ApplyPlanetColor(scrPlanet planet)
        {
            if (planet == null) return;
            RememberPlanetRendererSlot(planet);
            ApplyPlanetRendererColor(planet.planetRenderer, GetPlanetSlot(planet));
        }

        private static void ApplyPlanetRendererColor(PlanetRenderer renderer)
        {
            ApplyPlanetRendererColor(renderer, GetPlanetSlot(renderer));
        }

        private static void ApplyPlanetRendererColor(PlanetRenderer renderer, int slot)
        {
            if (renderer == null || Main.settings == null) return;
            if (!ShouldChangeBall && !ShouldChangeRing) return;
            if (applyingPlanetColor) return;

            applyingPlanetColor = true;
            try
            {
                NormalizeBallOpacitySettings();
                slot = Mathf.Clamp(slot, 0, 2);
                Color ballColor = BallColor(slot);
                Color tailColor = TailColor(slot);
                Color ringColor = RingColor(slot);
                if (ShouldChangeBall)
                {
                    try { renderer.DisableAllSpecialPlanets(); } catch { }
                    try
                    {
                        if (renderer.sprite != null && ADOBase.gc != null && ADOBase.gc.tex_planetWhite != null)
                            renderer.sprite.sprite = ADOBase.gc.tex_planetWhite;
                    }
                    catch
                    {
                    }

                    try { renderer.SetPlanetColor(ballColor); } catch { }
                    try { renderer.SetTailColor(tailColor); } catch { }
                    ApplyTailParticleColor(renderer, tailColor);
                    try { renderer.SetCoreColor(ballColor); } catch { }
                    InvokeRendererColor(renderer, "SetFaceColor", ballColor);
                }
                if (ShouldChangeRing)
                {
                    InvokeRendererColor(renderer, "SetRingColor", ringColor);
                    ApplyRingRendererColor(renderer, ringColor);
                }
            }
            finally
            {
                applyingPlanetColor = false;
            }
        }

        private static void ApplyRingRendererColor(PlanetRenderer renderer, Color ringColor)
        {
            if (renderer == null) return;

            object ringComp;
            if (TryGetMemberValue(renderer, "ringComp", out ringComp) && ringComp != null)
                TrySetMemberValue(ringComp, "color", ringColor);

            object ringObj;
            LineRenderer ring = TryGetMemberValue(renderer, "ring", out ringObj) ? ringObj as LineRenderer : null;
            if (ring != null)
            {
                try
                {
                    ring.startColor = ringColor;
                    ring.endColor = ringColor;
                }
                catch
                {
                }
            }
        }

        private static ParticleSystem GetTailParticles(PlanetRenderer renderer)
        {
            object particles;
            return TryGetMemberValue(renderer, "tailParticles", out particles)
                ? particles as ParticleSystem
                : null;
        }

        private static ParticleSystem GetTailParticlesCoop(PlanetRenderer renderer)
        {
            object particles;
            return TryGetMemberValue(renderer, "tailParticlesCoop", out particles)
                ? particles as ParticleSystem
                : null;
        }

        private static bool TryGetMemberValue(object target, string name, out object value)
        {
            value = null;
            if (target == null || string.IsNullOrEmpty(name)) return false;

            MemberInfo member = GetMember(target.GetType(), name);
            if (member == null) return false;

            try
            {
                FieldInfo field = member as FieldInfo;
                if (field != null)
                {
                    value = field.GetValue(target);
                    return true;
                }

                PropertyInfo property = member as PropertyInfo;
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    value = property.GetValue(target, null);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static void TrySetMemberValue(object target, string name, object value)
        {
            if (target == null || string.IsNullOrEmpty(name)) return;

            MemberInfo member = GetMember(target.GetType(), name);
            if (member == null) return;

            try
            {
                FieldInfo field = member as FieldInfo;
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                PropertyInfo property = member as PropertyInfo;
                if (property != null && property.CanWrite && property.GetIndexParameters().Length == 0)
                    property.SetValue(target, value, null);
            }
            catch
            {
            }
        }

        private static MemberInfo GetMember(Type type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name)) return null;
            string key = type.FullName + "." + name;
            MemberInfo cached;
            if (rendererMemberCache.TryGetValue(key, out cached)) return cached;

            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo field = t.GetField(name, RendererMemberFlags);
                if (field != null)
                {
                    rendererMemberCache[key] = field;
                    return field;
                }

                PropertyInfo property = t.GetProperty(name, RendererMemberFlags);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    rendererMemberCache[key] = property;
                    return property;
                }
            }

            rendererMemberCache[key] = null;
            return null;
        }

        private static void ApplyTailParticleColor(PlanetRenderer renderer, Color tailColor)
        {
            if (renderer == null) return;

            Color startColor = tailColor * TailStartColorMultiplier;
            ParticleSystem tailParticles = GetTailParticles(renderer);
            ParticleSystem tailParticlesCoop = GetTailParticlesCoop(renderer);

            ApplyTailParticleSystemColor(renderer, tailParticles, tailColor, startColor);
            if (tailParticlesCoop != tailParticles)
                ApplyTailParticleSystemColor(renderer, tailParticlesCoop, tailColor, startColor);
        }

        private static void ApplyTailParticleSystemColor(PlanetRenderer renderer, ParticleSystem particles, Color baseColor, Color startColor)
        {
            if (renderer == null || particles == null) return;

            try
            {
                if (setParticleSystemColorMethod == null)
                    setParticleSystemColorMethod = AccessTools.Method(
                        typeof(PlanetRenderer),
                        "SetParticleSystemColor",
                        new[] { typeof(ParticleSystem), typeof(Color), typeof(Color) }
                    );

                if (setParticleSystemColorMethod != null)
                {
                    particleSystemColorInvokeArgs[0] = particles;
                    particleSystemColorInvokeArgs[1] = baseColor;
                    particleSystemColorInvokeArgs[2] = startColor;
                    setParticleSystemColorMethod.Invoke(renderer, particleSystemColorInvokeArgs);
                    return;
                }
            }
            catch
            {
            }

            try
            {
                ParticleSystem.MainModule main = particles.main;
                main.startColor = new ParticleSystem.MinMaxGradient(startColor);
            }
            catch
            {
            }
        }

        private static void ApplyLogoColor(scrLogoText logoText)
        {
            if (logoText == null || Main.settings == null) return;
            NormalizeBallOpacitySettings();
            Color color = LogoColor;
            InvokeLogoColor(logoText, color, true);
            InvokeLogoColor(logoText, color, false);
        }

        private static void InvokeLogoColor(scrLogoText logoText, Color color, bool isFire)
        {
            try
            {
                MethodInfo method = GetLogoColorMethod(logoText.GetType());
                if (method == null) return;

                logoColorInvokeArgs[0] = color;
                logoColorInvokeArgs[1] = isFire;
                method.Invoke(logoText, logoColorInvokeArgs);
            }
            catch
            {
            }
        }

        private static MethodInfo GetLogoColorMethod(Type type)
        {
            if (logoColorMethodResolved) return logoColorMethod;
            logoColorMethodResolved = true;

            if (type == null) return null;

            MethodInfo[] methods = type.GetMethods(RendererMemberFlags);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "ColorLogo") continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 2 || parameters[1].ParameterType != typeof(bool)) continue;

                Type colorType = parameters[0].ParameterType;
                if (colorType == typeof(Color) || Nullable.GetUnderlyingType(colorType) == typeof(Color))
                {
                    logoColorMethod = method;
                    return logoColorMethod;
                }
            }

            return null;
        }

        private static readonly Dictionary<string, MethodInfo> rendererColorMethodCache =
            new Dictionary<string, MethodInfo>();
        private static readonly object[] rendererColorInvokeArgs = new object[1];

        private static void InvokeRendererColor(PlanetRenderer renderer, string methodName, Color color)
        {
            try
            {
                MethodInfo method;
                if (!rendererColorMethodCache.TryGetValue(methodName, out method))
                {
                    method = AccessTools.Method(typeof(PlanetRenderer), methodName, new[] { typeof(Color) });
                    rendererColorMethodCache[methodName] = method;
                }
                if (method != null)
                {
                    rendererColorInvokeArgs[0] = color;
                    method.Invoke(renderer, rendererColorInvokeArgs);
                }
            }
            catch
            {
            }
        }

        internal static void RefreshChangedResources()
        {
            if (!Main.modEnabled || Main.settings == null || !Main.settings.ResourceChangerOn) return;
            if (Main.settings.ChangeOttoIcon) RefreshOttoIcon();
            if (Main.settings.ChangeBallColor || Main.settings.ChangeRingColor) RefreshPlanetColors();
            if (Main.settings.ChangeTileColor) RefreshTileColors();
        }

        internal static void RestoreChangedResources()
        {
            RestoreOttoIcon();
            if (Main.settings == null) return;
            if (Main.settings.ChangeBallColor || Main.settings.ChangeRingColor) RestorePlanetColors();
            if (Main.settings.ChangeTileColor) RestoreTileColors();
        }

        internal static void RefreshPlanetColors()
        {
            if (!ShouldChangeBall && !ShouldChangeRing) return;
            scrPlanet[] planets = GetPlanets();
            for (int i = 0; i < planets.Length; i++) ApplyPlanetColor(planets[i]);
            if (ShouldChangeBall) ApplyLogoColor(scrLogoText.instance);
        }

        internal static void RestorePlanetColors()
        {
            scrPlanet[] planets = GetPlanets();
            for (int i = 0; i < planets.Length; i++)
            {
                scrPlanet planet = planets[i];
                if (planet == null || planet.planetRenderer == null) continue;
                LoadDefaultPlanetColor(planet);
            }

            try { scrLogoText.instance?.UpdateColors(); } catch { }
            rendererPlanetSlots.Clear();
        }

        private static void LoadDefaultPlanetColor(scrPlanet planet)
        {
            bool wasApplying = applyingPlanetColor;
            applyingPlanetColor = true;
            try { planet.planetRenderer.LoadPlanetColor(IsRedPlanet(planet)); }
            catch { }
            finally { applyingPlanetColor = wasApplying; }
        }

        internal static void RefreshTileColors()
        {
            if (!ShouldChangeTile) return;
            scrFloor[] floors = GetFloors();

            for (int i = 0; i < floors.Length; i++) ApplyTileColor(floors[i]);
        }

        internal static void RestoreTileColors()
        {
            scrFloor[] floors = GetFloors();

            for (int i = 0; i < floors.Length; i++)
            {
                scrFloor floor = floors[i];
                if (floor == null || floor.gameObject == null || floor.gameObject.tag != "Beat")
                    continue;
                if (floor.floorRenderer != null)
                    floor.floorRenderer.color = DefaultBeatTileColor;
            }
        }

        private static void ApplyOttoIcon()
        {
            if (
                !Main.modEnabled ||
                Main.settings == null ||
                !Main.settings.ResourceChangerOn ||
                !Main.settings.ChangeOttoIcon
            )
                return;

            scnEditor editor = scnEditor.instance;
            if (editor == null) return;

            Image autoImage = editor.autoImage;
            if (autoImage == null) return;

            BundleLoader.EnsureBundleLoaded();
            Sprite replacement = BundleLoader.bundleAutoSprite;
            if (replacement == null) return;

            bool autoState = RDC.auto;
            Color targetColor = autoState ? OttoActiveColor : OttoIdleColor;
            RectTransform rt = autoImage.rectTransform;
            Vector2 targetPosition = new Vector2(Main.settings.OttoOffsetX, Main.settings.OttoOffsetY);
            Vector3 targetScale = Vector3.one * OttoScale;

            if (!hasOttoOriginalTransform || ottoTrackedTransformImage != autoImage)
            {
                ottoOriginalAnchoredPosition = rt.anchoredPosition;
                ottoOriginalLocalScale = rt.localScale;
                ottoOriginalColor = autoImage.color;
                ottoTrackedTransformImage = autoImage;
                hasOttoOriginalTransform = true;
            }

            if (OttoApplyStateMatches(editor, autoImage, replacement, autoState, targetColor, targetPosition, targetScale))
                return;

            if (autoImage.sprite != replacement)
                ottoOriginalSprite = autoImage.sprite;

            OverrideAutoSpriteArray(editor, replacement);
            if (autoImage.sprite != replacement)
                autoImage.sprite = replacement;
            OverrideAutoButtonSpriteState(autoImage, replacement);

            if (autoImage.color != targetColor)
                autoImage.color = targetColor;

            if (rt.anchoredPosition != targetPosition)
                rt.anchoredPosition = targetPosition;
            if (rt.localScale != targetScale)
                rt.localScale = targetScale;

            RememberOttoApplyState(editor, autoImage, replacement, autoState, targetColor, targetPosition, targetScale);
        }

        private static void OverrideAutoSpriteArray(scnEditor editor, Sprite replacement)
        {
            if (editor == null || editor.autoSprites == null || replacement == null) return;
            if (ottoTrackedAutoSprites != editor.autoSprites ||
                ottoOriginalAutoSprites == null ||
                ottoOriginalAutoSprites.Length != editor.autoSprites.Length)
            {
                ottoTrackedAutoSprites = editor.autoSprites;
                ottoOriginalAutoSprites = (Sprite[])editor.autoSprites.Clone();
            }

            for (int i = 0; i < editor.autoSprites.Length; i++)
                if (editor.autoSprites[i] != replacement)
                    editor.autoSprites[i] = replacement;
        }

        internal static void RestoreOttoIcon()
        {
            InvalidateOttoIconCache();
            try
            {
                scnEditor editor = scnEditor.instance;

                if (
                    editor == null ||
                    editor.autoImage == null
                )
                    return;

                if (ottoOriginalSprite != null)
                    editor.autoImage.sprite =
                        ottoOriginalSprite;

                if (ottoOriginalAutoSprites != null &&
                    editor.autoSprites != null &&
                    ottoTrackedAutoSprites == editor.autoSprites &&
                    editor.autoSprites.Length == ottoOriginalAutoSprites.Length)
                {
                    for (int i = 0; i < editor.autoSprites.Length; i++)
                        editor.autoSprites[i] = ottoOriginalAutoSprites[i];
                }

                Button btn = editor.autoImage.GetComponent<Button>();
                if (btn == null) btn = editor.autoImage.GetComponentInParent<Button>();
                if (btn != null && hasOttoOriginalSpriteState)
                    btn.spriteState = ottoOriginalSpriteState;

                if (hasOttoOriginalTransform && ottoTrackedTransformImage == editor.autoImage)
                {
                    RectTransform rt = editor.autoImage.rectTransform;
                    if (rt != null)
                    {
                        rt.anchoredPosition = ottoOriginalAnchoredPosition;
                        rt.localScale = ottoOriginalLocalScale;
                    }
                    editor.autoImage.color = ottoOriginalColor;
                }

                hasOttoOriginalSpriteState = false;
                ottoSpriteStateButton = null;
                ottoSpriteStateImage = null;
                hasOttoOriginalTransform = false;
                ottoTrackedTransformImage = null;
            }
            catch
            {
            }
        }

        internal static void RefreshOttoIcon()
        {
            try
            {
                scnEditor editor = scnEditor.instance;

                if (editor == null)
                    return;

                if (ottoUpdateMethod == null)
                    ottoUpdateMethod = AccessTools.Method(typeof(scnEditor), "OttoUpdate");

                if (ottoUpdateMethod == null)
                    return;

                ottoUpdateMethod.Invoke(editor, null);
            }
            catch
            {
            }
        }

    }
}
