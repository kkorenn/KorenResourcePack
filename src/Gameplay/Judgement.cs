using System;
using UnityEngine;

namespace KorenResourcePack
{
    // Per-judgement HUD strip — renders the 9-slot count display along the bottom edge.
    // Owns the per-hit counter (judgementCounts) and the running "last slot" pointer that
    // the combo / overlay code reads to highlight the matching cell.
    internal static class Judgement
    {
        internal const int JudgementSlots = 9;

        internal static readonly float[] JudgementSlotWeights = { 0.85f, 1f, 1.1f, 1.2f, 1.7f, 1.2f, 1.1f, 1f, 0.85f };
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

        // Per-HitMargin counter (12 slots covers the full enum range).
        internal static readonly int[] judgementCounts = new int[12];
        // Last triggered slot index (0..8). Read by Combo to highlight the latest tier.
        internal static int lastJudgementSlot = 4;

        private static readonly string[] kJudgementValues = new string[JudgementSlots];
        private static readonly string[] kJudgementShadowValues = new string[JudgementSlots];
        private static readonly float[] kJudgementTextWidths = new float[JudgementSlots];
        private static readonly float[] kJudgementSlotWidths = new float[JudgementSlots];
        private static readonly float[] kJudgementCenters = new float[JudgementSlots];
        private static readonly int[] kJudgementCachedCount = new int[JudgementSlots];

        private static readonly GUIContent kSharedContent = new GUIContent();

        private static int kJudgementCachedXp = -1, kJudgementCachedPp = -1, kJudgementCachedMp = -1;
        private static bool kJudgementCacheXpMode = false;
        private static int kJudgementCachedFontSize = -1;

        private static float kJudgementWeightSum = 0f;

        internal static void DrawJudgementDisplay()
        {
            Styles.EnsurePercentStyle();

            int fontSize = Styles.ScaledFont(20, 0.035f);
            float shadowOffset = Mathf.Max(2f, Mathf.Round(fontSize * 0.08f));
            float baseY = Screen.height - Mathf.Max(4f, Screen.height * 0.006f) - fontSize - Main.settings.judgementPositionY;

            Styles.judgementStyle.fontSize = fontSize;
            Styles.judgementShadowStyle.fontSize = fontSize;

            bool xpJudgement = XPerfectBridge.Active;

            if (kJudgementWeightSum <= 0f)
            {
                float w = 0f;
                for (int i = 0; i < JudgementSlots; i++)
                    w += JudgementSlotWeights[i];

                kJudgementWeightSum = w;
            }

            float totalWeight = kJudgementWeightSum;

            int xc = 0, pc = 0, mc = 0;
            if (xpJudgement)
            {
                xc = XPerfectBridge.XCount();
                pc = XPerfectBridge.PlusCount();
                mc = XPerfectBridge.MinusCount();
            }

            bool fontChanged = fontSize != kJudgementCachedFontSize;
            bool xpChanged = xpJudgement != kJudgementCacheXpMode;

            float sumText = 0f;

            // -------------------------
            // CACHE + WIDTH COMPUTATION
            // -------------------------
            for (int i = 0; i < JudgementSlots; i++)
            {
                bool recalc = fontChanged || xpChanged;

                if (i == 4 && xpJudgement)
                {
                    if (kJudgementCachedXp != xc ||
                        kJudgementCachedPp != pc ||
                        kJudgementCachedMp != mc ||
                        recalc)
                    {
                        kJudgementValues[i] =
                            "<color=#60FF4E>" + pc + "</color> " +
                            "<color=#4DCCFF>" + xc + "</color> " +
                            "<color=#60FF4E>" + mc + "</color>";

                        kJudgementShadowValues[i] = pc + " " + xc + " " + mc;

                        kSharedContent.text = kJudgementValues[i];
                        kJudgementTextWidths[i] = Styles.judgementStyle.CalcSize(kSharedContent).x;
                    }
                }
                else
                {
                    int count = GetJudgementSlotCount(i);

                    if (count != kJudgementCachedCount[i] || recalc)
                    {
                        kJudgementCachedCount[i] = count;

                        kJudgementValues[i] = count.ToString();
                        kJudgementShadowValues[i] = kJudgementValues[i];

                        kSharedContent.text = kJudgementValues[i];
                        kJudgementTextWidths[i] = Styles.judgementStyle.CalcSize(kSharedContent).x;
                    }
                }

                sumText += kJudgementTextWidths[i];
            }

            kJudgementCachedXp = xc;
            kJudgementCachedPp = pc;
            kJudgementCachedMp = mc;
            kJudgementCacheXpMode = xpJudgement;
            kJudgementCachedFontSize = fontSize;

            float configuredWidth = Mathf.Max(180f, Screen.width * 0.13f);
            float gap = Mathf.Max(4f, fontSize * 0.18f);

            float requiredWidth = sumText + gap * (JudgementSlots - 1);
            float totalWidth = Mathf.Max(configuredWidth, requiredWidth);

            float extra = totalWidth - sumText - gap * (JudgementSlots - 1);

            for (int i = 0; i < JudgementSlots; i++)
            {
                float share = extra * (JudgementSlotWeights[i] / totalWeight);
                kJudgementSlotWidths[i] = kJudgementTextWidths[i] + share;
            }

            int pivot = 4;
            float centerX = Screen.width * 0.5f;

            kJudgementCenters[pivot] = centerX;

            float cursor;

            cursor = centerX;
            for (int i = pivot - 1; i >= 0; i--)
            {
                float wRightNeighbor = kJudgementSlotWidths[i + 1];
                float wCurrent = kJudgementSlotWidths[i];

                cursor -= (wCurrent * 0.5f + gap + wRightNeighbor * 0.5f);
                kJudgementCenters[i] = cursor;
            }

            cursor = centerX;
            for (int i = pivot + 1; i < JudgementSlots; i++)
            {
                float wLeftNeighbor = kJudgementSlotWidths[i - 1];
                float wCurrent = kJudgementSlotWidths[i];

                cursor += (wLeftNeighbor * 0.5f + gap + wCurrent * 0.5f);
                kJudgementCenters[i] = cursor;
            }

            int oldDepth = GUI.depth;
            GUI.depth = -10000;

            float rectH = fontSize + Screen.height * 0.009f;

            for (int i = 0; i < JudgementSlots; i++)
            {
                float halfWidth = Mathf.Max(kJudgementTextWidths[i], kJudgementSlotWidths[i]) * 0.5f + 2f;

                Rect r = new Rect(
                    kJudgementCenters[i] - halfWidth,
                    baseY,
                    halfWidth * 2f,
                    rectH
                );

                Styles.judgementStyle.normal.textColor = JudgementSlotColors[i];

                GUI.Label(
                    new Rect(r.x + shadowOffset, r.y + shadowOffset, r.width, r.height),
                    kJudgementShadowValues[i],
                    Styles.judgementShadowStyle
                );

                GUI.Label(r, kJudgementValues[i], Styles.judgementStyle);
            }

            GUI.depth = oldDepth;
        }

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

            for (int i = 0; i < JudgementSlots; i++)
                kJudgementCachedCount[i] = -1;

            kJudgementCachedXp = -1;
            kJudgementCachedPp = -1;
            kJudgementCachedMp = -1;
            kJudgementCachedFontSize = -1;
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
