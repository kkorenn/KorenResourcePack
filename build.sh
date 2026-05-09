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

# -----------------------------------------------------------------------------
# AssetBundle build (Unity batchmode). Calls CreateAssetBundle.BuildAllAssetBundles
# which produces korenresourcepackbundle for Windows/Linux/Mac under
# KorenResourcePack-Unity/BuiltAssetBundles/{,Linux/,Mac/}, then we mirror those
# into Bundles/.
#
# Skips Unity if every bundle source (sprites + font assets) is older than the
# existing built bundle — Unity batchmode is slow (~30s+ cold). Override:
#   FORCE_BUNDLE=1 ./build.sh
#   SKIP_BUNDLE=1 ./build.sh
# -----------------------------------------------------------------------------

UNITY_PROJECT="$SRC/KorenResourcePack-Unity"
BUILT="$UNITY_PROJECT/BuiltAssetBundles"
BUNDLE_NAME="korenresourcepackbundle"

if [ "${SKIP_BUNDLE:-0}" = "1" ]; then
  echo "[Bundle] SKIP_BUNDLE=1 -> skipping AssetBundle build."
else
  # Locate Unity matching the project's editor version.
  UNITY_VERSION="$(awk '/m_EditorVersion:/ {print $2}' "$UNITY_PROJECT/ProjectSettings/ProjectVersion.txt" 2>/dev/null || true)"
  UNITY_BIN=""
  if [ -n "$UNITY_VERSION" ] && [ -x "/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity" ]; then
    UNITY_BIN="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity"
  elif [ -n "${UNITY_PATH:-}" ] && [ -x "$UNITY_PATH" ]; then
    UNITY_BIN="$UNITY_PATH"
  else
    # Fall back to any Hub install (newest first) if exact version missing.
    if [ -d "/Applications/Unity/Hub/Editor" ]; then
      for d in $(ls -1 /Applications/Unity/Hub/Editor 2>/dev/null | sort -r); do
        if [ -x "/Applications/Unity/Hub/Editor/$d/Unity.app/Contents/MacOS/Unity" ]; then
          UNITY_BIN="/Applications/Unity/Hub/Editor/$d/Unity.app/Contents/MacOS/Unity"
          echo "[Bundle] Project wants $UNITY_VERSION; using $d as a fallback."
          break
        fi
      done
    fi
  fi

  if [ -z "$UNITY_BIN" ]; then
    echo "[Bundle] No Unity Editor found. Set UNITY_PATH=/path/to/Unity or install $UNITY_VERSION via Unity Hub."
    echo "[Bundle] Falling back to existing Bundles/ contents."
  else
    # Decide whether sources changed. Pick newest mtime under Assets/Font + Assets/Keyviewer
    # and the editor script itself, compare against current macOS bundle.
    NEED_BUILD=0
    if [ "${FORCE_BUNDLE:-0}" = "1" ]; then
      NEED_BUILD=1
    elif [ ! -f "$SRC/Bundles/Mac/$BUNDLE_NAME" ]; then
      NEED_BUILD=1
    else
      NEWEST=$(find \
        "$UNITY_PROJECT/Assets/Font" \
        "$UNITY_PROJECT/Assets/Keyviewer" \
        "$UNITY_PROJECT/Assets/Editor/CreateAssetBundles.cs" \
        -type f -print0 2>/dev/null | xargs -0 stat -f '%m' 2>/dev/null | sort -n | tail -1)
      BUNDLE_M=$(stat -f '%m' "$SRC/Bundles/Mac/$BUNDLE_NAME" 2>/dev/null || echo 0)
      if [ -n "$NEWEST" ] && [ "$NEWEST" -gt "$BUNDLE_M" ]; then
        NEED_BUILD=1
      fi
    fi

    if [ "$NEED_BUILD" = "1" ]; then
      # Unity refuses to open a project that the Editor already has open. Detect
      # via the lock file the Editor writes; tell the user to close it.
      if [ -f "$UNITY_PROJECT/Temp/UnityLockfile" ] && lsof "$UNITY_PROJECT/Temp/UnityLockfile" >/dev/null 2>&1; then
        echo "[Bundle] Unity Editor already has KorenResourcePack-Unity open."
        echo "[Bundle] Close it and re-run, or set SKIP_BUNDLE=1 to use existing Bundles/."
        exit 1
      fi
      echo "[Bundle] Sources changed (or FORCE_BUNDLE=1). Running Unity batchmode..."
      LOG="$SRC/.unity-build.log"
      rm -f "$LOG"
      # -nographics avoids needing a display; -batchmode + -quit makes it exit when done.
      # -executeMethod runs the public static method that builds Win/Linux/Mac bundles.
      if "$UNITY_BIN" \
        -batchmode -nographics -quit \
        -projectPath "$UNITY_PROJECT" \
        -executeMethod CreateAssetBundle.BuildAllAssetBundles \
        -logFile "$LOG"; then
        echo "[Bundle] Unity build OK."
      else
        echo "[Bundle] Unity batchmode failed. Tail of log:"
        tail -40 "$LOG" || true
        echo "[Bundle] Continuing with existing Bundles/ if any."
      fi

      mkdir -p "$SRC/Bundles" "$SRC/Bundles/Linux" "$SRC/Bundles/Mac"
      [ -f "$BUILT/$BUNDLE_NAME" ]       && cp "$BUILT/$BUNDLE_NAME"       "$SRC/Bundles/$BUNDLE_NAME"
      [ -f "$BUILT/Linux/$BUNDLE_NAME" ] && cp "$BUILT/Linux/$BUNDLE_NAME" "$SRC/Bundles/Linux/$BUNDLE_NAME"
      [ -f "$BUILT/Mac/$BUNDLE_NAME" ]   && cp "$BUILT/Mac/$BUNDLE_NAME"   "$SRC/Bundles/Mac/$BUNDLE_NAME"
    else
      echo "[Bundle] Up to date. Skipping Unity build (FORCE_BUNDLE=1 to override)."
    fi
  fi
fi

mcs -target:library -out:KorenResourcePack.dll \
  -r:"$MANAGED/Assembly-CSharp.dll" \
  -r:"$MANAGED/Assembly-CSharp-firstpass.dll" \
  -r:"$MANAGED/Rewired_Core.dll" \
  -r:"$MANAGED/RDTools.dll" \
  -r:"$MANAGED/UnityEngine.dll" \
  -r:"$MANAGED/UnityEngine.CoreModule.dll" \
  -r:"$MANAGED/UnityEngine.IMGUIModule.dll" \
  -r:"$MANAGED/UnityEngine.TextRenderingModule.dll" \
  -r:"$MANAGED/UnityEngine.TextCoreFontEngineModule.dll" \
  -r:"$MANAGED/UnityEngine.TextCoreTextEngineModule.dll" \
  -r:"$MANAGED/Unity.TextMeshPro.dll" \
  -r:"$MANAGED/UnityEngine.AssetBundleModule.dll" \
  -r:"$MANAGED/UnityEngine.ImageConversionModule.dll" \
  -r:"$MANAGED/UnityEngine.UIModule.dll" \
  -r:"$MANAGED/UnityEngine.UI.dll" \
  -r:"$MANAGED/UnityEngine.AudioModule.dll" \
  -r:"$MANAGED/UnityEngine.InputLegacyModule.dll" \
  -r:"$MANAGED/UnityEngine.ParticleSystemModule.dll" \
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
if [ -d Bundles ]; then
  mkdir -p "$DEST/Bundles"
  cp Bundles/korenresourcepackbundle "$DEST/Bundles/" 2>/dev/null || true
  if [ -d Bundles/Linux ]; then
    mkdir -p "$DEST/Bundles/Linux"
    cp Bundles/Linux/korenresourcepackbundle "$DEST/Bundles/Linux/" 2>/dev/null || true
  fi
  if [ -d Bundles/Mac ]; then
    mkdir -p "$DEST/Bundles/Mac"
    cp Bundles/Mac/korenresourcepackbundle "$DEST/Bundles/Mac/" 2>/dev/null || true
  fi
fi
# Ship Auto.png (Resource Changer / Otto icon) directly so users see the swap
# without having to rebuild the AssetBundle. BundleLoader.TryLoadSpriteFromDisk
# loads this when the bundle does not yet contain the sprite.
if [ -f "KorenResourcePack-Unity/Assets/Keyviewer/Auto.png" ]; then
  mkdir -p "$DEST/Bundles"
  cp "KorenResourcePack-Unity/Assets/Keyviewer/Auto.png" "$DEST/Bundles/Auto.png"
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
if [ -d Bundles ]; then
  mkdir -p "$STAGE/Bundles"
  cp Bundles/korenresourcepackbundle "$STAGE/Bundles/" 2>/dev/null || true
  if [ -d Bundles/Linux ]; then
    mkdir -p "$STAGE/Bundles/Linux"
    cp Bundles/Linux/korenresourcepackbundle "$STAGE/Bundles/Linux/" 2>/dev/null || true
  fi
  if [ -d Bundles/Mac ]; then
    mkdir -p "$STAGE/Bundles/Mac"
    cp Bundles/Mac/korenresourcepackbundle "$STAGE/Bundles/Mac/" 2>/dev/null || true
  fi
fi
if [ -f "KorenResourcePack-Unity/Assets/Keyviewer/Auto.png" ]; then
  mkdir -p "$STAGE/Bundles"
  cp "KorenResourcePack-Unity/Assets/Keyviewer/Auto.png" "$STAGE/Bundles/Auto.png"
fi
(cd "$(dirname "$STAGE")" && zip -r "$ZIP" KorenResourcePack >/dev/null)
rm -rf "$(dirname "$STAGE")"

shasum -a 256 KorenResourcePack.dll "$DEST/KorenResourcePack.dll"
echo "Zip: $ZIP"
