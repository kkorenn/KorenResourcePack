using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace KorenResourcePack
{
    // Level-name UI tweaks: shifts the song-name banner upward and strips inline <size>
    // tags so the title doesn't shrink awkwardly. Also exposes IsSongCaptionEmpty() used
    // by Combo / Overlay to lift the combo readout when the caption is hidden.
    internal static class LevelName
    {
        private static Text trackedText;
        private static Vector2 trackedOriginalPosition;
        private static int trackedOriginalFontSize;
        private static string trackedOriginalText;
        private static string lastRawText;
        private static string lastStrippedText;

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
                    trackedText = levelNameText;
                    trackedOriginalPosition = levelNameText.rectTransform.anchoredPosition;
                    trackedOriginalFontSize = levelNameText.fontSize;
                    trackedOriginalText = levelNameText.text;
                    lastRawText = null;
                    lastStrippedText = null;
                }

                float yOffset = Mathf.Clamp(Screen.height * 0.072f, 44f, 88f);
                int fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.036f), 20, 40);

                levelNameText.resizeTextForBestFit = false;
                levelNameText.alignment = TextAnchor.MiddleCenter;
                levelNameText.fontSize = fontSize;
                levelNameText.rectTransform.anchoredPosition = trackedOriginalPosition + new Vector2(0f, yOffset);

                string current = levelNameText.text;
                if (current != lastStrippedText)
                {
                    if (current != lastRawText)
                    {
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
                Main.mod?.Logger?.Log("[Warning] Level name UI adjust failed: " + ex.Message);
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
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[Warning] Level name UI restore failed: " + ex.Message);
            }
            finally
            {
                trackedText = null;
                trackedOriginalText = null;
                lastRawText = null;
                lastStrippedText = null;
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
    }
}
