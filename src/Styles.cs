using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private static int ScaledFont(int floor, float ratio)
        {
            float mult = (settings != null) ? Mathf.Clamp(settings.size, 0.3f, 3f) : 1f;
            return Mathf.Max(floor, Mathf.RoundToInt(Screen.height * ratio * mult));
        }

        private static void EnsurePercentStyle()
        {
            Font hudFont = GetPreferredHudFont();
            if (percentStyle != null && percentStyle.font == hudFont)
            {
                return;
            }
            percentStyle = null;
            percentShadowStyle = null;
            rightStatusStyle = null;
            rightStatusShadowStyle = null;
            comboValueStyle = null;
            comboValueShadowStyle = null;
            judgementStyle = null;
            judgementShadowStyle = null;
            percentStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                font = hudFont,
                fontSize = 34,
                richText = true,
                normal = { textColor = new Color(1f, 1f, 1f, 0.9f) }
            };

            percentShadowStyle = new GUIStyle(percentStyle);
            percentShadowStyle.richText = true;
            percentShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.28f);

            rightStatusStyle = new GUIStyle(percentStyle);
            rightStatusStyle.alignment = TextAnchor.UpperRight;

            rightStatusShadowStyle = new GUIStyle(percentShadowStyle);
            rightStatusShadowStyle.alignment = TextAnchor.UpperRight;

            comboValueStyle = new GUIStyle(percentStyle);
            comboValueStyle.alignment = TextAnchor.UpperCenter;
            comboValueStyle.normal.textColor = new Color(1f, 1f, 1f, 1f);

            comboValueShadowStyle = new GUIStyle(percentShadowStyle);
            comboValueShadowStyle.alignment = TextAnchor.UpperCenter;

            judgementStyle = new GUIStyle(percentStyle);
            judgementStyle.alignment = TextAnchor.UpperCenter;

            judgementShadowStyle = new GUIStyle(percentShadowStyle);
            judgementShadowStyle.alignment = TextAnchor.UpperCenter;
        }
    }
}
