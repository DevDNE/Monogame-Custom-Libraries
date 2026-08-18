# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A reusable MonoGame DesktopGL framework library (`MonoGame.GameFramework`) with **nine sample games in different genres** that exercise and validate it. Uses Microsoft.Extensions.DependencyInjection for wiring services together.

## Solution Structure

```
Game.sln
src/
  MonoGame.GameFramework/               ← Class library (reusable framework)
  MonoGame.GameFramework.BattleGrid/    ← Grid-based duel (Mega Man Battle Network-style)
  MonoGame.GameFramework.Platformer/    ← Side-scrolling platformer
  MonoGame.GameFramework.Shooter/       ← Twin-stick arena survival
  MonoGame.GameFramework.Puzzle/        ← Match-3 gem board
  MonoGame.GameFramework.Roguelike/     ← Turn-based dungeon crawler
  MonoGame.GameFramework.TowerDefense/  ← Wave-based tower defense
  MonoGame.GameFramework.Rhythm/        ← 4-lane rhythm game
  MonoGame.GameFramework.VisualNovel/   ← Dialogue-tree VN with save/load
  MonoGame.GameFramework.AutoBattler/   ← Auto-chess shop + combat loop
  MonoGame.GameFramework.Tests/         ← xUnit tests for the library + tools (290 tests)
```

## Build & Run

```bash
dotnet build Game.sln                                                                              # Build all projects
dotnet test  Game.sln                                                                              # Run all 290 library + tools tests
dotnet run --project src/MonoGame.GameFramework.BattleGrid/MonoGame.GameFramework.BattleGrid.csproj   # Run any sample — swap the project name
dotnet restore                                                                                     # Restore NuGet packages
```

Each sample game has its own `Content/Content.mgcb`. All reference a single font (`fonts/Arial.spritefont`); Rhythm additionally bundles `audio/click.wav`. **All nine ship pixel art** — a per-game palette, `.pix` sources and PNG exports under `Content/sprites/`, and a nine-slice-skinned title screen. `Rendering.Primitives` survives for the handful of things that are genuinely rectangles: HP bars, selection rings, scrims, the odd divider. Edit a content file with:
```bash
dotnet mgcb-editor ./src/MonoGame.GameFramework.BattleGrid/Content/Content.mgcb
```

Spritefont charset: all 9 games widened the default charset to include Latin-1 Supplement, en/em dashes, curly quotes, ellipsis, and bullet. Pasting any of those into flavour text no longer crashes `SpriteFont.MeasureString`. If you edit a `.spritefont` and the incremental cache doesn't pick it up, clear `Content/bin` + `Content/obj` in the affected game before rebuilding.

## Tech Stack

- **Framework**: MonoGame 3.8.4.1 (DesktopGL)
- **Target**: .NET 9.0
- **DI**: Microsoft.Extensions.DependencyInjection 9.0.0
- **Serialization**: Newtonsoft.Json (library only — used by SaveSystem and SettingsManager)
- **Config**: dotenv.net (demo only)

## Architecture

### Library (`MonoGame.GameFramework`)

**Namespace root**: `MonoGame.GameFramework`

The library is organized into domain folders, each with a matching namespace. Services are registered by `Core/ServiceCollectionExtensions.AddGameFrameworkManagers()` and resolved via DI.

| Folder | Namespace | Contents |
|---|---|---|
| `Audio/` | `MonoGame.GameFramework.Audio` | `SoundManager` |
| `Content/` | `MonoGame.GameFramework.Content` | `AssetCatalog` |
| `Core/` | `MonoGame.GameFramework.Core` | `ServiceCollectionExtensions` |
| `Debugging/` | `MonoGame.GameFramework.Debugging` | `ILogger`, `ConsoleLogger`, `DebugOverlay` |
| `Events/` | `MonoGame.GameFramework.Events` | `EventManager` (string-keyed + typed `Subscribe<T>`/`Publish<T>`), `GameEventArgs` |
| `Input/` | `MonoGame.GameFramework.Input` | `KeyboardManager`, `MouseManager`, `GamePadManager` |
| `Lifecycle/` | `MonoGame.GameFramework.Lifecycle` | `GameState` + `GameStateManager`, `GameScene` + `SceneManager`, `TitleScreenState` |
| `Persistence/` | `MonoGame.GameFramework.Persistence` | `SaveSystem`, `SaveFile<T>`, `SettingsManager` |
| `Pooling/` | `MonoGame.GameFramework.Pooling` | `ObjectPool<T>`, `PooledEntitySet<T>` |
| `Rendering/` | `MonoGame.GameFramework.Rendering` | `DrawManager`, `SpriteSheet`, `Camera2D`, `TileMap`, `TileLayer<T>`, `Primitives`, `PixelDraw`, `NineSlice`, `GridMath` |
| `Testing/` | `MonoGame.GameFramework.Testing` | `SmokeHarness` (headless `--exit-after N` support) |
| `Text/` | `MonoGame.GameFramework.Text` | `TextManager` (handle-based), `TextElement`, `TextHandle` |
| `Timing/` | `MonoGame.GameFramework.Timing` | `TimerManager` (`After`/`Every`/`Over`), internal `Timer` |
| `Tween/` | `MonoGame.GameFramework.Tweening` | `Tween<T>` + `Tween.Float/Vec2/Color` factories, `Easing` |
| `UI/` | `MonoGame.GameFramework.UI` | `UIManager` (hit-testing, focus, click handlers), `HpBar`, `LogBox` |

**Base classes** (all in `Lifecycle/`):
- `Lifecycle/GameState` — stack-based state with `Entered`/`Leaving`/`Obscuring`/`Revealed`/`Update`/virtual `Draw`.
- `Lifecycle/GameScene` — scene with `LoadContent`/`UnloadContent`/`Update`/virtual `Draw`.
- `Lifecycle/TitleScreenState` — base for title screens. Override `BackgroundColor`, `TitleText`, and `GetButtons()`; optionally override `SubtitleText`, `HintText`, `ButtonWidth`, `ButtonGap`, `TitleY`, `SubtitleY`, hover/normal/disabled button colors. **Pixel-art skin (all opt-in, added 2026-08-16)**: `ButtonFrame`/`HoverButtonFrame`/`DisabledButtonFrame` (a `NineSlice` each), `BackgroundTile`, `PixelScale`, the three `*ButtonFrameTint` colours, and `DrawBackdrop`/`DrawOverlay` hooks for art behind and in front of the menu. A screen that overrides none of them renders exactly as it did before the skin existed — which is what made it safe to change a base class with nine live consumers. All nine samples now skin their title screen and paint their own cast into `DrawBackdrop`, drawn from the same textures the game uses so the two cannot drift. `ButtonSpec(Id, Label, OnClick, Enabled)` captures each button; disabled buttons are rendered greyed and don't fire clicks. `Revealed()` re-invokes `GetButtons()` so state-dependent buttons (e.g. VN's Continue enabled when a save exists) can refresh on re-entry. All 9 sample games use this base; see `src/MonoGame.GameFramework.VisualNovel/GameStates/TitleState.cs` for the most complex consumer.

Per-game entities are plain classes — the library does not provide a shared entity base (the previous `Core.Entity` was deleted after 8 of 9 sample games skipped it). Shape your game's entities to fit the game; no inheritance required.

**Sprites and content** (added 2026-08-11 with Platformer's hero; see FINDINGS §1.17):
- **Asset layout**: `assets/` holds authoring sources (`.aseprite`) — committed, never compiled. Per-game `Content/sprites/` holds the exported PNGs the pipeline consumes. Frames are tiny (~350 bytes), so duplicating an export across games is cheaper than a shared-content-linking scheme.
- **`TextureFormat=Color`, never `Compressed`.** The spritefont blocks use `Compressed` (DXT); that's block compression and it mangles the hard 1px boundaries pixel art depends on. Also keep `ResizeToPowerOfTwo`/`MakeSquare` at `False` so a 32×32 source stays 32×32 — padding silently shifts every source rectangle. Copy the commented block in `template/Content/Content.mgcb`.
- **`SamplerState.PointClamp` on every `Begin` that draws sprites.** The default `LinearClamp` blurs pixel art at any scale ≠ 1:1. All nine samples now pass it, `TitleScreenState.Draw` passes it unconditionally, and `check-sprites` fails any bare `Begin()` in a project that ships textures — including on overlay states that currently draw only text, because the next sprite added to one would be silently blurred.
- **Scale by whole numbers only.** Non-integer scaling makes some pixels 1 screen-pixel and others 2.
- **Collision box and sprite destination may differ** — Platformer's player collides as 32×48 but draws 32×32 feet-anchored (`Player.SpriteDestination`), because the art happened to be authored on a 32×32 canvas. That specific mismatch is a Platformer detail, not a rule; games are free to make them identical. The only general point: don't assume one rect, and never non-uniformly scale pixel art to force a fit.
- **No library sprite/animation type yet, by design.** Platformer is consumer #1 and wants state→frame *selection*, not frame *cycling*. `SpriteSheet.Animated` stays deleted until a second consumer with a real multi-frame cycle justifies it. Nothing in the nine games needed it: sheets are indexed by an enum (`Board.Gem`, `TileKind`, `UnitType`+`Side`) and title-screen motion is two-frame or whole-pixel stepping.
- **Draw through `Rendering.PixelDraw`, not `SpriteBatch.Draw`.** It takes an integer scale rather than a destination rectangle, so a 1.5x scale or a squashed aspect is unrepresentable. `Sprite`/`Frame`/`FrameFootAnchored` for single draws, `Tile` for repeating fills and parallax. Reach for `SpriteBatch` directly only when you mean a genuine stretch — a full-lane alpha wash, say — and own that decision explicitly.
- **`Rendering.NineSlice` for anything resizable.** One 16x16 frame per game, 4px border, and every button, panel and card in that game comes out of it at whatever size the layout wants. `ContentBounds` gives the inside so a caller can lay text out without colliding with the border.

**Rendering path — direct-draw is the default, `DrawManager` is opt-in**: only 2 of 9 sample games (BattleGrid, Platformer) register sprites with `DrawManager`; the other 7 call `PixelDraw`/`Primitives.DrawRectangle` inline in their `Draw` overrides and are none the worse for it. Don't reach for `DrawManager` by default. It earns its place when you have a stable set of sprites whose draw order and tint you want managed centrally; for entities whose position is recomputed every frame, direct-draw is simpler and is what most of the samples do. (FINDINGS §8 Tier C #3.)

**SpriteSheet construction**: single factory, single frame.
- `SpriteSheet.Static(texture, destinationFrame, sourceFrame: ..., name: ...)` — creates a static (non-animated) sprite.
- `SpriteSheet.Tint` is mutable (defaults to `Color.White`); `DrawManager` respects it, so runtime tint/flash/fade works without replacing the sprite.
- Animation support was removed — no sample game used the multi-frame `Animated` factory. If a future game needs frame cycling, add it back then.

**UI helpers** (`UI/`):
- `HpBar.Draw(sb, rect, current, max, fill)` — background + fill bar. Fill width clamps `current` to `[0, max]`, handles `max == 0` defensively, no-ops on zero-width rect. Default background is `Color(25, 30, 45)`.
- `HpBar.DrawWithBorder(sb, rect, current, max, fill, border)` — adds a 2px border (configurable thickness). Use this for prominent bars; plain `Draw` for dozens of tiny on-enemy bars.
- `LogBox(maxLines, fadeStart, fadeStep, baseColor?)` — fixed-size scrolling-text panel. `Add(string)` enqueues + trims; `Draw(sb, font, origin, lineHeight)` lays lines top-down from `origin` with a fade gradient. For bottom-anchored placement, compute `origin.Y = bottomY - (Count - 1) * lineHeight`. Newest messages are fully opaque; oldest fade to `fadeStart * baseColor`.

**Pooling helpers** (`Pooling/`):
- `ObjectPool<T>(factory, prewarm, onRent?, onReturn?)` — Rent/Return pool for GC-sensitive entities (projectiles, enemies).
- `PooledEntitySet<T>(pool, isAlive)` — wraps `ObjectPool<T>` with a live list + rent/update/cull loop. `Rent()`, `UpdateAndCull(update, onCull?)`, `Cull(onCull?)`, `ReturnAll()`. `onCull` delegate fires per culled item before return, so consumers can tally side-effects (score, lives, gold) that differ by reason. See Shooter and TowerDefense `PlayState.cs` for usage.

**Debug overlay** (`Debugging/DebugOverlay.cs`):
- Tilde-toggled on-screen diagnostics. Registered as a DI singleton by `AddGameFrameworkManagers()` — every game gets it automatically.
- Wiring in `Game1` is three lines: resolve from DI, call `SetFont(_font)` after the font loads, and wrap the state-manager update: `_overlay.Update(gt); if (!_overlay.ShouldSkipUpdate) _gsm.Update(gt); … _gsm.Draw(sb, gt); _overlay.Draw(sb, gt);`.
- Keys (only while `Enabled`): `~` toggles the overlay, `Space` toggles pause, `.` steps one frame while paused.
- Built-in panel: FPS + frame-time ms, GC memory MB + gen0/1/2 counts, state-stack depth, UI element count, active timer count, and the last 12 events dispatched through `EventManager` (both string API and typed `Publish<T>`).
- Per-game watches: `_overlay.AddWatch("name", () => "value")` — registered once in `Game1.LoadContent` or a state's `Entered()`. `AddPooledSetWatch("name", set)` is a convenience for `PooledEntitySet<T>` — shows `N live / M pooled`. Shooter, TowerDefense, BattleGrid, and AutoBattler all register watches out of the box.
- **Pause semantics**: `ShouldSkipUpdate` only gates `GameStateManager.Update`. `KeyboardManager.Update`, `MouseManager.Update`, `UIManager.Update`, and the overlay's own `Update` all still run every frame — otherwise input would die while paused and the overlay couldn't react. Keep this in mind if a game-side subsystem must also halt during pause.

**Smoke harness** (`Testing/SmokeHarness.cs`):
- `Program.cs` parses `--exit-after N` from argv and pokes `ExitAfterFrames` on the DI-registered `SmokeHarness`. `Game1.Update` calls `_smoke.Tick()` and `Exit()`s when the budget runs out. Disabled by default (nothing happens without the flag).
- Run one game: `dotnet run --project src/MonoGame.GameFramework.Shooter/MonoGame.GameFramework.Shooter.csproj -- --exit-after 60`
- Run all 9: `scripts/smoke-all.sh [frames] [timeout_seconds]` — builds the solution, launches each sample with a perl-based wall-clock timeout, tails the log on any failure. Catches init-time crashes the unit suite can't see (SpriteFont charset issues, content-pipeline cache staleness, service-resolution failures, LoadContent throws).
- **Requires a GUI session.** Run it from a Terminal you're logged into — not over SSH, and not from an agent/background shell. Each sample opens an SDL window; with no window server to composite it, the process gets past init and content-load, then blocks forever in `Cocoa_GL_SwapWindow` → `SDL_CondWait` waiting on a vsync. Every sample then times out with `rc=142` and a **zero-byte log**, which looks identical to a mass crash. If you see that pattern, check where you're running it before debugging the games. This is also why smoke isn't in CI — a headless runner needs a virtual display (xvfb).

**Dev tools** (`src/MonoGame.GameFramework.Tools/`, binary `mgf-tools`):
- `lint-spritefont --spritefont <path> --project <dir>` — scans a project's C# source for string literals containing characters the spritefont's `CharacterRegion`s don't cover. Prevents the em-dash / curly-quote / accented-letter crash class (FINDINGS §1.10). Approximate by design (regex-based, handles single-line comments and block comments, doesn't fully understand verbatim/interpolated strings — false positives are rare and obvious).
- `lint-all-samples [--repo <root>]` — lints each `src/MonoGame.GameFramework.*` sample against its own `Content/fonts/Arial.spritefont`. Exits non-zero on any uncovered character.
- `check-content-cache --project <dir>` / `check-content-cache-all [--repo <root>]` — flags `*.spritefont` sources whose mtime is newer than their compiled `.xnb` under `Content/bin/<platform>/Content/`. Catches the FINDINGS §1.10 "MGCB incremental cache skipped the rebuild, build is green, game crashes at runtime" class. Output includes the `rm -rf Content/bin Content/obj && dotnet build` fix hint.
- `check-versions [--repo <root>]` — XML-parses every `src/**/*.csproj`, reports cross-project `TargetFramework` mismatches and any `PackageReference` appearing at more than one version. Solo packages (e.g. `dotenv.net` in BattleGrid, `xunit`/`FluentAssertions` in Tests) are reported as INFO, not failures. Exits non-zero on any real mismatch.
- `check-boot --project <dir>` / `check-boot-all [--repo <root>]` — regex-checks each sample's `Game1.cs` for the four boot conventions: `Primitives.Initialize`, DebugOverlay DI resolution + `SetFont`, `!overlay.ShouldSkipUpdate` guard around `GameStateManager.Update`, and `SmokeHarness.Tick()` + `Exit()`. Catches convention drift in new samples.
- `check-sprites --project <dir>` / `check-sprites-all [--repo <root>]` — enforces the pixel-art conventions that are silently wrong by default and produce no build error (FINDINGS §1.17): `TextureFormat=Compressed` on a sprite, `ResizeToPowerOfTwo`/`MakeSquare` padding, and bare `SpriteBatch.Begin()` in a project that ships textures. **Self-limiting** — a project with no `TextureImporter` blocks in its `.mgcb` is skipped entirely, so the rectangle-only samples stay silent until they actually gain sprites. `Begin(...)` with any arguments is left alone deliberately: deciding whether an arbitrary overload passes a sampler is the compiler's job, and a false CI failure is worse than a missed warning.
- `check-palette --project <dir>` / `check-palette-all [--repo <root>]` — verifies every PNG under `<dir>/Content/sprites` (or `<dir>/sprites`) uses only colours from **its nearest palette**, with binary alpha. Palettes resolve per sprite: a `palette.gpl` beside the art wins, otherwise it walks up to `assets/palette.gpl`. The `-all` variant covers `assets/`, `template/`, and every sample, and names the palette each target was judged against. **Self-limiting** the same way `check-sprites` is: no palette file or no sprites means no output.
- `check-palettes [--repo <root>]` — the palettes as a *set*: every one carries the shared `outline` spine, and none has a ramp collision. `check-palette-all` only asks whether art matches *a* palette; this asks whether the palettes themselves still form a family. Runs in CI.
- `render-pix --input <pix> [--output <png>]` / `render-pix-all [--repo <root>]` — render `.pix` text sprites to the PNGs the content pipeline eats. Output defaults to the `.pix` path with a `.png` extension, which is where `check-pix-all` expects it.
- `check-pix-all [--repo <root>]` — re-renders every `.pix` in memory and diffs it against the committed PNG beside it. The `.pix` is the source; the PNG is a build artefact that happens to be committed because MGCB wants a file on disk, and this is what keeps that claim true. Runs in CI.
- `conform-sprite --input <png> --output <png> [--size NxM|N] [--palette <file>] [--alpha-threshold <0-255>]` — maps an arbitrary image onto the palette: nearest colour in Oklab, binary alpha, optional box-resample. This is the seam that keeps the art front-end swappable. Read the **mean delta** it prints: near zero means the source already respected the palette; over ~0.05 means it's being forced, and forced art looks forced — regenerate rather than ship it.
- Run any: `dotnet run --project src/MonoGame.GameFramework.Tools -- <command>`

**mgf-tools takes one NuGet dependency: `SixLabors.ImageSharp` 4.1.0.** There is no BCL image codec and `Texture2D` needs a `GraphicsDevice` a CLI tool shouldn't create.

Licence, decided 2026-08-12: ImageSharp moved to the Six Labors Split Licence at 3.0 — Apache-2.0 for open-source/non-commercial, paid otherwise. **This repo is a prototype**, so it runs the actively-maintained 4.x line rather than the 2.1.x maintenance branch. Every build therefore prints `No Six Labors license found`; that warning is expected, not a misconfiguration. If this ever ships commercially, either buy a licence or revert to 2.1.13 (last Apache-2.0 release, drop-in — the APIs used here are identical across 2.x/3.x/4.x).

### Pixel-art pipeline

`assets/STYLE.md` is the style bible. Palettes are GIMP `.gpl` format specifically because Aseprite loads and saves them natively — the artist and CI read the same bytes, so there is no second copy to drift from.

**One palette per game.** Each sample carries `Content/sprites/palette.gpl`, and the nearest palette walking up from a sprite governs it; anything without a local palette falls through to the repo-wide `assets/palette.gpl`. Thirteen colours could not carry nine art directions, which is the whole reason for the split — nine games built on one library are supposed to look like nine games. The only thing every palette shares is `outline` `#1A1A1A`, enforced by `check-palettes`; hue, ramp count and UI chrome are each game's own call, because that is exactly the axis the samples exist to differ on.

Directions, one per game: BattleGrid neon-net cyan-vs-magenta · Platformer sunset ruins (a strict superset of `assets/palette.gpl`, so the committed hero never moved) · Shooter rust-belt foundry · Puzzle candy-reef high-chroma · Roguelike ash catacombs, near-monochrome plus one torch · TowerDefense verdant siege · Rhythm vaporwave · VisualNovel twilight study (the only skin ramp in the repo) · AutoBattler tabletop felt and timber.

```
  1. GENERATE   any front-end — a .pix text grid, Aseprite by hand, PixelLab,
                Retro Diffusion, a traced concept image. Unconstrained.
  2. CONFORM    mgf-tools conform-sprite ...   (mechanical, deterministic;
                a .pix skips this — it cannot be off-palette)
  3. GATE       mgf-tools check-palette-all + check-palettes + check-pix-all
                (all three run in CI)
```

**The `.pix` format** is a third front-end and where most art in this repo now lives: a key mapping single characters to palette entry names, then a grid of those characters, rendered to PNG by `render-pix-all`. It exists for three properties — `git diff` shows *which pixels* changed, every pixel names a palette entry so it cannot be off-palette, and `.` is the only transparency so alpha is binary by construction. It is not a replacement for Aseprite: hand-polish and anything much above 64x64 still want a real editor. `.pix` sources live beside their PNGs in `Content/sprites/`, and `check-pix-all` fails the build if the two disagree.

`assets/WORKFLOW.md` is the worked example — giving the Platformer's rectangle enemy a real sprite, via Nano Banana Pro, Retro Diffusion, or a local LoRA. It carries the palette prompt block, the accept/reject thresholds for the conform report, and where each tool actually earns its place.

Step 3 is what makes step 1 safe to change: adopting a new art tool stops being a bet on discipline, because the worst case is a red build. Best of all, set the sprite to Indexed colour mode in Aseprite with `palette.gpl` loaded — then off-palette pixels are unpaintable in the first place, which beats catching them at the gate.

Consistency decomposes, and only half of it is enforceable. Palette and binary alpha are mechanical (`check-palette`); `TextureFormat`/padding/sampler are mechanical (`check-sprites`). Light direction, proportion, and outline convention are judgment calls that live in `STYLE.md` and nowhere else. Don't expect the linters to catch a sprite that's simply lit from the wrong side.

**Ramp collisions**: two colours in the same hue family at the same lightness are one ramp step drawn twice — invisible to the eye, recorded in every file, and they turn "the mid blue" into a coin flip. `check-palette` reports them as INFO; `check-palettes` fails on the missing-spine case but still reports collisions as INFO, because resolving one repaints committed art and picking the winner is a human call.

`assets/palette.gpl` shipped with one (`#3A5FA0` duplicating `#2A5DA0`'s slot, one stray pixel in every hero frame — the signature of a colour-picker nudge). It was resolved 2026-08-12: `#2A5DA0` won on ramp fit rather than usage, since both appeared exactly once per file. `PaletteRegistryTests.EveryPalette_IsCollisionFree` now keeps every palette that way.

**Lightness is Oklab, not HSV value** (changed 2026-08-16). HSV `V` is `max(r,g,b)`, so any colour with a 255 channel scores exactly 100 however pale it is — it called `#FF6CBA` and `#FFB0DE` the same brightness. Harmless while the repo had one palette of mid-tones; the moment the games gained bright ramps it produced five false positives, and a check that cries wolf gets muted. Under Oklab L the historical `#2A5DA0`/`#3A5FA0` pair sits 1.2 apart while the tightest legitimate neighbours in the repo (`blue-4`/`blue-5-hilite`) sit 5.8 apart, so the threshold stayed at 2.

**Editing `.aseprite` sources**: use the `aseprite` MCP server (diivi, `mcp__aseprite__*`) — four servers were benchmarked 2026-08-12, see `assets/WORKFLOW.md` for the table. Two rules: always `draw_pixels_at` (takes `layer_name` and a **1-based** `frame_index`) rather than bare `draw_pixels`, and reach for `run_lua_script` for anything the typed tools get wrong. `hero.aseprite` was rebuilt through `run_lua_script` — creating frames, cels, tags, durations and the palette in one pass — which is the pattern to copy for a new multi-frame sprite.

Whatever the server, **verify the write landed** rather than trusting the success response: `git status` on the file is the cheap check, and a re-export diffed against the committed PNGs is the thorough one. The previously-used pixel-plugin MCP reported `pixels_drawn: 1` on `hero.aseprite` while leaving the file byte-identical on disk.

**Scaffolding a new sample** (`scripts/new-sample.sh`):
- `scripts/new-sample.sh <Name>` copies `template/` → `src/MonoGame.GameFramework.<Name>/`, substitutes the `__SAMPLE__` marker, adds the project to `Game.sln`, builds once.
- The template wires up `DebugOverlay` + `SmokeHarness` + a `TitleState` that inherits `TitleScreenState` **and skins its buttons with a `NineSlice`** + a stub `PlayState` + a `Content.mgcb` with a pre-widened spritefont charset **and a working art path**: `Content/sprites/palette.gpl` (a starter palette carrying the shared `outline` spine), `placeholder.pix` and `ui-frame.pix` with their rendered PNGs, correct processor settings, `PointClamp` `Begin`, integer-scaled draw. Game #10 is one command and starts with the whole pipeline — palette, `.pix` sources, gates, skinned menu — already proven end-to-end. Marker substitution covers `*.pix` and `*.gpl` too, so the scaffolded game's palette and sprite names carry its own name.
- Marker substitution only touches text files (`*.cs`, `*.csproj`, `*.mgcb`, `*.spritefont`, `*.json`, `*.md`). It must stay that way: macOS `sed` aborts with `RE error: illegal byte sequence` on the template's binary PNG, and with `set -e` that leaves a half-created project behind.
- Verified end-to-end 2026-08-11: scaffolds, substitutes, builds, produces its `.xnb`, passes `check-boot`/`check-content-cache`/`lint-all-samples`, and runs.

**Rendering helpers**:
- `Rendering.Primitives` — call `Initialize(GraphicsDevice)` once in `Game1.LoadContent`, then use `Primitives.Pixel` or `Primitives.DrawRectangle(sb, rect, color)` anywhere a solid-color rectangle is needed. Avoids re-creating 1×1 textures per entity.
- `Rendering.GridMath.TryMouseToCell(mouse, origin, cellSize, cols, rows, out col, out row)` — bounds-checked conversion from mouse position to grid cell for games that don't use `TileMap` (e.g. TowerDefense's non-tile grid, AutoBattler's shop board). If you have a `TileMap`, prefer its `TryWorldToCell` instead.

**Pixel texture**: call `Rendering.Primitives.Initialize(GraphicsDevice)` once in `Game1.LoadContent`, then use `Primitives.Pixel` or `Primitives.DrawRectangle(spriteBatch, rect, color)` anywhere a solid-color rectangle is needed. Avoids re-creating 1×1 textures in every entity.

**GameStateManager lifecycle**: `PushState`, `PopState`, and `ChangeState` automatically call the right hooks — `Entered`, `Leaving`, `Obscuring`, `Revealed`. Consumers should not invoke these manually after a transition. Initial push fires `Entered` only (nothing to reveal from).

**`TimerManager.Every`/`After` are cancellable**: both return the `Timer` they allocate. Store the reference if the caller may need to `Cancel()` it (e.g. a spawn timer that should stop when the wave target is met). Ignored return value is fine for fire-and-forget timers.

**`TileMap.GetLayer<T>(name)` — cache the reference**: `GetLayer<T>` does a dictionary lookup + cast on every call. Consumers should resolve the layer once in their constructor and keep a field (e.g. `private readonly TileLayer<Gem> _gems`), not call `GetLayer` per frame. `TileMap.TryWorldToCell` bounds-checks and clamps; prefer it over the raw `WorldToCell` when a click can land outside the map.

**Text rendering** — the library offers two paths, by design:
- **`Text.TextManager`** — handle-based, batched text with group management (`ClearGroup`, `ScrollText`). Use for HUD text that persists frame-to-frame: score counters, chat logs, status labels. The tactical demo's enemy HP counter and console overlay use this.
- **Direct `SpriteBatch.DrawString(font, ...)`** — one-off labels drawn inline in a `GameState.Draw` override. Use for title screens, win/lose banners, button labels, anything whose position is computed per-frame. The platformer's "You Win!" overlay and title buttons use this.

Rule of thumb: if you'd register it and mutate occasionally, use `TextManager`. If you'd recompute position every frame, use `DrawString`.

### Sample games

Nine sample games live in `src/` next to the library. Each is playable end-to-end and validates the library against a different genre. See `FINDINGS.md` for the 9-game review that drove the library's current shape.

- **`BattleGrid`** — 3×3 real-time grid duel (Mega Man Battle Network style). WASD movement, Space fire, Tab chip selection (Cannon/Wide Shot/Sword/Recov), enemy AI alternating move/attack patterns. Entities in `Components/Entities/`; `BattleScene` owns them; `PlayState` has a `Mode` enum (`Playing`/`SelectingChip`/`Won`/`Lost`). Uses `EventManager` (string API), `TextManager` (tilde-console), `Components/UI/ConsoleUI.cs` debug overlay. Art: *Neon Netscape* — cyan is yours, magenta is the intruder's, and nothing else on screen is either; `amber-spark` appears only on the frame a hit lands.
- **`Platformer`** — side-scroller with gravity, variable-height jump, coyote time, jump buffer, separate-axis AABB collision, patrolling enemy, camera lerp + snap-on-respawn. A/D or arrows move, Space jumps, R respawns. Art: *Sunset Ruins* — dithered dusk sky, parallax clouds and a silhouetted aqueduct, mossy masonry tiles, and the original hero unchanged (its palette is a superset of `assets/palette.gpl`).
- **`Shooter`** — twin-stick arena survival. 2400×1600 arena, WASD move, mouse aim, pursuing enemies spawning every 1.5 s via `TimerManager.Every`, pooled `Projectile`/`Enemy` via `PooledEntitySet<T>`, `Camera2D.ScreenToWorld` for mouse aiming. R restarts. Art: *Rust Belt Arena* — worn steel plate underfoot, hazard-striped walls, and a radially symmetric walker and drone that differ only in hue, because a sprite that never rotates must read from every angle.
- **`Puzzle`** — 7×7 match-3 gems via `TileMap` + `TileLayer<Gem>`. Click two adjacent cells to swap; matches of 3+ clear with gravity-pack and refill cascade. `TileMap.TryWorldToCell` for mouse-pick; `TileLayer<T>.Swap` for swap. Art: *Candy Reef* — six gems that differ in silhouette as well as hue, so the board stays playable without colour vision; recessed cell sockets so a cascade reads as holes.
- **`Roguelike`** — turn-based dungeon crawler. Procedural rooms-and-corridors on a 60×34 `TileLayer<TileKind>`, bump-to-attack, monsters act after each player action, stairs descend. Combat log via `UI.LogBox`. Art: *Ash Catacombs* — six steps of blue-grey and no hue in the architecture, monsters in the one hue the walls never use, and a Chebyshev torch falloff applied as a tint on the tile sprite.
- **`TowerDefense`** — wave-based. 20×14 grid, fixed S-shaped path, click to place towers (`GridMath.TryMouseToCell`), pooled enemies/projectiles via `PooledEntitySet<T>` with per-cull-reason `onCull` delegate (leaked → lose life; killed → gold). Three waves, 5 lives. Art: *Verdant Siege* — turf vs. rutted road answers "can I build here?" by hue, so the grid needs no gridlines; gold coins rise and fade where an enemy dies.
- **`Rhythm`** — 4-lane rhythm game. Hard-coded 30-second chart, ±50 ms / ±150 ms hit windows, click SFX via `Audio.SoundManager`. Lane flash uses float-per-lane (not `Tween`). Art: *Vaporwave Lanes* — a sliced sun over a perspective grid, with lane floors drawn as a Bayer dither so the backdrop reads *through* the board; notes flip aqua→pink on the hit frame.
- **`VisualNovel`** — 11-node dialogue graph with a 3-way choice branching to three endings, character-by-character text reveal using `Tween.Float(0→1, Easing.QuadOut)`, save/load via `Persistence.SaveSystem`. Title screen has conditional Continue button (enabled only when a save exists) — see how `TitleScreenState.GetButtons()` returns a spec list that `Revealed()` refreshes. Art: *Twilight Study* — one room lit from two directions (cool window, warm lamp) and the only skin ramp in the repo, because portraits are a visual novel's entire visual budget.
- **`AutoBattler`** — auto-chess. Shop → Combat → PostCombat state graph. 3 unit types (Warrior/Archer/Tank) with implicit rock-paper-scissors. Drag-to-place during shop (hand-rolled; `UIManager.OnClick` is wrong shape for drags). Combat ticks every 0.5 s with hand-rolled BFS pathfinding; `EventManager.Subscribe<UnitDamaged>`/`Publish<T>` (typed API) drive the combat log. Art: *Tabletop Skirmish* — felt board, timber shop frame, and six pieces painted like painted miniatures; the three classes differ by silhouette because at 48px and moving that is all that survives.

All 9 games share the same shell: `Program.cs` → DI container setup → `Game1.cs` thin shell calling `Primitives.Initialize` and pushing `TitleState` → `GameStates/` directory. All title screens inherit from `Lifecycle.TitleScreenState`.

## Mac Setup Notes

Requires both x64 and ARM .NET SDKs. Also needs `brew install freetype freeimage` with symlinks to `/usr/local/lib/` (see README.md for exact paths).
