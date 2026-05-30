using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JipperKeyViewer.KeyViewer
{
    public class RainSystem
    {
        private readonly KeyViewerSettings settings;

        private readonly Stack<Rain> rainPool = new Stack<Rain>();
        private readonly Stack<RawRain> rawRainPool = new Stack<RawRain>();
        private readonly List<int> rainActiveKeys = new List<int>();
        private readonly HashSet<int> rainActiveSet = new HashSet<int>();

        private const int MAX_RAWRAIN_POOL_SIZE = 60;
        private const int MAX_POOL_SIZE = 30;

        private readonly float[] rowSpeeds = new float[3];
        private readonly float[] rowHeights = new float[3];
        private float cachedRainSpeed1, cachedRainSpeed2, cachedRainSpeed3;
        private float cachedRainHeight1, cachedRainHeight2, cachedRainHeight3;

        /// <summary>Sprite used for ghost rain tiled rendering / 鬼雨 tiled 渲染用的精灵</summary>
        public Sprite GhostRainSprite;

        public RainSystem(KeyViewerSettings settings)
        {
            this.settings = settings;
        }

        public void UpdateEffects(Key[] keys)
        {
            if (keys == null || keys.Length == 0) return;
            if (rainActiveKeys.Count == 0) return;

            if (cachedRainSpeed1 != settings.RainSpeedRow1 || cachedRainSpeed2 != settings.RainSpeedRow2 ||
                cachedRainSpeed3 != settings.RainSpeedRow3 || cachedRainHeight1 != settings.RainHeightRow1 ||
                cachedRainHeight2 != settings.RainHeightRow2 || cachedRainHeight3 != settings.RainHeightRow3)
            {
                rowSpeeds[0] = settings.RainSpeedRow1 / 300f;
                rowSpeeds[1] = settings.RainSpeedRow2 / 300f;
                rowSpeeds[2] = settings.RainSpeedRow3 / 300f;
                rowHeights[0] = settings.RainHeightRow1;
                rowHeights[1] = settings.RainHeightRow2;
                rowHeights[2] = settings.RainHeightRow3;
                cachedRainSpeed1 = settings.RainSpeedRow1;
                cachedRainSpeed2 = settings.RainSpeedRow2;
                cachedRainSpeed3 = settings.RainSpeedRow3;
                cachedRainHeight1 = settings.RainHeightRow1;
                cachedRainHeight2 = settings.RainHeightRow2;
                cachedRainHeight3 = settings.RainHeightRow3;
            }

            float fadeDuration = settings.RainFadeDuration;
            // Single native property call per frame / 每帧只调用一次原生属性
            float unscaledDt = Time.unscaledDeltaTime;
            float dt = unscaledDt * 1000f;
            float dtSec = unscaledDt;

            for (int i = 0; i < rainActiveKeys.Count; i++)
            {
                int ki = rainActiveKeys[i];
                Key key = keys[ki];
                if (key == null || key.rainList.Count == 0)
                {
                    rainActiveSet.Remove(ki);
                    rainActiveKeys[i] = rainActiveKeys[rainActiveKeys.Count - 1];
                    rainActiveKeys.RemoveAt(rainActiveKeys.Count - 1);
                    i--;
                    continue;
                }

                int row = ki < 8 ? 0 : (ki < 16 ? 1 : 2);

                for (int j = key.rainList.Count - 1; j >= 0; j--)
                {
                    RawRain rain = key.rainList[j];
                    if (rain.removed) continue;

                    bool updateSize = rain.growing;

                    if (!rain.UpdateLocation(updateSize, rowSpeeds[row], rowHeights[row], dt))
                    {
                        ReturnRainAndRawRain(rain, key, j);
                        continue;
                    }

                    Rain r = rain.rainComponent;
                    if (r == null) continue;

                    if (rain.sizeDelta != null)
                    {
                        r.transform.sizeDelta = rain.sizeDelta.Value;
                        rain.sizeDelta = null;
                    }
                    if (rain.anchoredPosition != null)
                    {
                        r.transform.anchoredPosition = rain.anchoredPosition.Value;
                        rain.anchoredPosition = null;
                    }

                    if (r.fadingOut)
                    {
                        r.fadeTimer += dtSec;
                        float t = Mathf.Clamp01(r.fadeTimer / fadeDuration);
                        float eased = t * (2f - t);
                        float a = 1f - eased;
                        var c = r.image.color;
                        c.a = a;
                        r.image.color = c;
                        if (t >= 1f)
                        {
                            ReturnRainAndRawRain(rain, key, j);
                        }
                    }
                }
            }
        }

        private void ReturnRainAndRawRain(RawRain rain, Key key, int listIndex)
        {
            rain.removed = true;
            if (rain.rainComponent != null)
            {
                ReturnRain(rain.rainComponent);
                rain.rainComponent = null;
            }
            ReturnRawRain(rain);
            key.rainList.RemoveAt(listIndex);
        }

        public void TriggerRainEffect(int keyIndex, Key key)
        {
            if (key == null || !IsRainEnabledForKey(keyIndex))
                return;
            CreateRainDropForKey(keyIndex, key);
        }

        public void ReleaseRainEffect(int keyIndex, Key key)
        {
            if (key == null || key.rainList.Count == 0) return;
            for (int i = key.rainList.Count - 1; i >= 0; i--)
            {
                if (key.rainList[i].isGhost) continue;
                key.rainList[i].growing = false;
                if (settings.EnableRainFade && key.rainList[i].rainComponent != null)
                    key.rainList[i].rainComponent.StartFadeOut(settings.RainFadeDuration);
                break;
            }
        }

        public void TriggerGhostRain(int keyIndex, Key key)
        {
            if (key == null || !IsRainEnabledForKey(keyIndex)) return;
            CreateRainDropForKey(keyIndex, key, isGhost: true);
        }

        public void ReleaseGhostRain(int keyIndex, Key key)
        {
            if (key == null || key.rainList.Count == 0) return;
            for (int i = key.rainList.Count - 1; i >= 0; i--)
            {
                if (key.rainList[i].isGhost)
                {
                    key.rainList[i].growing = false;
                    break;
                }
            }
        }

        public void ClearActiveDrops(Key[] keys)
        {
            if (keys == null) return;
            rainActiveKeys.Clear();
            rainActiveSet.Clear();
            foreach (var key in keys)
            {
                if (key == null) continue;
                foreach (var rain in key.rainList)
                {
                    if (rain.rainComponent != null)
                    {
                        ReturnRain(rain.rainComponent);
                        rain.rainComponent = null;
                    }
                    ReturnRawRain(rain);
                }
                key.rainList.Clear();
            }
        }

        public void ClearAll(Key[] keys)
        {
            ClearActiveDrops(keys);
            ClearPool();
        }

        public void ClearPool()
        {
            while (rainPool.Count > 0)
                Object.Destroy(rainPool.Pop().gameObject);
        }

        public Color GetRainColor(byte color)
        {
            return color switch
            {
                0 => settings.RainColor,
                3 => settings.RainColor3,
                _ => settings.RainColor2
            };
        }

        public Rain GetRainFromPool(Transform parent, Sprite sprite = null, bool isTiled = false)
        {
            Rain r;
            if (rainPool.Count > 0)
            {
                r = rainPool.Pop();
                r.Init(parent, sprite, isTiled);
            }
            else
            {
                GameObject go = new GameObject("Rain");
                go.AddComponent<RectTransform>();
                r = go.AddComponent<Rain>();
                r.Init(parent, sprite, isTiled);
            }
            return r;
        }

        public void ReturnRain(Rain r)
        {
            r.gameObject.SetActive(false);
            r.rawRain = null;
            r.transform.SetParent(null);
            if (rainPool.Count < MAX_POOL_SIZE)
                rainPool.Push(r);
            else
                Object.Destroy(r.gameObject);
        }

        private RawRain GetRawRain(byte color)
        {
            RawRain r;
            if (rawRainPool.Count > 0)
            {
                r = rawRainPool.Pop();
                r.color = color;
                r.removed = false;
                r.elapsedMs = 0f;
                r.sizeDelta = null;
                r.anchoredPosition = null;
                r.rainComponent = null;
                r.isGhost = false;
                r.growing = false;
                r.FinalSize = default;
            }
            else
            {
                r = new RawRain(color);
            }
            return r;
        }

        public void ReturnRawRain(RawRain r)
        {
            if (rawRainPool.Count >= MAX_RAWRAIN_POOL_SIZE) return;
            r.removed = false;
            r.sizeDelta = null;
            r.anchoredPosition = null;
            r.rainComponent = null;
            r.isGhost = false;
            r.growing = false;
            rawRainPool.Push(r);
        }

        private void CreateRainDropForKey(int keyIndex, Key key, bool isGhost = false)
        {
            if (key == null || key.rain == null) return;

            // Ghost rain renders identically to normal rain (solid quad, untiled).
            Sprite rainSprite = null;
            bool isTiled = false;
            RawRain rawRain = GetRawRain(key.color);
            Rain rainComponent = GetRainFromPool(key.rain.transform, rainSprite, isTiled);
            rainComponent.rawRain = rawRain;
            rawRain.rainComponent = rainComponent;
            rawRain.isGhost = isGhost;
            rawRain.growing = true;
            rainComponent.image.color = isGhost ? settings.GhostRainColor : key.rainColor;
            if (isGhost)
                rainComponent.transform.SetAsLastSibling();

            key.rainList.Add(rawRain);

            if (!rainActiveSet.Contains(keyIndex))
            {
                rainActiveSet.Add(keyIndex);
                rainActiveKeys.Add(keyIndex);
            }
        }

        private bool IsRainEnabledForKey(int keyIndex)
        {
            if (keyIndex < 8) return settings.EnableRainForRow1;
            if (keyIndex < 16) return settings.EnableRainForRow2;
            if (keyIndex < 20) return settings.EnableRainForRow3;
            return false;
        }
    }
}
