using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace KorenResourcePack
{

    internal static class LevelName
    {
        private static Text trackedText;
        private static Vector2 trackedOriginalPosition;
        private static int trackedOriginalFontSize;
        private static string trackedOriginalText;
        private static string lastRawText;
        private static string lastStrippedText;
        private const float WarningLogInterval = 5f;
        private static float lastAdjustWarningTime = -999f;
        private static float lastRestoreWarningTime = -999f;
        private static readonly Color LevelNameShadowColor = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Vector2 LevelNameShadowDistance = new Vector2(2f, -2f);
        private static Shadow trackedShadow;
        private static bool trackedShadowExisted;
        private static bool trackedOriginalShadowEnabled;
        private static bool trackedOriginalShadowUseGraphicAlpha;
        private static Color trackedOriginalShadowColor;
        private static Vector2 trackedOriginalShadowDistance;

        private static readonly Regex SizeTagRegex =
            new Regex(@"</?size(=[^>]*)?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string StripSizeTags(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            return SizeTagRegex.Replace(raw, string.Empty);
        }

        internal static void AdjustLevelNameUi()
        {
            try
            {
                Text levelNameText = scrController.instance != null ? scrController.instance.txtLevelName : null;
                if (levelNameText == null)
                {
                    return;
                }

                if (trackedText != levelNameText)
                {
                    RestoreTrackedShadow();
                    trackedText = levelNameText;
                    trackedOriginalPosition = levelNameText.rectTransform.anchoredPosition;
                    trackedOriginalFontSize = levelNameText.fontSize;
                    trackedOriginalText = levelNameText.text;
                    lastRawText = null;
                    lastStrippedText = null;
                    CaptureTrackedShadow(levelNameText);
                }

                float yOffset = Mathf.Clamp(Screen.height * 0.072f, 44f, 88f);
                int fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.036f), 20, 40);
                Vector2 targetPosition = trackedOriginalPosition + new Vector2(0f, yOffset);

                if (levelNameText.resizeTextForBestFit) levelNameText.resizeTextForBestFit = false;
                if (levelNameText.alignment != TextAnchor.MiddleCenter) levelNameText.alignment = TextAnchor.MiddleCenter;
                if (levelNameText.fontSize != fontSize) levelNameText.fontSize = fontSize;
                if (levelNameText.rectTransform.anchoredPosition != targetPosition)
                    levelNameText.rectTransform.anchoredPosition = targetPosition;
                ApplyTrackedShadow(levelNameText);

                string current = levelNameText.text;
                if (current != lastStrippedText)
                {
                    if (current != lastRawText)
                    {
                        trackedOriginalText = current;
                        lastRawText = current;
                        lastStrippedText = StripSizeTags(current);
                    }
                    if (current != lastStrippedText)
                    {
                        levelNameText.text = lastStrippedText;
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarningThrottled(ref lastAdjustWarningTime, "[Warning] Level name UI adjust failed: " + ex.Message);
            }
        }

        internal static void RestoreLevelNameUi()
        {
            if (trackedText == null)
            {
                return;
            }

            try
            {
                trackedText.fontSize = trackedOriginalFontSize;
                trackedText.rectTransform.anchoredPosition = trackedOriginalPosition;
                if (trackedOriginalText != null)
                {
                    trackedText.text = trackedOriginalText;
                }
                RestoreTrackedShadow();
            }
            catch (Exception ex)
            {
                LogWarningThrottled(ref lastRestoreWarningTime, "[Warning] Level name UI restore failed: " + ex.Message);
            }
            finally
            {
                trackedText = null;
                trackedOriginalText = null;
                lastRawText = null;
                lastStrippedText = null;
            }
        }

        private static void CaptureTrackedShadow(Text text)
        {
            trackedShadow = text != null ? text.GetComponent<Shadow>() : null;
            trackedShadowExisted = trackedShadow != null;
            if (!trackedShadowExisted) return;

            trackedOriginalShadowEnabled = trackedShadow.enabled;
            trackedOriginalShadowUseGraphicAlpha = trackedShadow.useGraphicAlpha;
            trackedOriginalShadowColor = trackedShadow.effectColor;
            trackedOriginalShadowDistance = trackedShadow.effectDistance;
        }

        private static void ApplyTrackedShadow(Text text)
        {
            if (trackedShadow == null)
                trackedShadow = TextShadows.EnsureUiShadow(text, LevelNameShadowColor, LevelNameShadowDistance);
            if (trackedShadow == null) return;

            if (trackedShadow.effectColor != LevelNameShadowColor)
                trackedShadow.effectColor = LevelNameShadowColor;
            if (trackedShadow.effectDistance != LevelNameShadowDistance)
                trackedShadow.effectDistance = LevelNameShadowDistance;
            if (!trackedShadow.useGraphicAlpha)
                trackedShadow.useGraphicAlpha = true;
            if (!trackedShadow.enabled)
                trackedShadow.enabled = true;
        }

        private static void RestoreTrackedShadow()
        {
            if (trackedShadow == null)
            {
                trackedShadowExisted = false;
                return;
            }

            try
            {
                if (trackedShadowExisted)
                {
                    trackedShadow.effectColor = trackedOriginalShadowColor;
                    trackedShadow.effectDistance = trackedOriginalShadowDistance;
                    trackedShadow.useGraphicAlpha = trackedOriginalShadowUseGraphicAlpha;
                    trackedShadow.enabled = trackedOriginalShadowEnabled;
                }
                else
                {
                    UnityEngine.Object.Destroy(trackedShadow);
                }
            }
            catch
            {
            }
            finally
            {
                trackedShadow = null;
                trackedShadowExisted = false;
            }
        }

        internal static bool IsSongCaptionEmpty()
        {
            try
            {
                scnGame g = scnGame.instance;
                if (g == null || g.levelData == null) return false;
                return string.IsNullOrWhiteSpace(g.levelData.song) && string.IsNullOrWhiteSpace(g.levelData.artist);
            }
            catch { return false; }
        }

        private static void LogWarningThrottled(ref float lastLogTime, string message)
        {
            float now = 0f;
            try { now = Time.unscaledTime; } catch { }
            if (now - lastLogTime < WarningLogInterval)
                return;
            lastLogTime = now;
            Main.mod?.Logger?.Log(message);
        }
    }
}
