using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SkyHook;
using UnityEngine;

namespace KorenResourcePack
{
    // Port of KeyboardChatterBlocker's input path:
    // - replace scrController.CountValidKeysPressed with a filtered RDInput.GetMainPressKeys count
    // - filter async keyboard events at SkyHookManager.HookCallback
    // - accept repeats only when elapsed > threshold, or elapsed <= 5 ms like the original.
    internal static class ChatterBlocker
    {
        private static readonly Dictionary<KeyCode, long> lastKeyPress = new Dictionary<KeyCode, long>();
        private static readonly Dictionary<ushort, long> lastAsyncKeyPress = new Dictionary<ushort, long>();
        private static readonly Dictionary<KeyCode, long> lastKeyViewerPress = new Dictionary<KeyCode, long>();

        private static bool HasAnyFilter()
        {
            return Main.modEnabled && Main.settings != null && (Main.settings.KCBOn || Main.settings.KeyLimiterOn);
        }

        private static bool IsActive()
        {
            return Main.modEnabled && Main.settings != null && Main.settings.KCBOn;
        }

        private static long ThresholdMs()
        {
            return Math.Max(0L, (long)Math.Round(Main.settings != null ? Main.settings.KCBThresholdMs : 0f));
        }

        private static long NowMs()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        private static bool AcceptNormalKey(KeyCode key, Dictionary<KeyCode, long> lastPressByKey, long now, long thresholdMs, bool active)
        {
            if (!active) return true;

            long last;
            if (!lastPressByKey.TryGetValue(key, out last))
            {
                lastPressByKey[key] = now;
                return true;
            }

            long elapsed = now - last;
            if (elapsed > thresholdMs || elapsed <= 5L)
            {
                lastPressByKey[key] = now;
                return true;
            }

            if (Main.mod != null)
                Main.mod.Logger.Log("Blocked Key: " + key + " time: " + elapsed + "ms.");
            return false;
        }

        internal static bool AcceptKeyViewerPress(KeyCode key)
        {
            return AcceptNormalKey(key, lastKeyViewerPress, NowMs(), ThresholdMs(), IsActive());
        }

        private static void RecordKeyStats(scrController controller, object key)
        {
            try
            {
#if LEGACY
                controller.keyFrequency[key] = controller.keyFrequency.ContainsKey(key)
                    ? controller.keyFrequency[key] + 1
                    : 0;
                controller.keyTotal++;
#else
                scrPlayer player = controller != null ? controller.playerOne : null;
                if (player == null || player.keyFrequency == null) return;
                player.keyFrequency[key] = player.keyFrequency.ContainsKey(key)
                    ? player.keyFrequency[key] + 1
                    : 0;
                player.keyTotal++;
#endif
            }
            catch { }
        }

        private static int CountValidKeysPressed()
        {
            int count = 0;
            scrController controller = scrController.instance;
            if (controller == null) return 0;
            bool chatterActive = IsActive();
            long now = chatterActive ? NowMs() : 0L;
            long threshold = chatterActive ? ThresholdMs() : 0L;

#if LEGACY
            controller.keyLimiterOverCounter = 0;
#else
            if (controller.playerOne != null) controller.playerOne.keyLimiterOverCounter = 0;
#endif

            foreach (AnyKeyCode mainPressKey in RDInput.GetMainPressKeys())
            {
                object value = mainPressKey.value;
                if (value is KeyCode)
                {
                    KeyCode key = (KeyCode)value;
                    if (KeyLimiter.ShouldBlockKey(key))
                        continue;

                    RecordKeyStats(controller, key);
                    if (AcceptNormalKey(key, lastKeyPress, now, threshold, chatterActive))
                        count++;
                }
                else if (value is AsyncKeyCode)
                {
                    AsyncKeyCode key = (AsyncKeyCode)value;
                    RecordKeyStats(controller, key);
                    count++;
                }
            }

            return count;
        }

#if LEGACY
        [HarmonyPatch(typeof(scrController), "CountValidKeysPressed")]
#else
        [HarmonyPatch(typeof(scrPlayer), "CountValidKeysPressed")]
#endif
        private static class ScrControllerCountValidKeysPressedPatch
        {
            private static bool Prefix(ref int __result)
            {
                if (!HasAnyFilter())
                {
                    return true;
                }

                __result = CountValidKeysPressed();
                return false;
            }
        }

#if LEGACY
        [HarmonyPatch(typeof(SkyHookManager), "HookCallback")]
        private static class SkyHookManagerHookCallbackPatch
        {
            private static bool Prefix(SkyHookEvent ev)
            {
#else
        // Game no longer has SkyHookManager.HookCallback. The SkyHook event is now
        // forwarded into AsyncInputManager via a compiler-generated lambda
        // (AsyncInputManager.<>c.<Setup>b__N_0(SkyHookEvent)). Resolve it by
        // signature so the patch survives lambda index changes.
        [HarmonyPatch]
        private static class SkyHookManagerHookCallbackPatch
        {
            private static MethodBase TargetMethod()
            {
                Type nested = typeof(AsyncInputManager).GetNestedType("<>c", BindingFlags.NonPublic);
                if (nested == null) return null;
                return nested.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] ps = m.GetParameters();
                        return ps.Length == 1 && ps[0].ParameterType == typeof(SkyHookEvent);
                    });
            }

            private static bool Prefix(SkyHookEvent keyEvent)
            {
                SkyHookEvent ev = keyEvent;
#endif
                scrController controller = scrController.instance;
                if (controller == null) return true;

                if (ev.Type == SkyHook.EventType.KeyReleased || ev.Key == 27)
                    return true;

                if (KeyLimiter.ShouldBlockAsyncKey(ev.Key, ev.Label))
                    return false;

                if (!IsActive())
                    return true;

                long now = NowMs();
                long threshold = ThresholdMs();
                long last;
                if (!lastAsyncKeyPress.TryGetValue(ev.Key, out last))
                    last = 0L;

                long elapsed = now - last;
                if (elapsed > threshold || elapsed <= 5L)
                {
                    lastAsyncKeyPress[ev.Key] = now;
                    return true;
                }

                if (Main.mod != null)
                    Main.mod.Logger.Log("Blocked Async Key: " + ev.Label + " time: " + elapsed + "ms.");
                return false;
            }
        }
    }
}
