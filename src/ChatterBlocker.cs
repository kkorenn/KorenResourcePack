using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace KorenResourcePack
{
    // Keyboard chatter blocker. Matches the behaviour of FreneticLLC/KeyboardChatterBlocker:
    // a key-down event is dropped if it arrives within `KCBThresholdMs` of the previous
    // *accepted* press of the same key. The clock reference is press-to-press time, not
    // release-to-press — a chattering switch fires the same key multiple times in fast
    // succession while the key is still down, so release events are unreliable for the gate.
    //
    // Patches `UnityEngine.Input.GetKeyDown(KeyCode)` so every consumer in the game observes
    // the same decision. A per-frame cache (keyed by KeyCode + frameCount) ensures multiple
    // GetKeyDown calls in one frame all observe the same outcome.
    internal static class ChatterBlocker
    {
        private struct ChatterDecision
        {
            public int frame;
            public bool blocked;
        }

        // Last *accepted* press time per key. Blocked presses do NOT update this, so a chain
        // of bouncing presses keeps comparing against the original real press until a real
        // press lands after the threshold has elapsed.
        private static readonly Dictionary<KeyCode, float> lastAcceptedDown =
            new Dictionary<KeyCode, float>();
        private static readonly Dictionary<KeyCode, ChatterDecision> frameCache =
            new Dictionary<KeyCode, ChatterDecision>();

        private static bool IsActive()
        {
            return Main.modEnabled && Main.settings != null && Main.settings.KCBOn;
        }

        private static bool ResolveBlock(KeyCode key)
        {
            int curFrame = Time.frameCount;
            ChatterDecision cached;
            if (frameCache.TryGetValue(key, out cached) && cached.frame == curFrame)
                return cached.blocked;

            float now = Time.unscaledTime;
            float thresholdSec = Mathf.Max(0f, Main.settings.KCBThresholdMs) / 1000f;
            bool blocked = false;
            float lastAccepted;
            if (thresholdSec > 0f && lastAcceptedDown.TryGetValue(key, out lastAccepted))
            {
                if (now - lastAccepted < thresholdSec) blocked = true;
            }
            // Accepted press resets the reference point; blocked presses don't, so the next
            // event is still measured against the last *real* press (not the chatter spike).
            if (!blocked) lastAcceptedDown[key] = now;
            frameCache[key] = new ChatterDecision { frame = curFrame, blocked = blocked };
            return blocked;
        }

        // Patches the typed overload directly so we don't accidentally hijack the string /
        // button-name overload (which mod managers and UMM use for their own UI bindings).
        [HarmonyPatch(typeof(Input), "GetKeyDown", typeof(KeyCode))]
        private static class InputGetKeyDownPatch
        {
            private static void Postfix(KeyCode key, ref bool __result)
            {
                if (!__result) return;
                if (!IsActive()) return;
                if (ResolveBlock(key)) __result = false;
            }
        }
    }
}
