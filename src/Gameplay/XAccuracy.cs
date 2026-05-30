using System;
using System.Collections.Generic;
using UnityEngine;

namespace KorenResourcePack
{
    // Faithful reconstruction of the game's XAccuracy so we can project the
    // true best-achievable final value ("max xaccuracy").
    //
    // Game formula (scrMarginTracker.CalculatePercentAcc):
    //   weightedSum = 1.0 *(Perfect+Auto)
    //               + 0.75*(EarlyPerfect+LatePerfect)
    //               + 0.4 *(VeryEarly+VeryLate)
    //               + 0.2 *(TooEarly+TooLate)
    //               + 0.2 * deadTiles
    //   denom       = hitMargins.Count + deadTiles
    //   percentXAcc = (weightedSum / denom) * 0.9875^checkpointsUsed
    //
    // Max projection: assume every remaining hittable tile is a pure Perfect
    // (weight 1.0, +1 to numerator and denominator each) with no further deaths
    // or checkpoints. The checkpoint multiplier is already locked in, so the
    // achievable ceiling is capped at 0.9875^checkpointsUsed - never 1.0.
    //
    // "Remaining" is counted as hittable floors strictly ahead of the current
    // play position (currentSeqID), not floorCount - hitCount. That keeps the
    // projection tied to the actual game cursor: at level end (currentSeqID =
    // last floor) remaining is 0, so max == actual xacc. Counting by hit-count
    // bookkeeping instead over-counts by the non-hit start tile and any extra
    // presses, which is what made max read higher than actual at 100%.
    internal static class XAccuracy
    {
        private const double CheckpointPenalty = 0.9875;

        // prefixHittable[i] = number of hittable floors (!auto && !midSpin) among
        // indices 0..i inclusive. Rebuilt only when the level changes.
        private static List<scrFloor> cachedFloors;
        private static int cachedFloorCount = -1;
        private static int[] prefixHittable;
        private static int cachedHittableTotal;

        private static void EnsurePrefix(List<scrFloor> floors)
        {
            int count = floors.Count;
            if (ReferenceEquals(floors, cachedFloors) && count == cachedFloorCount && prefixHittable != null)
                return;

            int[] prefix = new int[count];
            int running = 0;
            for (int i = 0; i < count; i++)
            {
                scrFloor f = floors[i];
                if (f != null && !f.auto && !f.midSpin) running++;
                prefix[i] = running;
            }

            cachedFloors = floors;
            cachedFloorCount = count;
            prefixHittable = prefix;
            cachedHittableTotal = running;
        }

        // Hittable floors strictly ahead of currentSeqID (i.e. still to be hit).
        private static int RemainingHittable()
        {
            scrLevelMaker lm = scrLevelMaker.instance;
            scrController ctrl = scrController.instance;
            if (lm == null || lm.listFloors == null || ctrl == null) return 0;

            List<scrFloor> floors = lm.listFloors;
            int count = floors.Count;
            if (count == 0) return 0;
            EnsurePrefix(floors);

            int seq = ctrl.currentSeqID;
            if (seq < 0) seq = 0;
            if (seq > count - 1) seq = count - 1;

            int consumedHittable = prefixHittable[seq]; // hittable floors at indices 0..seq
            int remaining = cachedHittableTotal - consumedHittable;
            return remaining < 0 ? 0 : remaining;
        }

        private static int Hits(scrMarginTracker t, HitMargin m)
        {
            int i = (int)m;
            int[] counts = t.hitMarginsCount;
            return (counts != null && i >= 0 && i < counts.Length) ? counts[i] : 0;
        }

        // Best final XAccuracy reachable from the current state. Returns 1f when
        // no run / data is available so the UI shows a neutral ceiling.
        // Defaults to player one; co-op callers pass an explicit playerID.
        internal static float MaxRatio()
        {
            return MaxRatio(0);
        }

        // Per-player max projection (local co-op). playerID is 0-based.
        internal static float MaxRatio(int playerID)
        {
            try
            {
                scrController ctrl = scrController.instance;
                if (ctrl == null) return 1f;
                scrMarginTracker t = MistakesAccess.Tracker(playerID);
                if (t == null || t.hitMargins == null) return 1f;

                double checkpointFactor = Math.Pow(CheckpointPenalty, scrController.checkpointsUsed);

                int deadTiles = t.deadTiles;

                double weightedSum =
                      1.0  * (Hits(t, HitMargin.Perfect) + Hits(t, HitMargin.Auto))
                    + 0.75 * (Hits(t, HitMargin.EarlyPerfect) + Hits(t, HitMargin.LatePerfect))
                    + 0.4  * (Hits(t, HitMargin.VeryEarly) + Hits(t, HitMargin.VeryLate))
                    + 0.2  * (Hits(t, HitMargin.TooEarly) + Hits(t, HitMargin.TooLate))
                    + 0.2  * deadTiles;

                double denom = t.hitMargins.Count + deadTiles;

                // Each remaining hittable tile is projected as a pure Perfect (weight 1.0).
                // In co-op the play cursor (currentSeqID) is shared and players alternate
                // tiles, so split the remaining count evenly across the active players.
                int remaining = RemainingHittable();
                int playerCount = MistakesAccess.PlayerCount();
                if (playerCount > 1) remaining = remaining / playerCount;

                double finalNumerator = weightedSum + remaining;
                double finalDenominator = denom + remaining;
                if (finalDenominator <= 0.0) return Mathf.Clamp01((float)checkpointFactor);

                double max = checkpointFactor * (finalNumerator / finalDenominator);
                if (double.IsNaN(max) || double.IsInfinity(max)) return 1f;
                return Mathf.Clamp01((float)max);
            }
            catch { return 1f; }
        }
    }
}
