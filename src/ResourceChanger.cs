using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace KorenResourcePack
{
    internal static class ResourceChanger
    {
        private static Sprite ottoOriginalSprite;

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

        [HarmonyPatch(typeof(scnEditor), "OttoUpdate")]
        private static class OttoUpdatePatch
        {
            private static void Postfix()
            {
                if (
                    !Main.modEnabled ||
                    Main.settings == null ||
                    !Main.settings.ResourceChangerOn ||
                    !Main.settings.ChangeOttoIcon
                )
                    return;

                scnEditor editor = scnEditor.instance;

                if (editor == null)
                    return;

                Image autoImage = editor.autoImage;

                if (autoImage == null)
                    return;

                BundleLoader.EnsureBundleLoaded();

                if (BundleLoader.bundleAutoSprite == null)
                    return;

                // EXACT same strategy as JipperResourcePack:
                // continuously overwrite after OttoUpdate

                if (autoImage.sprite != BundleLoader.bundleAutoSprite)
                    ottoOriginalSprite = autoImage.sprite;

                autoImage.sprite = BundleLoader.bundleAutoSprite;
                OverrideAutoButtonSpriteState(autoImage, BundleLoader.bundleAutoSprite);

                autoImage.color =
                    RDC.auto
                        ? OttoActiveColor
                        : OttoIdleColor;

                RectTransform rt =
                    autoImage.rectTransform;

                rt.anchoredPosition =
                    new Vector2(
                        Main.settings.OttoOffsetX,
                        Main.settings.OttoOffsetY
                    );

                rt.localScale =
                    Vector3.one * OttoScale;
            }
        }

        [HarmonyPatch(typeof(scnEditor), "Update")]
        private static class OttoUpdateForcePatch
        {
            private static void Postfix()
            {
                if (
                    !Main.modEnabled ||
                    Main.settings == null ||
                    !Main.settings.ResourceChangerOn ||
                    !Main.settings.ChangeOttoIcon
                )
                    return;

                scnEditor editor = scnEditor.instance;

                if (editor == null)
                    return;

                Image autoImage = editor.autoImage;

                if (autoImage == null)
                    return;

                if (BundleLoader.bundleAutoSprite == null)
                    return;

                // FORCE EVERY FRAME
                // this is the important part

                autoImage.sprite = BundleLoader.bundleAutoSprite;
                OverrideAutoButtonSpriteState(autoImage, BundleLoader.bundleAutoSprite);

                autoImage.color =
                    RDC.auto
                        ? OttoActiveColor
                        : OttoIdleColor;

                RectTransform rt = autoImage.rectTransform;
                rt.anchoredPosition = new Vector2(Main.settings.OttoOffsetX, Main.settings.OttoOffsetY);
                rt.localScale = Vector3.one * OttoScale;
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
    }
}