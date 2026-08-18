#!/usr/bin/env bash
#
# Build a throwaway .aseprite (and a GIF) from an animation strip, so a cycle
# can be watched instead of read.
#
# Why this exists
# ---------------
# mgf-tools check-anim covers what a machine can decide about a cycle: dead
# frames, holes, a width that does not divide, partial alpha. It cannot tell
# you the walk looks wrong. That judgment needs playback and onion skinning,
# which is what Aseprite is for, and the corsair walk needed exactly that --
# two treatments of the feet looked fine as a static strip and were obviously
# broken in motion.
#
# Why the .aseprite goes to a temp directory and NOT beside the .pix
# -----------------------------------------------------------------
# The .pix is the source and the PNG is a build artefact that check-pix-all
# keeps honest. An .aseprite committed alongside would be a *second* source,
# able to disagree with the first, with nothing checking it -- the exact drift
# the .pix format was adopted to remove. So this is a harness: regenerate it
# whenever you want to look, throw it away after.
#
# Usage:
#   scripts/anim-preview.sh <strip.pix>              # frame count read from the file
#   scripts/anim-preview.sh <strip.png> <frame-width>
#
# Requires ASEPRITE_PATH, or aseprite on PATH.

set -euo pipefail

die() { echo "error: $*" >&2; exit 1; }

[[ $# -ge 1 ]] || die "usage: $(basename "$0") <strip.pix|strip.png> [frame-width]"

INPUT="$1"
[[ -f "$INPUT" ]] || die "not found: $INPUT"

ASEPRITE="${ASEPRITE_PATH:-$(command -v aseprite || true)}"
[[ -n "$ASEPRITE" && -x "$ASEPRITE" ]] || die \
  "Aseprite not found. Set ASEPRITE_PATH to the binary, e.g.
    export ASEPRITE_PATH=\"\$HOME/Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite\""

# Resolve the strip PNG and its frame width. A .pix carries both the canvas
# size and the frame count, so it needs no second argument; a bare PNG cannot
# know how many frames it holds and must be told.
if [[ "$INPUT" == *.pix ]]; then
  PNG="${INPUT%.pix}.png"
  [[ -f "$PNG" ]] || die "no PNG beside $INPUT — run: dotnet run --project src/MonoGame.GameFramework.Tools -- render-pix-all"
  WIDTH=$(awk '$1=="size"{print $2}'   "$INPUT" | head -1)
  HEIGHT=$(awk '$1=="size"{print $3}'  "$INPUT" | head -1)
  FRAMES=$(awk '$1=="frames"{print $2}' "$INPUT" | head -1)
  [[ -n "${FRAMES:-}" ]] || die "$INPUT has no 'frames N' directive, so it is a still, not a strip."
  FW=$(( WIDTH / FRAMES ))
else
  PNG="$INPUT"
  [[ $# -ge 2 ]] || die "a PNG needs a frame width: $(basename "$0") $PNG <frame-width>"
  FW="$2"
  read -r WIDTH HEIGHT < <("${ASEPRITE}" -b --script-param probe=1 "$PNG" \
      --script /dev/stdin <<< 'local s=app.activeSprite print(s.width.." "..s.height)' 2>/dev/null) \
    || die "could not read $PNG"
  FRAMES=$(( WIDTH / FW ))
fi

(( FRAMES > 0 )) || die "frame count resolved to $FRAMES"
(( WIDTH % FW == 0 )) || die "strip is ${WIDTH}px wide, not divisible by a frame width of ${FW}"

STEM=$(basename "${PNG%.*}")
OUT=$(mktemp -d "${TMPDIR:-/tmp}/mgf-anim-${STEM}-XXXXXX")
ASE="$OUT/$STEM.aseprite"
GIF="$OUT/$STEM.gif"
DURATION="${ANIM_MS:-130}"

LUA="$OUT/build.lua"
cat > "$LUA" <<LUA
local STRIP, OUT = "$PNG", "$ASE"
local W, H, N = $FW, $HEIGHT, $FRAMES

local src  = app.open(STRIP)
if not src then print("ERROR: cannot open " .. STRIP) return end
local scel = src.layers[1]:cel(1)
local simg, sx, sy = scel.image, scel.position.x, scel.position.y

local spr = Sprite(W, H, ColorMode.RGB)
while #spr.frames < N do spr:newEmptyFrame() end
local layer = spr.layers[1]
layer.name = "$STEM"

for i = 1, N do
  local img = Image(W, H, ColorMode.RGB)
  for y = 0, H - 1 do
    for x = 0, W - 1 do
      -- cel images are cel-local; offset sprite-global coords by cel.position
      local lx, ly = (i - 1) * W + x - sx, y - sy
      if lx >= 0 and ly >= 0 and lx < simg.width and ly < simg.height then
        img:putPixel(x, y, simg:getPixel(lx, ly))
      end
    end
  end
  spr:newCel(layer, i, img, Point(0, 0))
  spr.frames[i].duration = $DURATION / 1000.0
end

local tag = spr:newTag(1, N)
tag.name = "$STEM"

spr:saveAs(OUT)
print("frames=" .. #spr.frames .. " size=" .. spr.width .. "x" .. spr.height)
LUA

echo "building $FRAMES frame(s) of ${FW}x${HEIGHT} from $PNG"
# Aseprite prints the Lua error to stderr and still exits non-zero, so the
# message above this line is the real one. (The MCP wrapper swallows it, which
# is why a failure there means bisecting by hand.)
"$ASEPRITE" -b --script "$LUA" || die "Aseprite script failed; the Lua is at $LUA"
[[ -f "$ASE" ]] || die "Aseprite reported success but wrote no file — verify before trusting, see CLAUDE.md"

"$ASEPRITE" -b "$ASE" --scale 6 --save-as "$GIF" >/dev/null || die "GIF export failed"

echo
echo "  aseprite  $ASE"
echo "  gif       $GIF"
echo
echo "Open the .aseprite and press Enter to play it; F3 toggles onion skinning,"
echo "which is the check a static strip cannot give you."
echo "Throwaway: regenerate rather than edit, the .pix is the source."
