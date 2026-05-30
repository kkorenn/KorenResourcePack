using ADOFAI;
using ADOFAI.Editor.Actions;
using HarmonyLib;

namespace KorenResourcePack
{
    internal static class EffectRemoverPatches
    {
        [HarmonyPatch(typeof(LevelData), "Decode")]
        private static class LevelDataDecodePatch
        {
            private static void Postfix(LevelData __instance) => EffectRemover.OnLevelDataDecode(__instance);
        }

        [HarmonyPatch(typeof(SaveLevelEditorAction), "Execute")]
        private static class SaveLevelEditorActionPatch
        {
            private static bool Prefix() => EffectRemover.OnSaveLevelEditorActionPrefix();
        }

        [HarmonyPatch(typeof(scnEditor), "LoadGameScene")]
        private static class EditorLoadGameScenePatch
        {
            private static void Postfix(scnEditor __instance) => EffectRemover.OnEditorLoadGameScenePostfix(__instance);
        }
    }
}
