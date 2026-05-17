using System;
using System.Collections.Generic;
using System.IO;
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

        // KeyViewer sprites loaded from the bundle. Same naming convention as Jipper:
        // a sliced background fill and a sliced outline border.
        internal static Sprite bundleKeyBackground;
        internal static Sprite bundleKeyOutline;
        // Otto / RDC.auto editor icon replacement (Jipper's "ChangeRabbit" feature).
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
                string path;
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                        path = Path.Combine(bundlesRoot, "korenresourcepackbundle");
                        break;
                    case RuntimePlatform.LinuxPlayer:
                    case RuntimePlatform.LinuxEditor:
                        path = Path.Combine(bundlesRoot, "Linux", "korenresourcepackbundle");
                        break;
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.OSXEditor:
                        path = Path.Combine(bundlesRoot, "Mac", "korenresourcepackbundle");
                        break;
                    default:
                        path = Path.Combine(bundlesRoot, "korenresourcepackbundle");
                        break;
                }

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
                Shader sdfShader = Shader.Find("TextMeshPro/Mobile/Distance Field");
                if (sdfShader == null) sdfShader = Shader.Find("TextMeshPro/Distance Field");
                if (sdfShader == null) sdfShader = Shader.Find("UI/Default");

                foreach (UnityEngine.Object asset in all)
                {
                    TMP_FontAsset fa = asset as TMP_FontAsset;
                    if (fa != null)
                    {
                        // Bundle built against an older Unity/TMP than the game runs.
                        // Material reference can deserialise to null, which makes any
                        // TextMeshProUGUI using this font crash inside
                        // MaterialReference..ctor. Rebuild a working SDF material from
                        // the atlas texture so layout/render works.
                        if (fa.material == null && sdfShader != null)
                        {
                            try
                            {
                                Texture atlasTex = fa.atlasTexture;
                                if (atlasTex == null && fa.atlasTextures != null && fa.atlasTextures.Length > 0)
                                    atlasTex = fa.atlasTextures[0];
                                Material m = new Material(sdfShader) { name = "KRP_TMP_Mat_" + fa.name };
                                if (atlasTex != null) m.SetTexture("_MainTex", atlasTex);
                                fa.material = m;
                                Main.mod?.Logger?.Log("[Bundle] Rebuilt missing TMP material for '" + fa.name + "' (atlas=" + (atlasTex != null ? atlasTex.name : "<null>") + ")");
                            }
                            catch (Exception mex)
                            {
                                Main.mod?.Logger?.Log("[Bundle] Material rebuild failed for '" + fa.name + "': " + mex.Message);
                            }
                        }
                        else if (sdfShader != null && fa.material != null)
                        {
                            fa.material.shader = sdfShader;
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
                        // Match by sprite asset name. Source files live under
                        // KorenResourcePack-Unity/Assets/Keyviewer/{KeyBackground,KeyOutline}.png
                        // and import as Sprite (single) with a 9-slice border for Image.Type.Sliced.
                        if (string.Equals(sp.name, "KeyBackground", StringComparison.OrdinalIgnoreCase))
                            bundleKeyBackground = sp;
                        else if (string.Equals(sp.name, "KeyOutline", StringComparison.OrdinalIgnoreCase))
                            bundleKeyOutline = sp;
                        else if (string.Equals(sp.name, "Auto", StringComparison.OrdinalIgnoreCase))
                            bundleAutoSprite = sp;
                    }
                }

                // Always prefer the on-disk PNG for the Otto/Auto sprite over the bundle copy.
                // The bundle pipeline applies BC/ASTC compression at quality=50, which visibly
                // softens this 512x512 icon when it's drawn at editor scale. Loading the raw
                // PNG into RGBA32 with mipmaps + trilinear filtering keeps the icon crisp at
                // any size. Bundle copy is kept as a last-resort fallback.
                Sprite diskAuto = TryLoadSpriteFromDisk("Auto.png", highQuality: true);
                if (diskAuto != null) bundleAutoSprite = diskAuto;

                // Wire glyph fallbacks (Jipper-style). Some bundled fonts (e.g. JetBrainsMono)
                // do not include the symbols KeyViewer uses for special keys: ⇪ (U+21EA, Caps),
                // ↵ (U+21B5, Return), ␣ (U+2423, Space symbol), ⇧ (U+21E7), ⌘ (U+2318), and so on.
                // TextMeshPro consults `fallbackFontAssetTable` per font for missing glyphs.
                // Jipper does this at load time by appending RDConstants.data.chineseFontTMPro;
                // we additionally append the bundle's Maplestory Bold SDF (which carries those
                // arrow / box symbols), so every other bundled font picks them up too.
                ApplyFontFallbacks();

                // Game runs Unity 6; bundle was built with Unity 2022. TMP material
                // refs survive deserialization spottily — drop unusable entries and
                // substitute the game's own TMP font so labels keep rendering.
                ReplaceBrokenFontsWithGameFont();

                Main.mod?.Logger?.Log("[Bundle] Loaded " + bundleFonts.Count + " font(s): " + string.Join(", ", BundleFontKeysArr())
                    + "; sprites: bg=" + (bundleKeyBackground != null) + " outline=" + (bundleKeyOutline != null)
                    + " auto=" + (bundleAutoSprite != null));
                bundleLoaded = true;
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[Bundle] Load failed: " + ex);
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
            bundle = null;
            bundleFonts.Clear();
            bundleDefaultFont = null;
            bundleKeyBackground = null;
            bundleKeyOutline = null;
            bundleAutoSprite = null;
            bundleLoaded = false;
            bundleFailed = false;
        }

        /// <summary>
        /// For every bundled TMP_FontAsset, append a chain of fallback assets so that any
        /// glyph the primary font is missing falls through to a font that does have it.
        /// Mirrors Jipper's approach (`FontAsset.fallbackFontAssetTable.Add(...)`).
        /// </summary>
        private static void ApplyFontFallbacks()
        {
            try
            {
                List<TMP_FontAsset> fallbacks = new List<TMP_FontAsset>();

                // 1) Bundled Maplestory Bold SDF — has ⇪ ↵ ␣ ⇧ ⇥ ⌘ etc.
                TMP_FontAsset mapleBold;
                if (bundleFonts.TryGetValue("Maplestory Bold", out mapleBold) && mapleBold != null)
                    fallbacks.Add(mapleBold);

                // 2) Game's Chinese TMP font (covers CJK + many symbols). Same source Jipper uses.
                try
                {
                    if (RDConstants.data != null && RDConstants.data.chineseFontTMPro != null)
                        fallbacks.Add(RDConstants.data.chineseFontTMPro);
                }
                catch { /* RDConstants may not be ready in some contexts */ }

                if (fallbacks.Count == 0)
                    return;

                foreach (TMP_FontAsset fa in bundleFonts.Values)
                {
                    if (fa == null) continue;
                    if (fa.fallbackFontAssetTable == null)
                        fa.fallbackFontAssetTable = new List<TMP_FontAsset>();

                    foreach (TMP_FontAsset fb in fallbacks)
                    {
                        // Don't add the font as a fallback for itself, and don't duplicate.
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

        // Detects fonts whose material is still null after the rebuild attempt and
        // replaces them in bundleFonts (+ bundleDefaultFont) with the game's own
        // chineseFontTMPro so the rest of the mod's TMP code keeps working without
        // needing a Unity 6 bundle rebuild.
        private static void ReplaceBrokenFontsWithGameFont()
        {
            try
            {
                TMP_FontAsset gameFont = null;
                try
                {
                    if (RDConstants.data != null) gameFont = RDConstants.data.chineseFontTMPro;
                }
                catch { }

                if (gameFont == null)
                {
                    Main.mod?.Logger?.Log("[Bundle] No game TMP font available for broken-font fallback.");
                    return;
                }

                List<string> broken = new List<string>();
                foreach (KeyValuePair<string, TMP_FontAsset> kv in bundleFonts)
                {
                    TMP_FontAsset fa = kv.Value;
                    if (fa == null || fa.material == null)
                        broken.Add(kv.Key);
                }

                foreach (string name in broken)
                {
                    bundleFonts[name] = gameFont;
                }

                if (bundleDefaultFont == null || bundleDefaultFont.material == null)
                    bundleDefaultFont = gameFont;

                if (broken.Count > 0)
                    Main.mod?.Logger?.Log("[Bundle] Substituted game font for " + broken.Count + " broken bundle font(s): " + string.Join(", ", broken.ToArray()));
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[Bundle] ReplaceBrokenFontsWithGameFont failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Load a PNG from the mod's Bundles/ folder and wrap it as a Sprite. Returns null
        /// if the file does not exist or cannot be decoded. Probe order: Bundles/&lt;name&gt;,
        /// then Bundles/Mac/&lt;name&gt; / Bundles/Linux/&lt;name&gt; (so platform-specific
        /// dirs don't shadow a shared PNG when only built for one platform).
        /// </summary>
        private static Sprite TryLoadSpriteFromDisk(string fileName, bool highQuality = false)
        {
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
                // highQuality = true: enable mipmaps + trilinear + max anisotropic so a 512x512
                // source PNG stays sharp at any UI scale. LoadImage with mipChain=true regenerates
                // the chain after decode. Bilinear-only sampling on a downscaled UI sprite produces
                // the soft/blurry "buns" look the user reported on the Auto icon.
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, highQuality);
                if (!tex.LoadImage(bytes, false))
                {
                    Main.mod?.Logger?.Log("[Bundle] Disk fallback: failed to decode " + path);
                    return null;
                }
                tex.name = Path.GetFileNameWithoutExtension(fileName);
                tex.filterMode = highQuality ? FilterMode.Trilinear : FilterMode.Bilinear;
                tex.anisoLevel = highQuality ? 8 : 1;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.Apply(highQuality, false);

                Sprite sp = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                sp.name = tex.name;
                Main.mod?.Logger?.Log("[Bundle] Disk loaded: " + path + " (" + tex.width + "x" + tex.height + (highQuality ? ", mipmaps+trilinear" : "") + ")");
                return sp;
            }
            catch (Exception ex)
            {
                Main.mod?.Logger?.Log("[Bundle] Disk fallback error for " + fileName + ": " + ex.Message);
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
