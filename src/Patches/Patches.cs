using HarmonyLib;
using MonsterLove.StateMachine;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KorenResourcePack
{
    
    internal static partial class GamePatches
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
                if (!GCS.practiceMode && scnGame.instance != null) return;
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

        private static void RegisterHit(HitMargin hit)
        {
            Combo.RegisterComboHit(hit);
            Judgement.RegisterJudgementHit(hit);
        }

        private static int ClampAudioBuffer(int value)
        {
            return Math.Max(value, (int)KorenResourcePack.Audio.Fmod.MinBufferSize);
        }

        private static bool audioBufferFloorPending;
        private static int audioBufferFloorFrame;
        private const float MacAudioResetStartupDelaySeconds = 5f;

        internal static void RequestAudioBufferFloorEnforcement()
        {
            int rawSaved = PlayerPrefs.GetInt("audioBufferSize", Persistence.audioBufferSize);
            int clamped = ClampAudioBuffer(rawSaved);
            if (rawSaved != clamped)
                Persistence.audioBufferSize = clamped;

            audioBufferFloorPending = true;
            audioBufferFloorFrame = Time.frameCount + 3;
        }

        internal static void TickAudioBufferFloorEnforcement()
        {
            if (!audioBufferFloorPending) return;
            if (Time.frameCount < audioBufferFloorFrame) return;
            if (!IsGraphicsDeviceReady()) return;
            if (Application.platform == RuntimePlatform.OSXPlayer &&
                Time.realtimeSinceStartup < MacAudioResetStartupDelaySeconds)
                return;

            AudioConfiguration config = AudioSettings.GetConfiguration();
            int clamped = ClampAudioBuffer(config.dspBufferSize);
            if (config.dspBufferSize >= clamped)
            {
                audioBufferFloorPending = false;
                return;
            }

            config.dspBufferSize = clamped;
            AudioSettings.Reset(config);
            audioBufferFloorPending = false;
        }

        private static bool IsGraphicsDeviceReady()
        {
            return SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
        }

        private static bool IsAudioBufferSetting(PauseSettingButton setting)
        {
            return setting != null && setting.name == "audioBufferSize";
        }

        [HarmonyPatch(typeof(Persistence), "get_audioBufferSize")]
        private static class PersistenceAudioBufferGetPatch
        {
            private static void Postfix(ref int __result)
            {
                __result = ClampAudioBuffer(__result);
            }
        }

        [HarmonyPatch(typeof(Persistence), "set_audioBufferSize")]
        private static class PersistenceAudioBufferSetPatch
        {
            private static void Prefix(ref int value)
            {
                value = ClampAudioBuffer(value);
            }
        }

        [HarmonyPatch(typeof(AudioSettings), "Reset")]
        private static class AudioSettingsResetPatch
        {
            [HarmonyPriority(Priority.First)]
            private static void Prefix(ref AudioConfiguration config)
            {
                config.dspBufferSize = ClampAudioBuffer(config.dspBufferSize);
            }
        }

        [HarmonyPatch(typeof(scnLevelSelect), "CheckAudioBreak")]
        private static class LevelSelectCheckAudioBreakPatch
        {
            private static bool Prefix()
            {
                return Main.settings == null || !Main.settings.FmodEnabled;
            }
        }

        [HarmonyPatch(typeof(SettingsMenu), "GenerateSettings")]
        private static class SettingsMenuGenerateSettingsPatch
        {
            private static void Postfix(SettingsMenu __instance)
            {
                var settingsTabs = Traverse.Create(__instance)
                    .Field("settingsTabs")
                    .GetValue<List<List<PauseSettingButton>>>();
                if (settingsTabs == null) return;

                foreach (var tab in settingsTabs)
                {
                    if (tab == null) continue;
                    foreach (var setting in tab)
                    {
                        if (!IsAudioBufferSetting(setting)) continue;
                        setting.minInt = ClampAudioBuffer(setting.minInt);
                        if (setting.maxInt < setting.minInt)
                            setting.maxInt = setting.minInt;
                        setting.hasRange = setting.minInt != setting.maxInt;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SettingsMenu), "UpdateSetting")]
        private static class SettingsMenuUpdateSettingPatch
        {
            private static void Prefix(PauseSettingButton setting)
            {
                if (!IsAudioBufferSetting(setting)) return;
                setting.minInt = ClampAudioBuffer(setting.minInt);
                if (setting.maxInt < setting.minInt)
                    setting.maxInt = setting.minInt;
            }
        }

#if !LEGACY
        [HarmonyPatch(typeof(scrMarginTracker), "AddHit")]
        private static class MistakesManagerAddHitPatch
        {
            private static void Postfix(HitMargin hit)
            {
                RegisterHit(hit);
            }
        }
#endif

        [HarmonyPatch(typeof(scrPlanet), "MoveToNextFloor")]
        private static class PlanetMoveToNextFloorPatch
        {
            private static void Postfix(scrFloor floor)
            {
                TimingScale.UpdateTimingScale();
            }
        }

        [HarmonyPatch(typeof(StateBehaviour), "ChangeState", new[] { typeof(Enum) })]
        private static class StateChangeDeathPatch
        {
            private static void Postfix(Enum newState)
            {
                
                if (!(newState is States state)) return;
                if (state == States.Fail2) Main.OnRunDeath();
                else if (state == States.Won) Main.OnRunClear();
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
