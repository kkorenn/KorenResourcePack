using UnityEngine;
using UnityEngine.UI;

namespace JipperKeyViewer.KeyViewer
{
    public class RainGraphic : MaskableGraphic
    {
        private bool useEdgeFade;
        private float dNear;
        private float dFar;
        private float trackHeight;
        private float fadeLength;

        public void SetEdgeFade(bool enabled, float dNear, float dFar, float trackHeight, float fadeLength)
        {
            fadeLength = Mathf.Max(0f, fadeLength);
            if (useEdgeFade == enabled &&
                Mathf.Approximately(this.dNear, dNear) &&
                Mathf.Approximately(this.dFar, dFar) &&
                Mathf.Approximately(this.trackHeight, trackHeight) &&
                Mathf.Approximately(this.fadeLength, fadeLength))
                return;

            useEdgeFade = enabled;
            this.dNear = dNear;
            this.dFar = dFar;
            this.trackHeight = trackHeight;
            this.fadeLength = fadeLength;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            Color full = color;
            float span = dFar - dNear;
            if (!useEdgeFade || fadeLength <= 0.5f || trackHeight <= 0.5f || span <= 0.0001f)
            {
                AddQuad(vh, rect.xMin, rect.yMin, rect.xMax, rect.yMax, full, full, full, full);
                return;
            }

            float fadeStartD = trackHeight - fadeLength;
            float nearAlpha = AlphaAtDistance(dNear, fadeStartD, trackHeight, fadeLength);
            float farAlpha = AlphaAtDistance(dFar, fadeStartD, trackHeight, fadeLength);
            Color nearColor = full;
            Color farColor = full;
            nearColor.a *= nearAlpha;
            farColor.a *= farAlpha;

            bool crossesFadeStart = dNear < fadeStartD && dFar > fadeStartD;
            if (!crossesFadeStart)
            {
                AddQuad(vh, rect.xMin, rect.yMin, rect.xMax, rect.yMax, nearColor, nearColor, farColor, farColor);
                return;
            }

            float t = (fadeStartD - dNear) / span;
            float yMid = rect.yMin + t * rect.height;
            AddQuad(vh, rect.xMin, rect.yMin, rect.xMax, yMid, nearColor, nearColor, full, full);
            AddQuad(vh, rect.xMin, yMid, rect.xMax, rect.yMax, full, full, farColor, farColor);
        }

        private static float AlphaAtDistance(float d, float fadeStartD, float trackHeight, float fadeLength)
        {
            if (d <= fadeStartD) return 1f;
            if (d >= trackHeight) return 0f;
            return (trackHeight - d) / fadeLength;
        }

        private static void AddQuad(VertexHelper vh, float xMin, float yMin, float xMax, float yMax,
            Color bottomLeft, Color bottomRight, Color topRight, Color topLeft)
        {
            int index = vh.currentVertCount;
            vh.AddVert(new Vector3(xMin, yMin), bottomLeft, new Vector2(0f, 0f));
            vh.AddVert(new Vector3(xMax, yMin), bottomRight, new Vector2(1f, 0f));
            vh.AddVert(new Vector3(xMax, yMax), topRight, new Vector2(1f, 1f));
            vh.AddVert(new Vector3(xMin, yMax), topLeft, new Vector2(0f, 1f));
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
        }
    }

    public class Rain : MonoBehaviour
    {
        public RainGraphic image;
        public new RectTransform transform;
        public RawRain rawRain;

        private void Awake()
        {
            transform = GetComponent<RectTransform>();
            image = gameObject.AddComponent<RainGraphic>();
            image.raycastTarget = false;
        }

        public void Init(Transform parent, Sprite sprite = null, bool isTiled = false)
        {
            gameObject.SetActive(true);
            transform.SetParent(parent);
            transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 1);
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = Vector2.zero;
            transform.localScale = Vector3.one;
            image.SetEdgeFade(false, 0f, 0f, 0f, 0f);
        }
    }
}
