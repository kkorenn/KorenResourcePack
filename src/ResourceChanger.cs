using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static Sprite ottoOriginalSprite;
        private static Image ottoTrackedImage;
        private static Color ottoOriginalColor;

        private static Vector2 ottoOriginalPosition;
        private static Vector3 ottoOriginalScale;

        private static bool ottoOriginalCaptured;

        // Position offset
        private const float OttoYOffset = -10f;
        private const float OttoXOffset = -5f;

        // Scale multiplier (0.85 = 85% size)
        private const float OttoScale = 0.85f;

        private const float OttoIdleDimFactor = 0.343f;

        private static Color OttoActiveColor =>
            new Color(
                settings.OttoR,
                settings.OttoG,
                settings.OttoB,
                settings.OttoA
            );

        private static Color OttoIdleColor =>
            new Color(
                settings.OttoR * OttoIdleDimFactor,
                settings.OttoG * OttoIdleDimFactor,
                settings.OttoB * OttoIdleDimFactor,
                settings.OttoA
            );

        [HarmonyPatch(typeof(scnEditor), "OttoUpdate")]
        private static class OttoUpdatePatch
        {
            private static void Postfix(scnEditor __instance)
            {
                bool featureOn =
                    modEnabled &&
                    settings != null &&
                    settings.ResourceChangerOn &&
                    settings.ChangeOttoIcon;

                if (!featureOn)
                {
                    if (ottoOriginalCaptured)
                        RestoreOttoIcon();

                    return;
                }

                Image auto = __instance != null
                    ? __instance.autoImage
                    : null;

                if (auto == null)
                    return;

                EnsureBundleLoaded();

                Sprite replacement = bundleAutoSprite;

                if (replacement == null)
                    return;

                // Capture original state once
                if (!ottoOriginalCaptured)
                {
                    ottoOriginalSprite = auto.sprite;
                    ottoOriginalColor = auto.color;
                    ottoOriginalPosition = auto.rectTransform.anchoredPosition;
                    ottoOriginalScale = auto.rectTransform.localScale;

                    ottoTrackedImage = auto;
                    ottoOriginalCaptured = true;
                }
                if (auto.sprite != replacement)
                {
                    auto.sprite = replacement;
                }
                auto.rectTransform.anchoredPosition =
                    ottoOriginalPosition + new Vector2(OttoXOffset, OttoYOffset);

                // Make smaller
                auto.rectTransform.localScale =
                    ottoOriginalScale * OttoScale;

                // Apply color
                auto.color = RDC.auto
                    ? OttoActiveColor
                    : OttoIdleColor;
            }
        }

        internal static void RestoreOttoIcon()
        {
            try
            {
                if (!ottoOriginalCaptured)
                    return;

                if (ottoTrackedImage != null)
                {
                    if (ottoOriginalSprite != null)
                        ottoTrackedImage.sprite = ottoOriginalSprite;

                    ottoTrackedImage.color = ottoOriginalColor;

                    ottoTrackedImage.rectTransform.anchoredPosition =
                        ottoOriginalPosition;

                    ottoTrackedImage.rectTransform.localScale =
                        ottoOriginalScale;
                }
            }
            catch
            {
            }
            finally
            {
                ottoOriginalCaptured = false;
                ottoOriginalSprite = null;
                ottoTrackedImage = null;
            }
        }

        internal static void RefreshOttoIcon()
        {
            try
            {
                scnEditor editor = scnEditor.instance;

                if (editor == null)
                    return;

                System.Reflection.MethodInfo m =
                    AccessTools.Method(typeof(scnEditor), "OttoUpdate");

                if (m == null)
                    return;

                m.Invoke(editor, null);
            }
            catch
            {
            }
        }
    }
}