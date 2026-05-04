using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace KorenResourcePack
{
    public static partial class Main
    {
        internal static AssetBundle bundle;
        internal static readonly Dictionary<string, TMP_FontAsset> bundleFonts =
            new Dictionary<string, TMP_FontAsset>(StringComparer.OrdinalIgnoreCase);
        internal static TMP_FontAsset bundleDefaultFont;
        internal static bool bundleLoaded;
        internal static bool bundleFailed;

        internal static bool BundleAvailable => bundleLoaded && !bundleFailed;

        internal static void EnsureBundleLoaded()
        {
            if (bundleLoaded || bundleFailed) return;

            try
            {
                string modPath = mod != null ? mod.Path : null;
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
                    mod?.Logger?.Log("[Bundle] Bundle missing at " + path + " — TMP overlay disabled, falling back to IMGUI.");
                    bundleFailed = true;
                    return;
                }

                bundle = AssetBundle.LoadFromFile(path);
                if (bundle == null)
                {
                    mod?.Logger?.Log("[Bundle] AssetBundle.LoadFromFile returned null for " + path);
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
                    if (fa == null) continue;

                    if (sdfShader != null && fa.material != null)
                    {
                        fa.material.shader = sdfShader;
                    }

                    string displayName = StripFontAssetSuffix(fa.name);
                    if (!bundleFonts.ContainsKey(displayName))
                    {
                        bundleFonts[displayName] = fa;
                    }
                    if (bundleDefaultFont == null) bundleDefaultFont = fa;
                }

                mod?.Logger?.Log("[Bundle] Loaded " + bundleFonts.Count + " font(s): " + string.Join(", ", BundleFontKeysArr()));
                bundleLoaded = true;
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[Bundle] Load failed: " + ex);
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
            bundleLoaded = false;
            bundleFailed = false;
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
