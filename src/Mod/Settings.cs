using System;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace KorenResourcePack
{
    public class UiHidingProfile
    {
        public bool HideEverything = false;
        public bool HideJudgment = false;
        public bool HideMissIndicators = false;
        public bool HideTitle = false;
        public bool HideOtto = false;
        public bool HideTimingTarget = false;
        public bool HideNoFailIcon = false;
        public bool HideBeta = false;
        public bool HideResult = false;
        public bool HideHitErrorMeter = false;
        public bool HideLastFloorFlash = false;
    }

    public class Settings : UnityModManager.ModSettings
    {
        public string ActiveProfileName = "";

        public string language = "en";
        public bool languageInitialized = false;
        public float size = 0.7f;
        public string fontName = "Maplestory Bold";
        public bool importSettingsDontShowOnLaunch = false;

        public bool progressBarOn = true;
        public bool progressBarExpanded = false;
        public ColorRange ProgressBarFillColor = KorenProgressBarFillColor();
        public ColorRange ProgressBarBackColor = KorenProgressBarBackgroundColor();
        public ColorRange ProgressBarBorderColor = KorenProgressBarBorderColor();

        public float ProgressBarFillR = 0.97f, ProgressBarFillG = 0.99f, ProgressBarFillB = 1f, ProgressBarFillA = 0.96f;
        public float ProgressBarBackR = 0.05f, ProgressBarBackG = 0.05f, ProgressBarBackB = 0.06f, ProgressBarBackA = 0.8f;
        public float ProgressBarBorderR = 0.98f, ProgressBarBorderG = 0.99f, ProgressBarBorderB = 1f, ProgressBarBorderA = 0.68f;

        public bool statusOn = true;
        public bool statusExpanded = false;
        public bool ShowProgress = true;
        public bool ShowAccuracy = false;
        public bool ShowXAccuracy = true;
        public bool ShowMaxAccuracy = false;
        public bool ShowMaxXAccuracy = true;
        public bool ShowMusicTime = false;
        public bool ShowMapTime = false;
        public bool ShowCheckpoint = false;
        public bool ShowBest = true;
        public bool ShowFPS = true;
        public bool HideDebugText = true;
        public bool fpsExpanded = false;
        public ColorRange ProgressColor = KorenProgressColor();
        public ColorRange AccuracyColor = KorenAccuracyColor();
        public ColorRange XAccuracyColor = KorenAccuracyColor();
        public ColorRange MusicTimeColor = WhiteColorRange();
        public ColorRange MapTimeColor = WhiteColorRange();
        public ColorRange BestColor = KorenBestColor();

        public int DecimalPlaces = 2;

        public bool bpmOn = true;
        public bool bpmExpanded = false;
        public bool tbpmOn = true;
        public bool cbpmOn = true;
        public bool kpsOn = true;
        public float BpmColorMax = 8000f;
        public ColorRange BpmColor = KorenBpmColor();

        public float BpmColorLowR = 1f, BpmColorLowG = 1f, BpmColorLowB = 1f, BpmColorLowA = 1f;
        public float BpmColorHighR = 1f, BpmColorHighG = 0f, BpmColorHighB = 0f, BpmColorHighA = 1f;

        public bool comboOn = true;
        public bool comboExpanded = false;
        public bool EnableAutoCombo = true;
        public int ComboColorMax = 2000;
        public ColorRange ComboColor = KorenComboColor();

        public float ComboColorLowR = 1f, ComboColorLowG = 1f, ComboColorLowB = 1f, ComboColorLowA = 1f;
        public float ComboColorHighR = 1f, ComboColorHighG = 0.22f, ComboColorHighB = 0.22f, ComboColorHighA = 1f;
        public bool ComboMoveUpNoCaption = false;
        public bool CaptionText = true;
        public bool captionExpanded = false;
        public float captionY = -15f;
        public bool comboFastAnim = false;
        public float comboY = 0f;

        public string comboText = "Combo";

        public bool judgementOn = true;
        public bool judgementExpanded = false;
        public bool LocationUp = false;
        public float judgementPositionY = 0f;
        public float judgementSize = 1f;
        public float judgementSpacing = 0f;
        public bool XPerfectComboEnabled = false;

        public bool holdOn = false;
        public bool holdExpanded = false;
        public float HoldOffsetX = 190f;
        public float HoldOffsetY = -20f;

        public bool attemptOn = true;
        public bool attemptExpanded = false;
        public bool ShowAttempt = true;
        public bool ShowFullAttempt = true;
        public float AttemptOffsetX = 0f;
        public float AttemptOffsetY = 25f;
        public float AttemptSize = 1f;

        public bool timingScaleOn = true;
        public bool timingScaleExpanded = false;
        public float TimingScaleOffsetY = 30f;

        public bool keyViewerOn = true;
        public bool keyViewerExpanded = false;
        public string KeyViewerJkvJson = KeyViewerDefaults.JkvJson;
        public string keyViewerPresetJson = "";
        public string keyViewerSelectedTab = "4key";
        public float KeyViewerOffsetX = 0f;
        public float KeyViewerOffsetY = 240f;
        public float KeyViewerScale = 1f;
        public bool KeyViewerNoteEffect = true;
        public float KeyViewerNoteSpeed = 1000f;
        public float KeyViewerTrackHeight = 200f;
        public bool KeyViewerNoteReverse = false;
        public bool KeyViewerShowCounter = true;
        public float KeyViewerFadePx = 60f;

        public int KeyViewerAdvancedOutOfLimiterMode = 1;

        public string KeyViewerMode = "simple";

        public int KeyViewerSimpleStyle = 2;

        public float KeyViewerSimpleYLocation = 200f;
        public bool KeyViewerSimpleDownLocation = false;

        public int KeyViewerSimpleFootStyle = 0;
        public float KeyViewerSimpleFootOffsetX = 0f;
        public float KeyViewerSimpleFootOffsetY = -40f;
        public float KeyViewerSimpleSize = 1f;
        public bool KeyViewerSimpleUseRain = true;
        public bool KeyViewerSimpleUseGhostRain = true;
        public bool KeyViewerSimpleSyncToKeyLimiter = true;
        public float KeyViewerSimpleRainSpeed = 400f;
        public float KeyViewerSimpleRainHeight = 200f;
        public float KeyViewerSimpleRainWidth = 0f;
        public float KeyViewerSimpleRain2Width = 40f;
        public float KeyViewerSimpleRainOffsetY = 0f;
        public float KeyViewerSimpleRain2OffsetY = 0f;

        public int[] KeyViewerSimpleKey10 = new int[]
        {
            113, 51, 52, 116, 111, 45, 61, 92,
            32, 104
        };
        public int[] KeyViewerSimpleKey12 = new int[]
        {
            113, 51, 52, 116, 111, 45, 61, 92,
            32, 98, 104, 44
        };
        public int[] KeyViewerSimpleKey16 = new int[]
        {
            113, 51, 52, 116, 111, 45, 61, 92,
            32, 98, 104, 44, 97, 304, 303, 13
        };
        public int[] KeyViewerSimpleKey20 = new int[]
        {
            113, 51, 52, 116, 111, 45, 61, 92,
            32, 98, 104, 44, 97, 304, 303, 13,
            110, 103, 109, 107
        };

        public string[] KeyViewerSimpleKey10Text = new string[]
        {
            null, null, null, null,
            null, null, null, null,
            null, null
        };
        public string[] KeyViewerSimpleKey12Text = new string[]
        {
            null, null, null, null,
            null, null, null, null,
            null, null, null, null
        };
        public string[] KeyViewerSimpleKey16Text = new string[]
        {
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null
        };
        public string[] KeyViewerSimpleKey20Text = new string[]
        {
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, null, null, null
        };

        public int[] KeyViewerSimpleGhostKey10 = new int[]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0
        };
        public int[] KeyViewerSimpleGhostKey12 = new int[]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0
        };
        public int[] KeyViewerSimpleGhostKey16 = new int[]
        {
            0, 0, 114, 121, 105, 112, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0
        };
        public int[] KeyViewerSimpleGhostKey20 = new int[]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0
        };

        public int[] KeyViewerSimpleFootKey2 = new int[]
        {
            289, 284
        };
        public int[] KeyViewerSimpleFootKey4 = new int[]
        {
            289, 284, 288, 283
        };
        public int[] KeyViewerSimpleFootKey6 = new int[]
        {
            289, 284, 288, 283, 287, 282
        };
        public int[] KeyViewerSimpleFootKey8 = new int[]
        {
            289, 285, 288, 284, 287, 283, 286, 282
        };
        public int[] KeyViewerSimpleFootKey16 = new int[]
        {
            289, 285, 288, 284, 287, 283, 286, 282,
            48, 54, 57, 53, 56, 52, 55, 51
        };

        public float SKvBgR = 1f, SKvBgG = 0.2352941f, SKvBgB = 0.2352941f, SKvBgA = 0.1960784f;
        public float SKvBgcR = 1f, SKvBgcG = 1f, SKvBgcB = 1f, SKvBgcA = 1f;
        public float SKvOutR = 1f, SKvOutG = 0f, SKvOutB = 0f, SKvOutA = 1f;
        public float SKvOutcR = 1f, SKvOutcG = 1f, SKvOutcB = 1f, SKvOutcA = 1f;
        public float SKvTxtR = 1f, SKvTxtG = 1f, SKvTxtB = 1f, SKvTxtA = 1f;
        public float SKvTxtcR = 0f, SKvTxtcG = 0f, SKvTxtcB = 0f, SKvTxtcA = 1f;
        public float SKvRainR = 1f, SKvRainG = 0f, SKvRainB = 0f, SKvRainA = 1f;
        public float SKvRain2R = 1f, SKvRain2G = 1f, SKvRain2B = 1f, SKvRain2A = 1f;
        public float SKvRain3R = 1f, SKvRain3G = 0f, SKvRain3B = 0f, SKvRain3A = 1f;
        public float SKvGhostRainR = 1f, SKvGhostRainG = 0f, SKvGhostRainB = 0f, SKvGhostRainA = 0.45f;

        public bool UiHidingOn = false;
        public bool UiHidingExpanded = false;
        public UiHidingProfile UiHidingPlayingProfile = new UiHidingProfile();
        public UiHidingProfile UiHidingRecordingProfile = new UiHidingProfile();
        public bool UiHidingRecordingMode = false;
        public bool UiHidingUseRecordingModeShortcut = false;
        public bool UiHidingShortcutCtrl = true;
        public bool UiHidingShortcutShift = false;
        public bool UiHidingShortcutAlt = false;
        public int UiHidingShortcutKey = (int)KeyCode.F8;
        public bool UiHidingProfilesMigrated = false;
        public bool UiHidingOnlyDuringRun = true;
        public bool UiHideAll = false;
        public bool UiHideLevelName = false;
        public bool UiHidePercent = false;
        public bool UiHidePressToStart = false;
        public bool UiHideCountdown = false;
        public bool UiHidePauseButton = false;
        public bool UiHideAutoplayButton = false;
        public bool UiHideMutedIcon = false;
        public bool UiHideDifficulty = false;
        public bool UiHideModifiers = false;
        public bool UiHideCalibration = false;
        public bool UiHideDebug = false;
        public bool UiHideAchievements = false;

        public bool ResourceChangerOn = true;
        public bool ResourceChangerExpanded = false;
        public bool ChangeOttoIcon = true;
        public bool ChangeBallColor = true;
        public bool ChangeRingColor = false;
        public bool ChangeTileColor = false;

        public float OttoR = 1f, OttoG = 0f, OttoB = 0f, OttoA = 1f;
        public float OttoOffsetX = -10f;
        public float OttoOffsetY = 5f;

        public float BallR = 1f, BallG = 0.70703125f, BallB = 0.70703125f, BallA = 1f;
        public float BallOpacity = 1f;
        public bool BallPlanetSettingsMigrated = true;
        public float BallPlanet1R = 1f, BallPlanet1G = 0f, BallPlanet1B = 0f;
        public float BallPlanet2R = 1f, BallPlanet2G = 0f, BallPlanet2B = 0f;
        public float BallPlanet3R = 1f, BallPlanet3G = 0f, BallPlanet3B = 0f;
        public float BallPlanet1Opacity = 0.5f;
        public float BallPlanet2Opacity = 0.5f;
        public float BallPlanet3Opacity = 0.5f;
        public float TailPlanet1Opacity = 0.2f;
        public float TailPlanet2Opacity = 0.2f;
        public float TailPlanet3Opacity = 0.2f;
        public bool SeparateTailColor = false;
        public bool TailColorSettingsMigrated = false;
        public float TailPlanet1R = 1f, TailPlanet1G = 0f, TailPlanet1B = 0f;
        public float TailPlanet2R = 1f, TailPlanet2G = 0f, TailPlanet2B = 0f;
        public float TailPlanet3R = 1f, TailPlanet3G = 0f, TailPlanet3B = 0f;
        public float RingOpacity = 0f;
        public float RingR = 1f, RingG = 1f, RingB = 1f, RingA = 1f;
        public float TileR = 1f, TileG = 0.87109375f, TileB = 0.87109375f, TileA = 1f;

        public bool TweaksOn = true;
        public bool TweaksExpanded = false;
        public bool RemoveAllCheckpoints = true;
        public bool RemoveBallCoreParticles = true;
        public float StationaryTailOpacity = 0.196428567f;
        public bool DisableTileHitGlow = true;
        public bool RemovePlanetGlow = true;
        public bool HideJudgementPopups = true;
        public int HiddenJudgementPopupMask = 28680;
        public bool DisableAutoPause = true;
        public bool BlockMouseWheelScrollWhilePlaying = false;

        public bool EffectRemoverOn = true;
        public bool EffectRemoverExpanded = false;
        public bool EffectRemoverEnableSave = false;
        public float EffectRemoverCameraZoomScale = 250.0f;
        public bool EffectRemoverResetTrackAnimation = false;
        public bool EffectRemoverResetTrackColor = true;
        public bool EffectRemoverRemoveAllDecorations = true;
        public bool EffectRemoverResetTrackOpacity = false;
        public bool EffectRemoverSetCameraZoom = false;
        public bool EffectRemoverFilters = true;
        public bool EffectRemoverAdvancedFilters = true;
        public bool EffectRemoverParticles = true;
        public bool EffectRemoverDecorations = true;
        public bool EffectRemoverBackgrounds = true;
        public bool EffectRemoverCameras = false;
        public bool EffectRemoverRepeatEvents = false;
        public bool EffectRemoverFrameRate = true;
        public bool EffectRemoverHitSounds = true;
        public bool EffectRemoverPlanetOrbit = false;
        public bool EffectRemoverPlanetScale = false;
        public bool EffectRemoverPlanetRadius = false;
        public bool EffectRemoverTrackAnimations = false;
        public bool EffectRemoverTrackPositions = false;
        public bool EffectRemoverTrackMoves = true;
        public bool EffectRemoverTrackColors = true;
        public bool EffectRemoverHoldSounds = false;
        public bool EffectRemoverHideIcons = false;
        public bool EffectRemoverPlanetPanel = false;
        public bool EffectRemoverTrackPanel = false;

        public bool KCBOn = true;
        public bool KCBExpanded = false;
        public float KCBThresholdMs = 35f;

        public bool KeyLimiterOn = true;
        public bool KeyLimiterExpanded = false;
        public int[] KeyLimiterAllowed = new int[]
        {
            113, 51, 52, 116, 111, 45, 61, 92,
            32, 98, 104, 44, 97, 304, 303, 13
        };

        public bool JRestrictOn = false;
        public bool JRestrictExpanded = false;
        public int JRestrictMode = 1;
        public float JRestrictAccuracy = 100f;

        public int JRestrictAllowedMask = 0;

        public bool DeathLimitOn = false;
        public bool DeathLimitExpanded = false;
        public bool DeathLimitMaxDeathsOn = true;
        public int DeathLimitMaxDeaths = 10;
        public bool DeathLimitMaxMissesOn = false;
        public int DeathLimitMaxMisses = 3;
        public bool DeathLimitMaxOverloadsOn = false;
        public int DeathLimitMaxOverloads = 3;

        public bool AutoDeafenOn = false;
        public bool AutoDeafenExpanded = false;
        public bool AutoDeafenCtrl = true;
        public bool AutoDeafenShift = true;
        public bool AutoDeafenAlt = false;
        public bool AutoDeafenMeta = false;
        public int AutoDeafenKey = (int)KeyCode.D;
        public float AutoDeafenAtPercent = 5f;
        public bool AutoDeafenOnlyFromStart = true;
        public bool AutoUndeafenOn = true;
        public float AutoUndeafenAtPercent = 95f;

        private bool _colorRangesReady;

        internal void EnsureColorRanges()
        {
            if (_colorRangesReady) return;

            if (ProgressBarFillColor == null) ProgressBarFillColor = KorenProgressBarFillColor();
            ProgressBarFillColor.EnsureDefault(KorenProgressBarFillColor());

            if (ProgressBarBackColor == null) ProgressBarBackColor = KorenProgressBarBackgroundColor();
            ProgressBarBackColor.EnsureDefault(KorenProgressBarBackgroundColor());

            if (ProgressBarBorderColor == null) ProgressBarBorderColor = KorenProgressBarBorderColor();
            ProgressBarBorderColor.EnsureDefault(KorenProgressBarBorderColor());

            if (ProgressColor == null) ProgressColor = KorenProgressColor();
            ProgressColor.EnsureDefault(KorenProgressColor());

            if (AccuracyColor == null) AccuracyColor = KorenAccuracyColor();
            AccuracyColor.EnsureDefault(KorenAccuracyColor());

            if (XAccuracyColor == null) XAccuracyColor = KorenAccuracyColor();
            XAccuracyColor.EnsureDefault(KorenAccuracyColor());

            if (MusicTimeColor == null) MusicTimeColor = WhiteColorRange();
            MusicTimeColor.EnsureDefault(WhiteColorRange());

            if (MapTimeColor == null) MapTimeColor = WhiteColorRange();
            MapTimeColor.EnsureDefault(WhiteColorRange());

            if (BestColor == null) BestColor = KorenBestColor();
            BestColor.EnsureDefault(KorenBestColor());

            if (BpmColor == null) BpmColor = KorenBpmColor();
            BpmColor.EnsureDefault(KorenBpmColor());

            if (ComboColor == null) ComboColor = KorenComboColor();
            ComboColor.EnsureDefault(KorenComboColor());

            _colorRangesReady = true;
        }

        internal void InvalidateColorRangeCache() { _colorRangesReady = false; }

        internal static ColorRange WhiteColorRange()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(1f, Color.white),
            });
        }

        internal static ColorRange KorenProgressColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(0f, new Color(1f, 1f, 1f, 1f)),
                new ColorRangePoint(1f, new Color(1f, 0f, 0f, 1f)),
            });
        }

        internal static ColorRange KorenProgressBarFillColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(1f, new Color(1f, 0f, 0f, 1f)),
            });
        }

        internal static ColorRange KorenProgressBarBackgroundColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(1f, new Color(1f, 1f, 1f, 1f)),
            });
        }

        internal static ColorRange KorenProgressBarBorderColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(1f, new Color(0f, 0f, 0f, 1f)),
            });
        }

        internal static ColorRange KorenAccuracyColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(0.98f, new Color(1f, 0f, 0f, 1f)),
                new ColorRangePoint(1f, new Color(1f, 1f, 1f, 1f)),
            }, new Color(1f, 0.854902f, 0f, 1f));
        }

        internal static ColorRange KorenBestColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(0f, new Color(1f, 1f, 1f, 1f)),
                new ColorRangePoint(1f, new Color(1f, 0f, 0f, 1f)),
            });
        }

        internal static ColorRange KorenBpmColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(0f, new Color(1f, 1f, 1f, 1f)),
                new ColorRangePoint(1f, new Color(1f, 0f, 0f, 1f)),
            });
        }

        internal static ColorRange KorenComboColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(0f, new Color(1f, 1f, 1f, 1f)),
                new ColorRangePoint(1f, new Color(1f, 0f, 0f, 1f)),
            });
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {

            _colorRangesReady = false;
            EnsureColorRanges();
            NormalizeSavedKeyCodes();
            Save(this, modEntry);
        }

        internal void NormalizeSavedKeyCodes()
        {
            if (KeyLimiterAllowed == null)
                return;

            List<int> normalized = new List<int>(KeyLimiterAllowed.Length);
            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < KeyLimiterAllowed.Length; i++)
            {
                int key = KeyCodeCompat.NormalizeKeyCode(KeyLimiterAllowed[i]);
                if (key == (int)KeyCode.None)
                    continue;
                if (seen.Add(key))
                    normalized.Add(key);
            }

            if (normalized.Count == KeyLimiterAllowed.Length)
            {
                bool same = true;
                for (int i = 0; i < normalized.Count; i++)
                {
                    if (normalized[i] != KeyLimiterAllowed[i])
                    {
                        same = false;
                        break;
                    }
                }
                if (same)
                    return;
            }

            KeyLimiterAllowed = normalized.ToArray();
        }
    }
}
