using System;
using System.Collections.Generic;
using SkyHook;
using UnityEngine;

namespace KorenResourcePack
{

    internal static partial class ChatterBlocker
    {
        internal static bool OnCountValidKeysPressedPrefix(ref int __result) => CountValidKeysPressedPrefix(ref __result);
        internal static bool OnSkyHookEvent(SkyHookEvent ev) => HandleSkyHookEvent(ev);

        private static readonly Dictionary<KeyCode, long> lastKeyPress = new Dictionary<KeyCode, long>();
        private static readonly Dictionary<ushort, long> lastAsyncKeyPress = new Dictionary<ushort, long>();
        private static readonly Dictionary<KeyCode, long> lastKeyViewerPress = new Dictionary<KeyCode, long>();

        private static bool HasAnyFilter()
        {
            return Main.modEnabled && Main.settings != null && (Main.settings.KCBOn || KeyLimiter.IsEnabled());
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
            bool active = IsActive();
            if (!active) return true;
            return AcceptNormalKey(key, lastKeyViewerPress, NowMs(), ThresholdMs(), true);
        }

        private static void RecordKeyStats(scrController controller, object key)
        {
            try
            {
                RecordKeyStatsImpl(controller, key);
            }
            catch { }
        }

        private static void RecordKeyStatsImpl(scrController controller, object key)
        {
            scrPlayer player = controller != null ? controller.playerOne : null;
            if (player == null || player.keyFrequency == null) return;
            player.keyFrequency[key] = player.keyFrequency.ContainsKey(key)
                ? player.keyFrequency[key] + 1
                : 0;
            player.keyTotal++;
        }

        private static readonly HashSet<KeyCode> countedKeyBuf = new HashSet<KeyCode>();

        private static bool ClaimPhysicalKey(KeyCode key)
        {
            if (key == KeyCode.None)
                return true;

            if (countedKeyBuf.Contains(key))
                return false;

            countedKeyBuf.Add(key);
            return true;
        }

        private static readonly KeyCode[] bridgedModifiers =
        {
            KeyCode.LeftShift, KeyCode.RightShift,
            KeyCode.LeftControl, KeyCode.RightControl,
            KeyCode.LeftAlt, KeyCode.RightAlt,
            KeyCode.LeftCommand, KeyCode.RightCommand,
        };

        private static bool BridgeModifiersEnabled => ADOBase.platform == Platform.Mac;

        private static int CountValidKeysPressed()
        {
            int count = 0;
            scrController controller = scrController.instance;
            if (controller == null) return 0;
            ResetKeyLimiterOverCounter(controller);
            bool chatterActive = IsActive();
            bool keyLimiterActive = KeyLimiter.IsActive() && KeyLimiter.InPlayerControl();
            long now = chatterActive || keyLimiterActive ? NowMs() : 0L;
            long threshold = chatterActive ? ThresholdMs() : 0L;

            countedKeyBuf.Clear();

            foreach (AnyKeyCode mainPressKey in RDInput.GetMainPressKeys())
            {
                object value = mainPressKey.value;
                if (value is KeyCode)
                {
                    KeyCode key = (KeyCode)value;
                    if (KeyLimiter.ShouldBlockKey(key))
                        continue;

                    if (!ClaimPhysicalKey(key))
                        continue;

                    RecordKeyStats(controller, key);
                    if (AcceptNormalKey(key, lastKeyPress, now, threshold, chatterActive))
                        count++;
                }
                else if (value is AsyncKeyCode)
                {
                    AsyncKeyCode key = (AsyncKeyCode)value;
                    if (KeyLimiter.ShouldBlockAsyncKey(0, key.label))
                        continue;

                    KeyCode mapped = KeyLimiter.AsyncLabelToPhysicalUnityKey(key.label);
                    if (!ClaimPhysicalKey(mapped))
                        continue;

                    RecordKeyStats(controller, key);
                    if (mapped == KeyCode.None || AcceptNormalKey(mapped, lastKeyPress, now, threshold, chatterActive))
                        count++;
                }
            }

            if (BridgeModifiersEnabled && KeyLimiter.IsActive() && KeyLimiter.InPlayerControl())
            {
                for (int i = 0; i < bridgedModifiers.Length; i++)
                {
                    KeyCode mod = bridgedModifiers[i];
                    if (countedKeyBuf.Contains(mod)) continue;
                    if (!KeyLimiter.IsAllowedKey(mod)) continue;
                    if (!Input.GetKeyDown(mod)) continue;
                    if (!ClaimPhysicalKey(mod)) continue;
                    RecordKeyStats(controller, mod);
                    if (AcceptNormalKey(mod, lastKeyPress, now, threshold, chatterActive))
                        count++;
                }

                // RDInput.GetMainPressKeys doesn't surface every physical key on Mac
                // (notably numpad), so allowed keys never count as valid hits even when
                // bound. Bridge any allowed key Unity sees pressed this frame.
                int[] allowed = Main.settings != null ? Main.settings.KeyLimiterAllowed : null;
                if (allowed != null)
                {
                    for (int i = 0; i < allowed.Length; i++)
                    {
                        KeyCode key = (KeyCode)allowed[i];
                        if (key == KeyCode.None) continue;
                        if (countedKeyBuf.Contains(key)) continue;
                        if (!Input.GetKeyDown(key)) continue;
                        if (!ClaimPhysicalKey(key)) continue;
                        RecordKeyStats(controller, key);
                        if (AcceptNormalKey(key, lastKeyPress, now, threshold, chatterActive))
                            count++;
                    }
                }
            }

            return count;
        }

        private static void ResetKeyLimiterOverCounter(scrController controller)
        {
            if (controller != null && controller.playerOne != null)
                controller.playerOne.keyLimiterOverCounter = 0;
        }

        private static bool CountValidKeysPressedPrefix(ref int __result)
        {
            if (!HasAnyFilter())
            {
                return true;
            }

            __result = CountValidKeysPressed();
            return false;
        }

        private static bool HandleSkyHookEvent(SkyHookEvent ev)
        {
            KeyCode rawKey = KeyLimiter.HookKeyToPhysicalUnityKey(ev.Key, ev.Label);
            if (rawKey != KeyCode.None)
            {
                KeyViewer.ObserveRawKeyState(rawKey, ev.Type != SkyHook.EventType.KeyReleased);
                SettingsGui.ObserveHookCaptureKey(rawKey, ev.Type != SkyHook.EventType.KeyReleased);
            }

            if (KeyLimiter.IsMouseKey(rawKey) || KeyLimiter.IsMouseLabel(ev.Label))
                return true;

            if (ev.Type == SkyHook.EventType.KeyReleased || ev.Key == 27)
                return true;

            if (KeyLimiter.ShouldBlockAsyncKeyFromHook(ev.Key, ev.Label))
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
