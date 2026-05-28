using ADOFAI;
using ADOFAI.Editor.Actions;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace KorenResourcePack
{
    internal static class EffectRemover
    {
        private static readonly string[] ConditionalTagKeys =
        {
            "perfectTag",
            "hitTag",
            "earlyPerfectTag",
            "latePerfectTag",
            "barelyTag",
            "veryEarlyTag",
            "veryLateTag",
            "missTag",
            "tooEarlyTag",
            "tooLateTag",
            "lossTag"
        };

        private static bool Enabled =>
            Main.modEnabled &&
            Main.settings != null &&
            Main.settings.EffectRemoverOn;

        private static LevelEventType Event(int value)
        {
            return (LevelEventType)value;
        }

        internal static bool EditorSaveEnabled =>
            !Enabled || Main.settings.EffectRemoverEnableSave;

        internal static void RefreshEditorSaveButtons()
        {
            RefreshEditorSaveButtons(scnEditor.instance);
        }

        internal static void RefreshEditorSaveButtons(scnEditor editor)
        {
            SetEditorSaveButtons(editor, EditorSaveEnabled);
        }

        internal static void RestoreEditorSaveButtons()
        {
            SetEditorSaveButtons(scnEditor.instance, true);
        }

        private static void SetEditorSaveButtons(scnEditor editor, bool enabled)
        {
            if (editor == null) return;
            if (SceneManager.GetActiveScene().name != "scnEditor") return;

            if (editor.popupUnsavedChangesSave != null)
                editor.popupUnsavedChangesSave.interactable = enabled;
            if (editor.buttonSave != null)
                editor.buttonSave.interactable = enabled;
        }

        internal static void Remove(LevelData levelData)
        {
            if (!Enabled || levelData == null) return;

            Settings settings = Main.settings;
            List<LevelEventType> events = new List<LevelEventType>();

            if (settings.EffectRemoverDecorations)
                RemoveDecorations(events, levelData, settings);
            if (settings.EffectRemoverFilters)
                AddFilterEvents(events);
            if (settings.EffectRemoverAdvancedFilters)
                events.Add(Event(25));
            if (settings.EffectRemoverParticles)
            {
                events.Add(Event(64));
                events.Add(Event(63));
            }
            if (settings.EffectRemoverBackgrounds)
                RemoveBackgrounds(events, levelData);
            if (settings.EffectRemoverCameras)
                RemoveCameras(events, levelData, settings);
            if (settings.EffectRemoverPlanetOrbit)
                events.Add(Event(26));
            if (settings.EffectRemoverPlanetScale)
                events.Add(Event(56));
            if (settings.EffectRemoverPlanetRadius)
                events.Add(Event(52));
            if (settings.EffectRemoverRepeatEvents)
                events.Add(Event(31));
            if (settings.EffectRemoverFrameRate)
                events.Add(Event(61));
            if (settings.EffectRemoverHitSounds)
            {
                events.Add(Event(42));
                events.Add(Event(23));
            }
            if (settings.EffectRemoverHoldSounds)
                events.Add(Event(34));
            if (settings.EffectRemoverTrackAnimations)
                RemoveTrackAnimations(events, levelData, settings);
            if (settings.EffectRemoverTrackPositions)
                events.Add(Event(30));
            if (settings.EffectRemoverTrackMoves)
                events.Add(Event(18));
            if (settings.EffectRemoverTrackColors)
                RemoveTrackColors(events, levelData, settings);
            if (settings.EffectRemoverHideIcons)
                events.Add(Event(50));
            if (settings.EffectRemoverResetTrackOpacity)
                ResetTrackOpacity(levelData);

            if (events.Count == 0) return;

            HashSet<LevelEventType> eventSet = new HashSet<LevelEventType>(events);
            levelData.levelEvents.RemoveAll(data => data != null && eventSet.Contains(data.eventType));
        }

        private static void AddFilterEvents(List<LevelEventType> events)
        {
            events.Add(Event(22));
            events.Add(Event(24));
            events.Add(Event(27));
            events.Add(Event(28));
            events.Add(Event(32));
            events.Add(Event(36));
            events.Add(Event(37));
        }

        private static void RemoveBackgrounds(List<LevelEventType> events, LevelData levelData)
        {
            events.Add(Event(13));

            levelData.backgroundSettings = new LevelEvent(0, Event(7), GCS.settingsInfo["BackgroundSettings"]);
            levelData.miscSettings["bgVideo"] = "";
        }

        private static void RemoveCameras(List<LevelEventType> events, LevelData levelData, Settings settings)
        {
            events.Add(Event(12));

            if (!settings.EffectRemoverSetCameraZoom) return;

            float zoom = settings.EffectRemoverCameraZoomScale;
            if (zoom < 100f) zoom = 100f;
            if (zoom > 1000f) zoom = 1000f;
            settings.EffectRemoverCameraZoomScale = zoom;

            levelData.cameraSettings = new LevelEvent(0, Event(8), GCS.settingsInfo["CameraSettings"]);
            levelData.cameraSettings["zoom"] = zoom;
        }

        private static void RemoveDecorations(List<LevelEventType> events, LevelData levelData, Settings settings)
        {
            if (settings.EffectRemoverRemoveAllDecorations)
            {
                levelData.decorations.Clear();
                levelData.decorationSettings = new LevelEvent(0, Event(11), GCS.settingsInfo["DecorationSettings"]);

                events.Add(Event(11));
                events.Add(Event(19));
                events.Add(Event(20));
                events.Add(Event(21));
                events.Add(Event(60));
                events.Add(Event(29));
                events.Add(Event(58));
                events.Add(Event(59));
                return;
            }

            HashSet<string> conditionalEventTags = GetConditionalEventTags(levelData);
            HashSet<string> preservedDecorationTags = GetPreservedDecorationTags(levelData, conditionalEventTags);

            levelData.decorations.RemoveAll(data =>
                IsDecorationData(data) && !ShouldPreserve(data, conditionalEventTags, preservedDecorationTags));
            levelData.levelEvents.RemoveAll(data =>
                IsDecorationData(data) && !ShouldPreserve(data, conditionalEventTags, preservedDecorationTags));
        }

        private static HashSet<string> GetConditionalEventTags(LevelData levelData)
        {
            HashSet<string> tags = new HashSet<string>();

            foreach (LevelEvent eventData in levelData.levelEvents)
            {
                if (eventData == null || eventData.eventType != Event(35)) continue;

                foreach (string key in ConditionalTagKeys)
                {
                    if (!eventData.ContainsKey(key)) continue;

                    string tag = eventData.GetString(key);
                    if (!string.IsNullOrWhiteSpace(tag) && tag != "None" && tag != "없음")
                        tags.Add(tag);
                }
            }

            return tags;
        }

        private static HashSet<string> GetPreservedDecorationTags(LevelData levelData, HashSet<string> conditionalEventTags)
        {
            HashSet<string> tags = new HashSet<string>();

            foreach (LevelEvent eventData in levelData.levelEvents)
            {
                if (!IsDecorationData(eventData) || !HasAnyEventTag(eventData, conditionalEventTags)) continue;

                foreach (string tag in GetTags(eventData, "tag"))
                    tags.Add(tag);
            }

            return tags;
        }

        private static bool ShouldPreserve(LevelEvent eventData, HashSet<string> conditionalEventTags, HashSet<string> preservedDecorationTags)
        {
            return HasAnyEventTag(eventData, conditionalEventTags) || HasAnyTag(eventData, preservedDecorationTags);
        }

        private static bool IsDecorationData(LevelEvent eventData)
        {
            if (eventData == null) return false;

            LevelEventType type = eventData.eventType;
            return type == Event(11)
                || type == Event(19)
                || type == Event(20)
                || type == Event(21)
                || type == Event(60)
                || type == Event(29)
                || type == Event(58)
                || type == Event(59);
        }

        private static bool HasAnyEventTag(LevelEvent eventData, HashSet<string> tags)
        {
            foreach (string eventTag in GetTags(eventData, "eventTag"))
            {
                if (tags.Contains(eventTag)) return true;
            }

            return false;
        }

        private static bool HasAnyTag(LevelEvent eventData, HashSet<string> tags)
        {
            foreach (string tag in GetTags(eventData, "tag"))
            {
                if (tags.Contains(tag)) return true;
            }

            return false;
        }

        private static IEnumerable<string> GetTags(LevelEvent eventData, string key)
        {
            if (eventData == null || !eventData.ContainsKey(key)) yield break;

            string tags = eventData.GetString(key);
            if (string.IsNullOrWhiteSpace(tags)) yield break;

            string[] split = tags.Split(' ');
            for (int i = 0; i < split.Length; i++)
            {
                string tag = split[i];
                if (!string.IsNullOrWhiteSpace(tag)) yield return tag;
            }
        }

        private static void RemoveTrackAnimations(List<LevelEventType> events, LevelData levelData, Settings settings)
        {
            events.Add(Event(16));

            if (settings.EffectRemoverResetTrackAnimation)
            {
                levelData.trackSettings["trackAppearAnimation"] = TrackAnimationType.Fade;
                levelData.trackSettings["trackDisappearAnimation"] = TrackAnimationType.Fade;
                levelData.trackSettings["beatsAhead"] = 8.0f;
                levelData.trackSettings["beatsBehind"] = 0.0f;
            }
        }

        private static void RemoveTrackColors(List<LevelEventType> events, LevelData levelData, Settings settings)
        {
            events.Add(Event(15));
            events.Add(Event(17));

            if (settings.EffectRemoverResetTrackColor)
            {
                levelData.trackSettings["trackStyle"] = TrackStyle.Standard;
                levelData.trackSettings["trackColor"] = "debb7bff";
                levelData.trackSettings["trackColorType"] = TrackColorType.Single;
            }
        }

        private static void ResetTrackOpacity(LevelData levelData)
        {
            foreach (LevelEvent eventData in levelData.levelEvents)
            {
                if (eventData == null) continue;
                if (eventData.eventType != Event(18) && eventData.eventType != Event(30)) continue;
                if (eventData.ContainsKey("opacity"))
                    eventData["opacity"] = 100.0f;
            }
        }

        [HarmonyPatch(typeof(LevelData), "Decode")]
        private static class LevelDataDecodePatch
        {
            private static void Postfix(LevelData __instance)
            {
                Remove(__instance);
            }
        }

        [HarmonyPatch(typeof(SaveLevelEditorAction), "Execute")]
        private static class SaveLevelEditorActionPatch
        {
            private static bool Prefix()
            {
                return EditorSaveEnabled;
            }
        }

        [HarmonyPatch(typeof(scnEditor), "LoadGameScene")]
        private static class EditorLoadGameScenePatch
        {
            private static void Postfix(scnEditor __instance)
            {
                RefreshEditorSaveButtons(__instance);
            }
        }
    }
}
