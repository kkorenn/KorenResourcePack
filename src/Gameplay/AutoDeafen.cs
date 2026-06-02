using UnityEngine;

namespace KorenResourcePack
{
    // Toggles Discord deafen by tapping a configurable key chord at level-progress
    // thresholds. Deafen on the way up at AutoDeafenAtPercent, undeafen at
    // AutoUndeafenAtPercent. Only arms when the run started from the first tile so
    // checkpoint/practice retries never deafen.
    internal static class AutoDeafen
    {
        private const int PhaseIdle = 0;      // waiting to reach the deafen threshold
        private const int PhaseDeafened = 1;  // deafen sent, waiting for the undeafen threshold
        private const int PhaseDone = 2;      // run finished its deafen cycle (or was ineligible)

        private static int phase = PhaseIdle;

        // Hard anti-spam guard: never fire the chord more often than this. Deafen and
        // undeafen are seconds/minutes apart, so this only ever drops runaway storms
        // (e.g. an editor scrub loop), which previously flooded the OS input queue.
        private const float MinSendInterval = 0.3f;
        private static float lastSendTime = -999f;

        // Only auto-deafen during a real playthrough. In the editor percentComplete
        // jumps around as you scrub and the scene resets every frame, which would
        // oscillate deafen/undeafen endlessly.
        private static bool InRealPlay()
        {
            try { return scnGame.instance != null; }
            catch { return false; }
        }

        // Called every frame with the current level progress (0..1).
        internal static void Observe(float progress01)
        {
            Settings s = Main.settings;
            if (s == null) return;

            if (!s.AutoDeafenOn || !InRealPlay())
            {
                OnRunReset();
                return;
            }

            float pct = Mathf.Clamp01(progress01) * 100f;

            switch (phase)
            {
                case PhaseIdle:
                    if (pct >= s.AutoDeafenAtPercent)
                    {
                        bool eligible = !s.AutoDeafenOnlyFromStart || ProgressTracker.RunStartedFromFirstTile;
                        if (eligible)
                        {
                            Toggle("deafen");
                            phase = PhaseDeafened;
                        }
                        else
                        {
                            // Not started from 0 — skip this run entirely.
                            phase = PhaseDone;
                        }
                    }
                    break;

                case PhaseDeafened:
                    if (s.AutoUndeafenOn
                        && s.AutoUndeafenAtPercent > s.AutoDeafenAtPercent
                        && pct >= s.AutoUndeafenAtPercent)
                    {
                        Toggle("undeafen");
                        phase = PhaseDone;
                    }
                    break;
            }
        }

        // Resets per-run state. If we are still deafened, tap the chord once more so
        // the player is never left stuck deafened after a restart/exit/disable.
        internal static void OnRunReset()
        {
            // Safety undeafen must always go through (force) so the player is never
            // left stuck deafened. Can't storm: it only fires when phase==Deafened,
            // which is cleared to Idle right after, so repeat resets no-op.
            if (phase == PhaseDeafened)
                Toggle("reset-undeafen", true);
            phase = PhaseIdle;
        }

        private static void Toggle(string reason, bool force = false)
        {
            Settings s = Main.settings;
            if (s == null) return;

            float now = Time.unscaledTime;
            if (!force && now - lastSendTime < MinSendInterval) return;
            lastSendTime = now;

            NativeKeySender.SendChord(
                s.AutoDeafenCtrl, s.AutoDeafenShift, s.AutoDeafenAlt, s.AutoDeafenMeta,
                (KeyCode)KeyCodeCompat.NormalizeKeyCode(s.AutoDeafenKey));
            Main.mod?.Logger?.Log("[AutoDeafen] " + reason);
        }
    }
}
