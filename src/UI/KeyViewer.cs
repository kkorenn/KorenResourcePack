using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KorenResourcePack
{
    internal static partial class KeyViewer
    {
        
        private const int KvImageSortingOrder = 32701;
        private const int KvTextSortingOrder = 32702;
        private const float KvMaxCornerRadiusPx = 8f;

        private static GameObject kvImageRoot;
        private static Canvas kvImageCanvas;
        private static RectTransform kvNotesLayer;
        private static KvRainManager kvRainManager;
        private static RectTransform kvKeysLayer;
        private static bool kvImageBuilt;

        private static GameObject kvTextRoot;
        private static Canvas kvTextCanvas;
        private static bool kvTextBuilt;
        private static TMP_FontAsset kvActiveFont;
        private static string kvActiveFontName;
        private static readonly Color KvShadowColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color KvDropShadowColor = new Color(0f, 0f, 0f, 0.40f);
        private const float KvShadowDilate = 0.0f;
        private const float KvShadowSoftness = 0.22f;
        private const float KvShadowOffsetX = 0.45f;
        private const float KvShadowOffsetY = -0.45f;
        private const float KvShadowReferenceSize = 24f;

        internal class KvUiRect
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            
            public KvRoundedImage rounded;
            public Image image;
        }

        internal class KvRoundedImage : MaskableGraphic
        {
            private readonly List<Vector2> outer = new List<Vector2>(64);
            private readonly List<Vector2> inner = new List<Vector2>(64);
            private float cornerRadius;
            private bool verticalGradient;
            private bool reverseGradient;
            private float ringThickness;
            private bool noEdgeAA;
            
            private float topVertexAlpha = 1f;
            private float botVertexAlpha = 1f;

            public void SetShape(float radius, bool gradient, bool reverse, float borderThickness, bool noAA = false)
            {
                radius = Mathf.Max(0f, radius);
                borderThickness = Mathf.Max(0f, borderThickness);
                if (Mathf.Abs(cornerRadius - radius) < 0.01f &&
                    verticalGradient == gradient &&
                    reverseGradient == reverse &&
                    Mathf.Abs(ringThickness - borderThickness) < 0.01f &&
                    noEdgeAA == noAA)
                    return;

                cornerRadius = radius;
                verticalGradient = gradient;
                reverseGradient = reverse;
                ringThickness = borderThickness;
                noEdgeAA = noAA;
                SetVerticesDirty();
            }

            public void SetVertexAlpha(float top, float bot)
            {
                if (Mathf.Abs(topVertexAlpha - top) < 0.001f && Mathf.Abs(botVertexAlpha - bot) < 0.001f)
                    return;
                topVertexAlpha = top;
                botVertexAlpha = bot;
                SetVerticesDirty();
            }

            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();

                Rect rect = GetPixelAdjustedRect();
                if (rect.width <= 0f || rect.height <= 0f)
                    return;

                float radius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(rect.width, rect.height) * 0.25f);
                if (ringThickness > 0.01f)
                {
                    PopulateRing(vh, rect, radius, ringThickness);
                    return;
                }

                if (radius <= 0.01f && noEdgeAA)
                {
                    Color cTop = color;
                    Color cBot = color;
                    if (verticalGradient)
                    {
                        
                        cTop.a *= reverseGradient ? 1f : 0f;
                        cBot.a *= reverseGradient ? 0f : 1f;
                    }
                    if (topVertexAlpha < 0.999f || botVertexAlpha < 0.999f)
                    {
                        cTop.a *= topVertexAlpha;
                        cBot.a *= botVertexAlpha;
                    }
                    Color32 cTop32 = cTop;
                    Color32 cBot32 = cBot;

                    int v0 = vh.currentVertCount;
                    vh.AddVert(new Vector3(rect.xMin, rect.yMin), cBot32, Vector2.zero);
                    vh.AddVert(new Vector3(rect.xMax, rect.yMin), cBot32, Vector2.zero);
                    vh.AddVert(new Vector3(rect.xMax, rect.yMax), cTop32, Vector2.zero);
                    vh.AddVert(new Vector3(rect.xMin, rect.yMax), cTop32, Vector2.zero);
                    vh.AddTriangle(v0, v0 + 1, v0 + 2);
                    vh.AddTriangle(v0, v0 + 2, v0 + 3);
                    return;
                }

                float aa = noEdgeAA ? 0f : Mathf.Min(1.25f, rect.width * 0.25f, rect.height * 0.25f);
                Rect innerRect = new Rect(rect.xMin + aa, rect.yMin + aa, Mathf.Max(0f, rect.width - aa * 2f), Mathf.Max(0f, rect.height - aa * 2f));
                float innerRadius = Mathf.Max(0f, radius - aa);
                int segments = Mathf.Clamp(Mathf.CeilToInt(radius * 0.9f), 4, 12);

                outer.Clear();
                inner.Clear();
                AddRoundedRectPoints(outer, rect, radius, segments);
                AddRoundedRectPoints(inner, innerRect, innerRadius, segments);
                if (inner.Count < 3 || outer.Count != inner.Count)
                    return;

                int centerIndex = vh.currentVertCount;
                Vector2 center = innerRect.center;
                vh.AddVert(center, VertexColor(center, rect, 1f), Vector2.zero);

                int innerStart = vh.currentVertCount;
                for (int i = 0; i < inner.Count; i++)
                    vh.AddVert(inner[i], VertexColor(inner[i], rect, 1f), Vector2.zero);

                int outerStart = vh.currentVertCount;
                for (int i = 0; i < outer.Count; i++)
                    vh.AddVert(outer[i], VertexColor(outer[i], rect, 0f), Vector2.zero);

                for (int i = 0; i < inner.Count; i++)
                {
                    int next = (i + 1) % inner.Count;
                    vh.AddTriangle(centerIndex, innerStart + i, innerStart + next);
                    vh.AddTriangle(innerStart + i, outerStart + i, outerStart + next);
                    vh.AddTriangle(innerStart + i, outerStart + next, innerStart + next);
                }
            }

            private void PopulateRing(VertexHelper vh, Rect rect, float radius, float thickness)
            {
                float maxThickness = Mathf.Min(rect.width, rect.height) * 0.45f;
                float t = Mathf.Clamp(thickness, 0f, maxThickness);
                if (t <= 0.01f) return;

                Rect innerRect = new Rect(rect.xMin + t, rect.yMin + t, Mathf.Max(0f, rect.width - t * 2f), Mathf.Max(0f, rect.height - t * 2f));
                if (innerRect.width <= 0f || innerRect.height <= 0f) return;

                int segments = Mathf.Clamp(Mathf.CeilToInt(radius * 0.9f), 4, 12);
                outer.Clear();
                inner.Clear();
                AddRoundedRectPoints(outer, rect, radius, segments);
                AddRoundedRectPoints(inner, innerRect, Mathf.Max(0f, radius - t), segments);
                if (outer.Count != inner.Count) return;

                Color32 c = color;
                int outerStart = vh.currentVertCount;
                for (int i = 0; i < outer.Count; i++)
                    vh.AddVert(outer[i], c, Vector2.zero);

                int innerStart = vh.currentVertCount;
                for (int i = 0; i < inner.Count; i++)
                    vh.AddVert(inner[i], c, Vector2.zero);

                for (int i = 0; i < outer.Count; i++)
                {
                    int next = (i + 1) % outer.Count;
                    vh.AddTriangle(outerStart + i, outerStart + next, innerStart + next);
                    vh.AddTriangle(outerStart + i, innerStart + next, innerStart + i);
                }
            }

            private Color32 VertexColor(Vector2 p, Rect rect, float edgeAlpha)
            {
                Color c = color;
                if (verticalGradient)
                {
                    float t = Mathf.InverseLerp(rect.yMin, rect.yMax, p.y);
                    c.a *= reverseGradient ? t : (1f - t);
                }
                if (topVertexAlpha < 0.999f || botVertexAlpha < 0.999f)
                {
                    float t = Mathf.InverseLerp(rect.yMin, rect.yMax, p.y);
                    c.a *= Mathf.Lerp(botVertexAlpha, topVertexAlpha, t);
                }
                c.a *= edgeAlpha;
                return c;
            }

            private static void AddRoundedRectPoints(List<Vector2> points, Rect rect, float radius, int segments)
            {
                points.Clear();
                if (radius <= 0.01f)
                {
                    points.Add(new Vector2(rect.xMax, rect.yMin));
                    points.Add(new Vector2(rect.xMax, rect.yMax));
                    points.Add(new Vector2(rect.xMin, rect.yMax));
                    points.Add(new Vector2(rect.xMin, rect.yMin));
                    return;
                }

                AddArc(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, segments);
                AddArc(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, segments);
                AddArc(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, segments);
                AddArc(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f, segments);
            }

            private static void AddArc(List<Vector2> points, Vector2 center, float radius, float fromDeg, float toDeg, int segments)
            {
                for (int i = 0; i <= segments; i++)
                {
                    if (points.Count > 0 && i == 0) continue;
                    float a = Mathf.Lerp(fromDeg, toDeg, i / (float)segments) * Mathf.Deg2Rad;
                    points.Add(new Vector2(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius));
                }
            }
        }

        internal class KvRawRain
        {
            public KvKey key;
            public bool isGhost;
            public float startTime;
            public float endTime = -1f;
            public float x;
            public float width;
            public float baseY;
            public float trackHeight;
            public float speed;
            public bool reverse;
            public Color color;
        }

        internal class KvRainGraphic : MaskableGraphic
        {
            private float dNear;
            private float dFar;
            private float trackHeight;
            private float fadePx;
            private bool reverseFade;

            public void SetFadeParams(float dNear, float dFar, float trackHeight, float fadePx, bool reverse)
            {
                bool changed =
                    this.dNear != dNear ||
                    this.dFar != dFar ||
                    this.trackHeight != trackHeight ||
                    this.fadePx != fadePx ||
                    this.reverseFade != reverse;
                this.dNear = dNear;
                this.dFar = dFar;
                this.trackHeight = trackHeight;
                this.fadePx = fadePx;
                this.reverseFade = reverse;
                if (changed) SetVerticesDirty();
            }

            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                Rect r = rectTransform.rect;
                if (r.width <= 0f || r.height <= 0f) return;

                float xL = r.xMin;
                float xR = r.xMax;
                float yB = r.yMin;
                float yT = r.yMax;
                float h = r.height;

                Color baseCol = color;
                float fade = fadePx;
                float trackH = trackHeight;
                float span = dFar - dNear;

                if (fade <= 0.5f || trackH <= 0.5f || span <= 0.0001f)
                {
                    AddQuad(vh, xL, yB, xR, yT, baseCol, baseCol);
                    return;
                }

                float fadeStartD = trackH - fade;
                float aNear = AlphaAtD(dNear, fadeStartD, trackH, fade);
                float aFar = AlphaAtD(dFar, fadeStartD, trackH, fade);
                Color colNear = baseCol; colNear.a = baseCol.a * aNear;
                Color colFar = baseCol; colFar.a = baseCol.a * aFar;

                bool crosses = dNear < fadeStartD && dFar > fadeStartD;

                if (!crosses)
                {
                    if (reverseFade)
                        AddQuad(vh, xL, yB, xR, yT, colFar, colNear);
                    else
                        AddQuad(vh, xL, yB, xR, yT, colNear, colFar);
                    return;
                }

                float t = (fadeStartD - dNear) / span;
                if (reverseFade)
                {
                    float yMid = yT - t * h;
                    AddQuad(vh, xL, yMid, xR, yT, baseCol, colNear);
                    AddQuad(vh, xL, yB, xR, yMid, colFar, baseCol);
                }
                else
                {
                    float yMid = yB + t * h;
                    AddQuad(vh, xL, yB, xR, yMid, colNear, baseCol);
                    AddQuad(vh, xL, yMid, xR, yT, baseCol, colFar);
                }
            }

            private static float AlphaAtD(float d, float fadeStartD, float trackH, float fade)
            {
                if (d <= fadeStartD) return 1f;
                if (d >= trackH) return 0f;
                return (trackH - d) / fade;
            }

            private static void AddQuad(VertexHelper vh, float xL, float yB, float xR, float yT, Color bot, Color top)
            {
                int i = vh.currentVertCount;
                UIVertex v = UIVertex.simpleVert;
                v.position = new Vector3(xL, yB, 0f); v.color = bot; vh.AddVert(v);
                v.position = new Vector3(xR, yB, 0f); v.color = bot; vh.AddVert(v);
                v.position = new Vector3(xR, yT, 0f); v.color = top; vh.AddVert(v);
                v.position = new Vector3(xL, yT, 0f); v.color = top; vh.AddVert(v);
                vh.AddTriangle(i, i + 1, i + 2);
                vh.AddTriangle(i, i + 2, i + 3);
            }
        }

        internal class KvRain
        {
            public readonly KvRainPool pool;
            public readonly GameObject gameObject;
            public readonly RectTransform rectTransform;
            public readonly KvRainGraphic graphic;
            public readonly bool isGhost;
            public KvRawRain rawRain;

            public KvRain(KvRainPool pool, bool isGhost)
            {
                this.pool = pool;
                this.isGhost = isGhost;

                gameObject = new GameObject(isGhost ? "GhostRain" : "Rain", typeof(RectTransform));
                gameObject.transform.SetParent(pool.transform, false);
                rectTransform = gameObject.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.localScale = Vector3.one;

                graphic = gameObject.AddComponent<KvRainGraphic>();
                graphic.raycastTarget = false;
                graphic.color = Color.clear;
            }
        }

        internal class KvRainPool
        {
            public readonly RectTransform transform;
            private KvRain[] rainPool = new KvRain[16];
            private int rainPoolCount;
            private KvRain[] ghostPool = new KvRain[16];
            private int ghostPoolCount;

            public KvRainPool(RectTransform transform)
            {
                this.transform = transform;
            }

            public void AddPool(KvRain rain)
            {
                if (rain == null) return;
                rain.rawRain = null;
                rain.gameObject.SetActive(false);

                ref int count = ref rain.isGhost ? ref ghostPoolCount : ref rainPoolCount;
                ref KvRain[] target = ref rain.isGhost ? ref ghostPool : ref rainPool;
                if (count == target.Length)
                {
                    KvRain[] bigger = new KvRain[target.Length * 2];
                    Array.Copy(target, bigger, target.Length);
                    target = bigger;
                }
                target[count++] = rain;
            }

            public KvRain GetOrNewRain(bool isGhost)
            {
                ref int count = ref isGhost ? ref ghostPoolCount : ref rainPoolCount;
                KvRain[] target = isGhost ? ghostPool : rainPool;
                KvRain rain = count > 0 ? target[--count] : new KvRain(this, isGhost);
                rain.gameObject.SetActive(true);
                rain.rectTransform.anchoredPosition = Vector2.zero;
                rain.rectTransform.sizeDelta = Vector2.zero;
                return rain;
            }
        }

        internal class KvRainManager : MonoBehaviour
        {
            private readonly List<KvRain> active = new List<KvRain>(128);
            private readonly Queue<KvRawRain> pending = new Queue<KvRawRain>(128);

            public void Enqueue(KvRawRain rawRain)
            {
                if (rawRain == null || rawRain.key == null || rawRain.key.rainPool == null) return;
                pending.Enqueue(rawRain);
            }

            public void ClearAll()
            {
                pending.Clear();
                for (int i = active.Count - 1; i >= 0; i--)
                {
                    KvRain rain = active[i];
                    active.RemoveAt(i);
                    if (rain != null && rain.pool != null) rain.pool.AddPool(rain);
                }
            }

            private void Update()
            {
                float fadePx = Main.settings != null ? Main.settings.KeyViewerFadePx : 0f;
                if (fadePx < 0f) fadePx = 0f;

                while (pending.Count > 0)
                {
                    KvRawRain rawRain = pending.Dequeue();
                    if (rawRain == null || rawRain.key == null || rawRain.key.rainPool == null) continue;

                    KvRain rain = rawRain.key.rainPool.GetOrNewRain(rawRain.isGhost);
                    rain.rawRain = rawRain;
                    if (rain.graphic != null) rain.graphic.color = rawRain.color;
                    active.Add(rain);
                }

                if (active.Count == 0) return;

                float now = Time.unscaledTime;
                for (int i = 0; i < active.Count; i++)
                {
                    KvRain rain = active[i];
                    KvRawRain rawRain = rain.rawRain;
                    if (rawRain == null || rawRain.trackHeight <= 0.5f || rawRain.speed <= 0.5f)
                    {
                        Recycle(i--, rain);
                        continue;
                    }

                    float lead = (now - rawRain.startTime) * rawRain.speed;
                    float trail = rawRain.endTime < 0f ? 0f : (now - rawRain.endTime) * rawRain.speed;
                    if (trail > rawRain.trackHeight + 8f)
                    {
                        Recycle(i--, rain);
                        continue;
                    }

                    float drawH = lead - trail;
                    if (drawH <= 0.5f)
                    {
                        rain.rectTransform.sizeDelta = Vector2.zero;
                        continue;
                    }
                    if (drawH > rawRain.trackHeight) drawH = rawRain.trackHeight;

                    float y;
                    if (rawRain.reverse)
                    {
                        y = rawRain.baseY + trail;
                        float bottom = y + drawH;
                        float maxBottom = rawRain.baseY + rawRain.trackHeight;
                        if (bottom > maxBottom) drawH = maxBottom - y;
                    }
                    else
                    {
                        float top = rawRain.baseY - rawRain.trackHeight;
                        float rawY = rawRain.baseY - drawH - trail;
                        y = rawY > top ? rawY : top;
                        drawH = rawY + drawH - y;
                    }

                    if (drawH <= 0.5f)
                    {
                        rain.rectTransform.sizeDelta = Vector2.zero;
                        continue;
                    }

                    float dNear, dFar;
                    if (rawRain.reverse)
                    {
                        dNear = y - rawRain.baseY;
                        dFar = dNear + drawH;
                    }
                    else
                    {
                        dFar = rawRain.baseY - y;
                        dNear = dFar - drawH;
                    }
                    if (rain.graphic != null)
                    {
                        if (rain.graphic.color != rawRain.color) rain.graphic.color = rawRain.color;
                        rain.graphic.SetFadeParams(dNear, dFar, rawRain.trackHeight, fadePx, rawRain.reverse);
                    }
                    rain.rectTransform.anchoredPosition = new Vector2(rawRain.x + rawRain.width * 0.5f, -y);
                    rain.rectTransform.sizeDelta = new Vector2(rawRain.width, drawH);
                }
            }

            private void Recycle(int index, KvRain rain)
            {
                active.RemoveAt(index);
                if (rain != null && rain.pool != null) rain.pool.AddPool(rain);
            }
        }

        internal class KvKey
        {
            public string keyName;
            public string countPrefKey;
            public KeyCode keyCode;
            public KeyCode ghostKeyCode;
            public float dx, dy, width, height;
            public Color noteColor;
            public Color bgColor;
            public Color activeBgColor;
            public Color borderColor;
            public Color activeBorderColor;
            public float borderWidth;
            public float borderRadius;
            public string displayText;
            public float noteWidth;     
            public float noteOffsetY;
            public string noteAlignment; 
            public int noteAlignmentMode; 
            public bool noteEffectEnabled = true;
            public Color ghostNoteColor;
            public bool hasGhostNoteColor;
            public bool noteGlowEnabled;
            public float noteGlowSize;
            public float noteGlowOpacity;
            public Color noteGlowColor;
            public bool noteAutoYCorrection = true;
            public Color fontColor;
            public Color activeFontColor;
            public int fontSize;
            public Color counterColor;
            public Color activeCounterColor;
            public int counterFontSize;
            public string counterAlign;
            public TextAlignmentOptions counterAlignment;
            public bool counterStackTop;
            public bool counterStackBottom;
            public int count;
            public int statValue;
            public KvRawRain lastRain;
            public KvRawRain lastGhostRain;
            public KvRainPool rainPool;
            public bool wasPressed;
            public bool wasGhostPressed;
            public bool wasLimiterGhostPressed;
            public bool ignoredPress;
            public bool counterEnabled = true;
            public bool hasCustomDisplayText = false;
            public bool isStat;
            public bool isKps;
            public bool isTotal;
            public int lastCounterValue = int.MinValue;

            public TextMeshProUGUI labelTmp;
            public TextMeshProUGUI counterTmp;

            public GameObject visualRoot;
            public KvUiRect borderUi;
            public KvUiRect fillUi;
        }

        internal static List<KvKey> keyViewerKeys;
        private static string lastParsedPresetJson;
        private static string lastParsedTab;
        private static float keyViewerCanvasWidth = 800f;
        private static float keyViewerCanvasHeight = 250f;

        private static int keyViewerTotalPresses;
        private static readonly List<float> keyViewerPressLog = new List<float>();
        private static int keyViewerPressLogStart;
        private const float KvKpsWindow = 1.0f;
        private const float KvCounterTextRefreshInterval = 0.05f;
        private const int KvCounterTextUpdatesPerFrame = 4;
        private static float kvNextCounterTextRefreshTime;
        private static int kvCounterTextUpdateBudget;

        private static readonly object kvPressedKeysLock = new object();
        private static readonly HashSet<KeyCode> kvPressedKeys = new HashSet<KeyCode>();
        
        private static KeyCode[] kvPressedSnapshot = new KeyCode[16];
        private static int kvPressedSnapshotCount;
        private static bool kvPressedUseSnapshot;
        private static int[] kvRenderOrder;
        private static int kvRenderOrderCount;
        private static Rewired.Keyboard kvCachedKeyboard;
        private static bool kvKeyboardInitialized;

        public static void KeyViewerPollEvent()
        {
            Event e = Event.current;
            if (e == null) return;
            if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
                ObserveRawKeyState(e.keyCode, true);
            else if (e.type == EventType.KeyUp && e.keyCode != KeyCode.None)
                ObserveRawKeyState(e.keyCode, false);
        }

        internal static void ObserveRawKeyState(KeyCode key, bool pressed)
        {
            if (key == KeyCode.None) return;
            lock (kvPressedKeysLock)
            {
                if (pressed)
                    kvPressedKeys.Add(key);
                else
                    kvPressedKeys.Remove(key);
            }
        }

        private static bool KvHasObservedKey(KeyCode key)
        {
            
            if (kvPressedUseSnapshot)
            {
                int count = kvPressedSnapshotCount;
                KeyCode[] snap = kvPressedSnapshot;
                for (int i = 0; i < count; i++)
                    if (snap[i] == key) return true;
                return false;
            }
            lock (kvPressedKeysLock)
                return kvPressedKeys.Contains(key);
        }

        private static void SnapshotPressedKeysForFrame()
        {
            lock (kvPressedKeysLock)
            {
                int needed = kvPressedKeys.Count;
                if (kvPressedSnapshot.Length < needed)
                    kvPressedSnapshot = new KeyCode[Mathf.NextPowerOfTwo(needed < 16 ? 16 : needed)];
                kvPressedSnapshotCount = 0;
                foreach (KeyCode kc in kvPressedKeys)
                    kvPressedSnapshot[kvPressedSnapshotCount++] = kc;
            }
            kvPressedUseSnapshot = true;
        }

        private static void ReleaseKvPressedSnapshot()
        {
            kvPressedUseSnapshot = false;
        }

        private static void SetKvRectPosSize(RectTransform rt, float x, float y, float w, float h)
        {
            if (rt == null) return;
            Vector2 pos = rt.anchoredPosition;
            if (pos.x != x || pos.y != y)
                rt.anchoredPosition = new Vector2(x, y);
            Vector2 size = rt.sizeDelta;
            if (size.x != w || size.y != h)
                rt.sizeDelta = new Vector2(w, h);
        }

        private static bool KvIsRightAltAlias(KeyCode kc)
        {
            return kc == KeyCode.RightAlt || kc == KeyCode.AltGr;
        }

        private static KeyCode KvRightAltAlias(KeyCode kc)
        {
            if (kc == KeyCode.RightAlt) return KeyCode.AltGr;
            if (kc == KeyCode.AltGr) return KeyCode.RightAlt;
            return KeyCode.None;
        }

        private static bool KvInputGetKey(KeyCode kc)
        {
            try { return Input.GetKey(kc); }
            catch { return false; }
        }

        private static bool KvRewiredGetKey(KeyCode kc)
        {
            if (kvCachedKeyboard == null) return false;
            try
            {
                return kvCachedKeyboard.GetKey(kc);
            }
            catch
            {
                kvCachedKeyboard = null;
                return false;
            }
        }

        private static bool KvIsKeyPressed(KeyCode kc)
        {
            if (!kvKeyboardInitialized)
            {
                kvKeyboardInitialized = true;
                try
                {
                    if (Rewired.ReInput.isReady)
                        kvCachedKeyboard = Rewired.ReInput.controllers.Keyboard;
                }
                catch { }
            }

            if (KvInputGetKey(kc)) return true;
            if (KvHasObservedKey(kc)) return true;
            if (KvRewiredGetKey(kc)) return true;

            if (KvIsRightAltAlias(kc))
            {
                KeyCode alias = KvRightAltAlias(kc);
                if (KvInputGetKey(alias)) return true;
                if (KvHasObservedKey(alias)) return true;
                if (KvRewiredGetKey(alias)) return true;
            }

            return false;
        }

        private static bool KvApplyInputFilters(KeyCode key, bool rawPressed, bool wasPressed, ref bool ignoredPress)
        {
            if (!rawPressed)
            {
                ignoredPress = false;
                return false;
            }

            if (ignoredPress)
                return false;

            if (!wasPressed && !ChatterBlocker.AcceptKeyViewerPress(key))
            {
                ignoredPress = true;
                return false;
            }

            return true;
        }

        public static void ResetKeyViewerStats()
        {
            keyViewerPressLog.Clear();
            keyViewerPressLogStart = 0;
        }

        private static string KvCountKey(string keyName) { return "kvkey_" + (keyName ?? ""); }
        private const string KvTotalPrefKey = "kvtotal";
        private static bool keyViewerTotalLoaded;

        internal static int GetKeyViewerCount(string keyName)
        {
            if (string.IsNullOrEmpty(keyName)) return 0;
            string prefKey = KvCountKey(keyName);
            int pending;
            if (kvPendingCounterPrefs.TryGetValue(prefKey, out pending)) return pending;
            return PlayerPrefs.GetInt(prefKey, 0);
        }

        internal static void SetKeyViewerCount(string keyName, int value)
        {
            if (string.IsNullOrEmpty(keyName)) return;
            string prefKey = KvCountKey(keyName);
            PlayerPrefs.SetInt(prefKey, Mathf.Max(0, value));
            kvPendingCounterPrefs.Remove(prefKey);
            keyViewerKeys = null;
            ScheduleKvSave();
        }

        internal static int GetKeyViewerTotal()
        {
            LoadKeyViewerTotalIfNeeded();
            return keyViewerTotalPresses;
        }

        internal static void SetKeyViewerTotal(int value)
        {
            LoadKeyViewerTotalIfNeeded();
            keyViewerTotalPresses = Mathf.Max(0, value);
            PlayerPrefs.SetInt(KvTotalPrefKey, keyViewerTotalPresses);
            kvPendingTotalPref = false;
            keyViewerKeys = null;
            ScheduleKvSave();
        }

        internal static void ResetAllKeyViewerCounters()
        {
            if (keyViewerKeys != null)
            {
                foreach (KvKey k in keyViewerKeys)
                {
                    if (k != null && !string.IsNullOrEmpty(k.keyName))
                        PlayerPrefs.DeleteKey(k.countPrefKey ?? KvCountKey(k.keyName));
                }
            }
            
            for (int style = 0; style < 4; style++)
            {
                int[] codes = SimpleStyleCodes(style);
                for (int i = 0; i < codes.Length; i++)
                    PlayerPrefs.DeleteKey(KvCountKey(((KeyCode)codes[i]).ToString().ToUpperInvariant()));
            }
            for (int footStyle = 1; footStyle <= 5; footStyle++)
            {
                int[] codes = SimpleFootStyleCodes(footStyle);
                if (codes == null) continue;
                for (int i = 0; i < codes.Length; i++)
                {
                    PlayerPrefs.DeleteKey(KvCountKey(((KeyCode)codes[i]).ToString().ToUpperInvariant()));
                    PlayerPrefs.DeleteKey(KvCountKey("simple_foot_" + i));
                }
            }
            for (int i = 0; i < 20; i++)
                PlayerPrefs.DeleteKey(KvCountKey("simple_hand_" + i));
            
            string dmRaw = Main.settings.keyViewerPresetJson;
            if (!string.IsNullOrWhiteSpace(dmRaw))
            {
                try
                {
                    JObject root = JObject.Parse(dmRaw);
                    JObject keysTable = root["keys"] as JObject;
                    if (keysTable != null)
                    {
                        foreach (var prop in keysTable.Properties())
                        {
                            JArray arr = prop.Value as JArray;
                            if (arr == null) continue;
                            foreach (JToken t in arr)
                            {
                                if (t == null || t.Type != JTokenType.String) continue;
                                string name = t.ToString();
                                if (string.IsNullOrEmpty(name)) continue;
                                PlayerPrefs.DeleteKey(KvCountKey(name.ToUpperInvariant()));
                            }
                        }
                    }
                }
                catch { }
            }
            PlayerPrefs.DeleteKey(KvTotalPrefKey);
            kvPendingCounterPrefs.Clear();
            kvPendingTotalPref = false;
            keyViewerTotalPresses = 0;
            keyViewerTotalLoaded = true;
            keyViewerKeys = null;
            PlayerPrefs.Save();
        }

        internal static List<KeyValuePair<string, int>> EnumerateKeyViewerCounters()
        {
            List<KeyValuePair<string, int>> result = new List<KeyValuePair<string, int>>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (keyViewerKeys != null)
            {
                foreach (KvKey k in keyViewerKeys)
                {
                    if (k == null || string.IsNullOrEmpty(k.keyName)) continue;
                    if (k.isStat || k.isKps || k.isTotal) continue;
                    if (!k.counterEnabled) continue;
                    if (!seen.Add(k.keyName)) continue;
                    result.Add(new KeyValuePair<string, int>(k.keyName, k.count));
                }
            }
            if (string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase))
            {
                int[] codes = SimpleStyleCodes(Mathf.Clamp(Main.settings.KeyViewerSimpleStyle, 0, 3));
                for (int i = 0; i < codes.Length; i++)
                {
                    string name = ((KeyCode)codes[i]).ToString().ToUpperInvariant();
                    if (!seen.Add(name)) continue;
                    result.Add(new KeyValuePair<string, int>(name, GetKeyViewerCount(name)));
                }
            }
            else
            {
                
                string raw = Main.settings.keyViewerPresetJson;
                string tab = string.IsNullOrEmpty(Main.settings.keyViewerSelectedTab) ? "4key" : Main.settings.keyViewerSelectedTab;
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    try
                    {
                        JObject root = JObject.Parse(raw);
                        JObject keysTable = root["keys"] as JObject;
                        JArray keysArr = keysTable != null ? keysTable[tab] as JArray : null;
                        if (keysArr != null)
                        {
                            foreach (JToken t in keysArr)
                            {
                                if (t == null || t.Type != JTokenType.String) continue;
                                string name = t.ToString();
                                if (string.IsNullOrEmpty(name)) continue;
                                name = name.ToUpperInvariant();
                                if (!seen.Add(name)) continue;
                                result.Add(new KeyValuePair<string, int>(name, GetKeyViewerCount(name)));
                            }
                        }
                    }
                    catch { }
                }
            }
            return result;
        }

        private static int[] SimpleStyleCodes(int style)
        {
            switch (style)
            {
                case 0: return Main.settings.KeyViewerSimpleKey10;
                case 1: return Main.settings.KeyViewerSimpleKey12;
                case 2: return Main.settings.KeyViewerSimpleKey16;
                case 3: return Main.settings.KeyViewerSimpleKey20;
                default: return Main.settings.KeyViewerSimpleKey12;
            }
        }

        private static int[] SimpleFootStyleCodes(int style)
        {
            switch (style)
            {
                case 1: return Main.settings.KeyViewerSimpleFootKey2;
                case 2: return Main.settings.KeyViewerSimpleFootKey4;
                case 3: return Main.settings.KeyViewerSimpleFootKey6;
                case 4: return Main.settings.KeyViewerSimpleFootKey8;
                case 5: return Main.settings.KeyViewerSimpleFootKey16;
                default: return null;
            }
        }

        private static void LoadKeyViewerTotalIfNeeded()
        {
            if (keyViewerTotalLoaded) return;
            keyViewerTotalPresses = PlayerPrefs.GetInt(KvTotalPrefKey, 0);
            keyViewerTotalLoaded = true;
        }

        private static int GetDisplayedKeyViewerTotal()
        {
            if (keyViewerKeys == null) return 0;

            int total = 0;
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (KvKey k in keyViewerKeys)
            {
                if (k == null || k.isStat || !k.counterEnabled) continue;
                string key = k.countPrefKey ?? KvCountKey(k.keyName);
                if (!seen.Add(key)) continue;
                total += Mathf.Max(0, k.count);
            }
            return total;
        }

        private static float kvSavePending;
        private static readonly Dictionary<string, int> kvPendingCounterPrefs =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static bool kvPendingTotalPref;

        private static void ScheduleKvSave()
        {
            kvSavePending = Time.unscaledTime + 1.0f;
        }

        private static void MarkKvCounterPrefDirty(string prefKey, int value)
        {
            if (string.IsNullOrEmpty(prefKey)) return;
            kvPendingCounterPrefs[prefKey] = value;
            ScheduleKvSave();
        }

        private static void MarkKvTotalPrefDirty()
        {
            kvPendingTotalPref = true;
            ScheduleKvSave();
        }

        private static void FlushKvPendingPrefs()
        {
            if (kvPendingCounterPrefs.Count > 0)
            {
                foreach (KeyValuePair<string, int> pair in kvPendingCounterPrefs)
                    PlayerPrefs.SetInt(pair.Key, pair.Value);
                kvPendingCounterPrefs.Clear();
            }

            if (kvPendingTotalPref)
            {
                PlayerPrefs.SetInt(KvTotalPrefKey, keyViewerTotalPresses);
                kvPendingTotalPref = false;
            }
        }

        private static void FlushKvSaveIfDue()
        {
            if (kvSavePending > 0f && Time.unscaledTime >= kvSavePending)
            {
                FlushKvPendingPrefs();
                PlayerPrefs.Save();
                kvSavePending = 0f;
            }
        }

        private static void FlushKvSaveNow()
        {
            if (kvSavePending <= 0f) return;
            FlushKvPendingPrefs();
            PlayerPrefs.Save();
            kvSavePending = 0f;
        }

        private static int PruneKeyViewerPressLog(float now)
        {
            float cutoff = now - KvKpsWindow;
            while (keyViewerPressLogStart < keyViewerPressLog.Count && keyViewerPressLog[keyViewerPressLogStart] < cutoff)
                keyViewerPressLogStart++;

            if (keyViewerPressLogStart > 64 && keyViewerPressLogStart * 2 > keyViewerPressLog.Count)
            {
                keyViewerPressLog.RemoveRange(0, keyViewerPressLogStart);
                keyViewerPressLogStart = 0;
            }

            return keyViewerPressLog.Count - keyViewerPressLogStart;
        }

        private static readonly Dictionary<string, KeyCode> KeyNameMap = BuildKeyNameMap();

        private static Dictionary<string, KeyCode> BuildKeyNameMap()
        {
            Dictionary<string, KeyCode> m = new Dictionary<string, KeyCode>(StringComparer.OrdinalIgnoreCase);
            for (char c = 'A'; c <= 'Z'; c++)
            {
                KeyCode kc;
                if (Enum.TryParse<KeyCode>(c.ToString(), true, out kc))
                    m[c.ToString()] = kc;
            }
            for (int i = 0; i <= 9; i++) m[i.ToString()] = (KeyCode)Enum.Parse(typeof(KeyCode), "Alpha" + i);
            m["LEFT SHIFT"] = KeyCode.LeftShift;
            m["RIGHT SHIFT"] = KeyCode.RightShift;
            m["LEFT CTRL"] = KeyCode.LeftControl;
            m["RIGHT CTRL"] = KeyCode.RightControl;
            m["LEFT ALT"] = KeyCode.LeftAlt;
            m["RIGHT ALT"] = KeyCode.RightAlt;
            m["ALT LEFT"] = KeyCode.LeftAlt;
            m["ALT RIGHT"] = KeyCode.RightAlt;
            m["ALTLEFT"] = KeyCode.LeftAlt;
            m["ALTRIGHT"] = KeyCode.RightAlt;
            m["LALT"] = KeyCode.LeftAlt;
            m["RALT"] = KeyCode.RightAlt;
            m["ALT GR"] = KeyCode.RightAlt;
            m["ALTGR"] = KeyCode.RightAlt;
            m["HANGUL"] = KeyCode.RightAlt;
            m["HANGEUL"] = KeyCode.RightAlt;
            m["HAN/YOUNG"] = KeyCode.RightAlt;
            m["HAN/YEONG"] = KeyCode.RightAlt;
            m["HANJA"] = KeyCode.RightControl;
            m["SPACE"] = KeyCode.Space;
            m["TAB"] = KeyCode.Tab;
            m["RETURN"] = KeyCode.Return;
            m["ENTER"] = KeyCode.Return;
            m["BACKSPACE"] = KeyCode.Backspace;
            m["ESCAPE"] = KeyCode.Escape;
            m["UP"] = KeyCode.UpArrow;
            m["DOWN"] = KeyCode.DownArrow;
            m["LEFT"] = KeyCode.LeftArrow;
            m["RIGHT"] = KeyCode.RightArrow;
            m["COMMA"] = KeyCode.Comma;
            m["DOT"] = KeyCode.Period;
            m["PERIOD"] = KeyCode.Period;
            m["FORWARD SLASH"] = KeyCode.Slash;
            m["SLASH"] = KeyCode.Slash;
            m["BACK SLASH"] = KeyCode.Backslash;
            m["BACKSLASH"] = KeyCode.Backslash;
            m["SEMICOLON"] = KeyCode.Semicolon;
            m["APOSTROPHE"] = KeyCode.Quote;
            m["QUOTE"] = KeyCode.Quote;
            m["LEFT BRACKET"] = KeyCode.LeftBracket;
            m["RIGHT BRACKET"] = KeyCode.RightBracket;
            m["MINUS"] = KeyCode.Minus;
            m["EQUALS"] = KeyCode.Equals;
            m["GRAVE"] = KeyCode.BackQuote;
            m["SECTION"] = KeyCode.BackQuote;
            m["BACKQUOTE"] = KeyCode.BackQuote;
            m["BACK QUOTE"] = KeyCode.BackQuote;
            m["SQUARE BRACKET OPEN"] = KeyCode.LeftBracket;
            m["SQUARE BRACKET CLOSE"] = KeyCode.RightBracket;
            m["LEFT BRACKET"] = KeyCode.LeftBracket;
            m["RIGHT BRACKET"] = KeyCode.RightBracket;
            m["LBRACKET"] = KeyCode.LeftBracket;
            m["RBRACKET"] = KeyCode.RightBracket;
            m["OPEN BRACKET"] = KeyCode.LeftBracket;
            m["CLOSE BRACKET"] = KeyCode.RightBracket;
            m["["] = KeyCode.LeftBracket;
            m["]"] = KeyCode.RightBracket;
            m["UP ARROW"] = KeyCode.UpArrow;
            m["DOWN ARROW"] = KeyCode.DownArrow;
            m["LEFT ARROW"] = KeyCode.LeftArrow;
            m["RIGHT ARROW"] = KeyCode.RightArrow;
            m["CAPS LOCK"] = KeyCode.CapsLock;
            m["NUMPAD 0"] = KeyCode.Keypad0;
            m["NUMPAD 1"] = KeyCode.Keypad1;
            m["NUMPAD 2"] = KeyCode.Keypad2;
            m["NUMPAD 3"] = KeyCode.Keypad3;
            m["NUMPAD 4"] = KeyCode.Keypad4;
            m["NUMPAD 5"] = KeyCode.Keypad5;
            m["NUMPAD 6"] = KeyCode.Keypad6;
            m["NUMPAD 7"] = KeyCode.Keypad7;
            m["NUMPAD 8"] = KeyCode.Keypad8;
            m["NUMPAD 9"] = KeyCode.Keypad9;
            m["NUMPAD MULTIPLY"] = KeyCode.KeypadMultiply;
            m["NUMPAD PLUS"] = KeyCode.KeypadPlus;
            m["NUMPAD MINUS"] = KeyCode.KeypadMinus;
            m["NUMPAD DELETE"] = KeyCode.KeypadPeriod;
            m["NUMPAD DIVIDE"] = KeyCode.KeypadDivide;
            m["NUMPAD RETURN"] = KeyCode.KeypadEnter;
            m["NUMPAD ENTER"] = KeyCode.KeypadEnter;
            m["25"] = KeyCode.RightControl;
            m["21"] = KeyCode.RightAlt;
            m["91"] = KeyCode.LeftCommand;
            m["92"] = KeyCode.RightCommand;
            for (int i = 1; i <= 12; i++)
            {
                KeyCode fk;
                if (Enum.TryParse<KeyCode>("F" + i, true, out fk)) m["F" + i] = fk;
            }
            return m;
        }

        private static KeyCode ResolveKeyCode(string name)
        {
            if (string.IsNullOrEmpty(name)) return KeyCode.None;
            KeyCode kc;
            if (KeyNameMap.TryGetValue(name, out kc)) return kc;
            if (Enum.TryParse<KeyCode>(name.Replace(" ", ""), true, out kc)) return kc;
            return KeyCode.None;
        }

        private static readonly Dictionary<string, string> KeyDisplayMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "LEFT SHIFT", "LShift" },
            { "RIGHT SHIFT", "RShift" },
            { "LEFT CTRL", "LCtrl" },
            { "RIGHT CTRL", "RCtrl" },
            { "LEFT ALT", "LAlt" },
            { "RIGHT ALT", "RAlt" },
            { "ALT LEFT", "LAlt" },
            { "ALT RIGHT", "RAlt" },
            { "ALTLEFT", "LAlt" },
            { "ALTRIGHT", "RAlt" },
            { "LALT", "LAlt" },
            { "RALT", "RAlt" },
            { "ALT GR", "RAlt" },
            { "ALTGR", "RAlt" },
            { "HANGUL", "RAlt" },
            { "HANGEUL", "RAlt" },
            { "HAN/YOUNG", "RAlt" },
            { "HAN/YEONG", "RAlt" },
            { "HANJA", "RCtrl" },
            { "LEFT WIN", "LWin" },
            { "RIGHT WIN", "RWin" },
            { "BACKSPACE", "Back" },
            { "RETURN", "Enter" },
            { "ENTER", "Enter" },
            { "ESCAPE", "Esc" },
            { "BACKSLASH", "\\" },
            { "BACK SLASH", "\\" },
            { "FORWARD SLASH", "/" },
            { "SLASH", "/" },
            { "EQUALS", "=" },
            { "MINUS", "-" },
            { "DOT", "." },
            { "PERIOD", "." },
            { "COMMA", "," },
            { "SEMICOLON", ";" },
            { "APOSTROPHE", "'" },
            { "QUOTE", "'" },
            { "LEFT BRACKET", "[" },
            { "RIGHT BRACKET", "]" },
            { "LBRACKET", "[" },
            { "RBRACKET", "]" },
            { "OPEN BRACKET", "[" },
            { "CLOSE BRACKET", "]" },
            { "SQUARE BRACKET OPEN", "[" },
            { "SQUARE BRACKET CLOSE", "]" },
            { "UP ARROW", "↑" },
            { "DOWN ARROW", "↓" },
            { "LEFT ARROW", "←" },
            { "RIGHT ARROW", "→" },
            { "CAPS LOCK", "Caps" },
            { "NUMPAD 0", "Num0" },
            { "NUMPAD 1", "Num1" },
            { "NUMPAD 2", "Num2" },
            { "NUMPAD 3", "Num3" },
            { "NUMPAD 4", "Num4" },
            { "NUMPAD 5", "Num5" },
            { "NUMPAD 6", "Num6" },
            { "NUMPAD 7", "Num7" },
            { "NUMPAD 8", "Num8" },
            { "NUMPAD 9", "Num9" },
            { "NUMPAD MULTIPLY", "Num*" },
            { "NUMPAD PLUS", "Num+" },
            { "NUMPAD MINUS", "Num-" },
            { "NUMPAD DELETE", "Num." },
            { "NUMPAD DIVIDE", "Num/" },
            { "NDivide", "/" },
            { "NUMPAD RETURN", "NEnt" },
            { "25", "RCtrl" },
            { "21", "RAlt" },
            { "91", "LWin" },
            { "92", "RWin" },
            { "GRAVE", "`" },
            { "SECTION", "§" },
            { "BACKQUOTE", "`" },
            { "BACK QUOTE", "`" },
            { "TAB", "Tab" },
            { "SPACE", "Space" },
            { "CAPSLOCK", "Caps" },
            { "UP", "↑" },
            { "DOWN", "↓" },
            { "LEFT", "←" },
            { "RIGHT", "→" },
            { "PAGE UP", "PgUp" },
            { "PAGEUP", "PgUp" },
            { "PAGE DOWN", "PgDn" },
            { "PageDown", "PgDn" },
            { "NUM LOCK", "NmLk" },
            { "NUMLOCK", "NmLk" },
            { "SCROLL LOCK", "ScLk" },
            { "SCROLLLOCK", "ScLk" },
            { "PRINT SCREEN", "PrtSc" },
            { "PRINTSCREEN", "PrtSc" },
            { "PRINT", "PrtSc" },
            { "SYSREQ", "PrtSc" },
            { "INSERT", "Ins" },
            { "DELETE", "Del" },
            { "HOME", "Home" },
            { "END", "End" },
            { "PAUSE", "Pause" },
            { "BREAK", "Brk" },
            { "MENU", "Menu" },
            { "APPLICATION", "App" },
            { "CLEAR", "Clr" },
            { "HELP", "Help" },
        };

        private static string DefaultDisplayFor(string keyName)
        {
            if (string.IsNullOrEmpty(keyName)) return "";
            string s;
            if (KeyDisplayMap.TryGetValue(keyName, out s)) return s;
            return keyName;
        }

        private static TextAlignmentOptions KvCounterAlignment(string align)
        {
            if (string.Equals(align, "top", StringComparison.OrdinalIgnoreCase))
                return TextAlignmentOptions.Top;
            if (string.Equals(align, "bottom", StringComparison.OrdinalIgnoreCase))
                return TextAlignmentOptions.Bottom;
            if (string.Equals(align, "right", StringComparison.OrdinalIgnoreCase))
                return TextAlignmentOptions.MidlineRight;
            if (string.Equals(align, "left", StringComparison.OrdinalIgnoreCase))
                return TextAlignmentOptions.MidlineLeft;
            return TextAlignmentOptions.Center;
        }

        private static int KvNoteAlignmentMode(string align)
        {
            if (string.Equals(align, "left", StringComparison.OrdinalIgnoreCase)) return -1;
            if (string.Equals(align, "right", StringComparison.OrdinalIgnoreCase)) return 1;
            return 0;
        }

        private static void CacheKeyLayoutModes(KvKey k)
        {
            if (k == null) return;
            k.noteAlignmentMode = KvNoteAlignmentMode(k.noteAlignment);
            k.counterAlignment = KvCounterAlignment(k.counterAlign);
            k.counterStackTop = string.Equals(k.counterAlign, "top", StringComparison.OrdinalIgnoreCase);
            k.counterStackBottom = string.Equals(k.counterAlign, "bottom", StringComparison.OrdinalIgnoreCase);
        }

        private static Color HexToColor(string hex, float alpha)
        {
            if (string.IsNullOrEmpty(hex)) return new Color(1f, 1f, 1f, alpha);
            string s = hex.Trim();
            try
            {
                if (string.Equals(s, "transparent", StringComparison.OrdinalIgnoreCase))
                    return new Color(0f, 0f, 0f, 0f);

                if (s.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
                {
                    int lp = s.IndexOf('(');
                    int rp = s.IndexOf(')');
                    if (lp > 0 && rp > lp)
                    {
                        string inner = s.Substring(lp + 1, rp - lp - 1);
                        string[] parts = inner.Split(new[] { ',', '/' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            float r = ParseColorComponent(parts[0], 255f);
                            float g = ParseColorComponent(parts[1], 255f);
                            float b = ParseColorComponent(parts[2], 255f);
                            float a = parts.Length >= 4 ? ParseAlphaComponent(parts[3]) : 1f;
                            return new Color(r, g, b, a);
                        }
                    }
                    return new Color(1f, 1f, 1f, alpha);
                }

                string h = s.TrimStart('#');
                if (h.Length == 3)
                {
                    int r = Convert.ToInt32(new string(h[0], 2), 16);
                    int g = Convert.ToInt32(new string(h[1], 2), 16);
                    int b = Convert.ToInt32(new string(h[2], 2), 16);
                    return new Color(r / 255f, g / 255f, b / 255f, alpha);
                }
                if (h.Length == 4)
                {
                    int r = Convert.ToInt32(new string(h[0], 2), 16);
                    int g = Convert.ToInt32(new string(h[1], 2), 16);
                    int b = Convert.ToInt32(new string(h[2], 2), 16);
                    int a = Convert.ToInt32(new string(h[3], 2), 16);
                    return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                }
                if (h.Length == 6)
                {
                    int r = Convert.ToInt32(h.Substring(0, 2), 16);
                    int g = Convert.ToInt32(h.Substring(2, 2), 16);
                    int b = Convert.ToInt32(h.Substring(4, 2), 16);
                    return new Color(r / 255f, g / 255f, b / 255f, alpha);
                }
                if (h.Length == 8)
                {
                    int r = Convert.ToInt32(h.Substring(0, 2), 16);
                    int g = Convert.ToInt32(h.Substring(2, 2), 16);
                    int b = Convert.ToInt32(h.Substring(4, 2), 16);
                    int a = Convert.ToInt32(h.Substring(6, 2), 16);
                    return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                }
            }
            catch { }
            return new Color(1f, 1f, 1f, alpha);
        }

        private static float ParseColorComponent(string s, float scale)
        {
            string t = s.Trim();
            if (t.EndsWith("%"))
            {
                float pct;
                if (float.TryParse(t.TrimEnd('%').Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pct))
                    return Mathf.Clamp01(pct / 100f);
                return 1f;
            }
            float v;
            if (float.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v))
                return Mathf.Clamp01(v / scale);
            return 1f;
        }

        private static float ParseAlphaComponent(string s)
        {
            string t = s.Trim();
            if (t.EndsWith("%"))
            {
                float pct;
                if (float.TryParse(t.TrimEnd('%').Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pct))
                    return Mathf.Clamp01(pct / 100f);
                return 1f;
            }
            float v;
            if (float.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v))
                return v <= 1f ? Mathf.Clamp01(v) : Mathf.Clamp01(v / 255f);
            return 1f;
        }

        private static bool JNotNull(JToken t)
        {
            return t != null && t.Type != JTokenType.Null;
        }

        private static string JStr(JObject p, string key, string def)
        {
            JToken t = p[key];
            if (!JNotNull(t)) return def;
            
            if (t.Type != JTokenType.String && t.Type != JTokenType.Integer && t.Type != JTokenType.Float
                && t.Type != JTokenType.Boolean && t.Type != JTokenType.Date && t.Type != JTokenType.Guid
                && t.Type != JTokenType.Uri && t.Type != JTokenType.TimeSpan)
                return def;
            string s = t.ToString();
            return string.IsNullOrEmpty(s) ? def : s;
        }

        private static string JOptionalString(JObject p, string key)
        {
            JToken t = p[key];
            if (!JNotNull(t)) return null;
            if (t.Type != JTokenType.String && t.Type != JTokenType.Integer && t.Type != JTokenType.Float
                && t.Type != JTokenType.Boolean && t.Type != JTokenType.Date && t.Type != JTokenType.Guid
                && t.Type != JTokenType.Uri && t.Type != JTokenType.TimeSpan)
                return null;
            return t.ToString();
        }

        private static float JFloat(JObject p, string key, float def)
        {
            JToken t = p[key];
            if (!JNotNull(t)) return def;
            try { return t.ToObject<float>(); } catch { return def; }
        }

        private static int JInt(JObject p, string key, int def)
        {
            JToken t = p[key];
            if (!JNotNull(t)) return def;
            try { return t.ToObject<int>(); } catch { return def; }
        }

        private static bool JBool(JObject p, string key, bool def)
        {
            JToken t = p[key];
            if (!JNotNull(t)) return def;
            try { return t.ToObject<bool>(); } catch { return def; }
        }

        private static void BuildKeyViewerImageOverlayIfNeeded()
        {
            if (kvImageBuilt && kvImageRoot != null)
            {
                if (kvImageCanvas != null) kvImageCanvas.sortingOrder = KvImageSortingOrder;
                return;
            }

            kvImageRoot = new GameObject("KorenResourcePack.KeyViewer.Images");
            UnityEngine.Object.DontDestroyOnLoad(kvImageRoot);

            kvImageCanvas = kvImageRoot.AddComponent<Canvas>();
            kvImageCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            kvImageCanvas.sortingOrder = KvImageSortingOrder;

            var scaler = kvImageRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            kvImageRoot.AddComponent<GraphicRaycaster>().enabled = false;
            kvNotesLayer = NewKvLayer("Notes");
            EnsureKvRainManager();
            kvKeysLayer = NewKvLayer("Keys");
            kvImageBuilt = true;
        }

        private static RectTransform NewKvLayer(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(kvImageRoot.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static void EnsureKvRainManager()
        {
            if (kvImageRoot == null) return;
            if (kvRainManager == null)
                kvRainManager = kvImageRoot.AddComponent<KvRainManager>();
        }

        private static void DestroyKvImageChildren()
        {
            if (kvRainManager != null)
                kvRainManager.ClearAll();
            DestroyKvChildren(kvNotesLayer);
            DestroyKvChildren(kvKeysLayer);
        }

        private static void DestroyKvChildren(Transform parent)
        {
            if (parent == null) return;
            foreach (Transform child in parent)
            {
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private static KvUiRect NewKvSpriteRect(string name, Transform parent, Sprite sprite)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            KvUiRect ui = new KvUiRect();
            ui.gameObject = go;
            ui.rectTransform = rt;

            if (sprite != null)
            {
                Image image = go.AddComponent<Image>();
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
                image.raycastTarget = false;
                image.enabled = true;
                image.pixelsPerUnitMultiplier = 2f;
                ui.image = image;
            }
            else
            {
                KvRoundedImage rounded = go.AddComponent<KvRoundedImage>();
                rounded.raycastTarget = false;
                rounded.enabled = true;
                ui.rounded = rounded;
            }
            return ui;
        }

        private static KvUiRect NewKeyViewerRect(string name, Transform parent)
        {
            BundleLoader.EnsureBundleLoaded();
            bool isBorder = !string.IsNullOrEmpty(name) && name.IndexOf("Border", StringComparison.OrdinalIgnoreCase) >= 0;
            Sprite sprite = isBorder ? BundleLoader.bundleKeyOutline : BundleLoader.bundleKeyBackground;
            return NewKvSpriteRect(name, parent, sprite);
        }

        private static GameObject NewKeyVisualRoot(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(kvKeysLayer, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            return go;
        }

        private static KvRainPool NewKeyRainPool(string name)
        {
            if (kvNotesLayer == null) return null;
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(kvNotesLayer, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
            return new KvRainPool(rt);
        }

        private static void BeginKeyViewerImageFrame()
        {
            EnsureKvRainManager();
        }

        private static void EndKeyViewerImageFrame()
        {
        }

        private static void PlaceKeyRect(KvUiRect ui, Rect rect, Color color, float radius, float borderThickness)
        {
            PlaceKvUiRect(ui, rect, color, radius, borderThickness);
        }

        private static void PlaceKeyRect(KvUiRect ui, Rect rect, Color color, float radius)
        {
            PlaceKvUiRect(ui, rect, color, radius, 0f);
        }

        private static void PlaceKvUiRect(KvUiRect ui, Rect rect, Color color, float radius, float borderThickness)
        {
            if (ui == null || (ui.rounded == null && ui.image == null)) return;
            if (rect.width <= 0f || rect.height <= 0f || color.a <= 0f)
            {
                if (ui.rounded != null && ui.rounded.enabled) ui.rounded.enabled = false;
                if (ui.image != null && ui.image.enabled) ui.image.enabled = false;
                return;
            }

            SetKvRectPosSize(ui.rectTransform, rect.x, -rect.y, rect.width, rect.height);

            if (ui.image != null)
            {
                
                if (ui.image.color != color) ui.image.color = color;
                if (!ui.image.enabled) ui.image.enabled = true;
                return;
            }

            float maxRadius = KvMaxCornerRadiusPx;
            float quarterMin = (rect.width < rect.height ? rect.width : rect.height) * 0.25f;
            if (quarterMin < maxRadius) maxRadius = quarterMin;
            float effectiveRadius = radius < 0f ? 0f : radius;
            if (effectiveRadius > maxRadius) effectiveRadius = maxRadius;

            if (ui.rounded.color != color) ui.rounded.color = color;
            ui.rounded.SetVertexAlpha(1f, 1f);
            ui.rounded.SetShape(effectiveRadius, false, false, borderThickness);
            if (!ui.rounded.enabled) ui.rounded.enabled = true;
        }

        private static void BuildKeyViewerTextOverlayIfNeeded()
        {
            if (kvTextBuilt && kvTextRoot != null)
            {
                if (kvTextCanvas != null) kvTextCanvas.sortingOrder = KvTextSortingOrder;
                return;
            }

            kvTextRoot = new GameObject("KorenResourcePack.KeyViewer.Text");
            UnityEngine.Object.DontDestroyOnLoad(kvTextRoot);

            kvTextCanvas = kvTextRoot.AddComponent<Canvas>();
            kvTextCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            kvTextCanvas.sortingOrder = KvTextSortingOrder;

            var scaler = kvTextRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            kvTextRoot.AddComponent<GraphicRaycaster>().enabled = false;
            kvTextBuilt = true;
        }

        private static void DestroyKvTextChildren()
        {
            if (kvTextRoot == null) return;
            foreach (Transform child in kvTextRoot.transform)
            {
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private static TextMeshProUGUI NewKvLabel(string text, TextAlignmentOptions align)
        {
            GameObject go = new GameObject("KVLabel", typeof(RectTransform));
            go.transform.SetParent(kvTextRoot.transform, false);
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.alignment = align;
            t.color = Color.white;
            TmpCompatibility.DisableWordWrapping(t);
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.text = text ?? "";
            
            EnsureKvActiveFont();
            if (kvActiveFont != null) ApplyTmpFont(t);
            TmpCompatibility.TrySetOutline(t, KvShadowColor, 0.18f);
            RectTransform rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            return t;
        }

        private static void EnsureKvActiveFont()
        {
            if (kvActiveFont != null) return;
            try
            {
                string requested = Main.settings != null ? (Main.settings.fontName ?? "") : "";
                TMP_FontAsset fa = BundleLoader.GetBestTmpFont(requested);
                if (fa == null) return;
                kvActiveFont = fa;
                kvActiveFontName = requested;
            }
            catch { }
        }

        private static void SetTmpEnabled(TextMeshProUGUI t, bool value)
        {
            if (t != null && t.enabled != value) t.enabled = value;
        }

        private static void SetTmpText(TextMeshProUGUI t, string text)
        {
            if (t == null) return;
            string s = text ?? "";
            if (!ReferenceEquals(t.text, s) && t.text != s) t.text = s;
        }

        private static void SetTmpColor(TextMeshProUGUI t, Color color)
        {
            if (t != null && t.color != color) t.color = color;
        }

        private static void SetTmpFontSize(TextMeshProUGUI t, float fontSize)
        {
            if (t == null) return;
            float minSize = Mathf.Max(6f, fontSize * 0.4f);
            if (!t.enableAutoSizing) t.enableAutoSizing = true;
            if (Mathf.Abs(t.fontSizeMax - fontSize) > 0.01f) t.fontSizeMax = fontSize;
            if (Mathf.Abs(t.fontSizeMin - minSize) > 0.01f) t.fontSizeMin = minSize;
            if (Mathf.Abs(t.fontSize - fontSize) > 0.01f) t.fontSize = fontSize;
            ScaleTmpShadowOffset(t, fontSize);
        }

        private static void SetTmpAlignment(TextMeshProUGUI t, TextAlignmentOptions alignment)
        {
            if (t != null && t.alignment != alignment) t.alignment = alignment;
        }

        private static void SetTmpRect(RectTransform rt, float x, float y, float width, float height)
        {
            if (rt == null) return;
            Vector2 pos = rt.anchoredPosition;
            if (Mathf.Abs(pos.x - x) > 0.01f || Mathf.Abs(pos.y - y) > 0.01f)
                rt.anchoredPosition = new Vector2(x, y);

            Vector2 size = rt.sizeDelta;
            if (Mathf.Abs(size.x - width) > 0.01f || Mathf.Abs(size.y - height) > 0.01f)
                rt.sizeDelta = new Vector2(width, height);
        }

        private static void ApplyFontToKeyViewer()
        {
            if (!kvTextBuilt) return;
            string requested = Main.settings != null ? (Main.settings.fontName ?? "") : "";
            if (requested != kvActiveFontName || kvActiveFont == null)
            {
                TMP_FontAsset fa = null;
                try { fa = BundleLoader.GetBestTmpFont(requested); } catch { }
                if (fa == null) return;
                kvActiveFont = fa;
                kvActiveFontName = requested;
            }

            if (keyViewerKeys == null) return;
            foreach (var k in keyViewerKeys)
            {
                if (k.labelTmp != null && k.labelTmp.font != kvActiveFont)
                    ApplyTmpFont(k.labelTmp);
                if (k.counterTmp != null && k.counterTmp.font != kvActiveFont)
                    ApplyTmpFont(k.counterTmp);
            }
        }

        private static void ApplyTmpFont(TextMeshProUGUI text)
        {
            if (text == null || kvActiveFont == null) return;
            text.font = kvActiveFont;
            TmpCompatibility.SetFontSharedMaterial(text, BundleLoader.GetBundleFontMaterial(kvActiveFont));
            
            TmpCompatibility.TrySetOutline(text, KvShadowColor, 0.18f);
            ApplyTmpShadow(text, text.fontSize);
            TmpCompatibility.RefreshTextRendering(text);
        }

        private static void ApplyTmpShadow(TextMeshProUGUI text, float fontSize)
        {
            if (text == null) return;
            TextShadows.ApplyTmpDropShadow(text, KvDropShadowColor, KvShadowOffsetX, KvShadowOffsetY, KvShadowSoftness, KvShadowDilate);
            ScaleTmpShadowOffset(text, fontSize);
        }

        private static void ScaleTmpShadowOffset(TextMeshProUGUI text, float fontSize)
        {
            TextShadows.ScaleTmpDropShadowOffset(text, fontSize, KvShadowReferenceSize, KvShadowOffsetX, KvShadowOffsetY);
        }

        private static void RebuildKeyViewerLayout()
        {
            keyViewerKeys = new List<KvKey>();
            string raw;
            string tab;
            ResolveActivePreset(out raw, out tab);
            if (string.IsNullOrEmpty(tab)) tab = "4key";
            if (string.IsNullOrWhiteSpace(raw))
            {
                DestroyKvImageChildren();
                DestroyKvTextChildren();
                kvRenderOrderCount = 0;
                lastParsedPresetJson = raw;
                lastParsedTab = tab;
                return;
            }

            BuildKeyViewerImageOverlayIfNeeded();
            BuildKeyViewerTextOverlayIfNeeded();
            DestroyKvImageChildren();
            DestroyKvTextChildren();

            try
            {
                JObject root = JObject.Parse(raw);

                JToken sel = root["selectedKeyType"];
                if (JNotNull(sel) && sel.Type == JTokenType.String)
                {
                    tab = sel.ToString();
                    
                    if (!string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase))
                        Main.settings.keyViewerSelectedTab = tab;
                }

                JObject keysTable = root["keys"] as JObject;
                JObject posTable = (root["keyPositions"] as JObject) ?? (root["positions"] as JObject);
                if (keysTable == null || posTable == null)
                {
                    Main.mod?.Logger?.Log("[KeyViewer] preset missing 'keys' or 'keyPositions' object at root.");
                    lastParsedPresetJson = raw;
                    lastParsedTab = tab;
                    return;
                }

                JArray keyArr = keysTable[tab] as JArray;
                JArray posArr = posTable[tab] as JArray;
                if (keyArr == null || posArr == null)
                {
                    string availableKeys = "";
                    string availablePos = "";
                    foreach (var prop in keysTable.Properties())
                    {
                        availableKeys += (availableKeys.Length > 0 ? "," : "") + prop.Name;
                    }
                    foreach (var prop in posTable.Properties())
                    {
                        availablePos += (availablePos.Length > 0 ? "," : "") + prop.Name;
                    }
                    Main.mod?.Logger?.Log("[KeyViewer] tab '" + tab + "' missing. Available keys=[" + availableKeys + "] positions=[" + availablePos + "]");
                    lastParsedPresetJson = raw;
                    lastParsedTab = tab;
                    return;
                }

                int n = Mathf.Min(keyArr.Count, posArr.Count);
                float canvasW = 0f, canvasH = 0f;
                for (int i = 0; i < n; i++)
                {
                    JObject p = posArr[i] as JObject;
                    if (p == null) continue;
                    if (JBool(p, "hidden", false)) continue;
                    KvKey k = new KvKey();
                    k.keyName = keyArr[i].ToString();
                    string countKey = JOptionalString(p, "countKey");
                    k.countPrefKey = KvCountKey(!string.IsNullOrEmpty(countKey) ? countKey : k.keyName);
                    k.keyCode = ResolveKeyCode(k.keyName);
                    string ghostKey = JOptionalString(p, "ghostKey");
                    k.ghostKeyCode = !string.IsNullOrEmpty(ghostKey) ? ResolveKeyCode(ghostKey) : KeyCode.None;
                    k.dx = JFloat(p, "dx", 0f);
                    k.dy = JFloat(p, "dy", 0f);
                    k.width = JFloat(p, "width", 60f);
                    k.height = JFloat(p, "height", 60f);
                    string noteHex = JStr(p, "noteColor", "#FFFFFF");
                    float noteOp = JFloat(p, "noteOpacity", 80f) / 100f;
                    k.noteColor = HexToColor(noteHex, noteOp);
                    string bgHex = JStr(p, "backgroundColor", "#3C3C3C");
                    k.bgColor = HexToColor(bgHex, 0.5f);
                    k.activeBgColor = HexToColor(JStr(p, "activeBackgroundColor", bgHex), 0.5f);
                    if (JBool(p, "idleTransparent", false)) k.bgColor.a = 0f;
                    if (JBool(p, "activeTransparent", false)) k.activeBgColor.a = 0f;
                    k.borderColor = HexToColor(JStr(p, "borderColor", "#FFFFFF"), 0.4f);
                    k.activeBorderColor = HexToColor(JStr(p, "activeBorderColor", JStr(p, "borderColor", "#FFFFFF")), k.borderColor.a);
                    k.borderWidth = JFloat(p, "borderWidth", 3f);
                    k.borderRadius = JFloat(p, "borderRadius", 10f);

                    string dt = JOptionalString(p, "displayText");
                    k.displayText = !string.IsNullOrEmpty(dt) ? dt : DefaultDisplayFor(k.keyName);

                    k.noteWidth = JFloat(p, "noteWidth", 0f);
                    k.noteOffsetY = JFloat(p, "noteOffsetY", 0f);
                    k.noteAlignment = JStr(p, "noteAlignment", "center");
                    k.noteEffectEnabled = JBool(p, "noteEffectEnabled", true);
                    string ghostNoteHex = JOptionalString(p, "ghostNoteColor");
                    k.hasGhostNoteColor = !string.IsNullOrEmpty(ghostNoteHex);
                    if (k.hasGhostNoteColor)
                        k.ghostNoteColor = HexToColor(ghostNoteHex, JFloat(p, "ghostNoteOpacity", 45f) / 100f);
                    k.noteGlowEnabled = JBool(p, "noteGlowEnabled", false);
                    k.noteGlowSize = JFloat(p, "noteGlowSize", 20f);
                    k.noteGlowOpacity = JFloat(p, "noteGlowOpacity", 70f) / 100f;
                    string glowHex = JStr(p, "noteGlowColor", noteHex);
                    k.noteGlowColor = HexToColor(glowHex, k.noteGlowOpacity);
                    k.noteAutoYCorrection = JBool(p, "noteAutoYCorrection", true);

                    k.count = PlayerPrefs.GetInt(k.countPrefKey, 0);
                    string fontHex = JStr(p, "fontColor", "#FFFFFF");
                    k.fontColor = HexToColor(fontHex, 1f);
                    k.activeFontColor = HexToColor(JStr(p, "activeFontColor", fontHex), 1f);
                    k.fontSize = JInt(p, "fontSize", 18);

                    JObject counterObj = p["counter"] as JObject;
                    k.counterEnabled = counterObj != null ? JBool(counterObj, "enabled", true) : true;
                    k.counterFontSize = counterObj != null ? JInt(counterObj, "fontSize", Mathf.Max(8, Mathf.RoundToInt(k.fontSize * 0.85f))) : Mathf.Max(8, Mathf.RoundToInt(k.fontSize * 0.85f));
                    k.counterAlign = counterObj != null ? JStr(counterObj, "align", "bottom") : "bottom";
                    JObject counterFill = counterObj != null ? counterObj["fill"] as JObject : null;
                    string counterIdleHex = counterFill != null ? JStr(counterFill, "idle", fontHex) : fontHex;
                    string counterActiveHex = counterFill != null ? JStr(counterFill, "active", JStr(p, "activeFontColor", fontHex)) : JStr(p, "activeFontColor", fontHex);
                    k.counterColor = HexToColor(counterIdleHex, 1f);
                    k.activeCounterColor = HexToColor(counterActiveHex, 1f);
                    CacheKeyLayoutModes(k);

                    k.labelTmp = NewKvLabel(k.displayText, TextAlignmentOptions.Center);
                    if (k.counterEnabled)
                        k.counterTmp = NewKvLabel("", TextAlignmentOptions.Bottom);
                    else
                        k.counterTmp = null;

                    k.isStat = false;
                    k.isKps = false;
                    k.isTotal = false;
                    k.lastCounterValue = int.MinValue;

                    k.visualRoot = NewKeyVisualRoot("KVKey_" + i);
                    
                    k.fillUi = NewKeyViewerRect("Fill", k.visualRoot.transform);
                    k.borderUi = NewKeyViewerRect("Border", k.visualRoot.transform);
                    if (k.noteEffectEnabled)
                        k.rainPool = NewKeyRainPool("RainPool_" + i);

                    if (kvActiveFont != null)
                    {
                        ApplyTmpFont(k.labelTmp);
                        ApplyTmpFont(k.counterTmp);
                    }

                    keyViewerKeys.Add(k);

                    bool isFootKey = JBool(p, "isFootKey", false);
                    canvasW = Mathf.Max(canvasW, k.dx + k.width);
                    if (!isFootKey)
                        canvasH = Mathf.Max(canvasH, k.dy + k.height);
                }

                JObject statTable = root["statPositions"] as JObject;
                if (statTable != null)
                {
                    JArray statArr = statTable[tab] as JArray;
                    if (statArr != null)
                    {
                        for (int i = 0; i < statArr.Count; i++)
                        {
                            JObject p = statArr[i] as JObject;
                            if (p == null) continue;
                            if (JBool(p, "hidden", false)) continue;
                            KvKey k = new KvKey();
                            k.keyName = JStr(p, "statType", "stat");
                            k.keyCode = KeyCode.None;
                            k.dx = JFloat(p, "dx", 0f);
                            k.dy = JFloat(p, "dy", 0f);
                            k.width = JFloat(p, "width", 100f);
                            k.height = JFloat(p, "height", 30f);
                            k.noteColor = new Color(1f, 1f, 1f, 0f);
                            k.bgColor = HexToColor(JStr(p, "backgroundColor", "#3C3C3C"), 0.5f);
                            k.activeBgColor = k.bgColor;
                            if (JBool(p, "idleTransparent", false)) k.bgColor.a = 0f;
                            k.borderColor = HexToColor(JStr(p, "borderColor", "#FFFFFF"), 0.4f);
                            k.activeBorderColor = k.borderColor;
                            k.borderWidth = JFloat(p, "borderWidth", 4f);
                            k.borderRadius = JFloat(p, "borderRadius", 10f);

                            k.fontColor = HexToColor(JStr(p, "fontColor", "#FFFFFF"), 1f);
                            k.activeFontColor = k.fontColor;
                            k.fontSize = JInt(p, "fontSize", 16);
                            JObject counterObj = p["counter"] as JObject;
                            k.counterEnabled = counterObj != null ? JBool(counterObj, "enabled", true) : false;
                            k.counterFontSize = counterObj != null ? JInt(counterObj, "fontSize", k.fontSize) : k.fontSize;
                            k.counterAlign = counterObj != null ? JStr(counterObj, "align", "center") : "center";
                            JObject counterFill = counterObj != null ? counterObj["fill"] as JObject : null;
                            string counterIdleHex = counterFill != null ? JStr(counterFill, "idle", JStr(p, "fontColor", "#FFFFFF")) : JStr(p, "fontColor", "#FFFFFF");
                            string counterActiveHex = counterFill != null ? JStr(counterFill, "active", counterIdleHex) : counterIdleHex;
                            k.counterColor = HexToColor(counterIdleHex, 1f);
                            k.activeCounterColor = HexToColor(counterActiveHex, 1f);
                            CacheKeyLayoutModes(k);

                            string statLabel = k.keyName.Equals("kps", StringComparison.OrdinalIgnoreCase) ? "KPS" :
                                               k.keyName.Equals("total", StringComparison.OrdinalIgnoreCase) ? "Total" : k.keyName.ToUpperInvariant();
                            string jsonDisplay = JOptionalString(p, "displayText");
                            k.hasCustomDisplayText = !string.IsNullOrEmpty(jsonDisplay);
                            if (k.hasCustomDisplayText)
                                k.displayText = jsonDisplay;
                            else
                                k.displayText = k.counterEnabled ? statLabel : "0  " + statLabel;

                            k.count = -1;
                            k.isStat = true;
                            k.isKps = k.keyName.Equals("kps", StringComparison.OrdinalIgnoreCase);
                            k.isTotal = k.keyName.Equals("total", StringComparison.OrdinalIgnoreCase);
                            k.lastCounterValue = int.MinValue;
                            k.labelTmp = NewKvLabel(k.displayText, TextAlignmentOptions.Center);
                            if (k.counterEnabled)
                                k.counterTmp = NewKvLabel("", TextAlignmentOptions.Center);
                            ApplyTmpFont(k.labelTmp);
                            ApplyTmpFont(k.counterTmp);
                            k.visualRoot = NewKeyVisualRoot("KVStat_" + i);
                            
                            k.fillUi = NewKeyViewerRect("Fill", k.visualRoot.transform);
                            k.borderUi = NewKeyViewerRect("Border", k.visualRoot.transform);
                            keyViewerKeys.Add(k);
                            canvasW = Mathf.Max(canvasW, k.dx + k.width);
                            canvasH = Mathf.Max(canvasH, k.dy + k.height);
                        }
                    }
                }

                if (canvasW > 0f) keyViewerCanvasWidth = canvasW + 40f;
                if (canvasH > 0f)
                {
                    bool isSimpleMode = string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase);
                    float activeTrackHeight = isSimpleMode ? Main.settings.KeyViewerSimpleRainHeight : Main.settings.KeyViewerTrackHeight;
                    keyViewerCanvasHeight = canvasH + (Main.settings.KeyViewerNoteEffect ? activeTrackHeight : 0f) + 40f;
                }
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[KeyViewer] Parse failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
                keyViewerKeys = new List<KvKey>();
            }

            ApplyFontToKeyViewer();
            SortKeyViewerVisualLayers();

            lastParsedPresetJson = raw;
            lastParsedTab = tab;
            Main.mod?.Logger?.Log("[KeyViewer] Built " + keyViewerKeys.Count + " items for tab '" + tab + "' canvas=" + keyViewerCanvasWidth + "x" + keyViewerCanvasHeight);
        }

        private struct KvVisualLayerEntry
        {
            public KvKey key;
            public int index;
        }

        private static void SortKeyViewerVisualLayers()
        {
            if (keyViewerKeys == null) return;
            int n = keyViewerKeys.Count;
            if (n <= 1)
            {
                if (kvRenderOrder == null || kvRenderOrder.Length < n)
                    kvRenderOrder = new int[n];
                if (n == 1) kvRenderOrder[0] = 0;
                kvRenderOrderCount = n;
                return;
            }

            List<KvVisualLayerEntry> entries = new List<KvVisualLayerEntry>();
            for (int i = 0; i < n; i++)
            {
                KvKey k = keyViewerKeys[i];
                if (k != null && k.visualRoot != null)
                {
                    KvVisualLayerEntry e = new KvVisualLayerEntry();
                    e.key = k;
                    e.index = i;
                    entries.Add(e);
                }
            }

            entries.Sort((a, b) =>
            {
                int cmp = a.key.dy.CompareTo(b.key.dy);
                return cmp != 0 ? cmp : a.index.CompareTo(b.index);
            });

            if (kvRenderOrder == null || kvRenderOrder.Length < entries.Count)
                kvRenderOrder = new int[entries.Count];
            kvRenderOrderCount = entries.Count;

            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].key.visualRoot.transform.SetSiblingIndex(i);
                kvRenderOrder[i] = entries[i].index;
            }
        }

        private static void EnsureKeyViewerLayout()
        {
            string raw;
            string tab;
            ResolveActivePreset(out raw, out tab);
            if (keyViewerKeys == null || raw != lastParsedPresetJson || tab != lastParsedTab)
            {
                RebuildKeyViewerLayout();
            }
        }

        private static void ResolveActivePreset(out string raw, out string tab)
        {
            if (string.Equals(Main.settings.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase))
            {
                int style = Mathf.Clamp(Main.settings.KeyViewerSimpleStyle, 0, 3);
                raw = SimplePresets.GetJson(style);
                tab = SimplePresets.TabName;
                return;
            }
            raw = Main.settings.keyViewerPresetJson;
            tab = Main.settings.keyViewerSelectedTab;
        }

        private static void UpdateKeyViewerKeyImages(KvKey k, Rect keyRect, bool pressed, float scale)
        {
            if (k == null || k.fillUi == null) return;

            float scaledRadius = Mathf.Min(Mathf.Max(0f, k.borderRadius * scale), KvMaxCornerRadiusPx);
            bool showBorder = k.borderUi != null && k.borderWidth > 0.5f && Mathf.Max(k.borderColor.a, k.activeBorderColor.a) > 0f;
            
            bool spriteMode = k.fillUi != null && k.fillUi.image != null;
            if (showBorder)
            {
                float keyMin = Mathf.Min(keyRect.width, keyRect.height);
                float adaptiveBorder = Mathf.Clamp(k.borderWidth * (keyMin / 60f), 1f, keyMin * 0.12f);
                Color borderColor = pressed ? k.activeBorderColor : k.borderColor;
                PlaceKeyRect(k.borderUi, keyRect, borderColor, scaledRadius, adaptiveBorder);

                Rect fillRect;
                float fillRadius;
                if (spriteMode)
                {
                    fillRect = keyRect;
                    fillRadius = scaledRadius; 
                }
                else
                {
                    fillRect = new Rect(
                        keyRect.x + adaptiveBorder,
                        keyRect.y + adaptiveBorder,
                        Mathf.Max(0f, keyRect.width - adaptiveBorder * 2f),
                        Mathf.Max(0f, keyRect.height - adaptiveBorder * 2f)
                    );
                    fillRadius = Mathf.Max(0f, scaledRadius - adaptiveBorder);
                }
                PlaceKeyRect(k.fillUi, fillRect, pressed ? k.activeBgColor : k.bgColor, fillRadius);
            }
            else
            {
                if (k.borderUi != null)
                {
                    if (k.borderUi.rounded != null) k.borderUi.rounded.enabled = false;
                    if (k.borderUi.image != null) k.borderUi.image.enabled = false;
                }
                PlaceKeyRect(k.fillUi, keyRect, pressed ? k.activeBgColor : k.bgColor, scaledRadius);
            }
        }

        internal static void DrawKeyViewer()
        {
            LoadKeyViewerTotalIfNeeded();
            EnsureKeyViewerLayout();
            FlushKvSaveIfDue();

            if (keyViewerKeys == null || keyViewerKeys.Count == 0)
            {
                SetActiveIfChanged(kvImageRoot, false);
                SetActiveIfChanged(kvTextRoot, false);
                return;
            }
            SetActiveIfChanged(kvImageRoot, true);
            SetActiveIfChanged(kvTextRoot, true);
            BeginKeyViewerImageFrame();
            SnapshotPressedKeysForFrame();
            try
            {

            if (kvActiveFont == null || (Main.settings != null && Main.settings.fontName != kvActiveFontName))
                ApplyFontToKeyViewer();

            float scale = Mathf.Clamp(Main.settings.KeyViewerScale, 0.2f, 4f);
            float originX = Main.settings.KeyViewerOffsetX;
            float originY = (Screen.height - keyViewerCanvasHeight * scale) + Main.settings.KeyViewerOffsetY;

            float now = Time.unscaledTime;
            Settings ms = Main.settings;
            bool isSimpleMode = string.Equals(ms.KeyViewerMode, "simple", StringComparison.OrdinalIgnoreCase);
            bool reverse = ms.KeyViewerNoteReverse;
            float noteSpeed = isSimpleMode ? ms.KeyViewerSimpleRainSpeed : ms.KeyViewerNoteSpeed;
            float trackHeight = isSimpleMode ? ms.KeyViewerSimpleRainHeight : ms.KeyViewerTrackHeight;
            float speed = (noteSpeed > 1f ? noteSpeed : 1f) * scale;
            float trackH = (trackHeight > 0f ? trackHeight : 0f) * scale;
            bool noteEffectOn = ms.KeyViewerNoteEffect;
            bool showCounter = ms.KeyViewerShowCounter;
            if ((!noteEffectOn || (isSimpleMode && !ms.KeyViewerSimpleUseRain)) && kvRainManager != null)
                kvRainManager.ClearAll();
            
            int outOfLimiterMode = isSimpleMode ? 1 : Mathf.Clamp(ms.KeyViewerAdvancedOutOfLimiterMode, 0, 2);
            int currentKps = PruneKeyViewerPressLog(now);
            bool refreshCounterText = now >= kvNextCounterTextRefreshTime;
            if (refreshCounterText)
                kvNextCounterTextRefreshTime = now + KvCounterTextRefreshInterval;
            kvCounterTextUpdateBudget = refreshCounterText ? KvCounterTextUpdatesPerFrame : 0;

            float kvStackGapHalf = Mathf.Max(3f, 4f * scale);
            float kvStatStackGapHalf = scale;
            float kvInlinePad = Mathf.Max(6f, 12f * scale);
            float kvInlineGap = Mathf.Max(2f, 4f * scale);
            float kvCounterLift = Mathf.Max(3f, 5f * scale);

            float autoTopY = float.MaxValue;
            float autoBottomY = float.MinValue;

            for (int i = 0; i < keyViewerKeys.Count; i++)
            {
                var k = keyViewerKeys[i];
                if (k.isStat) continue;

                float y = originY + k.dy * scale;
                float yMax = y + k.height * scale;

                if (y < autoTopY) autoTopY = y;
                if (yMax > autoBottomY) autoBottomY = yMax;
            }

            int n = keyViewerKeys.Count;
            if (kvRenderOrder == null || kvRenderOrderCount != n)
                SortKeyViewerVisualLayers();

            for (int oi = 0; oi < kvRenderOrderCount; oi++)
            {
                int i = kvRenderOrder[oi];
                KvKey k = keyViewerKeys[i];
                bool isStat = k.isStat;
                bool rawPressed = !isStat && k.keyCode != KeyCode.None && KvIsKeyPressed(k.keyCode);
                bool rawGhostPressed = !isStat && k.ghostKeyCode != KeyCode.None && KvIsKeyPressed(k.ghostKeyCode);

                bool limiterBlocked = !isStat && k.keyCode != KeyCode.None && rawPressed
                                      && KeyLimiter.ShouldBlockKey(k.keyCode);
                bool limiterHidden = limiterBlocked && outOfLimiterMode == 0;
                bool limiterGhostPressed = limiterBlocked && outOfLimiterMode == 1;
                bool limiterFullPressed = limiterBlocked && outOfLimiterMode == 2;

                bool pressed = !isStat && k.keyCode != KeyCode.None
                               && !limiterHidden
                               && !limiterGhostPressed
                               && KvApplyInputFilters(k.keyCode, rawPressed, k.wasPressed, ref k.ignoredPress);
                bool ghostPressed = limiterGhostPressed || (!isStat && k.ghostKeyCode != KeyCode.None && rawGhostPressed);
                bool visualPressed = pressed;

                Rect keyRect = new Rect(
                    originX + k.dx * scale,
                    originY + k.dy * scale,
                    k.width * scale,
                    k.height * scale
                );

                bool canRain = !isStat && noteEffectOn && k.noteEffectEnabled && trackH > 0f && k.rainPool != null;
                Color ghostRainColor = k.hasGhostNoteColor ? k.ghostNoteColor : k.noteColor;
                if (!k.hasGhostNoteColor) ghostRainColor.a *= 0.45f;
                if (canRain)
                {
                    UpdateKeyViewerRainGeometry(k.lastRain, k, keyRect, scale, reverse, speed, trackH, autoTopY, autoBottomY, k.noteColor);
                    UpdateKeyViewerRainGeometry(k.lastGhostRain, k, keyRect, scale, reverse, speed, trackH, autoTopY, autoBottomY, ghostRainColor);
                }

                if (!isStat)
                {
                    if (pressed && !k.wasPressed)
                    {
                        if (canRain)
                            k.lastRain = BeginKeyViewerRain(k, false, now, keyRect, scale, reverse, speed, trackH, autoTopY, autoBottomY, k.noteColor);

                        if (!isSimpleMode)
                        {
                            keyViewerTotalPresses++;
                            MarkKvTotalPrefDirty();
                        }

                        if (!limiterFullPressed)
                        {
                            k.count++;
                            keyViewerPressLog.Add(now);
                            MarkKvCounterPrefDirty(k.countPrefKey ?? KvCountKey(k.keyName), k.count);
                        }
                    }
                    else if (!pressed && k.wasPressed)
                    {
                        EndKeyViewerRain(k.lastRain, now);
                    }

                    if (limiterGhostPressed && !k.wasLimiterGhostPressed)
                    {
                        if (!isSimpleMode)
                        {
                            keyViewerTotalPresses++;
                            MarkKvTotalPrefDirty();
                        }
                    }

                    if (ghostPressed && !k.wasGhostPressed)
                    {
                        if (canRain)
                            k.lastGhostRain = BeginKeyViewerRain(k, true, now, keyRect, scale, reverse, speed, trackH, autoTopY, autoBottomY, ghostRainColor);
                    }
                    else if (!ghostPressed && k.wasGhostPressed)
                    {
                        EndKeyViewerRain(k.lastGhostRain, now);
                    }

                    k.wasPressed = pressed;
                    k.wasGhostPressed = ghostPressed;
                    k.wasLimiterGhostPressed = limiterGhostPressed;
                }
                else
                {
                    if (k.isKps)
                        k.statValue = currentKps;
                    else if (k.isTotal)
                        k.statValue = isSimpleMode ? GetDisplayedKeyViewerTotal() : keyViewerTotalPresses;
                    else
                        k.statValue = 0;

                    if (!k.counterEnabled && !k.hasCustomDisplayText)
                    {
                        if (refreshCounterText && k.statValue != k.lastCounterValue)
                        {
                            k.lastCounterValue = k.statValue;
                            if (k.isKps)
                                k.displayText = currentKps + "  KPS";
                            else if (k.isTotal)
                                k.displayText = (isSimpleMode ? GetDisplayedKeyViewerTotal() : keyViewerTotalPresses) + "  Total";
                        }
                    }
                }

                UpdateKeyViewerKeyImages(k, keyRect, visualPressed, scale);

                int fs = Mathf.Max(8, Mathf.RoundToInt(k.fontSize * scale));

                bool showCounterForThisKey = showCounter && k.counterEnabled;

                if (k.labelTmp != null)
                {
                    SetTmpColor(k.labelTmp, visualPressed ? k.activeFontColor : k.fontColor);
                    SetTmpFontSize(k.labelTmp, fs);
                    SetTmpText(k.labelTmp, k.displayText);

                    var rt = k.labelTmp.rectTransform;

                    if (isStat && showCounterForThisKey)
                    {
                        
                        bool stackedTop = k.counterStackTop;
                        bool stackedBottom = k.counterStackBottom;
                        float stackGapHalf = kvStatStackGapHalf;
                        if (stackedTop)
                        {
                            
                            float counterHeight = keyRect.height * 0.5f;
                            SetTmpAlignment(k.labelTmp, TextAlignmentOptions.Top);
                            SetTmpFontSize(k.labelTmp, Mathf.Max(8, Mathf.RoundToInt(k.fontSize * scale * 1.15f)));
                            SetTmpRect(rt, keyRect.x, -(keyRect.y + counterHeight + stackGapHalf),
                                keyRect.width, keyRect.height - counterHeight - stackGapHalf);
                        }
                        else if (stackedBottom)
                        {
                            
                            SetTmpAlignment(k.labelTmp, TextAlignmentOptions.Bottom);
                            SetTmpFontSize(k.labelTmp, Mathf.Max(8, Mathf.RoundToInt(k.fontSize * scale * 1.15f)));
                            SetTmpRect(rt, keyRect.x, -keyRect.y,
                                keyRect.width, keyRect.height * 0.5f - stackGapHalf);
                        }
                        else
                        {
                            float pad = Mathf.Min(keyRect.width * 0.08f, kvInlinePad);
                            float availableWidth = Mathf.Max(0f, keyRect.width - pad * 2f);
                            float labelWidth = availableWidth * 0.42f;
                            SetTmpAlignment(k.labelTmp, TextAlignmentOptions.MidlineLeft);
                            SetTmpRect(rt, keyRect.x + pad, -keyRect.y, labelWidth, keyRect.height);
                        }
                    }
                    else if (showCounterForThisKey)
                    {
                        
                        bool nstackedTop = k.counterStackTop;
                        bool nstackedBottom = k.counterStackBottom;
                        float stackGapHalf = kvStackGapHalf;
                        float counterLift = kvCounterLift;
                        if (nstackedTop)
                        {
                            
                            float counterHeight = keyRect.height * 0.4f;
                            SetTmpAlignment(k.labelTmp, TextAlignmentOptions.Top);
                            SetTmpRect(rt, keyRect.x, -(keyRect.y + counterHeight + stackGapHalf + counterLift),
                                keyRect.width, keyRect.height - counterHeight - stackGapHalf);
                        }
                        else if (nstackedBottom)
                        {
                            
                            SetTmpAlignment(k.labelTmp, TextAlignmentOptions.Bottom);
                            SetTmpRect(rt, keyRect.x, -(keyRect.y + counterLift * 0.25f), keyRect.width, keyRect.height * 0.6f - stackGapHalf);
                        }
                        else
                        {
                            SetTmpAlignment(k.labelTmp, TextAlignmentOptions.Center);
                            SetTmpRect(rt, keyRect.x, -keyRect.y, keyRect.width, keyRect.height);
                        }
                    }
                    else
                    {
                        SetTmpAlignment(k.labelTmp, TextAlignmentOptions.Center);
                        SetTmpRect(rt, keyRect.x, -keyRect.y, keyRect.width, keyRect.height);
                    }
                    SetTmpEnabled(k.labelTmp, true);
                }

                if (k.counterTmp != null)
                {
                    SetTmpEnabled(k.counterTmp, showCounterForThisKey);
                    if (showCounterForThisKey)
                    {
                        int csize = Mathf.Max(8, Mathf.RoundToInt((k.counterFontSize > 0 ? k.counterFontSize : k.fontSize) * scale));
                        SetTmpFontSize(k.counterTmp, csize);
                        SetTmpColor(k.counterTmp, visualPressed ? k.activeCounterColor : k.counterColor);
                        int curCounter = isStat ? k.statValue : k.count;
                        bool counterTextMissing = string.IsNullOrEmpty(k.counterTmp.text);
                        if (curCounter != k.lastCounterValue &&
                            (counterTextMissing || (refreshCounterText && (isStat || kvCounterTextUpdateBudget > 0))))
                        {
                            if (!counterTextMissing && !isStat)
                                kvCounterTextUpdateBudget--;
                            k.lastCounterValue = curCounter;
                            SetTmpText(k.counterTmp, curCounter.ToString());
                        }
                        SetTmpAlignment(k.counterTmp, k.counterAlignment);

                        var rt = k.counterTmp.rectTransform;
                        if (isStat)
                        {
                            
                            bool stackedTop = k.counterStackTop;
                            bool stackedBottom = k.counterStackBottom;
                            float stackGapHalf = kvStatStackGapHalf;
                            if (stackedTop)
                            {
                                
                                int baseSize = k.counterFontSize > 0 ? k.counterFontSize : k.fontSize;
                                SetTmpFontSize(k.counterTmp, Mathf.Max(8, Mathf.RoundToInt(baseSize * scale * 1.15f)));
                                SetTmpAlignment(k.counterTmp, TextAlignmentOptions.Bottom);
                                SetTmpRect(rt, keyRect.x, -keyRect.y,
                                    keyRect.width, keyRect.height * 0.5f - stackGapHalf);
                            }
                            else if (stackedBottom)
                            {
                                
                                int baseSize = k.counterFontSize > 0 ? k.counterFontSize : k.fontSize;
                                SetTmpFontSize(k.counterTmp, Mathf.Max(8, Mathf.RoundToInt(baseSize * scale * 1.15f)));
                                SetTmpAlignment(k.counterTmp, TextAlignmentOptions.Top);
                                float labelHeight = keyRect.height * 0.5f;
                                SetTmpRect(rt, keyRect.x, -(keyRect.y + labelHeight + stackGapHalf),
                                    keyRect.width, keyRect.height - labelHeight - stackGapHalf);
                            }
                            else
                            {
                                float pad = Mathf.Min(keyRect.width * 0.08f, kvInlinePad);
                                float availableWidth = Mathf.Max(0f, keyRect.width - pad * 2f);
                                float labelWidth = availableWidth * 0.42f;
                                float gap = kvInlineGap;
                                SetTmpRect(rt, keyRect.x + pad + labelWidth + gap, -keyRect.y,
                                    Mathf.Max(0f, availableWidth - labelWidth - gap), keyRect.height);
                            }
                        }
                        else
                        {
                            
                            float counterLift = kvCounterLift;
                            bool nstackedTop = k.counterStackTop;
                            bool nstackedBottom = k.counterStackBottom;
                            float stackGapHalf = kvStackGapHalf;
                            if (nstackedTop)
                            {
                                SetTmpAlignment(k.counterTmp, TextAlignmentOptions.Bottom);
                                SetTmpRect(rt, keyRect.x, -(keyRect.y + counterLift), keyRect.width, keyRect.height * 0.4f - stackGapHalf);
                            }
                            else if (nstackedBottom)
                            {
                                SetTmpAlignment(k.counterTmp, TextAlignmentOptions.Top);
                                float labelHeight = keyRect.height * 0.6f;
                                SetTmpRect(rt, keyRect.x, -(keyRect.y + labelHeight + stackGapHalf - counterLift),
                                    keyRect.width, keyRect.height - labelHeight - stackGapHalf);
                            }
                            else
                            {
                                SetTmpRect(rt, keyRect.x, -keyRect.y, keyRect.width, keyRect.height);
                }
            }
        }

                }
            }

            EndKeyViewerImageFrame();
            }
            finally
            {
                ReleaseKvPressedSnapshot();
            }
        }

        private static KvRawRain BeginKeyViewerRain(KvKey k, bool ghost, float now, Rect keyRect,
                                                     float scale, bool reverse, float speed, float trackH,
                                                     float autoTopY, float autoBottomY, Color color)
        {
            if (k == null || k.rainPool == null || kvRainManager == null) return null;

            KvRawRain raw = new KvRawRain();
            raw.key = k;
            raw.isGhost = ghost;
            raw.startTime = now;
            raw.endTime = -1f;
            UpdateKeyViewerRainGeometry(raw, k, keyRect, scale, reverse, speed, trackH, autoTopY, autoBottomY, color);
            kvRainManager.Enqueue(raw);
            return raw;
        }

        private static void EndKeyViewerRain(KvRawRain raw, float now)
        {
            if (raw != null && raw.endTime < 0f)
                raw.endTime = now;
        }

        private static void UpdateKeyViewerRainGeometry(KvRawRain raw, KvKey k, Rect keyRect,
                                                         float scale, bool reverse, float speed, float trackH,
                                                         float autoTopY, float autoBottomY, Color color)
        {
            if (raw == null || k == null) return;

            float noteWidth = k.noteWidth > 0f ? k.noteWidth * scale : keyRect.width;
            if (noteWidth <= 0.5f) noteWidth = keyRect.width;
            int alignMode = k.noteAlignmentMode;
            float noteX = alignMode < 0
                ? keyRect.x
                : (alignMode > 0
                    ? keyRect.xMax - noteWidth
                    : keyRect.x + (keyRect.width - noteWidth) * 0.5f);

            float baseY = k.noteAutoYCorrection
                ? (reverse ? autoBottomY : autoTopY)
                : (reverse ? keyRect.yMax : keyRect.y);
            baseY += k.noteOffsetY * scale;

            raw.x = noteX;
            raw.width = noteWidth;
            raw.baseY = baseY;
            raw.trackHeight = trackH;
            raw.speed = speed;
            raw.reverse = reverse;
            raw.color = color;
        }

        internal static void ImportKeyViewerPreset()
        {
            string picked = PickPresetJsonFile();
            if (string.IsNullOrEmpty(picked)) return;
            try
            {
                string txt = File.ReadAllText(picked);
                JObject.Parse(txt);
                Main.settings.keyViewerPresetJson = txt;
                keyViewerKeys = null;
                Main.mod?.Logger?.Log("[KeyViewer] Imported preset from " + picked);
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[KeyViewer] Import failed: " + ex.Message);
            }
        }

        private static string PickPresetJsonFile()
        {
            try
            {
                return PickPresetJsonFileImpl();
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[KeyViewer] Picker failed: " + ex.Message);
                return null;
            }
        }

        private static string PickPresetJsonFileImpl()
        {
            string path = UnityFileDialog.FileBrowser.PickFile(
                "", "JSON Preset", new[] { "json" }, "Select DM Note preset");
            return string.IsNullOrEmpty(path) ? null : path;
        }

        internal static void HideKeyViewer()
        {
            FlushKvSaveNow();
            SetActiveIfChanged(kvImageRoot, false);
            SetActiveIfChanged(kvTextRoot, false);
        }

        internal static void ShowKeyViewer()
        {
            SetActiveIfChanged(kvImageRoot, true);
            SetActiveIfChanged(kvTextRoot, true);
        }

        private static void SetActiveIfChanged(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }

        internal static void DestroyKeyViewer()
        {
            FlushKvSaveNow();

            try
            {
                if (kvRainManager != null) kvRainManager.ClearAll();
                if (kvImageRoot != null) UnityEngine.Object.Destroy(kvImageRoot);
                if (kvTextRoot != null) UnityEngine.Object.Destroy(kvTextRoot);
            }
            catch { }

            kvImageRoot = null;
            kvImageCanvas = null;
            kvNotesLayer = null;
            kvRainManager = null;
            kvKeysLayer = null;
            kvImageBuilt = false;
            kvTextRoot = null;
            kvTextCanvas = null;
            kvTextBuilt = false;
            kvActiveFont = null;
            kvActiveFontName = null;
            kvCachedKeyboard = null;
            kvKeyboardInitialized = false;

            if (keyViewerKeys != null)
            {
                foreach (var k in keyViewerKeys)
                {
                    k.labelTmp = null;
                    k.counterTmp = null;
                    k.visualRoot = null;
                    k.borderUi = null;
                    k.fillUi = null;
                }
            }
            keyViewerKeys = null;
            lastParsedPresetJson = null;
            lastParsedTab = null;
            keyViewerPressLog.Clear();
            keyViewerPressLogStart = 0;
        }
    }
}
