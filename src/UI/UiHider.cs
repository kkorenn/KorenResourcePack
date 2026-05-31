using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KorenResourcePack
{
    internal static class UiHider
    {
        private static readonly Dictionary<Graphic, bool> graphicStates = new Dictionary<Graphic, bool>();

        internal static void Tick()
        {
            if (!ShouldApply())
            {
                Restore();
                return;
            }

            Restore();

            Settings s = Main.settings;
            bool all = s.UiHideAll;
            scrUIController ui = scrUIController.instance;
            scrController controller = scrController.instance;

            if (all || s.UiHideLevelName)
            {
                HideGraphic(ui != null ? ui.txtLevelName : null);
                HideGraphic(controller != null ? controller.txtLevelName : null);
            }
            if (all || s.UiHidePercent)
            {
                HideGraphic(ui != null ? ui.txtPercent : null);
                HideGraphic(controller != null ? controller.txtPercent : null);
            }
            if (all || s.UiHidePressToStart)
                HideGraphic(ui != null ? ui.txtPressToStart : null);
            if (all || s.UiHideCountdown)
                HideGraphic(ui != null ? ui.txtCountdown : null);
            if (all || s.UiHidePauseButton)
                HideGraphicsIn(ui != null && ui.pauseButton != null ? ui.pauseButton.gameObject : null);
            if (all || s.UiHideAutoplayButton)
                HideGraphicsIn(ui != null && ui.autoplayButton != null ? ui.autoplayButton.gameObject : null);
            if (all || s.UiHideMutedIcon)
                HideGraphic(ui != null ? ui.mutedImage : null);
            if (all || s.UiHideDifficulty)
            {
                HideGraphicsIn(ui != null && ui.difficultyContainer != null ? ui.difficultyContainer.gameObject : null);
                HideGraphicsIn(ui != null && ui.leftArrow != null ? ui.leftArrow.gameObject : null);
                HideGraphicsIn(ui != null && ui.rightArrow != null ? ui.rightArrow.gameObject : null);
                HideGraphicsIn(ui != null && ui.difficultyButtonLeft != null ? ui.difficultyButtonLeft.gameObject : null);
                HideGraphicsIn(ui != null && ui.difficultyButtonRight != null ? ui.difficultyButtonRight.gameObject : null);
                HideGraphic(ui != null ? ui.difficultyImage : null);
                HideGraphic(ui != null ? ui.difficultyText : null);
            }
            if (all || s.UiHideModifiers)
            {
                HideGraphicsIn(ui != null && ui.modifiersContainer != null ? ui.modifiersContainer.gameObject : null);
                HideGraphic(ui != null ? ui.noFailImage : null);
                HideGraphic(ui != null ? ui.unlockKeyLimiterImage : null);
            }
            if (all || s.UiHideCalibration)
            {
                HideGraphic(ui != null ? ui.txtTryCalibrating : null);
                HideGraphic(controller != null ? controller.txtTryCalibrating : null);
            }
            if (all || s.UiHideDebug)
            {
                HideGraphic(ui != null ? ui.txtDebug : null);
                HideGraphic(ui != null ? ui.txtOffset : null);
            }
            if (all || s.UiHideAchievements)
            {
                HideGraphicsIn(ui != null ? ui.achievementPanel : null);
                HideGraphicsIn(ui != null ? ui.achievementBlackPanel : null);
                HideGraphicsIn(ui != null && ui.achievementSkip != null ? ui.achievementSkip.gameObject : null);
            }

            if (all)
            {
                HideGraphic(ui != null ? ui.txtCongrats : null);
                HideGraphic(ui != null ? ui.txtAprilCongrats : null);
                HideGraphic(ui != null ? ui.txtAllStrictClear : null);
                HideGraphic(controller != null ? controller.txtCongrats : null);
                HideGraphic(controller != null ? controller.txtAprilCongrats : null);
                HideGraphic(controller != null ? controller.txtAllStrictClear : null);
            }
        }

        internal static void Restore()
        {
            if (graphicStates.Count == 0) return;

            foreach (KeyValuePair<Graphic, bool> state in graphicStates)
            {
                if (state.Key != null)
                    state.Key.enabled = state.Value;
            }
            graphicStates.Clear();
        }

        private static bool ShouldApply()
        {
            if (!Main.modEnabled || Main.settings == null || !Main.settings.UiHidingOn)
                return false;

            return !Main.settings.UiHidingOnlyDuringRun || Main.runVisible;
        }

        private static void HideGraphic(Graphic graphic)
        {
            if (graphic == null) return;
            if (!graphicStates.ContainsKey(graphic))
                graphicStates[graphic] = graphic.enabled;
            graphic.enabled = false;
        }

        private static void HideGraphicsIn(GameObject gameObject)
        {
            if (gameObject == null) return;

            Graphic[] graphics = gameObject.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                HideGraphic(graphics[i]);
        }
    }
}
