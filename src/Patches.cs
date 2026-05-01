using HarmonyLib;
using MonsterLove.StateMachine;
using System;

namespace KorenResourcePack
{
    public static partial class Main
    {
        [HarmonyPatch(typeof(scnGame), "Play")]
        private static class ScnGamePlayPatch
        {
            private static void Postfix()
            {
                ResetRunData("scnGame.Play");
                OnRunShow();
                SetRunVisible(true, "scnGame.Play");
            }
        }

        [HarmonyPatch(typeof(scrPressToStart), "ShowText")]
        private static class PressToStartShowTextPatch
        {
            private static void Postfix()
            {
                OnRunShow();
                SetRunVisible(true, "scrPressToStart.ShowText");
            }
        }

        [HarmonyPatch(typeof(scnEditor), "ResetScene")]
        private static class EditorResetScenePatch
        {
            private static void Postfix()
            {
                ResetRunData("scnEditor.ResetScene");
                SetRunVisible(true, "scnEditor.ResetScene");
            }
        }

        [HarmonyPatch(typeof(scrController), "StartLoadingScene")]
        private static class ControllerStartLoadingScenePatch
        {
            private static void Postfix()
            {
                OnRunHide();
                ResetRunData("scrController.StartLoadingScene");
                SetRunVisible(false, "scrController.StartLoadingScene");
            }
        }

        [HarmonyPatch(typeof(scrMistakesManager), "AddHit")]
        private static class MistakesManagerAddHitPatch
        {
            private static void Postfix(HitMargin hit)
            {
                RegisterComboHit(hit);
                RegisterJudgementHit(hit);
            }
        }

        [HarmonyPatch(typeof(scrPlanet), "MoveToNextFloor")]
        private static class PlanetMoveToNextFloorPatch
        {
            private static void Postfix()
            {
                UpdateTimingScale();
            }
        }

        [HarmonyPatch(typeof(StateBehaviour), "ChangeState", new[] { typeof(Enum) })]
        private static class StateChangeDeathPatch
        {
            private static void Postfix(Enum newState)
            {
                try
                {
                    if ((States)newState == States.Fail2) OnRunDeath();
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(scrUIController), "WipeToBlack")]
        private static class WipeToBlackPatch
        {
            private static void Postfix()
            {
                OnRunHide();
            }
        }
    }
}
