# KorenResourcePack-Unity

Unity sub-project for building the AssetBundle that ships with the KorenResourcePack mod.

## Build instructions

1. Open this folder in **Unity Editor 2022.3.62f2** (must match A Dance of Fire and Ice's Unity version).
2. Import the TTF files from `../Fonts/` into `Assets/Font/`.
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
6. Bundles appear in `Assets/AssetBundles/`:
   - `Assets/AssetBundles/korenresourcepackbundle` (Windows)
   - `Assets/AssetBundles/Linux/korenresourcepackbundle`
   - `Assets/AssetBundles/Mac/korenresourcepackbundle`
7. Copy these into the mod folder so `build.sh` picks them up:
   - `KorenResourcePack/Bundles/korenresourcepackbundle`
   - `KorenResourcePack/Bundles/Linux/korenresourcepackbundle`
   - `KorenResourcePack/Bundles/Mac/korenresourcepackbundle`

`build.sh` will then ship the right bundle into the mod's runtime folder.
