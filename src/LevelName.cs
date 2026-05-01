using System;
using UnityEngine;
using UnityEngine.UI;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static void AdjustLevelNameUi()
        {
            try
            {
                Text levelNameText = scrController.instance != null ? scrController.instance.txtLevelName : null;
                if (levelNameText == null)
                {
                    return;
                }

                if (trackedLevelNameText != levelNameText)
                {
                    trackedLevelNameText = levelNameText;
                    trackedLevelNameOriginalPosition = levelNameText.rectTransform.anchoredPosition;
                    trackedLevelNameOriginalFontSize = levelNameText.fontSize;
                }

                float yOffset = Mathf.Clamp(Screen.height * 0.072f, 44f, 88f);
                int fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.036f), 20, 40);

                levelNameText.resizeTextForBestFit = false;
                levelNameText.alignment = TextAnchor.MiddleCenter;
                levelNameText.fontSize = fontSize;
                levelNameText.rectTransform.anchoredPosition = trackedLevelNameOriginalPosition + new Vector2(0f, yOffset);
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[Warning] Level name UI adjust failed: " + ex.Message);
            }
        }

        private static void RestoreLevelNameUi()
        {
            if (trackedLevelNameText == null)
            {
                return;
            }

            try
            {
                trackedLevelNameText.fontSize = trackedLevelNameOriginalFontSize;
                trackedLevelNameText.rectTransform.anchoredPosition = trackedLevelNameOriginalPosition;
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[Warning] Level name UI restore failed: " + ex.Message);
            }
            finally
            {
                trackedLevelNameText = null;
            }
        }

        private static bool IsSongCaptionEmpty()
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
