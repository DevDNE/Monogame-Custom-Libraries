# Findings — Library Review from Building Sample Games

Original snapshot: 2026-04-18. Updated 2026-04-18 (a) — four non-deferred §6 items landed in commit `702dd54`; their findings below are annotated as **Fixed**. Updated 2026-04-18 (b) — BattleGrid expanded from a tactical-grid stub into a real game with chip selection, enemy AI patterns, and a full HUD. Updated 2026-04-18 (c) — **Shooter** sample added (twin-stick arena survival); first real load test for `ObjectPool`, `TimerManager`, and `Camera2D.ScreenToWorld`. Updated 2026-04-18 (d) — **Puzzle** sample added (match-3 cascade); first real consumer of `TileMap` + `TileLayer<T>`. Surfaced a SpriteFont charset footgun (§1.10). Updated 2026-04-18 (e) — **Roguelike** sample added (turn-based dungeon crawler with procedural generation); second `TileMap` consumer + hand-rolled turn scheduling. Updated 2026-04-18 (f) — **TowerDefense** sample added; second `ObjectPool` consumer and first mouse-driven grid-cell placement interaction. Updated 2026-04-18 (g) — **Rhythm** sample added; first real `Audio.SoundManager` consumer and first content-pipelined audio asset. Updated 2026-04-18 (h) — **VisualNovel** sample added; first real `Persistence.SaveSystem` consumer, first real `Tween<T>` consumer, and surfaced a namespace/class collision bug (§1.14). Updated 2026-04-18 (i) — **AutoBattler** sample added; first real `EventManager` typed-API consumer, most complex state machine (Title/Shop/Combat/PostCombat), hand-rolled BFS pathfinding. **§8 fully revised with 9-game data.** Updated 2026-04-18 (j) — **§8 Suggested Execution Order completed end-to-end**: Tween namespace renamed to `Tweening`; `TileLayer.Swap` + `TileMap.TryWorldToCell` + `GridMath.TryMouseToCell` added; `SoundManager.PlaySoundEffect` now logs on unloaded names. Tier B deletions landed: `Core.Entity` (+migrated 4 BattleGrid subclasses), `SpriteSheet.Animated`, `Utilities.MathUtilities`, `Debugging.PerformanceMonitor`; `Timing.Timer` constructor made internal. Tier D extractions landed: `Lifecycle.TitleScreenState` (all 9 games migrated, ~720 lines collapsed), `UI.HpBar` (4 consumers migrated), `UI.LogBox` (Roguelike + AutoBattler migrated; BattleGrid stays on `TextManager`), `Pooling.PooledEntitySet<T>` (Shooter + TowerDefense migrated). Tests: 100 → 122 passing. All items in §8 Tier A / Tier B / Tier D #1-5 are now ✅ Done.

## Context

The framework has been exercised by **nine sample games** in deliberately different genres:

1. **`MonoGame.GameFramework.BattleGrid`** — real-time grid duel inspired by Mega Man Battle Network. 3×3 grid per side, WASD movement, space-bar buster, Tab-triggered chip selection from a pool of four (Cannon / Wide Shot / Sword / Recov), enemy AI alternating between movement and two attack patterns, HP bars + controls hint HUD.
2. **`MonoGame.GameFramework.Platformer`** — side-scrolling platformer with gravity, AABB collision, camera follow, a patrolling enemy, goal + win state, and a title screen.
3. **`MonoGame.GameFramework.Shooter`** — top-down twin-stick arena survival. Free WASD movement in a 2400×1600 arena, mouse-aim firing with a 0.15 s cooldown, pursuit-AI enemies spawning in pairs every 1.5 s, score + HP HUD, game-over + R-to-restart.
4. **`MonoGame.GameFramework.Puzzle`** — match-3 gem board. 7×7 grid of colored gems, click two adjacent cells to swap, matches of 3+ in a row or column clear with gravity-pack and refill cascades, score counter, R to reshuffle.
5. **`MonoGame.GameFramework.Roguelike`** — turn-based dungeon crawler. Procedurally-generated rooms-and-corridors maps (60×34 `TileLayer<TileKind>`), bump-to-attack combat on a grid, monsters take a turn after every player action, stairs descend to a freshly generated deeper level. No FOV or inventory in MVP.
6. **`MonoGame.GameFramework.TowerDefense`** — wave-based tower defense on a 20×14 grid. Fixed S-shaped enemy path, click-to-place tower (20 gold), three waves of 5/8/12 enemies, 5 lives, victory on clearing wave 3.
7. **`MonoGame.GameFramework.Rhythm`** — 4-lane rhythm game with a hard-coded 30-second chart, ±50 ms / ±150 ms hit windows, combo counter, and a click sound on every hit. First audio consumer.
8. **`MonoGame.GameFramework.VisualNovel`** — 11-node dialogue graph with one 3-way choice branching to three endings, char-by-char text reveal (tweened), save/load via `SaveSystem`, Continue button on title screen.
9. **`MonoGame.GameFramework.AutoBattler`** — auto-chess with shop/combat/post-combat phases, 3 unit types with implicit rock-paper-scissors, drag-to-place during shop, hand-rolled BFS pathfinding during combat, hero-HP attrition across rounds.

All nine are playable end-to-end. The library is exercised by 9 distinct consumers across 9 distinct genres; the unused-surface analysis below draws on that full set.

---

## Library surface coverage

**Validated by all five games:**
- `Input.KeyboardManager`
- `Rendering.Primitives` — universal pattern for colored rectangles
- `Rendering.SpriteSheet.Static` + `SpriteSheet.Tint` — title-screen buttons in all five
- `Lifecycle.GameState` + `Lifecycle.GameStateManager` (auto-lifecycle)
- `UI.UIManager` — hit-testing + `OnClick` + `HoveredElement` for title buttons

**Validated by four games:**
- `Input.MouseManager` — all except Roguelike (keyboard-only by design)

**Validated by three games (3 of 5):**
- `Rendering.DrawManager` — BattleGrid + Platformer use it for world draws; Puzzle, Shooter, Roguelike bypass and draw directly. Now a minority pattern — only 2 of 5 games. `DrawManager` is optional, not essential.
- `Rendering.TileMap` + `TileLayer<T>` — Puzzle (7×7 gems), Roguelike (60×34 dungeon), and conceptually fits BattleGrid's hand-rolled grids. Fully validated.

**Validated by two games:**
- `Rendering.Camera2D` — Platformer (side-scrolling follow) + Shooter (twin-stick follow + `ScreenToWorld` for mouse aim).

**Validated by one game:**
- Shooter: `Pooling.ObjectPool<T>` (both Projectile and Enemy), `Timing.TimerManager.Every`, `Camera2D.ScreenToWorld`
- Puzzle: `TileMap.WorldToCell` (mouse-pick → grid cell)
- Roguelike: `TileLayer.Fill`, `TileMap.GetCellRect` for rendering at scale, `TileLayer<TileKind>` with an enum cell type. Also hand-rolled a tiny `TurnScheduler` pattern inline (see §1.11).
- BattleGrid: `Events.EventManager` (string API), `Lifecycle.SceneManager`, `Persistence.SettingsManager`, `Text.TextManager` (tilde-console log), `Input.GamePadManager`
- Platformer: `Camera2D.FollowLerp` with snap-on-respawn

**Not used by any game (still speculative after 5):**
- `Audio.SoundManager` — not a single game has played a sound
- `Content.AssetCatalog` — no game has >1 content asset
- `Debugging.ILogger` / `ConsoleLogger` / `PerformanceMonitor`
- `Persistence.SaveSystem` / `SaveFile<T>`
- `Tween.Tween<T>` / `Easing`
- `Utilities.MathUtilities`
- `Events.EventManager` typed API (`Subscribe<T>`/`Publish<T>`)
- `Timing.Timer` (raw class)
- `Core.Entity` — only BattleGrid still subscribes; Platformer, Shooter, Puzzle, Roguelike all skip it.
- `SpriteSheet.Animated` — five games, zero consumers.

After five games the unused set is ~10 primitives, stable across the last two data points. That set is the basis for §8's delete/keep recommendations.

---

## §1 — Shared patterns across both games (library candidates)

These would be added to the core library, not a genre module, because both games (and likely any future game) hit them.

### 1.1 `GameStateManager` lifecycle hook calls are manual
Both games push/change states and then call `Entered()` (and sometimes `Leaving()`) explicitly. The library's `PushState`/`PopState`/`ChangeState` only manipulate the stack — they don't fire lifecycle methods. Every consumer reinvents the same two-line ceremony:

```csharp
_gameStateManager.PeekState().Leaving();
_gameStateManager.ChangeState(nextState);
_gameStateManager.PeekState().Entered();
```

**Recommendation**: `GameStateManager.PushState(state)` should call `state.Entered()`. `PopState()` should call `Leaving()` on the popped state and `Revealed()` on the new top. `ChangeState` should call both. Consumers keep the option to call them manually for exotic flows, but the default should Just Work.

> **✅ Fixed 2026-04-18 (commit `702dd54`)**. `Push/Pop/ChangeState` now auto-fire `Entered`/`Leaving`/`Obscuring`/`Revealed`. Initial push on an empty stack fires `Entered` only (no `Revealed` — nothing to reveal from). Demo's `BattleState` migrated its `Revealed` debug-state push into `Entered`. Platformer's Play-click callback collapsed from four lines to one. Covered by 5 new `GameStateManagerTests`.

### 1.2 Pixel texture for colored rectangles
Both games need a 1×1 white texture for drawing colored rectangles. Platformer uses it ubiquitously (player, platforms, enemies, goal, overlays); tactical could use it but relies on pre-made sprites. Every game creates it the same way:

```csharp
_pixel = new Texture2D(GraphicsDevice, 1, 1);
_pixel.SetData(new[] { Color.White });
```

**Recommendation**: `Rendering.Primitives` or similar — a lazy-initialized `PixelTexture` service keyed to the current `GraphicsDevice`, plus convenience `DrawRectangle`/`DrawLine` extension methods on `SpriteBatch`. Low effort, eliminates copy-paste.

> **✅ Fixed 2026-04-18 (commits `702dd54` + follow-up)**. `Rendering/Primitives.cs` provides static `Initialize(GraphicsDevice)` + `Pixel` + `DrawRectangle(spriteBatch, rect, color)`. Adopted in the follow-up commit by the platformer (`Game1`/`PlayState`/`TitleState`) and the demo's `ConsoleUI`. The demo adoption also cleaned up a dead `GraphicsDevice` parameter chain through `BattleState` → `DebugState` → `ConsoleUI`. No unit tests (GraphicsDevice isn't headless-friendly); smoke-tested via both sample games.

### 1.3 Screen-space vs world-space rendering
Platformer has two draw contexts per frame: world (camera matrix) and UI (identity). The library provides `Camera2D.GetViewMatrix` but no guidance or helpers for mixed-context rendering. The tactical demo has one context because it uses no camera. Any future game with a camera will hit this.

**Recommendation**: Either document the two-pass pattern explicitly in CLAUDE.md, or ship a `Renderer` wrapper that manages world-vs-UI Begin/End. Probably the former — a wrapper is premature until more games show the same friction.

### 1.4 Input remapping is absent and both games pretend it's fine
Both games hardcode WASD/Arrows/Space. Any serious consumer will want rebinding. This is already on the Tier 3 backlog.

### 1.6 `ObjectPool<T>` works, but the ceremony repeats per game (NEW 2026-04-18c)
Shooter exercises `ObjectPool<Projectile>` and `ObjectPool<Enemy>` under real load (dozens of rent/return per second during combat). The pool itself is fine. What *does* repeat is the surrounding pattern:

```csharp
// Rent → Launch/Spawn → add to live list
// every frame: Update + cull dead → Return + remove from live list
private readonly ObjectPool<T> _pool;
private readonly List<T> _live = new();

// Rent:
T x = _pool.Rent();
x.Launch(...);
_live.Add(x);

// Cull:
for (int i = _live.Count - 1; i >= 0; i--) {
  _live[i].Update(dt);
  if (!_live[i].Alive) {
    _pool.Return(_live[i]);
    _live.RemoveAt(i);
  }
}
```

That block appears twice in Shooter's `PlayState` already. Any game with more entity types would repeat it per-type.

**Recommendation**: a thin `PooledEntitySet<T>` wrapper that combines pool + live-list + per-frame Update-and-cull using an `Alive` convention (or `Func<T,bool> isDead`). ~30 lines. Would delete ~20 lines per consumer entity type. Low-risk library addition after one more game confirms the pattern.

### 1.7 `TimerManager.Every` fits spawn waves well (NEW 2026-04-18c)
Shooter's enemy spawn uses `_timers.Every(1.5f, SpawnWave)` — exactly the shape the API was designed for, and a genuine one-line win over a hand-rolled accumulator. Confirms that `TimerManager` as-is is valuable *when* the pattern is "fire callback every N seconds", even if BattleGrid's per-frame countdowns don't fit.

(BattleGrid's four hand-rolled float counters from §5 still argue for a separate simpler API — the two styles serve different needs. Recommend keeping `TimerManager.Every`/`After` and adding a `CountdownTimer` struct that wraps `float remaining; bool Tick(dt)` for the common case.)

### 1.11 Turn scheduling: the library primitive doesn't fit; the hand-rolled one is trivial (NEW 2026-04-18e)
Roguelike needed a turn scheduler. The obvious library candidate — `Timing.TimerManager` — doesn't fit at all: timers are wall-clock and fire callbacks; turn scheduling is "after the player acts once, let each monster act once". They have different shapes.

What Roguelike ended up writing is ~10 lines in `PlayState.MonstersTurn`: a plain `foreach monster in list { ... }` loop that runs exactly once after each successful player action. BattleGrid's enemy AI tick is structurally similar (an action alternator gated by a timer). The shared pattern is *"after event X, run a one-shot pass over a set"* — not worth a library primitive.

**Recommendation**: do **not** add a `TurnScheduler` to the library. The per-game loop is 10 lines and wildly different in detail between BattleGrid and Roguelike. Two data points that look superficially similar but don't actually share code — the opposite of a library candidate.

### 1.12 Procgen lives in the game, not the library (NEW 2026-04-18e)
Roguelike's `DungeonGenerator` is ~80 lines of rooms-and-corridors carving. It operates entirely on `TileLayer<TileKind>` via the existing `Fill`/indexer/`InBounds` API. Nothing about the generator would transfer to another genre — even a second roguelike would likely want different generation (BSP, cellular automata, hand-authored prefabs) depending on its feel. The `TileLayer<T>` API is the right place to stop; generators stay per-game.

### 1.13 Scrolling text log is a third hand-rolled HUD pattern (NEW 2026-04-18e)
BattleGrid, Shooter, and now Roguelike all hand-roll some variety of scrolling text panel: BattleGrid's console, Shooter's last-event line, Roguelike's combat log with fade. Three games, three implementations, all ≤30 lines. The library has `Text.TextManager.ScrollText` for something like this — none of the games used it (it's coupled to the handle-based text model, which no game other than BattleGrid uses). A simple `LogBox` widget that takes (rect, font, max lines, fade style) would collapse ~90 lines across 3 games. Not an urgent extraction.

### 1.9 `TileMap` + `TileLayer<T>` handle match-3 well; a few sharp edges (NEW 2026-04-18d)
Puzzle's `Board` is the first real `TileMap` consumer. Verdict: the core API holds up, but there are sharp edges.

**What worked:**
- `TileLayer<Gem>` with a color-enum cell type is exactly the shape puzzle wanted. No wrapper needed.
- `Map.GetCellRect(c, r)` for rendering and `Map.WorldToCell(mousePos)` for click-pick are ergonomic both directions. Zero friction for the 80% case.
- `TileLayer.InBounds(c, r)` reads naturally in guard clauses.
- `Map.Origin` cleanly offsets the whole board to arbitrary screen coordinates without forcing a wrapper class.

**Sharp edges:**
- `Gems[c, r]` via a cached `TileLayer<Gem>` reference is what you want; `Map.GetLayer<Gem>("gems")[c, r]` is verbose. The usage pattern (cache once in ctor) isn't obvious — document it.
- `WorldToCell` returns a `(int, int)` tuple and doesn't clamp. Negative values and out-of-bounds come back silently. Puzzle added the bounds check itself. A `TryWorldToCell(Vector2, out int col, out int row)` that returns `bool` would be safer.
- Swap-two-cells is one of the most common grid ops; Puzzle hand-rolled a tuple-deconstructed swap. `TileLayer<T>.Swap((c,r), (c,r))` is a 3-line helper.

**Recommendation**: add `TileLayer.Swap`, `TileMap.TryWorldToCell`, and a CLAUDE.md note about caching the `GetLayer<T>` reference. Very low-effort, all backward-compatible.

### 1.10 SpriteFont default charset is a footgun for user-facing strings (NEW 2026-04-18d)
Every sample's content pipeline declares the Arial spritefont with `CharacterRegion Start=U+0020 End=U+007E` (ASCII printable). Any drawn string containing a character outside that range — em-dash `—`, curly quotes, non-breaking space, accented letters, anything pasted from a designer — crashes `SpriteFont.MeasureString` with `ArgumentException: Text contains characters that cannot be resolved by this SpriteFont.`

Puzzle hit this when a non-matching swap showed "No match — reverted" (the em-dash silently made it into the source from editor autocorrection or paste). One-character ASCII fix, but the defensive surface is real: every developer writing flavor text is one copy-paste away from a runtime crash on a rarely-visited UI path.

**Two workable library responses:**
1. **Widen the default spritefont charset** to include U+0020..U+00FF (Latin-1 supplement) plus common typographic punctuation (en-dash, em-dash, curly quotes, ellipsis). Template the Content.mgcb + spritefont when a new game is scaffolded. Catches 95% of paste-from-anywhere failures at the font-build step, not at runtime.
2. **Add a safe-draw helper** in `Text.TextManager` / a `Text.SafeFont` wrapper: `Measure`/`Draw` filter unknown glyphs down to `?` or a DefaultCharacter instead of throwing. Quieter failure mode for the remaining 5%.

Both are worth doing; (1) has higher leverage. Worth flagging in CLAUDE.md regardless, as a "things that will bite you" note.

> **✅ Fixed 2026-04-18 (post-AutoBattler)**. Rhythm's title screen hit the same class of bug with a second em-dash, which prompted executing Tier A item #1 from §8. All 9 games' `Content/fonts/Arial.spritefont` now declare seven `CharacterRegion` blocks covering U+0020..U+007E (ASCII), U+00A0..U+00FF (Latin-1 Supplement), U+2013..U+2014 (en/em-dash), U+2018..U+201D (curly quotes), U+2022 (bullet), and U+2026 (ellipsis). Rhythm's "4-lane rhythm — hit notes..." and AutoBattler's "Round {n} — COMBAT" text now render correctly without string edits.
>
> **Gotcha caught while fixing this (NEW 2026-04-18, §1.15)**: MonoGame's content pipeline incremental cache will silently skip rebuilding a spritefont `.xnb` when the source `.spritefont` XML changes shape but preserves the expected schema. `dotnet build` reports success without a `Building Font …` log line, and the cached `.xnb` continues to only rasterize the old charset. First attempt at this fix shipped, built green, and still crashed at runtime for exactly that reason. Workaround: delete `Content/bin` and `Content/obj` in each game before rebuilding. Longer-term: flag this in CLAUDE.md, or add a `dotnet build /t:Rebuild` note to the project readme.
>
> **Regression guard added 2026-04-19**: `mgf-tools check-content-cache-all` now flags any `.spritefont` whose mtime is newer than its compiled `.xnb` under `Content/bin/<platform>/Content/`. Catches this exact class (build green, `.xnb` stale, runtime crash) before `dotnet run`. Companion `mgf-tools check-boot-all` regex-verifies the four Game1.cs boot conventions (`Primitives.Initialize`, DebugOverlay DI + `SetFont`, `!overlay.ShouldSkipUpdate` guard, `SmokeHarness.Tick()` + `Exit()`); `check-versions` guards against cross-csproj `TargetFramework` / package-version drift. None add to the library; they live in `src/MonoGame.GameFramework.Tools/` alongside the spritefont linter.

### 1.16 `GameStateManager.Update`/`Draw` threw when a state mutated the stack (NEW 2026-04-18, post-AutoBattler)

A genuine library bug, not a content-pipeline quirk. AutoBattler's `CombatState.Update` calls `_onCombatEnded(winner)` when a side is eliminated, and that callback calls `_gameStateManager.ChangeState(_postCombatState)`. `ChangeState` pops and pushes on the internal `Stack<GameState>`, but `Update` was iterating the same stack with `foreach` — which throws `InvalidOperationException: Collection was modified after the enumerator was instantiated`.

Three previous samples hit this pattern coincidentally without crashing (BattleGrid's `PlayState.Entered` pushes a DebugState; the platformer transitions on a button click — both from outside the `Update` iteration). AutoBattler was the first to transition *from within* a state's own `Update`.

**Fix**: snapshot the stack into a reusable `List<GameState>` scratch buffer before iterating, so mid-iteration mutations don't invalidate the enumerator. Same treatment for `Draw`. Allocation-free after the first call once the list capacity stabilizes. Covered by a new `GameStateManagerTests.Update_AllowsStateToChangeStateMidIteration` regression test (100/100 total). Shipped in the same commit as this note.

**Finding**: the original 2026-04-18a auto-lifecycle fix (commit `702dd54`) made it *easier* for consumers to transition during Update, because `ChangeState` now does the state-lifecycle ceremony for you. This bug was latent before that commit because nobody was transitioning inline; the cleanup that removed the manual ceremony also removed the natural "call it after Update returns" habit. A good example of a library improvement unintentionally widening the surface area for a pre-existing latent bug.

### 1.8 `Camera2D.ScreenToWorld` is ergonomic for mouse-aim (NEW 2026-04-18c)
Shooter's aim direction needed three lines end-to-end:
```csharp
Vector2 mouseWorld = _camera.ScreenToWorld(_mouse.GetMousePosition());
Vector2 dir = mouseWorld - _player.Position;
dir.Normalize();
```
Zero friction. The Camera2D / MouseManager split composes cleanly. No change recommended.

### 1.5 `GameState.IsActive` conflates "updating" and "visible" (NEW 2026-04-18b)
`GameStateManager.Update` and `GameStateManager.Draw` both skip states where `IsActive == false`. That collapses two concerns into one flag. The intended pattern — "when a state is obscured by another, stop updating it but keep drawing it behind the overlay" — isn't expressible with the current API.

BattleGrid hit this when adding the chip-selection overlay. Pushing a `ChipSelectState` onto `PlayState` would have stopped `PlayState` from drawing too, hiding the battle behind the overlay. Workaround: `PlayState.Obscuring` is now a no-op (doesn't set `IsActive = false`), and chip selection is handled as an internal `Mode` enum rather than a separate pushed state. Works, but it pushes state-machine logic into the consumer that the library's state stack was supposed to handle.

**Recommendation**: split `IsActive` into `IsUpdating` and `IsVisible` (or similar). `PushState` sets the old top's `IsUpdating = false` but leaves `IsVisible = true`. That lets overlay-style states work as intended. Medium change; update the library tests accordingly.

---

## §2 — Platformer-specific patterns (genre-module candidates)

Patterns the tactical demo doesn't touch and likely wouldn't want. Strong candidates for `MonoGame.GameFramework.Platformer` *when* a second platformer-style game comes along.

### 2.1 Physics loop shape
- Gravity applied per-frame to `Velocity.Y` with terminal-velocity clamp.
- Horizontal input → target velocity → accelerate toward it (ground accel ≠ air accel).
- Separate-axis AABB resolution: move X, resolve X collisions; then move Y, resolve Y.
- `IsGrounded` set by collision resolution; consumed by coyote + jump-accel gating.

All of this is ~50 lines concentrated in `Player.Update`. It would be identical in any sibling platformer.

### 2.2 Jump feel
- Coyote time window (150 ms).
- Jump buffer window (150 ms).
- Variable-height jump (release button → clamp upward velocity).
- Post-move jump re-check so buffered jump fires on the same frame as landing.

This is the single most-duplicated chunk of code between any two platformers. A `PlatformerJumpConfig { Velocity, CutVelocity, CoyoteTime, BufferTime }` + a `PlatformerController` helper that consumes `(inputX, jumpPressed, jumpHeld, IsGrounded)` and produces `Velocity` would eliminate most of `Player.cs`.

### 2.3 Camera follow with snap-on-respawn
`Camera2D.FollowLerp` handles the normal follow. On respawn, we need to `Position = Target` to avoid a long lerp across the whole level. Both platformers will want this.

### 2.4 Death plane
`if (player.Position.Y > deathPlaneY) Respawn();` — common to any game with pits. Not sure it rises to a library primitive; it's one line per game.

### 2.5 Patrolling enemy
The `Enemy` class bounces between two x-coordinates at constant speed. Appears in many 2D genres (platformer, sidescroller, shmup). Could be `AI.PatrolBehavior` at the library level rather than a platformer-specific module.

---

## §3 — BattleGrid-specific patterns (for a hypothetical `GridDuel` module)

BattleGrid is no longer turn-based — it's real-time with a chip-selection pause. Patterns that would recur in any grid-duel style game:

- Grid-to-pixel coordinate helpers (`Grid.PlayerCellTopLeft`, `Grid.EnemyCellCenter`, `Grid.RowCenterY`). Two-sided 3×N grids are common in this genre. Could become `Rendering.TwoSidedGrid` or similar.
- Action-tick AI: simple state machine ticking at a fixed interval, alternating categories of actions (move / attack, or similar). A couple of lines of code but repeated in every enemy in this genre.
- Chip / card / ability selection overlay: pauses gameplay, offers 2–4 random options from a pool, commits on pick, then cooldown. The "inline Mode enum instead of separate state" workaround from §1.5 is really this pattern.
- Row-based projectile collision: projectile + entity collision is really "do they share a row at a given X overlap?" — row is the dominant axis. A `Rendering.RowGrid` helper that precomputes row bounds would simplify collision checks in dense patterns.
- HP bar + cooldown-indicator HUD: BattleGrid's hand-rolled bars via `Primitives.DrawRectangle` would repeat in every game with 1–2 combatants. A small `HUD.HpBar(rect, current, max, fill)` helper would eat this.

Not extracting yet (only one consumer), but the density of these patterns is higher than the platformer's — a second grid-duel game would turn most of them into library code quickly.

---

## §4 — API friction

Things that work but awkwardly. Each is a candidate for a small library fix; grouped by cost/benefit.

### 4.1 `UIManager.AddUIElement` forces `DrawManager.AddSprite`
`UIManager` tracks elements for hit-testing (`GetElementAt`, `HoveredElement`, `OnClick`). It also registers each element with `DrawManager`. The registration is hard-coded and can't be opted out of.

**Why this hurts**: Platformer uses two draw contexts (camera + screen). UI belongs in screen space. `DrawManager.Draw` renders in whatever transform the active `SpriteBatch.Begin` provides — but `DrawManager` always draws everything registered, and always with `Color.White`. The platformer's `TitleState` therefore adds buttons to `UIManager` (for the interaction layer) but never calls `_drawManager.Draw`, and instead re-draws the buttons manually with color + hover state.

**Recommendation**: split into `UIManager` (interaction: hit-test, focus, click callbacks, hover — no coupling to drawing) and a separate optional `UIRenderer` that *can* render registered elements through DrawManager for consumers that want that. Keep the existing one-line convenience `AddUIElement(group, sprite)` as an extension method that does both.

> **✅ Fixed 2026-04-18 (commit `702dd54`)**. `UIManager` constructor no longer takes `DrawManager`; `AddUIElement`/`RemoveUIElement` no longer touch it. Demo's `ConsoleUI` and `PlayerHealthUI` now call `DrawManager.AddSprite`/`RemoveSprite` explicitly alongside the UI registration — two lines instead of one, but the coupling is gone. No separate `UIRenderer` was introduced; the decoupling alone was enough. Covered by 5 new `UIManagerTests` including one that constructs `UIManager` with a null `MouseManager` to prove no `DrawManager` is required.

### 4.2 `DrawManager` renders everything white
`DrawManager.Draw` hardcodes `Color.White` and doesn't pass through a tint. For non-white sprites this is fine (texture has the colors). For the pixel-texture pattern in §1.2, it prevents colored rectangles through `DrawManager`.

**Recommendation**: when the pixel-texture utility (§1.2) lands, `SpriteSheet` should grow a `Tint` property that `DrawManager.Draw` respects, defaulting to `Color.White`.

> **✅ Fixed 2026-04-18 (commit `702dd54`)**. `SpriteSheet.Tint` is a mutable `Color` property defaulting to `Color.White`; `DrawManager.Draw` uses `sprite.Tint` instead of hardcoded white. Consumers can now tint, flash, or fade sprites at runtime without replacing them. Covered by 2 new `SpriteSheetTests`.

### 4.3 `Entity` abstract class is unused in 2 of 3 sample games
Platformer and Shooter's entity types (Player/Enemy/Projectile/Platform/Goal) are plain classes, not `Entity` subclasses. BattleGrid is the only consumer, and even there the abstract methods mostly no-op.

The abstract class is not forcing any real shape. Two of three games actively don't use it.

Notably, Shooter's entities evolved a *different* shape from BattleGrid's: plain `Alive` flag + `Update(float dt, ...)` + `Draw(SpriteBatch)` with no `ContentManager` involvement at all. This is the shape that lines up with the `ObjectPool<T>` lifecycle — `Entity`'s `LoadContent`/`UnloadContent` are pool-hostile.

**Recommendation**: strongly consider deleting `Core.Entity`. If a base entity earns its keep in the future, the Shooter shape (`bool Alive`, `Update(dt)`, `Draw`) is a better starting point than the current ContentManager-centric shape.

### 4.4 `Text` rendering has two disconnected paths
Tactical uses `Text.TextManager` (handle-based, batched). Platformer uses `SpriteBatch.DrawString` directly with a cached `SpriteFont`. Both work; neither dominates. `TextManager` is the right choice for HUD-like text that persists across frames; direct `DrawString` is right for one-off overlays ("You Win"). The library doesn't document which to pick.

**Recommendation**: CLAUDE.md should have a short "text rendering" section with the two patterns and when each fits. No code change needed.

> **✅ Fixed 2026-04-18 (commit `702dd54`)**. CLAUDE.md now has a "Text rendering" subsection under Architecture spelling out when to pick `TextManager` vs direct `DrawString`.

---

## §5 — Unused library surface (over-build audit)

Some library code was written speculatively and hasn't earned its place yet. Not a call to delete anything — just to be honest about validation status.

| Primitive | Status | Likely trigger |
|---|---|---|
| `ObjectPool<T>` | **Validated in Shooter** | Both Projectile and Enemy run through pools with prewarm + onReturn. Handled load fine. Surrounding boilerplate is the new finding — see §1.6. |
| `Timer` / `TimerManager` | **Partially validated in Shooter** | `TimerManager.Every` fits enemy spawn waves perfectly (§1.7). Raw `Timer` class still unused. BattleGrid's four hand-rolled countdowns still argue for a simpler `CountdownTimer` shape. |
| `Tween<T>` / `Easing` | Unused after 4 games | BattleGrid's sword-flash alpha and Puzzle's gem-fall animation are the obvious customers; both skipped for MVP scope. If the first polish pass on either doesn't reach for Tween, delete it. |
| `SaveSystem` / `SaveFile` | Unused | First persistent progress |
| `TileMap` / `TileLayer` | **Validated in Puzzle + Roguelike** | Two consumers at very different scales (7×7 vs 60×34). API scaled cleanly. BattleGrid and Platformer still hand-roll their grids; migration is reasonable follow-up. |
| `AssetCatalog` | Unused | Any game with ≥ 10 content entries. Both samples are down to one asset each (the font). |
| `ILogger` / `ConsoleLogger` | Unused | First debugging session painful enough to add logging. `ConsoleUI` partially fills this role in BattleGrid but doesn't use the library's logger. |
| `MathUtilities` | Unused after 3 games | Shooter used `System.Random` + `Vector2.Normalize` + `Math.Clamp` directly — `MathUtilities.Angle`/`RandomFloat`/`RandomInt` never felt missing. Strong candidate for deletion if puzzle + roguelike also skip it. |
| `PerformanceMonitor` | Unused | First perf complaint |
| `EventManager.Subscribe<T>`/`Publish<T>` (typed) | Unused | Any game that grows past ~5 event types. BattleGrid's event set (PlayerMoved/PlayerHit/EnemyHit/FiredProjectile) stayed small enough that the string API is still fine. |

After five games, three primitives are validated (`ObjectPool`, `TimerManager.Every`, `TileMap`/`TileLayer`). Three more are partially validated (`DrawManager`, `Camera2D`). The rest — roughly 30% of the library — has never been used. That's the steady-state signal. §8 below turns this into concrete keep/delete recommendations.

---

## §6 — Recommended actions

Ordered by value-per-effort. None are urgent; pick when the trigger hits.

1. ✅ **Fix `GameStateManager` lifecycle auto-calls** (§1.1). **Done 2026-04-18.**
2. ✅ **Decouple `UIManager` from `DrawManager`** (§4.1). **Done 2026-04-18.**
3. ✅ **Ship the pixel-texture helper + `SpriteSheet.Tint`** (§1.2, §4.2). **Done 2026-04-18.** Adopted by both samples in the follow-up commit.
4. ⏳ **Start a `MonoGame.GameFramework.Platformer` genre module** once a *second* platformer is started (§2). **Still deferred.** After BattleGrid shipped, it's now clearer that platformer + grid-duel share *nothing* — extraction is still sample-of-one.
5. ⏳ **Delete or redesign `Entity` abstract class** (§4.3). **Still deferred.** BattleGrid uses `Entity` but gains nothing from it; verdict unchanged — wait for game #3.
6. ✅ **Document text rendering paths in CLAUDE.md** (§4.4). **Done 2026-04-18.**

### New from 2026-04-18b

7. ⏳ **Split `GameState.IsActive` into `IsUpdating` and `IsVisible`** (§1.5, new). BattleGrid's chip-selection overlay couldn't be implemented as a pushed state because `IsActive = false` would also stop the background battle from drawing. It lives as an inline `Mode` enum instead. A proper overlay API is likely the single most impactful library change if a third game has any pause/menu overlay. **Medium effort, high leverage.**
8. ⏳ **Simpler per-frame `Timer` shape** (§5). `TimerManager.After`/`Every` don't fit the pattern that most games actually hit: "decrement each frame and check if elapsed". Four hand-rolled float counters in BattleGrid confirm this. A `Timer.Tick(dt)` returning a bool would eliminate that duplication.
9. ⏳ **Migrate BattleGrid and Platformer to `TileMap`/`TileLayer`** (§5). Both games hand-roll what the library already provides; the adoption would validate the library primitives and remove local code. Pure win when next touching either game.

Deferred — wait for a trigger:
- Input rebinding layer (Tier 3 backlog; first user friction).
- Particle system (landing dust would trigger this).
- ✅ **Dev console overlay** — Done 2026-04-19. Shipped as `Debugging.DebugOverlay` (tilde-toggled). Replaces BattleGrid's former `DebugState` + `ConsoleUI` and gives the other 8 games their first runtime-diagnostics surface. Also adds pause + step-frame (`Space` / `.`). Covered by 14 new tests. `EventManager` gained a public `AnyEvent` hook so the overlay can tail every event without pre-subscribing.
- ✅ **Headless smoke-test harness** — Done 2026-04-19. `Testing.SmokeHarness` + `--exit-after N` arg on every `Program.cs` + `scripts/smoke-all.sh`. Launches each of the 9 samples for 60 frames via `perl` alarm-based timeout; fails fast on any non-zero exit. Catches init-time crashes the unit suite can't (SpriteFont charset, content-pipeline staleness, service resolution, LoadContent throws).
- ✅ **Spritefont linter** — Done 2026-04-19. `src/MonoGame.GameFramework.Tools` project (binary `mgf-tools`) with `lint-spritefont` and `lint-all-samples` commands. Parses `.spritefont` `CharacterRegion`s and scans C# source for string literals containing uncovered characters. Prevents the em-dash / curly-quote / accented-letter crash class flagged in §1.10.
- ✅ **New-sample scaffolder** — Done 2026-04-19. `scripts/new-sample.sh <Name>` copies `template/` into `src/MonoGame.GameFramework.<Name>/`, renames `__SAMPLE__`, adds to `Game.sln`, builds once. Template includes DebugOverlay + SmokeHarness + a `TitleScreenState`-inheriting title + a widened spritefont charset. Game #10 is one command.
- Pathfinding (smart enemy).

---

## §7 — What the two-game exercise actually proved

- **Camera2D was worth building**. Only game #2 used it, but it was critical there. The audit-phase dirty-flag caching even had a measurable validation target (the `GetViewMatrix` stability test in the test project).
- **Test project caught a real bug** (`Tween<T>` zero-duration → stuck at `From`). The 87 tests cost about an hour; the single bug they found would have taken longer to notice.
- **Genre modules were correctly deferred**. Building them before game #2 would have designed against imagined abstractions. The items in §2 are concrete because they came from real duplication.
- **Over-build is visible now**. §5 lists 10+ primitives not used by either game. Some are fine ("will land soon"), some are speculative ("built because it seemed generally useful"). Honest accounting prevents the library from growing into a dumping ground.
- **The library passed the "second consumer" test.** The platformer was built in ~9 small phases without blocking changes to the library (aside from discoveries logged here). That's the main signal that the core abstractions are OK.
- **The findings loop works.** Four of the six §6 items were fixed in a single bundled follow-up commit (`702dd54`) with test coverage for each. The two deferred items (genre module, `Entity` redesign) are explicitly sample-of-one extractions that only game #3 can honestly validate — deferring them was correct, not procrastination.

### Updated after BattleGrid's buildout (2026-04-18b)

- **Genre-module extraction still has no signal, and the reason is stronger now.** Platformer and BattleGrid don't just have different mechanics — they have *different shapes of problem*. Platformer's core is continuous physics + camera; BattleGrid's core is discrete grid movement + discrete attack scheduling. The two share nothing at the gameplay layer. The only overlap is plumbing that already lives in the library (states, input, rendering, font).
- **The library's state-stack API doesn't handle pause-overlays.** §1.5 is the single biggest new friction. Most games that grow beyond a single play screen will hit it.
- **`Rendering.Primitives` was the right addition.** Both samples now use it ubiquitously; neither had to re-invent a 1×1 pixel texture. Low-effort library win validated by real use.
- **Rectangle-based entity rendering revealed a hitbox-anchor class of bug.** When Phase 5 moved the projectile spawn to the character's vertical center but left the hitbox as a 38×22 sub-region anchored top-left, collisions silently stopped working. Lesson for any `Entity` redesign: if a `Bounds` concept lands on the base class, it should default to the full visual rect — sub-hitboxes are a per-game opt-in with real risk of this exact desync.
- **Hand-rolled patterns in BattleGrid point to small, targeted library additions** (§3). A `TwoSidedGrid`, a simpler `Timer.Tick`, a `HUD.HpBar` — each would pay for itself in a second game of the same genre. None big enough to justify speculative build.

### Updated after Shooter (2026-04-18c)

- **`ObjectPool` works at load, but the rent/update/cull loop is boilerplate.** §1.6's `PooledEntitySet<T>` proposal is the first real library win to come out of Shooter. One more game with pooled entities would justify building it.
- **`Camera2D` is now 2-game-validated**, across two very different camera patterns (side-scrolling follow vs. twin-stick follow with screen-to-world). Confirmed worth keeping.
- **Entity base class's lack of validation deepened.** Shooter is the second game to skip `Core.Entity`, with a materially different entity shape optimized for pooling (§4.3). A third skip would make deletion clearly the right call.
- **HP-bar HUD is a repeating pattern.** BattleGrid and Shooter both hand-roll the same 4-rectangle bar (background, fill, 4 border strips). Two datapoints is starting to look like a library helper.
- **Title/Play state split is a shared skeleton across all 3 games.** The copy-paste between title states is ~80% identical. A `TitleState` base class in the library with virtual `Draw`/button-config hook would eliminate it — but only if the 4th and 5th games also follow this pattern.

### Updated after Puzzle (2026-04-18d)

- **`Core.Entity` deletion is now the obvious call.** 3 of 4 games skip it. Puzzle didn't even need to skip it consciously — grid cells aren't entities. Holding the class gives zero value and costs one naming decision ("is this an Entity or a plain class?") for every new game.
- **`TileMap` / `TileLayer` is a validated primitive.** Puzzle's `Board` is 200 lines on top of it and reads cleanly. The three sharp edges (§1.9) are the natural backlog.
- **`TitleState` copy-paste is now 4 games deep.** Roughly 80 lines duplicated per game, zero variation beyond button labels and background color. Clear candidate for a library `TitleState<TPlay>` base class or a `MenuBuilder` helper. Will decide in §8 after Roguelike.
- **SpriteFont charset footgun (§1.10) is the first library-level safety issue.** Any game shipped to real users would hit it. Fixing this in the sample-game template is cheap; fixing it properly in the library needs a small decision (widen font, or add safe-draw). Priority bumped to "do this before the next game if possible".
- **Four games, zero uses of `SoundManager`, `AssetCatalog`, `SaveSystem`, typed events, `MathUtilities`, raw `Timer`, `PerformanceMonitor`, `SpriteSheet.Animated`.** That's roughly a third of the library that has yet to justify its existence. §8 should make delete-or-keep recommendations on each.
- **`DrawManager` is validated but not essential.** Puzzle and Shooter skipped it and drew entities directly. Either pattern works; the library doesn't force it. That's the right shape.

### Updated after AutoBattler (2026-04-18i) — ninth and final sample game

- **`Events.EventManager.Subscribe<T>`/`Publish<T>` moves off the speculative list.** AutoBattler's `CombatState` subscribes to `UnitDamaged` and `UnitKilled`; the combat tick publishes them per-action. The typed API feels genuinely more ergonomic than the string API here: you get IntelliSense on the payload fields (`e.Attacker.Stats.Name`, etc.) instead of a generic `GameEventArgs.Message`. **Verdict**: keep both APIs. The string API is fine for cross-cutting event buses ("toggleConsole", "PlayerMoved"); the typed API wins when the subscriber cares about structured payload data.
- **Drag-and-drop in `UIManager` is genuinely missing.** `ShopState` hand-rolled mouse press/release/held tracking for card→board dragging (~30 lines). `UIManager.OnClick` is wrong shape — clicks are instantaneous, drags have start/update/end. Not a lot of code, but it appears in any game with placement mechanics. **Finding**: either grow `UIManager` with `OnDragStart/OnDragUpdate/OnDragEnd` callbacks, or accept this stays per-game. Flagging for §8 Tier D.
- **4-state GameStateManager graph works cleanly.** Title → Shop → Combat → PostCombat → Shop (loop) → ... PostCombat → Title on restart. `ChangeState` transitions auto-fire Entered/Leaving thanks to the 2026-04-18a library fix. Shared mutable state (`GameModel`) passed to each constructor; each state reads and mutates in place. Zero state-machine friction in this game.
- **Hand-rolled BFS pathfinding is ~40 lines.** Uses `Board.UnitAt` as a blocker predicate, treats the target cell as the only exception. `Pathing.NextStepToward` returns the first step (not the full path) which suits tick-based movement. **Finding for §8**: a library pathfinding helper would need the blocker predicate as a delegate, work on any `TileLayer<T>` with a per-cell walkability check, and return either a full path or a next-step. Shape of the API is now concrete. Still a sample-of-one; don't build yet.
- **Grid-cell drop detection is the same hand-rolled `(mouse - origin) / cellSize` pattern as Puzzle and TowerDefense.** Three consumers. Unambiguous extraction candidate. Bumped to Tier A in the revised §8.
- **Per-unit HP bars at 5-16 concurrent instances** — same 3-rectangle pattern (bg + fill) as the other samples. Fifth consumer. `UI.HpBar` helper is now 5-games deep.
- **`DrawManager` skipped again.** AutoBattler draws units/cards/HUD directly in each state. Total: 2 of 9 games use `DrawManager`. Genuinely optional.

### Updated after VisualNovel (2026-04-18h)

- **`Persistence.SaveSystem` moves off the speculative list.** VN saves on every advance (~10 nodes = ~10 `Save` calls per playthrough) and loads on Continue. The Save/TryLoad/Exists/Delete quartet feels correctly-shaped. Two observations:
  1. **`TryLoad` out-param pattern was natural** — more ergonomic than a `SaveFile<T>?` return. No change recommended.
  2. **Save-node-ID stability is a real library-user constraint that doesn't surface until there's a real consumer.** VN saves `DialogueState.CurrentNodeId` as a string. If the game's author later renames a node ID, old saves break. The library can't solve this — it's inherent to any persistent store — but it's worth flagging in CLAUDE.md so users aren't surprised. `SaveFile<T>.Version` already exists for migration.
- **`Tween<T>` FINALLY got a consumer after 8 games — and uncovered a real bug.** Wrote `Tween.Float(...)` in `PlayState.cs` and hit `error CS0234: The type or namespace name 'Float' does not exist in the namespace 'MonoGame.GameFramework.Tween'`. Root cause: the static class `Tween` lives inside a namespace also called `Tween`, so `using MonoGame.GameFramework.Tween;` makes `Tween.Float` refer to the namespace, not the class. Workaround: `using TweenOf = MonoGame.GameFramework.Tween.Tween;` — ugly alias.
  - **Finding (§1.14)**: rename either the namespace to `MonoGame.GameFramework.Tweening` OR rename the static factory class to `Tweens`. Same bug bit the test project (`TweenTests.cs` has the `TweenOf` alias) — that's two consumers hitting the same gotcha. **Library bug, fix next cleanup.**
- **`Tween<float>` + `Easing.QuadOut` worked well for text reveal.** Build once, update once per frame, read `.Current` as 0→1 progress. API is clean *once the naming collision is worked around*. Primitive validated; recommend keeping it, subject to fixing §1.14.
- **`TextManager` still NOT used by VN.** PlayState uses direct `SpriteBatch.DrawString` with a tweened substring because the text changes every frame during reveal. Second game to actively skip `TextManager` where it "should" fit — reinforces that the handle-based API fits persistent HUD text but not dynamic text.
- **Word-wrap had to be hand-rolled** (see `WrapText` in `PlayState.cs`). Any game with long strings and a fixed-width text box needs this. Third hand-rolled pattern across samples (after HpBar and LogBox). Medium-priority candidate: `Text.WrapText(string, SpriteFont, float maxWidth)` helper in the library.

### Updated after Rhythm (2026-04-18g)

- **`Audio.SoundManager` moves off the speculative list.** Rhythm loads one sound effect at play-state entry and calls `PlaySoundEffect("audio/click")` ~55 times per play session. API works. Two ergonomic observations:
  1. **String-keyed lookup is fine for one sound but will grow.** If a game has 20 sound effects, you want IntelliSense/compile-time checking. A `public static class SoundIds { public const string Click = "audio/click"; }` convention in the consuming game covers it; no library change needed.
  2. **`LoadSoundEffect` must be called before `PlaySoundEffect`** — obvious but not enforced. Calling `PlaySoundEffect` on an unloaded name silently no-ops (it returns early on the `ContainsKey` check). For a rhythm game that's a bug that would go unnoticed. Consider throwing or logging.
- **Hit-flash is hand-rolled float-per-lane, NOT `Tween<float>`.** Because it's just `MathF.Max(0, t - dt)` with an alpha derived from a linear ratio. Tween would have added no value here — which is actually a finding: Tween's API (build + hold + update) has more ceremony than this inline use case needs. `Easing.QuadOut(flash / maxFlash)` *would* be useful for non-linear fade. Tween shines when you need the Update loop to manage a collection of concurrent tweens with varying durations; one-per-lane with fixed duration doesn't need it.
  - **Finding**: Tween stays unused even though Rhythm "should" have been the customer. This weakens the case for keeping it. If VN's text-reveal also doesn't use Tween naturally, it becomes a deletion candidate.
- **Audio latency is not perceptible.** Click plays on key-press (not on scheduled time), which side-steps the scheduled-beat latency question entirely. A real rhythm game would need latency compensation; the MVP doesn't surface that problem. Useful calibration: the library doesn't need to solve latency today.
- **Content pipeline handled the `.wav` with no issue.** First non-font content entry across all 7 games. The commit is the WAV file + 5 lines in Content.mgcb. Low friction.

### Updated after TowerDefense (2026-04-18f)

- **`ObjectPool` pattern confirmed again.** Second pooled-entity game produces the same rent/update/cull boilerplate as Shooter. §1.6's `PooledEntitySet<T>` proposal now has two independent consumers justifying it. Upgrading to a Tier D extraction candidate at the next §8 revision.
- **Grid-cell click interaction is hand-rolled, NOT `UIManager.OnClick`.** TowerDefense ignored `UIManager` for placement entirely — it reads mouse position, computes `(col, row)` from origin + cell size, checks the path-cell set and tower dictionary, places. ~15 lines total. Making this a `UIManager` feature would require creating a `SpriteSheet` per grid cell (20×14 = 280 sprites) for hit-testing that doesn't need pre-existing visuals. The grid-click pattern is genuinely different from button-click.
  - **Finding**: `UIManager` is right to stay button-focused. A *separate* helper `Grid.TryMouseToCell(mousePos, origin, cellSize, cols, rows, out int col, out int row)` would eliminate TowerDefense's hand-rolled conversion. Puzzle had the same hand-rolled conversion. Two consumers. Flag for extraction.
- **Per-enemy HP bars validate the `UI.HpBar` helper candidate at scale.** TowerDefense renders up to ~15 enemy HP bars simultaneously, each with the same 4-rectangle pattern (bg + fill + no border at this size). Confirms §8 Tier D `HpBar` proposal would handle many-entities case without issue.
- **`TimerManager.Every` for wave spawns worked; but timer-cancel would be useful.** The wave spawn timer runs for the duration of the wave; there's no clean way to stop it after the wave's target count is reached (I just made `TrySpawnEnemy` check the counter and no-op). The existing `Timer.Cancel()` would work if the `TimerManager.Every` call returned the `Timer` — it does (`return timer`), I just didn't store it. Small finding: document that `Every`/`After` return the timer and that you can Cancel it.

### Updated after Roguelike (2026-04-18e) — and this is the fifth game

- **`TileMap` scales from 49 cells to 2,040 cells with zero friction.** Two consumers, two very different use-cases (match-3 gems vs. procgen dungeon), same API. Confirmed keeper.
- **`Core.Entity` deletion is now unambiguous.** 4 of 5 games skip it. Roguelike's `Actor` base class is a different shape entirely (grid `Col`/`Row` + HP + `Damage`), directly incompatible with `Core.Entity`'s `ContentManager`-centric lifecycle. No future game is likely to adopt `Core.Entity` as-is.
- **`TitleState` copy-paste is now 5 games deep.** Same ~80 lines per game, still zero variation beyond labels + background color. This is unambiguously a library extraction candidate; §8 will make the concrete proposal.
- **Turn scheduling doesn't want a library primitive.** §1.11 argues against adding a `TurnScheduler`: the two games with turn-ish logic (BattleGrid action ticks, Roguelike monster passes) don't share enough to justify it. Good example of a superficially-similar-but-actually-distinct pattern.
- **Procgen lives per-game (§1.12).** The `TileLayer<T>` API is the right stopping point; generators shouldn't live in the library.
- **Scrolling text logs are a third repeating pattern (§1.13).** BattleGrid, Shooter, Roguelike all hand-roll ~20-line variants. Medium-priority extraction candidate (`LogBox` widget).
- **Five games, ~30% of the library still unused.** Stable signal now — next game won't change the picture meaningfully. §8 makes delete/keep calls.

---

## §8 — Recommended next library work (prioritized, from 9-game data)

Nine games across nine genres — grid-duel, platformer, twin-stick shooter, match-3 puzzle, turn-based roguelike, tower defense, rhythm, visual novel, auto-chess — are all playable end-to-end. This is the fullest picture the exercise can produce without shipping a real project. This section turns every preceding observation into a concrete, actionable decision: **do this**, **delete this**, **hold for trigger**, or **do not build this**.

Tiers are ordered by priority. Each item names the section it came from for traceability.

### Library primitives validated by this exercise

The following primitives moved off the speculative list over the course of the 9 samples:

| Primitive | Validating consumer(s) | Notes |
|---|---|---|
| `Input.KeyboardManager` | all 9 | universal |
| `Input.MouseManager` | 8 of 9 (Roguelike skipped) | universal-ish |
| `Rendering.Primitives` | all 9 | universal |
| `Rendering.SpriteSheet.Static` + `Tint` | all 9 title screens | universal |
| `Lifecycle.GameState` + auto-lifecycle | all 9 | universal |
| `UI.UIManager` (hit-test + OnClick + HoveredElement) | all 9 title screens | universal |
| `Rendering.Camera2D` (follow + view matrix) | Platformer, Shooter | 2 consumers, distinct patterns |
| `Rendering.TileMap` + `TileLayer<T>` | Puzzle, Roguelike | 2 scales: 49 cells vs 2040 cells |
| `Pooling.ObjectPool<T>` | Shooter, TowerDefense | 2 consumers, real load |
| `Timing.TimerManager.Every` | Shooter, TowerDefense | spawn cadence |
| `Audio.SoundManager` | Rhythm | ~55 PlaySoundEffect calls/session |
| `Persistence.SaveSystem` | VisualNovel | Save/TryLoad/Exists/Delete |
| `Tween.Tween<T>` + `Easing` | VisualNovel | text-reveal, caveat §1.14 |
| `Events.EventManager` (string API) | BattleGrid, DebugState console | |
| `Events.EventManager.Subscribe<T>`/`Publish<T>` (typed) | AutoBattler | combat events |

**That's roughly 70% of the public API now validated by at least one real consumer.** The remaining ~30% is the deletion-candidate list below.

### Tier A — Do now (safety + trivial wins)

Small, backward-compatible, concrete evidence. Build them before the next game or the first real project.

1. ✅ **Widen default spritefont charset** (§1.10) — **Done post-AutoBattler.** All 9 spritefonts now cover ASCII + Latin-1 Supplement + common typographic punctuation. Em-dash/curly quotes/accented letters render without crashing.
2. ✅ **Rename `Tween` namespace or class to fix the collision** (§1.14). **Done 2026-04-18 (j)**. Namespace renamed `MonoGame.GameFramework.Tween` → `MonoGame.GameFramework.Tweening`; static class `Tween` kept. `TweenOf` alias removed from VN `PlayState` and test project. Test namespace `MonoGame.GameFramework.Tests.Tween` also collided with the class and was renamed `…Tests.Tweening` as part of the same fix.
3. ✅ **`TileLayer<T>.Swap((c,r),(c,r))`** (§1.9). **Done 2026-04-18 (j)**. 3-line helper using tuple-deconstruction swap + the existing indexer. Covered by 2 new tests.
4. ✅ **`TileMap.TryWorldToCell(Vector2, out int col, out int row)`** (§1.9). **Done 2026-04-18 (j)**. Bounds-checked variant; returns `false` for out-of-map coordinates without mutating `col`/`row` meaningfully. Covered by 6 new tests (inside/outside-bounds theory + origin-respect).
5. ✅ **`GridMath.TryMouseToCell(mouse, origin, cellSize, cols, rows, out col, out row)` helper** — shipped as `Rendering.GridMath` (not BattleGrid-specific `Grid`, which is sample code). **Done 2026-04-18 (j)**. Migrated Puzzle, TowerDefense, and AutoBattler ShopState from hand-rolled conversions. Puzzle already had a `TileMap`, so it uses `TileMap.TryWorldToCell` directly; the other two call `GridMath`. 4 new tests.
6. ✅ **Document `TimerManager.Every` return-value cancellability** in CLAUDE.md. **Done 2026-04-18 (j)**.
7. ✅ **Document the `cache-GetLayer<T>` pattern** in CLAUDE.md. **Done 2026-04-18 (j)**.
8. ✅ **Warn in `SoundManager.PlaySoundEffect` when the sound isn't loaded** (§1 Rhythm). **Done 2026-04-18 (j)**. Switched to `Debug.WriteLine` on unknown key (still no-throw). Same treatment for `PlaySong`.

**Total Tier A**: ~1 day of work. All items ✅ Done.

### Tier B — Delete (high confidence after 9 games)

Each has zero consumers across nine genre-diverse games. The remaining "kind of utility that'll land in game 6" argument gets thinner with every game that skips them.

1. ✅ **`Core.Entity`** — **Done 2026-04-18 (j)**. Class deleted. BattleGrid's four subclasses (`Player`, `EnemyPlayer`, `Projectile`, `Gameboard`) migrated to plain classes; `override` keywords dropped; empty `Gameboard.Update` method + its caller in `BattleScene.Update` removed as dead code. `using MonoGame.GameFramework.Core;` still present in every game's `Program.cs` because `ServiceCollectionExtensions` lives there.
2. ✅ **`SpriteSheet.Animated`** — **Done 2026-04-18 (j)**. Factory deleted; frame-cycling fields (`Frames[]`, `FrameInterval`, `CurrentFrame`, `elapsedTime`) and `Update` method removed; `Clone` method removed (dead). `Frames[CurrentFrame]` in `DrawManager.Draw` replaced by a single `SourceFrame` Rectangle field. `BattleConfig.CharacterFrameInterval` removed as dead constant.
3. ✅ **`Utilities.MathUtilities`** — **Done 2026-04-18 (j)**. File deleted; matching tests deleted; now-empty `Utilities/` folders removed from both library and tests.
4. ✅ **`Timing.Timer` (raw class)** — **Done 2026-04-18 (j)**. Kept as a class (it's still the return type of `TimerManager.After/Every/Over` so external consumers can `.Cancel()` it), but the constructor was made `internal` so external code can't directly `new Timer(...)`. Standalone `TimerTests.cs` deleted; `TimerManagerTests` covers observable behavior.
5. ✅ **`Debugging.PerformanceMonitor`** — **Done 2026-04-18 (j)**. File deleted; no consumers, no tests existed.

**Actual deletion**: ~1,185 lines removed, ~316 added net across the whole cleanup (see §8 summary line at the top).

**Explicitly NOT deleted** (contrary to the 5-game recommendation): `Events.EventManager.Subscribe<T>`/`Publish<T>`. AutoBattler validated it with a real multi-event combat system. Keep both APIs.

### Tier C — Hold (unused but with a clear future trigger)

Don't build or delete. Reassess when the trigger fires.

1. **`Content.AssetCatalog`** — triggers at ~10 content entries in one game. Current games have 1 (font) or 2 (font + click.wav).
2. **`Debugging.ILogger` / `ConsoleLogger`** — triggers on the first painful debugging session. Still deferred — the **tilde-toggled `Debugging.DebugOverlay`** (added 2026-04-19) covers the actual "I need runtime visibility" use case better than scrolling log lines would, so the `ILogger` trigger hasn't fired.
3. **`Rendering.DrawManager`** — only 2 of 9 games use it (BattleGrid, Platformer). The other 7 draw directly. Keep it; stop treating it as the default rendering pattern. Update CLAUDE.md to reflect that direct-draw is fine.

### Tier D — Extract these patterns from game code (library wins from 9-game evidence)

Patterns that repeated across 3+ games as near-identical copy-paste. These are the highest-leverage library additions the exercise can produce.

1. ✅ **`Lifecycle.TitleScreenState` base class** — **Done 2026-04-18 (j)**. Base class takes `IServiceProvider` (not the concrete `ServiceProvider`) so tests can feed a fake DI container. Abstract `BackgroundColor` / `TitleText` / `GetButtons()` plus virtual `SubtitleText` / `HintText` / custom hover colors / `ButtonWidth` / `ButtonGap` / `TitleY` / `SubtitleY` / etc. `CreateButtonSprite` hook for headless tests to bypass `Primitives.Pixel`. All 9 games migrated from ~90-119 lines to ~25-35 lines each. VN's conditional-Continue-button modelled via `ButtonSpec.Enabled`; `Revealed()` rebuilds the spec list so save presence is re-checked when the title reappears. 4 new tests + sample-game smoke.

2. ✅ **`UI.HpBar(rect, current, max, fill)` drawing helper** — **Done 2026-04-18 (j)**. Two flavours (`Draw` / `DrawWithBorder`) with default bg `Color(25, 30, 45)` matching every consumer. Fill width clamps `current` to `[0, max]` and handles `max == 0` / zero-width rect defensively. Migrated BattleGrid (with border), Shooter, TowerDefense Enemy, AutoBattler ShopState + CombatState. Roguelike uses text-only HP so stays unchanged (the 5th "consumer" the original note counted was text, not a bar). 7 new tests covering fill-width edges.

3. ✅ **`UI.LogBox`** scrolling-text widget — **Done 2026-04-18 (j)**. `Queue<string>`-backed, configurable `maxLines` / `fadeStart` / `fadeStep` / `baseColor`. `Draw(sb, font, origin, lineHeight)` lays out top-down; consumers that want bottom-aligned placement compute `origin.Y`. Migrated Roguelike (maxLines=5, fadeStart=0.5, fadeStep=0.1) and AutoBattler CombatState (maxLines=6, defaults). BattleGrid stays on `TextManager.ScrollText` by design — the two patterns coexist. 4 new tests.

4. **`Text.WrapText(string, SpriteFont, float maxWidth)`** — VN hand-rolled a word-wrap helper; any game with flavor text longer than a line wants it. 1 consumer today but the pattern is obvious and low-effort (~15 lines). **Still deferred** — build when the second text-heavy game appears.

5. ✅ **`Pooling.PooledEntitySet<T>`** — **Done 2026-04-18 (j)**. Wraps `(ObjectPool<T>, List<T> live)` with `Rent`, `UpdateAndCull(update, onCull?)`, `Cull(onCull?)`, `ReturnAll`. `onCull` delegate supports side-effects that differ per reason (TowerDefense: `e.Leaked → _lives--`, `!e.Alive → _gold += GoldPerKill`). `isAlive` predicate is per-set, so TowerDefense's compound check (`e.Alive && !e.Leaked`) works without changing the entity API. Shooter (projectiles + enemies) and TowerDefense (projectiles + enemies) both migrated. 5 new tests.

6. **`Timing.CountdownTimer` struct** — `float remaining; bool Tick(float dt) => (remaining -= dt) <= 0`. BattleGrid has 4, Rhythm has 4 (per-lane flash), AutoBattler has 1 (tick accum). Several consumers, minimal code. **Still deferred** — three-line pattern isn't painful inline; low-priority readability improvement.

7. **`UI.DragHandler`** — AutoBattler hand-rolled drag-start/update/end for card placement. Single consumer today, not enough signal. **Deferred** — revisit if a second drag-and-drop game appears.

### Tier E — Do NOT build (explicitly rejected after evidence)

Temptations the data has disarmed.

1. **`TurnScheduler` library primitive** (§1.11). BattleGrid, Roguelike, AutoBattler all have turn/tick logic; implementations don't share meaningful code. Each ~10-line tick loop does what it needs. Resist.
2. **Genre modules** (`Platformer`, `GridDuel`, etc.). 9 games share almost nothing at the gameplay layer. Extraction today would be sample-of-one for every genre. *Only revisit if a second game in the same genre appears.*
3. **Procgen helpers in the library** (§1.12). Per-game. Roguelike's `DungeonGenerator` is ~80 lines that operate entirely on `TileLayer<T>`; a second roguelike would want different generation anyway.
4. **Library-level pathfinding** (§1 AutoBattler). AutoBattler's hand-rolled BFS is sample-of-one. The shape of a library API is now clear (blocker-predicate delegate + `TileLayer<T>` operand + full-path vs. next-step return), but extracting it today would design against imagined needs. **Revisit when a second pathfinding consumer appears.**
5. **Extending `TextManager` for dynamic text** — 4 games that "should" use `TextManager` (BattleGrid flavor text, VN reveal, Rhythm score, TowerDefense wave text) actively skipped it for direct `SpriteBatch.DrawString`. The handle-based API fits persistent HUD labels; don't grow it for dynamic text. `TextManager` stays, constrained to its current use case.
6. **Per-game state-visibility split** (§1.5 was open after BattleGrid, now reassessed). Eight more games shipped without hitting this friction. BattleGrid's inline `Mode` enum and AutoBattler's multi-state graph both work cleanly. Demote from "build this" to "monitor".
7. **`UIManager` drag-and-drop built-in**. AutoBattler hand-rolled it in 30 lines. Single consumer. Don't extend `UIManager` without a second data point.

### Tier F — Still trigger-driven (no change)

- Particle system — first game needing dust/sparks/trails.
- Input rebinding — first user friction.
- FOV — second roguelike or stealth game.
- Dev console overlay — first painful debugging session.
- GUI widgets beyond `HpBar`/`LogBox`/title buttons — first menu-heavy screen (settings, pause, inventory).
- Shader / post-processing helpers — first screen-space effect.
- Physics beyond AABB — first game needing circles/polygons.

### Suggested execution order

All 6 items executed end-to-end on 2026-04-18 (j). Kept here as historical record.

| # | Status | Commit | Effort |
|---|---|---|---|
| 1 | ✅ | `chore: widen spritefont charset + rename Tween namespace (§1.14) + small grid/timer helpers (Tier A)` | 2 hours |
| 2 | ✅ | `refactor: delete unused primitives (Core.Entity, SpriteSheet.Animated, MathUtilities, raw Timer, PerformanceMonitor)` + migrate BattleGrid's `Entity` subscribers | 2 hours |
| 3 | ✅ | `feat: Lifecycle.TitleScreenState base class` + migrate all 9 games | 2 hours |
| 4 | ✅ | `feat: UI.HpBar drawing helper` + migrate 4 games | 1 hour |
| 5 | ✅ | `feat: UI.LogBox widget` + migrate Roguelike + AutoBattler | 1 hour |
| 6 | ✅ | `feat: Pooling.PooledEntitySet<T>` + migrate Shooter and TowerDefense | 1 hour |

**Total**: roughly a productive day's work. Net result after execution: 316 additions, 1,185 deletions; test count 100 → 122 passing. The library is objectively smaller, more focused, and duplicates less across the 9 consumer games. The data to justify every change is on file above.

### What actually mattered in hindsight

Two meta-observations after nine sample games:

1. **The delete list is bigger than the extract list.** Five primitives with zero consumers, five patterns with 3+ consumers. Library code that's not actively validated is more often wrong than right.
2. **The biggest win was free.** `Rendering.Primitives` (pixel texture + DrawRectangle helper) is one file, ~20 lines, and is universal across all 9 games. None of the bigger primitives had that leverage. When in doubt, the library's future additions should be tiny.

### Summary

Concrete next commit plan if you build from here:

1. **One cleanup commit**: widen spritefont charset + add `TileLayer.Swap` + `TileMap.TryWorldToCell` + CLAUDE.md `GetLayer<T>` note. (Tier A, ~1 hour.)
2. **One deletion commit**: drop `Core.Entity`, `SpriteSheet.Animated`, `MathUtilities`, `Timing.Timer`, typed `EventManager` API, `PerformanceMonitor`, and their tests. Update consumers (really just BattleGrid's `Entity` subscribers). (Tier B, ~2 hours.)
3. **One extraction commit**: `TitleScreenState` base class; migrate all 5 games to inherit from it. (Tier D item 1, ~2 hours including the migrations. Biggest visible win.)
4. **Optional second extraction commit**: `HpBar` helper; migrate BattleGrid / Shooter / Roguelike. (Tier D item 2, ~1 hour.)

Everything else waits for a concrete real-project need to surface. The library will be materially smaller, better-understood, and more focused after those three commits than it is today — and the data to justify every change is on file in §§1–7.
