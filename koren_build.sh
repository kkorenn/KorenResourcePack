#!/bin/bash
set -e

EXTRA_DOTNET_ARGS=()
for arg in "$@"; do
  case "$arg" in
    *=*)
      key="${arg%%=*}"
      val="${arg#*=}"
      case "$key" in
        SRC|GAME|MANAGED|UMM|LEGACY|FORCE_BUNDLE|SKIP_BUNDLE|UNITY_PATH)
          export "$key=$val"
          ;;
        *)
          EXTRA_DOTNET_ARGS+=("$arg")
          ;;
      esac
      ;;
    *)
      EXTRA_DOTNET_ARGS+=("$arg")
      ;;
  esac
done

SRC="${SRC:-/Users/koren/Documents/KorenResourcePack}"
GAME="${GAME:-/Users/koren/Library/Application Support/Steam/steamapps/common/A Dance of Fire and Ice}"
MANAGED="${MANAGED:-$GAME/ADanceOfFireAndIce.app/Contents/Resources/Data/Managed}"
UMM="${UMM:-$MANAGED/UnityModManager}"

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
# existing built bundle - Unity batchmode is slow (~30s+ cold). Override:
#   FORCE_BUNDLE=1 ./koren_build.sh
#   SKIP_BUNDLE=1 ./koren_build.sh
# -----------------------------------------------------------------------------

if [ "${LEGACY:-0}" = "1" ] || [ "${LEGACY:-0}" = "true" ]; then
  UNITY_PROJECT="$SRC/KorenResourcePack-Unity-Legacy"
  BUNDLES_OUT="$SRC/Bundles/Legacy"
else
  UNITY_PROJECT="$SRC/KorenResourcePack-Unity"
  BUNDLES_OUT="$SRC/Bundles"
fi
BUILT="$UNITY_PROJECT/BuiltAssetBundles"
BUNDLE_NAME="korenresourcepackbundle"

if [ "${SKIP_BUNDLE:-0}" = "1" ]; then
  echo "[Bundle] SKIP_BUNDLE=1 -> skipping AssetBundle build."
else
  UNITY_VERSION="$(awk '/m_EditorVersion:/ {print $2}' "$UNITY_PROJECT/ProjectSettings/ProjectVersion.txt" 2>/dev/null || true)"
  UNITY_BIN=""
  if [ -n "$UNITY_VERSION" ] && [ -x "/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity" ]; then
    UNITY_BIN="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity"
  elif [ -n "${UNITY_PATH:-}" ] && [ -x "$UNITY_PATH" ]; then
    UNITY_BIN="$UNITY_PATH"
  else
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
    NEED_BUILD=0
    if [ "${FORCE_BUNDLE:-0}" = "1" ]; then
      NEED_BUILD=1
    elif [ ! -f "$BUNDLES_OUT/Mac/$BUNDLE_NAME" ]; then
      NEED_BUILD=1
    else
      NEWEST=$(find \
        "$UNITY_PROJECT/Assets/Font" \
        "$UNITY_PROJECT/Assets/Keyviewer" \
        "$UNITY_PROJECT/Assets/Editor/CreateAssetBundles.cs" \
        -type f -print0 2>/dev/null | xargs -0 stat -f '%m' 2>/dev/null | sort -n | tail -1)
      BUNDLE_M=$(stat -f '%m' "$BUNDLES_OUT/Mac/$BUNDLE_NAME" 2>/dev/null || echo 0)
      if [ -n "$NEWEST" ] && [ "$NEWEST" -gt "$BUNDLE_M" ]; then
        NEED_BUILD=1
      fi
    fi

    if [ "$NEED_BUILD" = "1" ]; then
      if [ -f "$UNITY_PROJECT/Temp/UnityLockfile" ] && lsof "$UNITY_PROJECT/Temp/UnityLockfile" >/dev/null 2>&1; then
        echo "[Bundle] Unity Editor already has KorenResourcePack-Unity open."
        echo "[Bundle] Close it and re-run, or set SKIP_BUNDLE=1 to use existing Bundles/."
        exit 1
      fi
      echo "[Bundle] Sources changed (or FORCE_BUNDLE=1). Running Unity batchmode..."
      LOG="$SRC/.unity-build.log"
      rm -f "$LOG"
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

      mkdir -p "$BUNDLES_OUT" "$BUNDLES_OUT/Linux" "$BUNDLES_OUT/Mac"
      [ -f "$BUILT/$BUNDLE_NAME" ]       && cp "$BUILT/$BUNDLE_NAME"       "$BUNDLES_OUT/$BUNDLE_NAME"
      [ -f "$BUILT/Linux/$BUNDLE_NAME" ] && cp "$BUILT/Linux/$BUNDLE_NAME" "$BUNDLES_OUT/Linux/$BUNDLE_NAME"
      [ -f "$BUILT/Mac/$BUNDLE_NAME" ]   && cp "$BUILT/Mac/$BUNDLE_NAME"   "$BUNDLES_OUT/Mac/$BUNDLE_NAME"
    else
      echo "[Bundle] Up to date. Skipping Unity build (FORCE_BUNDLE=1 to override)."
    fi
  fi
fi

# -----------------------------------------------------------------------------
# C# compile + stage + zip + deploy via dotnet/MSBuild (KorenResourcePack.csproj).
# Install=true triggers the csproj's Install target which copies the staged
# payload into "$GAME/Mods/KorenResourcePack".
#
# LEGACY=1 builds against pre-3.1.0 / r141 ADOFAI API:
#   LEGACY=1 MANAGED=/path/to/legacy/Managed ./koren_build.sh
#   ./koren_build.sh LEGACY=1 MANAGED=/path/to/legacy/Managed
# -----------------------------------------------------------------------------
DOTNET_ARGS=(-c Release -nologo -p:Install=true -p:Game="$GAME" -p:Managed="$MANAGED" -p:UMM="$UMM")
if [ "${LEGACY:-0}" = "1" ] || [ "${LEGACY:-0}" = "true" ]; then
  echo "[Build] LEGACY=1 -> targeting pre-3.1.0 / r141 game API."
  DOTNET_ARGS+=(-p:Legacy=true)
  OUTPUT_NAME="KorenResourcePack_legacy"
else
  OUTPUT_NAME="KorenResourcePack"
fi

dotnet build "${DOTNET_ARGS[@]}" "${EXTRA_DOTNET_ARGS[@]}"

DIST="$SRC/dist"
DEST="$GAME/Mods/KorenResourcePack"
if [ -f "$DEST/$OUTPUT_NAME.dll" ]; then
  shasum -a 256 "$DIST/$OUTPUT_NAME.dll" "$DEST/$OUTPUT_NAME.dll"
else
  shasum -a 256 "$DIST/$OUTPUT_NAME.dll"
fi
echo "Zip: $DIST/$OUTPUT_NAME.zip"
