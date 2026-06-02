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
        private static readonly HashSet<KeyCode> reportedKeysThisFrame = new HashSet<KeyCode>();
        private static readonly HashSet<KeyCode> injectedKeyHeldPrev = new HashSet<KeyCode>();

        // Auto-deafen injects a key chord at the HID level to trigger Discord's global
        // shortcut. This hook (also HID, head-inserted) would otherwise suppress that
        // key during play, so it never reaches Discord. BeginInjectBypass opens a short
        // window during which the hook passes everything through untouched.
        private static volatile int injectBypassUntilTick;

        internal static void BeginInjectBypass(int ms)
        {
            injectBypassUntilTick = Environment.TickCount + ms;
        }

        private static bool InjectBypassActive()
        {
            int until = injectBypassUntilTick;
            if (until == 0) return false;
            return unchecked(Environment.TickCount - until) < 0;
        }

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

        private static int CountValidKeysPressed()
        {
            scrController controller = scrController.instance;
            if (controller == null) return 0;
            ResetKeyLimiterOverCounter(controller);

            bool chatterActive = IsActive();
            long now = NowMs();
            long threshold = ThresholdMs();
            int count = 0;

            reportedKeysThisFrame.Clear();
            foreach (AnyKeyCode mainPressKey in RDInput.GetMainPressKeys())
            {
                object value = mainPressKey.value;
                if (value is KeyCode)
                {
                    KeyCode key = (KeyCode)value;
                    reportedKeysThisFrame.Add(KeyLimiter.NormalizeKeyForComparison(key));
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

            count += CountKeysMissedByGame(controller, now, threshold, chatterActive);

            return count;
        }

        // macOS doesn't deliver an Input.GetKeyDown down-edge for modifier keys
        // (Left/Right Shift, etc.), so the game's main-key scan never counts them as
        // hits even though their held state is readable. Re-create the missing
        // down-edge for allowed keys the game failed to report this frame, using the
        // same multi-source held detection the KeyViewer uses, so anything that lights
        // up the KeyViewer also registers as a hit. No-op on Windows (the game already
        // reports those keys, so they land in reportedKeysThisFrame and are skipped).
        private static int CountKeysMissedByGame(scrController controller, long now, long threshold, bool chatterActive)
        {
            if (!KeyLimiter.IsActive() || !KeyLimiter.InPlayerControl())
            {
                injectedKeyHeldPrev.Clear();
                return 0;
            }

            int[] allowed = Main.settings != null ? Main.settings.KeyLimiterAllowed : null;
            if (allowed == null || allowed.Length == 0)
            {
                injectedKeyHeldPrev.Clear();
                return 0;
            }

            int injected = 0;
            for (int i = 0; i < allowed.Length; i++)
            {
                KeyCode key = KeyLimiter.NormalizeKeyForComparison((KeyCode)allowed[i]);
                if (key == KeyCode.None || KeyLimiter.IsMouseKey(key))
                    continue;

                // Game already reported it this frame: count handled above. Mark it
                // held so a lagging held-state read can't re-fire the edge next frame.
                if (reportedKeysThisFrame.Contains(key))
                {
                    injectedKeyHeldPrev.Add(key);
                    continue;
                }

                bool held = KeyViewer.IsKeyHeldAnySource(key);
                if (held && !injectedKeyHeldPrev.Contains(key))
                {
                    RecordKeyStats(controller, key);
                    if (AcceptNormalKey(key, lastKeyPress, now, threshold, chatterActive))
                        injected++;
                }

                if (held)
                    injectedKeyHeldPrev.Add(key);
                else
                    injectedKeyHeldPrev.Remove(key);
            }

            return injected;
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
            // Let the auto-deafen synthetic keystroke flow straight through to Discord.
            if (InjectBypassActive())
                return true;

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
            if (elapsed > threshold)
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
