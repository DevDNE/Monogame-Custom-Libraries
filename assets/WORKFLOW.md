# Worked example: an AI-generated sprite, end to end

> **Status, 2026-08-16.** The task below — give the Platformer's rectangle
> enemy a real sprite — is **done**, and so are the equivalents in the other
> eight games. It was done through a fourth track this document did not
> anticipate: `.pix`, a text grid rendered to PNG by `mgf-tools render-pix`.
> See `assets/STYLE.md`.
>
> That does not make this document stale, and it is worth being precise about
> why. `.pix` is good at small structural art — tiles, icons, UI frames,
> projectiles, anything with repetition or exact geometry. It is *bad* at
> organic form: a face, a creature with weight, anything you would want to
> gesture at rather than specify. The tracks below remain the route for those,
> and the pipeline they plug into is unchanged.
>
> Two things did change and matter to any generator run:
>
> * **Palettes are per game now.** The prompt block in Step 0 is
>   `assets/palette.gpl`, which still governs `assets/sprites/`. For a sprite
>   destined for a specific game, regenerate the block from *that game's*
>   `Content/sprites/palette.gpl` using the snippet in Step 0.
> * **`conform-sprite` picks the palette the same way everything else does** —
>   nearest `palette.gpl` walking up from the input file — so run it with the
>   output path inside the target game and it resolves correctly on its own.

The task: the Platformer's patrolling enemy drew as a coloured rectangle.
Give it a real 32x32 sprite that looks like it belongs next to the hero.

Three generation tracks are shown. They share the same spine, and the spine is
the point — the generator is the swappable part.

```
  PROMPT (palette-aware)  ->  GENERATE  ->  CONFORM  ->  JUDGE  ->  FIX  ->  GATE
                              track A/B/C   mechanical   accept/   Aseprite  CI
                                                         reject
```

---

## Step 0 — The palette block

Paste this into any generator prompt. Every track uses it; it is the single
highest-leverage thing you can put in a prompt, because it moves palette
discipline from "hope" to "instruction".

```
Strictly limit the image to these 13 colours and no others:
#1A1A1A (outline), #3A2410 #6B4423 #A87A3D #E0A77B #F5C396 #FFE0B8 (warm ramp:
skin, leather, hair), #1F3F73 #2A5DA0 #3D7EC8 #5A9CE0 #6FB0E8 (blue ramp: cloth,
metal), #7A3A2A (rust accent, use sparingly).
Light comes from directly above. Solid #1A1A1A outline around the exterior
silhouette only. Hard edges, no anti-aliasing, no gradients, no partial
transparency. Transparent background.
```

Regenerate it any time the palette changes — and point it at the palette of
the game the sprite is for, not at the repo default:

```bash
# assets/palette.gpl for authoring sources; swap the path for a game's own,
# e.g. src/MonoGame.GameFramework.Roguelike/Content/sprites/palette.gpl
python3 -c "
import sys
rows=[l.split() for l in open(sys.argv[1])
      if l.strip() and not l.startswith(('#','GIMP','Name:','Columns:'))]
print(', '.join('#%02X%02X%02X (%s)'%(int(r[0]),int(r[1]),int(r[2]),' '.join(r[3:]))
                for r in rows))" assets/palette.gpl
```

---

## Track A — Nano Banana Pro (Gemini 3 Pro Image)

**What it is actually good for here: character identity, not pixels.** It will
not give you a real 32x32 grid — you get a ~1K smooth render with thousands of
colours and feathered edges. What it *is* unusually good at is keeping one
character recognisably itself across multiple poses and angles, which is the
axis no linter and no palette can enforce.

So use it as the concept stage:

> A hostile forest creature, side view, standing. Stocky, wide stance, big
> readable silhouette, small head. Flat cel shading, thick dark outline, no
> texture detail. Character sheet: idle, walking, and lunging, same character
> in all three, side-on, evenly lit.
>
> *(+ the palette block from Step 0)*

Then either:

- **Trace it.** Open the render as a reference layer in Aseprite at 32x32 and
  draw over it. Highest quality, and at this size it is genuinely fast.
- **Conform it** (Step 1) and treat the output as a rough block-in to repaint.

Ask for a **flat background and chunky shapes**. The more detail it puts in,
the less survives a 32x reduction. Do not ask it for "pixel art" — you will get
a picture *of* pixel art, with a fake grid that does not align to anything.

## Track B — Retro Diffusion

**The best single-tool fit if your priority is "matches the look I already
have."** It runs a pixel-art-native model rather than downsampling a smooth
image, and it ships palette locking and text-guided palette creation, which is
exactly the constraint this pipeline cares about.

Two ways in:

- **Aseprite extension** — generate directly into the canvas your `.aseprite`
  source already lives in. Load `assets/palette.gpl` first
  (Palette -> Load Palette) and use the palette lock so output is constrained
  at generation time rather than corrected afterwards.
- **API** — for scripting a batch. Same conform + gate steps apply; nothing
  downstream changes.

Prompt in its idiom — short, subject-first, style keywords — plus the palette
block. Expect a **much lower mean delta** than Track A, because the model is
producing palette-legal pixels rather than being forced into them.

## Track C — Local LoRA (Draw Things or ComfyUI)

Free, offline, on your Apple Silicon machine. **Draw Things** is the lower
friction of the two on macOS; ComfyUI gives more control.

Honest constraint: a LoRA trained on *your* style is the ideal, and you cannot
train one yet. That needs roughly 20-50 consistent sprites; you have one
character in four frames. Training on that would just teach the model to
reproduce the hero.

### What actually works here — tested 2026-08-12 on an M2 Pro / 16GB

**Base FLUX.2 Klein 4B, no LoRA.** Everything below is measured on this
machine, not quoted from a model card.

```bash
python3 -m venv ~/.venvs/flux
~/.venvs/flux/bin/pip install torch diffusers transformers accelerate safetensors peft
```

```python
from diffusers import Flux2KleinPipeline
import torch
pipe = Flux2KleinPipeline.from_pretrained(
    "black-forest-labs/FLUX.2-klein-4B", torch_dtype=torch.bfloat16)
pipe.enable_model_cpu_offload(device="mps")     # keeps peak ~8GB, fits 16GB
img = pipe(prompt=PROMPT, num_inference_steps=20, guidance_scale=4.0,
           height=512, width=512,
           generator=torch.Generator("cpu").manual_seed(42)).images[0]
```

- `black-forest-labs/FLUX.2-klein-4B` is **ungated and Apache-2.0**. ~16GB
  download (transformer 7.75GB + text encoder 8.05GB + VAE).
- **180s per 512x512 image.** Slow, entirely usable at the volume a sprite set
  needs.
- `prompt` is **not** the first positional argument — `image` is. Pass
  `prompt=` by keyword or you get "Provide either `prompt` or `prompt_embeds`".
- fp32 OOMs (4B in fp32 is ~16GB for the transformer alone). bf16 works.

A prompt that produced a usable creature sprite first try:

> 16-bit pixel art sprite of a hostile forest creature, side view, full body,
> standing, retro SNES game sprite, large chunky pixels, hard aliased edges,
> flat cel shading, thick black outline, limited 13 colour palette, plain solid
> flat background, no gradients, no anti-aliasing

**Flux emits no alpha.** The background comes back opaque, and conform will
map it to a palette colour — one run had the grey background eat 640 of 1024
pixels as `blue-5-hilite`. Colour-key or flood-fill the background to
transparent *before* conform; doing so moved mean delta 0.1037 -> 0.0724.

**Do not use [Limbicnation/pixel-art-lora](https://huggingface.co/Limbicnation/pixel-art-lora).**
Its 172 tensors load cleanly in correct diffusers layout, and it still produces
a black image — mean brightness 0.1 at LoRA scale 1.0, 3.8 at scale 0.5, at
both 4 and 20 steps. The base model with the same prompt and settings produces
a real image, so the adapter is numerically wrong for this checkpoint rather
than misformatted. Untested but plausible alternative:
[Pixel Art Spritesheet 4-Walk Small](https://civitai.com/models/2356302/pixel-art-spritesheet-4-walk-small),
which is 32x32-native and Apache-2.0, but wants Klein **base** rather than the
distilled weights above — a second ~16GB pull.

One more measurement worth keeping: block-uniformity on Flux output was 16.1%
at 8x8 and 4.9% at 16x16. Even when the image looks like pixel art, it is not
on a grid. 512/32 = 16, so conform is doing real work, not cosmetic work.

### Considered and passed over

| Model | Why not |
|---|---|
| [evilsocket/alucard](https://huggingface.co/evilsocket/alucard) | **Tested 2026-08-12 — does not work.** On paper the best fit here: 32M params, 128x128 RGBA, and reference-frame conditioning for cross-frame character consistency that nothing else offers natively. In practice it emits noise. Across 20/50/100 ODE steps, cfg_text 1.0-12.0, and both CLIP variants, output was ~12,000 unique colours per 128x128 with **zero fully-transparent pixels** and mean alpha ~115 — i.e. the network emits near-zero values, the signature of an undertrained model. Installs and runs fine (41s load, 5.9s/sprite on CPU); the weights load cleanly. It is v0.1.0 and simply is not trained yet. Recheck if it gets a real release. |
| [thomaseding/pixelnet](https://huggingface.co/thomaseding/pixelnet) | Right idea, wrong maturity. A ControlNet fed a **checkerboard** to force logical pixel placement — attacking grid alignment at generation time instead of post-hoc. But it is experimental, SD-based, still needs post-processing, and its author says it performs poorly at small grids. |
| [2D Pixel Toolkit](https://civitai.com/models/165876/2d-pixel-toolkit-2d) | Fine SD 1.5 fallback, aimed at game assets. Superseded by the Flux.2 options on quality. |
| [Pixel Art XL](https://civitai.com/models/120096/pixel-art-xl) | Well-known SDXL LoRA, no trigger word needed. Slower than Klein for no gain here. |
| [M_Pixel](https://civitai.com/models/44960/mpixel) | Aimed at "refined pixel paintings" — illustration, not sprites. Wrong direction. |
| [PixelArtRedmond](https://huggingface.co/artificialguybr/pixelartredmond-1-5v-pixel-art-loras-for-sd-1-5) | Non-standard "bespoke-lora-trained-license". Not worth the diligence when Apache-2.0 options exist. |

**None of them solve the palette.** Not the best-licensed, not the
native-32x32, not the one with true grid output. Every one of them will hand
you colours that are not in `palette.gpl`, which is precisely why Step 1 exists
and why the generator stays swappable.

Worth watching: [Palette-Aligned Diffusion](https://arxiv.org/html/2509.02000)
conditions generation on a user-supplied palette so the model uses *exactly*
those colours. If usable weights ship, that collapses most of Step 1's colour
work into the generation step. It is a paper today, not a download.

### Judge a model by running it, not by its card

alucard above is the cautionary tale: its model card promises 128x128 RGBA with
transparent backgrounds and a true pixel grid, and it delivers none of those.
Model cards describe intent. Sample galleries are cherry-picked. The cheap
defence is that a generator's output has to survive `conform-sprite` before it
means anything, and the report is quantitative.

That check has already paid for itself once — run on alucard's noise, conform
returned `mean delta 0.0946` against a 0.05 threshold, 100% of pixels
recoloured, and fired its WARNING. It flagged garbage without being told the
generation had failed. Treat a first conform run as the audition.

Generate at 512x512 or 1024x1024, not 32x32. Diffusion models are bad at tiny
canvases; you want a clean large image that reduces well.

### Picking between them objectively

Sample images on a model page are cherry-picked. `conform-sprite` gives you a
number instead — run the same prompt through each LoRA, then:

```bash
for f in ~/Downloads/lora-test-*.png; do
  echo "== $f"
  dotnet run --project src/MonoGame.GameFramework.Tools -- \
    conform-sprite --input "$f" --output /tmp/$(basename "$f") --size 32 \
    | grep -E "mean delta|palette coverage"
done
```

Lowest **mean delta** fought your palette least. Highest **palette coverage**
kept your ramps intact rather than collapsing them flat. Those two numbers pick
the LoRA better than any gallery will.

---

## Step 1 — Conform

Identical for all three tracks. That is the whole design.

```bash
dotnet run --project src/MonoGame.GameFramework.Tools -- \
  conform-sprite \
  --input  ~/Downloads/enemy-raw.png \
  --output assets/sprites/enemy-f1-idle.png \
  --size   32
```

## Step 2 — Judge the report, before looking at the image

`conform-sprite` prints how far the art had to move to become legal. Read this
first — it is a cheaper and more honest signal than squinting at a 32x32 image.

| mean delta | Meaning | Do |
|---|---|---|
| < 0.01 | Art already respected the palette | Proceed |
| 0.01 - 0.05 | Normal for palette-aware generation | Proceed, inspect at 1:1 |
| > 0.05 | Being *forced* onto the palette | **Regenerate**, don't fix |

Also worth reading:

- **`palette coverage`** — 3/13 colours used means the generator collapsed your
  ramps and the sprite will read flat next to the hero.
- **`alpha flattened`** — a large count means heavily feathered edges, i.e. the
  silhouette was soft and is now guesswork.
- **`WARNING non-uniform scale`** — the source aspect did not match the target.
  Re-author, do not stretch.

The threshold is a starting heuristic (the reference round-trip measured
0.0127). Tune it once you have a few real generations to calibrate against.

### Calibration: the hero replacement, 2026-08-12

The first full run of this pipeline on a shipped asset. Three Flux seeds of one
hooded-scout prompt, then conform at 32x32:

| Source | mean delta | coverage | verdict |
|---|---|---|---|
| Flux seed 42 / 7 / 1234, raw | 0.0950 / 0.0994 / 0.0929 | 9/13 | WARNING |
| the same three, background keyed | 0.0646 / 0.0632 / 0.0539 | 8/13 | WARNING |
| final hand-authored frames | **0.0000** | **13/13** | ship |

Three things fall out of that table.

**Background removal is worth a fixed ~0.033**, consistently across all three
seeds. It is now a step in the generation script rather than advice, because
its size does not vary and forgetting it costs more than the threshold's whole
margin.

**Every generated candidate failed the gate, and the gate was right.** 0.054 to
0.099 against a 0.05 threshold, with coverage collapsing to 8-9 of 13 colours.
The instruction at that level is *regenerate, don't fix* -- so the pixels were
authored by hand, which is also what this document's own "Expectation setting"
section predicts at 32x32. The threshold made that call before anyone looked at
an image.

**The generator still earned its place -- on the design, not the pixels.** All
three seeds independently drew a diagonal baldric strap across the chest, an
idea not in the prompt. It solved a problem the hand-drawn draft had, where a
rust collar under the chin read unmistakably as a beard: putting the accent on
a diagonal across a blue torso gets it away from the face and gives it
something to contrast against. That detail shipped. Concept work is where 180
seconds a frame pays for itself; 1,024 pixels is not.

So: **judge the generation on the report, and mine it for ideas either way.**
A candidate that fails the gate can still be the reason the final art is good.

## Step 3 — Fix by hand

Conform makes art *legal*, not *good*. It cannot fix a muddy silhouette, and
single-pixel details are the first casualty of any downscale. Budget for this
step; it is where the sprite actually becomes usable.

Open in Aseprite with `assets/palette.gpl` loaded and the sprite set to
**Indexed** colour mode — off-palette pixels then become unpaintable, which
beats catching them at the gate.

**Which Aseprite MCP** — tested 2026-08-12 by driving each server over JSON-RPC
stdio and writing one magenta pixel to a copy of `hero.aseprite` at sprite
coordinate (21,27), then verifying by CLI export.

| Server | Version | Tools | Wrote file | Pixel at right coord |
|---|---|---|---|---|
| [diivi/aseprite-mcp](https://github.com/diivi/aseprite-mcp) | 1.6.0 | 116 | yes | **yes**, via `draw_pixels_at` |
| [ayigityol/aseprite-mcp](https://github.com/ayigityol/aseprite-mcp) | 1.1.0 | 43 | yes | **yes** |
| [Vollkorn-Games/aseprite-mcp](https://github.com/Vollkorn-Games/aseprite-mcp) | 0.1.0 | 65 | yes | **yes** |
| willibrandon/pixel-mcp | 0.5.0 | ~50 | no | no |

We run **diivi** (`mcp__aseprite__*`). Two rules:

- **Always `draw_pixels_at`, never bare `draw_pixels`.** The plain variant
  changes the file while putting the pixel in the wrong place — the same class
  of bug as pixel-mcp's open issue #19, and what a tool does when it has no
  layer argument and has to guess. `draw_pixels_at` takes `layer_name` and
  `frame_index` (**1-based**) and is correct.
- **`run_lua_script` is the escape hatch.** Arbitrary Aseprite Lua, ending in
  `spr:saveAs(spr.filename)`. Verified working. Use it for anything the typed
  tools get wrong — including loading `palette.gpl`, which diivi has no
  dedicated tool for (ayigityol and Vollkorn both have `load_palette`).

Whatever the server, **verify the write landed**: `git status` on the file is
the cheap check. A success response is not evidence — pixel-mcp returned
`pixels_drawn: 1` while leaving `hero.aseprite` byte-identical on disk.

## Step 4 — Place it and gate

```bash
# authoring source stays in assets/, export lands in the game
cp assets/sprites/enemy-f1-idle.png \
   src/MonoGame.GameFramework.Platformer/Content/sprites/

# register in Content.mgcb — copy the TextureImporter block from
# template/Content/Content.mgcb (TextureFormat=Color, no padding)

dotnet run --project src/MonoGame.GameFramework.Tools -- check-palette-all
dotnet run --project src/MonoGame.GameFramework.Tools -- check-sprites-all
```

Then swap `e.Draw(spriteBatch, Primitives.Pixel)` for a texture draw, following
`HeroSprites.cs` — and make sure the `SpriteBatch.Begin` passes
`SamplerState.PointClamp`, or `check-sprites` will tell you about it.

---

## Expectation setting

At **32x32**, hand-drawing is often faster than generating and cleaning up.
~1,000 pixels is not a canvas where a model saves you meaningful time, and you
spend the savings on Step 3. These tracks start clearly winning at 64x64+, on
tilesets, on backgrounds, and on multi-directional character sets — anywhere
volume or consistency-across-many-poses is the actual problem.

Use the generator for **what to draw** (silhouette, pose, character identity)
more than for **the pixels themselves**. The pipeline's job is to make sure
that whichever you use, the result cannot drift.
