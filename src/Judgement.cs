using System;
using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static void DrawJudgementDisplay()
        {
            EnsurePercentStyle();

            int fontSize = ScaledFont(20, 0.035f);
            float shadowOffset = Mathf.Max(2f, Mathf.Round(fontSize * 0.08f));
            float baseY = Screen.height - Mathf.Max(4f, Screen.height * 0.006f) - fontSize - settings.judgementPositionY;

            judgementStyle.fontSize = fontSize;
            judgementShadowStyle.fontSize = fontSize;

            float totalWeight = 0f;
            for (int i = 0; i < JudgementSlotWeights.Length; i++)
            {
                totalWeight += JudgementSlotWeights[i];
            }

            float configuredWidth = Mathf.Max(180f, Screen.width * 0.13f);
            float gap = Mathf.Max(4f, fontSize * 0.18f);
            string[] values = new string[JudgementSlotWeights.Length];
            float[] textWidths = new float[JudgementSlotWeights.Length];
            float[] slotWidths = new float[JudgementSlotWeights.Length];
            float sumText = 0f;

            for (int i = 0; i < JudgementSlotWeights.Length; i++)
            {
                values[i] = GetJudgementSlotCount(i).ToString();
                textWidths[i] = judgementStyle.CalcSize(new GUIContent(values[i])).x;
                sumText += textWidths[i];
            }

            float requiredWidth = sumText + gap * (JudgementSlotWeights.Length - 1);
            float totalWidth = Mathf.Max(configuredWidth, requiredWidth);
            float extra = totalWidth - sumText - gap * (JudgementSlotWeights.Length - 1);

            for (int i = 0; i < JudgementSlotWeights.Length; i++)
            {
                float share = extra * (JudgementSlotWeights[i] / totalWeight);
                slotWidths[i] = textWidths[i] + share;
            }

            float startX = (Screen.width - totalWidth) * 0.5f;
            float[] centers = new float[JudgementSlotWeights.Length];
            float cursor = startX;
            for (int i = 0; i < JudgementSlotWeights.Length; i++)
            {
                centers[i] = cursor + slotWidths[i] * 0.5f;
                cursor += slotWidths[i] + gap;
            }

            Color oldColor = GUI.color;
            int oldDepth = GUI.depth;
            GUI.depth = -10000;

            for (int i = 0; i < JudgementSlotWeights.Length; i++)
            {
                float halfRectWidth = Mathf.Max(textWidths[i], slotWidths[i]) * 0.5f + 2f;
                Rect textRect = new Rect(centers[i] - halfRectWidth, baseY, halfRectWidth * 2f, fontSize + Screen.height * 0.009f);
                judgementStyle.normal.textColor = JudgementSlotColors[i];
                GUI.Label(new Rect(textRect.x + shadowOffset, textRect.y + shadowOffset, textRect.width, textRect.height), values[i], judgementShadowStyle);
                GUI.Label(textRect, values[i], judgementStyle);
            }

            GUI.depth = oldDepth;
            GUI.color = oldColor;
        }

        private static void RegisterJudgementHit(HitMargin hit)
        {
            if (!modEnabled || !runVisible)
            {
                return;
            }

            int hitIndex = (int)hit;
            if (hitIndex >= 0 && hitIndex < judgementCounts.Length)
            {
                judgementCounts[hitIndex]++;
            }

            int slot = GetJudgementSlotForHit(hit);
            if (slot >= 0)
            {
                lastJudgementSlot = slot;
            }
        }

        private static void ResetJudgementDisplay()
        {
            Array.Clear(judgementCounts, 0, judgementCounts.Length);
            lastJudgementSlot = 4;
        }

        private static int GetJudgementSlotForHit(HitMargin hit)
        {
            switch (hit)
            {
                case HitMargin.FailOverload:
                    return 0;
                case HitMargin.TooEarly:
                    return 1;
                case HitMargin.VeryEarly:
                    return 2;
                case HitMargin.EarlyPerfect:
                    return 3;
                case HitMargin.Perfect:
                case HitMargin.Auto:
                    return 4;
                case HitMargin.LatePerfect:
                    return 5;
                case HitMargin.VeryLate:
                    return 6;
                case HitMargin.TooLate:
                    return 7;
                case HitMargin.FailMiss:
                    return 8;
                default:
                    return -1;
            }
        }

        private static int GetJudgementSlotCount(int slot)
        {
            switch (slot)
            {
                case 0:
                    return judgementCounts[(int)HitMargin.FailOverload];
                case 1:
                    return judgementCounts[(int)HitMargin.TooEarly];
                case 2:
                    return judgementCounts[(int)HitMargin.VeryEarly];
                case 3:
                    return judgementCounts[(int)HitMargin.EarlyPerfect];
                case 4:
                    return judgementCounts[(int)HitMargin.Perfect] + judgementCounts[(int)HitMargin.Auto];
                case 5:
                    return judgementCounts[(int)HitMargin.LatePerfect];
                case 6:
                    return judgementCounts[(int)HitMargin.VeryLate];
                case 7:
                    return judgementCounts[(int)HitMargin.TooLate];
                case 8:
                    return judgementCounts[(int)HitMargin.FailMiss];
                default:
                    return 0;
            }
        }
    }
}
