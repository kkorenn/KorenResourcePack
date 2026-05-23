# Unity Current

Unity sub-project for building the current AssetBundle that ships with the KorenResourcePack mod.

## Build instructions

1. Open this folder in the Unity Editor version from `ProjectSettings/ProjectVersion.txt` (currently **6000.3.10f1**).
2. Add or import TTF files into `Assets/Font/`.
3. For each TTF, generate a TMP_FontAsset:
   - `Window` → `TextMeshPro` → `Font Asset Creator`
   - Source Font File: pick the imported TTF
   - Sampling Point Size: 48 (auto-sizing recommended)
   - Padding: 9
   - Packing Method: Optimum
   - Atlas Resolution: 1024 × 1024
   - Character Set: Custom Range — `0-127,160-255,8194-8364` (basic Latin + punctuation + currency).
     If you also need Korean: append `44032-55203` for Hangul Syllables.
   - Render Mode: SDFAA
   - Click **Generate Font Atlas**, then **Save**, save as `<FontName> SDF.asset` next to the TTF.
4. Select each `*.asset` file and in the Inspector set its AssetBundle name to `korenresourcepackbundle`.
5. Run the menu: `Assets` → `Build Koren Bundle`.
6. Bundles appear in `BuiltAssetBundles/`:
   - `BuiltAssetBundles/korenresourcepackbundle` (Windows)
   - `BuiltAssetBundles/Linux/korenresourcepackbundle`
   - `BuiltAssetBundles/Mac/korenresourcepackbundle`
7. The root build copies those outputs into:
   - `Bundles/korenresourcepackbundle`
   - `Bundles/Linux/korenresourcepackbundle`
   - `Bundles/Mac/korenresourcepackbundle`

Run `./scripts/koren_build.sh` or `dotnet build -c Release` from the repository root to rebuild, stage, and package the matching bundle automatically.
