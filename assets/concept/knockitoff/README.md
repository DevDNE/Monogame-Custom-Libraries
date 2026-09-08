# Knock It Off — concept art

Generator output lands here. **Nothing in this directory is content.**

That distinction is load-bearing, so it is worth stating plainly:

| | `assets/concept/knockitoff/` | `src/MonoGame.GameFramework.KnockItOff/Content/sprites/` |
|---|---|---|
| What it is | 1K–4K smooth renders from a generator | `.pix` sources and the PNGs they render to |
| Colours | thousands, off-palette by definition | palette-legal, checked |
| Alpha | feathered | binary, checked |
| Gates | none apply | all of them apply |
| Compiled by MGCB | never | yes |

A concept PNG dropped into `Content/sprites/` will fail `check-palette-all`
with a wall of violations and, if it somehow does not, will ship as a blurry
off-palette mess that no linter objects to. Keep the boundary.

## Layout

```
palette-key-kitchen.png     01A — founds the kitchen, UI and FX ramps
palette-key-cast.png        01B — founds the six fur ramps and the features
kitchen-backdrop.png        02A
title-backdrop.png          02B
tile-states.png             03 — reference only; the tiles are typed as .pix
nuisances-lineup.png        06A
font-specimen.png           10 — reference only; the font is authored by hand
cats/<name>-sheet.png       04 — 34 identity sheets, five poses each
clips/<name>-<clip>.png     05 — walk, attack, cast, fall-ko, bench
nuisances/                  06B, 06C
items/                      07
ui/                         08
fx/                         09
```

## The pipeline out of here

```
concept/foo.png
  → describe-image      what is actually in this file (native grid, colours)
  → conform-sprite      nearest palette colour in Oklab, binary alpha, resample
  → trace-pix           PNG → .pix, so the polish pass happens in a format that diffs
  → hand polish         conform makes art legal, not good
  → render-pix-all      .pix → the PNG the content pipeline eats
  → check-pix-all       the two can never disagree again
```

Read the **mean delta** `conform-sprite` prints. Under 0.01 means the source
already respected the palette; 0.01–0.05 is normal for palette-aware
generation; over 0.05 means the art is being *forced* onto the palette and
looks it. For this generator, over 0.05 is the expected result and is not a
reason to reject a sheet — reject on identity drift instead, and author the
pixels by hand. See the prompt book's accept lines, and `assets/WORKFLOW.md`.

Full prompts: the Knock It Off prompt book artifact, and `PROMPTS.md` beside
this file for the two that come first.
