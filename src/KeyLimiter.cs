using HarmonyLib;
using MonsterLove.StateMachine;
using SkyHook;
using System.Collections.Generic;
using UnityEngine;

namespace KorenResourcePack
{
    // KeyboardChatterBlocker's key limiter filters only while the controller is in
    // PlayerControl. Normal key filtering happens in ChatterBlocker's CountValidKeysPressed
    // replacement; async key filtering happens in the SkyHook callback.
    internal static class KeyLimiter
    {
        private static readonly HashSet<int> cachedAllowedKeys = new HashSet<int>();
        private static int[] cachedAllowedSource;
        private static int cachedAllowedLength = -1;
        private static int cachedPlayerControlFrame = -1;
        private static bool cachedPlayerControl;

        // ADOFAI ships with a built-in key limiter (Persistence.keyLimiterKeys) that runs
        // inside RDInputType_Keyboard/RDInputType_AsyncKeyboard before RDInput.GetMainPressKeys
        // returns. Any key not in the player's saved limiter set is dropped upstream, so the
        // mod never sees presses for keys like '=' or '-' even if they're in our allowlist.
        // When the mod's KeyLimiter is active we force the game's gate off so the mod's
        // allowlist becomes the sole filter (additive instead of subtractive).
        [HarmonyPatch(typeof(RDInput), "get_useKeyLimiter")]
        private static class RDInputUseKeyLimiterPatch
        {
            private static void Postfix(ref bool __result)
            {
                if (__result && Main.modEnabled && Main.settings != null && Main.settings.KeyLimiterOn)
                    __result = false;
            }
        }

        internal static bool IsActive()
        {
            return Main.modEnabled && Main.settings != null && Main.settings.KeyLimiterOn && !SettingsGui.keyLimiterCapturing;
        }

        internal static bool InPlayerControl()
        {
            int frame = Time.frameCount;
            if (cachedPlayerControlFrame == frame)
                return cachedPlayerControl;

            cachedPlayerControlFrame = frame;
            cachedPlayerControl = false;
            try
            {
                scrController controller = scrController.instance;
                if (controller == null) return false;
                if (controller.paused || !controller.gameworld) return false;
                cachedPlayerControl = ((StateBehaviour)controller).stateMachine.GetState() is States state
                                      && state == States.PlayerControl;
                return cachedPlayerControl;
            }
            catch { return false; }
        }

        internal static bool IsAllowedKey(KeyCode key)
        {
            int[] allowed = Main.settings != null ? Main.settings.KeyLimiterAllowed : null;
            if (allowed == null) return false;

            if (!ReferenceEquals(allowed, cachedAllowedSource) || allowed.Length != cachedAllowedLength)
            {
                cachedAllowedKeys.Clear();
                for (int i = 0; i < allowed.Length; i++)
                    cachedAllowedKeys.Add(allowed[i]);

                cachedAllowedSource = allowed;
                cachedAllowedLength = allowed.Length;
            }

            return cachedAllowedKeys.Contains((int)key);
        }

        internal static bool ShouldBlockKey(KeyCode key)
        {
            return IsActive() && InPlayerControl() && !IsAllowedKey(key);
        }

        internal static bool ShouldBlockAsyncKey(ushort key, KeyLabel label)
        {
            if (!IsActive() || !InPlayerControl()) return false;
            KeyCode unityKey = AsyncKeyMapper.AsyncKeyToUnityKey(label);
            return unityKey == KeyCode.None || !IsAllowedKey(unityKey);
        }
    }
}
