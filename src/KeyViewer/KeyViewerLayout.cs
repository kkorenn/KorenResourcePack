using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KorenResourcePack;
using Object = UnityEngine.Object;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Layout and positioning: creating, initializing, positioning key elements / 布局和定位：创建、初始化、定位按键元素
    /// </summary>
    public partial class KeyViewer : MonoBehaviour
    {
        /// <summary>
        /// Create the canvas overlay and initialize all keys / 创建画布覆盖层并初始化所有按键
        /// Called when the mod is toggled on or when settings require rebuilding / 在 Mod 打开或设置需要重建时调用
        /// </summary>
        private void EnableKeyViewer()
        {
            if (KeyViewerObject != null || !Settings.Enabled) return;
            if (!TryLoadResources())
            {
                Main.Mod.Logger.Error("KeyViewer: Cannot load AssetBundle, please check assets/ directory");
                return;
            }
            // Create ScreenSpaceOverlay canvas (independent of game UI) / 创建 ScreenSpaceOverlay 画布（独立于游戏 UI）
            KeyViewerObject = new GameObject("Jipper KeyViewer");
            Canvas = KeyViewerObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = Canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f; // Match height so vertical positions are resolution-independent / 匹配高度使垂直位置不受分辨率影响
            // SizeObject applies the Size scale and serves as parent for all keys / SizeObject 应用大小缩放并作为所有按键的父级
            KeyViewerSizeObject = new GameObject("SizeObject");
            RectTransform rectTransform = KeyViewerSizeObject.AddComponent<RectTransform>();
            rectTransform.SetParent(KeyViewerObject.transform);
            rectTransform.localScale = new Vector3(Settings.Size, Settings.Size, 1);
            // Fill full canvas with bottom-left pivot so localScale doesn't shift child positions / 填满画布，左下角轴心，使缩放不改变子元素位置
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = Vector2.zero;
            rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
            // Initialize main keys based on selected layout / 根据选中的布局初始化主按键
            Keys = new Key[36];
            InitializeMainKeys(GetLayout(Settings.KeyViewerStyle));
            // Initialize foot keys based on selected layout / 根据选中的布局初始化脚键
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
            // Apply streamer mode (hide KPS/Total)
            if (Settings.StreamerMode)
            {
                if (Kps != null) Kps.gameObject.SetActive(false);
                if (Total != null) Total.gameObject.SetActive(false);
            }
            // Persist the overlay across scene loads / 使覆盖层在场景加载中持久化
            Object.DontDestroyOnLoad(KeyViewerObject);
            PressTimes = new Queue<long>();
            keyPressTimes = new Queue<long>[36];
            lastPerKeyKps = new int[36];
            Stopwatch = System.Diagnostics.Stopwatch.StartNew();
            RefreshAllCountDisplay();
        }

        /// <summary>
        /// Destroy the canvas overlay and clean up all resources / 销毁画布覆盖层并清理所有资源
        /// </summary>
        private void DisableKeyViewer()
        {
            if (KeyViewerObject == null) return;
            Object.Destroy(KeyViewerObject);
            KeyViewerObject = null;
            KeyViewerSizeObject = null;
            rainSystem?.ClearPool();
            // Destroy shadow materials / 销毁阴影材质
            foreach (var mat in shadowMaterials.Values)
                Object.Destroy(mat);
            shadowMaterials.Clear();
            Canvas = null;
            Keys = null;
            PressTimes = null;
            keyPressTimes = null;
            lastPerKeyKps = null;
            Stopwatch = null;
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

        /// <summary>
        /// Apply the current key/counter font size multipliers to all live key text components without rebuilding / 将当前按键/计数字体大小倍率实时应用到所有按键文本组件，无需重建
        /// </summary>
        private void ApplyFontSize()
        {
            float keyMax = 20f * Mathf.Max(0.01f, Settings.KeyFontSize);
            float counterMax = 20f * Mathf.Max(0.01f, Settings.CounterFontSize);
            void Apply(Key k)
            {
                if (k == null) return;
                if (k.text != null) k.text.fontSizeMax = keyMax;
                if (k.value != null) k.value.fontSizeMax = counterMax;
            }
            if (Keys != null)
                foreach (Key k in Keys) Apply(k);
            Apply(Kps);
            Apply(Total);
        }

        /// <summary>
        /// Initialize foot keys starting at Keys[20] / 初始化从 Keys[20] 开始的脚键
        /// Supports 2-16 keys, automatically arranging in 1 or 2 rows / 支持 2-16 个键，自动排列为 1 或 2 排
        /// </summary>
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
                // Center the second row under the first when not full / 第二排不满时居中于第一排下方
                if (size > 8 && row == 1)
                    x += (8 - (size - 8)) * 17;
                int y = baseY - row * 34;
                Keys[i] = CreateKey(i, x, y, 30, -1, true, false);
            }
        }

        /// <summary>
        /// Create a single key GameObject with background, outline, text, count, and optional rain container / 创建单个按键 GameObject，包含背景、轮廓、文本、计数和可选的雨滴容器
        /// </summary>
        /// <param name="i">Key index (-1=KPS, -2=Total, 0-35=keys) / 按键索引（-1=KPS，-2=Total，0-35=按键）</param>
        /// <param name="x">X position in canvas reference coordinates / 画布参考坐标系中的 X 位置</param>
        /// <param name="y">Y position in canvas reference coordinates / 画布参考坐标系中的 Y 位置</param>
        /// <param name="sizeX">Key width / 按键宽度</param>
        /// <param name="raining">Rain row index (-1=no rain, 0=row1, 1=row2, 3=row3) / 雨滴行索引（-1=无雨滴，0=第1排，1=第2排，3=第3排）</param>
        /// <param name="slim">Use slim style (for KPS/Total display) / 使用窄样式（用于 KPS/Total 显示）</param>
        /// <param name="count">Show press count text / 显示按下计数文本</param>
        private Key CreateKey(int i, float x, float y, float sizeX, int raining, bool slim = false, bool count = true)
        {
            if (i >= 0 && i < 20 && Settings.HideMainKeyCount)
                count = false;
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
            GameObject gameObject;
            Image image;
            TextMeshProUGUI text;
            // Background
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
            // Outline
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
            // KeyText
            gameObject = new GameObject("KeyText");
            transform = gameObject.AddComponent<RectTransform>();
            transform.SetParent(obj.transform);
            if (slim)
            {
                transform.sizeDelta = new Vector2(sizeX / 2, 30);
                transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0, 0.5f);
                transform.anchoredPosition = new Vector2(count ? 10 : 7.5f, 0);
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
                    transform.anchoredPosition = new Vector2(0, 2);
                }
            }
            transform.localScale = Vector3.one;
            text = gameObject.AddComponent<TextMeshProUGUI>();
            ApplyKrpFont(text);
            text.fontStyle = (TMPro.FontStyles)settings.FontStyleFlags;
            text.enableAutoSizing = true;
            text.fontSizeMin = 0;
            text.fontSizeMax = 20f * settings.KeyFontSize;
            text.alignment = slim ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
            text.color = settings.Text;
            text.raycastTarget = false;
            key.text = text;
            // Press count text / 按键计数文本
            if (count)
            {
                gameObject = new GameObject("CountText");
                transform = gameObject.AddComponent<RectTransform>();
                transform.SetParent(obj.transform);
                if (slim)
                {
                    transform.sizeDelta = new Vector2(sizeX / 2, 30);
                    transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(1, 0.5f);
                    transform.anchoredPosition = new Vector2(-10, 0);
                }
                else
                {
                    transform.sizeDelta = new Vector2(sizeX - 4, 16);
                    transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 0);
                    transform.anchoredPosition = new Vector2(0, 2);
                }
                transform.localScale = Vector3.one;
                text = gameObject.AddComponent<TextMeshProUGUI>();
                ApplyKrpFont(text);
                text.fontStyle = (FontStyles)settings.FontStyleFlags;
                text.enableAutoSizing = true;
                text.fontSizeMin = 0;
                text.fontSizeMax = 20f * settings.CounterFontSize;
                text.raycastTarget = false;
                text.alignment = slim ? TextAlignmentOptions.Right : TextAlignmentOptions.Top;
                text.color = settings.Text;
                key.value = text;
            }
            // Set initial text content / 设置初始文本内容
            UpdateKeyText(key, i);
            // Rain effect container (one per key, shared across all rain drops) / 雨滴效果容器（每个按键一个，所有雨滴共享）
            if (raining >= 0)
            {
                if (key.rain == null)
                {
                    key.rain = new GameObject("RainLine");
                    transform = key.rain.AddComponent<RectTransform>();
                    transform.SetParent(obj.transform);
                    transform.sizeDelta = new Vector2(sizeX, 275);
                    transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
                    // Position rain container below the key / 将雨滴容器放在按键下方
                    transform.anchoredPosition = new Vector2(0, raining switch
                    {
                        0 => -223,  // Row 1 offset / 第1排偏移
                        3 => -115,  // Row 3 offset / 第3排偏移
                        _ => -169   // Row 2 offset / 第2排偏移
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
            int pi = i >= 0 && i < Keys.Length ? i : i == -1 ? 36 : i == -2 ? 37 : -1;
            if (Settings.EnablePerKeyColors)
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
            else if (pi >= 36)
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

        /// <summary>
        /// Set the display text for a key based on its index and current bindings / 根据按键索引和当前绑定设置显示文本
        /// Special indices: -1=KPS, -2=Total / 特殊索引：-1=KPS，-2=Total
        /// </summary>
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

        /// <summary>
        /// Lowest key bottom edge Y for normalized positioning / 归一化定位中最低按键底边的 Y 值
        /// </summary>
        private float GetMinMainKeyOffset()
        {
            float bottomY = GetLayout(Settings.KeyViewerStyle).bottomY;
            return Settings.DownLocation ? bottomY - 200 : bottomY;
        }

        /// <summary>Total width of the main key layout in reference pixels / 主按键布局的总宽度（参考像素）</summary>
        private float GetMainLayoutRightmostOffset() => 428f;

        /// <summary>Topmost key top edge Y for normalized positioning / 归一化定位中最顶部按键顶边的 Y 值</summary>
        private float GetMaxMainKeyOffset()
        {
            return GetLayout(Settings.KeyViewerStyle).frontY + 25;
        }

        /// <summary>Width of the foot key section in reference pixels / 脚键区域的宽度（参考像素）</summary>
        private float GetFootLayoutRightmostOffset(int size)
        {
            int row0Cols = Mathf.Min(size, 8);
            return (row0Cols - 1) * 34 + 30;
        }

        /// <summary>
        /// Reposition main keys based on normalized (0-1) custom position / 基于归一化（0-1）自定义位置重新定位主按键
        /// Maps (0,0) to screen top-left and (1,1) to screen bottom-right / (0,0) 映射到屏幕左上角，(1,1) 映射到屏幕右下角
        /// </summary>
        private void ResetKeyViewerPosition()
        {
            if (Keys == null || !Settings.CustomPositionEnabled) return;
            Vector2 norm = Settings.MainKeyViewerPosition;
            // Convert normalized (X: 0=left 1=right, Y: 0=top 1=bottom) to reference pixel offsets from bottom-left.
            // X: interpolate so X=0 = left edge at screen left, X=1 = right edge at screen right.
            // Y: subtract min layout offset so Y=1 puts the lowest key's bottom edge at screen bottom.
            float r = GetMainLayoutRightmostOffset();
            float baseX = norm.x * (CanvasWidth - r);
            int remove = Settings.DownLocation ? 200 : 0;
            // Y: lerp so Y=0 = top edge at screen top, Y=1 = bottom edge at screen bottom
            float topBaseY = 1080f - GetMaxMainKeyOffset() + remove;
            float bottomBaseY = -GetMinMainKeyOffset();
            float baseY = Mathf.Lerp(bottomBaseY, topBaseY, 1f - norm.y);
            RepositionMainKeys(GetLayout(Settings.KeyViewerStyle), baseX, baseY);
        }

        private void ResetFootKeyViewerPosition()
        {
            if (Keys == null || !Settings.CustomPositionEnabled) return;
            Vector2 norm = Settings.FootKeyViewerPosition;
            // Convert normalized (X: 0=left 1=right, Y: 0=top 1=bottom) to reference pixel offsets from bottom-left.
            // Foot keys are slim (h=30, half=15); offset so Y=1 aligns bottom edge with screen edge.
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
            // Y: lerp so Y=0 = top edge at screen top, Y=1 = bottom edge at screen bottom
            // Single row (size≤8): top edge at baseY+15. Two rows: top+34, top edge at baseY+49.
            float footTopOffset = size <= 8 ? 15f : 49f;
            float topBaseY = 1080f - footTopOffset;
            float bottomBaseY = 15f; // lowest key center at baseY, bottom edge = baseY-15 → baseY=15
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

        /// <summary>Get color setting by numeric index (0-8) for the color picker / 通过数字索引（0-8）获取颜色设置，用于颜色选择器</summary>
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

        /// <summary>Set color setting by numeric index (0-8) from the color picker / 通过数字索引（0-8）从颜色选择器设置颜色</summary>
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

        /// <summary>
        /// Apply current color settings to all key elements / 将当前颜色设置应用到所有按键元素
        /// </summary>
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
                if (Settings.EnablePerKeyColors)
                {
                    k.background.color = Settings.PerKeyBackground[pi];
                    k.outline.color = Settings.PerKeyOutline[pi];
                    k.text.color = Settings.PerKeyText[pi];
                    if (k.value != null) k.value.color = Settings.PerKeyText[pi];
                }
                else if (pi == 36) // KPS
                {
                    k.background.color = Settings.KpsBackground;
                    k.outline.color = Settings.KpsOutline;
                    k.text.color = Settings.KpsText;
                    if (k.value != null) k.value.color = Settings.KpsText;
                }
                else if (pi == 37) // Total
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

        /// <summary>
        /// Handle main key layout change / 处理主按键布局变化
        /// </summary>
        private void ChangeKeyViewer()
        {
            ResetKeyViewer();
        }

        /// <summary>
        /// Destroy and recreate main keys (for layout/style changes) / 销毁并重建主按键（用于布局/样式变化）
        /// </summary>
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

        /// <summary>
        /// Destroy and recreate foot keys (for layout/style changes) / 销毁并重建脚键（用于布局/样式变化）
        /// </summary>
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

        /// <summary>
        /// Get the key code array for the current main layout / 获取当前主布局的按键代码数组
        /// </summary>
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

        /// <summary>
        /// Get the foot key code array for the current foot layout / 获取当前脚键布局的按键代码数组
        /// </summary>
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

        /// <summary>
        /// Mirror the active main+foot key set into KRP's KeyLimiter allowed list, so keys are
        /// configured once and KeyLimiter follows the keyviewer. No-op unless SyncToKeyLimiter is on.
        /// </summary>
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
        }

        /// <summary>
        /// Active main+foot key codes (deduped, zero-filtered) for the current JKV layout.
        /// Source of truth for KeyLimiter sync in simple mode. / 当前 JKV 布局的有效主+脚键码。
        /// </summary>
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
                    int c = (int)arr[i];
                    if (c == 0) continue;
                    if (seen.Add(c)) result.Add(c);
                }
            }
            Collect(GetKeyCode());
            Collect(GetFootKeyCode());
            return result.ToArray();
        }

        /// <summary>
        /// Get the ghost key code array for the current main layout / 获取当前主布局的鬼键代码数组
        /// </summary>
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

        /// <summary>
        /// Get the custom text labels for the current main layout / 获取当前主布局的自定义文本标签
        /// </summary>
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

        /// <summary>
        /// Get the custom text labels for the current foot key layout / 获取当前脚键布局的自定义文本标签
        /// </summary>
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

        /// <summary>
        /// Get the back-row index mapping for the current main layout / 获取当前主布局的后排索引映射
        /// </summary>
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

        /// <summary>Format count with thousands separator if enabled / 千分位格式化数字</summary>
        private static string FormatCount(int count)
        {
            return Settings.EnableCountFormatting ? count.ToString("N0") : count.ToString();
        }

        /// <summary>Refresh all key value displays (count or per-key KPS) / 刷新所有按键数值显示（计数或每键 KPS）</summary>
        public void RefreshAllCountDisplay()
        {
            if (Keys == null) return;
            for (int i = 0; i < Keys.Length; i++)
            {
                if (Keys[i] != null && Keys[i].value != null)
                {
                    if (Settings.EnablePerKeyKps)
                        Keys[i].value.text = (keyPressTimes != null && i < keyPressTimes.Length && keyPressTimes[i] != null) ? keyPressTimes[i].Count.ToString() : "0";
                    else
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
                // manual HSV to RGB
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
