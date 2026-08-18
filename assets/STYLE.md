# Pixel-art style bible

The rules every sprite in this repo follows. Roughly half of them are enforced
by `mgf-tools check-palette` and `check-sprites`; the rest are judgment calls
that no linter can make. Both halves are listed, and which is which is marked,
because a rule nobody checks is a rule that decays.

## The pipeline

```
  1. GENERATE   any front-end: a .pix text grid, hand-drawn in Aseprite,
                PixelLab, Retro Diffusion, a nanobanana concept traced by hand.
                Deliberately unconstrained -- this is the step that should stay
                swappable.
                        |
  2. CONFORM    mgf-tools conform-sprite --input <png> --output <png>
                Nearest-palette-colour match in Oklab, binary alpha, optional
                nearest-neighbour downsample. Mechanical and deterministic.
                (A .pix skips this step: it cannot be off-palette.)
                        |
  3. GATE       mgf-tools check-palette-all   (runs in CI)
                Rejects anything that did not go through step 2.
```

Step 3 is what makes step 1 safe to change. Without the gate, adopting a new
generator is a bet on discipline; with it, the worst case is a red build.

### The `.pix` front-end

Most art in this repo is now written as text: a key mapping single characters
to palette entry names, then a grid of those characters. `render-pix-all`
turns them into the PNGs the content pipeline eats, and `check-pix-all` fails
the build if a committed PNG stops matching its source.

```
name battlegrid-navi
size 32 32
key o outline
key C cyan-2
pixels
..............oo..............
...
```

It earns its place on three properties, not on being pleasant to draw in:

- **It diffs.** `git diff` on a `.png` says a sprite changed. On a `.pix` it
  says *which pixels*.
- **It cannot be off-palette.** Every pixel names a palette entry, so the gate
  has nothing left to catch — for this front-end.
- **Alpha is binary by construction.** `.` is transparent, everything else is
  opaque. There is no syntax for a feathered edge.

It is not a replacement for Aseprite. Hand-polish, onion-skinning and anything
much above 64x64 still want a real editor. The `.pix` files here are the small,
structural, high-repetition art — tiles, icons, UI frames, projectiles — where a
text grid is genuinely the better tool.

## Palette (ENFORCED)

![The nine palettes and the art built from them](./nine-games.png)

*Every palette in the repo, with the cast drawn from it. Regenerate after adding
a game or repainting one — it is the fastest way to see whether nine directions
still read as nine games rather than nine accidents.*

**One palette per game.** `assets/palette.gpl` is the repo-wide default; each
sample carries its own at `Content/sprites/palette.gpl`, and the nearest
palette walking up from a sprite is the one that governs it. Art with no local
palette keeps falling through to the default, which is why this change landed
without touching a committed PNG.

Thirteen colours could not carry nine art directions. That was the whole
problem: nine games built on one library are supposed to look like nine games,
and a shared warm/blue ramp made that impossible.

What holds them together is deliberately thin — **one shared colour**:

| Entry | Value | Rule |
|---|---|---|
| `outline` | `#1A1A1A` | Every palette carries it, unchanged. `check-palettes` fails without it. |

Everything else — hue, ramp count, ramp length, UI chrome — is the game's own
decision, because that is exactly the axis the samples are meant to differ on.
A thicker spine would have to be argued for against each game's direction in
turn, and the loser would fork its own colour anyway.

`assets/palette.gpl` keeps its original four ramps (`outline`, `warm-*` x6,
`blue-*` x5, `accent-rust`) and still governs `assets/sprites/`. The
Platformer's palette is a strict superset of it, so the committed hero frames
never moved.

Two colours in the same hue family at the same **lightness** are not two ramp
steps, they are one step drawn twice — nobody can see the difference but every
file records it, so "the mid blue" becomes a coin flip. `check-palettes`
reports that as a **ramp collision**, and no palette in the repo has one.

Lightness is measured in Oklab, not HSV value. HSV `V` is `max(r,g,b)`, so
every colour with a 255 channel scores exactly 100 however pale it is — it
called `#FF6CBA` and `#FFB0DE` the same brightness. That was harmless with one
palette of mid-tones and became five false positives the moment the games got
bright ramps, at which point the check is training people to ignore its output.

Load a palette in Aseprite via **Palette menu -> Load Palette -> palette.gpl**.
Set the sprite to Indexed colour mode and Aseprite makes it impossible to paint
off-palette in the first place -- much better than fixing it at the gate.

**Shade within one ramp.** A form lit and shadowed should walk up and down a
single ramp, not hop between them. Cross-ramp shading is what makes small
sprites read as muddy.

**Every palette has a spice, and it is rationed.** In `assets/palette.gpl` it
is `accent-rust`, carrying the hero's baldric strap and nothing else -- 6 px
per frame, under 2% of the sprite. The pattern repeats per game:
BattleGrid's `amber-spark` appears only on the frame a hit lands; Roguelike's
`blood-mark` is two pixels of monster eye; Platformer's `accent-rust` is the
beetle's shell, the one hazard on screen. One element, one place. If it starts
showing up everywhere, it stops being an accent.

**Hue is a gameplay channel before it is decoration.** The palettes that work
hardest here are the ones doing a job: BattleGrid gives the two duellists
opposite ends of the wheel because a 3x3 grid gives the player no time to parse
shape; TowerDefense separates buildable from road by hue so the grid needs no
gridlines; Roguelike paints monsters in the one hue the architecture never uses.
Decide what the player must read in a glance, then spend the palette on it.

**Colour alone is not enough where it matters.** Puzzle's six gems are six
different silhouettes as well as six hues, because red/green colour blindness
is roughly 8% of players and those are exactly the two that end up adjacent.
Where a hue distinction is load-bearing, give it a redundant channel.

## Canvas and proportion (PARTLY ENFORCED)

- Character sprites: **32x32 canvas**, feet-anchored to the bottom.
- The hero occupies **16x27** of that canvas -- 50% wide, 84% tall. Keep new
  characters in that neighbourhood so they read as the same cast.
- **Put the soles on row 31.** "Feet-anchored" is literal: `SpriteDestination`
  places the frame's bottom row on `Bounds.Bottom`, which is the floor. Art
  that stops short hovers by exactly that many pixels, and nothing catches it
  -- the build is green, the linters are silent, and the hero just floats. The
  hero this replaces bottomed out on row 29 and hovered 2px for its whole life.
- Collision box and sprite destination may legitimately differ. The Platformer
  hero collides 32x48 and draws 32x32. That is a per-game decision, not a rule
  (see CLAUDE.md).
- **Never scale by a non-integer.** 2x and 3x are fine; 1.5x makes some pixels
  one screen-pixel and others two. `check-sprites` catches the sampler-state
  half of this; the scale factor itself is on you.

## Light (JUDGMENT)

Light comes **from above**, with no strong left/right bias -- measured from the
hero, whose luminance centroid sits 1.3px above its geometric centre and is
horizontally neutral (0.00px on the idle frames, +/-0.02px on the walk pair).

That horizontal neutrality is load-bearing, not decorative: it is what makes
**mirroring a frame legal**. `hero-f4-walk-right` is `hero-f3-walk-left`
reflected, which is only free because no pixel's value depends on which side
of the sprite it sits on. Introduce a left/right bias and every mirrored frame
in the cast has to be redrawn by hand.

If a future sprite wants directional lighting, use **top-left**, and apply it
across the whole set at once. A cast lit from mixed directions is the single
most obvious tell of assembled-from-elsewhere art.

## Outline (JUDGMENT)

Solid `outline` (`#1A1A1A`) around the exterior silhouette. Interior detail is
separated by ramp value, not by more outline -- at 32x32 an interior black line
eats a pixel you cannot spare.

## Alpha (ENFORCED)

**Binary only: 0 or 255.** No feathered edges, ever. Partial alpha is how a
smooth-image generator's output announces itself, and it fights the hard 1px
boundaries pixel art depends on. All four hero frames are currently clean
(0 partial-alpha pixels of 4096) and `check-palette` keeps them that way.

## Translucency (JUDGMENT)

Alpha is binary, so there is no such thing as a 40%-opaque sprite here. When a
layer genuinely needs to show what is behind it, make it partially **absent**
rather than partially transparent: a Bayer dither of opaque pixels over a
transparent field, which is how the hardware this style comes from did it.

Rhythm's lane floor is the worked example. An opaque lane covered the sun the
whole backdrop was drawn for — the board is 400px of a 640px window, dead
centre — so the lane is a ~40% ordered dither instead. It knocks the grid back
far enough for the notes to hold contrast, and the horizon still reads through.

Use an **ordered** matrix, not a hash. A checkerboard leaves visible stripes
wherever the mix crosses 50%, which is exactly where a sky must not have one;
Bayer scatters evenly. Use a hash for *grime* — irregular wear on a floor or
wall — where a matrix would read as polka dots.

## Bringing in art from a generator

1. Generate at whatever size the tool is good at. Do not ask it for 32x32.
2. `conform-sprite --input raw.png --output sprites/thing.png --size 32`
3. Read the conform report. It prints how far each pixel had to move to reach
   the palette. **A large mean delta means the generator fought the palette** --
   the output is being forced, and it will look forced. Regenerate with a
   palette-aware prompt or a style reference instead of accepting it.
4. Hand-fix in Aseprite. Conform gets you legal, not good; silhouette and
   readability at 1:1 are still yours to fix.
5. Commit the `.aseprite` source to `assets/`, the PNG to the game's
   `Content/sprites/`.

## What is deliberately NOT a rule

- **Frame counts and animation timing.** No library animation type exists yet
  (`SpriteSheet.Animated` was removed); the Platformer selects frames by state
  rather than cycling. Revisit when a second consumer needs real cycling.
- **A shared sprite atlas.** Frames are ~350 bytes. Duplicating an export
  across games is cheaper than a content-linking scheme (CLAUDE.md, FINDINGS
  section 1.17).
