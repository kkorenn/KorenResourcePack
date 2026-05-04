using System;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KorenResourcePack
{
    public static partial class Main
    {
        // ========================================================
        // TMP Overlay — retained-mode HUD running on a Canvas.
        // Built lazily once the bundle is loaded. Every frame we
        // only mutate `.text` and visibility; no per-frame allocs.
        // ========================================================

        private static GameObject overlayRoot;
        private static Canvas overlayCanvas;
        private static CanvasScaler overlayScaler;

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
        private static bool overlayBuilt;

        private static readonly Color OverlayShadowColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color OverlayWhite = new Color(1f, 1f, 1f, 0.95f);

        internal static bool TryUseTmpOverlay()
        {
            EnsureBundleLoaded();
            if (!BundleAvailable) return false;
            BuildOverlayIfNeeded();
            ApplyFontToOverlay();
            return overlayBuilt;
        }

        /// <summary>Clears cached TMP font so the next overlay tick applies <see cref="Settings.fontName"/> again.</summary>
        internal static void InvalidateOverlayFontCache()
        {
            overlayActiveFont = null;
            overlayActiveFontName = null;
        }

        /// <summary>Forces judgement TMP labels to refresh (e.g. show "0") — cache must not use 0 as sentinel.</summary>
        internal static void InvalidateOverlayJudgementHudCache()
        {
            for (int i = 0; i < hudCachedJudgementCount.Length; i++)
                hudCachedJudgementCount[i] = -1;
            hudCachedJudgementXp = -1;
            hudCachedJudgementPp = -1;
            hudCachedJudgementMp = -1;
            hudCachedJudgementXpMode = false;
        }

        private static void BuildOverlayIfNeeded()
        {
            if (overlayBuilt && overlayRoot != null) return;

            overlayRoot = new GameObject("KorenResourcePack.Overlay");
            UnityEngine.Object.DontDestroyOnLoad(overlayRoot);

            overlayCanvas = overlayRoot.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 32700;

            overlayScaler = overlayRoot.AddComponent<CanvasScaler>();
            overlayScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            overlayScaler.scaleFactor = 1f;

            overlayRoot.AddComponent<GraphicRaycaster>().enabled = false;

            // Status block
            tmpProgress    = NewLabel("Progress",    TextAlignmentOptions.TopLeft);
            tmpAccuracy    = NewLabel("Accuracy",    TextAlignmentOptions.TopLeft);
            tmpXAccuracy   = NewLabel("XAccuracy",   TextAlignmentOptions.TopLeft);
            tmpMusicTime   = NewLabel("MusicTime",   TextAlignmentOptions.TopLeft);
            tmpCheckpoint  = NewLabel("Checkpoint",  TextAlignmentOptions.TopLeft);
            tmpBest        = NewLabel("Best",        TextAlignmentOptions.TopLeft);
            tmpFps         = NewLabel("FPS",         TextAlignmentOptions.TopLeft);
            tmpTbpm        = NewLabel("TBPM",        TextAlignmentOptions.TopRight);
            tmpCbpm        = NewLabel("CBPM",        TextAlignmentOptions.TopRight);

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
            t.alignment = align;
            t.color = OverlayWhite;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.text = string.Empty;
            // Built-in outline via material face/outline (cheap shadow proxy)
            t.outlineColor = OverlayShadowColor;
            t.outlineWidth = 0.18f;
            return t;
        }

        private static void ApplyFontToOverlay()
        {
            if (!overlayBuilt) return;
            string requested = settings != null ? (settings.fontName ?? "") : "";
            if (requested == overlayActiveFontName && overlayActiveFont != null) return;

            TMP_FontAsset fa = GetBundleFont(requested) ?? bundleDefaultFont;
            if (fa == null) return;
            overlayActiveFont = fa;
            overlayActiveFontName = requested;

            ApplyFontTo(tmpProgress);   ApplyFontTo(tmpAccuracy);   ApplyFontTo(tmpXAccuracy);
            ApplyFontTo(tmpMusicTime);  ApplyFontTo(tmpCheckpoint); ApplyFontTo(tmpBest);
            ApplyFontTo(tmpFps);        ApplyFontTo(tmpTbpm);       ApplyFontTo(tmpCbpm);
            ApplyFontTo(tmpCombo);      ApplyFontTo(tmpComboCaption);
            for (int i = 0; i < tmpJudgement.Length; i++) ApplyFontTo(tmpJudgement[i]);
            ApplyFontTo(tmpHold);
            ApplyFontTo(tmpAttempt); ApplyFontTo(tmpFullAttempt);
            ApplyFontTo(tmpTimingScale);
        }

        private static void ApplyFontTo(TextMeshProUGUI t)
        {
            if (t == null || overlayActiveFont == null) return;
            t.font = overlayActiveFont;
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
            overlayRoot = null; overlayCanvas = null; overlayScaler = null;
            tmpProgress = tmpAccuracy = tmpXAccuracy = tmpMusicTime = tmpCheckpoint = tmpBest = tmpFps = null;
            tmpTbpm = tmpCbpm = null;
            tmpCombo = tmpComboCaption = null;
            for (int i = 0; i < tmpJudgement.Length; i++) tmpJudgement[i] = null;
            tmpHold = null;
            tmpAttempt = tmpFullAttempt = null;
            tmpTimingScale = null;
            overlayActiveFont = null;
            overlayActiveFontName = null;
            overlayBuilt = false;
            InvalidateOverlayJudgementHudCache();
        }

        // ========================================================
        // PER-FRAME UPDATE
        // Called from OnFixedGUI; only mutates .text/.enabled/etc.
        // ========================================================

        private static int hudFrameW;
        private static int hudFrameH;

        private static void TickOverlay(float progress)
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

        private static int hudCachedFps = -1;
        private static int hudCachedCp = -1;
        private static float hudCachedProgress = -1f;

        private static void UpdateStatusBlock(float progress)
        {
            float fontPx = ScaledFontPx(18, 0.030f);
            float lineH = fontPx + hudFrameH * 0.006f;
            float leftX = hudFrameW * 0.012f;
            float topY = hudFrameH * 0.013f;
            int row = 0;

            ConfigureLine(tmpProgress,   leftX, topY + lineH * row, fontPx, false);
            ConfigureLine(tmpAccuracy,   leftX, topY + lineH * row, fontPx, false);
            ConfigureLine(tmpXAccuracy,  leftX, topY + lineH * row, fontPx, false);
            ConfigureLine(tmpMusicTime,  leftX, topY + lineH * row, fontPx, false);
            ConfigureLine(tmpCheckpoint, leftX, topY + lineH * row, fontPx, false);
            ConfigureLine(tmpBest,       leftX, topY + lineH * row, fontPx, false);
            ConfigureLine(tmpFps,        leftX, topY + lineH * row, fontPx, false);

            row = 0;
            bool show = settings.statusOn;
            if (show && settings.ShowProgress)
            {
                if (Mathf.Abs(progress - hudCachedProgress) > 0.0001f)
                {
                    hudCachedProgress = progress;
                    SetText(tmpProgress, "Progress | " + Math.Round(progress * 100f, 2) + "%");
                }
                Place(tmpProgress, leftX, topY + lineH * row++);
                tmpProgress.enabled = true;
            }
            else if (tmpProgress != null) tmpProgress.enabled = false;

            if (show && settings.ShowAccuracy)
            {
                SetText(tmpAccuracy, "Accuracy | " + GetAccuracyText());
                Place(tmpAccuracy, leftX, topY + lineH * row++);
                tmpAccuracy.enabled = true;
            }
            else if (tmpAccuracy != null) tmpAccuracy.enabled = false;

            if (show && settings.ShowXAccuracy)
            {
                SetText(tmpXAccuracy, "XAccuracy | " + GetXAccuracyText());
                Place(tmpXAccuracy, leftX, topY + lineH * row++);
                tmpXAccuracy.enabled = true;
            }
            else if (tmpXAccuracy != null) tmpXAccuracy.enabled = false;

            if (show && settings.ShowMusicTime)
            {
                SetText(tmpMusicTime, "Music/Map | " + (IsMusicPlaying() ? GetMusicTimeText() : GetMapTimeText()));
                Place(tmpMusicTime, leftX, topY + lineH * row++);
                tmpMusicTime.enabled = true;
            }
            else if (tmpMusicTime != null) tmpMusicTime.enabled = false;

            if (show && settings.ShowCheckpoint)
            {
                int cp = GetCheckpointCount();
                if (cp != hudCachedCp)
                {
                    hudCachedCp = cp;
                    SetText(tmpCheckpoint, "Checkpoints | " + cp);
                }
                Place(tmpCheckpoint, leftX, topY + lineH * row++);
                tmpCheckpoint.enabled = true;
            }
            else if (tmpCheckpoint != null) tmpCheckpoint.enabled = false;

            if (show && settings.ShowBest)
            {
                SetText(tmpBest, "Best | " + GetBestText());
                Place(tmpBest, leftX, topY + lineH * row++);
                tmpBest.enabled = true;
            }
            else if (tmpBest != null) tmpBest.enabled = false;

            if (show && settings.ShowFPS)
            {
                int fps = Mathf.RoundToInt(GetSmoothedFps());
                if (fps != hudCachedFps)
                {
                    hudCachedFps = fps;
                    SetText(tmpFps, "FPS | " + fps);
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
            float lineH = fontPx + hudFrameH * 0.006f;
            float blockW = hudFrameW * 0.33f;
            float leftX = hudFrameW * 0.012f;
            float topY = hudFrameH * 0.013f;
            float rightX = hudFrameW - leftX;

            bool show = settings.bpmOn;
            if (!show)
            {
                if (tmpTbpm != null) tmpTbpm.enabled = false;
                if (tmpCbpm != null) tmpCbpm.enabled = false;
                return;
            }

            float tBpm, cBpm;
            GetBpmValues(out tBpm, out cBpm);

            if (Mathf.Abs(tBpm - hudCachedTBpmRaw) > 0.005f)
            {
                hudCachedTBpmRaw = tBpm;
                SetText(tmpTbpm, "TBPM | " + Math.Round(tBpm, 2));
                tmpTbpm.color = LerpBpmColor(tBpm);
            }
            if (Mathf.Abs(cBpm - hudCachedCBpmRaw) > 0.005f)
            {
                hudCachedCBpmRaw = cBpm;
                SetText(tmpCbpm, "CBPM | " + Math.Round(cBpm, 2));
                tmpCbpm.color = LerpBpmColor(cBpm);
            }

            ConfigureLine(tmpTbpm, rightX, topY,        fontPx, true);
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
            if (!settings.comboOn)
            {
                if (tmpCombo != null) tmpCombo.enabled = false;
                if (tmpComboCaption != null) tmpComboCaption.enabled = false;
                return;
            }

            float scaleAnim = EvaluateComboScale();
            float valueBaseSize = ScaledFontPx(56, 0.075f);
            float valueSize = valueBaseSize * scaleAnim;
            float captionSize = valueSize * 0.35f;
            float centerX = hudFrameW * 0.5f;
            float heightScale = hudFrameH / ProgressBarReferenceHeight;
            float barTop = ProgressBarTargetTopOffset * heightScale;
            float barHeight = ProgressBarTargetHeight * heightScale;
            float verticalOffset = hudFrameH * 0.030f;
            if (settings.ComboMoveUpNoCaption && IsSongCaptionEmpty()) verticalOffset -= hudFrameH * 0.040f;
            float topY = Mathf.Max(0f, barTop + barHeight + verticalOffset + settings.comboY);

            if (perfectCombo != hudCachedCombo)
            {
                hudCachedCombo = perfectCombo;
                SetText(tmpCombo, perfectCombo.ToString());
            }
            tmpCombo.fontSize = valueSize;
            Color comboLow = new Color(settings.ComboColorLowR, settings.ComboColorLowG, settings.ComboColorLowB, settings.ComboColorLowA);
            if (settings.ComboColorMax > 0)
            {
                float t = Mathf.Clamp01((float)perfectCombo / settings.ComboColorMax);
                Color comboHigh = new Color(settings.ComboColorHighR, settings.ComboColorHighG, settings.ComboColorHighB, settings.ComboColorHighA);
                tmpCombo.color = Color.Lerp(comboLow, comboHigh, t);
            }
            else
            {
                tmpCombo.color = comboLow;
            }
            float rectWidth = hudFrameW * 0.4f;
            tmpCombo.rectTransform.sizeDelta = new Vector2(rectWidth, valueSize + hudFrameH * 0.016f);
            PlaceTopLeft(tmpCombo, centerX - rectWidth * 0.5f, topY);
            tmpCombo.enabled = true;

            if (settings.CaptionText)
            {
                SetText(tmpComboCaption, "Perfect Combo");
                tmpComboCaption.fontSize = captionSize;
                tmpComboCaption.color = OverlayWhite;
                float spacing = hudFrameH * 0.03f;
                tmpComboCaption.rectTransform.sizeDelta = new Vector2(rectWidth, captionSize + 8f);
                PlaceTopLeft(tmpComboCaption, centerX - rectWidth * 0.5f, topY + (valueSize + hudFrameH * 0.016f) - spacing - settings.captionY);
                tmpComboCaption.enabled = true;
            }
            else if (tmpComboCaption != null) tmpComboCaption.enabled = false;
        }

        // ----------------- JUDGEMENT -----------------

        private static readonly int[] hudCachedJudgementCount = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        private static int hudCachedJudgementXp = -1;
        private static int hudCachedJudgementPp = -1;
        private static int hudCachedJudgementMp = -1;
        private static bool hudCachedJudgementXpMode = false;

        private static void UpdateJudgementElements()
        {
            if (!settings.judgementOn)
            {
                for (int i = 0; i < tmpJudgement.Length; i++)
                    if (tmpJudgement[i] != null) tmpJudgement[i].enabled = false;
                return;
            }

            float fontPx = ScaledFontPx(14, 0.035f);
            float baseY = hudFrameH - Mathf.Max(4f, hudFrameH * 0.006f) - fontPx - settings.judgementPositionY;
            float gap = Mathf.Max(3f, fontPx * 0.07f);
            bool xpMode = XPerfectBridge.Active;
            int xc = 0, pc = 0, mc = 0;
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
                    if (xc != hudCachedJudgementXp || pc != hudCachedJudgementPp || mc != hudCachedJudgementMp || !hudCachedJudgementXpMode)
                    {
                        SetText(t, "<color=#60FF4E>" + pc + "</color> <color=#4DCCFF>" + xc + "</color> <color=#60FF4E>" + mc + "</color>");
                    }
                }
                else
                {
                    int count = GetJudgementSlotCount(i);
                    if (count != hudCachedJudgementCount[i] || (i == 4 && hudCachedJudgementXpMode != xpMode))
                    {
                        hudCachedJudgementCount[i] = count;
                        SetText(t, count.ToString());
                    }
                }

                t.color = JudgementSlotColors[i];
                t.fontSize = fontPx;
                t.richText = true;
                t.enabled = true;
            }

            hudCachedJudgementXp = xc;
            hudCachedJudgementPp = pc;
            hudCachedJudgementMp = mc;
            hudCachedJudgementXpMode = xpMode;

            // Pass 2 — preferred widths (no tiny max clamp: large counts were overlapping)
            float[] pref = new float[9];
            float pad = fontPx * 0.25f;
            float rowH = fontPx + hudFrameH * 0.009f;
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

            // each slot uses its OWN width (tight like your old system)
            float[] widths = new float[9];

            for (int i = 0; i < 9; i++)
            {
                widths[i] = pref[i]; // already includes padding
                tmpJudgement[i].rectTransform.sizeDelta = new Vector2(widths[i], rowH);
            }
            int pivot = 4;
            float centerX = hudFrameW * 0.5f;

            // center pivot
            float pivotHalf = widths[pivot] * 0.5f;
            PlaceTopLeft(tmpJudgement[pivot], centerX - pivotHalf, baseY);

            // LEFT side (center-based spacing)
            float cursor = centerX;

            for (int i = pivot - 1; i >= 0; i--)
            {
                float halfCur = widths[i] * 0.5f;
                float halfNext = widths[i + 1] * 0.5f;

                cursor -= (halfCur + gap + halfNext);
                PlaceTopLeft(tmpJudgement[i], cursor - halfCur, baseY);
            }

            // RIGHT side (center-based spacing)
            cursor = centerX;

            for (int i = pivot + 1; i < 9; i++)
            {
                float halfCur = widths[i] * 0.5f;
                float halfPrev = widths[i - 1] * 0.5f;

                cursor += (halfPrev + gap + halfCur);
                PlaceTopLeft(tmpJudgement[i], cursor - halfCur, baseY);
            }
        }

        // ----------------- HOLD -----------------

        private static void UpdateHoldElement()
        {
            if (!settings.holdOn)
            {
                if (tmpHold != null) tmpHold.enabled = false;
                return;
            }
            string label = GetHoldBehaviorLabel();
            if (string.IsNullOrEmpty(label))
            {
                tmpHold.enabled = false;
                return;
            }
            float fontPx = ScaledFontPx(16, 0.026f);
            float width = Mathf.Max(200f, hudFrameW * 0.18f);
            float x = (hudFrameW - width) * 0.87f + settings.HoldOffsetX;
            float y = hudFrameH - Mathf.Max(28f, hudFrameH * 0.05f) + settings.HoldOffsetY;
            SetText(tmpHold, label);
            tmpHold.fontSize = fontPx;
            tmpHold.rectTransform.sizeDelta = new Vector2(width, fontPx + 8f);
            PlaceTopLeft(tmpHold, x, y);
            tmpHold.enabled = true;
        }

        // ----------------- ATTEMPT -----------------

        private static void UpdateAttemptElements()
        {
            if (!settings.attemptOn || playDatas == null)
            {
                if (tmpAttempt != null) tmpAttempt.enabled = false;
                if (tmpFullAttempt != null) tmpFullAttempt.enabled = false;
                return;
            }
            float fontPx = ScaledFontPx(14, 0.022f);
            float baseY = hudFrameH - Mathf.Max(4f, hudFrameH * 0.006f) - fontPx - settings.judgementPositionY - 80f;
            float judgementWidth = Mathf.Max(180f, hudFrameW * 0.13f);
            float judgementRight = hudFrameW * 0.5f + judgementWidth * 0.5f;
            float attemptX = judgementRight + fontPx * 0.8f + settings.AttemptOffsetX;
            float lineHeight = fontPx + hudFrameH * 0.004f;

            int row = 0;
            PlayData data = null;
            try { data = GetPlayData(lastMapHash); } catch { }

            if (settings.ShowAttempt)
            {
                int newRaw = data != null ? data.GetAttempts(startProgress, GetCurrentMultiplier()) : 0;
                if (newRaw > lastAttemptRaw)
                {
                    lastAttemptRaw = newRaw;
                    displayAttempt = newRaw + 1;
                }
                SetText(tmpAttempt, "Attempt " + displayAttempt);
                tmpAttempt.fontSize = fontPx;
                tmpAttempt.rectTransform.sizeDelta = new Vector2(300f, lineHeight + 8f);
                PlaceTopLeft(tmpAttempt, attemptX, baseY + row * lineHeight + settings.AttemptOffsetY);
                tmpAttempt.enabled = true;
                row++;
            }
            else if (tmpAttempt != null) tmpAttempt.enabled = false;

            if (settings.ShowFullAttempt)
            {
                int newRaw = data != null ? data.GetAllAttempts() : 0;
                if (newRaw > lastFullAttemptRaw)
                {
                    lastFullAttemptRaw = newRaw;
                    displayFullAttempt = newRaw + 1;
                }
                SetText(tmpFullAttempt, "Full Attempt " + displayFullAttempt);
                tmpFullAttempt.fontSize = fontPx;
                tmpFullAttempt.rectTransform.sizeDelta = new Vector2(300f, lineHeight + 8f);
                PlaceTopLeft(tmpFullAttempt, attemptX, baseY + row * lineHeight + settings.AttemptOffsetY);
                tmpFullAttempt.enabled = true;
            }
            else if (tmpFullAttempt != null) tmpFullAttempt.enabled = false;
        }

        // ----------------- TIMING SCALE -----------------

        private static float hudCachedTimingScale = -1f;

        private static void UpdateTimingScaleElement()
        {
            if (!settings.timingScaleOn)
            {
                if (tmpTimingScale != null) tmpTimingScale.enabled = false;
                return;
            }
            float fontPx = ScaledFontPx(14, 0.022f);
            if (Mathf.Abs(currentMarginScale - hudCachedTimingScale) > 0.0001f)
            {
                hudCachedTimingScale = currentMarginScale;
                SetText(tmpTimingScale, "Timing Scale - " + Math.Round(currentMarginScale * 100f, 2) + "%");
            }
            tmpTimingScale.fontSize = fontPx;
            tmpTimingScale.rectTransform.sizeDelta = new Vector2(hudFrameW * 0.5f, fontPx + 12f);
            float baseY = hudFrameH
                - Mathf.Max(4f, hudFrameH * 0.006f)
                - ScaledFontPx(20, 0.035f)
                - settings.judgementPositionY
                - fontPx
                - hudFrameH * 0.008f
                - 80f
                + settings.TimingScaleOffsetY;
            PlaceTopLeft(tmpTimingScale, (hudFrameW - hudFrameW * 0.5f) * 0.5f, baseY);
            tmpTimingScale.enabled = true;
        }

        // ----------------- HELPERS -----------------

        private static float ScaledFontPx(int floor, float ratio)
        {
            float mult = (settings != null) ? Mathf.Clamp(settings.size, 0.3f, 3f) : 1f;
            return Mathf.Max(floor, Screen.height * ratio * mult);
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
            // Anchor top-left of canvas → screen-pixel offset (Unity Y-up so flip)
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(screenX, -screenY);
        }

        private static void ConfigureLine(TextMeshProUGUI t, float x, float y, float fontPx, bool rightAlign)
        {
            if (t == null) return;

            t.fontSize = fontPx;
            t.alignment = rightAlign ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;

            RectTransform rt = t.rectTransform;

            if (rightAlign)
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
            }

            rt.anchoredPosition = new Vector2(rightAlign ? -(hudFrameW - x) : x, -y);
            rt.sizeDelta = new Vector2(hudFrameW * 0.5f, fontPx + 8f);
        }
    }
}
