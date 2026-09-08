#!/usr/bin/env bash
# Headless smoke test — launches each sample game, runs --exit-after N frames,
# fails on any non-zero exit or on wall-clock timeout. Catches crashes that
# the unit-test suite can't see (SpriteFont charset, content-pipeline cache,
# service-resolution, LoadContent throws, etc.).
#
# Usage:   scripts/smoke-all.sh [frames] [timeout_seconds]
# Default: 120 frames, 45s timeout per sample.

set -uo pipefail
cd "$(dirname "$0")/.."

FRAMES="${1:-120}"
TIMEOUT_S="${2:-45}"

PROJECTS=(
  BattleGrid
  Platformer
  Shooter
  Puzzle
  Roguelike
  TowerDefense
  Rhythm
  VisualNovel
  AutoBattler
  KnockItOff
)

echo "Building solution..."
if ! dotnet build Game.sln -c Debug --nologo -v minimal >/dev/null; then
  echo "FAIL: build failed"
  exit 2
fi

FAILED=()
for proj in "${PROJECTS[@]}"; do
  printf '=== %-14s ' "$proj"
  start=$(date +%s)
  # perl provides a reliable cross-platform alarm-based timeout.
  if perl -e 'alarm shift; exec @ARGV' "$TIMEOUT_S" \
         dotnet run --no-build --project "src/MonoGame.GameFramework.$proj/MonoGame.GameFramework.$proj.csproj" \
           -- --exit-after "$FRAMES" >/tmp/smoke-"$proj".log 2>&1; then
    elapsed=$(( $(date +%s) - start ))
    printf 'ok (%ds)\n' "$elapsed"
  else
    rc=$?
    printf 'FAIL (rc=%d)\n' "$rc"
    echo "  --- last 20 log lines ---"
    tail -20 /tmp/smoke-"$proj".log | sed 's/^/  /'
    FAILED+=("$proj")
  fi
done

echo
if [ ${#FAILED[@]} -eq 0 ]; then
  echo "All ${#PROJECTS[@]} samples passed smoke."
  exit 0
else
  echo "Failures: ${FAILED[*]}"
  exit 1
fi
