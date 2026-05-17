using MonsterLove.StateMachine;
using SkyHook;
using UnityEngine;

namespace KorenResourcePack
{
    // KeyboardChatterBlocker's key limiter filters only while the controller is in
    // PlayerControl. Normal key filtering happens in ChatterBlocker's CountValidKeysPressed
    // replacement; async key filtering happens in the SkyHook callback.
    internal static class KeyLimiter
    {
        internal static bool IsActive()
        {
            return Main.modEnabled && Main.settings != null && Main.settings.KeyLimiterOn && !SettingsGui.keyLimiterCapturing;
        }

        internal static bool InPlayerControl()
        {
            try
            {
                scrController controller = scrController.instance;
                if (controller == null) return false;
                if (controller.paused || !controller.gameworld) return false;
                return ((StateBehaviour)controller).stateMachine.GetState() is States state
                       && state == States.PlayerControl;
            }
            catch { return false; }
        }

        internal static bool IsAllowedKey(KeyCode key)
        {
            int[] allowed = Main.settings != null ? Main.settings.KeyLimiterAllowed : null;
            if (allowed == null) return false;
            int raw = (int)key;
            for (int i = 0; i < allowed.Length; i++)
            {
                if (allowed[i] == raw) return true;
            }
            return false;
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
