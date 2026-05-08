using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KorenResourcePack
{
    /// <summary>
    /// "Simple" KeyViewer mode — hardcoded Key10/12/16/20 layouts with sliced sprite
    /// backgrounds, KPS/Total counters, and rain particle effect. The advanced/JSON-preset
    /// path remains available via KeyViewerMode = "dmnote".
    ///
    /// Cross-platform input via UnityEngine.Input.GetKey (no separate input thread).
    /// Foot-keyviewer slots (20-35) are reserved but not built yet.
    ///
    /// Lifecycle:
    ///   EnsureSimpleBuilt() - constructs canvas + key GOs once, rebuilds when style changes.
    ///   DrawSimpleKeyViewer()- polls input, animates rain, updates counters every frame.
    ///   HideSimpleKeyViewer()- SetActive(false) without destroying.
    ///   DestroySimpleKeyViewer()- tear down GOs (mod unload / mode switch).
    /// </summary>
    public static partial class Main
    {
        // Backslot indices used by the back row. Renames the underlying slot ordering so the
        // visible left-to-right key order matches a typing layout while the count array stays
        // consistent across all four styles.
        private static readonly byte[] SkvBackSeq10 = { 8, 9 };
        private static readonly byte[] SkvBackSeq12 = { 9, 8, 10, 11 };
        private static readonly byte[] SkvBackSeq16 = { 12, 13, 9, 8, 10, 11, 14, 15 };
        private static readonly byte[] SkvBackSeq20 = { 12, 13, 9, 8, 10, 11, 14, 15, 17, 16, 18, 19 };

        private class SkvKey
        {
            public int slot;            // index into KeyViewerSimpleCount, or -1 (Kps) / -2 (Total)
            public int color;           // rain color group: 1 = top row, 2 = bottom row, 3 = extra row
            public KeyCode keyCode;
            public bool pressed;
            public Image background;
            public Image outline;
            public TextMeshProUGUI label;
            public TextMeshProUGUI count;
            public RectTransform rainParent;
            public List<SkvRain> rains = new List<SkvRain>(8);
        }

        private class SkvRain
        {
            public float startTime;
            public RectTransform rect;
            public Image image;
            public bool active;
            public int color;
            public float xSize;
            public Vector2 finalSize;
        }

        private static GameObject skvRoot;
        private static GameObject skvScale;
        private static Canvas skvCanvas;
        private static List<SkvKey> skvKeys;
        private static SkvKey skvKps;
        private static SkvKey skvTotal;
        private static int skvLastKpsCount;
        private static readonly Queue<float> skvPressTimes = new Queue<float>(32);
        private static int skvBuiltStyle = -1;
        private static bool skvBuiltDownLoc;
        private static float skvBuiltSize = -1f;

        // -------------------------------- Public entry ---------------------------------
        internal static bool SimpleKeyViewerEnabled
        {
            get { return string.Equals(settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase); }
        }

        internal static void DrawSimpleKeyViewer()
        {
            EnsureBundleLoaded();
            EnsureSimpleBuilt();
            if (skvRoot == null) return;
            skvRoot.SetActive(true);

            float now = Time.unscaledTime;
            float speed = Mathf.Max(1f, settings.KeyViewerSimpleRainSpeed);
            float trackHeight = Mathf.Max(1f, settings.KeyViewerSimpleRainHeight);

            foreach (SkvKey k in skvKeys)
            {
                if (k == null) continue;
                bool down = k.keyCode != KeyCode.None && KvIsKeyPressed(k.keyCode);
                if (down != k.pressed)
                {
                    k.pressed = down;
                    if (down)
                    {
                        // Slot 9 in Key10 maps to Comma but we store its count at index 10
                        // so Key12+ can keep slot 9 reserved for the lower row's first key.
                        int slot = k.slot;
                        if (slot == 9 && settings.KeyViewerSimpleStyle == 0) slot = 10;
                        if (slot >= 0 && slot < settings.KeyViewerSimpleCount.Length)
                            settings.KeyViewerSimpleCount[slot]++;
                        settings.KeyViewerSimpleTotalCount++;
                        skvPressTimes.Enqueue(now);

                        if (settings.KeyViewerSimpleUseRain && k.rainParent != null)
                            SpawnSkvRain(k, now);
                    }
                    UpdateSkvKeyColors(k);
                    UpdateSkvCountText(k);
                }

                AnimateSkvRains(k, now, speed, trackHeight);
            }

            // KPS recalc (1-second sliding window).
            while (skvPressTimes.Count > 0 && now - skvPressTimes.Peek() > 1f)
                skvPressTimes.Dequeue();
            int kps = skvPressTimes.Count;
            if (kps != skvLastKpsCount)
            {
                skvLastKpsCount = kps;
                if (skvKps != null && skvKps.count != null) skvKps.count.text = kps.ToString();
            }
            if (skvTotal != null && skvTotal.count != null)
                skvTotal.count.text = settings.KeyViewerSimpleTotalCount.ToString();
        }

        internal static void HideSimpleKeyViewer()
        {
            if (skvRoot != null) skvRoot.SetActive(false);
        }

        internal static void DestroySimpleKeyViewer()
        {
            try
            {
                if (skvRoot != null) UnityEngine.Object.Destroy(skvRoot);
            }
            catch { }
            skvRoot = null;
            skvScale = null;
            skvCanvas = null;
            skvKeys = null;
            skvKps = null;
            skvTotal = null;
            skvLastKpsCount = 0;
            skvPressTimes.Clear();
            skvBuiltStyle = -1;
            skvBuiltDownLoc = false;
            skvBuiltSize = -1f;
        }

        // -------------------------------- Build ----------------------------------------
        private static void EnsureSimpleBuilt()
        {
            int style = Mathf.Clamp(settings.KeyViewerSimpleStyle, 0, 3);
            bool downLoc = settings.KeyViewerSimpleDownLocation;
            float size = Mathf.Max(0.2f, settings.KeyViewerSimpleSize);

            if (skvRoot != null
                && skvBuiltStyle == style
                && skvBuiltDownLoc == downLoc
                && Mathf.Abs(skvBuiltSize - size) < 0.001f)
                return;

            DestroySimpleKeyViewer();

            skvRoot = new GameObject("KorenResourcePack.SimpleKeyViewer");
            UnityEngine.Object.DontDestroyOnLoad(skvRoot);

            skvCanvas = skvRoot.AddComponent<Canvas>();
            skvCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            skvCanvas.sortingOrder = 32700;

            CanvasScaler scaler = skvRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            skvRoot.AddComponent<GraphicRaycaster>().enabled = false;

            skvScale = new GameObject("Scale", typeof(RectTransform));
            skvScale.transform.SetParent(skvRoot.transform, false);
            RectTransform sr = skvScale.GetComponent<RectTransform>();
            sr.anchorMin = sr.anchorMax = sr.offsetMin = sr.offsetMax = Vector2.zero;
            sr.localScale = new Vector3(size, size, 1f);

            skvKeys = new List<SkvKey>(36);
            for (int i = 0; i < 36; i++) skvKeys.Add(null);

            switch (style)
            {
                case 0: BuildSkvLayout10(downLoc); break;
                case 1: BuildSkvLayout12(downLoc); break;
                case 2: BuildSkvLayout16(downLoc); break;
                case 3: BuildSkvLayout20(downLoc); break;
            }

            skvBuiltStyle = style;
            skvBuiltDownLoc = downLoc;
            skvBuiltSize = size;
        }

        // Layout positions in 1920x1080 design pixels.
        private static void BuildSkvLayout10(bool downLoc)
        {
            int rem = downLoc ? 200 : 0;
            for (int i = 0; i < 8; i++)
                skvKeys[i] = CreateSkvKey(i, 54f * i, 279f - rem, 50f, 0);
            skvKeys[8] = CreateSkvKey(8, 81f, 225f - rem, 131f, 1);
            skvKeys[8].rainParent = skvKeys[3] != null ? skvKeys[3].rainParent : null;
            skvKeys[9] = CreateSkvKey(9, 54f * 4f, 225f - rem, 131f, 1);
            skvKeys[9].rainParent = skvKeys[4] != null ? skvKeys[4].rainParent : null;
            skvKps = CreateSkvKey(-1, 0f, 225f - rem, 77f, -1);
            skvTotal = CreateSkvKey(-2, 81f + 54f * 5f, 225f - rem, 77f, -1);
        }

        private static void BuildSkvLayout12(bool downLoc)
        {
            int rem = downLoc ? 200 : 0;
            for (int i = 0; i < 8; i++)
                skvKeys[i] = CreateSkvKey(i, 54f * i, 279f - rem, 50f, 0);
            skvKeys[8] = CreateSkvKey(8, 81f + 54f, 225f - rem, 77f, 1);
            skvKeys[9] = CreateSkvKey(9, 81f, 225f - rem, 50f, 1);
            skvKeys[10] = CreateSkvKey(10, 54f * 4f, 225f - rem, 77f, 1);
            skvKeys[11] = CreateSkvKey(11, 54f * 4f + 81f, 225f - rem, 50f, 1);
            for (int i = 0; i < 4; i++)
            {
                int j = SkvBackSeq12[i];
                if (skvKeys[j] != null && skvKeys[i + 2] != null)
                    skvKeys[j].rainParent = skvKeys[i + 2].rainParent;
            }
            skvKps = CreateSkvKey(-1, 0f, 225f - rem, 77f, -1);
            skvTotal = CreateSkvKey(-2, 81f + 54f * 5f, 225f - rem, 77f, -1);
        }

        private static void BuildSkvLayout16(bool downLoc)
        {
            int rem = downLoc ? 200 : 0;
            for (int i = 0; i < 8; i++)
                skvKeys[i] = CreateSkvKey(i, 54f * i, 320f - rem, 50f, 0);
            for (int i = 0; i < 8; i++)
            {
                int j = SkvBackSeq16[i];
                skvKeys[j] = CreateSkvKey(j, 54f * i, 266f - rem, 50f, 1);
                if (skvKeys[i] != null) skvKeys[j].rainParent = skvKeys[i].rainParent;
            }
            skvKps = CreateSkvKey(-1, 0f, 220f - rem, 212f, -1, slim: true);
            skvTotal = CreateSkvKey(-2, 216f, 220f - rem, 212f, -1, slim: true);
        }

        private static void BuildSkvLayout20(bool downLoc)
        {
            int rem = downLoc ? 200 : 0;
            for (int i = 0; i < 8; i++)
                skvKeys[i] = CreateSkvKey(i, 54f * i, 333f - rem, 50f, 0);
            for (int i = 0; i < 8; i++)
            {
                int j = SkvBackSeq20[i];
                skvKeys[j] = CreateSkvKey(j, 54f * i, 279f - rem, 50f, 1);
                if (skvKeys[i] != null) skvKeys[j].rainParent = skvKeys[i].rainParent;
            }
            skvKeys[16] = CreateSkvKey(16, 81f + 54f, 225f - rem, 77f, 3);
            skvKeys[17] = CreateSkvKey(17, 81f, 225f - rem, 50f, 3);
            skvKeys[18] = CreateSkvKey(18, 54f * 4f, 225f - rem, 77f, 3);
            skvKeys[19] = CreateSkvKey(19, 54f * 4f + 81f, 225f - rem, 50f, 3);
            skvKps = CreateSkvKey(-1, 0f, 225f - rem, 77f, -1);
            skvTotal = CreateSkvKey(-2, 81f + 54f * 5f, 225f - rem, 77f, -1);
        }

        // -------------------------------- CreateSkvKey ---------------------------------
        private static SkvKey CreateSkvKey(int slot, float x, float y, float sizeX, int rainColor, bool slim = false, bool count = true)
        {
            float sizeY = slim ? 30f : 50f;

            GameObject go = new GameObject(slot >= 0 ? ("Key" + slot) : (slot == -1 ? "KPS" : "Total"));
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(skvScale.transform, false);
            rt.sizeDelta = new Vector2(sizeX, sizeY);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.localScale = Vector3.one;

            SkvKey k = new SkvKey();
            k.slot = slot;
            k.color = rainColor;
            k.keyCode = ResolveSkvKeyCodeForSlot(slot);

            // Background image. The 2x sizeDelta + 0.5x localScale trick keeps the 9-slice
            // border crisp on small keys: native border is ~11px, but rendering at 2x then
            // scaling to 50% visually compresses it to ~5.5 effective screen pixels.
            GameObject bgGo = new GameObject("Background", typeof(RectTransform));
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.SetParent(rt, false);
            bgRt.anchorMin = bgRt.anchorMax = bgRt.pivot = Vector2.zero;
            bgRt.anchoredPosition = Vector2.zero;
            bgRt.sizeDelta = new Vector2(sizeX * 2f, sizeY * 2f);
            bgRt.localScale = new Vector3(0.5f, 0.5f, 1f);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = bundleKeyBackground;
            bgImg.type = Image.Type.Sliced;
            bgImg.preserveAspect = false;
            bgImg.raycastTarget = false;
            bgImg.color = SkvBg();
            k.background = bgImg;

            // Outline — drawn after BG so it renders on top in sibling order.
            GameObject outGo = new GameObject("Outline", typeof(RectTransform));
            RectTransform outRt = outGo.GetComponent<RectTransform>();
            outRt.SetParent(rt, false);
            outRt.anchorMin = outRt.anchorMax = outRt.pivot = Vector2.zero;
            outRt.anchoredPosition = Vector2.zero;
            outRt.sizeDelta = new Vector2(sizeX * 2f, sizeY * 2f);
            outRt.localScale = new Vector3(0.5f, 0.5f, 1f);
            Image outImg = outGo.AddComponent<Image>();
            outImg.sprite = bundleKeyOutline;
            outImg.type = Image.Type.Sliced;
            outImg.preserveAspect = false;
            outImg.raycastTarget = false;
            outImg.color = SkvOut();
            k.outline = outImg;

            // Top label.
            GameObject lblGo = new GameObject("KeyText", typeof(RectTransform));
            RectTransform lblRt = lblGo.GetComponent<RectTransform>();
            lblRt.SetParent(rt, false);
            if (slim)
            {
                lblRt.sizeDelta = new Vector2(sizeX / 2f, 30f);
                lblRt.anchorMin = lblRt.anchorMax = lblRt.pivot = new Vector2(0f, 0.5f);
                lblRt.anchoredPosition = new Vector2(count ? 10f : 7.5f, 0f);
            }
            else
            {
                lblRt.sizeDelta = new Vector2(sizeX - 4f, 32f);
                lblRt.anchorMin = lblRt.anchorMax = lblRt.pivot = new Vector2(0.5f, 1f);
                lblRt.anchoredPosition = new Vector2(0f, 2f);
            }
            lblRt.localScale = Vector3.one;
            TextMeshProUGUI lblTmp = lblGo.AddComponent<TextMeshProUGUI>();
            lblTmp.font = kvActiveFont != null ? kvActiveFont : bundleDefaultFont;
            lblTmp.enableAutoSizing = true;
            lblTmp.fontSizeMin = 0;
            lblTmp.fontSizeMax = 20;
            lblTmp.alignment = (slim && count) ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
            lblTmp.color = SkvTxt();
            lblTmp.raycastTarget = false;
            k.label = lblTmp;

            if (count)
            {
                GameObject ctGo = new GameObject("CountText", typeof(RectTransform));
                RectTransform ctRt = ctGo.GetComponent<RectTransform>();
                ctRt.SetParent(rt, false);
                if (slim)
                {
                    ctRt.sizeDelta = new Vector2(sizeX / 2f, 30f);
                    ctRt.anchorMin = ctRt.anchorMax = ctRt.pivot = new Vector2(1f, 0.5f);
                    ctRt.anchoredPosition = new Vector2(-10f, 0f);
                }
                else
                {
                    ctRt.sizeDelta = new Vector2(sizeX - 4f, 16f);
                    ctRt.anchorMin = ctRt.anchorMax = ctRt.pivot = new Vector2(0.5f, 0f);
                    ctRt.anchoredPosition = new Vector2(0f, 2f);
                }
                ctRt.localScale = Vector3.one;
                TextMeshProUGUI ctTmp = ctGo.AddComponent<TextMeshProUGUI>();
                ctTmp.font = kvActiveFont != null ? kvActiveFont : bundleDefaultFont;
                ctTmp.enableAutoSizing = true;
                ctTmp.fontSizeMin = 0;
                ctTmp.fontSizeMax = 20;
                ctTmp.alignment = slim ? TextAlignmentOptions.Right : TextAlignmentOptions.Top;
                ctTmp.color = SkvTxt();
                ctTmp.raycastTarget = false;
                k.count = ctTmp;
            }

            UpdateSkvLabelText(k);
            UpdateSkvCountText(k);

            // Rain track for visible rain columns (top row, bottom row, extra row).
            if (rainColor == 0 || rainColor == 2 || rainColor == 3)
            {
                GameObject rainGo = new GameObject("RainLine", typeof(RectTransform));
                RectTransform rainRt = rainGo.GetComponent<RectTransform>();
                rainRt.SetParent(rt, false);
                rainRt.sizeDelta = new Vector2(sizeX, 275f);
                rainRt.anchorMin = rainRt.anchorMax = rainRt.pivot = Vector2.zero;
                rainRt.localScale = Vector3.one;
                rainRt.anchoredPosition = new Vector2(0f, rainColor == 0 ? -223f : (rainColor == 3 ? -115f : -169f));
                k.rainParent = rainRt;
            }

            return k;
        }

        // -------------------------------- Helpers --------------------------------------
        private static Color SkvBg() { return new Color(settings.SKvBgR, settings.SKvBgG, settings.SKvBgB, settings.SKvBgA); }
        private static Color SkvBgClicked() { return new Color(settings.SKvBgcR, settings.SKvBgcG, settings.SKvBgcB, settings.SKvBgcA); }
        private static Color SkvOut() { return new Color(settings.SKvOutR, settings.SKvOutG, settings.SKvOutB, settings.SKvOutA); }
        private static Color SkvOutClicked() { return new Color(settings.SKvOutcR, settings.SKvOutcG, settings.SKvOutcB, settings.SKvOutcA); }
        private static Color SkvTxt() { return new Color(settings.SKvTxtR, settings.SKvTxtG, settings.SKvTxtB, settings.SKvTxtA); }
        private static Color SkvTxtClicked() { return new Color(settings.SKvTxtcR, settings.SKvTxtcG, settings.SKvTxtcB, settings.SKvTxtcA); }

        private static Color SkvRainColorFor(int color)
        {
            switch (color)
            {
                case 1: return new Color(settings.SKvRainR, settings.SKvRainG, settings.SKvRainB, settings.SKvRainA);
                case 3: return new Color(settings.SKvRain3R, settings.SKvRain3G, settings.SKvRain3B, settings.SKvRain3A);
                default: return new Color(settings.SKvRain2R, settings.SKvRain2G, settings.SKvRain2B, settings.SKvRain2A);
            }
        }

        private static void UpdateSkvKeyColors(SkvKey k)
        {
            if (k.background != null) k.background.color = k.pressed ? SkvBgClicked() : SkvBg();
            if (k.outline != null) k.outline.color = k.pressed ? SkvOutClicked() : SkvOut();
            Color t = k.pressed ? SkvTxtClicked() : SkvTxt();
            if (k.label != null) k.label.color = t;
            if (k.count != null) k.count.color = t;
        }

        private static void UpdateSkvLabelText(SkvKey k)
        {
            if (k.label == null) return;
            if (k.slot == -1) { k.label.text = "KPS"; return; }
            if (k.slot == -2) { k.label.text = "Total"; return; }
            k.label.text = SkvKeyToString(k.keyCode);
        }

        private static void UpdateSkvCountText(SkvKey k)
        {
            if (k.count == null) return;
            if (k.slot == -1) { k.count.text = skvLastKpsCount.ToString(); return; }
            if (k.slot == -2) { k.count.text = settings.KeyViewerSimpleTotalCount.ToString(); return; }
            int slot = k.slot;
            if (slot == 9 && settings.KeyViewerSimpleStyle == 0) slot = 10;
            if (slot >= 0 && slot < settings.KeyViewerSimpleCount.Length)
                k.count.text = settings.KeyViewerSimpleCount[slot].ToString();
        }

        private static KeyCode ResolveSkvKeyCodeForSlot(int slot)
        {
            if (slot < 0) return KeyCode.None;
            int[] arr;
            switch (settings.KeyViewerSimpleStyle)
            {
                case 0: arr = settings.KeyViewerSimpleKey10; break;
                case 1: arr = settings.KeyViewerSimpleKey12; break;
                case 2: arr = settings.KeyViewerSimpleKey16; break;
                case 3: arr = settings.KeyViewerSimpleKey20; break;
                default: arr = settings.KeyViewerSimpleKey12; break;
            }
            if (slot >= 0 && slot < arr.Length) return (KeyCode)arr[slot];
            return KeyCode.None;
        }

        // Trim KeyCode names to short labels suitable for a single-key tile.
        private static string SkvKeyToString(KeyCode kc)
        {
            string s = kc.ToString();
            if (s.StartsWith("Alpha")) s = s.Substring(5);
            if (s.StartsWith("Keypad")) s = "N" + s.Substring(6);
            if (s.StartsWith("Left")) s = "L" + s.Substring(4);
            if (s.StartsWith("Right")) s = "R" + s.Substring(5);
            if (s.EndsWith("Shift")) s = s.Substring(0, s.Length - 5) + "⇧";
            if (s.EndsWith("Control")) s = s.Substring(0, s.Length - 7) + "Ctrl";
            switch (s)
            {
                case "Plus": return "+";
                case "Minus": return "-";
                case "Multiply": return "*";
                case "Divide": return "/";
                case "Enter": return "↵";
                case "Return": return "↵";
                case "Equals": return "=";
                case "Period": return ".";
                case "Comma": return ",";
                case "Tab": return "⇥";
                case "Space": return "␣";
                case "Backslash": return "\\";
                case "Slash": return "/";
                case "Semicolon": return ";";
                case "Quote": return "'";
                case "BackQuote": return "`";
                case "CapsLock": return "⇪";
                case "Backspace": return "Back";
                case "UpArrow": return "↑";
                case "DownArrow": return "↓";
                case "LeftArrow": return "←";
                case "RightArrow": return "→";
                case "LBracket": return "[";
                case "RBracket": return "]";
                case "LeftBracket": return "[";
                case "RightBracket": return "]";
                case "None": return "";
                default: return s;
            }
        }

        // -------------------------------- Rain -----------------------------------------
        private static void SpawnSkvRain(SkvKey k, float startTime)
        {
            SkvRain rain = new SkvRain();
            rain.startTime = startTime;
            rain.color = k.color;
            switch (k.color)
            {
                case 1: rain.xSize = 50f; break;
                case 3: rain.xSize = 30f; break;
                default: rain.xSize = 40f; break;
            }
            GameObject go = new GameObject("Rain", typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(k.rainParent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
            Image img = go.AddComponent<Image>();
            img.color = SkvRainColorFor(k.color);
            img.raycastTarget = false;
            // Match the reference: plain Image, no sprite. UI Image without a sprite renders
            // as a flat rect using only the color. Sliced sprite would distort the rain shape.
            rain.rect = rt;
            rain.image = img;
            rain.active = true;
            k.rains.Add(rain);
        }

        private static void AnimateSkvRains(SkvKey k, float now, float speed, float trackHeight)
        {
            if (k.rains.Count == 0) return;
            for (int i = k.rains.Count - 1; i >= 0; i--)
            {
                SkvRain r = k.rains[i];
                if (!r.active || r.rect == null) { k.rains.RemoveAt(i); continue; }

                float y = (now - r.startTime) * (speed * 100f / 300f);
                if (k.pressed && i == k.rains.Count - 1)
                    r.finalSize = new Vector2(r.xSize, y);

                if (y > trackHeight)
                {
                    float sizeY = r.finalSize.y - y + trackHeight;
                    if (sizeY < 0f)
                    {
                        UnityEngine.Object.Destroy(r.rect.gameObject);
                        r.active = false;
                        k.rains.RemoveAt(i);
                        continue;
                    }
                    r.rect.sizeDelta = new Vector2(r.finalSize.x, sizeY);
                    r.rect.anchoredPosition = new Vector2(0f, -trackHeight);
                }
                else
                {
                    if (k.pressed && i == k.rains.Count - 1)
                        r.rect.sizeDelta = r.finalSize;
                    else if (r.rect.sizeDelta.y == 0f)
                        r.rect.sizeDelta = new Vector2(r.xSize, y);
                    r.rect.anchoredPosition = new Vector2(0f, -y);
                }
            }
        }
    }
}
