#!/usr/bin/env bash
set -euo pipefail

# Jira StopWatch by Komasa - multi-target single-file build script
# Targets: macOS (arm64), Windows (x64), Linux (x64)
# Output: dist/

ROOT_DIR="$(cd "$(dirname "$0")"/.. && pwd)"
PROJECT="$ROOT_DIR/source/StopWatch/StopWatch.csproj"
TFM="net10.0"
DIST_DIR="$ROOT_DIR/dist"

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

# macOS arm64 (produces .app bundle). We zip the .app into dist
RID_OSX="osx-arm64"
echo "==> Publishing macOS (arm64)"
dotnet publish "$PROJECT" "${PUBLISH_OPTS[@]}" -r "$RID_OSX" --self-contained true >/dev/null
OSX_PUB="$ROOT_DIR/source/StopWatch/bin/Release/$TFM/$RID_OSX/publish"
# App bundle name is derived from AssemblyName in csproj (StopWatch.app)
if [[ -d "$OSX_PUB/StopWatch.app" ]]; then
  (cd "$OSX_PUB" && zip -qry "$DIST_DIR/JiraStopWatchByKomasa-macos-arm64.zip" "StopWatch.app")
  echo "   -> $DIST_DIR/JiraStopWatchByKomasa-macos-arm64.zip"
else
  # Fallback: zip all publish files
  (cd "$OSX_PUB" && zip -qry "$DIST_DIR/JiraStopWatchByKomasa-macos-arm64.zip" .)
  echo "   -> $DIST_DIR/JiraStopWatchByKomasa-macos-arm64.zip (no .app found, zipped publish folder)"
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
