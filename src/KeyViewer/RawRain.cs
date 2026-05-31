using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Absolute-time rain-drop model, ported from JipperResourcePack (JKV). / 绝对时间雨滴模型
    ///
    /// Position and length are derived from wall-clock timestamps (Stopwatch ms), NOT from
    /// accumulated Time.unscaledDeltaTime. Unity clamps per-frame delta time to
    /// Time.maximumDeltaTime, so a delta-accumulating model under-counts elapsed time during
    /// frame lag and the bar renders too short ("flat"). Driving the geometry from absolute
    /// timestamps makes the bar length track real held time regardless of framerate.
    /// 用绝对时间戳计算雨条，避免卡顿时雨条变扁。
    /// </summary>
    public class RawRain
    {
        public long startTime;        // Stopwatch ms at press (absolute) / 按下时刻
        public long releaseTime;      // Stopwatch ms at release / 释放时刻
        public byte color;
        public float xSize;           // bar width, by color / 雨条宽度
        public float finalSizeY;      // frozen bar length, captured at release / 释放时冻结的长度
        public bool finalSizeComputed;
        public bool finishSize;       // key released (no longer growing) / 已释放
        public bool finishSizeSetup;  // one-time size applied in post-release growing region
        public bool sizeOver;         // bar top reached the height boundary (now scrolling) / 越过高度边界
        public bool removed;
        public bool isGhost;
        public Rain rainComponent;

        // Optional fade-out overlay (KRP feature, kept). Timestamp-driven like the geometry.
        public bool fading;
        public long fadeStartMs;

        // Computed output, applied to the Unity transform on the main thread by RainSystem.
        // 计算结果，由 RainSystem 在主线程写入 transform。
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

        /// <summary>Freeze the bar length at the moment of release / 释放时冻结雨条长度</summary>
        public void Finish(long now)
        {
            if (finishSize) return;
            releaseTime = now;
            finishSize = true;
        }

        /// <summary>
        /// Recompute geometry from absolute time. Returns false once the bar has fully scrolled
        /// off and should be recycled. Mirrors JKV RainManager.Update geometry, parameterized by
        /// per-row speed/height. / 依据绝对时间重算几何；滚出屏幕后返回 false。
        /// </summary>
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
                    // Released bar scrolling past the top: shrink from the bottom as it leaves.
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
                    // Released bar still rising with its frozen length.
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
                    // Held long enough to fill the column: cap at height, stop growing.
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
                    // Still held: grow from the key up to the current y.
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
