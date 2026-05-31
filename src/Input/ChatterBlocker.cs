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

        private static readonly HashSet<string> countedKeyBuf = new HashSet<string>();
        private static readonly List<string> keyIdentityBuf = new List<string>(4);

        private static string PhysicalKeyIdentity(KeyCode key)
        {
            if (key != KeyCode.None)
                return "k:" + (int)KeyLimiter.NormalizeKeyForComparison(key);

            return null;
        }

        private static void AddKeyIdentity(List<string> identities, KeyCode key)
        {
            string identity = PhysicalKeyIdentity(key);
            if (identity == null)
                return;

            if (!identities.Contains(identity))
                identities.Add(identity);
        }

        private static void AddFallbackIdentity(List<string> identities, string fallback)
        {
            if (string.IsNullOrEmpty(fallback))
                return;

            string identity = "a:" + fallback;
            if (!identities.Contains(identity))
                identities.Add(identity);
        }

        private static bool ClaimKeyIdentities(List<string> identities)
        {
            if (identities.Count == 0)
                return true;

            for (int i = 0; i < identities.Count; i++)
            {
                if (countedKeyBuf.Contains(identities[i]))
                    return false;
            }

            for (int i = 0; i < identities.Count; i++)
                countedKeyBuf.Add(identities[i]);

            return true;
        }

        private static bool ClaimPhysicalKey(KeyCode key)
        {
            keyIdentityBuf.Clear();
            AddKeyIdentity(keyIdentityBuf, key);
            return ClaimKeyIdentities(keyIdentityBuf);
        }

        private static bool ClaimAsyncKey(KeyLabel label)
        {
            keyIdentityBuf.Clear();
            AddKeyIdentity(keyIdentityBuf, KeyLimiter.AsyncLabelToPhysicalUnityKey(label));
            AddKeyIdentity(keyIdentityBuf, AsyncKeyMapper.AsyncKeyToUnityKey(label));
            AddFallbackIdentity(keyIdentityBuf, label.ToString());
            return ClaimKeyIdentities(keyIdentityBuf);
        }

        private static readonly KeyCode[] bridgedModifiers =
        {
            KeyCode.LeftShift, KeyCode.RightShift,
            KeyCode.LeftControl, KeyCode.RightControl,
            KeyCode.LeftAlt, KeyCode.RightAlt,
            KeyCode.LeftCommand, KeyCode.RightCommand,
        };

        private static bool BridgeModifiersEnabled => ADOBase.platform == Platform.Mac;

        // Mac runs the game's async (SkyHook) input alongside the Unity Input.GetKeyDown
        // bridge below. Both observe the same physical press, but one frame apart (async
        // leads, being the low-latency path), and countedKeyBuf only dedups within a single
        // frame -- so the lagging echo counts one tap as two valid presses -> in-game double
        // press. Remember the frame each physical key last counted and drop the re-count on an
        // adjacent frame. A genuine re-press needs a release+press (>=2 frames even at 60fps),
        // so only the cross-source echo is suppressed, never real input.
        private static readonly Dictionary<int, int> lastValidPressFrame = new Dictionary<int, int>();
        private const int BridgeEchoFrames = 1;

        private static bool IsBridgeEcho(KeyCode key)
        {
            if (key == KeyCode.None) return false;
            int frame;
            if (!lastValidPressFrame.TryGetValue((int)KeyLimiter.NormalizeKeyForComparison(key), out frame))
                return false;
            return (uint)(Time.frameCount - frame) <= BridgeEchoFrames;
        }

        private static void MarkValidPress(KeyCode key)
        {
            if (key == KeyCode.None) return;
            lastValidPressFrame[(int)KeyLimiter.NormalizeKeyForComparison(key)] = Time.frameCount;
        }

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

                    if (keyLimiterActive && BridgeModifiersEnabled && IsBridgeEcho(key))
                        continue;

                    RecordKeyStats(controller, key);
                    if (AcceptNormalKey(key, lastKeyPress, now, threshold, chatterActive))
                    {
                        MarkValidPress(key);
                        count++;
                    }
                }
                else if (value is AsyncKeyCode)
                {
                    AsyncKeyCode key = (AsyncKeyCode)value;
                    if (KeyLimiter.ShouldBlockAsyncKey(0, key.label))
                        continue;

                    KeyCode mapped = KeyLimiter.AsyncLabelToPhysicalUnityKey(key.label);
                    if (!ClaimAsyncKey(key.label))
                        continue;

                    if (keyLimiterActive && BridgeModifiersEnabled && IsBridgeEcho(mapped))
                        continue;

                    RecordKeyStats(controller, key);
                    if (mapped == KeyCode.None || AcceptNormalKey(mapped, lastKeyPress, now, threshold, chatterActive))
                    {
                        MarkValidPress(mapped);
                        MarkValidPress(AsyncKeyMapper.AsyncKeyToUnityKey(key.label));
                        count++;
                    }
                }
            }

            if (BridgeModifiersEnabled && KeyLimiter.IsActive() && KeyLimiter.InPlayerControl())
            {
                for (int i = 0; i < bridgedModifiers.Length; i++)
                {
                    KeyCode mod = bridgedModifiers[i];
                    if (countedKeyBuf.Contains(PhysicalKeyIdentity(mod))) continue;
                    if (IsBridgeEcho(mod)) continue;
                    if (!KeyLimiter.IsAllowedKey(mod)) continue;
                    if (!Input.GetKeyDown(mod)) continue;
                    if (!ClaimPhysicalKey(mod)) continue;
                    RecordKeyStats(controller, mod);
                    if (AcceptNormalKey(mod, lastKeyPress, now, threshold, chatterActive))
                    {
                        MarkValidPress(mod);
                        count++;
                    }
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
                        if (countedKeyBuf.Contains(PhysicalKeyIdentity(key))) continue;
                        if (IsBridgeEcho(key)) continue;
                        if (!Input.GetKeyDown(key)) continue;
                        if (!ClaimPhysicalKey(key)) continue;
                        RecordKeyStats(controller, key);
                        if (AcceptNormalKey(key, lastKeyPress, now, threshold, chatterActive))
                        {
                            MarkValidPress(key);
                            count++;
                        }
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
