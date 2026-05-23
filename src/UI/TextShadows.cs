using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KorenResourcePack
{
    internal static class TextShadows
    {
        private static readonly int UnderlayColorId    = Shader.PropertyToID("_UnderlayColor");
        private static readonly int UnderlayOffsetXId  = Shader.PropertyToID("_UnderlayOffsetX");
        private static readonly int UnderlayOffsetYId  = Shader.PropertyToID("_UnderlayOffsetY");
        private static readonly int UnderlaySoftnessId = Shader.PropertyToID("_UnderlaySoftness");
        private static readonly int UnderlayDilateId   = Shader.PropertyToID("_UnderlayDilate");

        internal static void ApplyTmpDropShadow(TextMeshProUGUI text, Color color, float offsetX, float offsetY, float softness, float dilate)
        {
            if (text == null) return;
            ApplyTmpDropShadow(TmpCompatibility.GetFontMaterial(text), color, offsetX, offsetY, softness, dilate);
        }

        internal static void ApplyTmpDropShadow(Material mat, Color color, float offsetX, float offsetY, float softness, float dilate)
        {
            if (mat == null) return;

            mat.EnableKeyword("UNDERLAY_ON");
            SetMaterialColor(mat, UnderlayColorId, color);
            SetMaterialFloat(mat, UnderlayOffsetXId, offsetX);
            SetMaterialFloat(mat, UnderlayOffsetYId, offsetY);
            SetMaterialFloat(mat, UnderlaySoftnessId, softness);
            SetMaterialFloat(mat, UnderlayDilateId, dilate);
        }

        internal static void ScaleTmpDropShadowOffset(TextMeshProUGUI text, float fontPx, float referenceSize, float baseOffsetX, float baseOffsetY, float mult = 1f)
        {
            if (text == null || referenceSize <= 0f) return;
            Material mat = TmpCompatibility.GetFontMaterial(text);
            if (mat == null) return;

            float scale = fontPx / referenceSize;
            SetMaterialFloat(mat, UnderlayOffsetXId, baseOffsetX * scale * mult);
            SetMaterialFloat(mat, UnderlayOffsetYId, baseOffsetY * scale * mult);
        }

        internal static UnityEngine.UI.Shadow EnsureUiShadow(Text text, Color color, Vector2 distance)
        {
            if (text == null) return null;
            UnityEngine.UI.Shadow shadow = text.GetComponent<UnityEngine.UI.Shadow>();
            if (shadow == null)
                shadow = text.gameObject.AddComponent<UnityEngine.UI.Shadow>();

            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
            shadow.enabled = true;
            return shadow;
        }

        private static void SetMaterialFloat(Material mat, int propertyId, float value)
        {
            if (mat == null || !mat.HasProperty(propertyId)) return;
            if (Mathf.Abs(mat.GetFloat(propertyId) - value) > 0.0001f)
                mat.SetFloat(propertyId, value);
        }

        private static void SetMaterialColor(Material mat, int propertyId, Color value)
        {
            if (mat == null || !mat.HasProperty(propertyId)) return;
            if (mat.GetColor(propertyId) != value)
                mat.SetColor(propertyId, value);
        }
    }
}
