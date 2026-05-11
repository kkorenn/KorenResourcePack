using System;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KorenResourcePack
{
    internal static class Overlay
    {
        // ========================================================
        // TMP Overlay — retained-mode HUD running on a Canvas.
        // Built lazily once the bundle is loaded. Every frame we
        // only mutate `.text` and visibility; no per-frame allocs.
        // ========================================================

        private static GameObject overlayRoot;
        private static Canvas overlayCanvas;
        private static CanvasScaler overlayScaler;
        private static string hudCachedFpsText;

        // Status block (top-left)
        private static TextMeshProUGUI tmpProgress;
        private static TextMeshProUGUI tmpAccuracy;
        private static TextMeshProUGUI tmpXAccuracy;
        private static TextMeshProUGUI tmpMusicTime;
        private static TextMeshProUGUI tmpCheckpoint;
        private static TextMeshProUGUI tmpBest;
        private static TextMeshProUGUI tmpFps;
        // BPM block (top-right)
        private static TextMeshProUGUI tmpTbpm;
        private static TextMeshProUGUI tmpCbpm;
        // Combo
        private static TextMeshProUGUI tmpCombo;
        private static TextMeshProUGUI tmpComboCaption;
        // Judgement (9 slots)
        private static readonly TextMeshProUGUI[] tmpJudgement = new TextMeshProUGUI[9];
        // Hold
        private static TextMeshProUGUI tmpHold;
        // Attempt
        private static TextMeshProUGUI tmpAttempt;
        private static TextMeshProUGUI tmpFullAttempt;
        // Timing scale
        private static TextMeshProUGUI tmpTimingScale;

        private static TMP_FontAsset overlayActiveFont;
        private static string overlayActiveFontName;
        internal static bool overlayBuilt;

        private static readonly Color OverlayShadowColor  = new Color(0f,   0f,   0f,   0.55f);
        private static readonly Color OverlayWhite        = new Color(1f,   1f,   1f,   0.95f);

        // ---- shadow tuning knobs (tweak here, applied in NewLabel) ----
        // Soft, readable drop-shadow that scales with the outline so nothing
        // looks inconsistent between small status text and big combo numbers.
        private static readonly Color  ShadowColor  = new Color(0f, 0f, 0f, 0.35f);
        private const float            ShadowDilate = 0.0f;   // no extra dilation
        private const float            ShadowSoftness = 0.25f; // blur spread 0–1

        // TMP exposes shadow offset via the underlying material; we store a
        // per-label Vector2 and apply it in ApplyShadowOffset() so the offset
        // can scale with font size without re-creating materials.
        // Base offset in normalised UV space (TMP uses ~0.0–1.0 range here).
        private const float            ShadowOffsetX =  0.5f;
        private const float            ShadowOffsetY = -0.5f;

        internal static bool TryUseTmpOverlay()
        {
            BundleLoader.EnsureBundleLoaded();
            if (!BundleLoader.BundleAvailable) return false;
            BuildOverlayIfNeeded();
            ApplyFontToOverlay();
            return overlayBuilt;
        }

        /// <summary>Clears cached TMP font so the next overlay tick applies <see cref="Settings.fontName"/> again.</summary>
        internal static void InvalidateOverlayFontCache()
        {
            overlayActiveFont     = null;
            overlayActiveFontName = null;
        }

        /// <summary>Forces judgement TMP labels to refresh (e.g. show "0") — cache must not use 0 as sentinel.</summary>
        internal static void InvalidateOverlayJudgementHudCache()
        {
            for (int i = 0; i < hudCachedJudgementCount.Length; i++)
                hudCachedJudgementCount[i] = -1;
            hudCachedJudgementXp     = -1;
            hudCachedJudgementPp     = -1;
            hudCachedJudgementMp     = -1;
            hudCachedJudgementXpMode = false;
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

            // Status block
            tmpProgress   = NewLabel("Progress",   TextAlignmentOptions.TopLeft);
            tmpAccuracy   = NewLabel("Accuracy",   TextAlignmentOptions.TopLeft);
            tmpXAccuracy  = NewLabel("XAccuracy",  TextAlignmentOptions.TopLeft);
            tmpMusicTime  = NewLabel("MusicTime",  TextAlignmentOptions.TopLeft);
            tmpCheckpoint = NewLabel("Checkpoint", TextAlignmentOptions.TopLeft);
            tmpBest       = NewLabel("Best",       TextAlignmentOptions.TopLeft);
            tmpFps        = NewLabel("FPS",        TextAlignmentOptions.TopLeft);
            tmpTbpm       = NewLabel("TBPM",       TextAlignmentOptions.TopRight);
            tmpCbpm       = NewLabel("CBPM",       TextAlignmentOptions.TopRight);

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

        // ------------------------------------------------------------------
        // NewLabel — creates a TMP label with outline + drop-shadow baked in.
        //
        // TMP's shadow is driven by the shared font material's shader
        // properties.  To avoid polluting the shared asset we call
        // t.fontMaterial (which auto-creates a per-instance material copy)
        // and then set the four UNITY_UI_SHADOW_* properties directly.
        // All labels share the same shadow colour/softness; only the offset
        // is tunable per-label if needed in the future.
        // ------------------------------------------------------------------
        private static TextMeshProUGUI NewLabel(string name, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(overlayRoot.transform, false);

            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.alignment          = align;
            t.color              = OverlayWhite;
            t.enableWordWrapping = false;
            t.overflowMode       = TextOverflowModes.Overflow;
            t.raycastTarget      = false;
            t.text               = string.Empty;

            // Outline (cheap readable border)
            t.outlineColor = OverlayShadowColor;
            t.outlineWidth = 0.18f;

            // Drop-shadow via per-instance material properties.
            // fontMaterial creates a unique material copy so we never
            // touch the shared atlas asset.
            ApplyShadowToMaterial(t.fontMaterial);

            return t;
        }

        // ------------------------------------------------------------------
        // ApplyShadowToMaterial
        // Sets the four TMP shader keywords/properties that drive the
        // built-in drop-shadow effect.  Safe to call whenever a new font
        // asset is applied (which replaces the material).
        // ------------------------------------------------------------------
        private static void ApplyShadowToMaterial(Material mat)
        {
            if (mat == null) return;

            // Enable the drop-shadow keyword (required by TMP SDF shader)
            mat.EnableKeyword("UNDERLAY_ON");

            // Underlay = TMP's internal name for the drop-shadow layer
            mat.SetColor("_UnderlayColor",    ShadowColor);
            mat.SetFloat("_UnderlayOffsetX",  ShadowOffsetX);
            mat.SetFloat("_UnderlayOffsetY",  ShadowOffsetY);
            mat.SetFloat("_UnderlaySoftness", ShadowSoftness);
            mat.SetFloat("_UnderlayDilate",   ShadowDilate);
        }

        private static void ApplyFontToOverlay()
        {
            if (!overlayBuilt) return;
            string requested = Main.settings != null ? (Main.settings.fontName ?? "") : "";
            if (requested == overlayActiveFontName && overlayActiveFont != null) return;

            TMP_FontAsset fa = BundleLoader.GetBundleFont(requested) ?? BundleLoader.bundleDefaultFont;
            if (fa == null) return;
            overlayActiveFont     = fa;
            overlayActiveFontName = requested;

            ApplyFontTo(tmpProgress);  ApplyFontTo(tmpAccuracy);  ApplyFontTo(tmpXAccuracy);
            ApplyFontTo(tmpMusicTime); ApplyFontTo(tmpCheckpoint); ApplyFontTo(tmpBest);
            ApplyFontTo(tmpFps);       ApplyFontTo(tmpTbpm);       ApplyFontTo(tmpCbpm);
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
            // fontMaterial is recreated when the font asset changes,
            // so we must reapply shadow properties after every font swap.
            ApplyShadowToMaterial(t.fontMaterial);
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
            tmpProgress    = tmpAccuracy = tmpXAccuracy = tmpMusicTime = tmpCheckpoint = tmpBest = tmpFps = null;
            tmpTbpm        = tmpCbpm = null;
            tmpCombo       = tmpComboCaption = null;
            for (int i = 0; i < tmpJudgement.Length; i++) tmpJudgement[i] = null;
            tmpHold        = null;
            tmpAttempt     = tmpFullAttempt = null;
            tmpTimingScale = null;
            overlayActiveFont     = null;
            overlayActiveFontName = null;
            overlayBuilt          = false;
            InvalidateOverlayJudgementHudCache();
        }

        // ========================================================
        // PER-FRAME UPDATE
        // Called from OnFixedGUI; only mutates .text/.enabled/etc.
        // ========================================================

        private static int hudFrameW;
        private static int hudFrameH;

        internal static void TickOverlay(float progress)
        {
            if (!overlayBuilt) return;

            hudFrameW = Screen.width;
            hudFrameH = Screen.height;

            UpdateStatusBlock(progress);
            UpdateBpmBlock();
            UpdateComboElement();
            UpdateJudgementElements();
            UpdateHoldElement();
            UpdateAttemptElements();
            UpdateTimingScaleElement();
        }

        // ----------------- STATUS -----------------

        private static int   hudCachedFps      = -1;
        private static int   hudCachedCp       = -1;
        private static float hudCachedProgress = -1f;

        private static void UpdateStatusBlock(float progress)
        {
            float fontPx = ScaledFontPx(18, 0.030f);
            float lineH  = fontPx + hudFrameH * 0.006f;
            float leftX  = hudFrameW * 0.012f;
            float topY   = hudFrameH * 0.013f;
            int   row    = 0;

            ConfigureLine(tmpProgress,   leftX, topY + lineH * row, fontPx, false);
            ConfigureLine(tmpAccuracy,   leftX, topY + lineH * row, fontPx, false);
            ConfigureLine(tmpXAccuracy,  leftX, topY + lineH * row, fontPx, false);
            ConfigureLine(tmpMusicTime,  leftX, topY + lineH * row, fontPx, false);
            ConfigureLine(tmpCheckpoint, leftX, topY + lineH * row, fontPx, false);
            ConfigureLine(tmpBest,       leftX, topY + lineH * row, fontPx, false);
            ConfigureLine(tmpFps,        leftX, topY + lineH * row, fontPx, false);

            row = 0;
            bool show = Main.settings.statusOn;
            if (show && Main.settings.ShowProgress)
            {
                if (Mathf.Abs(progress - hudCachedProgress) > 0.0001f)
                {
                    hudCachedProgress = progress;
                    SetText(tmpProgress, "Progress | " + Status.FormatProgressRange(progress));
                }
                Place(tmpProgress, leftX, topY + lineH * row++);
                tmpProgress.enabled = true;
            }
            else if (tmpProgress != null) tmpProgress.enabled = false;

            if (show && Main.settings.ShowAccuracy)
            {
                SetText(tmpAccuracy, "Accuracy | " + Status.GetAccuracyText());
                Place(tmpAccuracy, leftX, topY + lineH * row++);
                tmpAccuracy.enabled = true;
            }
            else if (tmpAccuracy != null) tmpAccuracy.enabled = false;

            if (show && Main.settings.ShowXAccuracy)
            {
                SetText(tmpXAccuracy, "XAccuracy | " + Status.GetXAccuracyText());
                Place(tmpXAccuracy, leftX, topY + lineH * row++);
                tmpXAccuracy.enabled = true;
            }
            else if (tmpXAccuracy != null) tmpXAccuracy.enabled = false;

            if (show && Main.settings.ShowMusicTime)
            {
                SetText(tmpMusicTime, "Music/Map | " + (Status.IsMusicPlaying() ? Status.GetMusicTimeText() : Status.GetMapTimeText()));
                Place(tmpMusicTime, leftX, topY + lineH * row++);
                tmpMusicTime.enabled = true;
            }
            else if (tmpMusicTime != null) tmpMusicTime.enabled = false;

            if (show && Main.settings.ShowCheckpoint)
            {
                int cp = Status.GetCheckpointCount();
                if (cp != hudCachedCp)
                {
                    hudCachedCp = cp;
                    SetText(tmpCheckpoint, "Checkpoints | " + cp);
                }
                Place(tmpCheckpoint, leftX, topY + lineH * row++);
                tmpCheckpoint.enabled = true;
            }
            else if (tmpCheckpoint != null) tmpCheckpoint.enabled = false;

            if (show && Main.settings.ShowBest)
            {
                SetText(tmpBest, "Best | " + Status.GetBestText());
                Place(tmpBest, leftX, topY + lineH * row++);
                tmpBest.enabled = true;
            }
            else if (tmpBest != null) tmpBest.enabled = false;

            if (show && Main.settings.ShowFPS)
            {
                string fpsText = Status.GetFpsText();
                if (fpsText != hudCachedFpsText)
                {
                    hudCachedFpsText = fpsText;
                    SetText(tmpFps, fpsText);
                }
                Place(tmpFps, leftX, topY + lineH * row++);
                tmpFps.enabled = true;
            }
            else if (tmpFps != null) tmpFps.enabled = false;
        }

        // ----------------- BPM -----------------

        private static float hudCachedTBpmRaw = -1f;
        private static float hudCachedCBpmRaw = -1f;

        private static void UpdateBpmBlock()
        {
            float fontPx = ScaledFontPx(18, 0.030f);
            float lineH  = fontPx + hudFrameH * 0.006f;
            float blockW = hudFrameW * 0.33f;
            float leftX  = hudFrameW * 0.012f;
            float topY   = hudFrameH * 0.013f;
            float rightX = hudFrameW - leftX;

            bool show = Main.settings.bpmOn;
            if (!show)
            {
                if (tmpTbpm != null) tmpTbpm.enabled = false;
                if (tmpCbpm != null) tmpCbpm.enabled = false;
                return;
            }

            float tBpm, cBpm;
            Bpm.GetBpmValues(out tBpm, out cBpm);

            if (Mathf.Abs(tBpm - hudCachedTBpmRaw) > 0.005f)
            {
                hudCachedTBpmRaw = tBpm;
                SetText(tmpTbpm, "TBPM | " + Math.Round(tBpm, 2));
                tmpTbpm.color = Bpm.LerpBpmColor(tBpm);
            }
            if (Mathf.Abs(cBpm - hudCachedCBpmRaw) > 0.005f)
            {
                hudCachedCBpmRaw = cBpm;
                SetText(tmpCbpm, "CBPM | " + Math.Round(cBpm, 2));
                tmpCbpm.color = Bpm.LerpBpmColor(cBpm);
            }

            ConfigureLine(tmpTbpm, rightX, topY,         fontPx, true);
            ConfigureLine(tmpCbpm, rightX, topY + lineH, fontPx, true);
            tmpTbpm.rectTransform.sizeDelta = new Vector2(blockW, lineH);
            tmpCbpm.rectTransform.sizeDelta = new Vector2(blockW, lineH);
            tmpTbpm.enabled = true;
            tmpCbpm.enabled = true;
        }

        // ----------------- COMBO -----------------

        private static int hudCachedCombo = -1;

        private static void UpdateComboElement()
        {
            if (!Main.settings.comboOn)
            {
                if (tmpCombo        != null) tmpCombo.enabled        = false;
                if (tmpComboCaption != null) tmpComboCaption.enabled = false;
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
            tmpCombo.fontSize = valueSize;
            Color comboLow = new Color(Main.settings.ComboColorLowR, Main.settings.ComboColorLowG,
                                       Main.settings.ComboColorLowB, Main.settings.ComboColorLowA);
            if (Main.settings.ComboColorMax > 0)
            {
                float t = Mathf.Clamp01((float)Main.perfectCombo / Main.settings.ComboColorMax);
                Color comboHigh = new Color(Main.settings.ComboColorHighR, Main.settings.ComboColorHighG,
                                            Main.settings.ComboColorHighB, Main.settings.ComboColorHighA);
                tmpCombo.color = Color.Lerp(comboLow, comboHigh, t);
            }
            else
            {
                tmpCombo.color = comboLow;
            }

            // Scale shadow offset with combo font size so the shadow looks
            // proportional at both small and large sizes.
            ScaleShadowOffset(tmpCombo, valueSize);

            float rectWidth = hudFrameW * 0.4f;
            tmpCombo.rectTransform.sizeDelta = new Vector2(rectWidth, valueSize + hudFrameH * 0.016f);
            PlaceTopLeft(tmpCombo, centerX - rectWidth * 0.5f, topY);
            tmpCombo.enabled = true;

            if (Main.settings.CaptionText)
            {
                string caption = (Main.settings.XPerfectComboEnabled && XPerfectBridge.Active)
                    ? "XPerfect Combo"
                    : "Perfect Combo";

                SetText(tmpComboCaption, caption);
                tmpComboCaption.fontSize = captionSize;
                tmpComboCaption.color    = OverlayWhite;
                ScaleShadowOffset(tmpComboCaption, captionSize);
                float spacing = hudFrameH * 0.03f;
                tmpComboCaption.rectTransform.sizeDelta = new Vector2(rectWidth, captionSize + 8f);
                PlaceTopLeft(tmpComboCaption, centerX - rectWidth * 0.5f,
                             topY + (valueSize + hudFrameH * 0.016f) - spacing - Main.settings.captionY);
                tmpComboCaption.enabled = true;
            }
            else if (tmpComboCaption != null) tmpComboCaption.enabled = false;
        }

        // ----------------- JUDGEMENT -----------------

        private static readonly int[] hudCachedJudgementCount = new int[] { -1,-1,-1,-1,-1,-1,-1,-1,-1 };
        private static int  hudCachedJudgementXp     = -1;
        private static int  hudCachedJudgementPp     = -1;
        private static int  hudCachedJudgementMp     = -1;
        private static bool hudCachedJudgementXpMode = false;

        private static void UpdateJudgementElements()
        {
            if (!Main.settings.judgementOn)
            {
                for (int i = 0; i < tmpJudgement.Length; i++)
                    if (tmpJudgement[i] != null) tmpJudgement[i].enabled = false;
                return;
            }

            float fontPx = ScaledFontPx(14, 0.035f);
            float baseY  = hudFrameH - Mathf.Max(4f, hudFrameH * 0.006f) - fontPx - Main.settings.judgementPositionY;
            float gap    = Mathf.Max(3f, fontPx * 0.07f);
            bool  xpMode = XPerfectBridge.Active;
            int   xc = 0, pc = 0, mc = 0;
            if (xpMode)
            {
                xc = XPerfectBridge.XCount();
                pc = XPerfectBridge.PlusCount();
                mc = XPerfectBridge.MinusCount();
            }

            // Pass 1 — update text / style
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
                    }
                }
                else
                {
                    int count = Judgement.GetJudgementSlotCount(i);
                    if (count != hudCachedJudgementCount[i] || (i == 4 && hudCachedJudgementXpMode != xpMode))
                    {
                        hudCachedJudgementCount[i] = count;
                        SetText(t, count.ToString());
                    }
                }

                t.color    = Judgement.JudgementSlotColors[i];
                t.fontSize = fontPx;
                t.richText = true;
                t.enabled  = true;

                ScaleShadowOffset(t, fontPx);
            }

            hudCachedJudgementXp     = xc;
            hudCachedJudgementPp     = pc;
            hudCachedJudgementMp     = mc;
            hudCachedJudgementXpMode = xpMode;

            // Pass 2 — preferred widths
            float[] pref  = new float[9];
            float   pad   = fontPx * 0.25f;
            float   rowH  = fontPx + hudFrameH * 0.009f;
            for (int i = 0; i < 9; i++)
            {
                TextMeshProUGUI t = tmpJudgement[i];
                float w = t.GetPreferredValues().x + pad;
                if (w < fontPx * 0.48f)
                {
                    int len = string.IsNullOrEmpty(t.text) ? 1 : t.text.Length;
                    w = Mathf.Max(w, fontPx * (0.42f + Mathf.Min(len, 12) * 0.58f));
                }
                pref[i] = w;
            }

            float[] widths = new float[9];
            for (int i = 0; i < 9; i++)
            {
                widths[i] = pref[i];
                tmpJudgement[i].rectTransform.sizeDelta = new Vector2(widths[i], rowH);
            }

            int   pivot     = 4;
            float centerX   = hudFrameW * 0.5f;
            float pivotHalf = widths[pivot] * 0.5f;
            PlaceTopLeft(tmpJudgement[pivot], centerX - pivotHalf, baseY);

            float cursor = centerX;
            for (int i = pivot - 1; i >= 0; i--)
            {
                float halfCur  = widths[i]     * 0.5f;
                float halfNext = widths[i + 1] * 0.5f;
                cursor -= (halfCur + gap + halfNext);
                PlaceTopLeft(tmpJudgement[i], cursor - halfCur, baseY);
            }

            cursor = centerX;
            for (int i = pivot + 1; i < 9; i++)
            {
                float halfCur  = widths[i]     * 0.5f;
                float halfPrev = widths[i - 1] * 0.5f;
                cursor += (halfPrev + gap + halfCur);
                PlaceTopLeft(tmpJudgement[i], cursor - halfCur, baseY);
            }
        }

        // ----------------- HOLD -----------------

        private static void UpdateHoldElement()
        {
            if (!Main.settings.holdOn)
            {
                if (tmpHold != null) tmpHold.enabled = false;
                return;
            }
            string label = Hold.GetHoldBehaviorLabel();
            if (string.IsNullOrEmpty(label))
            {
                tmpHold.enabled = false;
                return;
            }
            float fontPx = ScaledFontPx(16, 0.026f);
            float width  = Mathf.Max(200f, hudFrameW * 0.18f);
            float x      = (hudFrameW - width) * 0.87f + Main.settings.HoldOffsetX;
            float y      = hudFrameH - Mathf.Max(28f, hudFrameH * 0.05f) + Main.settings.HoldOffsetY;
            SetText(tmpHold, label);
            tmpHold.fontSize = fontPx;
            tmpHold.rectTransform.sizeDelta = new Vector2(width, fontPx + 8f);
            PlaceTopLeft(tmpHold, x, y);
            ScaleShadowOffset(tmpHold, fontPx);
            tmpHold.enabled = true;
        }

        // ----------------- ATTEMPT -----------------

        private static void UpdateAttemptElements()
        {
            if (!Main.settings.attemptOn || PlayCount.playDatas == null)
            {
                if (tmpAttempt     != null) tmpAttempt.enabled     = false;
                if (tmpFullAttempt != null) tmpFullAttempt.enabled = false;
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
            PlayCount.PlayData data           = null;
            try { data = PlayCount.GetPlayData(PlayCount.lastMapHash); } catch { }

            if (Main.settings.ShowAttempt)
            {
                int newRaw = data != null ? data.GetAttempts(PlayCount.startProgress, PlayCount.GetCurrentMultiplier()) : 0;
                if (newRaw > Attempt.lastAttemptRaw)
                {
                    Attempt.lastAttemptRaw  = newRaw;
                    Attempt.displayAttempt  = newRaw + 1;
                }
                SetText(tmpAttempt, "Attempt " + Attempt.displayAttempt);
                tmpAttempt.fontSize = fontPx;
                tmpAttempt.rectTransform.sizeDelta = new Vector2(300f, lineHeight + 8f);
                PlaceTopLeft(tmpAttempt, attemptX, baseY + row * lineHeight + Main.settings.AttemptOffsetY);
                ScaleShadowOffset(tmpAttempt, fontPx);
                tmpAttempt.enabled = true;
                row++;
            }
            else if (tmpAttempt != null) tmpAttempt.enabled = false;

            if (Main.settings.ShowFullAttempt)
            {
                int newRaw = data != null ? data.GetAllAttempts() : 0;
                if (newRaw > Attempt.lastFullAttemptRaw)
                {
                    Attempt.lastFullAttemptRaw  = newRaw;
                    Attempt.displayFullAttempt  = newRaw + 1;
                }
                SetText(tmpFullAttempt, "Full Attempt " + Attempt.displayFullAttempt);
                tmpFullAttempt.fontSize = fontPx;
                tmpFullAttempt.rectTransform.sizeDelta = new Vector2(300f, lineHeight + 8f);
                PlaceTopLeft(tmpFullAttempt, attemptX, baseY + row * lineHeight + Main.settings.AttemptOffsetY);
                ScaleShadowOffset(tmpFullAttempt, fontPx);
                tmpFullAttempt.enabled = true;
            }
            else if (tmpFullAttempt != null) tmpFullAttempt.enabled = false;
        }

        // ----------------- TIMING SCALE -----------------

        private static float hudCachedTimingScale = -1f;

        private static void UpdateTimingScaleElement()
        {
            if (!Main.settings.timingScaleOn)
            {
                if (tmpTimingScale != null) tmpTimingScale.enabled = false;
                return;
            }
            float fontPx = ScaledFontPx(14, 0.022f);
            if (Mathf.Abs(TimingScale.CurrentMarginScale - hudCachedTimingScale) > 0.0001f)
            {
                hudCachedTimingScale = TimingScale.CurrentMarginScale;
                SetText(tmpTimingScale, "Timing Scale - " + Math.Round(TimingScale.CurrentMarginScale * 100f, 2) + "%");
            }
            tmpTimingScale.fontSize = fontPx;
            tmpTimingScale.rectTransform.sizeDelta = new Vector2(hudFrameW * 0.5f, fontPx + 12f);
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
            tmpTimingScale.enabled = true;
        }

        // ----------------- HELPERS -----------------

        private static float ScaledFontPx(int floor, float ratio)
        {
            float mult = (Main.settings != null) ? Mathf.Clamp(Main.settings.size, 0.3f, 3f) : 1f;
            return Mathf.Max(floor, Screen.height * ratio * mult);
        }

        // Scales the shadow offset proportionally to the font size so the
        // drop-shadow looks consistent regardless of whether we're rendering
        // tiny status text (14px) or a huge combo number (56px+).
        // The base offset is defined at a reference size of 24px.
        private const float ShadowReferenceSize = 24f;

        private static void ScaleShadowOffset(TextMeshProUGUI t, float fontPx)
        {
            if (t == null) return;
            Material mat = t.fontMaterial;
            if (mat == null) return;
            float scale = fontPx / ShadowReferenceSize;
            mat.SetFloat("_UnderlayOffsetX",  ShadowOffsetX * scale);
            mat.SetFloat("_UnderlayOffsetY",  ShadowOffsetY * scale);
        }

        private static void SetText(TextMeshProUGUI t, string s)
        {
            if (t == null) return;
            if (!ReferenceEquals(t.text, s) && t.text != s) t.text = s;
        }

        private static void Place(TextMeshProUGUI t, float x, float y)
        {
            PlaceTopLeft(t, x, y);
        }

        private static void PlaceTopLeft(TextMeshProUGUI t, float screenX, float screenY)
        {
            if (t == null) return;
            RectTransform rt = t.rectTransform;
            rt.anchorMin       = new Vector2(0f, 1f);
            rt.anchorMax       = new Vector2(0f, 1f);
            rt.pivot           = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(screenX, -screenY);
        }

        private static void ConfigureLine(TextMeshProUGUI t, float x, float y, float fontPx, bool rightAlign)
        {
            if (t == null) return;

            t.fontSize  = fontPx;
            t.alignment = rightAlign ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;

            RectTransform rt = t.rectTransform;

            if (rightAlign)
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(1f, 1f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot     = new Vector2(0f, 1f);
            }

            rt.anchoredPosition = new Vector2(rightAlign ? -(hudFrameW - x) : x, -y);
            rt.sizeDelta        = new Vector2(hudFrameW * 0.5f, fontPx + 8f);

            ScaleShadowOffset(t, fontPx);
        }
    }
}