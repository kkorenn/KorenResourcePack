#!/bin/bash
# Dependency check + NuGet prime for KorenResourcePack (macOS / Linux).
# Run once before ./scripts/koren_build.sh.
set -e

SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$SRC"

UNITY_VERSION="$(awk '/m_EditorVersion:/ {print $2}' unity/current/ProjectSettings/ProjectVersion.txt 2>/dev/null || echo "6000.3.10f1")"

MISSING=0
warn() { echo "  [missing] $1"; MISSING=1; }
ok()   { echo "  [ok]      $1"; }

echo "== Required =="
if command -v dotnet >/dev/null 2>&1; then
  ok "dotnet ($(dotnet --version))"
else
  warn "dotnet SDK 6.0+ — install: https://dotnet.microsoft.com/download  (or: brew install --cask dotnet-sdk  /  apt install dotnet-sdk-8.0)"
fi

echo "== Optional (only needed to rebuild AssetBundles) =="
if command -v python3 >/dev/null 2>&1; then
  ok "python3 ($(python3 --version 2>&1))"
else
  warn "python3 — only needed if Unity font assets contain .otf files (auto-converts to .ttf). Install: brew install python  /  apt install python3"
fi

UNITY_HUB_DIR="/Applications/Unity/Hub/Editor"
[ "$(uname)" = "Linux" ] && UNITY_HUB_DIR="$HOME/Unity/Hub/Editor"
if [ -x "$UNITY_HUB_DIR/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity" ] || [ -x "$UNITY_HUB_DIR/$UNITY_VERSION/Editor/Unity" ]; then
  ok "Unity $UNITY_VERSION"
elif [ -d "$UNITY_HUB_DIR" ] && [ -n "$(ls -1 "$UNITY_HUB_DIR" 2>/dev/null)" ]; then
  echo "  [warn]    Unity $UNITY_VERSION not installed (other versions present). Bundles can be rebuilt with fallback Editor, or set SKIP_BUNDLE=1."
else
  warn "Unity $UNITY_VERSION — only needed to rebuild AssetBundles. Install via Unity Hub: https://unity.com/download . Otherwise run with SKIP_BUNDLE=1."
fi

echo "== Game / UMM =="
GAME="${GAME:-$HOME/Library/Application Support/Steam/steamapps/common/A Dance of Fire and Ice}"
[ "$(uname)" = "Linux" ] && GAME="${GAME:-$HOME/.steam/steam/steamapps/common/A Dance of Fire and Ice}"
if [ -d "$GAME" ]; then
  ok "ADOFAI install: $GAME"
else
  warn "ADOFAI install not found. Set GAME=/path/to/game env var, or pass -p:Game=/path to dotnet build."
fi

MANAGED=""
[ -d "$GAME/ADanceOfFireAndIce.app/Contents/Resources/Data/Managed" ] && MANAGED="$GAME/ADanceOfFireAndIce.app/Contents/Resources/Data/Managed"
[ -z "$MANAGED" ] && [ -d "$GAME/A Dance of Fire and Ice_Data/Managed" ] && MANAGED="$GAME/A Dance of Fire and Ice_Data/Managed"
if [ -n "$MANAGED" ] && [ -f "$MANAGED/UnityModManager/UnityModManager.dll" ]; then
  ok "UnityModManager installed"
elif [ -n "$MANAGED" ]; then
  warn "UnityModManager.dll not found in $MANAGED/UnityModManager — install UMM into ADOFAI first: https://www.nexusmods.com/site/mods/21"
fi

echo
if [ "$MISSING" = "1" ]; then
  echo "Some required deps missing. Install them, then re-run ./scripts/setup.sh."
  exit 1
fi

echo "== Prime NuGet packages =="
dotnet restore

echo
echo "Setup complete."
echo "Build normal:         dotnet build -c Release"
echo "Build legacy:         dotnet build -c Release -p:Legacy=true"
echo "Skip bundle:          dotnet build -c Release -p:SkipBundle=true"
echo "Skip bundle + legacy: dotnet build -c Release -p:SkipBundle=true -p:Legacy=true"
echo "Install/deploy:       dotnet build -c Release -p:Install=true"
