using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace KorenResourcePack
{
    // KeyLimiter — port of AdofaiTweaks' "key limiter".
    //
    // Strategy mirrors AdofaiTweaks (PizzaLovers007/AdofaiTweaks): rather than filtering
    // raw Input.GetKeyDown globally (which would break menu/system keys), patch the
    // game's *own* hit-input counters:
    //
    //   RDInputType_Keyboard.Main(ButtonState)
    //   RDInputType_AsyncKeyboard.Main(ButtonState)
    //
    // These return how many bound hit-keys are currently in the requested state. We
    // override __result with the count of (ActiveKeys ∪ ALWAYS_BOUND_KEYS) matching the
    // state. Anything that doesn't go through this path (Esc, F-keys, UI handlers, etc.)
    // is untouched.
    //
    // ALWAYS_BOUND_KEYS = Mouse0..Mouse6 only, exactly matching AdofaiTweaks.
    //
    // Double-click fix:
    //   On v120+ both RDInputType_Keyboard and RDInputType_AsyncKeyboard are active.
    //   The original per-state memo let each input type "own" a different ButtonState in
    //   the same frame (e.g. Keyboard owned WentDown, Async owned IsDown), so the game's
    //   GetMain sum counted the real press twice — visible as a double-click at high speed.
    //   Fix: elect one input type as "primary" for the entire frame (first caller wins).
    //   All ButtonStates route through the primary; the secondary always returns 0.
    internal static class KeyLimiter
    {
        private static readonly KeyCode[] AlwaysBoundKeys = new[]
        {
            KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Mouse2,
            KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6,
        };

        // ── Frame-level primary election ────────────────────────────────────────────
        private static int _primaryFrame = -1;
        private static int _primaryInstanceID = 0;

        // Per-state cache for the primary instance only. Cleared on each new frame.
        private struct Memo { public int count; }
        private static readonly Dictionary<int, Memo> _memo = new Dictionary<int, Memo>();

        // ── Helpers ─────────────────────────────────────────────────────────────────
        private static bool InGameplay()
        {
            try
            {
                scrController c = scrController.instance;
                if (c == null) return false;
                if (!c.gameworld) return false;
                if (c.paused) return false;
                return true;
            }
            catch { return false; }
        }

        private static bool ButtonStatePressed(KeyCode key, ButtonState state)
        {
            switch (state)
            {
                case ButtonState.WentDown: return Input.GetKeyDown(key);
                case ButtonState.WentUp:   return Input.GetKeyUp(key);
                case ButtonState.IsDown:   return Input.GetKey(key);
                case ButtonState.IsUp:     return !Input.GetKey(key);
                default: return false;
            }
        }

        private static int CountPressed(ButtonState state)
        {
            int count = 0;

            int[] arr = Main.settings.KeyLimiterAllowed;
            if (arr != null)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    if (ButtonStatePressed((KeyCode)arr[i], state)) count++;
                }
            }

            for (int i = 0; i < AlwaysBoundKeys.Length; i++)
            {
                if (ButtonStatePressed(AlwaysBoundKeys[i], state)) count++;
            }

            return count;
        }

        // ── Core patch logic ─────────────────────────────────────────────────────────
        private static void ApplyMain(RDInputType inputInstance, ButtonState state, ref int __result)
        {
            if (!Main.modEnabled || Main.settings == null || !Main.settings.KeyLimiterOn) return;
            if (SettingsGui.keyLimiterCapturing) return;
            if (!InGameplay()) return;

            int curFrame = Time.frameCount;
            int instId = inputInstance != null
                ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(inputInstance)
                : 0;

            // Elect a primary input type for this frame — first caller wins.
            if (_primaryFrame != curFrame)
            {
                _primaryFrame = curFrame;
                _primaryInstanceID = instId;
                _memo.Clear();
            }

            // Non-primary input type: suppress completely so GetMain can't double-count.
            if (instId != _primaryInstanceID)
            {
                __result = 0;
                return;
            }

            // Primary input type: compute once per ButtonState per frame, then cache.
            int stateKey = (int)state;
            Memo entry;
            if (_memo.TryGetValue(stateKey, out entry))
            {
                __result = entry.count;
                return;
            }

            int count = CountPressed(state);
            _memo[stateKey] = new Memo { count = count };
            __result = count;
        }

        // ── Harmony patches ──────────────────────────────────────────────────────────
        [HarmonyPatch(typeof(RDInputType_Keyboard), "Main", typeof(ButtonState))]
        private static class RDInputKeyboardMainPatch
        {
            private static void Postfix(RDInputType_Keyboard __instance, ButtonState state, ref int __result)
                => ApplyMain(__instance, state, ref __result);
        }

        [HarmonyPatch(typeof(RDInputType_AsyncKeyboard), "Main", typeof(ButtonState))]
        private static class RDInputAsyncKeyboardMainPatch
        {
            private static void Postfix(RDInputType_AsyncKeyboard __instance, ButtonState state, ref int __result)
                => ApplyMain(__instance, state, ref __result);
        }
    }
}