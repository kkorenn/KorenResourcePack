using UnityEngine;
using UnityModManagerNet;

namespace KorenResourcePack
{
    public static partial class Main
    {
        public class Settings : UnityModManager.ModSettings
        {
            public string language = "en";
            public float size = 1f;
            public string fontName = "";

            public bool progressBarOn = true;
            public bool progressBarExpanded = false;
            public float ProgressBarFillR = 0.97f, ProgressBarFillG = 0.99f, ProgressBarFillB = 1.00f, ProgressBarFillA = 0.96f;
            public float ProgressBarBackR = 0.05f, ProgressBarBackG = 0.05f, ProgressBarBackB = 0.06f, ProgressBarBackA = 0.80f;
            public float ProgressBarBorderR = 0.98f, ProgressBarBorderG = 0.99f, ProgressBarBorderB = 1.00f, ProgressBarBorderA = 0.68f;

            public bool statusOn = true;
            public bool statusExpanded = false;
            public bool ShowProgress = true;
            public bool ShowAccuracy = false;
            public bool ShowXAccuracy = true;
            public bool ShowMusicTime = true;
            public bool ShowCheckpoint = false;
            public bool ShowBest = false;
            public bool ShowFPS = true;
            public bool fpsExpanded = false;

            public bool bpmOn = true;
            public bool bpmExpanded = false;
            public float BpmColorMax = 8000f;
            public float BpmColorLowR = 1f, BpmColorLowG = 1f, BpmColorLowB = 1f, BpmColorLowA = 1f;
            public float BpmColorHighR = 1f, BpmColorHighG = 0f, BpmColorHighB = 1f, BpmColorHighA = 1f;

            public bool comboOn = true;
            public bool comboExpanded = false;
            public bool EnableAutoCombo = true;
            public int ComboColorMax = 1000;
            public float ComboColorLowR = 1f, ComboColorLowG = 1f, ComboColorLowB = 1f, ComboColorLowA = 1f;
            public float ComboColorHighR = 0.72f, ComboColorHighG = 0.35f, ComboColorHighB = 1f, ComboColorHighA = 1f;
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

            // ----------------- Simple KeyViewer (mode = "simple") -----------------
            // Mode selector. "dmnote" = JSON preset (advanced; existing behavior).
            // "simple" = hardcoded Key10/12/16/20 layouts with sliced sprite backgrounds,
            // KPS/Total boxes, and rain particles. Designed for users who don't want to
            // hand-author a JSON preset.
            public string KeyViewerMode = "dmnote";
            // 0 = Key10, 1 = Key12, 2 = Key16, 3 = Key20.
            public int KeyViewerSimpleStyle = 1;
            // Down-mode shifts the whole rig 200px lower (matches Jipper's "DownLocation").
            public bool KeyViewerSimpleDownLocation = false;
            public float KeyViewerSimpleSize = 1f;
            public bool KeyViewerSimpleUseRain = true;
            public float KeyViewerSimpleRainSpeed = 100f;
            public float KeyViewerSimpleRainHeight = 200f;
            // Per-key counts for Simple mode. Index follows the slot scheme below:
            // 0-7   = top hand row, 8-15 = bottom hand row (12/16/20 key fills lower entries),
            // 16-19 = extra row for Key20, 20-35 = foot keys (Key2-16, currently unused).
            public int[] KeyViewerSimpleCount = new int[36];
            public int KeyViewerSimpleTotalCount = 0;
            // KeyCode arrays per layout (stored as ints because KeyCode isn't directly
            // serializable through UMM ModSettings without a custom converter).
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
            // Color slots for Simple mode keys.
            // Background: rgba(143, 60, 255, 50)  (#8F3CFF / 0.196 alpha)
            public float SKvBgR = 0.5607843f, SKvBgG = 0.2352941f, SKvBgB = 1f, SKvBgA = 0.1960784f;
            // BackgroundClicked: white
            public float SKvBgcR = 1f, SKvBgcG = 1f, SKvBgcB = 1f, SKvBgcA = 1f;
            // Outline: rgb(141, 62, 255)
            public float SKvOutR = 0.5529412f, SKvOutG = 0.2431373f, SKvOutB = 1f, SKvOutA = 1f;
            // OutlineClicked: white
            public float SKvOutcR = 1f, SKvOutcG = 1f, SKvOutcB = 1f, SKvOutcA = 1f;
            // Text: white
            public float SKvTxtR = 1f, SKvTxtG = 1f, SKvTxtB = 1f, SKvTxtA = 1f;
            // TextClicked: black
            public float SKvTxtcR = 0f, SKvTxtcG = 0f, SKvTxtcB = 0f, SKvTxtcA = 1f;
            // Rain colors. RainColor = rgb(131, 32, 219); RainColor2 = white; RainColor3 = magenta
            public float SKvRainR = 0.5137255f, SKvRainG = 0.1254902f, SKvRainB = 0.8588235f, SKvRainA = 1f;
            public float SKvRain2R = 1f, SKvRain2G = 1f, SKvRain2B = 1f, SKvRain2A = 1f;
            public float SKvRain3R = 1f, SKvRain3G = 0f, SKvRain3B = 1f, SKvRain3A = 1f;

            // Resource Changer (Jipper-style). Currently scoped to the Otto / RDC.auto editor icon.
            public bool ResourceChangerOn = false;
            public bool ResourceChangerExpanded = false;
            public bool ChangeOttoIcon = true;
            // Single base color for the Otto icon. Default = #FF0000 (red).
            // The "auto off" tint is derived at render time by multiplying RGB by ~0.343
            // (the same ratio Jipper uses between #9900FF and #320054), so the user only
            // ever picks one color and the dim variant follows.
            public float OttoR = 1f, OttoG = 0f, OttoB = 0f, OttoA = 1f;

            public override void Save(UnityModManager.ModEntry modEntry)
            {
                Save(this, modEntry);
            }
        }
    }
}
