#!/usr/bin/env bash
# Build, test and smoke-run Reflets de Lignée (Godot 4 .NET) from the command line.
#   ./Scripts/dev.sh build   compile Core, its tests and the Godot game assembly
#   ./Scripts/dev.sh test    run the engine-free Core tests (dotnet test, no Godot needed)
#   ./Scripts/dev.sh smoke   run the game headless with --smoke; fails on any engine or script error
#   ./Scripts/dev.sh all     build + test + smoke
# GODOT_BIN overrides the editor binary (default: Godot 4.7.2 .NET in ~/Godot).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SLN="$ROOT/MirrorLegacy.sln"
TESTS="$ROOT/tests/Core.Tests/MirrorChronicles.Core.Tests.csproj"
GODOT_BIN="${GODOT_BIN:-$HOME/Godot/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64}"

build() {
  dotnet build "$SLN" --nologo
}

run_tests() {
  dotnet test "$TESTS" --nologo
}

smoke() {
  [ -x "$GODOT_BIN" ] || { echo "ERROR: Godot not found at $GODOT_BIN (set GODOT_BIN)" >&2; exit 1; }
  build
  # First run on a fresh clone: import resources so scripts and scenes resolve
  [ -d "$ROOT/game/.godot/imported" ] || "$GODOT_BIN" --headless --path "$ROOT/game" --import >/dev/null 2>&1 || true

  local log
  log="$(mktemp)"
  if ! timeout 120 "$GODOT_BIN" --headless --path "$ROOT/game" -- --smoke >"$log" 2>&1; then
    cat "$log"; rm -f "$log"
    echo "SMOKE FAILED: Godot exited with an error" >&2; exit 1
  fi
  cat "$log"
  if grep -qE "^(ERROR|SCRIPT ERROR|USER ERROR)" "$log"; then
    rm -f "$log"
    echo "SMOKE FAILED: errors reported by the engine" >&2; exit 1
  fi
  rm -f "$log"
  echo "SMOKE OK"
}

# Plays the smoke years in a real window (needs a display) and saves it as PNG.
screenshot() {
  local out="${1:?usage: $0 screenshot <file.png>}"
  [ -x "$GODOT_BIN" ] || { echo "ERROR: Godot not found at $GODOT_BIN (set GODOT_BIN)" >&2; exit 1; }
  build
  timeout 120 "$GODOT_BIN" --path "$ROOT/game" -- --smoke --screenshot="$(realpath -m "$out")"
  [ -f "$out" ] || { echo "SCREENSHOT FAILED: $out was not written" >&2; exit 1; }
  echo "SCREENSHOT $out"
}

case "${1:-}" in
  build)      build ;;
  test)       run_tests ;;
  smoke)      smoke ;;
  screenshot) screenshot "${2:-}" ;;
  all)        build && run_tests && smoke ;;
  *)          echo "usage: $0 build|test|smoke|screenshot <file.png>|all" >&2; exit 2 ;;
esac
