#!/bin/bash
set -e

SRC="/Users/koren/Documents/KorenResourcePack"
GAME="/Users/koren/Library/Application Support/Steam/steamapps/common/A Dance of Fire and Ice"
MANAGED="$GAME/ADanceOfFireAndIce.app/Contents/Resources/Data/Managed"
UMM="$MANAGED/UnityModManager"
DEST="$GAME/Mods/KorenResourcePack"

cd "$SRC"

mcs -target:library -out:KorenResourcePack.dll \
  -r:"$MANAGED/Assembly-CSharp.dll" \
  -r:"$MANAGED/RDTools.dll" \
  -r:"$MANAGED/UnityEngine.dll" \
  -r:"$MANAGED/UnityEngine.CoreModule.dll" \
  -r:"$MANAGED/UnityEngine.IMGUIModule.dll" \
  -r:"$MANAGED/UnityEngine.TextRenderingModule.dll" \
  -r:"$MANAGED/UnityEngine.UI.dll" \
  -r:"$MANAGED/UnityEngine.AudioModule.dll" \
  -r:"$MANAGED/netstandard.dll" \
  -r:"$UMM/UnityModManager.dll" \
  -r:"$UMM/0Harmony.dll" \
  KorenResourcePack.cs

mkdir -p "$DEST"
cp Info.json KorenResourcePack.dll "$DEST/"

ZIP="$SRC/KorenResourcePack.zip"
rm -f "$ZIP"
STAGE="$(mktemp -d)/KorenResourcePack"
mkdir -p "$STAGE"
cp Info.json KorenResourcePack.dll "$STAGE/"
(cd "$(dirname "$STAGE")" && zip -r "$ZIP" KorenResourcePack >/dev/null)
rm -rf "$(dirname "$STAGE")"

shasum -a 256 KorenResourcePack.dll "$DEST/KorenResourcePack.dll"
echo "Zip: $ZIP"
