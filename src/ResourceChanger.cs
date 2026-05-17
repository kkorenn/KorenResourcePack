using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace KorenResourcePack
{
    internal static class ResourceChanger
    {
        private static Sprite ottoOriginalSprite;
        private static bool applyingPlanetColor;
        private static Sprite[] ottoOriginalAutoSprites;
        private static Sprite[] ottoTrackedAutoSprites;
        private static SpriteState ottoOriginalSpriteState;
        private static bool hasOttoOriginalSpriteState;
        private static Button ottoSpriteStateButton;
        private static readonly Dictionary<int, int> rendererPlanetSlots = new Dictionary<int, int>();
        private static readonly Color DefaultBeatTileColor = new Color(0.675f, 0.675f, 0.766f, 1f);

        // Replaces every entry of the Button's SpriteState (highlighted/pressed/selected/disabled)
        // with our sprite so UI transitions don't briefly swap back to the vanilla auto icon
        // when the button is pressed/hovered. Color tint transitions can stay — color is also
        // overridden each frame in the Update postfix.
        private static void OverrideAutoButtonSpriteState(Image autoImage, Sprite replacement)
        {
            if (autoImage == null || replacement == null) return;
            Button btn = autoImage.GetComponent<Button>();
            if (btn == null) btn = autoImage.GetComponentInParent<Button>();
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

        // Scale multiplier
        private const float OttoScale = 0.85f;

        // Idle dim factor
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
            GetPlanetRgb(slot, out r, out g, out b);
            return new Color(r, g, b, GetTailOpacity(slot));
        }

        private static Color RingColor(int slot)
        {
            float r, g, b;
            GetPlanetRgb(slot, out r, out g, out b);
            return new Color(r, g, b, 0f);
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

        private static scrPlanet[] GetPlanets()
        {
            try
            {
                scrController controller = ADOBase.controller;
                PlanetarySystem system = controller != null ? controller.planetarySystem : null;
                if (system != null && system.allPlanets != null && system.allPlanets.Count > 0)
                    return system.allPlanets.ToArray();
            }
            catch
            {
            }

            try { return UnityEngine.Object.FindObjectsOfType<scrPlanet>(); }
            catch { return new scrPlanet[0]; }
        }

        private static bool IsRedPlanet(scrPlanet planet)
        {
            try
            {
                PlanetarySystem system = planet != null && planet.planetarySystem != null
                    ? planet.planetarySystem
                    : (ADOBase.controller != null ? ADOBase.controller.planetarySystem : null);

                if (system != null)
                {
                    if (system.planetRed == planet) return true;
                    if (system.planetBlue == planet) return false;
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
                PlanetarySystem system = planet.planetarySystem != null
                    ? planet.planetarySystem
                    : (ADOBase.controller != null ? ADOBase.controller.planetarySystem : null);

                if (system != null)
                {
                    if (system.planetRed == planet) return 0;
                    if (system.planetBlue == planet) return 1;
                    if (system.allPlanets != null)
                    {
                        int index = system.allPlanets.IndexOf(planet);
                        if (index >= 0) return Mathf.Clamp(index, 0, 2);
                    }
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
            Main.settings.BallOpacity = ballOpacity;
            Main.settings.RingOpacity = 0f;
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
            if (applyingPlanetColor) return;

            applyingPlanetColor = true;
            try
            {
                NormalizeBallOpacitySettings();
                slot = Mathf.Clamp(slot, 0, 2);
                Color ballColor = BallColor(slot);
                Color tailColor = TailColor(slot);
                Color ringColor = RingColor(slot);
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
                try { renderer.SetCoreColor(ballColor); } catch { }
                InvokeRendererColor(renderer, "SetRingColor", ringColor);
                ApplyRingRendererColor(renderer, ringColor);
                InvokeRendererColor(renderer, "SetFaceColor", ballColor);
            }
            finally
            {
                applyingPlanetColor = false;
            }
        }

        private static void ApplyRingRendererColor(PlanetRenderer renderer, Color ringColor)
        {
            if (renderer == null) return;
            try
            {
                if (renderer.ringComp != null)
                    renderer.ringComp.color = ringColor;
            }
            catch
            {
            }

            try
            {
                if (renderer.ring != null)
                {
                    renderer.ring.startColor = ringColor;
                    renderer.ring.endColor = ringColor;
                }
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
            try { logoText.ColorLogo(color, true); } catch { }
            try { logoText.ColorLogo(color, false); } catch { }
        }

        private static void InvokeRendererColor(PlanetRenderer renderer, string methodName, Color color)
        {
            try
            {
                MethodInfo method = AccessTools.Method(typeof(PlanetRenderer), methodName, new[] { typeof(Color) });
                if (method != null) method.Invoke(renderer, new object[] { color });
            }
            catch
            {
            }
        }

        internal static void RefreshChangedResources()
        {
            if (!Main.modEnabled || Main.settings == null || !Main.settings.ResourceChangerOn) return;
            if (Main.settings.ChangeOttoIcon) RefreshOttoIcon();
            if (Main.settings.ChangeBallColor) RefreshPlanetColors();
            if (Main.settings.ChangeTileColor) RefreshTileColors();
        }

        internal static void RestoreChangedResources()
        {
            RestoreOttoIcon();
            if (Main.settings == null) return;
            if (Main.settings.ChangeBallColor) RestorePlanetColors();
            if (Main.settings.ChangeTileColor) RestoreTileColors();
        }

        internal static void RefreshPlanetColors()
        {
            if (!ShouldChangeBall) return;
            scrPlanet[] planets = GetPlanets();
            for (int i = 0; i < planets.Length; i++) ApplyPlanetColor(planets[i]);
            ApplyLogoColor(scrLogoText.instance);
        }

        internal static void RestorePlanetColors()
        {
            scrPlanet[] planets = GetPlanets();
            for (int i = 0; i < planets.Length; i++)
            {
                scrPlanet planet = planets[i];
                if (planet == null || planet.planetRenderer == null) continue;
                try { planet.planetRenderer.LoadPlanetColor(IsRedPlanet(planet)); } catch { }
            }

            try { scrLogoText.instance?.UpdateColors(); } catch { }
        }

        internal static void RefreshTileColors()
        {
            if (!ShouldChangeTile) return;
            scrFloor[] floors;
            try { floors = UnityEngine.Object.FindObjectsOfType<scrFloor>(); }
            catch { return; }

            for (int i = 0; i < floors.Length; i++) ApplyTileColor(floors[i]);
        }

        internal static void RestoreTileColors()
        {
            scrFloor[] floors;
            try { floors = UnityEngine.Object.FindObjectsByType<scrFloor>(FindObjectsSortMode.None); }
            catch { return; }

            for (int i = 0; i < floors.Length; i++)
            {
                scrFloor floor = floors[i];
                if (floor.gameObject.tag != "Beat") return;
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

            if (autoImage.sprite != replacement)
                ottoOriginalSprite = autoImage.sprite;

            OverrideAutoSpriteArray(editor, replacement);
            autoImage.sprite = replacement;
            OverrideAutoButtonSpriteState(autoImage, replacement);

            autoImage.color = RDC.auto ? OttoActiveColor : OttoIdleColor;

            RectTransform rt = autoImage.rectTransform;
            rt.anchoredPosition = new Vector2(Main.settings.OttoOffsetX, Main.settings.OttoOffsetY);
            rt.localScale = Vector3.one * OttoScale;
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
                editor.autoSprites[i] = replacement;
        }

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

        internal static void RestoreOttoIcon()
        {
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

                System.Reflection.MethodInfo method =
                    AccessTools.Method(
                        typeof(scnEditor),
                        "OttoUpdate"
                    );

                if (method == null)
                    return;

                method.Invoke(editor, null);
            }
            catch
            {
            }
        }

        [HarmonyPatch(typeof(scrPlanet), "Start")]
        private static class PlanetStartPatch
        {
            private static void Postfix(scrPlanet __instance)
            {
                if (ShouldChangeBall) ApplyPlanetColor(__instance);
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

#if LEGACY
        [HarmonyPatch(typeof(scnLevelSelect), "RainbowMode")]
#else
        [HarmonyPatch(typeof(PlanetarySystem), "RainbowMode")]
#endif
        private static class LevelSelectRainbowPatch
        {
            private static bool Prefix()
            {
                return !ShouldChangeBall;
            }
        }

#if LEGACY
        [HarmonyPatch(typeof(scnLevelSelect), "EnbyMode")]
#else
        [HarmonyPatch(typeof(PlanetarySystem), "EnbyMode")]
#endif
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
