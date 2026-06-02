
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    public partial class KeyViewer : MonoBehaviour
    {
        private static GUIStyle jkvFoldoutStyle;
        private static GUIStyle FoldoutStyle
        {
            get
            {
                if (jkvFoldoutStyle == null)
                    jkvFoldoutStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
                return jkvFoldoutStyle;
            }
        }

        public void DrawSettingsWindow()
        {
            GUILayout.BeginVertical();


            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            var redTextStyle = new GUIStyle(GUI.skin.button) { normal = { textColor = Color.red } };
            if (GUILayout.Button(I18n.Tr("reset_counts"), redTextStyle, GUILayout.MinWidth(120)))
            {
                lastTotal = -1;
                lastKps = -1;
                Settings.TotalCount = 0;
                for (int i = 0; i < Settings.Count.Length; i++)
                    Settings.Count[i] = 0;
                if (PressTimes != null) PressTimes.Clear();
                if (keyPressTimes != null)
                {
                    for (int i = 0; i < keyPressTimes.Length; i++)
                    {
                        if (keyPressTimes[i] != null)
                            keyPressTimes[i].Clear();
                    }
                }
                if (lastPerKeyKps != null)
                {
                    for (int i = 0; i < lastPerKeyKps.Length; i++)
                        lastPerKeyKps[i] = 0;
                }
                if (Keys != null)
                {
                    for (int i = 0; i < Keys.Length; i++)
                    {
                        if (Keys[i] != null && Keys[i].value != null)
                            Keys[i].value.text = "0";
                    }
                }
                if (Kps != null && Kps.value != null) Kps.value.text = "0";
                if (Total != null && Total.value != null) Total.value.text = "0";
                SaveSettings();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            bool newFormatting = GUILayout.Toggle(Settings.EnableCountFormatting, I18n.Tr("count_formatting"));
            if (newFormatting != Settings.EnableCountFormatting)
            {
                Settings.EnableCountFormatting = newFormatting;
                SaveSettings();
                RefreshAllCountDisplay();
            }
            GUILayout.EndHorizontal();

            bool newSync = GUILayout.Toggle(Settings.SyncToKeyLimiter, I18n.Tr("sync_to_keylimiter"));
            if (newSync != Settings.SyncToKeyLimiter)
            {
                Settings.SyncToKeyLimiter = newSync;
                SaveSettings();
            }


            bool newDownLocation = GUILayout.Toggle(Settings.DownLocation, I18n.Tr("place_below"));
            if (newDownLocation != Settings.DownLocation)
            {
                Settings.DownLocation = newDownLocation;
                ResetKeyViewer();
                ResetFootKeyViewer();
                SaveSettings();
            }

            GUILayout.Space(10);

            bool newCustomPosition = GUILayout.Toggle(Settings.CustomPositionEnabled, I18n.Tr("custom_pos"));
            if (newCustomPosition != Settings.CustomPositionEnabled)
            {
                Settings.CustomPositionEnabled = newCustomPosition;
                SaveSettings();
                if (Settings.CustomPositionEnabled)
                {
                    ResetKeyViewerPosition();
                    ResetFootKeyViewerPosition();
                }
                else
                {
                    ResetKeyViewer();
                    ResetFootKeyViewer();
                }
            }

            if (Settings.CustomPositionEnabled)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label(I18n.Tr("main_key_pos") + ":");
                Vector2 tempMainPos = Settings.MainKeyViewerPosition;
                Vector2 tempFootPos = Settings.FootKeyViewerPosition;
                bool positionChanged = false;

                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(20));
                float newMainX = GUILayout.HorizontalSlider(tempMainPos.x, 0f, 1f, GUILayout.Width(120));
                if (newMainX != tempMainPos.x)
                {
                    tempMainPos.x = newMainX;
                    positionChanged = true;
                }
                string mainXText = GUILayout.TextField(tempMainPos.x.ToString("F2"), FloatFieldWidth(tempMainPos.x.ToString("F2")));
                if (float.TryParse(mainXText, out float parsedMainX) && parsedMainX != tempMainPos.x)
                {
                    tempMainPos.x = Mathf.Clamp01(parsedMainX);
                    positionChanged = true;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Y:", GUILayout.Width(20));
                float newMainY = GUILayout.HorizontalSlider(tempMainPos.y, 0f, 1f, GUILayout.Width(120));
                if (newMainY != tempMainPos.y)
                {
                    tempMainPos.y = newMainY;
                    positionChanged = true;
                }
                string mainYText = GUILayout.TextField(tempMainPos.y.ToString("F2"), FloatFieldWidth(tempMainPos.y.ToString("F2")));
                if (float.TryParse(mainYText, out float parsedMainY) && parsedMainY != tempMainPos.y)
                {
                    tempMainPos.y = Mathf.Clamp01(parsedMainY);
                    positionChanged = true;
                }
                GUILayout.EndHorizontal();

                GUILayout.Label(I18n.Tr("foot_key_pos") + ":");

                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(20));
                float newFootX = GUILayout.HorizontalSlider(tempFootPos.x, 0f, 1f, GUILayout.Width(120));
                if (newFootX != tempFootPos.x)
                {
                    tempFootPos.x = newFootX;
                    positionChanged = true;
                }
                string footXText = GUILayout.TextField(tempFootPos.x.ToString("F2"), FloatFieldWidth(tempFootPos.x.ToString("F2")));
                if (float.TryParse(footXText, out float parsedFootX) && parsedFootX != tempFootPos.x)
                {
                    tempFootPos.x = Mathf.Clamp01(parsedFootX);
                    positionChanged = true;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Y:", GUILayout.Width(20));
                float newFootY = GUILayout.HorizontalSlider(tempFootPos.y, 0f, 1f, GUILayout.Width(120));
                if (newFootY != tempFootPos.y)
                {
                    tempFootPos.y = newFootY;
                    positionChanged = true;
                }
                string footYText = GUILayout.TextField(tempFootPos.y.ToString("F2"), FloatFieldWidth(tempFootPos.y.ToString("F2")));
                if (float.TryParse(footYText, out float parsedFootY) && parsedFootY != tempFootPos.y)
                {
                    tempFootPos.y = Mathf.Clamp01(parsedFootY);
                    positionChanged = true;
                }
                GUILayout.EndHorizontal();

                if (positionChanged)
                {
                    Settings.MainKeyViewerPosition = tempMainPos;
                    Settings.FootKeyViewerPosition = tempFootPos;
                    ResetKeyViewerPosition();
                    ResetFootKeyViewerPosition();
                    SaveSettings();
                }

                if (GUILayout.Button(I18n.Tr("reset_pos")))
                {
                    Settings.MainKeyViewerPosition = new Vector2(0, 1);
                    Settings.FootKeyViewerPosition = new Vector2(0.24f, 1f);
                    ResetKeyViewerPosition();
                    ResetFootKeyViewerPosition();
                    SaveSettings();
                }
                GUILayout.EndVertical();
            }

            GUILayout.Label(I18n.Tr("key_layout") + ":");
            KeyviewerStyle newStyle = (KeyviewerStyle)GUILayout.SelectionGrid((int)Settings.KeyViewerStyle,
                KeyLayoutNames, 3);
            if (newStyle != Settings.KeyViewerStyle)
            {
                Settings.KeyViewerStyle = newStyle;
                ChangeKeyViewer();
                SaveSettings();
            }

            GUILayout.Label(I18n.Tr("foot_keys") + ":");
            FootKeyviewerStyle newFootStyle = (FootKeyviewerStyle)GUILayout.SelectionGrid((int)Settings.FootKeyViewerStyle,
                FootKeyLayoutNames, 5);
            if (newFootStyle != Settings.FootKeyViewerStyle)
            {
                Settings.FootKeyViewerStyle = newFootStyle;
                ResetFootKeyViewer();
                SaveSettings();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(I18n.Tr("size") + ":");
            float newSettingsSize = GUILayout.HorizontalSlider(Settings.Size, 0.1f, 2f, GUILayout.Width(120));
            string sizeText = GUILayout.TextField(newSettingsSize.ToString("F2"), FloatFieldWidth(newSettingsSize.ToString("F2")));
            if (float.TryParse(sizeText, out float parsedSize))
            {
                newSettingsSize = Mathf.Clamp(parsedSize, 0.1f, 2f);
            }
            if (newSettingsSize != Settings.Size)
            {
                Settings.Size = newSettingsSize;
                if (KeyViewerSizeObject != null)
                    KeyViewerSizeObject.transform.localScale = new Vector3(Settings.Size, Settings.Size, 1);
                SaveSettings();
            }
            GUILayout.EndHorizontal();

            DrawFontSizeControls();

            GUILayout.Space(10);

            bool newHideCount = GUILayout.Toggle(Settings.HideMainKeyCount, I18n.Tr("hide_main_count"));
            if (newHideCount != Settings.HideMainKeyCount)
            {
                Settings.HideMainKeyCount = newHideCount;
                ResetKeyViewer();
                SaveSettings();
            }

            if (!Settings.HideMainKeyCount)
            {
                bool newPerKeyKps = GUILayout.Toggle(Settings.EnablePerKeyKps, I18n.Tr("per_key_kps"));
                if (newPerKeyKps != Settings.EnablePerKeyKps)
                {
                    Settings.EnablePerKeyKps = newPerKeyKps;
                    RefreshAllCountDisplay();
                    SaveSettings();
                }
            }

            bool newStreamer = GUILayout.Toggle(Settings.StreamerMode, I18n.Tr("streamer_mode"));
            if (newStreamer != Settings.StreamerMode)
            {
                Settings.StreamerMode = newStreamer;
                if (Kps != null) Kps.gameObject.SetActive(!newStreamer);
                if (Total != null) Total.gameObject.SetActive(!newStreamer);
                SaveSettings();
            }

            GUILayout.Space(10);

            bool newRainEffect = GUILayout.Toggle(Settings.EnableRainEffect, I18n.Tr("rain_effect"));
            if (newRainEffect != Settings.EnableRainEffect)
            {
                Settings.EnableRainEffect = newRainEffect;
                if (!Settings.EnableRainEffect)
                    rainSystem.ClearActiveDrops(Keys);
                SaveSettings();
            }

            if (Settings.EnableRainEffect)
            {
                GUILayout.Label(I18n.Tr("rain_rows") + ":");
                GUILayout.BeginHorizontal();
                Settings.EnableRainForRow1 = GUILayout.Toggle(Settings.EnableRainForRow1, I18n.Tr("rain_row1"));
                Settings.EnableRainForRow2 = GUILayout.Toggle(Settings.EnableRainForRow2, I18n.Tr("rain_row2"));
                if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
                    Settings.EnableRainForRow3 = GUILayout.Toggle(Settings.EnableRainForRow3, I18n.Tr("rain_row3"));
                GUILayout.EndHorizontal();

                GUILayout.Label(I18n.Tr("rain_height") + ":");
                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("rain_row1") + ":");
                Settings.RainHeightRow1 = GUILayout.HorizontalSlider(Settings.RainHeightRow1, 1f, 1000f, GUILayout.Width(120));
                string height1Text = GUILayout.TextField(Settings.RainHeightRow1.ToString("F2"), FloatFieldWidth(Settings.RainHeightRow1.ToString("F2")));
                if (float.TryParse(height1Text, out float newHeight1))
                    Settings.RainHeightRow1 = Mathf.Clamp(newHeight1, 1f, 1000f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("rain_row2") + ":");
                Settings.RainHeightRow2 = GUILayout.HorizontalSlider(Settings.RainHeightRow2, 1f, 1000f, GUILayout.Width(120));
                string height2Text = GUILayout.TextField(Settings.RainHeightRow2.ToString("F2"), FloatFieldWidth(Settings.RainHeightRow2.ToString("F2")));
                if (float.TryParse(height2Text, out float newHeight2))
                    Settings.RainHeightRow2 = Mathf.Clamp(newHeight2, 1f, 1000f);
                GUILayout.EndHorizontal();

                if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(I18n.Tr("rain_row3") + ":");
                    Settings.RainHeightRow3 = GUILayout.HorizontalSlider(Settings.RainHeightRow3, 1f, 1000f, GUILayout.Width(120));
                    string height3Text = GUILayout.TextField(Settings.RainHeightRow3.ToString("F2"), FloatFieldWidth(Settings.RainHeightRow3.ToString("F2")));
                    if (float.TryParse(height3Text, out float newHeight3))
                        Settings.RainHeightRow3 = Mathf.Clamp(newHeight3, 1f, 1000f);
                    GUILayout.EndHorizontal();
                }

                GUILayout.Label(I18n.Tr("rain_speed") + ":");
                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("rain_row1") + ":");
                Settings.RainSpeedRow1 = GUILayout.HorizontalSlider(Settings.RainSpeedRow1, 50f, 1000f, GUILayout.Width(120));
                string speed1Text = GUILayout.TextField(Settings.RainSpeedRow1.ToString("F0"), FloatFieldWidth(Settings.RainSpeedRow1.ToString("F0")));
                if (float.TryParse(speed1Text, out float newSpeed1))
                    Settings.RainSpeedRow1 = Mathf.Clamp(newSpeed1, 50f, 1000f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("rain_row2") + ":");
                Settings.RainSpeedRow2 = GUILayout.HorizontalSlider(Settings.RainSpeedRow2, 50f, 1000f, GUILayout.Width(120));
                string speed2Text = GUILayout.TextField(Settings.RainSpeedRow2.ToString("F0"), FloatFieldWidth(Settings.RainSpeedRow2.ToString("F0")));
                if (float.TryParse(speed2Text, out float newSpeed2))
                    Settings.RainSpeedRow2 = Mathf.Clamp(newSpeed2, 50f, 1000f);
                GUILayout.EndHorizontal();

                if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(I18n.Tr("rain_row3") + ":");
                    Settings.RainSpeedRow3 = GUILayout.HorizontalSlider(Settings.RainSpeedRow3, 50f, 1000f, GUILayout.Width(120));
                    string speed3Text = GUILayout.TextField(Settings.RainSpeedRow3.ToString("F0"), FloatFieldWidth(Settings.RainSpeedRow3.ToString("F0")));
                    if (float.TryParse(speed3Text, out float newSpeed3))
                        Settings.RainSpeedRow3 = Mathf.Clamp(newSpeed3, 50f, 1000f);
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(5);
                bool newRainFade = GUILayout.Toggle(Settings.EnableRainFade, I18n.Tr("rain_fade"));
                if (newRainFade != Settings.EnableRainFade)
                {
                    Settings.EnableRainFade = newRainFade;
                    if (newRainFade)
                        Settings.EnableRainOpacityGradient = false;
                    if (rainSystem != null && Keys != null)
                    {
                        rainSystem.ClearActiveDrops(Keys);
                    }
                    SaveSettings();
                }
                if (Settings.EnableRainFade)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(I18n.Tr("fade_duration") + ":", GUILayout.Width(120));
                    float newFadeDur = GUILayout.HorizontalSlider(Settings.RainFadeDuration, 0.03f, 5.0f, GUILayout.Width(200));
                    string fadeDurText = GUILayout.TextField(newFadeDur.ToString("F2"), FloatFieldWidth(newFadeDur.ToString("F2")));
                    if (float.TryParse(fadeDurText, out float parsedFade))
                        newFadeDur = Mathf.Clamp(parsedFade, 0.03f, 5.0f);
                    if (newFadeDur != Settings.RainFadeDuration)
                    {
                        Settings.RainFadeDuration = newFadeDur;
                        SaveSettings();
                    }
                    GUILayout.EndHorizontal();
                }

                bool newOpacityGradient = GUILayout.Toggle(Settings.EnableRainOpacityGradient, I18n.Tr("release_opacity_gradient"));
                if (newOpacityGradient != Settings.EnableRainOpacityGradient)
                {
                    Settings.EnableRainOpacityGradient = newOpacityGradient;
                    if (newOpacityGradient)
                        Settings.EnableRainFade = false;
                    if (rainSystem != null && Keys != null)
                        rainSystem.ClearActiveDrops(Keys);
                    SaveSettings();
                }
                if (Settings.EnableRainOpacityGradient)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(I18n.Tr("opacity_gradient_length") + ":", GUILayout.Width(120));
                    float newGradientLength = GUILayout.HorizontalSlider(Settings.RainOpacityGradientLength, 1f, 300f, GUILayout.Width(200));
                    string gradientLengthText = GUILayout.TextField(newGradientLength.ToString("F0"), FloatFieldWidth(newGradientLength.ToString("F0")));
                    if (float.TryParse(gradientLengthText, out float parsedGradientLength))
                        newGradientLength = Mathf.Clamp(parsedGradientLength, 1f, 300f);
                    if (newGradientLength != Settings.RainOpacityGradientLength)
                    {
                        Settings.RainOpacityGradientLength = newGradientLength;
                        SaveSettings();
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(5);
                bool newGhostRain = GUILayout.Toggle(Settings.EnableGhostRain, I18n.Tr("ghost_rain"));
                if (newGhostRain != Settings.EnableGhostRain)
                {
                    Settings.EnableGhostRain = newGhostRain;
                    if (!newGhostRain && rainSystem != null && Keys != null)
                        rainSystem.ClearActiveDrops(Keys);
                    SaveSettings();
                }
            }

            GUILayout.Space(10);

            KeyChangeExpanded = GUILayout.Toggle(KeyChangeExpanded, (KeyChangeExpanded ? "\u25E2 " : "\u25B6 ") + I18n.Tr("key_change"), FoldoutStyle);
            if (KeyChangeExpanded)
                DrawKeyChangeSection();

            if (Settings.EnableRainEffect && Settings.EnableGhostRain)
            {
                GhostRainChangeExpanded = GUILayout.Toggle(GhostRainChangeExpanded, (GhostRainChangeExpanded ? "◢ " : "▶ ") + I18n.Tr("ghost_rain"), FoldoutStyle);
                if (GhostRainChangeExpanded)
                    DrawGhostKeyChangeSection();
            }

            TextChangeExpanded = GUILayout.Toggle(TextChangeExpanded, (TextChangeExpanded ? "\u25E2 " : "\u25B6 ") + I18n.Tr("text_change"), FoldoutStyle);
            if (TextChangeExpanded)
                DrawTextChangeSection();

            GUILayout.Space(5);

            bool colorsExpanded = GUILayout.Toggle(ColorExpanded != null, (ColorExpanded != null ? "\u25E2 " : "\u25B6 ") + I18n.Tr("colors"), FoldoutStyle);
            if (colorsExpanded && ColorExpanded == null) ColorExpanded = new bool[10];
            if (!colorsExpanded) ColorExpanded = null;
            if (ColorExpanded != null)
            {
                bool pk = GUILayout.Toggle(Settings.EnablePerKeyColors, I18n.Tr("per_key_colors"));
                if (pk != Settings.EnablePerKeyColors)
                {
                    Settings.EnablePerKeyColors = pk;
                    ResetKeyViewer();
                    UpdateAllKeyColors();
                    SaveSettings();
                }
                bool separateKpsTotal = GUILayout.Toggle(Settings.EnableSeparateKpsTotalColors, I18n.Tr("separate_kps_total_colors"));
                if (separateKpsTotal != Settings.EnableSeparateKpsTotalColors)
                {
                    Settings.EnableSeparateKpsTotalColors = separateKpsTotal;
                    if (!separateKpsTotal)
                    {
                        kpsColorType = -1;
                        totalColorType = -1;
                        if (perKeyColorSelected >= 36) perKeyColorSelected = -1;
                    }
                    ResetKeyViewer();
                    UpdateAllKeyColors();
                    SaveSettings();
                }
                if (Settings.EnablePerKeyColors)
                    DrawPerKeyColorSettings();
                else
                    DrawColorSettings();
            }

            GUILayout.EndVertical();
        }

        private int perKeyFontSizeSelected = -1;

        private void DrawFontSizeControls()
        {
            Settings.EnsurePerKeyFontSizes();
            bool newPerKey = GUILayout.Toggle(Settings.EnablePerKeyFontSizes, I18n.Tr("per_key_font_size"));
            if (newPerKey != Settings.EnablePerKeyFontSizes)
            {
                Settings.EnablePerKeyFontSizes = newPerKey;
                ApplyFontSize();
                SaveSettings();
            }

            if (Settings.EnablePerKeyFontSizes)
            {
                DrawPerKeyFontSizeSettings();
                return;
            }

            bool changed = false;
            float newKeyFontSize = DrawFontSizeSlider(I18n.Tr("key_font_size"), Settings.KeyFontSize);
            if (newKeyFontSize != Settings.KeyFontSize)
            {
                Settings.KeyFontSize = newKeyFontSize;
                changed = true;
            }

            float newCounterFontSize = DrawFontSizeSlider(I18n.Tr("counter_font_size"), Settings.CounterFontSize);
            if (newCounterFontSize != Settings.CounterFontSize)
            {
                Settings.CounterFontSize = newCounterFontSize;
                changed = true;
            }

            bool spacingChanged = false;
            float newCounterSpacing = DrawCounterSpacingSlider(I18n.Tr("counter_spacing"), Settings.CounterTextSpacing);
            if (newCounterSpacing != Settings.CounterTextSpacing)
            {
                Settings.CounterTextSpacing = newCounterSpacing;
                spacingChanged = true;
                changed = true;
            }

            if (changed)
            {
                Settings.ResetPerKeyFontSizesToGlobal();
                if (spacingChanged)
                    ResetKeyViewer();
                else
                    ApplyFontSize();
                SaveSettings();
            }
        }

        private static float DrawFontSizeSlider(string label, float current)
        {
            current = Mathf.Clamp(current <= 0f ? 1f : current, 0.1f, 3f);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":");
            float next = GUILayout.HorizontalSlider(current, 0.1f, 3f, GUILayout.Width(120));
            string text = GUILayout.TextField(next.ToString("F2"), FloatFieldWidth(next.ToString("F2")));
            if (float.TryParse(text, out float parsed))
                next = Mathf.Clamp(parsed, 0.1f, 3f);
            GUILayout.EndHorizontal();
            return next;
        }

        private static float DrawCounterSpacingSlider(string label, float current)
        {
            current = Mathf.Clamp(current, -12f, 32f);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":");
            float next = GUILayout.HorizontalSlider(current, -12f, 32f, GUILayout.Width(120));
            string text = GUILayout.TextField(next.ToString("F1"), FloatFieldWidth(next.ToString("F1")));
            if (float.TryParse(text, out float parsed))
                next = Mathf.Clamp(parsed, -12f, 32f);
            GUILayout.EndHorizontal();
            return next;
        }

        private void DrawPerKeyFontSizeSettings()
        {
            if (!IsFontSizeSlotActive(perKeyFontSizeSelected))
                perKeyFontSizeSelected = -1;

            GUILayout.BeginVertical("box");
            KeyCode[] keyCodes = GetKeyCode();
            KeyCode[] footKeyCodes = GetFootKeyCode();

            void KeyBtn(int idx, string label)
            {
                Color prev = GUI.backgroundColor;
                if (perKeyFontSizeSelected == idx)
                    GUI.backgroundColor = new Color(0.55f, 0.75f, 1f, 1f);
                if (GUILayout.Button(label))
                    perKeyFontSizeSelected = perKeyFontSizeSelected == idx ? -1 : idx;
                GUI.backgroundColor = prev;
            }

            GUILayout.Label(I18n.Tr("row1_keys") + ":");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 8 && i < keyCodes.Length; i++)
                KeyBtn(i, KeyToString(keyCodes[i]));
            GUILayout.EndHorizontal();

            byte[] backSequence = GetBackSequence();
            if (backSequence.Length > 0)
            {
                GUILayout.Label(I18n.Tr("row2_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int b = 0; b < backSequence.Length && b < 8; b++)
                    KeyBtn(backSequence[b], KeyToString(keyCodes[backSequence[b]]));
                GUILayout.EndHorizontal();
            }

            if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
            {
                GUILayout.Label(I18n.Tr("row3_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int b = 8; b < backSequence.Length; b++)
                {
                    int i = backSequence[b];
                    if (i < keyCodes.Length)
                        KeyBtn(i, KeyToString(keyCodes[i]));
                }
                GUILayout.EndHorizontal();
            }

            if (footKeyCodes != null && footKeyCodes.Length > 0)
            {
                GUILayout.Label(I18n.Tr("foot_keys") + ":");
                int rows = footKeyCodes.Length <= 8 ? 1 : 2;
                for (int r = 0; r < rows; r++)
                {
                    GUILayout.BeginHorizontal();
                    int start = r * 8;
                    int end = Mathf.Min(start + 8, footKeyCodes.Length);
                    for (int f = start; f < end; f++)
                        KeyBtn(20 + f, KeyToString(footKeyCodes[f]));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            KeyBtn(36, "KPS");
            KeyBtn(37, "Total");
            GUILayout.EndHorizontal();

            if (perKeyFontSizeSelected >= 0)
            {
                int s = perKeyFontSizeSelected;
                string keyLabel = s == 36 ? "KPS" : s == 37 ? "Total" : KeyToString(GetKeyCodeForIndex(s));
                GUILayout.Space(5);
                GUILayout.Label(I18n.Tr("selected_key") + ": " + keyLabel);

                float newKeySize = DrawFontSizeSlider(I18n.Tr("key_font_size"), Settings.PerKeyFontSize[s]);
                float newCounterSize = DrawFontSizeSlider(I18n.Tr("counter_font_size"), Settings.PerKeyCounterFontSize[s]);
                if (newKeySize != Settings.PerKeyFontSize[s] || newCounterSize != Settings.PerKeyCounterFontSize[s])
                {
                    Settings.PerKeyFontSize[s] = newKeySize;
                    Settings.PerKeyCounterFontSize[s] = newCounterSize;
                    ApplyFontSize();
                    SaveSettings();
                }

                if (GUILayout.Button(I18n.Tr("reset_font_size")))
                {
                    Settings.PerKeyFontSize[s] = Settings.KeyFontSize;
                    Settings.PerKeyCounterFontSize[s] = Settings.CounterFontSize;
                    ApplyFontSize();
                    SaveSettings();
                }
            }

            if (GUILayout.Button(I18n.Tr("reset_all_font_sizes")))
            {
                Settings.ResetPerKeyFontSizesToGlobal();
                ApplyFontSize();
                SaveSettings();
            }
            GUILayout.EndVertical();
        }

        private static bool IsFontSizeSlotActive(int slot)
        {
            if (slot < 0) return false;
            if (slot == 36 || slot == 37) return true;
            if (slot < 20)
            {
                KeyCode[] keyCodes = GetKeyCode();
                return keyCodes != null && slot < keyCodes.Length;
            }

            KeyCode[] footKeyCodes = GetFootKeyCode();
            int footIndex = slot - 20;
            return footKeyCodes != null && footIndex >= 0 && footIndex < footKeyCodes.Length;
        }

        private void DrawKeyChangeSection()
        {
            GUILayout.BeginVertical("box");
            KeyCode[] keyCodes = GetKeyCode();

            GUILayout.Label(I18n.Tr("row1_keys") + ":");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 8; i++)
            {
                if (GUILayout.Button(KeyToString(keyCodes[i])))
                {
                    SelectedKey = i;
                    changeState = 0;
                }
            }
            GUILayout.EndHorizontal();

            byte[] backSequence = GetBackSequence();
            if (backSequence.Length > 0)
            {
                GUILayout.Label(I18n.Tr("row2_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int i = 0; i < backSequence.Length && i < 8; i++)
                {
                    if (GUILayout.Button(KeyToString(keyCodes[backSequence[i]])))
                    {
                        SelectedKey = backSequence[i];
                        changeState = 0;
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
            {
                GUILayout.Label(I18n.Tr("row3_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int b = 8; b < backSequence.Length; b++)
                {
                    int i = backSequence[b];
                    if (i < keyCodes.Length)
                    {
                        if (GUILayout.Button(KeyToString(keyCodes[i])))
                        {
                            SelectedKey = i;
                            changeState = 0;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            KeyCode[] footKeyCodes = GetFootKeyCode();
            if (footKeyCodes != null && footKeyCodes.Length > 0)
            {
                GUILayout.Label(I18n.Tr("foot_keys_list") + ":");
                if (footKeyCodes.Length <= 8)
                {
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < footKeyCodes.Length; i++)
                    {
                        if (GUILayout.Button(KeyToString(footKeyCodes[i])))
                        {
                            SelectedKey = i + 20;
                            changeState = 0;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < 8; i++)
                    {
                        if (GUILayout.Button(KeyToString(footKeyCodes[i])))
                        {
                            SelectedKey = i + 20;
                            changeState = 0;
                        }
                    }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
                    int remaining = footKeyCodes.Length - 8;
                    for (int s = 0; s < 8 - remaining; s++)
                        GUILayout.FlexibleSpace();
                    for (int i = 8; i < footKeyCodes.Length; i++)
                    {
                        if (GUILayout.Button(KeyToString(footKeyCodes[i])))
                        {
                            SelectedKey = i + 20;
                            changeState = 0;
                        }
                    }
                    for (int s = 0; s < 8 - remaining; s++)
                        GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }

            if (SelectedKey != -1 && changeState == 0)
                GUILayout.Label("<b>" + I18n.Tr("press_new_key") + "</b>");
            GUILayout.EndVertical();
        }

        private void DrawGhostKeyChangeSection()
        {
            GUILayout.BeginVertical("box");
            KeyCode[] ghostKeyCodes = GetGhostKeyCode();

            GUILayout.Label(I18n.Tr("row1_keys") + ":");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 8; i++)
                DrawGhostKeyButton(i, ghostKeyCodes);
            GUILayout.EndHorizontal();

            byte[] backSequence = GetBackSequence();
            if (backSequence.Length > 0)
            {
                GUILayout.Label(I18n.Tr("row2_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int i = 0; i < backSequence.Length && i < 8; i++)
                    DrawGhostKeyButton(backSequence[i], ghostKeyCodes);
                GUILayout.EndHorizontal();
            }

            if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
            {
                GUILayout.Label(I18n.Tr("row3_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int b = 8; b < backSequence.Length; b++)
                    DrawGhostKeyButton(backSequence[b], ghostKeyCodes);
                GUILayout.EndHorizontal();
            }

            if (SelectedKey != -1 && changeState == 2)
                GUILayout.Label("<b>" + I18n.Tr("press_new_key") + "</b>");
            GUILayout.EndVertical();
        }

        private void DrawGhostKeyButton(int i, KeyCode[] ghostKeyCodes)
        {
            bool isBound = ghostKeyCodes[i] != KeyCode.None;
            string label = isBound ? KeyToString(ghostKeyCodes[i]) : "-";
            bool selected = i == SelectedKey && changeState == 2;
            if (GUILayout.Button(selected ? "<b>" + label + "</b>" : label))
            {
                if (isBound)
                {
                    ghostKeyCodes[i] = KeyCode.None;
                    SelectedKey = -1;
                    SaveSettings();
                }
                else
                {
                    SelectedKey = i;
                    changeState = 2;
                }
            }
        }

        private void DrawTextChangeSection()
        {
            GUILayout.BeginVertical("box");
            KeyCode[] keyCodes = GetKeyCode();
            string[] keyTexts = GetKeyText();

            GUILayout.Label(I18n.Tr("row1_text") + ":");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 8; i++)
            {
                string buttonText = !string.IsNullOrEmpty(keyTexts[i]) ? keyTexts[i] : KeyToString(keyCodes[i]);
                if (GUILayout.Button(buttonText))
                {
                    SelectedKey = i;
                    changeState = 1;
                }
            }
            GUILayout.EndHorizontal();

            byte[] backSequence = GetBackSequence();
            if (backSequence.Length > 0)
            {
                GUILayout.Label(I18n.Tr("row2_text") + ":");
                GUILayout.BeginHorizontal();
                for (int i = 0; i < backSequence.Length && i < 8; i++)
                {
                    int keyIndex = backSequence[i];
                    string buttonText = !string.IsNullOrEmpty(keyTexts[keyIndex]) ? keyTexts[keyIndex] : KeyToString(keyCodes[keyIndex]);
                    if (GUILayout.Button(buttonText))
                    {
                        SelectedKey = keyIndex;
                        changeState = 1;
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
            {
                GUILayout.Label(I18n.Tr("row3_text") + ":");
                GUILayout.BeginHorizontal();
                for (int b = 8; b < backSequence.Length; b++)
                {
                    int i = backSequence[b];
                    if (i < keyTexts.Length)
                    {
                        string buttonText = !string.IsNullOrEmpty(keyTexts[i]) ? keyTexts[i] : KeyToString(keyCodes[i]);
                        if (GUILayout.Button(buttonText))
                        {
                            SelectedKey = i;
                            changeState = 1;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            KeyCode[] footKeyCodes = GetFootKeyCode();
            string[] footKeyTexts = GetFootKeyText();
            if (footKeyCodes != null && footKeyCodes.Length > 0)
            {
                GUILayout.Label(I18n.Tr("foot_keys_text") + ":");
                if (footKeyCodes.Length <= 8)
                {
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < footKeyCodes.Length; i++)
                    {
                        string buttonText = !string.IsNullOrEmpty(footKeyTexts[i]) ? footKeyTexts[i] : KeyToString(footKeyCodes[i]);
                        if (GUILayout.Button(buttonText))
                        {
                            SelectedKey = i + 20;
                            changeState = 1;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < 8; i++)
                    {
                        string buttonText = !string.IsNullOrEmpty(footKeyTexts[i]) ? footKeyTexts[i] : KeyToString(footKeyCodes[i]);
                        if (GUILayout.Button(buttonText))
                        {
                            SelectedKey = i + 20;
                            changeState = 1;
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    int remaining = footKeyCodes.Length - 8;
                    for (int s = 0; s < 8 - remaining; s++)
                        GUILayout.FlexibleSpace();
                    for (int i = 8; i < footKeyCodes.Length; i++)
                    {
                        string buttonText = !string.IsNullOrEmpty(footKeyTexts[i]) ? footKeyTexts[i] : KeyToString(footKeyCodes[i]);
                        if (GUILayout.Button(buttonText))
                        {
                            SelectedKey = i + 20;
                            changeState = 1;
                        }
                    }
                    for (int s = 0; s < 8 - remaining; s++)
                        GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }

            if (SelectedKey != -1 && changeState == 1)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("input_text") + ":");
                if (SelectedKey < 20)
                {
                    string currentText = !string.IsNullOrEmpty(keyTexts[SelectedKey]) ? keyTexts[SelectedKey] : KeyToString(keyCodes[SelectedKey]);
                    string newText = GUILayout.TextField(currentText, GUILayout.Width(150));
                    if (keyTexts[SelectedKey] != newText)
                    {
                        if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
                            Keys[SelectedKey].text.text = newText;
                        keyTexts[SelectedKey] = string.IsNullOrEmpty(newText) || newText == KeyToString(keyCodes[SelectedKey]) ? null : newText;
                    }
                }
                else
                {
                    int footIndex = SelectedKey - 20;
                    string currentText = footKeyTexts != null && !string.IsNullOrEmpty(footKeyTexts[footIndex])
                        ? footKeyTexts[footIndex] : KeyToString(footKeyCodes[footIndex]);
                    string newText = GUILayout.TextField(currentText, GUILayout.Width(150));
                    if (footKeyTexts[footIndex] != newText)
                    {
                        if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
                            Keys[SelectedKey].text.text = newText;
                        footKeyTexts[footIndex] = string.IsNullOrEmpty(newText) || newText == KeyToString(footKeyCodes[footIndex]) ? null : newText;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(I18n.Tr("reset")))
                {
                    if (SelectedKey < 20)
                    {
                        keyTexts[SelectedKey] = null;
                        if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
                            Keys[SelectedKey].text.text = KeyToString(keyCodes[SelectedKey]);
                    }
                    else
                    {
                        int footIndex = SelectedKey - 20;
                        footKeyTexts[footIndex] = null;
                        if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
                            Keys[SelectedKey].text.text = KeyToString(footKeyCodes[footIndex]);
                    }
                    SelectedKey = -1;
                    SaveSettings();
                }
                if (GUILayout.Button(I18n.Tr("save_btn")))
                {
                    SelectedKey = -1;
                    SaveSettings();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawColorSettings()
        {
            GUILayout.BeginVertical("box");
            string[] colorNames = {
                I18n.Tr("color_bg"), I18n.Tr("color_bg_clicked"), I18n.Tr("color_outline"), I18n.Tr("color_outline_clicked"),
                I18n.Tr("color_text"), I18n.Tr("color_text_clicked"),
                I18n.Tr("color_rain1"), I18n.Tr("color_rain2"), I18n.Tr("color_rain3"),
                I18n.Tr("ghost_rain_color")
            };
            Color[] defaultColors = {
                Background, BackgroundClicked, Outline, OutlineClicked,
                Text, TextClicked,
                RainColor, RainColor2, RainColor3,
                new Color(1f, 1f, 1f, 0.4f)
            };
            for (int i = 0; i < 10; i++)
            {
                if (i >= 6 && !Settings.EnableRainEffect)
                    continue;
                if (i == 9 && !Settings.EnableGhostRain)
                    continue;
                ColorExpanded[i] = GUILayout.Toggle(ColorExpanded[i], ColorExpanded[i] ? $"\u25E2 {colorNames[i]}" : $"\u25B6 {colorNames[i]}", FoldoutStyle);
                if (ColorExpanded[i])
                {
                    GUILayout.BeginVertical("box");
                    Color currentColor = GetColorByIndex(i);
                    Color newColor = DrawColorPicker(colorNames[i], currentColor, defaultColors[i]);
                    if (newColor != currentColor)
                    {
                        SetColorByIndex(i, newColor);
                        UpdateAllKeyColors();
                        SaveSettings();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.Space(5);
            if (Settings.EnableSeparateKpsTotalColors)
            {
                DrawKpsTotalColors(36, I18n.Tr("kps_colors"), ref kpsColorType);
                GUILayout.Space(3);
                DrawKpsTotalColors(37, I18n.Tr("total_colors"), ref totalColorType);
            }
            GUILayout.EndVertical();
        }

        int kpsColorType = -1;
        int totalColorType = -1;

        private void DrawKpsTotalColors(int pi, string label, ref int expandedType)
        {
            bool show = GUILayout.Toggle(expandedType >= 0, (expandedType >= 0 ? "◢ " : "▶ ") + label, FoldoutStyle);
            if (show != (expandedType >= 0))
                expandedType = show ? 0 : -1;
            if (expandedType < 0) return;

            string[] typeNames = {
                I18n.Tr("color_bg"), I18n.Tr("color_outline"), I18n.Tr("color_text")
            };
            Color[] defaults = { Background, Outline, Text };

            for (int t = 0; t < 3; t++)
            {
                bool expanded = GUILayout.Toggle(expandedType == t,
                    (expandedType == t ? "◢ " : "▶ ") + typeNames[t], FoldoutStyle);
                if (expanded != (expandedType == t))
                    expandedType = expanded ? t : -1;
                if (expandedType == t)
                {
                    GUILayout.BeginVertical("box");
                    Color cur = pi == 36
                        ? (t switch { 0 => Settings.KpsBackground, 1 => Settings.KpsOutline, _ => Settings.KpsText })
                        : (t switch { 0 => Settings.TotalBackground, 1 => Settings.TotalOutline, _ => Settings.TotalText });
                    Color newColor = DrawColorPicker(typeNames[t], cur, defaults[t]);
                    if (newColor != cur)
                    {
                        if (pi == 36)
                        {
                            if (t == 0) Settings.KpsBackground = newColor;
                            else if (t == 1) Settings.KpsOutline = newColor;
                            else Settings.KpsText = newColor;
                        }
                        else
                        {
                            if (t == 0) Settings.TotalBackground = newColor;
                            else if (t == 1) Settings.TotalOutline = newColor;
                            else Settings.TotalText = newColor;
                        }
                        UpdateAllKeyColors();
                        SaveSettings();
                    }
                    GUILayout.EndVertical();
                }
            }
        }
        private Color DrawColorPicker(string label, Color currentColor, Color defaultColor)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(label);
            void DrawChannel(string name, ref float channel)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(name + ":", GUILayout.Width(20));
                channel = GUILayout.HorizontalSlider(channel, 0f, 1f, GUILayout.Width(150));
                string txt = GUILayout.TextField(channel.ToString("F2"), GUILayout.Width(40));
                if (float.TryParse(txt, out float val))
                    channel = Mathf.Clamp01(val);
                GUILayout.EndHorizontal();
            }
            DrawChannel("R", ref currentColor.r);
            DrawChannel("G", ref currentColor.g);
            DrawChannel("B", ref currentColor.b);
            DrawChannel("A", ref currentColor.a);
            GUILayout.BeginHorizontal();
            GUILayout.Label(I18n.Tr("preview") + ":", GUILayout.Width(40));
            Rect previewRect = GUILayoutUtility.GetRect(100, 20);
            GUIUtils.DrawRect(previewRect, currentColor);
            GUILayout.EndHorizontal();
            if (GUILayout.Button(I18n.Tr("reset_default")))
            {
                currentColor = defaultColor;
            }
            GUILayout.EndVertical();
            return currentColor;
        }

        private int perKeyColorSelected = -1;
        private int perKeyColorTypeIndex = -1;

        private void DrawPerKeyColorSettings()
        {
            GUILayout.BeginVertical("box");
            KeyCode[] keyCodes = GetKeyCode();
            KeyCode[] footKeyCodes = GetFootKeyCode();

            bool pressed;
            void KeyBtn(int idx, string label)
            {
                Color c = Settings.PerKeyBackground[idx];
                var style = new GUIStyle(GUI.skin.button);
                style.normal.textColor = c.grayscale > 0.5f ? Color.black : Color.white;
                if (perKeyColorSelected == idx)
                    GUI.backgroundColor = Color.Lerp(c, Color.white, 0.4f);
                else
                    GUI.backgroundColor = c;
                pressed = GUILayout.Button(label, style);
                GUI.backgroundColor = Color.white;
                if (pressed)
                {
                    if (perKeyColorSelected != idx) perKeyColorTypeIndex = -1;
                    perKeyColorSelected = perKeyColorSelected == idx ? -1 : idx;
                }
            }

            GUILayout.Label(I18n.Tr("row1_keys") + ":");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 8; i++) KeyBtn(i, KeyToString(keyCodes[i]));
            GUILayout.EndHorizontal();

            byte[] backSequence = GetBackSequence();
            if (backSequence.Length > 0)
            {
                GUILayout.Label(I18n.Tr("row2_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int b = 0; b < backSequence.Length && b < 8; b++)
                    KeyBtn(backSequence[b], KeyToString(keyCodes[backSequence[b]]));
                GUILayout.EndHorizontal();
            }

            if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
            {
                GUILayout.Label(I18n.Tr("row3_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int b = 8; b < backSequence.Length; b++)
                {
                    int i = backSequence[b];
                    if (i < keyCodes.Length)
                        KeyBtn(i, KeyToString(keyCodes[i]));
                }
                GUILayout.EndHorizontal();
            }

            if (footKeyCodes != null && footKeyCodes.Length > 0)
            {
                GUILayout.Label(I18n.Tr("foot_keys") + ":");
                int rows = footKeyCodes.Length <= 8 ? 1 : 2;
                for (int r = 0; r < rows; r++)
                {
                    GUILayout.BeginHorizontal();
                    int start = r * 8;
                    int end = Mathf.Min(start + 8, footKeyCodes.Length);
                    for (int f = start; f < end; f++)
                        KeyBtn(20 + f, KeyToString(footKeyCodes[f]));
                    GUILayout.EndHorizontal();
                }
            }

            if (Settings.EnableSeparateKpsTotalColors)
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                KeyBtn(36, "KPS");
                KeyBtn(37, "Total");
                GUILayout.EndHorizontal();
            }

            if (perKeyColorSelected >= 0 && perKeyColorSelected < 38)
            {
                GUILayout.Space(5);
                int s = perKeyColorSelected;
                string keyLabel = s == 36 ? "KPS" : s == 37 ? "Total" : KeyToString(GetKeyCodeForIndex(s));
                GUILayout.Label("Key " + s + " (" + keyLabel + ")");
                string rainKey = s < 8 ? "color_rain1" : s < 16 ? "color_rain2" : s < 20 ? "color_rain3" : "";

                string[] typeNames = {
                    I18n.Tr("color_bg"), I18n.Tr("color_bg_clicked"),
                    I18n.Tr("color_outline"), I18n.Tr("color_outline_clicked"),
                    I18n.Tr("color_text"), I18n.Tr("color_text_clicked"),
                    I18n.Tr(rainKey) + " (" + s + ")"
                };
                Color[] values = {
                    Settings.PerKeyBackground[s], Settings.PerKeyBackgroundClicked[s],
                    Settings.PerKeyOutline[s], Settings.PerKeyOutlineClicked[s],
                    Settings.PerKeyText[s], Settings.PerKeyTextClicked[s],
                    Settings.PerKeyRainColor[s]
                };
                Color[] defaults = {
                    Background, BackgroundClicked, Outline, OutlineClicked, Text, TextClicked, RainColor
                };

                int[] typeOrder = s >= 36 ? new int[] { 0, 2, 4 }
                    : s >= 20 ? new int[] { 0, 1, 2, 3, 4, 5 }
                    : new int[] { 0, 1, 2, 3, 4, 5, 6 };
                int typeCount = typeOrder.Length;

                for (int ti = 0; ti < typeCount; ti++)
                {
                    int t = typeOrder[ti];
                    bool expanded = GUILayout.Toggle(perKeyColorTypeIndex == t,
                        (perKeyColorTypeIndex == t ? "\u25E2 " : "\u25B6 ") + typeNames[t], FoldoutStyle);
                    if (expanded != (perKeyColorTypeIndex == t))
                        perKeyColorTypeIndex = expanded ? t : -1;

                    if (perKeyColorTypeIndex == t)
                    {
                        GUILayout.BeginVertical("box");
                        Color cur = values[t];
                        Color newColor = DrawColorPicker(typeNames[t], cur, defaults[t]);
                        if (newColor != cur)
                        {
                            switch (t)
                            {
                                case 0: Settings.PerKeyBackground[s] = newColor; break;
                                case 1: Settings.PerKeyBackgroundClicked[s] = newColor; break;
                                case 2: Settings.PerKeyOutline[s] = newColor; break;
                                case 3: Settings.PerKeyOutlineClicked[s] = newColor; break;
                                case 4: Settings.PerKeyText[s] = newColor; break;
                                case 5: Settings.PerKeyTextClicked[s] = newColor; break;
                                case 6: Settings.PerKeyRainColor[s] = newColor; break;
                            }
                            UpdateAllKeyColors();
                            SaveSettings();
                        }
                        GUILayout.EndVertical();
                    }
                }

                if (s < 36 && Settings.Count != null && s < Settings.Count.Length)
                {
                    GUILayout.Space(5);
                    var redStyle = new GUIStyle(GUI.skin.button) { normal = { textColor = Color.red } };
                    if (GUILayout.Button(I18n.Tr("reset_counts") + " (" + Settings.Count[s] + ")", redStyle))
                    {
                        Settings.Count[s] = 0;
                        if (keyPressTimes != null && s < keyPressTimes.Length && keyPressTimes[s] != null)
                            keyPressTimes[s].Clear();
                        if (lastPerKeyKps != null && s < lastPerKeyKps.Length)
                            lastPerKeyKps[s] = 0;
                        if (Keys != null && s < Keys.Length && Keys[s] != null && Keys[s].value != null)
                            Keys[s].value.text = "0";
                        SaveSettings();
                    }
                }
            }

            if (GUILayout.Button(I18n.Tr("per_key_color_reset")))
            {
                Settings.InitPerKeyColors();
                UpdateAllKeyColors();
                SaveSettings();
            }
            GUILayout.EndVertical();
        }

        private static KeyCode GetKeyCodeForIndex(int idx)
        {
            KeyCode[] main = GetKeyCode();
            if (main != null && idx < main.Length) return main[idx];
            KeyCode[] foot = GetFootKeyCode();
            int fi = idx - 20;
            if (foot != null && fi >= 0 && fi < foot.Length) return foot[fi];
            return KeyCode.None;
        }

        private string BuildFontStyleSummary()
        {
            int f = Settings.FontStyleFlags;
            if (f == 0) return "Normal";
            var parts = new System.Collections.Generic.List<string>();
            if ((f & 1) != 0) parts.Add("B");
            if ((f & 2) != 0) parts.Add("I");
            if ((f & 4) != 0) parts.Add("U");
            if ((f & 8) != 0) parts.Add("Lc");
            if ((f & 16) != 0) parts.Add("Uc");
            if ((f & 32) != 0) parts.Add("Sc");
            if ((f & 64) != 0) parts.Add("St");
            if ((f & 128) != 0) parts.Add("Sup");
            if ((f & 256) != 0) parts.Add("Sub");
            return string.Join(" ", parts);
        }
    }
}
