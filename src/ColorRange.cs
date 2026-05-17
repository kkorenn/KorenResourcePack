using System;
using System.Collections.Generic;
using UnityEngine;

namespace KorenResourcePack
{
    [Serializable]
    public class ColorRangePoint
    {
        public float Progress;
        public float R;
        public float G;
        public float B;
        public float A = 1f;

        public ColorRangePoint()
        {
        }

        public ColorRangePoint(float progress, Color color)
        {
            Progress = progress;
            SetColor(color);
        }

        public Color ToColor()
        {
            return new Color(R, G, B, A);
        }

        public void SetColor(Color color)
        {
            R = color.r;
            G = color.g;
            B = color.b;
            A = color.a;
        }

        public void Clamp()
        {
            Progress = Mathf.Clamp01(Progress);
            R = Mathf.Clamp01(R);
            G = Mathf.Clamp01(G);
            B = Mathf.Clamp01(B);
            A = Mathf.Clamp01(A);
        }
    }

    [Serializable]
    public class ColorRange
    {
        public List<ColorRangePoint> Points = new List<ColorRangePoint>();
        public bool UsePerfectColor;
        public ColorRangePoint PerfectColor;

        public ColorRange()
        {
        }

        public ColorRange(IEnumerable<ColorRangePoint> points)
        {
            if (points != null) Points.AddRange(points);
            Normalize();
        }

        public ColorRange(IEnumerable<ColorRangePoint> points, Color perfectColor)
            : this(points)
        {
            UsePerfectColor = true;
            PerfectColor = new ColorRangePoint(1f, perfectColor);
        }

        public Color GetColor(float progress)
        {
            Normalize();
            float key = Mathf.Clamp01(progress);
            if (UsePerfectColor && key >= 1f && PerfectColor != null)
                return PerfectColor.ToColor();

            if (Points == null || Points.Count == 0)
                return UsePerfectColor && PerfectColor != null ? PerfectColor.ToColor() : Color.white;

            if (Points.Count == 1)
                return Points[0].ToColor();

            if (key <= Points[0].Progress)
                return Points[0].ToColor();

            int last = Points.Count - 1;
            if (key >= Points[last].Progress)
                return Points[last].ToColor();

            for (int i = 1; i < Points.Count; i++)
            {
                ColorRangePoint high = Points[i];
                if (key > high.Progress) continue;

                ColorRangePoint low = Points[i - 1];
                float span = high.Progress - low.Progress;
                float t = span <= 0.0001f ? 0f : (key - low.Progress) / span;
                return Color.Lerp(low.ToColor(), high.ToColor(), t);
            }

            return Points[last].ToColor();
        }

        public void EnsureDefault(ColorRange defaults)
        {
            if (Points == null) Points = new List<ColorRangePoint>();
            if (Points.Count == 0 && defaults != null)
            {
                Points = ClonePoints(defaults.Points);
            }

            if (defaults != null)
            {
                UsePerfectColor = defaults.UsePerfectColor;
                if (UsePerfectColor && PerfectColor == null && defaults.PerfectColor != null)
                    PerfectColor = new ColorRangePoint(defaults.PerfectColor.Progress, defaults.PerfectColor.ToColor());
            }

            Normalize();
        }

        public ColorRange Clone()
        {
            ColorRange clone = new ColorRange(ClonePoints(Points));
            clone.UsePerfectColor = UsePerfectColor;
            if (PerfectColor != null)
                clone.PerfectColor = new ColorRangePoint(PerfectColor.Progress, PerfectColor.ToColor());
            return clone;
        }

        public void AddPoint(float progress, Color color)
        {
            if (Points == null) Points = new List<ColorRangePoint>();
            Points.Add(new ColorRangePoint(progress, color));
            Normalize();
        }

        public void Normalize()
        {
            if (Points == null) Points = new List<ColorRangePoint>();
            for (int i = 0; i < Points.Count; i++)
            {
                if (Points[i] == null) Points[i] = new ColorRangePoint(1f, Color.white);
                Points[i].Clamp();
            }
            Points.Sort((a, b) => a.Progress.CompareTo(b.Progress));
            if (UsePerfectColor)
            {
                if (PerfectColor == null) PerfectColor = new ColorRangePoint(1f, Color.white);
                PerfectColor.Clamp();
            }
        }

        private static List<ColorRangePoint> ClonePoints(List<ColorRangePoint> points)
        {
            List<ColorRangePoint> clone = new List<ColorRangePoint>();
            if (points == null) return clone;
            for (int i = 0; i < points.Count; i++)
            {
                ColorRangePoint point = points[i];
                if (point != null)
                    clone.Add(new ColorRangePoint(point.Progress, point.ToColor()));
            }
            return clone;
        }
    }
}
