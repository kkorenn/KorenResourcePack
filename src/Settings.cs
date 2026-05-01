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
            public float updInterval = 10;

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

            public bool judgementOn = true;
            public bool judgementExpanded = false;
            public bool LocationUp = false;
            public float judgementPositionY = 0;

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

            public override void Save(UnityModManager.ModEntry modEntry)
            {
                Save(this, modEntry);
            }
        }
    }
}
