using HarmonyLib;
using MonsterLove.StateMachine;
using System;

namespace KorenResourcePack
{
    // Game-event Harmony patches. Each Postfix forwards into the appropriate Main hook
    // (ResetRunData/OnRunShow/SetRunVisible/etc.) — kept as a thin dispatcher so the
    // patch surface is grouped in one place.
    internal static class GamePatches
    {
        [HarmonyPatch(typeof(scnGame), "Play")]
        private static class ScnGamePlayPatch
        {
            private static void Postfix()
            {
                Main.ResetRunData("scnGame.Play");
                PlayCount.OnRunShow();
                Main.SetRunVisible(true, "scnGame.Play");
            }
        }

        [HarmonyPatch(typeof(scrPressToStart), "ShowText")]
        private static class PressToStartShowTextPatch
        {
            private static void Postfix()
            {
                PlayCount.OnRunShow();
                Main.SetRunVisible(true, "scrPressToStart.ShowText");
            }
        }

        [HarmonyPatch(typeof(scnEditor), "ResetScene")]
        private static class EditorResetScenePatch
        {
            private static void Postfix()
            {
                Main.ResetRunData("scnEditor.ResetScene");
                Main.SetRunVisible(true, "scnEditor.ResetScene");
            }
        }

        [HarmonyPatch(typeof(scrController), "StartLoadingScene")]
        private static class ControllerStartLoadingScenePatch
        {
            private static void Postfix()
            {
                Main.OnRunHide();
                Main.ResetRunData("scrController.StartLoadingScene");
                Main.SetRunVisible(false, "scrController.StartLoadingScene");
            }
        }

        [HarmonyPatch(typeof(scrMistakesManager), "AddHit")]
        private static class MistakesManagerAddHitPatch
        {
            private static void Postfix(HitMargin hit)
            {
                Combo.RegisterComboHit(hit);
                Judgement.RegisterJudgementHit(hit);
            }
        }

        [HarmonyPatch(typeof(scrPlanet), "MoveToNextFloor")]
        private static class PlanetMoveToNextFloorPatch
        {
            private static void Postfix()
            {
                TimingScale.UpdateTimingScale();
            }
        }

        [HarmonyPatch(typeof(StateBehaviour), "ChangeState", new[] { typeof(Enum) })]
        private static class StateChangeDeathPatch
        {
            private static void Postfix(Enum newState)
            {
                try
                {
                    if ((States)newState == States.Fail2) Main.OnRunDeath();
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(scrUIController), "WipeToBlack")]
        private static class WipeToBlackPatch
        {
            private static void Postfix()
            {
                Main.OnRunHide();
            }
        }
    }
}
