# Family 01 — the palette keys, ready to run

Two prompts, fully expanded. The prompt book writes them with `<BLOCK S>` and
`<BLOCK P0>` as placeholders; here the blocks are pasted in, so each fence
below is the whole prompt and nothing needs assembling.

Run **01A first**, then **01B**, then the extraction at the bottom. Every other
family in the book waits on the palette these two produce.

Attach no reference images. These two *are* the references.

---

## 01A — Kitchen key

> Founds: `tile-cream` ×3, `grout`, `avocado` ×2, `steel` ×3, `cabinet-dark`,
> `formica-gold` ×3, `chrome`, `water`, `frost`, `cardboard`, `mark-red`.

```text
A single flat illustration of one corner of a 1970s kitchen counter, seen
straight on and filling the frame. This is a materials study: every surface
should be large enough and flat enough to sample a colour from.

It must physically contain all of the following, each clearly separated:

  - the counter surface, in cream glazed ceramic tiles, with darker grout
    lines between them, and a slight sheen along the top edge of each tile
  - a stainless steel sink basin set into the counter, with standing water
    in the bottom of it
  - the counter's rolled avocado-green edge trim, running along the lip
  - a harvest-gold formica cabinet face below, with a chrome handle
  - one cabinet door standing open onto a near-black interior, with the
    edge of a stacked saucepan just catching the light inside it
  - a plain corrugated cardboard box sitting closed on the counter
  - a tall glass of iced water, frost on the outside of the glass
  - a red-and-white checked tea towel folded over the counter's edge

No cats. No people. No hands. No text anywhere, including on the box.

STYLE — applies to every element in the image, without exception:

Flat cel shading. Two or three values per material and no more. Hard edges
between values; no blending between them.

Light comes from directly above with no left or right bias. The image must
be correct if it is mirrored horizontally — no pixel's value may depend on
which side of the subject it sits on. No cast shadows, no rim lighting from
one side, no specular hotspots off-centre.

Solid #1A1A1A outline around exterior silhouettes only. Separate interior
detail by value within a single material, never with a black line.

None of the following: anti-aliasing, gradients, glow, bloom, blur, depth of
field, lens flare, ambient occlusion, film grain, texture noise, halftone,
scanlines, chromatic aberration, drop shadows, vignetting.

No partial transparency anywhere. Every pixel is either fully opaque or
fully absent.

Orthographic side elevation. No perspective convergence, no foreshortening,
no three-quarter view unless the prompt asks for one by name.

Do not draw a pixel grid. Do not imitate pixel art. Do not add a border, a
frame, a caption, a watermark, a label, a colour swatch strip, or any text.

Plain flat background. No scenery, no props, nothing behind the subject.

COLOUR — a 1970s domestic kitchen, deliberately muted, so that the cats are
the saturated things in every scene:

  cream glazed ceramic tile, with a darker warm grout
  avocado green, for the counter's rolled edge trim
  harvest gold formica, for cabinet faces and shop furniture
  brushed steel and chrome, for the sink and the trim
  a near-black cabinet interior, warm rather than blue

Fur painted in real cat colours on top of that: ginger, grey, cream, brown
tabby, seal point, and a cool blue-black — never a true black — for black
cats, so that a black cat stays separate from its own outline.

Keep every material to two or three flat values. Nothing neon, nothing
pastel, no purple or teal anywhere in the kitchen itself.
```

Save as `palette-key-kitchen.png` in this directory.

---

## 01B — Cast key

> Founds: `orange` ×3, `grey` ×3, `cream` ×3, `tabby-brown` ×3, `seal` ×3,
> `void` ×3, `pink` ×2, `eye-yellow`, `eye-green`, `eye-blue`, `tooth-white`.

```text
Six cats standing side-on in a row on a plain flat cream ground, evenly
spaced, all facing left, all the same size, all lit identically. This is a
coat study: each animal is here for its colour, so keep the poses neutral
and identical and let the coats be the only difference.

Left to right:

  1. a ginger tabby, with visible ring markings and a white bib
  2. a grey-and-white shorthair, hard boundary between the two
  3. a cream longhair with blue eyes
  4. a brown mackerel tabby with green eyes and a white chin
  5. a seal-point Siamese: pale body, dark mask, ears, feet and tail,
     blue eyes
  6. a black cat with yellow eyes, painted in a cool blue-black that is
     clearly lighter than the outline colour, so the cat never merges
     into its own edge

The fourth one has its mouth open, showing clean white teeth. The second
wears a plain red collar. Pink noses, pink inner ears, and one visible
pink paw pad across the group.

All six are the same species with the same proportions and the same
treatment: one artist's cast, not six pictures of cats.

STYLE — applies to every element in the image, without exception:

Flat cel shading. Two or three values per material and no more. Hard edges
between values; no blending between them.

Light comes from directly above with no left or right bias. The image must
be correct if it is mirrored horizontally — no pixel's value may depend on
which side of the subject it sits on. No cast shadows, no rim lighting from
one side, no specular hotspots off-centre.

Solid #1A1A1A outline around exterior silhouettes only. Separate interior
detail by value within a single material, never with a black line.

None of the following: anti-aliasing, gradients, glow, bloom, blur, depth of
field, lens flare, ambient occlusion, film grain, texture noise, halftone,
scanlines, chromatic aberration, drop shadows, vignetting.

No partial transparency anywhere. Every pixel is either fully opaque or
fully absent.

Orthographic side elevation. No perspective convergence, no foreshortening,
no three-quarter view unless the prompt asks for one by name.

Do not draw a pixel grid. Do not imitate pixel art. Do not add a border, a
frame, a caption, a watermark, a label, a colour swatch strip, or any text.

Plain flat background. No scenery, no props, nothing behind the subject.

COLOUR — a 1970s domestic kitchen, deliberately muted, so that the cats are
the saturated things in every scene:

  cream glazed ceramic tile, with a darker warm grout
  avocado green, for the counter's rolled edge trim
  harvest gold formica, for cabinet faces and shop furniture
  brushed steel and chrome, for the sink and the trim
  a near-black cabinet interior, warm rather than blue

Fur painted in real cat colours on top of that: ginger, grey, cream, brown
tabby, seal point, and a cool blue-black — never a true black — for black
cats, so that a black cat stays separate from its own outline.

Keep every material to two or three flat values. Nothing neon, nothing
pastel, no purple or teal anywhere in the kitchen itself.
```

Save as `palette-key-cast.png` in this directory.

---

## Then extract

`scripts/extract-knockitoff-palette.sh` runs all of this. By hand:

```bash
C=assets/concept/knockitoff
T="dotnet run --project src/MonoGame.GameFramework.Tools --"

# step 0 — measure before judging. Native grid, colour count, alpha, bounds.
$T describe-image  --input $C/palette-key-kitchen.png
$T describe-image  --input $C/palette-key-cast.png

$T extract-palette --input $C/palette-key-kitchen.png --output /tmp/kitchen.gpl \
                   --name "Retro Kitchen — surfaces" --max-colours 18
$T extract-palette --input $C/palette-key-cast.png    --output /tmp/cast.gpl \
                   --name "Retro Kitchen — fur"      --max-colours 20
```

Then merge the two into the game's palette, replacing the scaffolded starter
one wholesale — it is three neutral ramps that commit to nothing and it says
`REPLACE THIS` at the top:

```
src/MonoGame.GameFramework.KnockItOff/Content/sprites/palette.gpl
```

Four rules for the merge, all of which bite:

1. **`outline #1A1A1A` stays exactly as it is, first in the file.**
   `extract-palette` rewrites the outline to the spine on the way out, so both
   halves will already carry it — keep one.
2. **Rename the entries to the design's group names.** Extraction names by hue
   family (`blue-2`, `orange-3-hilite`); the design wants `tile-cream`,
   `grout`, `avocado`, `steel`, `cabinet-dark`, `formica-gold`, `chrome`,
   `orange`, `grey`, `cream`, `tabby-brown`, `seal`, `void`, `pink`,
   `eye-yellow`, `eye-green`, `eye-blue`, `tooth-white`, `cardboard`,
   `water`, `frost`, `mark-red`.
3. **Add the four colours no extraction can find.** `fae-green` and `spark`
   appear nowhere in either key image, and `tooth-white` and `frost` may get
   clustered away into a neighbouring ramp. Each is a one-sprite colour; pick
   them by hand.
4. **Check the `void` ramp's floor.** Its darkest step must sit at least
   3 Oklab L above `#1A1A1A`. Six cats in the roster are black, and if `void`
   collapses toward the outline they lose their silhouettes to their own edges
   — silently, with every gate still green.

Then:

```bash
$T check-palettes        # spine present, no ramp collisions, across all 13
$T check-palette-all     # the two scaffolded sprites still legal under the new palette
dotnet test Game.sln     # PaletteRegistryTests runs the same rules per palette
```

`check-palette-all` is the one that will complain: the scaffolded
`placeholder.png` and `ui-frame.png` are painted from the starter palette, and
replacing it makes them off-palette. That is expected. Re-key their `.pix`
sources to the new entry names and `render-pix-all`, or delete them once real
art exists — they are scaffolding, not assets.
