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

        public Sprite GhostRainSprite;

        public RainSystem(KeyViewerSettings settings)
        {
            this.settings = settings;
        }

        public void UpdateEffects(Key[] keys, long now)
        {
            if (keys == null || keys.Length == 0) return;
            if (rainActiveKeys.Count == 0) return;

            if (IsDmNoteRainMode())
            {
                var host = global::KorenResourcePack.Main.settings;
                float speed = Mathf.Max(1f, host != null ? host.KeyViewerNoteSpeed : 1000f) / 1000f;
                float height = Mathf.Max(0f, host != null ? host.KeyViewerTrackHeight : 200f);
                rowSpeeds[0] = rowSpeeds[1] = rowSpeeds[2] = speed;
                rowHeights[0] = rowHeights[1] = rowHeights[2] = height;
            }
            else if (cachedRainSpeed1 != settings.RainSpeedRow1 || cachedRainSpeed2 != settings.RainSpeedRow2 ||
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
                float speedFactor = rowSpeeds[row];
                float height = rowHeights[row];

                for (int j = key.rainList.Count - 1; j >= 0; j--)
                {
                    RawRain rain = key.rainList[j];
                    if (rain.removed) continue;

                    if (!rain.UpdateLocation(now, speedFactor, height))
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
                    r.image.SetEdgeFade(
                        settings.EnableRainOpacityGradient,
                        rain.visibleNear,
                        rain.visibleFar,
                        height,
                        settings.RainOpacityGradientLength
                    );

                    if (rain.fading)
                    {
                        if (fadeDuration <= 0f)
                        {
                            ReturnRainAndRawRain(rain, key, j);
                            continue;
                        }
                        float t = (now - rain.fadeStartMs) / (fadeDuration * 1000f);
                        if (t >= 1f)
                        {
                            ReturnRainAndRawRain(rain, key, j);
                            continue;
                        }
                        if (t < 0f) t = 0f;
                        float eased = t * (2f - t);
                        var c = r.image.color;
                        c.a = 1f - eased;
                        r.image.color = c;
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

        public void TriggerRainEffect(int keyIndex, Key key, long now)
        {
            if (key == null || !IsRainEnabledForKey(keyIndex))
                return;
            CreateRainDropForKey(keyIndex, key, now);
        }

        public void ReleaseRainEffect(int keyIndex, Key key, long now)
        {
            if (key == null || key.rainList.Count == 0) return;
            for (int i = key.rainList.Count - 1; i >= 0; i--)
            {
                RawRain rain = key.rainList[i];
                if (rain.isGhost) continue;
                rain.Finish(now);
                if (settings.EnableRainOpacityGradient)
                    rain.fading = false;
                else if (settings.EnableRainFade && rain.rainComponent != null)
                {
                    rain.fading = true;
                    rain.fadeStartMs = now;
                }
                break;
            }
        }

        public void TriggerGhostRain(int keyIndex, Key key, long now)
        {
            if (key == null || !IsRainEnabledForKey(keyIndex)) return;
            CreateRainDropForKey(keyIndex, key, now, isGhost: true);
        }

        public void ReleaseGhostRain(int keyIndex, Key key, long now)
        {
            if (key == null || key.rainList.Count == 0) return;
            for (int i = key.rainList.Count - 1; i >= 0; i--)
            {
                if (key.rainList[i].isGhost)
                {
                    key.rainList[i].Finish(now);
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
                ResetRawRain(r);
                r.SetColor(color);
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
            ResetRawRain(r);
            rawRainPool.Push(r);
        }

        private static void ResetRawRain(RawRain r)
        {
            r.startTime = 0;
            r.releaseTime = 0;
            r.finalSizeY = 0f;
            r.finalSizeComputed = false;
            r.finishSize = false;
            r.finishSizeSetup = false;
            r.sizeOver = false;
            r.removed = false;
            r.isGhost = false;
            r.fading = false;
            r.fadeStartMs = 0;
            r.sizeDelta = null;
            r.anchoredPosition = null;
            r.visibleNear = 0f;
            r.visibleFar = 0f;
            r.rainComponent = null;
        }

        private void CreateRainDropForKey(int keyIndex, Key key, long now, bool isGhost = false)
        {
            if (key == null || key.rain == null) return;

            Sprite rainSprite = null;
            bool isTiled = false;
            RawRain rawRain = GetRawRain(key.color);
            rawRain.startTime = now;
            Rain rainComponent = GetRainFromPool(key.rain.transform, rainSprite, isTiled);
            rainComponent.rawRain = rawRain;
            rawRain.rainComponent = rainComponent;
            rawRain.isGhost = isGhost;
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
            if (IsDmNoteRainMode())
            {
                var host = global::KorenResourcePack.Main.settings;
                return host != null && host.KeyViewerNoteEffect;
            }
            if (keyIndex < 8) return settings.EnableRainForRow1;
            if (keyIndex < 16) return settings.EnableRainForRow2;
            if (keyIndex < 20) return settings.EnableRainForRow3;
            return false;
        }

        private static bool IsDmNoteRainMode()
        {
            var host = global::KorenResourcePack.Main.settings;
            return host != null && string.Equals(host.KeyViewerMode, "dmnote", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
