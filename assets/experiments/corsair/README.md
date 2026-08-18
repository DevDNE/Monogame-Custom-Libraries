# Pipeline test: a corsair

A throwaway asset built to exercise `assets/STYLE.md`'s pipeline end to end
through the **`.pix` front-end**. It is not wired into any game and nothing
imports it. It lives under `assets/experiments/` rather than `assets/sprites/`
for the same reason the car does — the art is deliberately not legal against
the canonical palette, and forcing that gate open to admit it would cost more
than the test is worth.

```
sprites/corsair.pix       the source: a 32x32 character grid, 15 keys
sprites/corsair.png       build artefact, re-rendered by render-pix-all
sprites/corsair-walk.pix  four-frame walk cycle, one 128x32 strip
sprites/corsair-walk.png  build artefact
sprites/palette.gpl       measured from a reference by extract-palette
corsair-preview.png       8x nearest-neighbour preview, for humans
corsair-walk.gif          8x, 130ms/frame preview of the cycle
```

## What the test found

The first attempt passed every gate in the repo and was still wrong, in ways
that were all mechanically measurable and none of which any gate could see:

| | v1, drawn by eye | measured truth | v3, through the pipeline |
|---|---|---|---|
| canvas | 32x40 | **32x32** | 32x32 |
| content | 25x39 — 81% wide | **15x28** — 47% wide | 15x28 — 47% wide |
| colours | 20, invented | **15** | 15, measured |
| soles | row 39 | **row 31** | row 31 |

That is the finding, and it is not about this sprite. **Every gate here checks
legality, not fidelity.** Palette membership, binary alpha, `.pix`/PNG
agreement, texture format, sampler state — v1 passed all of them, because art
and palette invented together always agree with each other. What was missing
was a way to get a reference *in* and a way to measure how far the result
landed *out*, and both gaps were filled by adding four commands rather than by
changing a single gate.

## The four things that were missing

**`describe-image`** answers, in one command, every question v1 guessed:

```
  native grid       32x32  (20x blocks — the file is an integer upscale)
  distinct colours  15
  content bounds    x11..25, y4..31  (15x28)
  canvas fill       47% wide, 88% tall
  soles on last row yes
```

The reference was a 640x640 PNG. So is a 640x640 PNG of 40x40 art, and the eye
cannot tell them apart — but every 20x20 block was one flat colour, which is a
fact and not an opinion. Worth noting what that last block of output says: the
reference is **already on-cast**. 15x28 in 32x32 with soles on the last row is
within a pixel of STYLE.md's "content ~16x27, soles on row 31", which nobody
would have guessed and nobody had to.

**`extract-palette`** builds the `.gpl` from the image instead of from hex
codes read off a screenshot. It also snaps the outline to the shared `#1A1A1A`
spine, so the result is a legal member of the palette family on the way out
rather than something to fix afterwards.

**`trace-pix`** is the direction `.pix` was missing. It could only be authored
into by hand, which silently forced any sprite that began life as an image to
be re-typed from a blank grid — and a blank grid gives you nothing to be wrong
about until you render it. Round-trip is the contract: `render-pix` of a traced
`.pix` reproduces the input byte for byte.

**`compare-sprite`** turns "looks off" into a number, and splits it in two:
*shape IoU* normalises away canvas and scale, *canvas IoU* does not. The gap
between them is itself the diagnosis.

## Result: three attempts, and only the third used the tools

| | shape IoU | canvas IoU | exact px |
|---|---|---|---|
| v1 — freehand, 32x40 canvas | 82.2% | *n/a — canvases differ* | — |
| v2 — freehand, measured canvas | 81.0% | 58.6% | 8.6% |
| v2 — offset swept with `compare-sprite` | 81.0% | **77.8%** | 11.9% |
| v3 — `conform-sprite` + `trace-pix` | **100%** | **100%** | **73.4%** |

The interesting rows are the middle two and the gap to the last.

**v2 taught the tools were working.** Shape IoU barely moved from v1 — 82.2% to
81.0% — because v1 already had a plausible chunky-biped silhouette. What no
freehand pass could reveal is that the sprite sat *three columns left* of where
the reference puts it; sweeping the offset and reading canvas IoU found +3 in
one pass, worth **19 points**. Sweeping further was worse — aligning the
bounding boxes exactly (+5) drops to 70.8%, because two sprites can share a
bounding box and distribute mass differently inside it. Neither number was
available to the eye and the second contradicts the obvious heuristic.

**v3 shows what the freehand loop was costing.** Once the reference is a file,
`conform-sprite --size 32x32` and `trace-pix` produce the grid directly, and
both silhouette metrics go to 100%. Every one of the 84 pixels that still
differs is the same substitution:

```
  #000000 -> #1A1A1A   84 px
```

which is the outline moving onto the shared spine — the one deliberate repaint
in the whole import, and the reason `exact px` reads 73.4% rather than 100%.
Nothing else in the sprite changed.

The honest reading: **two rounds of careful freehand work were worth less than
one command**, and the reason is not talent, it is that the freehand rounds were
working from a mental image while the pipeline works from the file.

## Two bugs the exercise found in its own new tools

Worth recording because both were silent, and both were caught by using the
tools on real input rather than by testing them:

**`extract-palette` picked the wrong outline.** The first rule snapped whatever
fell inside an Oklab radius of `#1A1A1A`. Pure `#000000` — far and away the
commonest outline colour in a real reference — sits **0.2175** away, outside
any radius tight enough to be safe, while the dark red `#500000` sits **0.1239**
*inside* it. So the extractor named a dark red as the silhouette colour and left
the black as a grey ramp step. The fix is darkest-low-chroma-wins, which is
what an outline actually is; `PaletteExtractorTests` pins it.

**A grid row contained a non-ASCII character.** A stray `و` survived a
row-width check, because that check counts characters and the row was still 32
of them. `render-pix` would have caught it as an unkeyed character, but the
lesson is that a width check is not a content check.

## Result: the gates catch, and they catch different things

Carried over from the first run and still true. One pixel of the committed PNG
was changed to a colour **already in the palette**, then both gates run:

| Gate | Verdict | Why |
|---|---|---|
| `check-palette` | **passes** | correct — the pixel is on-palette |
| `check-pix-all` | **fails** | `differs from its .pix in 1 pixel(s); first at 16,25` |

For `.pix`-sourced art `check-pix-all` is load-bearing and `check-palette` is a
formality, which is what `PixDocument`'s header claims and this demonstrates.
Palette conformance is *transitive*: the PNG is re-rendered from the palette on
every check, so it cannot drift off-palette without also drifting from source.

## The palette

15 colours, all measured. Ramps: crimson (3), blush, skin, leather (2), gold,
denim (3), steel, bone (2), plus the spine. **0 ramp collisions.**

One colour is not the reference's: it outlines in `#000000` and every palette
here shares `outline` `#1A1A1A`. `extract-palette` rewrites it and
`conform-sprite` moves the art — 84 px at a 0.2178 Oklab distance, the largest
single change in the import and the only deliberate one.

## What this sprite is, exactly

It is the reference, conformed to a palette measured from itself and traced
into `.pix`. It is **not an original drawing** — the two freehand attempts are
described above and both were discarded. Treat it as a test fixture that
happens to be a faithful reproduction of a recognisable third-party character:
it exercises the pipeline end to end, and it is not a shippable asset. Anything
that ships wants art of its own; what generalises here is the *path*, not the
pixels.

On STYLE.md compliance, it now measures dead on — 32x32, content 15x28 at 47%
wide and 88% tall, soles on row 31 — because the reference did, which is the
part nobody would have guessed and nobody had to.

## Reproducing it

```bash
REF=path/to/reference.png

# 0. MEASURE — before drawing anything
dotnet run --project src/MonoGame.GameFramework.Tools -- describe-image --input $REF

# 1. GENERATE / 2. CONFORM — palette from the source, then art onto the palette
dotnet run --project src/MonoGame.GameFramework.Tools -- extract-palette \
  --input $REF --output sprites/palette.gpl --name Corsair
dotnet run --project src/MonoGame.GameFramework.Tools -- conform-sprite \
  --input $REF --output /tmp/ref.png --size 32x32 --palette sprites/palette.gpl

# 2b. TRACE — into the format you can hand-edit
dotnet run --project src/MonoGame.GameFramework.Tools -- trace-pix \
  --input /tmp/ref.png --output /tmp/ref.pix --palette sprites/palette.gpl

# 3. GATE + 4. COMPARE
dotnet run --project src/MonoGame.GameFramework.Tools -- render-pix --input sprites/corsair.pix
dotnet run --project src/MonoGame.GameFramework.Tools -- check-pix-all
dotnet run --project src/MonoGame.GameFramework.Tools -- check-palettes
dotnet run --project src/MonoGame.GameFramework.Tools -- compare-sprite --a $REF --b sprites/corsair.png
```

## If you ever want this in a game

Copy `sprites/` into the game's `Content/sprites/`, keeping the `.pix` beside
its PNG so `check-pix-all` keeps covering it, and merge the palette into that
game's own rather than adding a twelfth. Then the two things no linter catches
on the way in: register the texture `TextureFormat=Color` with no
power-of-two padding, and pass `SamplerState.PointClamp` on the `Begin` that
draws it — `check-sprites` covers both only once the project has a
`TextureImporter` block.


## The walk cycle

Four frames on one 128x32 strip, contact / passing / contact / passing:

| frame | pose | bbox | row 31 |
|---|---|---|---|
| 0 | step, far foot up | 15x28 | 6 px — near foot only |
| 1 | passing, hips +1 | 15x**29** | 9 px — both feet |
| 2 | step, near foot up | 15x28 | 3 px — far foot only |
| 3 | passing, hips +1 | 15x**29** | 9 px — both feet |

**The feet stay planted and the hips rise.** A walk lifts the hips because the
support leg straightens under them; lifting the whole sprite off the floor
instead reads as hopping. `assets/experiments/car` made the same call for its
suspension — body travels, tyres do not. Here it shows up as the passing frames
being 29 rows tall against the contact frames' 28, with every frame still
putting a sole on row 31.

**The feet move vertically only, and that is a finding, not a shortcut.** Two
horizontal treatments were tried first and both looked broken:

- *Swing the feet apart* (near foot -2, far foot +2). Opened a four-pixel gap
  between the boots, and on the opposite contact the near foot slid straight
  over the far one so the character showed a single foot.
- *Nudge the stepping foot forward 1px* while lifting it. The boot is 3 rows
  tall under a leg 7 columns wide, so a foot shifted even one pixel leaves its
  own leg behind and reads as a detached blob.

At 32x32 there is no room to swing a foot without also redrawing the leg above
it, and the leg is five pixels of trouser. So each step lifts one foot in place;
travel is carried by the bob and the alternation. That is how sprites this size
have always done it, and it took two visibly wrong attempts to re-derive.

### One strip, not four files

The repo has both conventions — the Platformer hero and the car ship separate
`*-f1.png … -f4.png`, the VisualNovel portraits ship one 144x64 strip. For a
`.pix` source the strip wins, because the frames land side by side in the text
and the cycle becomes readable as a diff: the boot rows of all four frames are
one line each, and a change to frame 2 shows up as a change to the middle of
that line rather than as "some PNG moved".

### Testing the cycle

The strip declares `frames 4`, which is what makes it visible to the animation
gate. Two halves, split by what is decidable:

**`mgf-tools check-anim` — mechanical, in CI.** Slices the strip and fails on a
frame identical to its neighbour, a transparent frame, a width that does not
divide, or partial alpha. Transitions wrap, so the loop seam is measured. On
this cycle:

```
  frame  content            opaque  soles
      0  15x28 at (11,4)       315  last row
      1  15x29 at (11,3)       324  last row
      2  15x28 at (11,4)       310  last row
      3  15x29 at (11,3)       324  last row

  transition   changed   region
  0 -> 1          20.3%   x11..25, y3..31
  1 -> 2          21.3%   x11..25, y3..31
  2 -> 3          21.3%   x11..25, y3..31
  3 -> 0          20.3%   x11..25, y3..31  (loop seam)
```

Two things fall out of that table. The passing frames are **29 rows tall
against the contacts' 28** — that is the hip rise, visible as a number. And
every transition costs ~20% of the sprite, because lifting the body shifts
every row: a 1px bob is cheap to draw and expensive as a delta.

Footing is reported, never failed. A walk keeps a sole on the last row; a jump
and an explosion correctly do not, and a gate that fires on those is a gate
people mute.

**`scripts/anim-preview.sh` — visual, on demand.** Builds a throwaway
`.aseprite` and a GIF into a temp directory, for playback and onion skinning.
That half cannot be automated and is exactly what was missing when the two
rejected foot treatments were authored: both looked fine as a static strip.

The `.aseprite` is **not committed, deliberately**. The `.pix` is the source and
the PNG is an artefact `check-pix-all` keeps honest; a committed `.aseprite`
would be a second source able to disagree with the first, with nothing checking
it — the drift the format was adopted to remove.

### This does not unblock `SpriteSheet.Animated`

CLAUDE.md holds the library's animation type deleted "until a second consumer
with a real multi-frame cycle justifies it". **This is not that consumer.** It
is an experiment asset that no game references, and counting it would be
exactly the move that rule exists to prevent — the bar is a game that needs
frame cycling, not a strip that exists.
