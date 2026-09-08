# Knock It Off — a cat auto chess

Design v0.1, 2026-09-04. Working title; alternates at the end. This is the
game's spec, written to be implementable in this repo: every number here is a
first pass for tuning, every mechanic is meant to be built as written.

The one-paragraph pitch: **an auto chess where the board is a kitchen counter
and the win condition is knocking the other side's cats off it.** Eight
households, one counter each, cats adopted three at a time and merged into
bigger cats, the usual shop-and-economy loop — and under all of it a hex grid
with edges that drop to the floor, a sink in the middle, and tiles that can be
pried up so there is somewhere new to fall. Cats knock things off tables. That
is the whole game.

---

## 1. The Counter

### 1.1 Shape

A **7-column × 8-row** hex board, pointy-top, alternating row offset (odd rows
shifted half a hex right). You own the bottom four rows; the opponent owns the
top four. The board is 180°-symmetric, so the opponent's counter is yours
rotated: `Mirror(col, row) = (6 - col, 7 - row)`.

```
              col:  0    1    2    3    4    5    6
      row 0       ( . )( . )( . )( . )( . )( . )( . )      enemy back edge
      row 1          ( . )( . )( . )( . )( . )( . )( . )
      row 2       ( . )( . )( . )( . )( . )( . )( . )
      row 3          ( . )( . )( . )(~~~)( . )( . )( . )   -- midline --
      row 4       ( . )( . )( . )(~~~)( . )( . )( . )
      row 5          ( . )( . )( . )( . )( . )( . )( . )
      row 6       ( . )( . )( . )( . )( . )( . )( . )
      row 7          ( . )( . )( . )( . )( . )( . )( . )   your back edge

      ( . )  a tile        (~~~)  the Sink        beyond the border: the floor
```

- **56 hexes, 54 standable.** 27 per side during placement.
- **The Sink** is the two centre hexes, `(3,3)` and `(3,4)`. They are the only
  two hexes that touch the board's exact centre, they map onto each other under
  the mirror, and nothing can stand on them. The Sink splits the counter into
  a left lane and a right lane, so front lines meet in two places, not one.
- **The edge** is the 26 perimeter hexes. A hex direction that leaves the board
  leads to the floor.
- Storage is offset `(col, row)`; maths is axial `(q = col - (row - (row & 1)) / 2, r = row)`.
  The six directions in axial: E `(+1,0)`, W `(-1,0)`, NE `(+1,-1)`, NW `(0,-1)`,
  SE `(0,+1)`, SW `(-1,+1)`.

Why hexes, briefly: six neighbours means three cats cannot wall a lane, every
shove has six possible lines instead of four, flanking is a real position
rather than a diagonal edge case, and a two-hex hole in the middle carves the
board into lanes without gridlines.

### 1.2 Tile states

Every hex is a **Tile**. Tiles have a state, and the state is what the knock-off
game is played on:

| State | What it is | How it happens | What it does |
|---|---|---|---|
| **Normal** | a ceramic tile | default | nothing |
| **Wet** | water on the tile | Van's *Splash*, the Water Bowl placemat | a unit **starting** a slide here slides 1 hex further |
| **Cracked** | grout gone, tile loose | Manx's *Rump Bump* | a unit displaced **off** it breaks it (→ Hole); a **Heavy** unit displaced **onto** it falls through |
| **Hole** | the tile is gone; you can see into the cabinet | Cheshire's *Vanish*, Chonk's *Clear the Table*, a broken Cracked tile, the Loose Tile placemat, Closing Time | units path around it; a unit displaced into it **falls**; Voids re-emerge beside it |
| **Sink** | the two centre hexes | permanent | a Hole with water in it: same rules, different splash |

**The counter resets between rounds.** Holes are patched, water is wiped,
cracks are grouted. The fiction is that the human cleans up; the reason is that
a hole that persisted would make the board unlearnable by stage 3.

### 1.3 Screen

Design canvas **640×360**, presented through `ScreenScaler` at 3× on 1080p, 4×
on 1440p, 6× on 4K. Hex tile art is **48×40** (pointy-top, squashed for a
slightly raised camera); row pitch is 30 px, so the board is **360×250** in
design pixels — 56% of the canvas width, which is what an auto chess wants.
Cats are **32×32**, soles on row 31, drawn with the bottom row 12 px below the
hex centre so they stand on the tile's lower half. Draw order is row-major so a
cat in a lower row overlaps the one above it.

This canvas needs a pixel font. Arial at 8 px through nearest-neighbour is not
a font. That is a new asset for this game, not a library change.

---

## 2. Knocking things off

This section is the rules engine. Everything a cat, item or trait does to move
another unit resolves through it.

### 2.1 Vocabulary

- **Displacement** — any forced movement. Two kinds: a **Shove** moves the
  target away from the source; a **Yank** moves it toward the source.
- **Force** — how many hexes, before weight. 1 is normal, 2 is a big cat, 3 is
  Caracal.
- **Weight** — every cat is **Light**, **Medium** or **Heavy**. Light slides
  **+1** hex, Medium **+0**, Heavy **−1** (minimum 0: a Heavy cat hit by Force 1
  rocks in place and takes the hit). Statuses can change a unit's class by one.
- **Planted** — cannot be displaced at all. Loafs get it from their trait,
  Pampered cats while shielded, several spells grant it to neighbours, and
  three items give it back to a carry. Something shoved *into* a Planted unit
  collides with it.
- **Fall** — the unit leaves the counter for the rest of the round.
- **KO** — a death *or* a fall. Traits that count "knockouts" count both.

### 2.2 Direction

A shove from an adjacent source moves along the hex direction from source to
target. From further away (a Reach-2 hiss, a Yank), it moves along the hex
direction **closest to the source→target line**. Exact ties — a target sitting
at 30° between two directions — resolve **toward the nearer edge or hole**,
then clockwise. Cats are like that.

A Yank uses the same rule with the direction reversed, and stops when the unit
is adjacent to the source. If there is a hole between them, the unit does not
get that far.

### 2.3 Resolving a slide

For a displacement of effective force `F = Force + weight modifier (+1 if the start tile is Wet)`,
repeat `F` times:

1. Look at the next hex in the direction.
2. **Off the board or a Hole** → the unit **falls**. Stop.
3. **Occupied** → **Collision**: the slide stops; both units take **8% of the
   sliding unit's max HP** as true damage. (Knocker 6: the blocking unit is
   itself shoved with `F - 1`, chaining.)
4. Otherwise the unit moves. If the hex it just **left** was Cracked, that
   hex becomes a Hole. If the hex it **landed on** is Cracked and the unit is
   Heavy, it falls through.

The slide animates at 0.12 s per hex with a squash; combat does not pause. The
unit cannot act mid-slide, keeps its target, and can still be hit.

### 2.4 Falling

A fallen cat is out for the round. It is not dead:

- **On-death effects do not trigger.** A fall is the counter to a death-trigger
  comp. Cat Sìth stealing a soul needs a body.
- **Fall-specific effects do**: the Void clan's re-emergence, Bastet's *Nine
  Lives*, Schrödinger's coin, Van's *Swimmer*.
- The cat is still a survivor for nothing: at round end it counts as KO'd for
  win/loss and for damage.

Off the edge: the sprite slides past the counter's lip, clipped by the board,
with a two-frame scrabble and a soft *thump* from below. Into the Sink: splash.
Into a Hole: a dust puff and the sound of something landing on pans.

### 2.5 The geometry cheat sheet

These follow from the rules, and they are what a player has to learn:

- **Melee shoves from the front push toward the enemy's back edge.** A cat
  standing on the back row is one Force-1 shove from the floor; standing one
  row in costs nothing in most comps and denies every Force-1 knock-off. That
  positioning decision, every round, is the game.
- **Leaps that land behind push toward the middle.** Savannah lands behind
  its target and kicks it forward — toward the Sink if the lane lines up,
  into your Loaf for collision damage if not.
- **Yanks pull across holes.** Bakeneko standing across the Sink from the
  enemy front line is fishing.
- **The Pouncer trait flips Bengal's geometry.** Alone, Bengal leaps from
  your side and bowls its target toward their edge. With two Pouncers, it
  starts the fight behind their line and bowls targets toward the Sink instead.
  Both are good. They are different comps.
- **Holes made on your side help enemy Voids.** Cheshire's first *Vanish*
  opens a hole in your back line. That is a liability and a portal; which one
  depends on who brought the black cats.
- **Weight is a stat you can build.** Box in a Box makes a carry Heavy;
  Spooked makes a tank Medium. Force 2 on a Spooked Medium cat is three hexes.

### 2.6 Guardrails

Knock-offs are instant KOs, so access is rationed:

- **Basic attacks never displace.** Only spells do, plus the Knocker trait's
  every-Nth-attack Swat.
- **Force 1 is the norm.** Force 2 is on three cats (one at cost 2, two at
  cost 3+), Force 3 is on one (Caracal, cost 4). Chonk's Force 2 is the
  legendary because it also makes holes.
- **Holes are made by two 5-costs, one 2-cost's setup, one rare placemat, and
  overtime.** Nothing else.
- **Every clan has an answer.** Pampered shields are Planted; Loafs plant when
  low; Voids come back; Groomers at 4 plant the carry for 2 s; the Anti-Slip
  Mat, Perch, Nimble and Won't Move are items that do it.

---

## 3. A round

The genre loop is standard and this section only names the parts and the
places this game differs. Eight players. You lose when your **Patience** (the
human's) hits zero: the cat is put outside.

| Genre term | Here | Notes |
|---|---|---|
| Gold | **Treats** | 5 base per round; +1 per 10 banked (max 5); win/loss streaks +1/+2/+3 |
| XP / Level | **Trust** | 4 per round, buy 4 for 4 Treats; Trust level = cats allowed on the counter (2 → 9) |
| Shop | **The Shelter** | 5 cats, 2 Treats to **shake the bag** (reroll) |
| Sell | **Rehome** | full refund at 1★, standard otherwise |
| Bench | **The Windowsill** | 9 slots |
| Carousel | **The Cat Distribution System** | a slow turntable of cats holding toys, everyone grabs one |
| Player HP | **Patience** | 100; on a loss lose `2 + stage + 2 per surviving enemy cat (+1 per star above 1★)` |

**Three of a kind.** Three copies of a cat merge into a 2★ (HP ×1.8, Claw ×1.5,
spell numbers step up); three 2★ merge into a 3★. A 3★ 1-cost is a fully grown
cat and is described that way in its card text. Visually 2★ gets a silver name
tag on the collar, 3★ a gold tag and a shared gold shimmer behind the sprite.

**Pool sizes** by cost: 30 / 25 / 18 / 10 / 9 copies. **Shop odds** by Trust
level (1/2/3/4/5-cost %):

| Level | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|
| 3 | 75 | 25 | — | — | — |
| 4 | 55 | 30 | 15 | — | — |
| 5 | 45 | 33 | 20 | 2 | — |
| 6 | 30 | 40 | 25 | 5 | — |
| 7 | 19 | 30 | 35 | 15 | 1 |
| 8 | 18 | 25 | 32 | 22 | 3 |
| 9 | 10 | 20 | 25 | 35 | 10 |

### 3.1 Phases

1. **Planning** — 30 s. Drag cats between the windowsill and your 27 hexes.
   Place placemats. Buy, shake, rehome.
2. **Combat** — 30 s of real-time auto-battle (see §9 for the sim tick).
3. **Closing Time** — if both sides still stand at 30 s, the human starts
   clearing the counter. Every 5 s the outermost ring of tiles — any hex
   adjacent to the counter's edge or to an already-cleared hex — is cleared,
   and any cat standing on it is set down on the floor: it **falls**. At 50 s
   whatever is left is cleared and the round is a draw; both take damage.
   This is the overtime rule and it uses the fall mechanic rather than adding
   a new one. Planted does not resist a human.
4. **Result** — damage, Treats, presents from Mousers, the Cat Distribution
   System on carousel rounds.

### 3.2 Nuisances (PvE)

The PvE rounds teach the knock-off rules one at a time and drop toys.

| Round | Nuisance | Behaviour | Teaches |
|---|---|---|---|
| 1-1 | Moths ×3 | flutter, 1 HP each, drop kibble | attacking; Mouser presents |
| 1-2 | **The Roomba** | a low disc that bumps: Force 1 shove on contact | the shove rule, in round two |
| 1-3 | Roomba ×2 + Moths | patrols both lanes | pathing round the Sink |
| 2-7 | **Biscuit** (a golden retriever) | Heavy, enormous HP, *Boop*: Force 2 shove every 6 s | weight and collisions; drops 2 toy components |
| 3-7 | **The Toddler** | every 6 s yanks the nearest cat 2 hexes toward the nearest edge | yanks and edges; drops a Fish Bone |
| 4-7 | **The Vacuum** | switches on every 8 s: every cat is shoved 1 hex toward it | Planted |
| 5-7 | **The Cucumber** | sits still; every cat within 2 hexes is Spooked while it lives | Spooked and weight class |
| 6-7 | **The Human's Hand** | every 10 s picks up a random unplanted cat and sets it on the floor | that nothing is safe |

---

## 4. Cats

### 4.1 Stats

| Stat | Meaning | Notes |
|---|---|---|
| **HP** | health | ×1.8 per star |
| **Claw** | attack damage | ×1.5 per star; physical, reduced by Fluff |
| **Swipe** | attacks per second | 0.55 (Chonk) to 0.95 (Mau) |
| **Reach** | attack range in hexes | 1 melee; 2 casters and groomers; 3–4 Mousers |
| **Fluff** | armour | physical damage × `100 / (100 + Fluff)` |
| **Whiskers** | magic resist | same formula for spell damage |
| **Mischief** | mana | +10 per attack; +3% of pre-mitigation damage taken (max 20 per hit); cast at max |
| **Weight** | Light / Medium / Heavy | see §2.1; shown as one, two or three paw prints on the card |
| **Zoom** | move speed, hexes/s | Medium 1.5; Light and Pouncers 2.0; Heavy 1.2; leaps are 0.3 s arcs |
| **Spell Power** | spell damage/heal multiplier | 100% base |

Melee Reach-1 cats attack and are attacked at range 1; **Mousers attack by
batting objects across the counter** (bottle caps, kibble, olives, peanuts,
grapes — each Mouser has its own projectile), which is where their range comes
from and why their attack animations are all "line up, flick".

### 4.2 Traits

Each cat has one **Clan** (where it's from) and one **Role** (what it does).
Two cats have two clans. Numbers are `at 2 / at 4 / at 6` unless marked.

#### Clans

| Clan | Breakpoints | Effect |
|---|---|---|
| **Alley** — strays and working cats | 2 / 4 / 6 | *Scrappy.* Alley cats gain **+15 / 30 / 50% Claw and +15 / 30 / 50 Fluff**. Every KO on the counter — either side, death or fall — grants Alley cats a further **+6% Claw** for the rest of combat. They thrive in a mess. |
| **Pampered** — indoor cats | 2 / 4 / 6 | *Cushioned.* At combat start Pampered cats gain a shield of **20 / 35 / 50% max HP**. **While shielded they are Planted.** They are not moving from their spot. |
| **Show** — pedigree | 2 / 4 / 6 | *Spotlight.* Your highest-cost Show cat (ties: furthest back) gets the spotlight: **+25 / 45 / 70% Spell Power and +15 / 30 / 50% Swipe**; other Show cats get half. When it is KO'd the spotlight moves to the next Show cat after 1 s. A literal light circle follows it. |
| **Wild** — hunters | 2 / 4 / 6 | *The Hunt.* The first enemy a Wild cat damages is **Marked**. Wild cats target the Marked enemy whenever it is in reach and deal **+20 / 40 / 70%** damage to it; when it is KO'd, the next enemy a Wild cat damages is Marked. At 6, **Marked enemies take +1 Force** from all displacement. |
| **Void** — black cats | 2 / 4 / 6 | *Through the Void.* A Void that **falls** re-emerges **2 s later** on a random empty hex adjacent to any Hole (the Sink counts). At 2: the first Void to fall each combat, at 50% HP. At 4: every Void, once each, at 70%. At 6: every fall, at 100%, and re-emerging deals 200 magic damage to adjacent enemies. |
| **Mythic** — cats of folklore | 3 / 5 | *Moon Omen.* 6 s into combat and every 6 s after, the moon shows over the counter and **every Mythic fills its Mischief and casts**. At 5: every 4 s, and Mythic spells deal **+30%**. A coordinated burst on a metronome you can see coming. |

#### Roles

| Role | Breakpoints | Effect |
|---|---|---|
| **Knocker** — the ones who push things off tables | 2 / 4 / 6 | *Knock It Off.* Knockers gain **+15% Claw**. Every **4th / 3rd / 2nd** attack is a **Swat**: a Force-1 shove. At 4, Knocker spell displacement gains **+1 Force**. At 6, collisions **chain**: the blocking unit is shoved with the remaining force. |
| **Loaf** — tanks | 2 / 4 / 6 | *Bread Mode.* Loafs gain **+20 / 40 / 70 Fluff and Whiskers**. The first time a Loaf drops below 50% HP it **loafs** for 4 s: paws tucked, **Planted**, 25% less damage taken, heals 4% max HP/s. It still swats. At 6 there is no once-per-combat limit. |
| **Pouncer** — divers | 2 / 4 / 6 | *Ambush.* At combat start Pouncers leap to the hex behind the enemy farthest from them; leaps ignore holes and the Sink. Pouncers gain **20 / 40 / 60% crit chance**, crits dealing **150 / 175 / 200%**. |
| **Hisser** — casters | 2 / 4 | *Spooked.* Hissers gain **+20 / 50% Spell Power**. Enemies damaged by a Hisser spell are **Spooked** for 3 s: −20% Fluff and Whiskers, and they count as **one weight class lighter** (a Spooked Light cat slides one extra hex). Hissers set up knock-offs. |
| **Groomer** — support | 2 / 4 | *Grooming.* Every 3 s each Groomer licks the lowest-HP ally within 2 hexes for **5 / 10% max HP**. At 4, grooming also **cleanses** Spooked and Wet and grants **Planted for 2 s**. |
| **Mouser** — ranged | 2 / 4 | *Presents.* Mousers gain **+20 / 45% Swipe**. When a Mouser KOs an enemy there is a **30 / 60%** chance it leaves a present: **+1 Treat** at round end. |

Membership, so the breakpoints are honest about what is reachable without
emblems: Alley 6, Pampered 6, Show 6, Wild 7, Void 6, Mythic 5, Knocker 6,
Loaf 7, Pouncer 6, Hisser 5, Groomer 5, Mouser 5. Skog and Cat Sìth carry two
clans each.

### 4.3 The knock-off toolkit at a glance

| Does | Who |
|---|---|
| **Shove** | Tabby Tom F1 · Scrap F1 vs Light · Sock F1 at edges · Smudge F1 (flee) · Bengal F1 (bowl over) · Manx **F2** + crack · Sphinx 25%/attack F1 · Fold F1 × 3 hexes · Savannah **F2** forward · Lykoi F1 vs Spooked · Bosun F1 on edges · Caracal **F3** · Chonk **F2** + holes · Wampus F1 sideways · Knocker swats |
| **Yank** | Bakeneko F1 ring · Maneki F1 nearest |
| **Make holes** | Cheshire · Chonk · Manx (via Cracked) · Loose Tile placemat · Closing Time |
| **Wet** | Van · Water Bowl placemat |
| **Planted** | Bodega, Marmalade, Mittens (+ neighbours), Moose (+ neighbours), Skog (+ neighbours), Midnight, Schrödinger · Pampered shield · Loaf trait · Groomer 4 · Anti-Slip Mat, Perch, Nimble, Won't Move |
| **Change weight** | Spooked −1 · Box in a Box +1 · Mittens' *Thumbs* +1 · Manx's *No Tail* (takes +1 Force) · Wild 6 (+1 Force vs Marked) |
| **Come back** | Void clan · Bastet's *Nine Lives* · Schrödinger's coin · Van's *Swimmer* (Sink only) |

---

## 5. The roster

34 cats. Stats are 1★. Spell numbers are `1★ / 2★ / 3★`. Every entry has
its signature animation because "they'll all have unique animations" is a
requirement, and because the cast animation is where a cat's personality
actually lives at 32 px.

### Cost 1

| Cat | Clan | Role | Wt | Reach | HP | Claw | Swipe | Fluff | Whisk | Mischief |
|---|---|---|---|---|---|---|---|---|---|---|
| Tabby Tom | Alley | Knocker | M | 1 | 600 | 50 | 0.70 | 30 | 20 | 0 / 60 |
| Bodega | Alley | Loaf | H | 1 | 750 | 40 | 0.60 | 45 | 30 | 0 / 70 |
| Scrap | Alley | Mouser | M | 3 | 500 | 45 | 0.75 | 20 | 20 | 0 / 50 |
| Marmalade | Pampered | Loaf | H | 1 | 700 | 45 | 0.55 | 40 | 20 | 0 / 80 |
| Sock | Pampered | Pouncer | L | 1 | 500 | 55 | 0.80 | 20 | 20 | 0 / 50 |
| Pip | Wild | Mouser | L | 3 | 480 | 50 | 0.80 | 15 | 15 | 0 / 60 |
| Whisker | Show | Groomer | L | 2 | 480 | 35 | 0.70 | 15 | 25 | 20 / 60 |
| Smudge | Void | Hisser | L | 2 | 480 | 35 | 0.70 | 15 | 25 | 10 / 60 |

**Tabby Tom** — the stray who started it. *Swat*: shoves the target 1 hex and
deals 150 / 225 / 340 physical; a collision hurts both. Idle: tail flick, one
ear rotates independently. Cast: the slow paw-extend **while staring straight
at the camera**, then the shove — the fourth-wall stare is his. Art: brown
mackerel tabby, torn left ear, white chin; the everycat.

**Bodega** — sits on the register. *"We're Closed."*: sits where it stands,
Planted 4 s, gains a 250 / 375 / 550 shield and **taunts** adjacent enemies.
Idle: the slow blink of a cat that has seen everything. Cast: rear lowers,
tail wraps round the paws, a little flip-sign reading "NO" appears overhead.
Art: enormous grey-and-white shorthair, unbothered, the widest cost-1
silhouette.

**Scrap** — one-eyed, flicks **kibble**. *Kibble Volley*: 3 / 4 / 5 kibble at
the target for 60 / 90 / 135 physical each; the last one shoves 1 hex if the
target is Light. Idle: scratches an ear with a hind leg. Cast: a rapid
paw-flick loop, kibble arcing. Art: scruffy black-and-tan, one eye squinted
shut, notched ear.

**Marmalade** — the orange one. *Flop*: tips over sideways with a thud (the
tile, not the screen, shakes 1 px), Planted 3 s, heals 15 / 20 / 30% max HP,
and attacks against it deal 30% less while flopped — it's just so much cat.
Idle: belly rises and falls, one paw twitches. Art: round orange tabby, white
bib, vacant expression. **Heavy at cost 1**: the cheap answer to a Knocker
board.

**Sock** — tuxedo kitten, white paws. *Ankle Attack*: leaps to the lowest-HP
enemy within 3 hexes for 200 / 300 / 450 physical; if that enemy is adjacent
to an edge or hole the leap **bowls it over** — Force 1 in the leap direction,
Sock takes its hex. Idle: the pre-pounce butt wiggle. Cast:
crouch–wiggle–launch–land, four frames, the wiggle held. Art: black with white
mitts and bib, enormous eyes.

**Pip** — calico kitten, bats **bottle caps**. *Hair Tie*: snaps a hair tie
at the Marked (else current) target for 180 / 270 / 400 physical and −30%
Swipe for 3 s. Idle: stares at something off-screen, pupils dilate to full.
Cast: pulls the tie back with both paws, lets go. Art: white base with orange
and black patches placed so all three read at 32 px.

**Whisker** — grey kitten with a rosette. *Lick*: grooms the lowest-HP ally
within 2 hexes for 180 / 270 / 420 and cleanses Spooked and Wet. Idle: licks a
paw, wipes its face. Cast: leans into the ally, three quick lick frames, the
ally sparkles. Art: silver-grey, pink collar, a show rosette that is the only
red on the sprite.

**Smudge** — smoky black kitten. *Hiss*: the target **flees** 1 hex directly
away (a Force-1 shove that respects Planted), takes 160 / 240 / 360 magic, and
is Spooked. Idle: the fur along the spine lifts and settles. Cast: arched back,
fur up, mouth open, the hiss drawn as three dashes. Art: `void` ramp with a
grey smudge on the nose — the one non-black feature that keeps a black kitten
separate from the outline.

### Cost 2

| Cat | Clan | Role | Wt | Reach | HP | Claw | Swipe | Fluff | Whisk | Mischief |
|---|---|---|---|---|---|---|---|---|---|---|
| Duchess | Show | Hisser | M | 2 | 650 | 40 | 0.65 | 25 | 40 | 30 / 80 |
| Sphinx | Show | Pouncer | L | 1 | 600 | 65 | 0.85 | 15 | 30 | 0 / 60 |
| Meezer | Show | Mouser | L | 4 | 550 | 55 | 0.80 | 15 | 25 | 0 / 70 |
| Bengal | Wild | Pouncer | M | 1 | 650 | 65 | 0.80 | 25 | 20 | 0 / 70 |
| Van | Wild | Hisser | M | 2 | 620 | 40 | 0.65 | 20 | 35 | 20 / 70 |
| Manx | Alley | Knocker | M | 1 | 700 | 55 | 0.70 | 35 | 20 | 0 / 70 |
| Ragdoll | Pampered | Groomer | H | 2 | 750 | 40 | 0.60 | 30 | 35 | 20 / 80 |
| Mittens | Pampered | Loaf | M | 1 | 720 | 45 | 0.60 | 40 | 30 | 0 / 70 |

**Duchess** — a Persian. *Withering Stare*: a cone — the hex in front and the
two beside it, fanning to five — for 220 / 330 / 500 magic; enemies hit lose
25% Swipe for 4 s. Idle: grooms a cheek, judges you. Cast: eyes narrow to
slits, one slow blink, the cone lights. Art: white, flat face, doll eyes, a
tiara.

**Sphinx** — a Sphynx. *Goblin Mode*: for 4 s, +60 / 80 / 100% Swipe and each
attack has a 25% chance to Swat (Force 1). Idle: shivers; the wrinkles shift.
Cast: hunches, eyes go wide, a manic six-frame flurry. Art: pink-grey, hairless,
huge ears, gold chain; the only cat with no fur ramp at all.

**Meezer** — a Siamese, bats **olives**. *Yowl*: a piercing cry down a 4-hex
line for 200 / 300 / 450 magic; everything in it is Spooked. Idle: a silent
meow at irregular intervals — Siamese talk. Cast: head back, mouth wide, sound
rings drawn as expanding hex outlines. Art: seal-point, blue eyes, angular.

**Bengal** — the knock-off tool at cost 2. *Bowl Over*: leaps at the target
from up to 3 hexes, lands on its hex; the target is shoved 1 hex in the leap
direction and takes 250 / 375 / 560 physical. Idle: prowls in place, shoulder
blades rolling. Cast: a three-frame arc, land, the shove is the landing. Art:
gold with black rosettes, long and muscular.

**Van** — a Turkish Van, the cat that swims. Passive *Swimmer*: shoved into
the Sink, Van climbs out on the far side instead of falling. *Splash*: 200 /
300 / 450 magic in a 1-hex ring round the target and every hex in it is **Wet**
for 5 s. Idle: shakes a wet paw. Cast: reaches toward the Sink, dips a paw,
flings droplets — the only spell whose animation points at the board's middle.
Art: white with an auburn head and tail.

**Manx** — no tail. Passive *No Tail*: Manx takes +1 Force from all
displacement; it cannot balance. *Rump Bump*: hops and slams its rump into the
target: **Force 2**, 180 / 270 / 400 physical, and **the hex the target lands
on is Cracked**. Idle: a rabbit-hop bounce. Cast: turns around — the one
back-view frame in the roster — and hops backward into the target. Art: stubby,
round-rumped, long hind legs, and nothing where the tail should be.

**Ragdoll** — goes limp. Passive: Ragdoll takes no collision damage. *Comfort
Flop*: moves next to the lowest-HP ally within 3 hexes, heals it 250 / 375 /
560 and shields both for 200 / 300 / 450. Idle: slowly melts flatter, then
resets. Cast: a boneless slide across the hexes and a flop. Art: big, floppy,
blue-eyed colourpoint on a cream body.

**Mittens** — polydactyl. Passive *Thumbs*: grips the counter — counts one
weight class heavier (Medium → Heavy). *Making Biscuits*: kneads for 3 s:
Planted, +40 Fluff and Whiskers, and adjacent allies are Planted too while it
kneads. Idle: kneads the air with oversized mitts. Art: white with a grey cap;
paws drawn four pixels wide instead of three.

### Cost 3

| Cat | Clan | Role | Wt | Reach | HP | Claw | Swipe | Fluff | Whisk | Mischief |
|---|---|---|---|---|---|---|---|---|---|---|
| Moose | Alley | Loaf | H | 1 | 900 | 60 | 0.60 | 45 | 35 | 0 / 90 |
| Bosun | Alley | Mouser | M | 3 | 650 | 65 | 0.75 | 25 | 25 | 0 / 80 |
| Fold | Pampered | Knocker | M | 1 | 750 | 60 | 0.70 | 35 | 30 | 0 / 80 |
| Savannah | Wild | Knocker | M | 1 | 720 | 70 | 0.80 | 25 | 25 | 0 / 70 |
| Midnight | Void | Loaf | H | 1 | 850 | 55 | 0.60 | 40 | 45 | 0 / 90 |
| Lykoi | Void | Pouncer | L | 1 | 650 | 70 | 0.85 | 20 | 30 | 0 / 60 |
| Birman | Show | Groomer | M | 2 | 700 | 45 | 0.65 | 25 | 40 | 30 / 90 |

**Moose** — a Maine Coon. *Bulwark*: braces; Moose and all adjacent allies are
Planted for 4 s and gain 30 / 45 / 60 Fluff and Whiskers; Moose gains a 350 /
525 / 800 shield. Idle: the enormous tail sweeps once; an ear tuft moves. Cast:
plants all four feet and puffs up a pixel. Art: brown tabby with a shaggy ruff
and lynx-tipped ears; fills the 32-px canvas edge to edge, the biggest domestic
sprite.

**Bosun** — a ship's cat, bats **peanuts**. *Broadside*: a peanut at each of
the 3 nearest enemies for 200 / 300 / 450 physical; anything hit while standing
on an edge hex is shoved 1 hex outward. Idle: sways as if on deck. Cast: a
three-shot volley, each flick a frame. Art: black-and-white, red neckerchief,
one gold earring.

**Fold** — a Scottish Fold that sits like a person. *Table Sweep*: a slow arc of
the paw across the three hexes in front (target and both flanks): each enemy
there is shoved 1 hex and takes 220 / 330 / 500 physical. Idle: sits bolt
upright, paws in lap, the Fold "Buddha sit". Cast: the sweep, right to left,
five frames, the arm fully extended on frame three. Art: grey, round face, ears
folded flat.

**Savannah** — tall. *Long Jump*: leaps **over** the target to the hex behind
it and kicks it **forward** — toward where Savannah came from — with **Force 2**
and 250 / 375 / 560 physical. From your side that is toward the Sink. Idle:
tall stance, tail up, ears swivelling. Cast: three-frame arc, land, mule-kick.
Art: spotted, huge ears, very long legs; the tallest silhouette on the counter.

**Midnight** — an all-black cat. *Absence*: becomes a shadow for 2.5 s —
untargetable and Planted, healing 20 / 28 / 40% max HP; enemies targeting it
retarget. Idle: only the eyes move; the body barely breathes. Cast: the sprite
drops to silhouette, the eyes stay. Art: drawn in the `void` ramp (blue-black)
so it separates from the `outline` spine; two yellow eyes are the only detail.

**Lykoi** — the werewolf cat. *Moonbite*: lunges at the target for 280 / 420 /
630 physical and heals for half of it; if the target is Spooked, Moonbite also
shoves it 1 hex. Idle: patchy fur ripples, ears back. Cast: a two-frame lunge
with the mouth open. Art: sparse black-grey roan with bald patches around the
eyes and muzzle — a deliberately ragged silhouette.

**Birman** — the sacred cat with white gloves. *White Gloves*: heals the two
lowest-HP allies within 3 hexes for 260 / 390 / 580 and grants them +20% Spell
Power for 5 s. Idle: sits with the paws neatly together so the gloves show.
Cast: both paws raised, two beams. Art: colourpoint with pure white paws, a long
cream coat, a medal.

### Cost 4

| Cat | Clan | Role | Wt | Reach | HP | Claw | Swipe | Fluff | Whisk | Mischief |
|---|---|---|---|---|---|---|---|---|---|---|
| Skog | Wild + Mythic | Loaf | H | 1 | 1000 | 70 | 0.60 | 50 | 50 | 0 / 100 |
| Caracal | Wild | Knocker | M | 1 | 850 | 85 | 0.80 | 30 | 30 | 0 / 80 |
| Bakeneko | Mythic | Hisser | M | 2 | 750 | 50 | 0.70 | 30 | 45 | 40 / 110 |
| Maneki | Mythic | Groomer | M | 2 | 800 | 50 | 0.65 | 35 | 45 | 30 / 100 |
| Cat Sìth | Void + Mythic | Pouncer | L | 1 | 800 | 85 | 0.90 | 25 | 40 | 0 / 70 |
| Mau | Show | Mouser | L | 4 | 700 | 80 | 0.95 | 20 | 30 | 0 / 60 |

**Skog** — a Norwegian Forest Cat; Freyja's cats pulled her chariot. *Frost
Coat*: a 500 / 750 / 1200 shield; while it holds, Skog and adjacent allies are
Planted and enemies that hit Skog lose 20% Swipe. Idle: snow-motes drift off
the coat. Cast: the coat frosts white from the tips inward over four frames.
Art: huge, long-haired, bushy tail; brown tabby with a dedicated `frost`
highlight on the tips.

**Caracal** — the ears. *The Slap*: a long wind-up and one slap: **Force 3** and
350 / 525 / 800 physical; collisions along the way deal double collision damage.
Idle: the ear tufts twitch independently. Cast: six frames of wind-up with the
ears flattening, one frame of slap, two of recoil — the longest anticipation in
the game, on purpose, so the victim's owner sees it coming. Art: tawny with
black ear tufts; the ears are the silhouette and stay 6 px tall.

**Bakeneko** — the yokai that walks upright and dances with a napkin on its
head. *Napkin Dance*: every enemy within 2 hexes is **yanked** 1 hex toward
Bakeneko and takes 300 / 450 / 700 magic. A yanked enemy that crosses a Hole
falls in. Idle: sits like a person; the tail is forked. Cast: stands up on two
legs, eight frames of dance, napkin flapping. Art: calico, forked tail, a red
napkin, a paper-lantern glow.

**Maneki** — the beckoning cat. *Beckon*: raises its paw — allies within 2
hexes heal 300 / 450 / 700 and the nearest enemy is yanked 1 hex toward Maneki.
Each cast has a 25% chance to drop a Treat at round end. Idle: the slow paw
wave, which is also the cast — the cast just holds it longer and glows. Art:
white with orange and black patches, red collar, gold bell, a gold coin in one
paw.

**Cat Sìth** — the fairy cat that steals souls. *Soul Steal*: leaps to the
lowest-HP enemy within 4 hexes for 320 / 480 / 720 magic; if it KOs, Cat Sìth
gains 10% of the victim's max HP for the rest of combat and refunds its
Mischief in full. A fall is not a KO for this purpose — there is no soul on the
floor. Idle: the white chest patch pulses. Cast: leap, and a small wisp leaves
the victim. Art: large black cat, white chest spot, green eyes, a 1-px
`fae-green` ring on the outline.

**Mau** — an Egyptian Mau, the fastest cat there is; bats **grapes**. *Blur*:
for 4 s, +80 / 100 / 140% Swipe and every attack ricochets to a second enemy
for 50%. Idle: crouched sprint-stance, the "worried" brow marking. Cast: the
sprite doubles — a one-frame afterimage in the `void` ramp trails it. Art:
silver with black spots, mascara lines, a gold ankh tag; drawn leaning forward.

### Cost 5

| Cat | Clan | Role | Wt | Reach | HP | Claw | Swipe | Fluff | Whisk | Mischief |
|---|---|---|---|---|---|---|---|---|---|---|
| Bastet | Mythic | Groomer | M | 2 | 950 | 60 | 0.70 | 40 | 60 | 50 / 150 |
| Cheshire | Void | Hisser | L | 2 | 800 | 50 | 0.70 | 30 | 50 | 30 / 100 |
| Chonk | Pampered | Knocker | H | 1 | 1200 | 90 | 0.55 | 50 | 40 | 0 / 120 |
| Schrödinger | Void | Loaf | H | 1 | 1000 | 60 | 0.60 | 45 | 55 | 0 / 100 |
| Wampus | Wild | Pouncer | M | 1 | 950 | 100 | 0.85 | 35 | 35 | 0 / 80 |

**Bastet** — the temple cat. *Nine Lives*: every ally gains a **Life**: the
next time it would die **or fall**, it instead returns to its start hex after
1.5 s with 40 / 55 / 100% HP. One Life per unit at a time; casts on a team that
already has them heal 300 / 450 / 900 instead. Idle: perfectly still; the gold
earring sways. Cast: eyes flare, nine points of light orbit her and settle on
allies. Art: sleek black-bronze, gold hoops, a sun-disc collar.

**Cheshire** — the trickster. *Vanish*: fades out, leaving only its grin on the
hex for 1.5 s, after which **the tile falls away — a Hole**. Cheshire reappears
on an empty hex adjacent to the enemy with the highest Claw; enemies within 1
hex are Spooked and take 300 / 450 / 900 magic. The first cast opens a hole on
*your* side; every cast after opens one beside their carry. Idle: the grin
stays fixed while the body sways behind it. Cast: body fades in four frames,
grin holds two, tile drops. Art: purple and pink stripes, an oversized grin in
a `tooth-white` that appears on nothing else.

**Chonk** — the wrecking ball. *Clear the Table*: a slow look at the camera,
then one sweep: every adjacent enemy is shoved **Force 2** for 400 / 600 / 1200
physical, and **each hex they were shoved from falls away as a Hole**. The
front line becomes a minefield for the next swat. Idle: breathing is the only
motion; a chin fold moves. Cast: the look is a four-frame hold, the sweep two
frames, and the tiles drop one after another. Art: grey, small ears set far
apart, the roundest silhouette on the counter, wall to wall on the canvas.

**Schrödinger** — a cat in a box. Passive *Superposition*: whenever it would
die or fall, flip a coin — heads, the box is back on its start hex with 50% HP.
Every time. *Box Time*: closes the lid: untargetable and Planted for 3 s, heals
25 / 35 / 50%; adjacent enemies are Spooked when the lid pops. Idle: a
cardboard box; two ears and eyes rise over the rim, then duck. Art: the one
sprite not shaped like a cat — a `cardboard` box with a "this side up" arrow,
and ears.

**Wampus** — six legs, yellow eyes, from the hills. *Six-Legged Rush*: dashes
to the Marked enemy (else the farthest) for 450 / 675 / 1350 physical; if the
target is KO'd, Wampus rushes again, up to three times. Every enemy it passes
through is shoved 1 hex **sideways**. Idle: six legs shift weight in an
impossible rhythm. Cast: a smear frame, impact, repeat. Art: shaggy dark brown,
glowing eyes, two extra pairs of forelegs — the only walk cycle in the game
with more than four feet to keep in phase.

---

## 6. Items

Three kinds: **Toys** (components that combine), **Placemats** (items you put
on a hex), and **Snacks** (consumables). Toys are the standard depth; placemats
are the thing only a board with tile states can have.

### 6.1 Toys — components

| Component | Stat |
|---|---|
| **Feather Wand** | +15 Claw |
| **Catnip** | +15 Spell Power |
| **Laser Pointer** | +12% Swipe |
| **Scratching Post** | +25 Fluff |
| **Cozy Blanket** | +25 Whiskers |
| **Cardboard Box** | +180 HP |
| **Bell Collar** | +15 starting Mischief |
| **Fish Bone** | combines with any of the above into an emblem |

### 6.2 Toys — combined (28)

Two components on one cat combine. Doubles first, then pairs.

| Recipe | Toy | Effect |
|---|---|---|
| Wand + Wand | **Feather Frenzy** | +35 Claw; +5 Claw per KO this combat |
| Catnip + Catnip | **Catnip Overdose** | +50 Spell Power; spells cast a second time at 40% |
| Laser + Laser | **Red Dot** | +20% Swipe; **+1 Reach**; the holder's target loses 15% Swipe — it's watching the dot |
| Post + Post | **Cat Tree** | +40 Fluff; attackers take 3% of their max HP as magic damage per hit; can't be crit |
| Blanket + Blanket | **Blanket Fort** | +40 Whiskers; heal 3% max HP every 2 s, doubled while Planted |
| Box + Box | **Box in a Box** | +500 HP; **one weight class heavier** |
| Bell + Bell | **Jingle Bells** | +20 Mischief; after each cast refund 20% of max Mischief |
| Wand + Catnip | **The Zoomies** | +20 Claw, +20 SP; after casting, +40% Swipe and +1 Zoom for 3 s |
| Wand + Laser | **Chase** | +15 Claw, +15% Swipe; +4% Swipe per attack on the same target, up to 10 |
| Wand + Post | **Claws Out** | +15 Claw, +20 Fluff; attacks shred 30% of the target's Fluff for 5 s |
| Wand + Blanket | **Under the Blanket** | +15 Claw, +20 Whiskers; heal 20% of attack damage; once per combat at 40% HP, a 25% shield for 5 s |
| Wand + Box | **Hackles Up** | +15 Claw, +200 HP; below 60% HP gain +25% Claw and +20% Swipe for the rest of combat |
| Wand + Bell | **Bell on a String** | +15 Claw, +15 Mischief; attacks grant +5 extra Mischief |
| Catnip + Laser | **Static Fur** | +15 SP, +15% Swipe; every 3rd attack arcs 120 magic to 3 nearby enemies and Spooks them 1 s |
| Catnip + Post | **Hairball** | +15 SP, +20 Fluff; spell damage applies *Hairball* 5 s: 1% max HP true damage/s and −50% healing |
| Catnip + Blanket | **The Look** | +15 SP, +20 Whiskers; enemies within 2 hexes lose 30% Whiskers and take 100 magic when they cast |
| Catnip + Box | **Sun Puddle** | +15 SP, +200 HP; a 25% HP shield for the first 8 s; when it ends, +40 SP |
| Catnip + Bell | **Slow Blink** | +15 SP, +15 Mischief; +30 SP every 5 s of combat, stacking |
| Laser + Post | **Sharpened** | +15% Swipe, +20 Fluff; each attack dealt or taken +1% Claw and SP, to 25 stacks; at 25, +25 Fluff and Whiskers |
| Laser + Blanket | **Night Vision** | +15% Swipe, +20 Whiskers; after casting, +50% Swipe for 4 s and +25 magic per attack |
| Laser + Box | **Toe Beans** | +15% Swipe, +200 HP; +10% Swipe per 20% HP missing, to +40% |
| Laser + Bell | **Nimble** | +15% Swipe, +15 Mischief; for the first 10 s the holder can't be Spooked or displaced |
| Post + Blanket | **Winter Coat** | +20 Fluff, +20 Whiskers; +15 of each per enemy targeting the holder |
| Post + Box | **Perch** | +20 Fluff, +250 HP; **on an edge hex the holder is Planted** and gains +40 Fluff. Cats love a ledge |
| Post + Bell | **Won't Move** | +20 Fluff, +15 Mischief; once per combat at 40% HP: a 30% shield, 20 Mischief and Planted for 4 s |
| Blanket + Box | **Purr** | +20 Whiskers, +250 HP; every 5 s adjacent allies heal 6% max HP |
| Blanket + Bell | **Window Seat** | +20 Whiskers, +15 Mischief; starting in the back two rows: +20 SP and 10 Mischief every 3 s; front two: +40 Fluff and Whiskers and +5 Mischief when hit |
| Box + Bell | **Group Nap** | +200 HP, +15 Mischief; at combat start allies within 2 hexes get a 250 shield for 8 s (scales with stage) |

**Fish Bone** makes emblems — a cat gains the trait and counts toward it:
Wand → **Knocker** (a torn bandana), Catnip → **Hisser** (a spray-bottle
scar), Laser → **Pouncer** (a hunting tag), Post → **Loaf** (a bread tag),
Blanket → **Groomer** (a hairbrush), Box → **Void** (a black ribbon), Bell →
**Show** (a rosette). Fish Bone + Fish Bone → **Extra Bowl**: +1 cat on the
counter. Alley, Pampered, Wild, Mythic and Mouser emblems appear only on the
Cat Distribution System.

### 6.3 Placemats

A placemat is placed on one of your 27 hexes during planning and stays there
across rounds. Its effect applies to **whoever stands on it** — an enemy that
advances onto your side is standing on your mats. If the hex becomes a Hole,
the mat is gone for that round.

| Placemat | Effect |
|---|---|
| **Anti-Slip Mat** | the unit on it is **Planted** |
| **Sunbeam** | the unit on it heals 3% max HP/s and has +20 SP |
| **Scratch Pad** | the unit on it has +15% Swipe and its attacks shred 20% Fluff |
| **Water Bowl** | the six hexes around it are **Wet** at combat start and again every 8 s |
| **Loose Tile** *(rare)* | the first unit to step onto it — friend or foe — falls through; once per combat |

### 6.4 Snacks

| Snack | Effect |
|---|---|
| **Copycat Mirror** | duplicates a 1★ or 2★ cat |
| **Catnip Spray** | three free shakes of the bag |
| **Lint Roller** | take a cat's toys back |
| **Toy Swap** | reroll your held components |
| **Loaded Treat Bag** | the next three shops favour the traits of a chosen cat |

---

## 7. Animation and art

### 7.1 One clip set for every cat

Every cat has the same named clips, so the animator, the gates and the content
list treat 34 cats as one shape:

| Clip | Frames | Driven by | Notes |
|---|---|---|---|
| `idle` | 4 | time | the personality clip: blink, tail, ear, breath. Each cat's is different and listed in §5 |
| `walk` | 4 | **distance** | contact / passing / contact / passing, as the Platformer hero does it — the legs stop when the cat is blocked |
| `attack` | 4 | time | wind-up, strike, recover, return. Mousers: line up, flick, follow-through, return |
| `cast` | 6–8 | time | the signature. Holds are **durations**, never duplicated frames — `check-anim` fails a duplicate |
| `hit` | 1 | event | a flinch frame plus a white tint flash (`SpriteSheet.Tint` already exists for exactly this) |
| `fall` | 2 | event | the scrabble pose; the slide, clip and thump are code and shared VFX |
| `ko` | 3 | event | flop, lie, get up and pad off the counter's edge. Cats are fine. Nobody dies on a kitchen counter |
| `loaf` | 1 + breath | state | Loafs only: the bread pose while *loafing* |
| `bench` | 2 | time | sitting on the windowsill; the shop card uses frame 1 |

Shared VFX, not per cat: splash, tile-falls-away, dust puff, spotlight circle,
the Mark paw print, the Spooked `!!`, shield ring, Wet shimmer (a Bayer dither
over the tile, per STYLE.md's translucency rule), the crack overlay, the Hole
tile showing the cabinet interior, star tags, the moon for Moon Omen, and
Closing Time's approaching hand.

### 7.2 Budget, honestly

34 cats × 25 frames is about **850 frames of 32×32**, plus tiles, VFX, toys,
UI and a font. In this repo's `.pix` front-end a four-frame clip is one text
grid; that is on the order of 240 strips. It is the long pole of the whole
project by a wide margin, and it should be staged:

1. **Playable**: `idle` (2 frames), `walk`, `attack` for all 34, shared VFX,
   tiles, one font. ~300 frames. The game is fully testable here.
2. **Personality**: the 34 `cast` clips. ~240 frames. This is where the
   uniqueness requirement is actually spent, and it is worth doing cat by cat
   as each spell is implemented, not as a batch.
3. **Polish**: `hit`, `fall`, `ko`, the full 4-frame idles, `bench`.

Rules that carry over unchanged from `assets/STYLE.md`: 32×32, soles on row
31, outline in `#1A1A1A`, one facing drawn and mirrored at draw time (which is
only legal because light comes from above with no left/right bias — keep it
that way), binary alpha, integer scale only.

### 7.3 Palette — *Retro Kitchen*

One palette, `Content/sprites/palette.gpl`, sharing only `outline` with the
rest of the repo. Direction: **a 1970s kitchen** — avocado counter edge,
cream ceramic tile, harvest-gold formica for the shop, chrome sink — with the
cats painted in real fur colours on top. The kitchen is muted so that 34 cats
in six fur ramps carry the colour.

Ramps to build (names, not hex values — measure and build them with
`extract-palette` from a concept image, then let `check-palettes` judge):

| Group | Ramps | Notes |
|---|---|---|
| Fur | `orange` ×3, `grey` ×3, `cream` ×3, `tabby-brown` ×3, `seal` ×3, `void` ×3 | `void` is a blue-black **above** the outline's lightness so black cats separate from their own outline; the darkest `void` step must sit ≥ 3 Oklab L above `#1A1A1A` |
| Skin and features | `pink` ×2, `eye-yellow`, `eye-green`, `eye-blue`, `tooth-white` | `tooth-white` is Cheshire's grin and nothing else |
| Kitchen | `tile-cream` ×3, `grout`, `avocado` ×2, `steel` ×3, `cabinet-dark` | `cabinet-dark` is what you see through a Hole |
| UI | `formica-gold` ×3, `chrome` | the shop frame and windowsill |
| FX | `water`, `spark`, `frost`, `fae-green` | `frost` is Skog's tips and Frost Coat; `fae-green` is Cat Sìth's ring |
| Accent | `mark-red` | the Wild paw mark, the Spooked `!!`, Bosun's neckerchief. Rationed: it means "gameplay" |

That is roughly **38 colours**, the largest palette in the repo (Platformer has
23). Six fur ramps of three steps each is the minimum that keeps a Persian,
a Ragdoll and a Birman apart, and `check-palettes`' collision rule (two colours
in one hue family within 2 Oklab L) is the constraint that will bite: plan the
`cream` and `tile-cream` ramps together so they interleave rather than collide,
or fold them into one ramp and let the tiles borrow it.

**Silhouette is the redundant channel.** Puzzle's gems differ in shape as well
as hue for the 8% of players who cannot tell red from green; 34 cats need the
same discipline more than six gems did. Each roster entry names its silhouette
— round (Marmalade, Chonk), tall (Savannah), long (Bengal), wide (Bodega,
Moose), ragged (Lykoi), upright (Fold, Bakeneko), a box (Schrödinger) — and
two cats in the same clan should never share one.

### 7.4 Board art

`tile.pix` (48×40), `tile-cracked.pix`, `tile-hole.pix`, `tile-wet` as a
dither overlay, `sink.pix` covering two hexes, and the counter's rounded lip
under the perimeter as part of a static backdrop rather than per-tile
variants. Placemats are 32×24 sprites centred on their tile. Toys are 12×12
icons. The Cat Distribution System is a turntable drawn once and rotated by
stepping its sprite through eight 45° frames — the wheel trick from the car
in `assets/experiments/car/`.

### 7.5 Sound

Hisses and thumps, mostly. Purchase: *mrrp*. Fall off the edge: a soft pad
landing from below, pitched by weight. Sink: splash. Tile falls away: crockery
on a hard floor. Cast: each cat's own vocalisation. Use `SoundManager`'s pitch
and pan on every one-shot — Rhythm's click taught that a one-shot fired at one
pitch dead centre fuses into a tick, and combat here fires dozens a second.

---

## 8. Building it in this repo

### 8.1 A tenth sample, not a rewrite

`scripts/new-sample.sh KnockItOff` scaffolds it with the palette, `.pix`
path, skinned title screen and gates already wired. Keep `AutoBattler` as it
is: it is the minimal reference cited across CLAUDE.md and FINDINGS for the
typed `EventManager`, drag-to-place and the Shop → Combat → PostCombat state
graph, and those three are exactly what this game copies out of it. What it
does not copy is the 0.5 s combat tick — see §8.3.

### 8.2 Library seams this game opens

- **`Rendering.HexGrid`** — pure statics beside `GridMath`: offset ↔ axial ↔
  cube, the six directions, distance, ring, line, `Mirror`, the closest-
  direction rule from §2.2, pixel ↔ hex for a pointy-top grid with a squashed
  height, and `TryMouseToHex`. Testable without a `GraphicsDevice`, like
  `ScreenScaler.Fit` is. Also **`HexGrid.Slide(start, dir, steps, isBlocked, isHole)`**
  returning the path and how it ended — the *geometry* of §2.3 in the library,
  the *rules* (weight, Planted, Cracked) in the game, so the part that is hard
  to get right on a hex grid has a unit test the sample cannot have (Tests
  references the library and tools, never a sample).
- **An animator.** FINDINGS §9.2 revised the trigger to "restore one when
  something needs a cycle" and warned that `.pix` would resist the authoring.
  This is 34 somethings, and the cycles are **time-driven** (idle, attack,
  cast) as well as distance-driven (walk) — the second pattern the Platformer
  hero could not show. Shape: `Clip { Texture, FrameWidth, int[] FrameMs, Loop }`
  and `Animator { Play, Update(dt), Advance(distance) }`, drawn through
  `PixelDraw.Frame`. Both drivers, one type.
- **Frame durations as data.** A PNG strip carries none, `check-anim` fails a
  duplicated frame, and a cast with a held stare (Tom, Chonk, Caracal) needs
  holds. Proposal: an optional `durations 6,6,2,2,10` directive in `.pix`,
  which `render-pix` writes to `<name>.anim.json` beside the PNG. The `.pix`
  stays the single source, the PNG stays a pure artefact, `check-anim`'s rule
  stays as it is, and a missing directive means "uniform" — so every existing
  strip is unaffected.

### 8.3 The combat sim

Real time, not turns: Swipe speeds of 0.55–0.95/s, slides at 0.12 s/hex,
30-second rounds. Run it on a **fixed 50 ms tick** with rendering interpolated
between ticks, units processed in a stable order, and every random draw from a
seeded `Random` handed in at construction — `Camera2D` takes one for the same
reason. Then the sim has no dependency on a texture, a window or a clock, and:

- **An auto-battler is the one genre whose core loop needs no input.**
  FINDINGS §13's problem — the smoke harness launches games but cannot drive
  them — does not apply to combat here. A `--sim <seed> --rounds N` flag on the
  sample that runs N combats headless and prints the outcomes is the balance
  pass, and it runs on a headless CI runner where the smoke harness cannot.
- Every mechanic in §2 becomes a scripted scenario: place these cats here,
  run the sim, assert who fell. That is where the design's numbers get their
  second pass.

### 8.4 Gates that already apply

`check-palette-all`, `check-palettes`, `check-pix-all`, `check-anim-all` (the
`frames N` directive makes each clip a checked strip; the soles report will
say "no" for `fall`, `ko` and most `cast` frames, which is correct and not a
failure), `check-sprites`, `check-boot`, `lint-all-samples`. Nothing new is
needed to keep the art legal. `describe-image` is step 0 for any cat that
starts as a generated concept, and `compare-sprite` is how a 2★ variant is
checked against its 1★.

### 8.5 Effort

Systems — hex board, sim, displacement, 34 spells, 12 traits, 40 items,
economy, PvE, UI — is a few weeks of focused work for one person on top of
what `AutoBattler` already proves out. Art is the long pole (§7.2). The
recommended order is §7.2's: get to "playable" with two-frame idles and
placeholder tiles, and spend the cast animations one cat at a time as each
spell lands, so that every session ends with one more cat that is *finished*.

---

## 9. Balance knobs and open questions

**Knobs, in the order they will need turning:**

1. Collision damage (8%) and whether Force-1 shoves at cost 1 are too cheap.
2. Void re-emergence HP (50 / 70 / 100%) — the anti-knock comp must lose to
   a full-commit Knocker board, not blank it.
3. Closing Time cadence (5 s per ring) — fast enough to end fights, slow
   enough to read.
4. Pouncer leap-behind vs the Sink: a Pouncer that starts behind an enemy
   already at the edge is one swat from the floor itself.
5. The Cheshire first-cast hole on your own side — a feature if the tension
   is fun, a bug if it is only a tax.

**Decisions taken here, easy to reverse:**

- **The name.** *Knock It Off* is the working title. *Off the Table* and
  *Counter Cats* are the alternates; the first is the funniest, the second the
  clearest.
- **A kitchen counter, not rooftops.** Moonlit roof tiles with a chimney as
  the hole was the other coherent fiction. The counter wins because knocking
  things off tables is the cat joke everyone already knows, and the game is
  that joke.
- **Falls do not trigger on-death effects.** It gives falls an identity
  separate from kills and makes Cat Sìth care how a target leaves.
- **7×8 with a two-hex Sink**, rather than a hex-shaped or larger board.
  Auto chess players know 7×8; the Sink is the smallest hole that is
  point-symmetric.
- **Components that combine**, rather than standalone items. It is the deeper
  system and the one the audience expects; the placemats are the part that is
  new.
- **34 cats, twelve traits.** Enough for eight players to build different
  boards; small enough that every cast animation can be a real one.
