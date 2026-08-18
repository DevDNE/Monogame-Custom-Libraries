# Pipeline test: a red car

A throwaway asset built to exercise `assets/WORKFLOW.md` end to end. It is not
wired into any game and nothing imports it. It lives under `assets/experiments/`
rather than `assets/sprites/` on purpose — `check-palette-all` scans
`assets/sprites/`, and this art is deliberately not legal against the canonical
palette. Putting it here keeps CI honest instead of forcing the gate open.

```
car.aseprite            authoring source, 48x32, 4 frames, tag "drive"
sprites/car-drive-f*.png conformed exports, gate-clean against palette-car.gpl
palette-car.gpl         canonical 13 + a 5-step red ramp (scoped to this test)
car-drive.gif           6x preview of the drive cycle
```

## What the test was actually for

"Make a red car" turned out to be the most useful possible input, because **the
canonical palette has no red.** Its four ramps are `outline`, `warm` (browns and
tans), `blue`, and a single `accent-rust`. So the exercise stopped being "does
the tooling run" and became "does the gate catch a colour the palette cannot
express" — which is the thing the pipeline exists to do.

## Result 1 — the gate rejects it, correctly

Conforming the unconstrained red car against `assets/palette.gpl`:

| metric | value | verdict |
|---|---|---|
| mean delta | **0.0840** | over the 0.05 "regenerate, don't fix" line |
| max delta | 0.1563 | |
| recoloured | 693 px (100%) | nothing survived untouched |
| palette coverage | 8/13 | |
| `accent-rust` | **214 px, 31% of the sprite** | STYLE.md budgets it at "under 2%" |

That last row is the real failure. The body's red ramp does not collapse onto one
wrong colour, it **scatters across three different ramps** — `warm-0-shadow`,
`warm-2-mid` and `warm-3` — which breaks STYLE.md's "shade within one ramp" rule
outright, and spends a third of the sprite on a colour reserved as a spice.

The output is a brown car with a tan roof and blue wheels. The number said so
before anyone looked at the image, which is the whole point of reading the report
first.

For reference, a clean 5-step red ramp measured against the canonical palette
averages **0.1093** — over twice the threshold, and worse than every Flux
candidate in WORKFLOW.md's calibration table (0.054–0.099).

## Result 2 — the red ramp, and why these reds

`palette-car.gpl` is the canonical 13 rows byte-identical and in their original
order, plus five reds. Hue is held at 351–5°, far enough from the warm ramp
(26–34°) and from `accent-rust` (12°) that no red lands in an existing hue
family; values are spaced 27/43/66/85/96 to clear `accent-rust`'s 47.8.

Checked with the same algorithm `Palette.FindRampCollisions` uses: **0 collisions
across all 18 colours**, and the tightest red pair sits 11.0 apart in value where
the shipped `blue-4`/`blue-5-hilite` pair sits 3.1 apart.

## Result 3 — shipped art conforms at zero

First pass with the red ramp available scored 0.0331 — under threshold, but with
three colours still being forced:

| source | → | lands on | delta |
|---|---|---|---|
| `#96A0AF` steel rim | → | `blue-5-hilite` | 0.0879 |
| `#B4DCFA` glass highlight | → | `warm-5-hilite` | **0.1295** |
| `#555F6E` rim shadow | → | `blue-2-mid` | 0.0942 |

The palette has no neutral grey, so anything steel-coloured gets forced, and a
pale blue highlight jumping to a *warm* cream is a cross-ramp landing.

WORKFLOW.md's own calibration answers this: the hand-authored hero frames scored
**0.0000**, and that — not the 0.05 pass mark — is the bar for art that ships.
So the frames were re-authored directly in palette colours (WORKFLOW.md Step 3,
"fix by hand"), giving **0.0000 mean delta, 0 px recoloured, 11/18 coverage**.

Rims use `blue-3`/`blue-0-shadow`: STYLE.md assigns the blue ramp to "cloth,
metal, water", so blue wheels are on-style rather than a compromise. `blue-4`
was tried first and read as a toy at 1:1.

## The animation

Four frames, 80 ms each, tag `drive`, forward only.

- **The body bobs, the wheels do not.** Body travels `0, -1, -2, -1` px while the
  tyres stay planted on row 31. That reads as suspension travel; bobbing the
  whole sprite reads as the car hopping off the ground.
- **Wheels rotate 45° per frame** via a two-spoke bar. Over four frames it turns
  180°, and a 2-fold-symmetric bar at 180° is back where it started — so the loop
  is seamless with no duplicate frames.
- **One facing, no mirror set.** The car only drives forward (right). Note that
  mirroring it would *not* be free the way it is for the hero: STYLE.md keeps the
  cast horizontally neutral so frames can be reflected, but this car is lit
  neutrally yet shaped asymmetrically, and a mirrored copy would put the
  headlight on the wrong end.

## Reproducing it

```bash
# 1. GENERATE — the off-palette version, standing in for any front-end
python3 gen_car.py raw  /tmp/car-raw      # (script kept with the session, not committed)

# 2. CONFORM — against the canonical palette, to see the rejection
dotnet run --project src/MonoGame.GameFramework.Tools -- conform-sprite \
  --input /tmp/car-raw/car-raw-f1.png --output /tmp/car-canonical-f1.png

# 2b. CONFORM — against the scoped palette, for the real exports
dotnet run --project src/MonoGame.GameFramework.Tools -- conform-sprite \
  --input /tmp/car-legal/car-legal-f1.png \
  --output assets/experiments/car/sprites/car-drive-f1.png \
  --palette assets/experiments/car/palette-car.gpl

# 3. GATE
dotnet run --project src/MonoGame.GameFramework.Tools -- check-palette \
  --project assets/experiments/car --palette assets/experiments/car/palette-car.gpl
```

## If you ever want this in a game

Merge the five `red-*` rows into `assets/palette.gpl` and move the PNGs to a
game's `Content/sprites/`. Hold off until then: that palette is deliberately the
*observed* set of colours in shipped art, and nothing red ships today. Widening
it for an experiment is how an observed palette quietly becomes an aspirational
one.

Two things the linters will not catch on the way in — register the texture with
`TextureFormat=Color` and no power-of-two padding, and pass
`SamplerState.PointClamp` on the `Begin` that draws it. `check-sprites` covers
both once the project has a `TextureImporter` block, but the 48x32 canvas is on
you: it is not the cast's 32x32, so anything assuming a square source frame will
be wrong.
