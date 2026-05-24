using System;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KorenResourcePack
{
    internal static class Overlay
    {
        
        private static GameObject overlayRoot;
        private static Canvas overlayCanvas;
        private static CanvasScaler overlayScaler;
        private static string hudCachedFpsText;

        private static TextMeshProUGUI tmpProgress;
        private static TextMeshProUGUI tmpAccuracy;
        private static TextMeshProUGUI tmpXAccuracy;
        private static TextMeshProUGUI tmpMusicTime;
        private static TextMeshProUGUI tmpMapTime;
        private static TextMeshProUGUI tmpCheckpoint;
        private static TextMeshProUGUI tmpBest;
        private static TextMeshProUGUI tmpFps;
        
        private static TextMeshProUGUI tmpTbpm;
        private static TextMeshProUGUI tmpCbpm;
        private static TextMeshProUGUI tmpKps;
        
        private static TextMeshProUGUI tmpCombo;
        private static TextMeshProUGUI tmpComboCaption;
        
        private static readonly TextMeshProUGUI[] tmpJudgement = new TextMeshProUGUI[9];
        
        private static TextMeshProUGUI tmpHold;
        
        private static TextMeshProUGUI tmpAttempt;
        private static TextMeshProUGUI tmpFullAttempt;
        
        private static TextMeshProUGUI tmpTimingScale;

        private static TMP_FontAsset overlayActiveFont;
        private static string overlayActiveFontName;
        internal static bool overlayBuilt;
        private static bool overlayBuildFailed;

        private static readonly Color OverlayShadowColor  = new Color(0f,   0f,   0f,   0.55f);
        private static readonly Color OverlayWhite        = new Color(1f,   1f,   1f,   0.95f);

        private static readonly Color  ShadowColor  = new Color(0f, 0f, 0f, 0.35f);
        private const float            ShadowDilate = 0.0f;   
        private const float            ShadowSoftness = 0.25f; 

        private const float            ShadowOffsetX =  0.5f;
        private const float            ShadowOffsetY = -0.5f;
        private static readonly Vector2 AnchorTopLeft  = new Vector2(0f, 1f);
        private static readonly Vector2 AnchorTopRight = new Vector2(1f, 1f);
        private static readonly Vector2 PivotTopLeft   = new Vector2(0f, 1f);
        private static readonly Vector2 PivotTopRight  = new Vector2(1f, 1f);

        internal static bool TryUseTmpOverlay()
        {
            if (overlayBuildFailed) return false;

            BundleLoader.EnsureBundleLoaded();
            if (!BundleLoader.BundleAvailable && BundleLoader.GetFallbackTmpFont() == null) return false;

            try
            {
                BuildOverlayIfNeeded();
                ApplyFontToOverlay();
                return overlayBuilt;
            }
            catch (Exception ex)
            {
                DestroyOverlay();
                overlayBuildFailed = true;
                Main.mod?.Logger?.Log("[Overlay] TMP overlay failed; falling back to IMGUI: " + ex);
                return false;
            }
        }

        internal static void InvalidateOverlayFontCache()
        {
            overlayActiveFont     = null;
            overlayActiveFontName = null;
            overlayBuildFailed    = false;
        }

        internal static void InvalidateOverlayPercentCaches()
        {
            hudCachedProgress = -999f;
            hudCachedTimingScale = -999f;
        }

        internal static void InvalidateOverlayJudgementHudCache()
        {
            for (int i = 0; i < hudCachedJudgementCount.Length; i++)
                hudCachedJudgementCount[i] = -1;
            hudCachedJudgementXp     = -1;
            hudCachedJudgementPp     = -1;
            hudCachedJudgementMp     = -1;
            hudCachedJudgementXpMode = false;
            hudCachedJudgementFontPx = -1f;
        }

        private static void BuildOverlayIfNeeded()
        {
            if (overlayBuilt && overlayRoot != null) return;

            overlayRoot = new GameObject("KorenResourcePack.Overlay");
            UnityEngine.Object.DontDestroyOnLoad(overlayRoot);

            overlayCanvas             = overlayRoot.AddComponent<Canvas>();
            overlayCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 32700;

            overlayScaler             = overlayRoot.AddComponent<CanvasScaler>();
            overlayScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            overlayScaler.scaleFactor = 1f;

            overlayRoot.AddComponent<GraphicRaycaster>().enabled = false;

            tmpProgress   = NewLabel("Progress",   TextAlignmentOptions.TopLeft);
            tmpAccuracy   = NewLabel("Accuracy",   TextAlignmentOptions.TopLeft);
            tmpXAccuracy  = NewLabel("XAccuracy",  TextAlignmentOptions.TopLeft);
            tmpMusicTime  = NewLabel("MusicTime",  TextAlignmentOptions.TopLeft);
            tmpMapTime    = NewLabel("MapTime",    TextAlignmentOptions.TopLeft);
            tmpCheckpoint = NewLabel("Checkpoint", TextAlignmentOptions.TopLeft);
            tmpBest       = NewLabel("Best",       TextAlignmentOptions.TopLeft);
            tmpFps        = NewLabel("FPS",        TextAlignmentOptions.TopLeft);
            tmpTbpm       = NewLabel("TBPM",       TextAlignmentOptions.TopRight);
            tmpCbpm       = NewLabel("CBPM",       TextAlignmentOptions.TopRight);
            tmpKps        = NewLabel("KPS",        TextAlignmentOptions.TopRight);

            tmpCombo        = NewLabel("Combo",        TextAlignmentOptions.Center);
            tmpComboCaption = NewLabel("ComboCaption", TextAlignmentOptions.Center);

            for (int i = 0; i < tmpJudgement.Length; i++)
                tmpJudgement[i] = NewLabel("Judgement_" + i, TextAlignmentOptions.Center);

            tmpHold        = NewLabel("Hold",        TextAlignmentOptions.MidlineRight);
            tmpAttempt     = NewLabel("Attempt",     TextAlignmentOptions.TopLeft);
            tmpFullAttempt = NewLabel("FullAttempt", TextAlignmentOptions.TopLeft);
            tmpTimingScale = NewLabel("TimingScale", TextAlignmentOptions.Center);

            overlayBuilt = true;
        }

        private static TextMeshProUGUI NewLabel(string name, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(overlayRoot.transform, false);

            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.alignment          = align;
            t.color              = OverlayWhite;
            TmpCompatibility.DisableWordWrapping(t);
            t.overflowMode       = TextOverflowModes.Overflow;
            t.raycastTarget      = false;
            t.richText           = true;
            t.text               = string.Empty;

            EnsureOverlayActiveFont();
            if (overlayActiveFont != null) ApplyFontTo(t);
            TmpCompatibility.TrySetOutline(t, OverlayShadowColor, 0.18f);

            return t;
        }

        private static void EnsureOverlayActiveFont()
        {
            if (overlayActiveFont != null) return;
            try
            {
                string requested = Main.settings != null ? (Main.settings.fontName ?? "") : "";
                TMP_FontAsset fa = BundleLoader.GetBestTmpFont(requested);
                if (fa == null) return;
                overlayActiveFont = fa;
                overlayActiveFontName = requested;
            }
            catch { }
        }

        private static void ApplyShadowToMaterial(Material mat)
        {
            TextShadows.ApplyTmpDropShadow(mat, ShadowColor, ShadowOffsetX, ShadowOffsetY, ShadowSoftness, ShadowDilate);
        }

        private static void ApplyFontToOverlay()
        {
            if (!overlayBuilt) return;
            string requested = Main.settings != null ? (Main.settings.fontName ?? "") : "";
            if (requested == overlayActiveFontName && overlayActiveFont != null) return;

            TMP_FontAsset fa = BundleLoader.GetBestTmpFont(requested);
            if (fa == null) return;
            overlayActiveFont     = fa;
            overlayActiveFontName = requested;

            ApplyFontTo(tmpProgress);  ApplyFontTo(tmpAccuracy);  ApplyFontTo(tmpXAccuracy);
            ApplyFontTo(tmpMusicTime); ApplyFontTo(tmpMapTime);    ApplyFontTo(tmpCheckpoint);
            ApplyFontTo(tmpBest);      ApplyFontTo(tmpFps);        ApplyFontTo(tmpTbpm);
            ApplyFontTo(tmpCbpm);      ApplyFontTo(tmpKps);
            ApplyFontTo(tmpCombo);     ApplyFontTo(tmpComboCaption);
            for (int i = 0; i < tmpJudgement.Length; i++) ApplyFontTo(tmpJudgement[i]);
            ApplyFontTo(tmpHold);
            ApplyFontTo(tmpAttempt); ApplyFontTo(tmpFullAttempt);
            ApplyFontTo(tmpTimingScale);
        }

        private static void ApplyFontTo(TextMeshProUGUI t)
        {
            if (t == null || overlayActiveFont == null) return;
            t.font = overlayActiveFont;
            Material sharedMaterial = BundleLoader.GetBundleFontMaterial(overlayActiveFont);
            TmpCompatibility.SetFontSharedMaterial(t, sharedMaterial);
            
            TmpCompatibility.TrySetOutline(t, OverlayShadowColor, 0.18f);
            
            ApplyShadowToMaterial(TmpCompatibility.GetFontMaterial(t));
            TmpCompatibility.RefreshTextRendering(t);
        }

        internal static void HideOverlay()
        {
            if (overlayRoot != null) overlayRoot.SetActive(false);
        }

        internal static void ShowOverlay()
        {
            if (overlayRoot != null) overlayRoot.SetActive(true);
        }

        internal static void DestroyOverlay()
        {
            try
            {
                if (overlayRoot != null) UnityEngine.Object.Destroy(overlayRoot);
            }
            catch { }
            overlayRoot    = null; overlayCanvas  = null; overlayScaler = null;
            tmpProgress    = tmpAccuracy = tmpXAccuracy = tmpMusicTime = tmpMapTime = tmpCheckpoint = tmpBest = tmpFps = null;
            tmpTbpm        = tmpCbpm = tmpKps = null;
            tmpCombo       = tmpComboCaption = null;
            for (int i = 0; i < tmpJudgement.Length; i++) tmpJudgement[i] = null;
            tmpHold        = null;
            tmpAttempt     = tmpFullAttempt = null;
            tmpTimingScale = null;
            overlayActiveFont     = null;
            overlayActiveFontName = null;
            overlayBuilt          = false;
            overlayBuildFailed    = false;
            InvalidateOverlayCaches();
        }

        private static void InvalidateOverlayCaches()
        {
            hudCachedCp       = -1;
            hudCachedProgress = -999f;
            hudCachedProgressText = null;
            hudCachedAccuracyRaw = null; hudCachedAccuracyText = null;
            hudCachedXAccuracyRaw = null; hudCachedXAccuracyText = null;
            hudCachedMusicTimeRaw = null; hudCachedMusicTimeText = null;
            hudCachedMapTimeRaw = null; hudCachedMapTimeText = null;
            hudCachedCheckpointText = null;
            hudCachedBestRaw = null; hudCachedBestText = null;
            hudCachedTBpmRaw = -1f;
            hudCachedCBpmRaw = -1f;
            hudCachedTbpmText = null;
            hudCachedCbpmText = null;
            hudCachedKpsText = null;
            hudCachedCombo = -1;
            hudCachedFpsText = null;
            hudCachedAttemptDisplay = -1;
            hudCachedFullAttemptDisplay = -1;
            hudCachedAttemptText = null;
            hudCachedFullAttemptText = null;
            hudCachedTimingScale = -999f;
            hudCachedTimingScaleText = null;
            InvalidateOverlayJudgementHudCache();
        }

        private static int hudFrameW;
        private static int hudFrameH;
        
        private static float hudTickSizeMult = 1f;
        private static float hudTickStatusFontPx;
        private static float hudTickBpmFontPx;

        internal static void TickOverlay(float progress)
        {
            if (!overlayBuilt) return;

            hudFrameW = Screen.width;
            hudFrameH = Screen.height;
            hudTickSizeMult = (Main.settings != null) ? Mathf.Clamp(Main.settings.size, 0.3f, 3f) : 1f;
            float h = hudFrameH;
            
            float statusBase = h * 0.030f * hudTickSizeMult;
            hudTickStatusFontPx = statusBase < 18f ? 18f : statusBase;
            hudTickBpmFontPx = hudTickStatusFontPx;

            UpdateStatusBlock(progress);
            UpdateBpmBlock();
            UpdateComboElement();
            UpdateJudgementElements();
            UpdateHoldElement();
            UpdateAttemptElements();
            UpdateTimingScaleElement();
        }

        private static int   hudCachedCp       = -1;
        private static float hudCachedProgress = -1f;
        private static string hudCachedProgressText;
        private static string hudCachedAccuracyRaw;
        private static string hudCachedAccuracyText;
        private static string hudCachedXAccuracyRaw;
        private static string hudCachedXAccuracyText;
        private static string hudCachedMusicTimeRaw;
        private static string hudCachedMusicTimeText;
        private static string hudCachedMapTimeRaw;
        private static string hudCachedMapTimeText;
        private static string hudCachedCheckpointText;
        private static string hudCachedBestRaw;
        private static string hudCachedBestText;

        private static void UpdateStatusBlock(float progress)
        {
            Settings s = Main.settings;
            if (!s.statusOn)
            {
                SetEnabled(tmpProgress, false);
                SetEnabled(tmpAccuracy, false);
                SetEnabled(tmpXAccuracy, false);
                SetEnabled(tmpMusicTime, false);
                SetEnabled(tmpMapTime, false);
                SetEnabled(tmpCheckpoint, false);
                SetEnabled(tmpBest, false);
                SetEnabled(tmpFps, false);
                return;
            }

            float fontPx = hudTickStatusFontPx;
            float lineH  = fontPx + hudFrameH * 0.006f;
            float leftX  = hudFrameW * 0.012f;
            float topY   = hudFrameH * 0.013f;
            int   row    = 0;

            if (s.ShowProgress)
            {
                if (Mathf.Abs(progress - hudCachedProgress) > 0.0001f || hudCachedProgressText == null)
                {
                    hudCachedProgress = progress;
                    hudCachedProgressText = Status.FormatStatusLine("Progress", Status.FormatProgressRange(progress));
                }
                SetText(tmpProgress, hudCachedProgressText);
                SetColor(tmpProgress, Status.GetProgressColor(progress));
                ConfigureLine(tmpProgress, leftX, topY + lineH * row++, fontPx, false);
                SetEnabled(tmpProgress, true);
            }
            else SetEnabled(tmpProgress, false);

            if (s.ShowAccuracy)
            {
                string raw = Status.GetAccuracyText();
                if (!ReferenceEquals(raw, hudCachedAccuracyRaw) && raw != hudCachedAccuracyRaw)
                {
                    hudCachedAccuracyRaw = raw;
                    hudCachedAccuracyText = Status.FormatStatusLine("Accuracy", raw);
                }
                SetText(tmpAccuracy, hudCachedAccuracyText);
                SetColor(tmpAccuracy, Status.GetAccuracyColor());
                ConfigureLine(tmpAccuracy, leftX, topY + lineH * row++, fontPx, false);
                SetEnabled(tmpAccuracy, true);
            }
            else SetEnabled(tmpAccuracy, false);

            if (s.ShowXAccuracy)
            {
                string raw = Status.GetXAccuracyText();
                if (!ReferenceEquals(raw, hudCachedXAccuracyRaw) && raw != hudCachedXAccuracyRaw)
                {
                    hudCachedXAccuracyRaw = raw;
                    hudCachedXAccuracyText = Status.FormatStatusLine("XAccuracy", raw);
                }
                SetText(tmpXAccuracy, hudCachedXAccuracyText);
                SetColor(tmpXAccuracy, Status.GetXAccuracyColor());
                ConfigureLine(tmpXAccuracy, leftX, topY + lineH * row++, fontPx, false);
                SetEnabled(tmpXAccuracy, true);
            }
            else SetEnabled(tmpXAccuracy, false);

            if (s.ShowMusicTime)
            {
                string raw = Status.GetPrimaryTimeStatusText();
                if (!ReferenceEquals(raw, hudCachedMusicTimeRaw) && raw != hudCachedMusicTimeRaw)
                {
                    hudCachedMusicTimeRaw = raw;
                    hudCachedMusicTimeText = Status.FormatExistingStatusLine(raw);
                }
                SetText(tmpMusicTime, hudCachedMusicTimeText);
                SetColor(tmpMusicTime, Status.GetMusicTimeColor());
                ConfigureLine(tmpMusicTime, leftX, topY + lineH * row++, fontPx, false);
                SetEnabled(tmpMusicTime, true);
            }
            else SetEnabled(tmpMusicTime, false);

            if (s.ShowMapTime)
            {
                string raw = Status.GetMapTimeStatusText();
                if (!ReferenceEquals(raw, hudCachedMapTimeRaw) && raw != hudCachedMapTimeRaw)
                {
                    hudCachedMapTimeRaw = raw;
                    hudCachedMapTimeText = Status.FormatExistingStatusLine(raw);
                }
                SetText(tmpMapTime, hudCachedMapTimeText);
                SetColor(tmpMapTime, Status.GetMapTimeColor());
                ConfigureLine(tmpMapTime, leftX, topY + lineH * row++, fontPx, false);
                SetEnabled(tmpMapTime, true);
            }
            else SetEnabled(tmpMapTime, false);

            if (s.ShowCheckpoint)
            {
                int cp = Status.GetCheckpointCount();
                if (cp != hudCachedCp || hudCachedCheckpointText == null)
                {
                    hudCachedCp = cp;
                    hudCachedCheckpointText = Status.FormatStatusLine("Checkpoints", cp.ToString());
                }
                SetText(tmpCheckpoint, hudCachedCheckpointText);
                SetColor(tmpCheckpoint, OverlayWhite);
                ConfigureLine(tmpCheckpoint, leftX, topY + lineH * row++, fontPx, false);
                SetEnabled(tmpCheckpoint, true);
            }
            else SetEnabled(tmpCheckpoint, false);

            if (s.ShowBest)
            {
                string raw = Status.GetBestText();
                if (!ReferenceEquals(raw, hudCachedBestRaw) && raw != hudCachedBestRaw)
                {
                    hudCachedBestRaw = raw;
                    hudCachedBestText = Status.FormatStatusLine("Best", raw);
                }
                SetText(tmpBest, hudCachedBestText);
                SetColor(tmpBest, Status.GetBestColor());
                ConfigureLine(tmpBest, leftX, topY + lineH * row++, fontPx, false);
                SetEnabled(tmpBest, true);
            }
            else SetEnabled(tmpBest, false);

            if (s.ShowFPS)
            {
                string fpsText = Status.GetFpsText();
                if (!ReferenceEquals(fpsText, hudCachedFpsText) && fpsText != hudCachedFpsText)
                {
                    hudCachedFpsText = fpsText;
                    SetText(tmpFps, Status.FormatExistingStatusLine(fpsText));
                }
                SetColor(tmpFps, OverlayWhite);
                ConfigureLine(tmpFps, leftX, topY + lineH * row++, fontPx, false);
                SetEnabled(tmpFps, true);
            }
            else SetEnabled(tmpFps, false);
        }

        private static float hudCachedTBpmRaw = -1f;
        private static float hudCachedCBpmRaw = -1f;
        private static string hudCachedTbpmText;
        private static string hudCachedCbpmText;
        private static string hudCachedKpsText;
        private static readonly System.Globalization.CultureInfo HudInvariant = System.Globalization.CultureInfo.InvariantCulture;

        private static void UpdateBpmBlock()
        {
            if (!Main.settings.bpmOn)
            {
                SetEnabled(tmpTbpm, false);
                SetEnabled(tmpCbpm, false);
                SetEnabled(tmpKps, false);
                return;
            }

            float fontPx = hudTickBpmFontPx;
            float lineH  = fontPx + hudFrameH * 0.006f;
            float blockW = hudFrameW * 0.33f;
            float leftX  = hudFrameW * 0.012f;
            float topY   = hudFrameH * 0.013f;
            float rightX = hudFrameW - leftX;

            float tBpm, cBpm;
            Bpm.GetBpmValues(out tBpm, out cBpm);

            if (Mathf.Abs(tBpm - hudCachedTBpmRaw) > 0.005f || hudCachedTbpmText == null)
            {
                hudCachedTBpmRaw = tBpm;
                hudCachedTbpmText = Status.FormatStatusLine("TBPM", tBpm.ToString("0.##", HudInvariant));
                SetText(tmpTbpm, hudCachedTbpmText);
            }
            if (Mathf.Abs(cBpm - hudCachedCBpmRaw) > 0.005f || hudCachedCbpmText == null)
            {
                hudCachedCBpmRaw = cBpm;
                hudCachedCbpmText = Status.FormatStatusLine("CBPM", cBpm.ToString("0.##", HudInvariant));
                hudCachedKpsText  = Status.FormatStatusLine("KPS",  (cBpm / 60f).ToString("0.##", HudInvariant));
                SetText(tmpCbpm, hudCachedCbpmText);
                SetText(tmpKps,  hudCachedKpsText);
            }
            SetColor(tmpTbpm, Bpm.LerpBpmColor(tBpm));
            Color currentBpmColor = Bpm.LerpBpmColor(cBpm);
            SetColor(tmpCbpm, currentBpmColor);
            SetColor(tmpKps, currentBpmColor);

            ConfigureLine(tmpTbpm, rightX, topY,             fontPx, true, blockW, lineH);
            ConfigureLine(tmpCbpm, rightX, topY + lineH,     fontPx, true, blockW, lineH);
            ConfigureLine(tmpKps,  rightX, topY + lineH * 2f, fontPx, true, blockW, lineH);
            SetEnabled(tmpTbpm, true);
            SetEnabled(tmpCbpm, true);
            SetEnabled(tmpKps, true);
        }

        private static int hudCachedCombo = -1;

        private static void UpdateComboElement()
        {
            if (!Main.settings.comboOn)
            {
                SetEnabled(tmpCombo, false);
                SetEnabled(tmpComboCaption, false);
                return;
            }

            float scaleAnim     = Combo.EvaluateComboScale();
            float valueBaseSize = ScaledFontPx(56, 0.075f);
            float valueSize     = valueBaseSize * scaleAnim;
            float captionSize   = valueSize * 0.35f;
            float centerX       = hudFrameW * 0.5f;
            float heightScale   = hudFrameH / ProgressBar.ProgressBarReferenceHeight;
            float barTop        = ProgressBar.ProgressBarTargetTopOffset * heightScale;
            float barHeight     = ProgressBar.ProgressBarTargetHeight * heightScale;
            float verticalOffset = hudFrameH * 0.030f;
            if (Main.settings.ComboMoveUpNoCaption && LevelName.IsSongCaptionEmpty())
                verticalOffset -= hudFrameH * 0.040f;
            float topY = Mathf.Max(0f, barTop + barHeight + verticalOffset + Main.settings.comboY);

            if (Main.perfectCombo != hudCachedCombo)
            {
                hudCachedCombo = Main.perfectCombo;
                SetText(tmpCombo, Main.perfectCombo.ToString());
            }
            SetFontSize(tmpCombo, valueSize);
            SetColor(tmpCombo, Combo.GetComboColor(Main.perfectCombo));

            ScaleShadowOffset(tmpCombo, valueSize, 0.22f);

            float rectWidth = hudFrameW * 0.4f;
            SetSize(tmpCombo.rectTransform, rectWidth, valueSize + hudFrameH * 0.016f);
            PlaceTopLeft(tmpCombo, centerX - rectWidth * 0.5f, topY);
            SetEnabled(tmpCombo, true);

            if (Main.settings.CaptionText)
            {
                string caption = (Main.settings.XPerfectComboEnabled && XPerfectBridge.Active)
                    ? "XPerfect Combo"
                    : "Perfect Combo";

                SetText(tmpComboCaption, caption);
                SetFontSize(tmpComboCaption, captionSize);
                SetColor(tmpComboCaption, OverlayWhite);
                ScaleShadowOffset(tmpComboCaption, captionSize, 0.22f);
                float spacing = hudFrameH * 0.03f;
                SetSize(tmpComboCaption.rectTransform, rectWidth, captionSize + 8f);
                PlaceTopLeft(tmpComboCaption, centerX - rectWidth * 0.5f,
                             topY + (valueSize + hudFrameH * 0.016f) - spacing - Main.settings.captionY);
                SetEnabled(tmpComboCaption, true);
            }
            else SetEnabled(tmpComboCaption, false);
        }

        private static readonly int[] hudCachedJudgementCount = new int[] { -1,-1,-1,-1,-1,-1,-1,-1,-1 };
        private static readonly float[] hudJudgementWidths = new float[9];
        private static int  hudCachedJudgementXp     = -1;
        private static int  hudCachedJudgementPp     = -1;
        private static int  hudCachedJudgementMp     = -1;
        private static bool hudCachedJudgementXpMode = false;
        private static float hudCachedJudgementFontPx = -1f;

        private static void UpdateJudgementElements()
        {
            if (!Main.settings.judgementOn)
            {
                for (int i = 0; i < tmpJudgement.Length; i++)
                    SetEnabled(tmpJudgement[i], false);
                return;
            }

            float fontPx = ScaledFontPx(14, 0.035f);
            float baseY  = hudFrameH - Mathf.Max(4f, hudFrameH * 0.006f) - fontPx - Main.settings.judgementPositionY;
            float gap    = Mathf.Max(3f, fontPx * 0.07f);
            bool  xpMode = XPerfectBridge.Active;
            int   xc = 0, pc = 0, mc = 0;
            bool  layoutDirty = Mathf.Abs(fontPx - hudCachedJudgementFontPx) > 0.01f;
            if (xpMode)
            {
                xc = XPerfectBridge.XCount();
                pc = XPerfectBridge.PlusCount();
                mc = XPerfectBridge.MinusCount();
            }

            for (int i = 0; i < 9; i++)
            {
                TextMeshProUGUI t = tmpJudgement[i];
                if (i == 4 && xpMode)
                {
                    if (xc != hudCachedJudgementXp || pc != hudCachedJudgementPp ||
                        mc != hudCachedJudgementMp  || !hudCachedJudgementXpMode)
                    {
                        SetText(t, "<color=#60FF4E>" + pc + "</color> <color=#4DCCFF>" + xc +
                                   "</color> <color=#60FF4E>" + mc + "</color>");
                        layoutDirty = true;
                    }
                }
                else
                {
                    int count = Judgement.GetJudgementSlotCount(i);
                    if (count != hudCachedJudgementCount[i] || (i == 4 && hudCachedJudgementXpMode != xpMode))
                    {
                        hudCachedJudgementCount[i] = count;
                        SetText(t, count.ToString());
                        layoutDirty = true;
                    }
                }

                SetColor(t, Judgement.JudgementSlotColors[i]);
                SetFontSize(t, fontPx);
                SetEnabled(t, true);

                ScaleShadowOffset(t, fontPx, 0.3f);
            }

            hudCachedJudgementXp     = xc;
            hudCachedJudgementPp     = pc;
            hudCachedJudgementMp     = mc;
            hudCachedJudgementXpMode = xpMode;

            float   pad   = fontPx * 0.25f;
            float   rowH  = fontPx + hudFrameH * 0.009f;
            if (layoutDirty || hudJudgementWidths[4] <= 0f)
            {
                hudCachedJudgementFontPx = fontPx;
                for (int i = 0; i < 9; i++)
                {
                    TextMeshProUGUI t = tmpJudgement[i];
                    float pref;
                    try { pref = t.GetPreferredValues().x; }
                    catch { pref = (string.IsNullOrEmpty(t.text) ? 1 : t.text.Length) * fontPx * 0.55f; }
                    float w = pref + pad;
                    if (w < fontPx * 0.48f)
                    {
                        int len = string.IsNullOrEmpty(t.text) ? 1 : t.text.Length;
                        w = Mathf.Max(w, fontPx * (0.42f + Mathf.Min(len, 12) * 0.58f));
                    }
                    hudJudgementWidths[i] = w;
                    SetSize(tmpJudgement[i].rectTransform, w, rowH);
                }
            }

            int   pivot     = 4;
            float centerX   = hudFrameW * 0.5f;
            float pivotHalf = hudJudgementWidths[pivot] * 0.5f;
            PlaceTopLeft(tmpJudgement[pivot], centerX - pivotHalf, baseY);

            float cursor = centerX;
            for (int i = pivot - 1; i >= 0; i--)
            {
                float halfCur  = hudJudgementWidths[i]     * 0.5f;
                float halfNext = hudJudgementWidths[i + 1] * 0.5f;
                cursor -= (halfCur + gap + halfNext);
                PlaceTopLeft(tmpJudgement[i], cursor - halfCur, baseY);
            }

            cursor = centerX;
            for (int i = pivot + 1; i < 9; i++)
            {
                float halfCur  = hudJudgementWidths[i]     * 0.5f;
                float halfPrev = hudJudgementWidths[i - 1] * 0.5f;
                cursor += (halfPrev + gap + halfCur);
                PlaceTopLeft(tmpJudgement[i], cursor - halfCur, baseY);
            }
        }

        private static void UpdateHoldElement()
        {
            if (!Main.settings.holdOn)
            {
                SetEnabled(tmpHold, false);
                return;
            }
            string label = Hold.GetHoldBehaviorLabel();
            if (string.IsNullOrEmpty(label))
            {
                SetEnabled(tmpHold, false);
                return;
            }
            float fontPx = ScaledFontPx(16, 0.026f);
            float width  = Mathf.Max(200f, hudFrameW * 0.18f);
            float x      = (hudFrameW - width) * 0.87f + Main.settings.HoldOffsetX;
            float y      = hudFrameH - Mathf.Max(28f, hudFrameH * 0.05f) + Main.settings.HoldOffsetY;
            SetText(tmpHold, label);
            SetFontSize(tmpHold, fontPx);
            SetSize(tmpHold.rectTransform, width, fontPx + 8f);
            PlaceTopLeft(tmpHold, x, y);
            ScaleShadowOffset(tmpHold, fontPx);
            SetEnabled(tmpHold, true);
        }

        private static int hudCachedAttemptDisplay = -1;
        private static int hudCachedFullAttemptDisplay = -1;
        private static string hudCachedAttemptText;
        private static string hudCachedFullAttemptText;

        private static void UpdateAttemptElements()
        {
            if (!Main.settings.attemptOn || PlayCount.playDatas == null)
            {
                SetEnabled(tmpAttempt, false);
                SetEnabled(tmpFullAttempt, false);
                return;
            }
            float    fontPx         = ScaledFontPx(14, 0.022f);
            float    baseY          = hudFrameH - Mathf.Max(4f, hudFrameH * 0.006f) - fontPx
                                      - Main.settings.judgementPositionY - 80f;
            float    judgementWidth = Mathf.Max(180f, hudFrameW * 0.13f);
            float    judgementRight = hudFrameW * 0.5f + judgementWidth * 0.5f;
            float    attemptX       = judgementRight + fontPx * 0.8f + Main.settings.AttemptOffsetX;
            float    lineHeight     = fontPx + hudFrameH * 0.004f;
            int      row            = 0;
            PlayCount.PlayData data;
            PlayCount.TryGetPlayData(PlayCount.lastMapHash, out data);

            if (Main.settings.ShowAttempt)
            {
                int newRaw = data != null ? data.GetAttempts(PlayCount.startProgress, PlayCount.GetCurrentMultiplier()) : 0;
                Attempt.lastAttemptRaw  = newRaw;
                Attempt.displayAttempt  = PlayCount.GetSessionAttemptDisplay();
                if (Attempt.displayAttempt != hudCachedAttemptDisplay)
                {
                    hudCachedAttemptDisplay = Attempt.displayAttempt;
                    hudCachedAttemptText = "Attempt " + Attempt.displayAttempt;
                    SetText(tmpAttempt, hudCachedAttemptText);
                }
                SetFontSize(tmpAttempt, fontPx);
                SetSize(tmpAttempt.rectTransform, 300f, lineHeight + 8f);
                PlaceTopLeft(tmpAttempt, attemptX, baseY + row * lineHeight + Main.settings.AttemptOffsetY);
                ScaleShadowOffset(tmpAttempt, fontPx);
                SetEnabled(tmpAttempt, true);
                row++;
            }
            else SetEnabled(tmpAttempt, false);

            if (Main.settings.ShowFullAttempt)
            {
                int newRaw = data != null ? data.GetAllAttempts() : 0;
                Attempt.lastFullAttemptRaw  = newRaw;
                Attempt.displayFullAttempt  = newRaw;
                if (Attempt.displayFullAttempt != hudCachedFullAttemptDisplay)
                {
                    hudCachedFullAttemptDisplay = Attempt.displayFullAttempt;
                    hudCachedFullAttemptText = "Full Attempt " + Attempt.displayFullAttempt;
                    SetText(tmpFullAttempt, hudCachedFullAttemptText);
                }
                SetFontSize(tmpFullAttempt, fontPx);
                SetSize(tmpFullAttempt.rectTransform, 300f, lineHeight + 8f);
                PlaceTopLeft(tmpFullAttempt, attemptX, baseY + row * lineHeight + Main.settings.AttemptOffsetY);
                ScaleShadowOffset(tmpFullAttempt, fontPx);
                SetEnabled(tmpFullAttempt, true);
            }
            else SetEnabled(tmpFullAttempt, false);
        }

        private static float hudCachedTimingScale = -1f;
        private static string hudCachedTimingScaleText;

        private static void UpdateTimingScaleElement()
        {
            if (!Main.settings.timingScaleOn)
            {
                SetEnabled(tmpTimingScale, false);
                return;
            }
            float fontPx = ScaledFontPx(14, 0.022f);
            float cur = TimingScale.CurrentMarginScale;
            if (Mathf.Abs(cur - hudCachedTimingScale) > 0.0001f || hudCachedTimingScaleText == null)
            {
                hudCachedTimingScale = cur;
                hudCachedTimingScaleText = "Timing Scale - " + (cur * 100f).ToString("0.##", HudInvariant) + "%";
                SetText(tmpTimingScale, hudCachedTimingScaleText);
            }
            SetFontSize(tmpTimingScale, fontPx);
            SetSize(tmpTimingScale.rectTransform, hudFrameW * 0.5f, fontPx + 12f);
            float baseY = hudFrameH
                - Mathf.Max(4f, hudFrameH * 0.006f)
                - ScaledFontPx(20, 0.035f)
                - Main.settings.judgementPositionY
                - fontPx
                - hudFrameH * 0.008f
                - 80f
                + Main.settings.TimingScaleOffsetY;
            PlaceTopLeft(tmpTimingScale, (hudFrameW - hudFrameW * 0.5f) * 0.5f, baseY);
            ScaleShadowOffset(tmpTimingScale, fontPx);
            SetEnabled(tmpTimingScale, true);
        }

        private static float ScaledFontPx(int floor, float ratio)
        {
            float mult = (Main.settings != null) ? Mathf.Clamp(Main.settings.size, 0.3f, 3f) : 1f;
            return Mathf.Max(floor, Screen.height * ratio * mult);
        }

        private const float ShadowReferenceSize = 24f;

        private static void ScaleShadowOffset(TextMeshProUGUI t, float fontPx, float mult = 1f)
        {
            TextShadows.ScaleTmpDropShadowOffset(t, fontPx, ShadowReferenceSize, ShadowOffsetX, ShadowOffsetY, mult);
        }

        private static void SetText(TextMeshProUGUI t, string s)
        {
            if (t == null) return;
            if (!ReferenceEquals(t.text, s) && t.text != s) t.text = s;
        }

        private static void SetEnabled(TextMeshProUGUI t, bool value)
        {
            if (t != null && t.enabled != value) t.enabled = value;
        }

        private static void SetColor(TextMeshProUGUI t, Color color)
        {
            if (t != null && t.color != color) t.color = color;
        }

        private static void SetFontSize(TextMeshProUGUI t, float fontPx)
        {
            if (t != null && Mathf.Abs(t.fontSize - fontPx) > 0.01f) t.fontSize = fontPx;
        }

        private static void SetAlignment(TextMeshProUGUI t, TextAlignmentOptions alignment)
        {
            if (t != null && t.alignment != alignment) t.alignment = alignment;
        }

        private static void SetAnchorAndPivot(RectTransform rt, Vector2 anchor, Vector2 pivot)
        {
            if (rt == null) return;
            if (rt.anchorMin != anchor) rt.anchorMin = anchor;
            if (rt.anchorMax != anchor) rt.anchorMax = anchor;
            if (rt.pivot != pivot) rt.pivot = pivot;
        }

        private static void SetAnchoredPosition(RectTransform rt, float x, float y)
        {
            if (rt == null) return;
            Vector2 current = rt.anchoredPosition;
            if (Mathf.Abs(current.x - x) > 0.01f || Mathf.Abs(current.y - y) > 0.01f)
                rt.anchoredPosition = new Vector2(x, y);
        }

        private static void SetSize(RectTransform rt, float width, float height)
        {
            if (rt == null) return;
            Vector2 current = rt.sizeDelta;
            if (Mathf.Abs(current.x - width) > 0.01f || Mathf.Abs(current.y - height) > 0.01f)
                rt.sizeDelta = new Vector2(width, height);
        }

        private static void Place(TextMeshProUGUI t, float x, float y)
        {
            PlaceTopLeft(t, x, y);
        }

        private static void PlaceTopLeft(TextMeshProUGUI t, float screenX, float screenY)
        {
            if (t == null) return;
            RectTransform rt = t.rectTransform;
            SetAnchorAndPivot(rt, AnchorTopLeft, PivotTopLeft);
            SetAnchoredPosition(rt, screenX, -screenY);
        }

        private static void ConfigureLine(TextMeshProUGUI t, float x, float y, float fontPx, bool rightAlign, float width = -1f, float height = -1f)
        {
            if (t == null) return;

            SetFontSize(t, fontPx);
            SetAlignment(t, rightAlign ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft);

            RectTransform rt = t.rectTransform;
            SetAnchorAndPivot(rt, rightAlign ? AnchorTopRight : AnchorTopLeft, rightAlign ? PivotTopRight : PivotTopLeft);
            SetAnchoredPosition(rt, rightAlign ? -(hudFrameW - x) : x, -y);
            SetSize(rt, width >= 0f ? width : hudFrameW * 0.5f, height >= 0f ? height : fontPx + 8f);

            ScaleShadowOffset(t, fontPx);
        }
    }
}
