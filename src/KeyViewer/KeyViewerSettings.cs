
using TMPro;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    [System.Serializable]
    public class KeyViewerSettings
    {
        public int Version = 2;
        public KeyviewerStyle KeyViewerStyle = KeyviewerStyle.Key16;
        public FootKeyviewerStyle FootKeyViewerStyle = FootKeyviewerStyle.Key4;

        public KeyCode[] key8 = {
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash
        };
        public string[] key8Text = new string[8];
        public KeyCode[] key10 = {
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.Comma
        };
        public string[] key10Text = new string[10];
        public KeyCode[] key12 = {
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period
        };
        public string[] key12Text = new string[12];
        public KeyCode[] key14 = {
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period, KeyCode.CapsLock, KeyCode.LeftShift
        };
        public string[] key14Text = new string[14];
        public KeyCode[] key16 = {
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period, KeyCode.CapsLock, KeyCode.LeftShift, KeyCode.Return, KeyCode.H
        };
        public string[] key16Text = new string[16];
        public KeyCode[] key20 = {
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period, KeyCode.CapsLock, KeyCode.LeftShift, KeyCode.Return, KeyCode.H,
            KeyCode.LeftControl, KeyCode.D, KeyCode.RightShift, KeyCode.Semicolon
        };
        public string[] key20Text = new string[20];

        public KeyCode[] footkey2 = { KeyCode.F8, KeyCode.F3 };
        public KeyCode[] footkey4 = { KeyCode.F8, KeyCode.F3, KeyCode.F7, KeyCode.F2 };
        public KeyCode[] footkey6 = { KeyCode.F8, KeyCode.F3, KeyCode.F7, KeyCode.F2, KeyCode.F6, KeyCode.F1 };
        public KeyCode[] footkey8 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1 };
        public KeyCode[] footkey10 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10 };
        public KeyCode[] footkey12 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12 };
        public KeyCode[] footkey14 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12, KeyCode.F13, KeyCode.F14 };
        public KeyCode[] footkey16 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12, KeyCode.F13, KeyCode.F14, KeyCode.F15, KeyCode.F16 };

        public string[] footkey2Text = new string[2];
        public string[] footkey4Text = new string[4];
        public string[] footkey6Text = new string[6];
        public string[] footkey8Text = new string[8];
        public string[] footkey10Text = new string[10];
        public string[] footkey12Text = new string[12];
        public string[] footkey14Text = new string[14];
        public string[] footkey16Text = new string[16];

        public KeyCode[] GhostKey8 = new KeyCode[8];
        public KeyCode[] GhostKey10 = new KeyCode[10];
        public KeyCode[] GhostKey12 = new KeyCode[12];
        public KeyCode[] GhostKey14 = new KeyCode[14];
        public KeyCode[] GhostKey16 = new KeyCode[16];
        public KeyCode[] GhostKey20 = new KeyCode[20];

        public int[] Count = new int[36];
        public int TotalCount;

        public bool DownLocation;
        public float Size = 1f;
        public float KeyFontSize = 1f;
        public float CounterFontSize = 1f;
        public float CounterTextSpacing = 0f;
        public bool EnablePerKeyFontSizes = false;
        public float[] PerKeyFontSize;
        public float[] PerKeyCounterFontSize;
        public bool Enabled = true;

        public Color Background = KeyViewer.Background;
        public Color BackgroundClicked = KeyViewer.BackgroundClicked;
        public Color Outline = KeyViewer.Outline;
        public Color OutlineClicked = KeyViewer.OutlineClicked;
        public Color Text = KeyViewer.Text;
        public Color TextClicked = KeyViewer.TextClicked;
        public Color RainColor = KeyViewer.RainColor;
        public Color RainColor2 = KeyViewer.RainColor2;
        public Color RainColor3 = KeyViewer.RainColor3;
        public Color GhostRainColor = new Color(1f, 1f, 1f, 0.4f);
        public bool SyncToKeyLimiter = false;
        public bool EnableSeparateKpsTotalColors = false;

        public Color KpsBackground = KeyViewer.Background;
        public Color KpsOutline = KeyViewer.Outline;
        public Color KpsText = KeyViewer.Text;

        public Color TotalBackground = KeyViewer.Background;
        public Color TotalOutline = KeyViewer.Outline;
        public Color TotalText = KeyViewer.Text;

        public bool EnableRainEffect = true;
        public bool EnableRainFade = true;
        public bool EnableRainOpacityGradient = false;
        public float RainOpacityGradientLength = 60f;
        public bool EnableGhostRain = false;
        public float RainFadeDuration = 0.5f;
        public bool EnableRainForRow1 = true;
        public bool EnableRainForRow2 = true;
        public bool EnableRainForRow3 = true;
        public float RainSpeedRow1 = 100f;
        public float RainSpeedRow2 = 100f;
        public float RainSpeedRow3 = 100f;
        public float RainHeightRow1 = 275f;
        public float RainHeightRow2 = 275f;
        public float RainHeightRow3 = 275f;

        public Vector2 MainKeyViewerPosition = new Vector2(0, 1);
        public Vector2 FootKeyViewerPosition = new Vector2(0.24f, 1f);
        public bool CustomPositionEnabled = false;

        public int FontIndex = 1;
        public string FontName = "";
        public int FontStyleFlags = 0;
        public string Language = "en";

        public bool EnableCountFormatting = false;
        public bool HideMainKeyCount = false;
        public bool EnablePerKeyKps = false;
        public bool StreamerMode = false;

        public bool EnablePerKeyColors = false;
        public Color[] PerKeyBackground;
        public Color[] PerKeyBackgroundClicked;
        public Color[] PerKeyOutline;
        public Color[] PerKeyOutlineClicked;
        public Color[] PerKeyText;
        public Color[] PerKeyTextClicked;
        public Color[] PerKeyRainColor;

        private const int PerKeySlotCount = 38;

        private static float ClampFontSizeMultiplier(float value, float fallback = 1f)
        {
            if (value <= 0f) value = fallback > 0f ? fallback : 1f;
            return Mathf.Clamp(value, 0.1f, 3f);
        }

        public void EnsurePerKeyFontSizes()
        {
            KeyFontSize = ClampFontSizeMultiplier(KeyFontSize);
            CounterFontSize = ClampFontSizeMultiplier(CounterFontSize);
            CounterTextSpacing = Mathf.Clamp(CounterTextSpacing, -12f, 32f);

            if (PerKeyFontSize == null || PerKeyFontSize.Length != PerKeySlotCount ||
                PerKeyCounterFontSize == null || PerKeyCounterFontSize.Length != PerKeySlotCount)
            {
                ResetPerKeyFontSizesToGlobal();
                return;
            }

            for (int i = 0; i < PerKeySlotCount; i++)
            {
                PerKeyFontSize[i] = ClampFontSizeMultiplier(PerKeyFontSize[i], KeyFontSize);
                PerKeyCounterFontSize[i] = ClampFontSizeMultiplier(PerKeyCounterFontSize[i], CounterFontSize);
            }
        }

        public void ResetPerKeyFontSizesToGlobal()
        {
            KeyFontSize = ClampFontSizeMultiplier(KeyFontSize);
            CounterFontSize = ClampFontSizeMultiplier(CounterFontSize);
            PerKeyFontSize = new float[PerKeySlotCount];
            PerKeyCounterFontSize = new float[PerKeySlotCount];
            for (int i = 0; i < PerKeySlotCount; i++)
            {
                PerKeyFontSize[i] = KeyFontSize;
                PerKeyCounterFontSize[i] = CounterFontSize;
            }
        }

        public void InitPerKeyColors()
        {
            int n = PerKeySlotCount;
            PerKeyBackground = new Color[n];
            PerKeyBackgroundClicked = new Color[n];
            PerKeyOutline = new Color[n];
            PerKeyOutlineClicked = new Color[n];
            PerKeyText = new Color[n];
            PerKeyTextClicked = new Color[n];
            PerKeyRainColor = new Color[n];
            for (int i = 0; i < n; i++)
            {
                PerKeyBackground[i] = KeyViewer.Background;
                PerKeyBackgroundClicked[i] = KeyViewer.BackgroundClicked;
                PerKeyOutline[i] = KeyViewer.Outline;
                PerKeyOutlineClicked[i] = KeyViewer.OutlineClicked;
                PerKeyText[i] = KeyViewer.Text;
                PerKeyTextClicked[i] = KeyViewer.TextClicked;
                if (i < 8) PerKeyRainColor[i] = KeyViewer.RainColor;
                else if (i < 16) PerKeyRainColor[i] = KeyViewer.RainColor2;
                else if (i < 20) PerKeyRainColor[i] = KeyViewer.RainColor3;
                else PerKeyRainColor[i] = KeyViewer.RainColor;
            }
        }

        public KeyViewerSettings()
        {
            key8Text = key8Text ?? new string[8];
            key10Text = key10Text ?? new string[10];
            key12Text = key12Text ?? new string[12];
            key14Text = key14Text ?? new string[14];
            key16Text = key16Text ?? new string[16];
            key20Text = key20Text ?? new string[20];
            footkey2Text = footkey2Text ?? new string[2];
            footkey4Text = footkey4Text ?? new string[4];
            footkey6Text = footkey6Text ?? new string[6];
            footkey8Text = footkey8Text ?? new string[8];
            footkey10Text = footkey10Text ?? new string[10];
            footkey12Text = footkey12Text ?? new string[12];
            footkey14Text = footkey14Text ?? new string[14];
            footkey16Text = footkey16Text ?? new string[16];
            GhostKey8 = GhostKey8 ?? new KeyCode[8];
            GhostKey10 = GhostKey10 ?? new KeyCode[10];
            GhostKey12 = GhostKey12 ?? new KeyCode[12];
            GhostKey14 = GhostKey14 ?? new KeyCode[14];
            GhostKey16 = GhostKey16 ?? new KeyCode[16];
            GhostKey20 = GhostKey20 ?? new KeyCode[20];
            Count = Count ?? new int[36];
            EnsurePerKeyFontSizes();
            if (PerKeyBackground == null || PerKeyBackground.Length != 38 ||
                PerKeyBackgroundClicked == null || PerKeyBackgroundClicked.Length != 38 ||
                PerKeyOutline == null || PerKeyOutline.Length != 38 ||
                PerKeyOutlineClicked == null || PerKeyOutlineClicked.Length != 38 ||
                PerKeyText == null || PerKeyText.Length != 38 ||
                PerKeyTextClicked == null || PerKeyTextClicked.Length != 38 ||
                PerKeyRainColor == null || PerKeyRainColor.Length != 38)
                InitPerKeyColors();
        }
    }

    public class FontEntry
    {
        public string name;
        public TMP_FontAsset font;
        public string sourceFontName;
        public FontEntry(string name, TMP_FontAsset font) { this.name = name; this.font = font; sourceFontName = name; }
    }

    public static class GUIUtils
    {
        public static void DrawRect(Rect position, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(position, Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
