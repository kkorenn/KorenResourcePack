using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KorenResourcePack;
using Object = UnityEngine.Object;

namespace JipperKeyViewer.KeyViewer
{
    public partial class KeyViewer : MonoBehaviour
    {
        private void EnableKeyViewer()
        {
            if (KeyViewerObject != null || !HostKeyViewerEnabled()) return;
            if (!TryLoadResources())
            {
                Main.Mod.Logger.Error("KeyViewer: Cannot load AssetBundle, please check assets/ directory");
                return;
            }
            bool dmNoteMode = IsDmNoteMode();
            KeyViewerObject = new GameObject("Jipper KeyViewer");
            Canvas = KeyViewerObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = Canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            KeyViewerSizeObject = new GameObject("SizeObject");
            RectTransform rectTransform = KeyViewerSizeObject.AddComponent<RectTransform>();
            rectTransform.SetParent(KeyViewerObject.transform);
            float hostScale = dmNoteMode ? GetDmNoteScale() : Settings.Size;
            rectTransform.localScale = new Vector3(hostScale, hostScale, 1);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = Vector2.zero;
            rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
            Keys = new Key[36];
            dmNoteLayoutActive = dmNoteMode;
            if (dmNoteMode)
            {
                InitializeDmNoteKeyViewer();
            }
            else
            {
                InitializeMainKeys(GetLayout(Settings.KeyViewerStyle));
                switch (Settings.FootKeyViewerStyle)
                {
                    case FootKeyviewerStyle.Key2:  InitializeFootKeyViewer(2);  break;
                    case FootKeyviewerStyle.Key4:  InitializeFootKeyViewer(4);  break;
                    case FootKeyviewerStyle.Key6:  InitializeFootKeyViewer(6);  break;
                    case FootKeyviewerStyle.Key8:  InitializeFootKeyViewer(8);  break;
                    case FootKeyviewerStyle.Key10: InitializeFootKeyViewer(10); break;
                    case FootKeyviewerStyle.Key12: InitializeFootKeyViewer(12); break;
                    case FootKeyviewerStyle.Key14: InitializeFootKeyViewer(14); break;
                    case FootKeyviewerStyle.Key16: InitializeFootKeyViewer(16); break;
                }
                if (Settings.StreamerMode)
                {
                    if (Kps != null) Kps.gameObject.SetActive(false);
                    if (Total != null) Total.gameObject.SetActive(false);
                }
            }
            Object.DontDestroyOnLoad(KeyViewerObject);
            PressTimes = new Queue<long>();
            keyPressTimes = new Queue<long>[36];
            lastPerKeyKps = new int[36];
            Stopwatch = System.Diagnostics.Stopwatch.StartNew();
            RefreshAllCountDisplay();
            if (dmNoteMode)
                RefreshDmNoteCountDisplay();
        }

        private void DisableKeyViewer()
        {
            if (KeyViewerObject == null) return;
            Object.Destroy(KeyViewerObject);
            KeyViewerObject = null;
            KeyViewerSizeObject = null;
            rainSystem?.ClearPool();
            foreach (var mat in shadowMaterials.Values)
                Object.Destroy(mat);
            shadowMaterials.Clear();
            Canvas = null;
            Keys = null;
            PressTimes = null;
            keyPressTimes = null;
            lastPerKeyKps = null;
            Stopwatch = null;
            dmNoteRuntimeKeys = null;
            dmNoteLayoutActive = false;
        }

        struct ExtraSlot { public int index; public float x, y, w; public int rainRow; public bool slim; }

        struct LayoutDesc { public float frontY; public float bottomY; public ExtraSlot[] extras; }

        private static LayoutDesc GetLayout(KeyviewerStyle style)
        {
            return style switch
            {
                KeyviewerStyle.Key8 => new LayoutDesc
                {
                    frontY = 279, bottomY = 218,
                    extras = new ExtraSlot[]
                    {
                        new() { index = -1, x = 0, y = 233, w = 212, rainRow = -1, slim = true },
                        new() { index = -2, x = 216, y = 233, w = 212, rainRow = -1, slim = true },
                    }
                },
                KeyviewerStyle.Key10 => new LayoutDesc
                {
                    frontY = 279, bottomY = 200,
                    extras = new ExtraSlot[]
                    {
                        new() { index = 8, x = 162, y = 225, w = 50, rainRow = 1 },
                        new() { index = 9, x = 216, y = 225, w = 50, rainRow = 1 },
                        new() { index = -1, x = 0, y = 225, w = 158, rainRow = -1 },
                        new() { index = -2, x = 270, y = 225, w = 158, rainRow = -1 },
                    }
                },
                KeyviewerStyle.Key12 => new LayoutDesc
                {
                    frontY = 279, bottomY = 200,
                    extras = new ExtraSlot[]
                    {
                        new() { index = 9, x = 108, y = 225, w = 50, rainRow = 1 },
                        new() { index = 8, x = 162, y = 225, w = 50, rainRow = 1 },
                        new() { index = 10, x = 216, y = 225, w = 50, rainRow = 1 },
                        new() { index = 11, x = 270, y = 225, w = 50, rainRow = 1 },
                        new() { index = -1, x = 0, y = 225, w = 104, rainRow = -1 },
                        new() { index = -2, x = 324, y = 225, w = 104, rainRow = -1 },
                    }
                },
                KeyviewerStyle.Key14 => new LayoutDesc
                {
                    frontY = 299, bottomY = 184,
                    extras = new ExtraSlot[]
                    {
                        new() { index = 9, x = 54, y = 245, w = 50, rainRow = 1 },
                        new() { index = 8, x = 108, y = 245, w = 50, rainRow = 1 },
                        new() { index = 10, x = 162, y = 245, w = 50, rainRow = 1 },
                        new() { index = 11, x = 216, y = 245, w = 50, rainRow = 1 },
                        new() { index = 12, x = 270, y = 245, w = 50, rainRow = 1 },
                        new() { index = 13, x = 324, y = 245, w = 50, rainRow = 1 },
                        new() { index = -1, x = 0, y = 199, w = 212, rainRow = -1, slim = true },
                        new() { index = -2, x = 216, y = 199, w = 212, rainRow = -1, slim = true },
                    }
                },
                KeyviewerStyle.Key16 => new LayoutDesc
                {
                    frontY = 320, bottomY = 205,
                    extras = new ExtraSlot[]
                    {
                        new() { index = 12, x = 0, y = 266, w = 50, rainRow = 1 },
                        new() { index = 13, x = 54, y = 266, w = 50, rainRow = 1 },
                        new() { index = 9, x = 108, y = 266, w = 50, rainRow = 1 },
                        new() { index = 8, x = 162, y = 266, w = 50, rainRow = 1 },
                        new() { index = 10, x = 216, y = 266, w = 50, rainRow = 1 },
                        new() { index = 11, x = 270, y = 266, w = 50, rainRow = 1 },
                        new() { index = 14, x = 324, y = 266, w = 50, rainRow = 1 },
                        new() { index = 15, x = 378, y = 266, w = 50, rainRow = 1 },
                        new() { index = -1, x = 0, y = 220, w = 212, rainRow = -1, slim = true },
                        new() { index = -2, x = 216, y = 220, w = 212, rainRow = -1, slim = true },
                    }
                },
                KeyviewerStyle.Key20 => new LayoutDesc
                {
                    frontY = 333, bottomY = 200,
                    extras = new ExtraSlot[]
                    {
                        new() { index = 12, x = 0, y = 279, w = 50, rainRow = 1 },
                        new() { index = 13, x = 54, y = 279, w = 50, rainRow = 1 },
                        new() { index = 9, x = 108, y = 279, w = 50, rainRow = 1 },
                        new() { index = 8, x = 162, y = 279, w = 50, rainRow = 1 },
                        new() { index = 10, x = 216, y = 279, w = 50, rainRow = 1 },
                        new() { index = 11, x = 270, y = 279, w = 50, rainRow = 1 },
                        new() { index = 14, x = 324, y = 279, w = 50, rainRow = 1 },
                        new() { index = 15, x = 378, y = 279, w = 50, rainRow = 1 },
                        new() { index = 17, x = 108, y = 225, w = 50, rainRow = 3 },
                        new() { index = 16, x = 162, y = 225, w = 50, rainRow = 3 },
                        new() { index = 18, x = 216, y = 225, w = 50, rainRow = 3 },
                        new() { index = 19, x = 270, y = 225, w = 50, rainRow = 3 },
                        new() { index = -1, x = 0, y = 225, w = 104, rainRow = -1 },
                        new() { index = -2, x = 324, y = 225, w = 104, rainRow = -1 },
                    }
                },
                _ => throw new System.ArgumentOutOfRangeException(nameof(style), style, null)
            };
        }

        private void InitializeMainKeys(LayoutDesc layout)
        {
            int remove = Settings.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++)
                Keys[i] = CreateKey(i, 54 * i, layout.frontY - remove, 50, 0);
            foreach (var e in layout.extras)
            {
                Key key = CreateKey(e.index, e.x, e.y - remove, e.w, e.rainRow, e.slim);
                if (e.index == -1) Kps = key;
                else if (e.index == -2) Total = key;
                else Keys[e.index] = key;
            }
        }

        private void RepositionMainKeys(LayoutDesc layout, float baseX, float baseY)
        {
            int remove = Settings.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++)
                SetKeyPosition(i, baseX + 54 * i, baseY + layout.frontY - remove);
            foreach (var e in layout.extras)
                SetKeyPosition(e.index, baseX + e.x, baseY + e.y - remove);
        }

        private void ApplyFontSize()
        {
            Settings.EnsurePerKeyFontSizes();
            void Apply(Key k, int slot)
            {
                if (k == null) return;
                if (k.text != null) k.text.fontSizeMax = 20f * GetKeyFontSizeMultiplier(slot);
                if (k.value != null) k.value.fontSizeMax = 20f * GetCounterFontSizeMultiplier(slot);
            }
            if (Keys != null)
            {
                for (int i = 0; i < Keys.Length; i++)
                    Apply(Keys[i], i);
            }
            Apply(Kps, 36);
            Apply(Total, 37);
        }

        private static int FontSizeSlotForKeyIndex(int i)
        {
            if (i >= 0 && i < 36) return i;
            if (i == -1) return 36;
            if (i == -2) return 37;
            return -1;
        }

        private static float GetKeyFontSizeMultiplier(int slot)
        {
            if (Settings.EnablePerKeyFontSizes &&
                Settings.PerKeyFontSize != null && slot >= 0 && slot < Settings.PerKeyFontSize.Length)
                return Mathf.Max(0.01f, Settings.PerKeyFontSize[slot]);
            return Mathf.Max(0.01f, Settings.KeyFontSize);
        }

        private static float GetCounterFontSizeMultiplier(int slot)
        {
            if (Settings.EnablePerKeyFontSizes &&
                Settings.PerKeyCounterFontSize != null && slot >= 0 && slot < Settings.PerKeyCounterFontSize.Length)
                return Mathf.Max(0.01f, Settings.PerKeyCounterFontSize[slot]);
            return Mathf.Max(0.01f, Settings.CounterFontSize);
        }

        private void InitializeFootKeyViewer(int size)
        {
            for (int i = 20; i < 20 + size; i++)
            {
                int col;
                int row;
                if (size <= 8)
                {
                    col = i - 20;
                    row = 0;
                }
                else
                {
                    if (i - 20 < 8)
                    {
                        col = i - 20;
                        row = 0;
                    }
                    else
                    {
                        col = (i - 20) - 8;
                        row = 1;
                    }
                }
                int baseY = size > 8 ? 15 + 34 : 15;
                int x = 432 + col * 34;
                if (size > 8 && row == 1)
                    x += (8 - (size - 8)) * 17;
                int y = baseY - row * 34;
                Keys[i] = CreateKey(i, x, y, 30, -1, true, false);
            }
        }

        private Key CreateKey(int i, float x, float y, float sizeX, int raining, bool slim = false, bool count = true)
        {
            if (i >= 0 && i < 20 && Settings.HideMainKeyCount)
                count = false;
            float textCounterSpacing = Mathf.Clamp(Settings.CounterTextSpacing, -12f, 32f);
            float halfTextCounterSpacing = textCounterSpacing * 0.5f;
            GameObject obj = new("Key " + i);
            KeyViewerSettings settings = Settings;
            RectTransform transform = obj.AddComponent<RectTransform>();
            transform.SetParent(KeyViewerSizeObject.transform);
            transform.sizeDelta = new Vector2(sizeX, slim ? 30 : 50);
            transform.anchorMin = transform.anchorMax = Vector2.zero;
            transform.pivot = new Vector2(0, 0.5f);
            transform.anchoredPosition = new Vector2(x, y);
            transform.localScale = Vector3.one;
            Key key = obj.AddComponent<Key>();
            key.isPressed = false;
            int pi = FontSizeSlotForKeyIndex(i);
            GameObject gameObject;
            Image image;
            TextMeshProUGUI text;
            gameObject = new GameObject("Background");
            transform = gameObject.AddComponent<RectTransform>();
            transform.SetParent(obj.transform);
            transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = new Vector2(sizeX * 2, (slim ? 30 : 50) * 2);
            transform.localScale = new Vector3(0.5f, 0.5f);
            image = gameObject.AddComponent<Image>();
            image.color = settings.Background;
            if (keyBackgroundSprite != null)
            {
                image.sprite = keyBackgroundSprite;
                image.type = Image.Type.Sliced;
            }
            image.raycastTarget = false;
            key.background = image;
            gameObject = new GameObject("Outline");
            transform = gameObject.AddComponent<RectTransform>();
            transform.SetParent(obj.transform);
            transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = new Vector2(sizeX * 2, (slim ? 30 : 50) * 2);
            transform.localScale = new Vector3(0.5f, 0.5f);
            image = gameObject.AddComponent<Image>();
            image.color = settings.Outline;
            if (keyOutlineSprite != null)
            {
                image.sprite = keyOutlineSprite;
                image.type = Image.Type.Sliced;
            }
            image.raycastTarget = false;
            key.outline = image;
            gameObject = new GameObject("KeyText");
            transform = gameObject.AddComponent<RectTransform>();
            transform.SetParent(obj.transform);
            if (slim)
            {
                transform.sizeDelta = new Vector2(sizeX / 2, 30);
                transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0, 0.5f);
                transform.anchoredPosition = new Vector2(count ? 10 - halfTextCounterSpacing : 7.5f, 0);
            }
            else
            {
                transform.sizeDelta = new Vector2(sizeX - 4, 32);
                if (!count)
                {
                    transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 0.5f);
                    transform.anchoredPosition = Vector2.zero;
                }
                else
                {
                    transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 1);
                    transform.anchoredPosition = new Vector2(0, 2 + halfTextCounterSpacing);
                }
            }
            transform.localScale = Vector3.one;
            text = gameObject.AddComponent<TextMeshProUGUI>();
            ApplyKrpFont(text);
            text.fontStyle = (TMPro.FontStyles)settings.FontStyleFlags;
            text.enableAutoSizing = true;
            text.fontSizeMin = 0;
            text.fontSizeMax = 20f * GetKeyFontSizeMultiplier(pi);
            text.alignment = slim ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
            text.color = settings.Text;
            text.raycastTarget = false;
            key.text = text;
            if (count)
            {
                gameObject = new GameObject("CountText");
                transform = gameObject.AddComponent<RectTransform>();
                transform.SetParent(obj.transform);
                if (slim)
                {
                    transform.sizeDelta = new Vector2(sizeX / 2, 30);
                    transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(1, 0.5f);
                    transform.anchoredPosition = new Vector2(-10 + halfTextCounterSpacing, 0);
                }
                else
                {
                    transform.sizeDelta = new Vector2(sizeX - 4, 16);
                    transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 0);
                    transform.anchoredPosition = new Vector2(0, 2 - halfTextCounterSpacing);
                }
                transform.localScale = Vector3.one;
                text = gameObject.AddComponent<TextMeshProUGUI>();
                ApplyKrpFont(text);
                text.fontStyle = (FontStyles)settings.FontStyleFlags;
                text.enableAutoSizing = true;
                text.fontSizeMin = 0;
                text.fontSizeMax = 20f * GetCounterFontSizeMultiplier(pi);
                text.raycastTarget = false;
                text.alignment = slim ? TextAlignmentOptions.Right : TextAlignmentOptions.Top;
                text.color = settings.Text;
                key.value = text;
            }
            UpdateKeyText(key, i);
            if (raining >= 0)
            {
                if (key.rain == null)
                {
                    key.rain = new GameObject("RainLine");
                    transform = key.rain.AddComponent<RectTransform>();
                    transform.SetParent(obj.transform);
                    transform.sizeDelta = new Vector2(sizeX, 275);
                    transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
                    transform.anchoredPosition = new Vector2(0, raining switch
                    {
                        0 => -223,
                        3 => -115,
                        _ => -169
                    });
                    transform.localScale = Vector3.one;
                    key.rain.AddComponent<Canvas>();
                    key.rain.AddComponent<GraphicRaycaster>();
                }
                key.color = (byte)raining;
            }
            else
            {
                key.color = 1;
                key.rain?.SetActive(false);
                key.rain = null;
            }
            if (Settings.EnablePerKeyColors && (pi < 36 || Settings.EnableSeparateKpsTotalColors))
            {
                if (pi >= 0)
                {
                    key.background.color = Settings.PerKeyBackground[pi];
                    key.outline.color = Settings.PerKeyOutline[pi];
                    key.text.color = Settings.PerKeyText[pi];
                    if (key.value != null) key.value.color = Settings.PerKeyText[pi];
                    key.rainColor = Settings.PerKeyRainColor[pi];
                }
            }
            else if (pi >= 36 && Settings.EnableSeparateKpsTotalColors)
            {
                key.background.color = pi == 36 ? Settings.KpsBackground : Settings.TotalBackground;
                key.outline.color = pi == 36 ? Settings.KpsOutline : Settings.TotalOutline;
                key.text.color = pi == 36 ? Settings.KpsText : Settings.TotalText;
                if (key.value != null) key.value.color = key.text.color;
            }
            if (!Settings.EnablePerKeyColors && raining >= 0)
            {
                key.rainColor = rainSystem.GetRainColor((byte)raining);
            }
            return key;
        }

        private static void UpdateKeyText(Key key, int i)
        {
            if (key == null) return;
            if (i == -1)
            {
                key.text.text = "KPS";
                if (key.value != null) key.value.text = "0";
                return;
            }
            if (i == -2)
            {
                key.text.text = "Total";
                if (key.value != null) key.value.text = FormatCount(Settings.TotalCount);
                return;
            }
            if (i < 20)
            {
                KeyCode[] keyCodes = GetKeyCode();
                string[] keyTexts = GetKeyText();
                if (keyCodes != null && keyTexts != null && i < keyCodes.Length && i < keyTexts.Length)
                {
                    string displayText = !string.IsNullOrEmpty(keyTexts[i]) ? keyTexts[i] : KeyToString(keyCodes[i]);
                    key.text.text = displayText;
                    if (key.value != null)
                        key.value.text = FormatCount(Settings.Count[i]);
                }
            }
            else
            {
                KeyCode[] footKeyCodes = GetFootKeyCode();
                string[] footTexts = GetFootKeyText();
                int footIndex = i - 20;
                if (footKeyCodes != null && footIndex >= 0 && footIndex < footKeyCodes.Length)
                {
                    string displayText = footTexts != null && footIndex < footTexts.Length && !string.IsNullOrEmpty(footTexts[footIndex])
                        ? footTexts[footIndex] : KeyToString(footKeyCodes[footIndex]);
                    key.text.text = displayText;
                }
            }
        }

        private float GetMinMainKeyOffset()
        {
            float bottomY = GetLayout(Settings.KeyViewerStyle).bottomY;
            return Settings.DownLocation ? bottomY - 200 : bottomY;
        }

        private float GetMainLayoutRightmostOffset() => 428f;

        private float GetMaxMainKeyOffset()
        {
            return GetLayout(Settings.KeyViewerStyle).frontY + 25;
        }

        private float GetFootLayoutRightmostOffset(int size)
        {
            int row0Cols = Mathf.Min(size, 8);
            return (row0Cols - 1) * 34 + 30;
        }

        private void ResetKeyViewerPosition()
        {
            if (Keys == null || !Settings.CustomPositionEnabled) return;
            Vector2 norm = Settings.MainKeyViewerPosition;
            float r = GetMainLayoutRightmostOffset();
            float baseX = norm.x * (CanvasWidth - r);
            int remove = Settings.DownLocation ? 200 : 0;
            float topBaseY = 1080f - GetMaxMainKeyOffset() + remove;
            float bottomBaseY = -GetMinMainKeyOffset();
            float baseY = Mathf.Lerp(bottomBaseY, topBaseY, 1f - norm.y);
            RepositionMainKeys(GetLayout(Settings.KeyViewerStyle), baseX, baseY);
        }

        private void ResetFootKeyViewerPosition()
        {
            if (Keys == null || !Settings.CustomPositionEnabled) return;
            Vector2 norm = Settings.FootKeyViewerPosition;
            int size = 0;
            switch (Settings.FootKeyViewerStyle)
            {
                case FootKeyviewerStyle.Key2: size = 2; break;
                case FootKeyviewerStyle.Key4: size = 4; break;
                case FootKeyviewerStyle.Key6: size = 6; break;
                case FootKeyviewerStyle.Key8: size = 8; break;
                case FootKeyviewerStyle.Key10: size = 10; break;
                case FootKeyviewerStyle.Key12: size = 12; break;
                case FootKeyviewerStyle.Key14: size = 14; break;
                case FootKeyviewerStyle.Key16: size = 16; break;
                default: return;
            }
            float r = GetFootLayoutRightmostOffset(size);
            float baseX = norm.x * (CanvasWidth - r);
            float footTopOffset = size <= 8 ? 15f : 49f;
            float topBaseY = 1080f - footTopOffset;
            float bottomBaseY = 15f;
            float baseY = Mathf.Lerp(bottomBaseY, topBaseY, 1f - norm.y);
            int firstRowCount = size <= 8 ? size : 8;
            float yBase = size > 8 ? baseY + 34 : baseY;
            for (int i = 20; i < 20 + size; i++)
            {
                int offset = i - 20;
                if (offset < firstRowCount)
                {
                    SetKeyPosition(i, baseX + offset * 34, yBase);
                }
                else
                {
                    int col = offset - firstRowCount;
                    float x = baseX + col * 34 + (firstRowCount - (size - firstRowCount)) * 17;
                    SetKeyPosition(i, x, yBase - 34);
                }
            }
        }

        private int lastScreenWidth, lastScreenHeight;
        private float canvasWidth;

        private float CanvasWidth
        {
            get
            {
                if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
                {
                    canvasWidth = Screen.width * 1080f / Screen.height;
                    lastScreenWidth = Screen.width;
                    lastScreenHeight = Screen.height;
                }
                return canvasWidth;
            }
        }

        private void SetKeyPosition(int keyIndex, float x, float y)
        {
            if (keyIndex == -1 && Kps != null)
            {
                ((RectTransform)Kps.transform).anchoredPosition = new Vector2(x, y);
            }
            else if (keyIndex == -2 && Total != null)
            {
                ((RectTransform)Total.transform).anchoredPosition = new Vector2(x, y);
            }
            else if (keyIndex >= 0 && keyIndex < Keys.Length && Keys[keyIndex] != null)
            {
                ((RectTransform)Keys[keyIndex].transform).anchoredPosition = new Vector2(x, y);
            }
        }

        private Color GetColorByIndex(int index)
        {
            return index switch
            {
                0 => Settings.Background,
                1 => Settings.BackgroundClicked,
                2 => Settings.Outline,
                3 => Settings.OutlineClicked,
                4 => Settings.Text,
                5 => Settings.TextClicked,
                6 => Settings.RainColor,
                7 => Settings.RainColor2,
                8 => Settings.RainColor3,
                9 => Settings.GhostRainColor,
                _ => Color.white
            };
        }

        private void SetColorByIndex(int index, Color color)
        {
            switch (index)
            {
                case 0: Settings.Background = color; break;
                case 1: Settings.BackgroundClicked = color; break;
                case 2: Settings.Outline = color; break;
                case 3: Settings.OutlineClicked = color; break;
                case 4: Settings.Text = color; break;
                case 5: Settings.TextClicked = color; break;
                case 6: Settings.RainColor = color; break;
                case 7: Settings.RainColor2 = color; break;
                case 8: Settings.RainColor3 = color; break;
                case 9: Settings.GhostRainColor = color; break;
            }
        }

        private void UpdateAllKeyColors()
        {
            if (Keys == null) return;
            if (Settings.EnablePerKeyColors)
            {
                for (int i = 0; i < Keys.Length; i++)
                {
                    if (Keys[i] == null) continue;
                    Keys[i].background.color = Settings.PerKeyBackground[i];
                    Keys[i].outline.color = Settings.PerKeyOutline[i];
                    Keys[i].text.color = Settings.PerKeyText[i];
                    if (Keys[i].value != null) Keys[i].value.color = Settings.PerKeyText[i];
                    Keys[i].rainColor = Settings.PerKeyRainColor[i];
                }
            }
            else
            {
                KeyCode[] keyCodes = GetKeyCode();
                KeyCode[] footKeyCodes = GetFootKeyCode();
                for (int i = 0; i < keyCodes.Length && i < Keys.Length; i++)
                {
                    if (Keys[i] != null)
                    {
                        Keys[i].background.color = Settings.Background;
                        Keys[i].outline.color = Settings.Outline;
                        Keys[i].text.color = Settings.Text;
                        if (Keys[i].value != null) Keys[i].value.color = Settings.Text;
                        Keys[i].rainColor = rainSystem.GetRainColor(Keys[i].color);
                    }
                }
                if (footKeyCodes != null)
                {
                    for (int i = 0; i < footKeyCodes.Length; i++)
                    {
                        int index = i + 20;
                        if (index < Keys.Length && Keys[index] != null)
                        {
                            Keys[index].background.color = Settings.Background;
                            Keys[index].outline.color = Settings.Outline;
                            Keys[index].text.color = Settings.Text;
                            if (Keys[index].value != null) Keys[index].value.color = Settings.Text;
                        }
                    }
                }
            }
            void ApplyGlobalOrPerKey(Key k, int pi)
            {
                if (k == null) return;
                if (Settings.EnablePerKeyColors && (pi < 36 || Settings.EnableSeparateKpsTotalColors))
                {
                    k.background.color = Settings.PerKeyBackground[pi];
                    k.outline.color = Settings.PerKeyOutline[pi];
                    k.text.color = Settings.PerKeyText[pi];
                    if (k.value != null) k.value.color = Settings.PerKeyText[pi];
                }
                else if (pi == 36 && Settings.EnableSeparateKpsTotalColors)
                {
                    k.background.color = Settings.KpsBackground;
                    k.outline.color = Settings.KpsOutline;
                    k.text.color = Settings.KpsText;
                    if (k.value != null) k.value.color = Settings.KpsText;
                }
                else if (pi == 37 && Settings.EnableSeparateKpsTotalColors)
                {
                    k.background.color = Settings.TotalBackground;
                    k.outline.color = Settings.TotalOutline;
                    k.text.color = Settings.TotalText;
                    if (k.value != null) k.value.color = Settings.TotalText;
                }
                else
                {
                    k.background.color = Settings.Background;
                    k.outline.color = Settings.Outline;
                    k.text.color = Settings.Text;
                    if (k.value != null) k.value.color = Settings.Text;
                }
            }
            ApplyGlobalOrPerKey(Kps, 36);
            ApplyGlobalOrPerKey(Total, 37);
        }

        private void ChangeKeyViewer()
        {
            ResetKeyViewer();
        }

        private void ResetKeyViewer()
        {
            SelectedKey = -1;
            if (Keys != null)
            {
                rainSystem.ClearActiveDrops(Keys);
                for (int i = 0; i < 20; i++)
                {
                    if (Keys[i] != null && Keys[i].gameObject != null)
                        Object.Destroy(Keys[i].gameObject);
                }
                if (Total != null && Total.gameObject != null)
                    Object.Destroy(Total.gameObject);
                if (Kps != null && Kps.gameObject != null)
                    Object.Destroy(Kps.gameObject);
            }
            rainSystem.ClearPool();
            InitializeMainKeys(GetLayout(Settings.KeyViewerStyle));
            if (Settings.StreamerMode)
            {
                if (Kps != null) Kps.gameObject.SetActive(false);
                if (Total != null) Total.gameObject.SetActive(false);
            }
            if (Settings.CustomPositionEnabled)
                ResetKeyViewerPosition();
            RefreshAllCountDisplay();
        }

        private void ResetFootKeyViewer()
        {
            SelectedKey = -1;
            if (Keys != null)
            {
                for (int i = 20; i < 36; i++)
                {
                    var key = Keys[i];
                    if (key == null) continue;
                    foreach (var rain in key.rainList)
                    {
                        if (rain.rainComponent != null)
                        {
                            rainSystem.ReturnRain(rain.rainComponent);
                            rain.rainComponent = null;
                        }
                        rainSystem.ReturnRawRain(rain);
                    }
                    key.rainList.Clear();
                }
                for (int i = 20; i < 36; i++)
                {
                    if (Keys[i] != null && Keys[i].gameObject != null)
                        Object.Destroy(Keys[i].gameObject);
                }
            }
            rainSystem.ClearPool();
            switch (Settings.FootKeyViewerStyle)
            {
                case FootKeyviewerStyle.Key2:  InitializeFootKeyViewer(2);  break;
                case FootKeyviewerStyle.Key4:  InitializeFootKeyViewer(4);  break;
                case FootKeyviewerStyle.Key6:  InitializeFootKeyViewer(6);  break;
                case FootKeyviewerStyle.Key8:  InitializeFootKeyViewer(8);  break;
                case FootKeyviewerStyle.Key10: InitializeFootKeyViewer(10); break;
                case FootKeyviewerStyle.Key12: InitializeFootKeyViewer(12); break;
                case FootKeyviewerStyle.Key14: InitializeFootKeyViewer(14); break;
                case FootKeyviewerStyle.Key16: InitializeFootKeyViewer(16); break;
            }
            if (Settings.CustomPositionEnabled)
                ResetFootKeyViewerPosition();
            RefreshAllCountDisplay();
        }

        private static KeyCode[] GetKeyCode()
        {
            return Settings.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => Settings.key8,
                KeyviewerStyle.Key12 => Settings.key12,
                KeyviewerStyle.Key14 => Settings.key14,
                KeyviewerStyle.Key16 => Settings.key16,
                KeyviewerStyle.Key20 => Settings.key20,
                KeyviewerStyle.Key10 => Settings.key10,
                _ => Settings.key16
            };
        }

        private static KeyCode[] GetFootKeyCode()
        {
            return Settings.FootKeyViewerStyle switch
            {
                FootKeyviewerStyle.Key2 => Settings.footkey2,
                FootKeyviewerStyle.Key4 => Settings.footkey4,
                FootKeyviewerStyle.Key6 => Settings.footkey6,
                FootKeyviewerStyle.Key8 => Settings.footkey8,
                FootKeyviewerStyle.Key10 => Settings.footkey10,
                FootKeyviewerStyle.Key12 => Settings.footkey12,
                FootKeyviewerStyle.Key14 => Settings.footkey14,
                FootKeyviewerStyle.Key16 => Settings.footkey16,
                _ => new KeyCode[0]
            };
        }

        internal static void SyncKeysToKeyLimiter()
        {
            var krp = KorenResourcePack.Main.settings;
            if (krp == null || Settings == null || !Settings.SyncToKeyLimiter) return;

            int[] result = GetActiveLimiterKeyCodes();
            if (result == null) return;

            int[] current = krp.KeyLimiterAllowed;
            if (current != null && current.Length == result.Length)
            {
                bool same = true;
                for (int i = 0; i < current.Length; i++)
                    if (current[i] != result[i]) { same = false; break; }
                if (same) return;
            }
            krp.KeyLimiterAllowed = result;
            SettingsGui.MarkSettingsDirty();
        }

        internal static int[] GetActiveLimiterKeyCodes()
        {
            if (Settings == null) return null;
            var seen = new HashSet<int>();
            var result = new List<int>();
            void Collect(KeyCode[] arr)
            {
                if (arr == null) return;
                for (int i = 0; i < arr.Length; i++)
                {
                    int c = global::KorenResourcePack.KeyCodeCompat.NormalizeKeyCode((int)arr[i]);
                    if (c == 0) continue;
                    if (seen.Add(c)) result.Add(c);
                }
            }
            Collect(GetKeyCode());
            Collect(GetFootKeyCode());
            return result.ToArray();
        }

        private static KeyCode[] GetGhostKeyCode()
        {
            return Settings.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => Settings.GhostKey8,
                KeyviewerStyle.Key10 => Settings.GhostKey10,
                KeyviewerStyle.Key12 => Settings.GhostKey12,
                KeyviewerStyle.Key14 => Settings.GhostKey14,
                KeyviewerStyle.Key16 => Settings.GhostKey16,
                KeyviewerStyle.Key20 => Settings.GhostKey20,
                _ => Settings.GhostKey16
            };
        }

        private static string[] GetKeyText()
        {
            return Settings.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => Settings.key8Text,
                KeyviewerStyle.Key12 => Settings.key12Text,
                KeyviewerStyle.Key14 => Settings.key14Text,
                KeyviewerStyle.Key16 => Settings.key16Text,
                KeyviewerStyle.Key20 => Settings.key20Text,
                KeyviewerStyle.Key10 => Settings.key10Text,
                _ => Settings.key16Text
            };
        }

        private static string[] GetFootKeyText()
        {
            return Settings.FootKeyViewerStyle switch
            {
                FootKeyviewerStyle.Key2 => Settings.footkey2Text,
                FootKeyviewerStyle.Key4 => Settings.footkey4Text,
                FootKeyviewerStyle.Key6 => Settings.footkey6Text,
                FootKeyviewerStyle.Key8 => Settings.footkey8Text,
                FootKeyviewerStyle.Key10 => Settings.footkey10Text,
                FootKeyviewerStyle.Key12 => Settings.footkey12Text,
                FootKeyviewerStyle.Key14 => Settings.footkey14Text,
                FootKeyviewerStyle.Key16 => Settings.footkey16Text,
                _ => new string[0]
            };
        }

        private static byte[] GetBackSequence()
        {
            return Settings.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => BackSequence8,
                KeyviewerStyle.Key12 => BackSequence12,
                KeyviewerStyle.Key14 => BackSequence14,
                KeyviewerStyle.Key16 => BackSequence16,
                KeyviewerStyle.Key20 => BackSequence20,
                KeyviewerStyle.Key10 => BackSequence10,
                _ => BackSequence16
            };
        }

        private static string FormatCount(int count)
        {
            return Settings.EnableCountFormatting ? count.ToString("N0") : count.ToString();
        }

        public void RefreshAllCountDisplay()
        {
            if (Keys == null) return;
            if (IsDmNoteMode())
            {
                RefreshDmNoteCountDisplay();
                return;
            }
            for (int i = 0; i < Keys.Length; i++)
            {
                if (Keys[i] != null && Keys[i].value != null)
                {
                    if (Settings.EnablePerKeyKps)
                        Keys[i].value.text = (keyPressTimes != null && i < keyPressTimes.Length && keyPressTimes[i] != null) ? keyPressTimes[i].Count.ToString() : "0";
                    else if (Settings.Count != null && i < Settings.Count.Length)
                        Keys[i].value.text = FormatCount(Settings.Count[i]);
                }
            }
            if (Total != null && Total.value != null)
                Total.value.text = FormatCount(Settings.TotalCount);
        }

        public void AutoAssignRainbowColors()
        {
            int n = 38;
            Settings.EnablePerKeyColors = true;
            for (int i = 0; i < n; i++)
            {
                float hue = i * 0.618033988f;
                hue -= Mathf.Floor(hue);
                float h = hue * 6f;
                int sector = (int)h;
                float f = h - sector;
                float p = 0.9f * (1f - 0.85f);
                float q = 0.9f * (1f - 0.85f * f);
                float t = 0.9f * (1f - 0.85f * (1f - f));
                float r, g, b;
                switch (sector % 6)
                {
                    case 0: r = 0.9f; g = t; b = p; break;
                    case 1: r = q; g = 0.9f; b = p; break;
                    case 2: r = p; g = 0.9f; b = t; break;
                    case 3: r = p; g = q; b = 0.9f; break;
                    case 4: r = t; g = p; b = 0.9f; break;
                    default: r = 0.9f; g = p; b = q; break;
                }
                Color baseColor = new Color(r, g, b);
                float bright = baseColor.grayscale > 0.5f ? 0f : 1f;
                Settings.PerKeyBackground[i] = baseColor;
                Settings.PerKeyBackgroundClicked[i] = Color.Lerp(baseColor, Color.white, 0.5f);
                Settings.PerKeyOutline[i] = baseColor;
                Settings.PerKeyOutlineClicked[i] = Color.Lerp(baseColor, Color.white, 0.7f);
                Settings.PerKeyText[i] = new Color(bright, bright, bright);
                Settings.PerKeyTextClicked[i] = new Color(1f - bright, 1f - bright, 1f - bright);
                Settings.PerKeyRainColor[i] = baseColor;
            }
            ResetKeyViewer();
            ResetFootKeyViewer();
            SaveSettings();
        }
    }
}
