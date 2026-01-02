#!/usr/bin/env bash
set -euo pipefail

# Jira StopWatch by Komasa - multi-target single-file build script
# Targets: macOS (arm64), Windows (x64), Linux (x64)
# Output: dist/

ROOT_DIR="$(cd "$(dirname "$0")"/.. && pwd)"
PROJECT="$ROOT_DIR/source/StopWatch/StopWatch.csproj"
TFM="net10.0"
DIST_DIR="$ROOT_DIR/dist"
ICONS_DIR="$ROOT_DIR/source/StopWatch/icons"

# Common publish options for single-file, self-contained apps
PUBLISH_OPTS=(
  -c Release
  -p:PublishSingleFile=true
  -p:SelfContained=true
  -p:IncludeAllContentForSelfExtract=true
  -p:IncludeNativeLibrariesForSelfExtract=true
  -p:EnableCompressionInSingleFile=true
  -p:DebugType=None
  -p:DebugSymbols=false
)

rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

echo "Using dotnet: $(dotnet --version)" && echo

# --- macOS icon (.icns) generation from PNG ---
ensure_macos_icns() {
  local png="$ICONS_DIR/stopwatchimg.png"
  local icns="$ICONS_DIR/stopwatch.icns"
  if [[ ! -f "$png" ]]; then
    echo "!! PNG icon not found: $png" >&2
    return 0
  fi
  # Rebuild .icns if missing or older than PNG
  if [[ ! -f "$icns" || "$png" -nt "$icns" ]]; then
    if command -v sips >/dev/null 2>&1 && command -v iconutil >/dev/null 2>&1; then
      local tmpdir
      tmpdir="$(mktemp -d)"
      local set="$tmpdir/stopwatch.iconset"
      mkdir -p "$set"
      # Generate required sizes
      for size in 16 32 64 128 256 512; do
        local dbl=$((size*2))
        sips -z $size $size   "$png" --out "$set/icon_${size}x${size}.png" >/dev/null
        sips -z $dbl  $dbl    "$png" --out "$set/icon_${size}x${size}@2x.png" >/dev/null
      done
      iconutil -c icns "$set" -o "$icns" >/dev/null
      rm -rf "$tmpdir"
      echo "Generated macOS .icns: $icns"
    else
      echo "!! 'sips' and 'iconutil' are required to build .icns. Skipping icon generation." >&2
    fi
  fi
}

# macOS arm64 (produces .app bundle). Kopiujemy .app bez pakowania do ZIP
RID_OSX="osx-arm64"
echo "==> Publishing macOS (arm64)"
ensure_macos_icns
dotnet publish "$PROJECT" "${PUBLISH_OPTS[@]}" -r "$RID_OSX" --self-contained true >/dev/null
OSX_PUB="$ROOT_DIR/source/StopWatch/bin/Release/$TFM/$RID_OSX/publish"
# App bundle name is derived from AssemblyName in csproj (StopWatch.app)
MAC_APP_SRC="$OSX_PUB/StopWatch.app"
MAC_APP_DST="$DIST_DIR/JiraStopWatchByKomasa.app"
if [[ -d "$MAC_APP_SRC" ]]; then
  rm -rf "$MAC_APP_DST"
  if command -v ditto >/dev/null 2>&1; then
    ditto "$MAC_APP_SRC" "$MAC_APP_DST"
  else
    cp -R "$MAC_APP_SRC" "$MAC_APP_DST"
  fi
  # Upewnij się, że plik wykonywalny ma prawa wykonywania
  if [[ -f "$MAC_APP_DST/Contents/MacOS/StopWatch" ]]; then
    chmod +x "$MAC_APP_DST/Contents/MacOS/StopWatch"
  fi
  # Usuń ewentualną flagę kwarantanny i odśwież cache ikon
  if command -v xattr >/dev/null 2>&1; then
    xattr -r -d com.apple.quarantine "$MAC_APP_DST" 2>/dev/null || true
  fi
  /usr/bin/touch "$MAC_APP_DST"
  # Ad-hoc codesign to satisfy Gatekeeper when launching directly
  if command -v codesign >/dev/null 2>&1; then
    codesign --force --deep -s - --timestamp=none "$MAC_APP_DST" >/dev/null || true
  fi
  echo "   -> $MAC_APP_DST"
else
  echo "   !! .app bundle not found in $OSX_PUB" >&2
fi

# Windows x64
RID_WIN="win-x64"
echo "\n==> Publishing Windows (x64)"
dotnet publish "$PROJECT" "${PUBLISH_OPTS[@]}" -r "$RID_WIN" --self-contained true >/dev/null
WIN_PUB="$ROOT_DIR/source/StopWatch/bin/Release/$TFM/$RID_WIN/publish"
WIN_EXE="StopWatch.exe"
if [[ -f "$WIN_PUB/$WIN_EXE" ]]; then
  cp "$WIN_PUB/$WIN_EXE" "$DIST_DIR/JiraStopWatchByKomasa-win-x64.exe"
  echo "   -> $DIST_DIR/JiraStopWatchByKomasa-win-x64.exe"
else
  echo "   !! Windows executable not found in $WIN_PUB" >&2
  exit 1
fi

# Linux x64
RID_LINUX="linux-x64"
echo "\n==> Publishing Linux (x64)"
dotnet publish "$PROJECT" "${PUBLISH_OPTS[@]}" -r "$RID_LINUX" --self-contained true >/dev/null
LIN_PUB="$ROOT_DIR/source/StopWatch/bin/Release/$TFM/$RID_LINUX/publish"
LIN_BIN="StopWatch"
if [[ -f "$LIN_PUB/$LIN_BIN" ]]; then
  cp "$LIN_PUB/$LIN_BIN" "$DIST_DIR/JiraStopWatchByKomasa-linux-x64"
  chmod +x "$DIST_DIR/JiraStopWatchByKomasa-linux-x64"
  echo "   -> $DIST_DIR/JiraStopWatchByKomasa-linux-x64"
else
  echo "   !! Linux binary not found in $LIN_PUB" >&2
  exit 1
fi

echo "\nAll artifacts are in: $DIST_DIR"
