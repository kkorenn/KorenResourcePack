
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using KorenResourcePack;

namespace JipperKeyViewer.KeyViewer
{
    public partial class KeyViewer : MonoBehaviour
    {
        void ScanGameFonts()
        {
            var allFonts = Resources.FindObjectsOfTypeAll<Font>();
            if (allFonts == null || allFonts.Length == 0)
                return;

            int added = 0;
            foreach (var font in allFonts)
            {
                bool exists = false;
                foreach (var e in fontList)
                    if (e.sourceFontName == font.name) { exists = true; break; }
                if (exists) continue;

                var tmpFont = TMP_FontAsset.CreateFontAsset(font);
                if (tmpFont != null)
                {
                    var entry = new FontEntry(font.name, tmpFont);
                    entry.sourceFontName = font.name;
                    fontList.Add(entry);
                    added++;
                }
            }

            if (added > 0)
                Main.Mod.Logger.Log($"KeyViewer: Converted {added} traditional font(s) to TMP_FontAsset");
        }

        private bool TryLoadResources()
        {
            if (keyBackgroundSprite != null) return true;

            fontList.Clear();
            shadowMaterials.Clear();

            BundleLoader.EnsureBundleLoaded();
            keyBackgroundSprite = BundleLoader.KeyBackgroundSprite;
            keyOutlineSprite = BundleLoader.KeyOutlineSprite;
            ghostRainSprite = null;

            foreach (string name in BundleLoader.BundleFontDisplayNames())
            {
                if (string.IsNullOrEmpty(name)) continue;
                TMP_FontAsset fa = BundleLoader.GetBundleFont(name);
                if (fa != null && !fontList.Exists(e => e.name == name))
                    fontList.Add(new FontEntry(name, fa));
            }
            ScanGameFonts();
            if (fontList.Count == 0)
            {
                TMP_FontAsset fallback = BundleLoader.GetFallbackTmpFont();
                if (fallback != null) fontList.Add(new FontEntry("Default", fallback));
            }

            LinkFallbackFonts();

            if (Settings.FontIndex >= fontList.Count)
                Settings.FontIndex = 0;

            fontNameIndex = new Dictionary<string, int>(fontList.Count);
            for (int i = 0; i < fontList.Count; i++)
                fontNameIndex[fontList[i].name] = i;

            if (keyBackgroundSprite == null)
                Main.Mod?.Logger?.Log("KeyViewer: KRP bundle key sprites unavailable");

            return keyBackgroundSprite != null;
        }

        private TMP_FontAsset GetCurrentFont()
        {
            string requested = KorenResourcePack.Main.settings != null ? KorenResourcePack.Main.settings.fontName : null;
            return BundleLoader.GetBestTmpFont(requested);
        }

        static readonly Color JkvOutlineColor = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color JkvDropShadowColor = new Color(0f, 0f, 0f, 0.40f);

        internal void ApplyKrpFont(TextMeshProUGUI text)
        {
            if (text == null) return;
            TMP_FontAsset font = GetCurrentFont();
            if (font == null) return;
            text.font = font;
            TmpCompatibility.SetFontSharedMaterial(text, BundleLoader.GetBundleFontMaterial(font));
            TmpCompatibility.TrySetOutline(text, JkvOutlineColor, 0.18f);
            TextShadows.ApplyTmpDropShadow(text, JkvDropShadowColor, 0.45f, -0.45f, 0.22f, 0f);
            TmpCompatibility.RefreshTextRendering(text);
        }

        private void UpdateAllFonts()
        {
            if (GetCurrentFont() == null) return;
            FontStyles style = (FontStyles)Settings.FontStyleFlags;
            void UpdateText(TextMeshProUGUI t)
            {
                if (t == null) return;
                ApplyKrpFont(t);
                t.fontStyle = style;
            }
            if (Keys != null)
            {
                foreach (Key key in Keys)
                {
                    if (key == null) continue;
                    UpdateText(key.text);
                    UpdateText(key.value);
                }
            }
            UpdateText(Kps?.text);
            UpdateText(Kps?.value);
            UpdateText(Total?.text);
            UpdateText(Total?.value);
        }

        Material GetShadowMaterial(TMP_FontAsset font)
        {
            if (font == null) return null;
            if (shadowMaterials.TryGetValue(font, out var mat)) return mat;

            var fontMat = BundleLoader.GetBundleFontMaterial(font) ?? GetFontMaterial(font);
            if (fontMat == null)
            {
                Main.Mod.Logger.Error("KeyViewer: Cannot get material from font asset, skipping shadow");
                return null;
            }
            mat = new Material(fontMat);
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.5f));
            mat.SetFloat("_UnderlayOffsetX", 1f);
            mat.SetFloat("_UnderlayOffsetY", -1f);
            mat.SetFloat("_UnderlaySoftness", 0f);
            shadowMaterials[font] = mat;
            return mat;
        }

        static MemberInfo cachedMaterialMember;
        static bool cachedMaterialLogged;

        static Material GetFontMaterial(TMP_FontAsset font)
        {
            if (cachedMaterialMember == null)
            {
                var t = font.GetType();
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
                cachedMaterialMember = (MemberInfo)t.GetProperty("material", flags) ?? t.GetField("material", flags);
            }

            Material result = null;
            if (cachedMaterialMember is PropertyInfo pi)
            {
                var val = pi.GetValue(font);
                if (val != null) result = (Material)val;
            }
            else if (cachedMaterialMember is FieldInfo fi)
            {
                var val = fi.GetValue(font);
                if (val != null) result = (Material)val;
            }

            if (!cachedMaterialLogged)
            {
                cachedMaterialLogged = true;
                string foundBy = cachedMaterialMember != null
                    ? $"{cachedMaterialMember.MemberType} \"{cachedMaterialMember.Name}\""
                    : "none";
                Main.Mod.Logger.Log($"KeyViewer: Font material resolved via {foundBy}");
            }
            return result;
        }

        static void LinkFallbackFonts()
        {
            FontEntry cjkEntry = null;
            foreach (var e in fontList)
                if (e.name == "CJK (Default)") { cjkEntry = e; break; }
            if (cjkEntry?.font == null) return;

            foreach (var entry in fontList)
            {
                if (entry.font == null || entry == cjkEntry) continue;
                if (entry.font.fallbackFontAssetTable == null)
                    entry.font.fallbackFontAssetTable = new List<TMP_FontAsset>();
                if (!entry.font.fallbackFontAssetTable.Contains(cjkEntry.font))
                    entry.font.fallbackFontAssetTable.Add(cjkEntry.font);
            }
        }

        void ScanCustomFonts()
        {
            string modPath = Path.GetDirectoryName(Main.Mod?.Path) ?? ".";
            string customFontDir = Path.Combine(modPath, "CustomFont");

            if (!Directory.Exists(customFontDir))
            {
                Directory.CreateDirectory(customFontDir);
                Main.Mod.Logger.Log($"KeyViewer: Created CustomFont directory at {customFontDir}");
                return;
            }

            string[] ttfFiles = Directory.GetFiles(customFontDir, "*.ttf", SearchOption.TopDirectoryOnly);
            string[] otfFiles = Directory.GetFiles(customFontDir, "*.otf", SearchOption.TopDirectoryOnly);
            string[] fontFiles = new string[ttfFiles.Length + otfFiles.Length];
            Array.Copy(ttfFiles, fontFiles, ttfFiles.Length);
            Array.Copy(otfFiles, 0, fontFiles, ttfFiles.Length, otfFiles.Length);

            if (fontFiles.Length == 0)
            {
                Main.Mod.Logger.Log($"KeyViewer: No .ttf/.otf files found in CustomFont directory");
                return;
            }

            foreach (string fontPath in fontFiles)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(fontPath);
                    string entryName = $"Custom: {fileName}";

                    bool exists = false;
                    foreach (var e in fontList)
                    {
                        if (e.name.Equals(entryName, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (exists)
                    {
                        Main.Mod.Logger.Log($"KeyViewer: Custom font '{fileName}' already loaded, skipping");
                        continue;
                    }

                    Font font = new Font(fontPath);
                    TMP_FontAsset tmpFont = TMP_FontAsset.CreateFontAsset(font);
                    if (tmpFont != null)
                    {
                        fontList.Add(new FontEntry(entryName, tmpFont));
                    }
                    else
                    {
                        Main.Mod.Logger.Error($"KeyViewer: Failed to create TMP_FontAsset from '{fontPath}'");
                    }
                }
                catch (Exception e)
                {
                    Main.Mod.Logger.Error($"KeyViewer: Failed to load custom font '{fontPath}': {e.Message}");
                }
            }
        }
    }
}
