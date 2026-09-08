#!/usr/bin/env bash
# Family 01 of the Knock It Off prompt book: turn the two generated palette key
# images into two .gpl candidates, and measure them on the way through.
#
# It deliberately stops short of writing the game's palette. Merging the two
# halves is a judgment call — renaming entries to the design's group names,
# adding the four one-sprite colours no clustering pass can find, and checking
# the `void` ramp's floor — and a script that guessed at it would produce a
# palette nobody had looked at, which is the exact failure the gates cannot see.
#
# Usage: scripts/extract-knockitoff-palette.sh

set -euo pipefail
cd "$(dirname "$0")/.."

C=assets/concept/knockitoff
GAME=src/MonoGame.GameFramework.KnockItOff/Content/sprites/palette.gpl
TOOLS=src/MonoGame.GameFramework.Tools

tools() { dotnet run --project "$TOOLS" -- "$@"; }

missing=0
for f in palette-key-kitchen palette-key-cast; do
  if [ ! -f "$C/$f.png" ]; then
    echo "missing: $C/$f.png"
    missing=1
  fi
done
if [ "$missing" -ne 0 ]; then
  echo
  echo "Run family 01 first. The two prompts are in $C/PROMPTS.md,"
  echo "expanded and ready to paste."
  exit 1
fi

echo "=============================================================="
echo " STEP 0 — measure, before judging anything"
echo "=============================================================="
echo
echo "-- palette-key-kitchen.png"
tools describe-image --input "$C/palette-key-kitchen.png"
echo
echo "-- palette-key-cast.png"
tools describe-image --input "$C/palette-key-cast.png"

echo
echo "=============================================================="
echo " STEP 1 — extract two candidate palettes"
echo "=============================================================="
echo
tools extract-palette --input "$C/palette-key-kitchen.png" \
                      --output /tmp/knockitoff-kitchen.gpl \
                      --name "Retro Kitchen — surfaces" --max-colours 18
echo
tools extract-palette --input "$C/palette-key-cast.png" \
                      --output /tmp/knockitoff-cast.gpl \
                      --name "Retro Kitchen — fur" --max-colours 20

echo
echo "=============================================================="
echo " NEXT — merge them by hand"
echo "=============================================================="
cat <<EOF

  /tmp/knockitoff-kitchen.gpl   surfaces, UI, FX
  /tmp/knockitoff-cast.gpl      the six fur ramps and the features

Merge into:

  $GAME

replacing the scaffolded starter palette wholesale — it is three neutral
ramps that commit to nothing and it says REPLACE THIS at the top.

Four things the merge has to get right:

  1. outline #1A1A1A stays exactly as it is, first in the file.
  2. Rename entries to the design's group names: tile-cream, grout, avocado,
     steel, cabinet-dark, formica-gold, chrome, orange, grey, cream,
     tabby-brown, seal, void, pink, eye-*, cardboard, water, mark-red.
  3. Add by hand the four colours no extraction can find: fae-green, spark,
     tooth-white, frost. Each lands on exactly one sprite or one effect.
  4. void's darkest step must sit >= 3 Oklab L above #1A1A1A. Six cats in the
     roster are black; if void collapses toward the outline they lose their
     silhouettes to their own edges and every gate stays green.

Then:

  dotnet run --project $TOOLS -- check-palettes
  dotnet run --project $TOOLS -- check-palette-all
  dotnet test Game.sln

check-palette-all will flag the two scaffolded sprites: placeholder.png and
ui-frame.png are painted from the starter palette. Expected. Re-key their
.pix sources and render-pix-all, or delete them once real art exists.

EOF
