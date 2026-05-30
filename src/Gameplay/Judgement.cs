using System;
using UnityEngine;

namespace KorenResourcePack
{
    internal static class Judgement
    {
        internal const int JudgementSlots = 9;

        internal static readonly Color[] JudgementSlotColors =
        {
            new Color(0.78f, 0.35f, 1f, 1f),
            new Color(1f, 0.22f, 0.22f, 1f),
            new Color(1f, 0.44f, 0.31f, 1f),
            new Color(0.63f, 1f, 0.31f, 1f),
            new Color(0.38f, 1f, 0.31f, 1f),
            new Color(0.63f, 1f, 0.31f, 1f),
            new Color(1f, 0.44f, 0.31f, 1f),
            new Color(1f, 0.22f, 0.22f, 1f),
            new Color(0.78f, 0.35f, 1f, 1f)
        };

        internal static readonly int[] judgementCounts = new int[12];

        internal static int lastJudgementSlot = 4;

        internal static void RegisterJudgementHit(HitMargin hit)
        {
            if (!Main.modEnabled || !Main.runVisible)
                return;

            int idx = (int)hit;

            if (idx >= 0 && idx < judgementCounts.Length)
                judgementCounts[idx]++;

            int slot = GetJudgementSlotForHit(hit);

            if (slot >= 0)
                lastJudgementSlot = slot;
        }

        internal static void ResetJudgementDisplay()
        {
            Array.Clear(judgementCounts, 0, judgementCounts.Length);
            lastJudgementSlot = 4;
        }

        internal static int GetJudgementSlotForHit(HitMargin hit)
        {
            switch (hit)
            {
                case HitMargin.FailOverload: return 0;
                case HitMargin.TooEarly: return 1;
                case HitMargin.VeryEarly: return 2;
                case HitMargin.EarlyPerfect: return 3;
                case HitMargin.Perfect:
                case HitMargin.Auto: return 4;
                case HitMargin.LatePerfect: return 5;
                case HitMargin.VeryLate: return 6;
                case HitMargin.TooLate: return 7;
                case HitMargin.FailMiss: return 8;
                default: return -1;
            }
        }

        internal static int GetJudgementSlotCount(int slot)
        {
            switch (slot)
            {
                case 0: return judgementCounts[(int)HitMargin.FailOverload];
                case 1: return judgementCounts[(int)HitMargin.TooEarly];
                case 2: return judgementCounts[(int)HitMargin.VeryEarly];
                case 3: return judgementCounts[(int)HitMargin.EarlyPerfect];
                case 4: return judgementCounts[(int)HitMargin.Perfect] + judgementCounts[(int)HitMargin.Auto];
                case 5: return judgementCounts[(int)HitMargin.LatePerfect];
                case 6: return judgementCounts[(int)HitMargin.VeryLate];
                case 7: return judgementCounts[(int)HitMargin.TooLate];
                case 8: return judgementCounts[(int)HitMargin.FailMiss];
                default: return 0;
            }
        }
    }
}
