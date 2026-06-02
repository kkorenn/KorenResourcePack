using HarmonyLib;
using UnityEngine;

namespace KorenResourcePack
{
    internal static class UiHiderPatches
    {
        [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
        private static class JudgmentTextShowPatch
        {
            private static void Prefix(ref Vector3 position)
            {
                if (UiHider.ShouldHideJudgementText())
                    position = UiHider.HiddenPosition;
            }
        }

        [HarmonyPatch("scrMissIndicator", "Awake")]
        private static class MissIndicatorPatch
        {
            private static void Postfix(object __instance)
            {
                if (!UiHider.ShouldHideMissIndicators()) return;

                Component component = __instance as Component;
                if (component != null)
                    component.transform.position = UiHider.HiddenPosition;
            }
        }

        [HarmonyPatch(typeof(scrShowIfDebug), "Update")]
        private static class HideAutoplayTextPatch
        {
            private static bool prevAuto;

            private static void Prefix()
            {
                prevAuto = RDC.auto;
                if (UiHider.ShouldHideOtto())
                    RDC.auto = false;
            }

            private static void Postfix()
            {
                RDC.auto = prevAuto;
            }
        }

        [HarmonyPatch(typeof(scnEditor), "SwitchToEditMode")]
        private static class HideNoFailAndTimingTargetPatch
        {
            private static void Postfix()
            {
                UiHider.ApplyNow();
            }
        }

        private static class HideResultTextAndFlashPatches
        {
            private static bool shouldIgnoreFlashOnce;

            [HarmonyPatch(typeof(scrController), "OnLandOnPortal")]
            private static class HideResultTextPatch
            {
                private static void Prefix()
                {
                    if (UiHider.ShouldHideLastFloorFlash())
                        shouldIgnoreFlashOnce = true;
                }

                private static void Postfix(scrController __instance)
                {
                    if (!UiHider.ShouldHideResult()) return;

                    SetMemberInactive(__instance, "txtCongrats");
                    SetMemberInactive(__instance, "txtResults");
                    SetMemberInactive(__instance, "txtAllStrictClear");
                }
            }

            [HarmonyPatch("scrFlash", "Flash")]
            private static class HideLastFloorFlashPatch
            {
                private static bool Prefix(object[] __args)
                {
                    Color colorStart;
                    if (!shouldIgnoreFlashOnce || !TryGetFlashColor(__args, out colorStart) || !IsLastFloorFlashColor(colorStart))
                        return true;

                    shouldIgnoreFlashOnce = false;
                    return false;
                }
            }
        }

        private static class HideHitErrorMeterPatches
        {
            private static void HideErrorMeter()
            {
                if (!UiHider.ShouldHideHitErrorMeter()) return;

                scrController controller = scrController.instance;
                if (controller == null || !controller.gameworld) return;

                GameObject errorMeter = UiHider.GetGameObject(UiHider.GetMemberValue(controller, "errorMeter"));
                if (errorMeter != null && errorMeter.activeSelf)
                    errorMeter.SetActive(false);
            }

            [HarmonyPatch(typeof(scrController), "paused", MethodType.Setter)]
            private static class ScrControllerHideHitErrorMeterPatch
            {
                private static void Postfix()
                {
                    HideErrorMeter();
                }
            }

            [HarmonyPatch(typeof(scrPlanet), "MoveToNextFloor")]
            private static class ScrPlanetHideHitErrorMeterPatch
            {
                private static void Postfix()
                {
                    HideErrorMeter();
                }
            }

            [HarmonyPatch("TaroCutsceneScript", "DisplayText")]
            private static class TaroCutsceneScriptHideHitErrorMeterPatch
            {
                private static void Postfix()
                {
                    HideErrorMeter();
                }
            }
        }

        private static void SetMemberInactive(object owner, string memberName)
        {
            GameObject gameObject = UiHider.GetGameObject(UiHider.GetMemberValue(owner, memberName));
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        private static bool IsLastFloorFlashColor(Color color)
        {
            return Mathf.Abs(color.r - 1f) < 0.001f
                && Mathf.Abs(color.g - 1f) < 0.001f
                && Mathf.Abs(color.b - 1f) < 0.001f
                && Mathf.Abs(color.a - 0.4f) < 0.001f;
        }

        private static bool TryGetFlashColor(object[] args, out Color color)
        {
            color = default(Color);
            if (args == null || args.Length == 0 || !(args[0] is Color))
                return false;

            color = (Color)args[0];
            return true;
        }
    }
}
