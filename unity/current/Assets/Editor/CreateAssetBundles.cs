using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateAssetBundle
{
    const string KorenBundleName = "korenresourcepackbundle";

    /// <summary>Output outside Assets/ so Unity does not import .manifest / bundle binaries into the project (very slow and confusing).</summary>
    static string BuiltBundlesRoot =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BuiltAssetBundles"));

    [MenuItem("Assets/Assign Koren bundle assets")]
    static void MenuAssignBundleNames()
    {
        int fontCount = AssignFontBundleNames();
        int spriteCount = AssignKeyviewerSpriteBundleNames();
        Debug.Log($"[KorenResourcePack] Assigned '{KorenBundleName}' to {fontCount} TMP_FontAsset(s) under Assets/Font and {spriteCount} sprite(s) under Assets/Keyviewer.");
    }

    /// <returns>Number of TMP_FontAsset found under Assets/Font.</returns>
    static int AssignFontBundleNames()
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/Font" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetImporter imp = AssetImporter.GetAtPath(path);
            if (imp != null)
                imp.assetBundleName = KorenBundleName;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return guids.Length;
    }

    /// <summary>
    /// Assign the Koren bundle to KeyViewer sprites (KeyBackground, KeyOutline).
    /// These are imported as sprites by their .meta (textureType: 8 / spriteMode: 1) and sliced via spriteBorder.
    /// </summary>
    /// <returns>Number of sprite assets found under Assets/Keyviewer.</returns>
    static int AssignKeyviewerSpriteBundleNames()
    {
        if (!System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, "Keyviewer")))
            return 0;

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Keyviewer" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetImporter imp = AssetImporter.GetAtPath(path);
            if (imp != null)
                imp.assetBundleName = KorenBundleName;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return guids.Length;
    }

    // public so Unity's -executeMethod CreateAssetBundle.BuildAllAssetBundles works from build.sh.
    [MenuItem("Assets/Build Koren Bundle", false, 0)]
    public static void BuildAllAssetBundles()
    {
        int fontCount = AssignFontBundleNames();
        int spriteCount = AssignKeyviewerSpriteBundleNames();
        if (fontCount == 0 && spriteCount == 0)
        {
            Debug.LogError(
                "[KorenResourcePack] No TMP_FontAsset under Assets/Font and no sprites under Assets/Keyviewer. Nothing will be packed. " +
                "Import TMP font assets and/or KeyBackground/KeyOutline sprites, then use Assets → Assign Koren bundle assets.");
            return;
        }

        string root = BuiltBundlesRoot;
        if (!Directory.Exists(root))
            Directory.CreateDirectory(root);

        int ok = 0;
        if (TryBuildForTarget(root, BuildTarget.StandaloneWindows64, "Windows (→ BuiltAssetBundles/)"))
            ok++;
        string linuxDir = Path.Combine(root, "Linux");
        if (TryBuildForTarget(linuxDir, BuildTarget.StandaloneLinux64, "Linux (→ BuiltAssetBundles/Linux/)"))
            ok++;
        string macDir = Path.Combine(root, "Mac");
        if (TryBuildForTarget(macDir, BuildTarget.StandaloneOSX, "macOS (→ BuiltAssetBundles/Mac/)"))
            ok++;

        if (ok < 3)
        {
            Debug.LogWarning(
                "[KorenResourcePack] Some platforms were skipped or failed. On a Mac, install **Windows Build Support** and **Linux Build Support** " +
                "for this Unity version in Unity Hub (Add modules), then run **Assets → Build Koren Bundle** again. " +
                "Do not use **current platform only** for a release — that menu only builds one OS.");
        }

        Debug.Log(
            "[KorenResourcePack] Done. Copy **BuiltAssetBundles/korenresourcepackbundle** to the mod **Bundles/** folder; " +
            "put Linux/Mac copies under **Bundles/Linux/** and **Bundles/Mac/** (see BundleLoader).");
    }

    [MenuItem("Assets/Build Koren Bundle (current platform only — not for release)", false, 100)]
    static void BuildThisPlatformOnly()
    {
        int fontCount = AssignFontBundleNames();
        int spriteCount = AssignKeyviewerSpriteBundleNames();
        if (fontCount == 0 && spriteCount == 0)
        {
            Debug.LogError("[KorenResourcePack] No TMP_FontAsset under Assets/Font and no sprites under Assets/Keyviewer; aborting.");
            return;
        }

        BuildTarget editorTarget = EditorUserBuildSettings.activeBuildTarget;
        string outDir;
        BuildTarget bundleTarget;

        switch (editorTarget)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                outDir = BuiltBundlesRoot;
                bundleTarget = BuildTarget.StandaloneWindows64;
                break;
            case BuildTarget.StandaloneLinux64:
                outDir = Path.Combine(BuiltBundlesRoot, "Linux");
                bundleTarget = BuildTarget.StandaloneLinux64;
                break;
            case BuildTarget.StandaloneOSX:
                outDir = Path.Combine(BuiltBundlesRoot, "Mac");
                bundleTarget = BuildTarget.StandaloneOSX;
                break;
            default:
                Debug.LogWarning(
                    "[KorenResourcePack] Active build target is " + editorTarget +
                    "; building a macOS bundle into BuiltAssetBundles/Mac. Switch Standalone platform for matching output.");
                outDir = Path.Combine(BuiltBundlesRoot, "Mac");
                bundleTarget = BuildTarget.StandaloneOSX;
                break;
        }

        if (!TryBuildForTarget(outDir, bundleTarget, "current platform (dev)"))
            return;

        Debug.Log(
            "[KorenResourcePack] Dev build only — for Windows/Linux/macOS release bundles use **Assets → Build Koren Bundle** " +
            "after installing Unity Hub build modules for those targets.");
    }

    static bool TryBuildForTarget(string outputDir, BuildTarget target, string label)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
        {
            Debug.LogWarning(
                "[KorenResourcePack] Skipping " + label + ": **" + target + "** is not supported by this editor install. " +
                "Unity Hub → Installs → your " + Application.unityVersion + " editor → **Add modules** → enable **Windows Build Support** " +
                "and/or **Linux Build Support** (Mono is enough for Asset Bundles), then restart Unity.");
            return false;
        }

        try
        {
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            BuildPipeline.BuildAssetBundles(
                outputDir,
                BuildAssetBundleOptions.None,
                target);

            LogBundleOutput(outputDir, label);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[KorenResourcePack] " + label + " build failed: " + ex.Message + "\n" + ex);
            return false;
        }
    }

    static void LogBundleOutput(string dir, string label)
    {
        string bundlePath = Path.Combine(dir, KorenBundleName);
        if (File.Exists(bundlePath))
        {
            long bytes = new FileInfo(bundlePath).Length;
            Debug.Log("[KorenResourcePack] " + label + ": **" + KorenBundleName + "** → " + bundlePath + " (" + bytes + " bytes)");
            return;
        }

        Debug.LogWarning(
            "[KorenResourcePack] " + label + ": expected file **" + KorenBundleName + "** missing under " + dir + ". Files present:");
        if (!Directory.Exists(dir))
            return;
        foreach (string f in Directory.GetFiles(dir))
            Debug.Log("  • " + Path.GetFileName(f));
    }
}
