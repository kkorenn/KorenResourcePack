#!/bin/bash
set -e

SRC="/Users/koren/Documents/KorenResourcePack"
GAME="/Users/koren/Library/Application Support/Steam/steamapps/common/A Dance of Fire and Ice"
MANAGED="$GAME/ADanceOfFireAndIce.app/Contents/Resources/Data/Managed"
UMM="$MANAGED/UnityModManager"
DEST="$GAME/Mods/KorenResourcePack"

cd "$SRC"

# Convert any OTFs in Fonts/ to TTF (Unity loads TTF more reliably than CFF-based OTF)
if ls Fonts/*.otf >/dev/null 2>&1; then
  python3 tools/otf2ttf.py
fi

mcs -target:library -out:KorenResourcePack.dll \
  -r:"$MANAGED/Assembly-CSharp.dll" \
  -r:"$MANAGED/Rewired_Core.dll" \
  -r:"$MANAGED/RDTools.dll" \
  -r:"$MANAGED/UnityEngine.dll" \
  -r:"$MANAGED/UnityEngine.CoreModule.dll" \
  -r:"$MANAGED/UnityEngine.IMGUIModule.dll" \
  -r:"$MANAGED/UnityEngine.TextRenderingModule.dll" \
  -r:"$MANAGED/UnityEngine.TextCoreFontEngineModule.dll" \
  -r:"$MANAGED/UnityEngine.TextCoreTextEngineModule.dll" \
  -r:"$MANAGED/UnityEngine.UI.dll" \
  -r:"$MANAGED/UnityEngine.AudioModule.dll" \
  -r:"$MANAGED/UnityEngine.InputLegacyModule.dll" \
  -r:"$MANAGED/netstandard.dll" \
  -r:"$MANAGED/Newtonsoft.Json.dll" \
  -r:"$MANAGED/System.IO.Compression.dll" \
  -r:"$MANAGED/System.IO.Compression.FileSystem.dll" \
  -r:"$UMM/UnityModManager.dll" \
  -r:"$UMM/0Harmony.dll" \
  src/*.cs

mkdir -p "$DEST"
cp Info.json KorenResourcePack.dll "$DEST/"
if [ -d Fonts ]; then
  mkdir -p "$DEST/Fonts"
  find Fonts -type f \( -iname '*.ttf' -o -iname '*.ttc' \) -exec cp {} "$DEST/Fonts/" \;
fi

ZIP="$SRC/KorenResourcePack.zip"
rm -f "$ZIP"
STAGE="$(mktemp -d)/KorenResourcePack"
mkdir -p "$STAGE"
cp Info.json KorenResourcePack.dll "$STAGE/"
if [ -d Fonts ]; then
  mkdir -p "$STAGE/Fonts"
  find Fonts -type f \( -iname '*.ttf' -o -iname '*.ttc' \) -exec cp {} "$STAGE/Fonts/" \;
fi
(cd "$(dirname "$STAGE")" && zip -r "$ZIP" KorenResourcePack >/dev/null)
rm -rf "$(dirname "$STAGE")"

shasum -a 256 KorenResourcePack.dll "$DEST/KorenResourcePack.dll"
echo "Zip: $ZIP"
