using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace KorenResourcePack
{
    internal static class BundleLoader
    {
        internal static AssetBundle bundle;
        internal static readonly Dictionary<string, TMP_FontAsset> bundleFonts =
            new Dictionary<string, TMP_FontAsset>(StringComparer.OrdinalIgnoreCase);
        internal static TMP_FontAsset bundleDefaultFont;
        internal static bool bundleLoaded;
        internal static bool bundleFailed;
        private static readonly List<UnityEngine.Object> localBundleObjects = new List<UnityEngine.Object>();
        private static readonly Dictionary<string, MemberInfo> tmpFontMemberCache = new Dictionary<string, MemberInfo>();
        private const BindingFlags TmpFontMemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        internal static Sprite bundleKeyBackground;
        internal static Sprite bundleKeyOutline;
        
        internal static Sprite bundleAutoSprite;

        internal static bool BundleAvailable => bundleLoaded && !bundleFailed;
        internal static Sprite KeyBackgroundSprite { get { EnsureBundleLoaded(); return bundleKeyBackground; } }
        internal static Sprite KeyOutlineSprite { get { EnsureBundleLoaded(); return bundleKeyOutline; } }
        internal static Sprite AutoSprite { get { EnsureBundleLoaded(); return bundleAutoSprite; } }

        internal static void EnsureBundleLoaded()
        {
            if (bundleLoaded || bundleFailed) return;

            try
            {
                string modPath = Main.mod != null ? Main.mod.Path : null;
                if (string.IsNullOrEmpty(modPath))
                {
                    bundleFailed = true;
                    return;
                }

                string bundlesRoot = Path.Combine(modPath, "Bundles");
                string path = ResolveBundlePath(bundlesRoot);

                if (!File.Exists(path))
                {
                    Main.mod?.Logger?.Log("[Bundle] Bundle missing at " + path + " — TMP overlay disabled, falling back to IMGUI.");
                    bundleFailed = true;
                    return;
                }

                bundle = AssetBundle.LoadFromFile(path);
                if (bundle == null)
                {
                    Main.mod?.Logger?.Log("[Bundle] AssetBundle.LoadFromFile returned null for " + path);
                    bundleFailed = true;
                    return;
                }

                UnityEngine.Object[] all = bundle.LoadAllAssets();
                Shader sdfShader = GetTmpSdfShader();

                foreach (UnityEngine.Object asset in all)
                {
                    TMP_FontAsset fa = asset as TMP_FontAsset;
                    if (fa != null)
                    {
                        
                        Material fontMaterial = GetTmpFontMaterial(fa);
                        Texture atlasTex = GetTmpFontAtlasTexture(fa);
                        if (!IsTmpMaterialUsable(fontMaterial) && sdfShader != null)
                        {
                            try
                            {
                                if (fontMaterial == null)
                                {
                                    Material m = new Material(sdfShader) { name = "KRP_TMP_Mat_" + fa.name };
                                    TrackLocalObject(m);
                                    SetTmpFontMaterial(fa, m);
                                    fontMaterial = GetTmpFontMaterial(fa) ?? m;
                                }
                                else
                                {
                                    fontMaterial.shader = sdfShader;
                                }

                                ConfigureTmpFontMaterial(fontMaterial, atlasTex);
                            }
                            catch (Exception mex)
                            {
                                Main.mod?.Logger?.Log("[Bundle] Material rebuild failed for '" + fa.name + "': " + mex.Message);
                            }
                        }
                        else if (sdfShader != null && fontMaterial != null)
                        {
                            fontMaterial.shader = sdfShader;
                            ConfigureTmpFontMaterial(fontMaterial, atlasTex);
                        }

                        string displayName = StripFontAssetSuffix(fa.name);
                        if (!bundleFonts.ContainsKey(displayName))
                        {
                            bundleFonts[displayName] = fa;
                        }
                        if (bundleDefaultFont == null) bundleDefaultFont = fa;
                        continue;
                    }

                    Sprite sp = asset as Sprite;
                    if (sp != null)
                    {
                        
                        if (string.Equals(sp.name, "KeyBackground", StringComparison.OrdinalIgnoreCase))
                            bundleKeyBackground = sp;
                        else if (string.Equals(sp.name, "KeyOutline", StringComparison.OrdinalIgnoreCase))
                            bundleKeyOutline = sp;
                        else if (string.Equals(sp.name, "Auto", StringComparison.OrdinalIgnoreCase))
                            bundleAutoSprite = sp;
                    }
                }

                Sprite diskAuto = TryLoadSpriteFromDisk("Auto.png", highQuality: true);
                if (diskAuto != null) bundleAutoSprite = diskAuto;

                ApplyFontFallbacks();

                ReplaceBrokenFontsWithGameFont();

                Main.mod?.Logger?.Log("[Bundle] Loaded " + bundleFonts.Count + " font(s): " + string.Join(", ", BundleFontKeysArr())
                    + "; sprites: bg=" + (bundleKeyBackground != null) + " outline=" + (bundleKeyOutline != null)
                    + " auto=" + (bundleAutoSprite != null));
                bundleLoaded = true;
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[Bundle] Load failed: " + ex);
                UnloadBundle();
                bundleFailed = true;
            }
        }

        internal static TMP_FontAsset GetBundleFont(string displayName)
        {
            EnsureBundleLoaded();
            if (!bundleLoaded) return null;
            if (!string.IsNullOrEmpty(displayName) && bundleFonts.TryGetValue(displayName, out TMP_FontAsset fa)) return fa;
            return bundleDefaultFont;
        }

        internal static TMP_FontAsset GetBestTmpFont(string displayName)
        {
            TMP_FontAsset fa = GetBundleFont(displayName);
            if (IsTmpFontUsable(fa)) return fa;

            if (fa != bundleDefaultFont && IsTmpFontUsable(bundleDefaultFont))
                return bundleDefaultFont;

            return GetFallbackTmpFont();
        }

        internal static TMP_FontAsset GetFallbackTmpFont()
        {
            TMP_FontAsset gameFont = GetGameTmpFont();
            if (IsTmpFontUsable(gameFont))
                return gameFont;

            TMP_FontAsset settingsFont = GetTmpSettingsDefaultFont();
            if (IsTmpFontUsable(settingsFont))
                return settingsFont;

            return null;
        }

        internal static Material GetBundleFontMaterial(TMP_FontAsset font)
        {
            return EnsureTmpFontMaterial(font);
        }

        internal static IEnumerable<string> BundleFontDisplayNames()
        {
            EnsureBundleLoaded();
            return bundleFonts.Keys;
        }

        internal static void UnloadBundle()
        {
            try
            {
                if (bundle != null) bundle.Unload(true);
            }
            catch { }
            DestroyLocalObjects();
            bundle = null;
            bundleFonts.Clear();
            bundleDefaultFont = null;
            bundleKeyBackground = null;
            bundleKeyOutline = null;
            bundleAutoSprite = null;
            bundleLoaded = false;
            bundleFailed = false;
        }

        private static void TrackLocalObject(UnityEngine.Object obj)
        {
            if (obj != null) localBundleObjects.Add(obj);
        }

        private static string ResolveBundlePath(string bundlesRoot)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return Path.Combine(bundlesRoot, "Linux", "korenresourcepackbundle");
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return Path.Combine(bundlesRoot, "Mac", "korenresourcepackbundle");
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                default:
                    return Path.Combine(bundlesRoot, "korenresourcepackbundle");
            }
        }

        private static void DestroyLocalObjects()
        {
            for (int i = localBundleObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object obj = localBundleObjects[i];
                if (obj == null) continue;
                try { UnityEngine.Object.Destroy(obj); } catch { }
            }
            localBundleObjects.Clear();
        }

        private static void ApplyFontFallbacks()
        {
            try
            {
                List<TMP_FontAsset> fallbacks = new List<TMP_FontAsset>();

                TMP_FontAsset mapleBold;
                if (bundleFonts.TryGetValue("Maplestory Bold", out mapleBold) && mapleBold != null)
                    fallbacks.Add(mapleBold);

                try
                {
                    if (RDConstants.data != null && RDConstants.data.chineseFontTMPro != null)
                        fallbacks.Add(RDConstants.data.chineseFontTMPro);
                }
                catch {  }

                if (fallbacks.Count == 0)
                    return;

                foreach (TMP_FontAsset fa in bundleFonts.Values)
                {
                    if (fa == null) continue;
                    if (fa.fallbackFontAssetTable == null)
                        fa.fallbackFontAssetTable = new List<TMP_FontAsset>();

                    foreach (TMP_FontAsset fb in fallbacks)
                    {
                        
                        if (fb == null || fb == fa) continue;
                        if (!fa.fallbackFontAssetTable.Contains(fb))
                            fa.fallbackFontAssetTable.Add(fb);
                    }
                }
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[Bundle] ApplyFontFallbacks failed: " + ex.Message);
            }
        }

        private static void ReplaceBrokenFontsWithGameFont()
        {
            try
            {
                TMP_FontAsset gameFont = GetFallbackTmpFont();

                if (gameFont == null)
                {
                    Main.mod?.Logger?.Log("[Bundle] No game TMP font available for broken-font fallback.");
                    return;
                }

                List<string> broken = new List<string>();
                foreach (KeyValuePair<string, TMP_FontAsset> kv in bundleFonts)
                {
                    TMP_FontAsset fa = kv.Value;
                    if (!IsTmpFontUsable(fa))
                        broken.Add(kv.Key);
                }

                foreach (string name in broken)
                {
                    bundleFonts[name] = gameFont;
                }

                if (!IsTmpFontUsable(bundleDefaultFont))
                    bundleDefaultFont = gameFont;

                if (broken.Count > 0)
                    Main.mod?.Logger?.Log("[Bundle] Substituted game font for " + broken.Count + " broken bundle font(s): " + string.Join(", ", broken.ToArray()));
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[Bundle] ReplaceBrokenFontsWithGameFont failed: " + ex.Message);
            }
        }

        private static Material GetTmpFontMaterial(TMP_FontAsset font)
        {
            if (font == null) return null;
            string[] materialMembers = { "material", "m_Material", "m_material" };
            for (int i = 0; i < materialMembers.Length; i++)
            {
                object value;
                if (TryGetTmpFontMemberValue(font, materialMembers[i], out value))
                {
                    Material material = value as Material;
                    if (material != null) return material;
                }
            }

            return null;
        }

        private static void SetTmpFontMaterial(TMP_FontAsset font, Material material)
        {
            if (font == null || material == null) return;
            string[] materialMembers = { "material", "m_Material", "m_material" };
            for (int i = 0; i < materialMembers.Length; i++)
            {
                if (TrySetTmpFontMemberValue(font, materialMembers[i], material))
                    return;
            }
        }

        private static bool IsTmpFontUsable(TMP_FontAsset font)
        {
            return font != null && IsTmpMaterialUsable(EnsureTmpFontMaterial(font));
        }

        private static TMP_FontAsset GetGameTmpFont()
        {
            try
            {
                if (RDConstants.data != null && RDConstants.data.chineseFontTMPro != null)
                    return RDConstants.data.chineseFontTMPro;
            }
            catch
            {
            }
            return null;
        }

        private static TMP_FontAsset GetTmpSettingsDefaultFont()
        {
            try
            {
                PropertyInfo property = typeof(TMP_Settings).GetProperty(
                    "defaultFontAsset",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.GetValue(null, null) as TMP_FontAsset;

                FieldInfo field = typeof(TMP_Settings).GetField(
                    "s_DefaultFontAsset",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null) as TMP_FontAsset;
            }
            catch
            {
            }

            return null;
        }

        private static Material EnsureTmpFontMaterial(TMP_FontAsset font)
        {
            if (font == null) return null;

            Material material = GetTmpFontMaterial(font);
            Texture atlasTexture = GetTmpFontAtlasTexture(font);
            if (material != null)
            {
                if (IsTmpMaterialUsable(material))
                    return material;

                Shader existingSdfShader = GetTmpSdfShader();
                if (existingSdfShader == null)
                    return null;

                material.shader = existingSdfShader;
                ConfigureTmpFontMaterial(material, atlasTexture);
                return material;
            }

            if (atlasTexture == null) return null;

            Shader sdfShader = GetTmpSdfShader();
            if (sdfShader == null) return null;

            try
            {
                Material rebuilt = new Material(sdfShader) { name = "KRP_TMP_Mat_" + font.name };
                TrackLocalObject(rebuilt);
                ConfigureTmpFontMaterial(rebuilt, atlasTexture);
                SetTmpFontMaterial(font, rebuilt);
                return GetTmpFontMaterial(font) ?? rebuilt;
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[Bundle] EnsureTmpFontMaterial failed for '" + font.name + "': " + ex.Message);
                return null;
            }
        }

        private static Shader GetTmpSdfShader()
        {
            Shader sdfShader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (sdfShader == null) sdfShader = Shader.Find("TextMeshPro/Distance Field");
            return sdfShader;
        }

        private static bool IsTmpMaterialUsable(Material material)
        {
            if (material == null || material.shader == null) return false;
            string shaderName = material.shader.name;
            return !string.IsNullOrEmpty(shaderName)
                   && shaderName.StartsWith("TextMeshPro/", StringComparison.OrdinalIgnoreCase);
        }

        private static Texture GetTmpFontAtlasTexture(TMP_FontAsset font)
        {
            if (font == null) return null;

            object value;
            if (TryGetTmpFontMemberValue(font, "atlasTexture", out value))
            {
                Texture texture = value as Texture;
                if (texture != null) return texture;
            }

            string[] textureArrayMembers = { "atlasTextures", "m_AtlasTextures" };
            for (int i = 0; i < textureArrayMembers.Length; i++)
            {
                if (!TryGetTmpFontMemberValue(font, textureArrayMembers[i], out value))
                    continue;

                Array textures = value as Array;
                if (textures == null || textures.Length == 0)
                    continue;

                Texture texture = textures.GetValue(0) as Texture;
                if (texture != null) return texture;
            }

            string[] textureMembers = { "m_AtlasTexture", "atlas", "m_atlas" };
            for (int i = 0; i < textureMembers.Length; i++)
            {
                if (TryGetTmpFontMemberValue(font, textureMembers[i], out value))
                {
                    Texture texture = value as Texture;
                    if (texture != null) return texture;
                }
            }

            return null;
        }

        private static void ConfigureTmpFontMaterial(Material material, Texture atlasTexture)
        {
            if (material == null) return;
            if (atlasTexture != null)
            {
                material.mainTexture = atlasTexture;
                SetMaterialTexture(material, "_MainTex", atlasTexture);
                SetMaterialFloat(material, "_TextureWidth", atlasTexture.width);
                SetMaterialFloat(material, "_TextureHeight", atlasTexture.height);
            }

            SetMaterialColor(material, "_FaceColor", Color.white);
            SetMaterialFloat(material, "_FaceDilate", 0f);
            SetMaterialFloat(material, "_OutlineWidth", 0f);
            SetMaterialFloat(material, "_OutlineSoftness", 0f);
            
            SetMaterialFloat(material, "_GradientScale", 7f);
            SetMaterialFloat(material, "_ScaleX", 1f);
            SetMaterialFloat(material, "_ScaleY", 1f);
            SetMaterialFloat(material, "_PerspectiveFilter", 0.875f);
        }

        private static void SetMaterialTexture(Material material, string propertyName, Texture texture)
        {
            if (material != null && material.HasProperty(propertyName))
                material.SetTexture(propertyName, texture);
        }

        private static void SetMaterialColor(Material material, string propertyName, Color color)
        {
            if (material != null && material.HasProperty(propertyName))
                material.SetColor(propertyName, color);
        }

        private static void SetMaterialFloat(Material material, string propertyName, float value)
        {
            if (material != null && material.HasProperty(propertyName))
                material.SetFloat(propertyName, value);
        }

        private static bool TryGetTmpFontMemberValue(object target, string name, out object value)
        {
            value = null;
            if (target == null || string.IsNullOrEmpty(name)) return false;

            MemberInfo member = GetTmpFontMember(target.GetType(), name);
            if (member == null) return false;

            try
            {
                FieldInfo field = member as FieldInfo;
                if (field != null)
                {
                    value = field.GetValue(target);
                    return true;
                }

                PropertyInfo property = member as PropertyInfo;
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    value = property.GetValue(target, null);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool TrySetTmpFontMemberValue(object target, string name, object value)
        {
            if (target == null || string.IsNullOrEmpty(name)) return false;

            MemberInfo member = GetTmpFontMember(target.GetType(), name);
            if (member == null) return false;

            try
            {
                FieldInfo field = member as FieldInfo;
                if (field != null)
                {
                    field.SetValue(target, value);
                    return true;
                }

                PropertyInfo property = member as PropertyInfo;
                if (property != null && property.CanWrite && property.GetIndexParameters().Length == 0)
                {
                    property.SetValue(target, value, null);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static MemberInfo GetTmpFontMember(Type type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name)) return null;
            string key = type.FullName + "." + name;
            MemberInfo cached;
            if (tmpFontMemberCache.TryGetValue(key, out cached)) return cached;

            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo field = t.GetField(name, TmpFontMemberFlags);
                if (field != null)
                {
                    tmpFontMemberCache[key] = field;
                    return field;
                }

                PropertyInfo property = t.GetProperty(name, TmpFontMemberFlags);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    tmpFontMemberCache[key] = property;
                    return property;
                }
            }

            tmpFontMemberCache[key] = null;
            return null;
        }

        private static Sprite TryLoadSpriteFromDisk(string fileName, bool highQuality = false)
        {
            Texture2D tex = null;
            Sprite sp = null;
            try
            {
                string modPath = Main.mod != null ? Main.mod.Path : null;
                if (string.IsNullOrEmpty(modPath)) return null;
                string bundles = Path.Combine(modPath, "Bundles");
                string[] candidates = new[]
                {
                    Path.Combine(bundles, fileName),
                    Path.Combine(bundles, "Mac", fileName),
                    Path.Combine(bundles, "Linux", fileName),
                };
                string path = null;
                foreach (string c in candidates)
                {
                    if (File.Exists(c)) { path = c; break; }
                }
                if (path == null)
                {
                    Main.mod?.Logger?.Log("[Bundle] Disk fallback: " + fileName + " not found under Bundles/. ResourceChanger feature disabled.");
                    return null;
                }

                byte[] bytes = File.ReadAllBytes(path);
                
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, highQuality);
                if (!tex.LoadImage(bytes, false))
                {
                    Main.mod?.Logger?.Log("[Bundle] Disk fallback: failed to decode " + path);
                    try { UnityEngine.Object.Destroy(tex); } catch { }
                    return null;
                }
                tex.name = Path.GetFileNameWithoutExtension(fileName);
                tex.filterMode = highQuality ? FilterMode.Trilinear : FilterMode.Bilinear;
                tex.anisoLevel = highQuality ? 8 : 1;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.Apply(highQuality, true);

                sp = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                sp.name = tex.name;
                TrackLocalObject(tex);
                TrackLocalObject(sp);
                Main.mod?.Logger?.Log("[Bundle] Disk loaded: " + path + " (" + tex.width + "x" + tex.height + (highQuality ? ", mipmaps+trilinear" : "") + ")");
                return sp;
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[Bundle] Disk fallback error for " + fileName + ": " + ex.Message);
                try { if (sp != null) UnityEngine.Object.Destroy(sp); } catch { }
                try { if (tex != null) UnityEngine.Object.Destroy(tex); } catch { }
                return null;
            }
        }

        private static string StripFontAssetSuffix(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            string n = name;
            string[] suffixes = { " SDF", " Atlas", " TMP" };
            foreach (string s in suffixes)
            {
                if (n.EndsWith(s, StringComparison.OrdinalIgnoreCase))
                {
                    n = n.Substring(0, n.Length - s.Length).TrimEnd();
                    break;
                }
            }
            return n;
        }

        private static string[] BundleFontKeysArr()
        {
            string[] arr = new string[bundleFonts.Count];
            int i = 0;
            foreach (string k in bundleFonts.Keys) arr[i++] = k;
            return arr;
        }
    }
}
