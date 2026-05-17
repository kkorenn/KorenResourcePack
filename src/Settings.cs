using UnityEngine;
using UnityModManagerNet;

namespace KorenResourcePack
{
        public class Settings : UnityModManager.ModSettings
    {
        public string language = "en";
        public float size = 1f;
        public string fontName = "";

        public bool progressBarOn = true;
        public bool progressBarExpanded = false;
        public ColorRange ProgressBarFillColor = JipperProgressBarFillColor();
        public ColorRange ProgressBarBackColor = JipperProgressBarBackgroundColor();
        public ColorRange ProgressBarBorderColor = JipperProgressBarBorderColor();
        // Legacy single progress-bar colors. Kept so old configs deserialize cleanly;
        // runtime/editor now use the Jipper-style color ranges above.
        public float ProgressBarFillR = 0.97f, ProgressBarFillG = 0.99f, ProgressBarFillB = 1.00f, ProgressBarFillA = 0.96f;
        public float ProgressBarBackR = 0.05f, ProgressBarBackG = 0.05f, ProgressBarBackB = 0.06f, ProgressBarBackA = 0.80f;
        public float ProgressBarBorderR = 0.98f, ProgressBarBorderG = 0.99f, ProgressBarBorderB = 1.00f, ProgressBarBorderA = 0.68f;

        public bool statusOn = true;
        public bool statusExpanded = false;
        public bool ShowProgress = true;
        public bool ShowAccuracy = false;
        public bool ShowXAccuracy = true;
        public bool ShowMusicTime = true;
        public bool ShowMapTime = false;
        public bool ShowMapTimeIfNotMusic = true;
        public bool ShowCheckpoint = false;
        public bool ShowBest = false;
        public bool ShowFPS = true;
        public bool HideDebugText = true;
        public bool fpsExpanded = false;
        public ColorRange ProgressColor = JipperProgressColor();
        public ColorRange AccuracyColor = JipperAccuracyColor();
        public ColorRange XAccuracyColor = JipperAccuracyColor();
        public ColorRange MusicTimeColor = WhiteColorRange();
        public ColorRange MapTimeColor = WhiteColorRange();
        public ColorRange BestColor = JipperProgressColor();

        // Decimal precision applied to every percent-style HUD readout
        // (Progress, Accuracy, XAccuracy, Best, Timing Scale). 0–6 keeps room for
        // both terse "98%" and forensic "99.987654%" displays.
        public int DecimalPlaces = 2;

        public bool bpmOn = true;
        public bool bpmExpanded = false;
        public float BpmColorMax = 8000f;
        public ColorRange BpmColor = JipperBpmColor();
        // Legacy two-stop BPM color fields. Kept so old configs deserialize cleanly;
        // the visible editor and runtime renderer now use BpmColor's Jipper-style range.
        public float BpmColorLowR = 1f, BpmColorLowG = 1f, BpmColorLowB = 1f, BpmColorLowA = 1f;
        public float BpmColorHighR = 1f, BpmColorHighG = 0f, BpmColorHighB = 0f, BpmColorHighA = 1f;

        public bool comboOn = true;
        public bool comboExpanded = false;
        public bool EnableAutoCombo = true;
        public int ComboColorMax = 1000;
        public ColorRange ComboColor = JipperComboColor();
        // Legacy two-stop combo color fields. Kept so old configs deserialize cleanly;
        // runtime/editor now use ComboColor's Jipper-style range.
        public float ComboColorLowR = 1f, ComboColorLowG = 1f, ComboColorLowB = 1f, ComboColorLowA = 1f;
        public float ComboColorHighR = 1f, ComboColorHighG = 0.22f, ComboColorHighB = 0.22f, ComboColorHighA = 1f;
        public bool ComboMoveUpNoCaption = false;
        public bool CaptionText = false;
        public bool captionExpanded = false;
        public float captionY = 0;
        public bool comboFastAnim = false;
        public float comboY = 0;

        public bool judgementOn = true;
        public bool judgementExpanded = false;
        public bool LocationUp = false;
        public float judgementPositionY = 0;
        public bool XPerfectComboEnabled = false;

        public bool holdOn = true;
        public bool holdExpanded = false;
        public float HoldOffsetX = 0f;
        public float HoldOffsetY = 0f;

        public bool attemptOn = true;
        public bool attemptExpanded = false;
        public bool ShowAttempt = true;
        public bool ShowFullAttempt = false;
        public float AttemptOffsetX = 0f;
        public float AttemptOffsetY = 0f;

        public bool timingScaleOn = true;
        public bool timingScaleExpanded = false;
        public float TimingScaleOffsetY = 0f;

        public bool keyViewerOn = false;
        public bool keyViewerExpanded = false;
        public string keyViewerPresetJson = "";
        public string keyViewerSelectedTab = "4key";
        public float KeyViewerOffsetX = 0f;
        public float KeyViewerOffsetY = 0f;
        public float KeyViewerScale = 1f;
        public bool KeyViewerNoteEffect = true;
        public float KeyViewerNoteSpeed = 100f;
        public float KeyViewerTrackHeight = 200f;
        public bool KeyViewerNoteReverse = false;
        public bool KeyViewerShowCounter = true;
        public float KeyViewerFadePx = 60f;

        // ----------------- KeyViewer mode -----------------
        // "dmnote" = user-supplied JSON preset (advanced).
        // "simple" = pre-baked Key10/12/16/20 preset that runs through the same renderer.
        public string KeyViewerMode = "dmnote";

        // ----------------- Simple-mode settings (Jipper-equivalent) -----------------
        // 0=Key10, 1=Key12, 2=Key16, 3=Key20. Default mirrors Jipper's Key16.
        public int KeyViewerSimpleStyle = 2;
        // Jipper's normal keyviewer Y location. Legacy DownLocation is kept for old configs.
        public float KeyViewerSimpleYLocation = 200f;
        public bool KeyViewerSimpleDownLocation = false;
        // 0=None, 1=Key2, 2=Key4, 3=Key6, 4=Key8, 5=Key16.
        public int KeyViewerSimpleFootStyle = 2;
        public float KeyViewerSimpleSize = 1f;
        public bool KeyViewerSimpleUseRain = true;
        public bool KeyViewerSimpleUseGhostRain = false;
        public float KeyViewerSimpleRainSpeed = 100f;
        public float KeyViewerSimpleRainHeight = 200f;
        // KeyCode arrays per style. Stored as int because KeyCode isn't UMM-serializable.
        public int[] KeyViewerSimpleKey10 = new int[]
        {
            (int)KeyCode.Tab, (int)KeyCode.Alpha1, (int)KeyCode.Alpha2, (int)KeyCode.E,
            (int)KeyCode.P, (int)KeyCode.Equals, (int)KeyCode.Backspace, (int)KeyCode.Backslash,
            (int)KeyCode.Space, (int)KeyCode.Comma,
        };
        public int[] KeyViewerSimpleKey12 = new int[]
        {
            (int)KeyCode.Tab, (int)KeyCode.Alpha1, (int)KeyCode.Alpha2, (int)KeyCode.E,
            (int)KeyCode.P, (int)KeyCode.Equals, (int)KeyCode.Backspace, (int)KeyCode.Backslash,
            (int)KeyCode.Space, (int)KeyCode.C, (int)KeyCode.Comma, (int)KeyCode.Period,
        };
        public int[] KeyViewerSimpleKey16 = new int[]
        {
            (int)KeyCode.Tab, (int)KeyCode.Alpha1, (int)KeyCode.Alpha2, (int)KeyCode.E,
            (int)KeyCode.P, (int)KeyCode.Equals, (int)KeyCode.Backspace, (int)KeyCode.Backslash,
            (int)KeyCode.Space, (int)KeyCode.C, (int)KeyCode.Comma, (int)KeyCode.Period,
            (int)KeyCode.CapsLock, (int)KeyCode.LeftShift, (int)KeyCode.Return, (int)KeyCode.H,
        };
        public int[] KeyViewerSimpleKey20 = new int[]
        {
            (int)KeyCode.Tab, (int)KeyCode.Alpha1, (int)KeyCode.Alpha2, (int)KeyCode.E,
            (int)KeyCode.P, (int)KeyCode.Equals, (int)KeyCode.Backspace, (int)KeyCode.Backslash,
            (int)KeyCode.Space, (int)KeyCode.C, (int)KeyCode.Comma, (int)KeyCode.Period,
            (int)KeyCode.CapsLock, (int)KeyCode.LeftShift, (int)KeyCode.Return, (int)KeyCode.H,
            (int)KeyCode.CapsLock, (int)KeyCode.D, (int)KeyCode.RightShift, (int)KeyCode.Semicolon,
        };

        // Per-slot displayText overrides. Empty/null = derive from KeyCode.
        public string[] KeyViewerSimpleKey10Text = new string[10];
        public string[] KeyViewerSimpleKey12Text = new string[12];
        public string[] KeyViewerSimpleKey16Text = new string[16];
        public string[] KeyViewerSimpleKey20Text = new string[20];

        public int[] KeyViewerSimpleGhostKey10 = new int[10];
        public int[] KeyViewerSimpleGhostKey12 = new int[12];
        public int[] KeyViewerSimpleGhostKey16 = new int[16];
        public int[] KeyViewerSimpleGhostKey20 = new int[20];

        public int[] KeyViewerSimpleFootKey2 = new int[] { (int)KeyCode.F8, (int)KeyCode.F3 };
        public int[] KeyViewerSimpleFootKey4 = new int[] { (int)KeyCode.F8, (int)KeyCode.F3, (int)KeyCode.F7, (int)KeyCode.F2 };
        public int[] KeyViewerSimpleFootKey6 = new int[] { (int)KeyCode.F8, (int)KeyCode.F3, (int)KeyCode.F7, (int)KeyCode.F2, (int)KeyCode.F6, (int)KeyCode.F1 };
        public int[] KeyViewerSimpleFootKey8 = new int[] { (int)KeyCode.F8, (int)KeyCode.F4, (int)KeyCode.F7, (int)KeyCode.F3, (int)KeyCode.F6, (int)KeyCode.F2, (int)KeyCode.F5, (int)KeyCode.F1 };
        public int[] KeyViewerSimpleFootKey16 = new int[]
        {
            (int)KeyCode.F8, (int)KeyCode.F4, (int)KeyCode.F7, (int)KeyCode.F3,
            (int)KeyCode.F6, (int)KeyCode.F2, (int)KeyCode.F5, (int)KeyCode.F1,
            (int)KeyCode.Alpha0, (int)KeyCode.Alpha6, (int)KeyCode.Alpha9, (int)KeyCode.Alpha5,
            (int)KeyCode.Alpha8, (int)KeyCode.Alpha4, (int)KeyCode.Alpha7, (int)KeyCode.Alpha3,
        };

        // Color slots — defaults use KRP's red palette.
        // Background: rgba(255, 60, 60, 0.196)
        public float SKvBgR = 1f, SKvBgG = 0.2352941f, SKvBgB = 0.2352941f, SKvBgA = 0.1960784f;
        public float SKvBgcR = 1f, SKvBgcG = 1f, SKvBgcB = 1f, SKvBgcA = 1f;
        public float SKvOutR = 1f, SKvOutG = 0.2431373f, SKvOutB = 0.2431373f, SKvOutA = 1f;
        public float SKvOutcR = 1f, SKvOutcG = 1f, SKvOutcB = 1f, SKvOutcA = 1f;
        public float SKvTxtR = 1f, SKvTxtG = 1f, SKvTxtB = 1f, SKvTxtA = 1f;
        public float SKvTxtcR = 0f, SKvTxtcG = 0f, SKvTxtcB = 0f, SKvTxtcA = 1f;
        public float SKvRainR = 0.8627451f, SKvRainG = 0.1254902f, SKvRainB = 0.1254902f, SKvRainA = 1f;
        public float SKvRain2R = 1f, SKvRain2G = 1f, SKvRain2B = 1f, SKvRain2A = 1f;
        public float SKvRain3R = 1f, SKvRain3G = 0f, SKvRain3B = 0f, SKvRain3A = 1f;

        // Resource Changer (Jipper-style).
        public bool ResourceChangerOn = false;
        public bool ResourceChangerExpanded = false;
        public bool ChangeOttoIcon = true;
        public bool ChangeBallColor = true;
        public bool ChangeTileColor = true;
        // Single base color for the Otto icon. Default = #FF0000 (red).
        // The "auto off" tint is derived at render time by multiplying RGB by ~0.343,
        // so the user only ever picks one color and the dim variant follows.
        public float OttoR = 1f, OttoG = 0f, OttoB = 0f, OttoA = 1f;
        public float OttoOffsetX = -10f;
        public float OttoOffsetY = 5f;
        // Resource color defaults. BallA is kept only so old configs deserialize cleanly;
        // opacity now lives in separate ball/ring controls.
        public float BallR = 1f, BallG = 0.70703125f, BallB = 0.70703125f, BallA = 1f;
        public float BallOpacity = 1f;
        public bool BallPlanetSettingsMigrated = false;
        public float BallPlanet1R = 1f, BallPlanet1G = 0.70703125f, BallPlanet1B = 0.70703125f;
        public float BallPlanet2R = 1f, BallPlanet2G = 0.70703125f, BallPlanet2B = 0.70703125f;
        public float BallPlanet3R = 1f, BallPlanet3G = 0.70703125f, BallPlanet3B = 0.70703125f;
        public float BallPlanet1Opacity = 1f;
        public float BallPlanet2Opacity = 1f;
        public float BallPlanet3Opacity = 1f;
        public float TailPlanet1Opacity = 1f;
        public float TailPlanet2Opacity = 1f;
        public float TailPlanet3Opacity = 1f;
        public float RingOpacity = 0f;
        public float TileR = 1f, TileG = 0.87109375f, TileB = 0.87109375f, TileA = 1f;

        // Tweaks.
        public bool TweaksOn = false;
        public bool TweaksExpanded = false;
        public bool RemoveAllCheckpoints = false;
        public bool RemoveBallCoreParticles = false;
        public bool DisableTileHitGlow = false;

        // Keyboard chatter blocker. Mirrors KeyboardChatterBlocker.dll: CountValidKeysPressed
        // and SkyHook async key events are filtered by press-to-press interval in ms.
        public bool KCBOn = false;
        public bool KCBExpanded = false;
        public float KCBThresholdMs = 100f;

        // KeyLimiter from KeyboardChatterBlocker.dll. Only listed keys register during
        // PlayerControl; menus/editor/system keys outside that state are untouched.
        public bool KeyLimiterOn = false;
        public bool KeyLimiterExpanded = false;
        public int[] KeyLimiterAllowed = new int[]
        {
            (int)KeyCode.Z, (int)KeyCode.X, (int)KeyCode.Comma, (int)KeyCode.Period,
            (int)KeyCode.LeftShift, (int)KeyCode.RightShift, (int)KeyCode.Space,
        };

        // Judgement restriction. Forces a level fail when the player drops below the
        // configured rule. Modes: 0 = accuracy threshold, 1 = pure-perfect only,
        // 2 = X-pure-perfect only, 3 = custom (bitmask of allowed HitMargins),
        // 4 = no miss (Too Early fails).
        public bool JRestrictOn = false;
        public bool JRestrictExpanded = false;
        public int JRestrictMode = 0;
        public float JRestrictAccuracy = 100f; // percent threshold for mode 0
        // Bitmask over HitMargin enum values (0..11). Default = Perfect only.
        public int JRestrictAllowedMask = 1 << 3; // HitMargin.Perfect

        internal void EnsureColorRanges()
        {
            if (ProgressBarFillColor == null) ProgressBarFillColor = JipperProgressBarFillColor();
            ProgressBarFillColor.EnsureDefault(JipperProgressBarFillColor());

            if (ProgressBarBackColor == null) ProgressBarBackColor = JipperProgressBarBackgroundColor();
            ProgressBarBackColor.EnsureDefault(JipperProgressBarBackgroundColor());

            if (ProgressBarBorderColor == null) ProgressBarBorderColor = JipperProgressBarBorderColor();
            ProgressBarBorderColor.EnsureDefault(JipperProgressBarBorderColor());

            if (ProgressColor == null) ProgressColor = JipperProgressColor();
            ProgressColor.EnsureDefault(JipperProgressColor());

            if (AccuracyColor == null) AccuracyColor = JipperAccuracyColor();
            AccuracyColor.EnsureDefault(JipperAccuracyColor());

            if (XAccuracyColor == null) XAccuracyColor = JipperAccuracyColor();
            XAccuracyColor.EnsureDefault(JipperAccuracyColor());

            if (MusicTimeColor == null) MusicTimeColor = WhiteColorRange();
            MusicTimeColor.EnsureDefault(WhiteColorRange());

            if (MapTimeColor == null) MapTimeColor = WhiteColorRange();
            MapTimeColor.EnsureDefault(WhiteColorRange());

            if (BestColor == null) BestColor = JipperProgressColor();
            BestColor.EnsureDefault(JipperProgressColor());

            if (BpmColor == null) BpmColor = JipperBpmColor();
            BpmColor.EnsureDefault(JipperBpmColor());

            if (ComboColor == null) ComboColor = JipperComboColor();
            ComboColor.EnsureDefault(JipperComboColor());
        }

        internal static ColorRange WhiteColorRange()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(1f, Color.white),
            });
        }

        internal static ColorRange JipperProgressColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(0f, Color.white),
                new ColorRangePoint(1f, new Color(1f, 0.7098039f, 0.7098039f, 1f)),
            });
        }

        internal static ColorRange JipperProgressBarFillColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(1f, new Color(1f, 0.8039216f, 0.8039216f, 1f)),
            });
        }

        internal static ColorRange JipperProgressBarBackgroundColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(1f, Color.white),
            });
        }

        internal static ColorRange JipperProgressBarBorderColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(1f, Color.black),
            });
        }

        internal static ColorRange JipperAccuracyColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(0.98f, Color.red),
                new ColorRangePoint(1f, Color.white),
            }, new Color(1f, 0.854902f, 0f, 1f));
        }

        internal static ColorRange JipperBpmColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(0f, Color.white),
                new ColorRangePoint(1f, Color.red),
            });
        }

        internal static ColorRange JipperComboColor()
        {
            return new ColorRange(new[]
            {
                new ColorRangePoint(0f, new Color(1f, 0.7098039f, 0.7098039f, 1f)),
                new ColorRangePoint(1f, new Color(1f, 0.3490196f, 0.3490196f, 1f)),
            });
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            EnsureColorRanges();
            Save(this, modEntry);
        }
    }
}
