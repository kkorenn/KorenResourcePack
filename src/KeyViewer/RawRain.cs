using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    public class RawRain
    {
        public long startTime;
        public long releaseTime;
        public byte color;
        public float xSize;
        public float finalSizeY;
        public bool finalSizeComputed;
        public bool finishSize;
        public bool finishSizeSetup;
        public bool sizeOver;
        public bool removed;
        public bool isGhost;
        public Rain rainComponent;

        public bool fading;
        public long fadeStartMs;

        public Vector2? sizeDelta;
        public Vector2? anchoredPosition;
        public float visibleNear;
        public float visibleFar;

        public RawRain(byte color)
        {
            SetColor(color);
        }

        public void SetColor(byte c)
        {
            color = c;
            xSize = c switch
            {
                0 => 50,
                3 => 30,
                _ => 40
            };
        }

        public void Finish(long now)
        {
            if (finishSize) return;
            releaseTime = now;
            finishSize = true;
        }

        public bool UpdateLocation(long now, float speedFactor, float height)
        {
            float y = (now - startTime) * speedFactor;

            if (finishSize)
            {
                if (!finalSizeComputed)
                {
                    finalSizeY = (releaseTime - startTime) * speedFactor;
                    finalSizeComputed = true;
                }

                if (y > height)
                {
                    float sizeY = finalSizeY - y + height;
                    if (sizeY < 0) return false;
                    sizeDelta = new Vector2(xSize, sizeY);
                    visibleNear = height - sizeY;
                    visibleFar = height;
                    if (!sizeOver)
                    {
                        anchoredPosition = new Vector2(0, height);
                        sizeOver = true;
                    }
                }
                else
                {
                    anchoredPosition = new Vector2(0, y);
                    visibleNear = y - finalSizeY;
                    visibleFar = y;
                    if (!finishSizeSetup)
                    {
                        sizeDelta = new Vector2(xSize, finalSizeY);
                        finishSizeSetup = true;
                    }
                }
            }
            else
            {
                if (y > height)
                {
                    if (!sizeOver)
                    {
                        sizeDelta = new Vector2(xSize, height);
                        anchoredPosition = new Vector2(0, height);
                        sizeOver = true;
                    }
                    visibleNear = 0f;
                    visibleFar = height;
                }
                else
                {
                    sizeDelta = new Vector2(xSize, y);
                    anchoredPosition = new Vector2(0, y);
                    visibleNear = 0f;
                    visibleFar = y;
                }
            }
            return true;
        }
    }
}
