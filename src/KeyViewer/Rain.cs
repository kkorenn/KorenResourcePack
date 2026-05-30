using UnityEngine;
using UnityEngine.UI;

namespace JipperKeyViewer.KeyViewer
{
    public class Rain : MonoBehaviour
    {
        public Image image;
        public new RectTransform transform;
        public RawRain rawRain;
        public bool fadingOut;
        public float fadeTimer;

        private void Awake()
        {
            transform = GetComponent<RectTransform>();
            image = gameObject.AddComponent<Image>();
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
            fadingOut = false;
            fadeTimer = 0f;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = isTiled ? Image.Type.Tiled : Image.Type.Simple;
            }
            else
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
            }
        }

        public void StartFadeOut(float duration)
        {
            fadingOut = true;
            fadeTimer = 0f;
        }
    }
}
