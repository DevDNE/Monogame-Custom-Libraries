# MonoGame.GameFramework

A reusable MonoGame DesktopGL framework library with **nine sample games across nine genres** that exercise and validate it. Uses Microsoft.Extensions.DependencyInjection for wiring services. See [`FINDINGS.md`](./FINDINGS.md) for the 9-game library review that drove the current shape.

## Setting Up Mac Environment

- Install both x64 and ARM .NET SDKs (not optional)
- Follow the official MonoGame setup closely, including the VS Code extensions: <https://docs.monogame.net/articles/getting_started/1_setting_up_your_development_environment_unix.html>
- `brew install freetype freeimage`
- Symlink shims so the native libs resolve at runtime:
  ```bash
  # freetype — first try
  sudo ln -s /opt/homebrew/lib/libfreetype6.dylib /usr/local/lib/libfreetype6
  # or if that path is missing (older formula)
  sudo ln -s /opt/homebrew/Cellar/freetype/2.13.2/lib/libfreetype.6.dylib /usr/local/lib/libfreetype6

  # freeimage
  sudo ln -s /opt/homebrew/Cellar/freeimage/3.18.0/lib/libfreeimage.dylib /usr/local/lib/libfreeimage
  ```
- Open a content pipeline file: `dotnet mgcb-editor ./src/MonoGame.GameFramework.BattleGrid/Content/Content.mgcb`
- Launch any sample via `dotnet run --project src/<project>/<project>.csproj` (see list below)

## Project Structure

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
  MonoGame.GameFramework.Tests/         ← 160 xUnit tests
```

## Library (`MonoGame.GameFramework`)

Services are registered via `Core/ServiceCollectionExtensions.AddGameFrameworkManagers()` and resolved via DI.

| Folder | Contents |
|---|---|
| `Audio/` | `SoundManager` |
| `Content/` | `AssetCatalog` |
| `Core/` | `ServiceCollectionExtensions` |
| `Debugging/` | `ILogger`, `ConsoleLogger`, `DebugOverlay` (tilde-toggled overlay; pause + step-frame) |
| `Events/` | `EventManager` (string API + typed `Subscribe<T>`/`Publish<T>`), `GameEventArgs` |
| `Input/` | `KeyboardManager`, `MouseManager`, `GamePadManager` |
| `Lifecycle/` | `GameState` + `GameStateManager`, `GameScene` + `SceneManager`, `TitleScreenState` |
| `Persistence/` | `SaveSystem`, `SaveFile<T>`, `SettingsManager` |
| `Pooling/` | `ObjectPool<T>`, `PooledEntitySet<T>` |
| `Rendering/` | `DrawManager`, `SpriteSheet`, `Camera2D`, `TileMap`, `TileLayer<T>`, `Primitives`, `GridMath` |
| `Testing/` | `SmokeHarness` (for `--exit-after N` headless smoke testing) |
| `Text/` | `TextManager` (handle-based), `TextElement`, `TextHandle` |
| `Timing/` | `TimerManager` (`After`/`Every`/`Over`) |
| `Tween/` | `Tween<T>` + `Tween.Float/Vec2/Color` factories, `Easing` (namespace: `MonoGame.GameFramework.Tweening`) |
| `UI/` | `UIManager` (hit-testing, focus, click handlers), `HpBar`, `LogBox` |

For detailed usage notes (text-rendering rules, lifecycle hooks, pooling patterns, etc.), see [`CLAUDE.md`](./CLAUDE.md).

## Sample games

Each is playable end-to-end. Run any with:

```bash
dotnet run --project src/MonoGame.GameFramework.BattleGrid/MonoGame.GameFramework.BattleGrid.csproj
```

| Sample | Genre | What it validates |
|---|---|---|
| `BattleGrid` | Real-time grid duel | `EventManager` (string API), `TextManager`, `GameStateManager` with inline-mode overlay |
| `Platformer` | Side-scroller | `Camera2D` with follow-lerp + snap-on-respawn, AABB collision, coyote time, jump buffer |
| `Shooter` | Twin-stick arena | `PooledEntitySet<Projectile>` + `<Enemy>`, `TimerManager.Every` for spawn waves, `Camera2D.ScreenToWorld` |
| `Puzzle` | Match-3 | `TileMap` + `TileLayer<Gem>`, `TileLayer.Swap`, `TileMap.TryWorldToCell` |
| `Roguelike` | Dungeon crawler | Procedural `TileLayer<TileKind>` generation, bump-to-attack, `UI.LogBox` combat log |
| `TowerDefense` | Wave defense | `GridMath.TryMouseToCell`, `PooledEntitySet<T>` with per-reason `onCull`, `TimerManager.Every` |
| `Rhythm` | 4-lane rhythm | `Audio.SoundManager` (click SFX), per-lane flash timers |
| `VisualNovel` | Dialogue tree | `SaveSystem` save/load, `Tween.Float` with `Easing.QuadOut` for char-by-char text reveal |
| `AutoBattler` | Auto-chess | `EventManager.Subscribe<T>`/`Publish<T>` typed API, multi-state graph (Title→Shop→Combat→PostCombat) |

All 9 title screens inherit from `Lifecycle.TitleScreenState` (override `BackgroundColor`, `TitleText`, `GetButtons()` — ~30 lines each vs. ~100 hand-rolled).

## Build, test, run

```bash
dotnet build Game.sln                          # Build all projects
dotnet test  Game.sln                          # Run the 160 library + tools tests
dotnet restore                                 # Restore NuGet packages
scripts/smoke-all.sh                           # Launch each of the 9 samples for 120 frames, fail on crash
scripts/new-sample.sh <Name>                   # Scaffold a new sample (copies template/, wires into Game.sln)
dotnet run --project src/MonoGame.GameFramework.Tools -- lint-all-samples
                                               # Check every sample's source against its spritefont charset
dotnet run --project src/MonoGame.GameFramework.Tools -- check-content-cache-all
                                               # Flag .spritefont sources newer than their compiled .xnb
dotnet run --project src/MonoGame.GameFramework.Tools -- check-versions
                                               # Diff TargetFramework / package versions across csprojs
dotnet run --project src/MonoGame.GameFramework.Tools -- check-boot-all
                                               # Verify each Game1.cs wires up the boot conventions
```

## Tests (`MonoGame.GameFramework.Tests`)

160 xUnit tests with FluentAssertions covering pure-logic pieces: `ObjectPool`, `PooledEntitySet`, `TimerManager`, `Tween`/`Easing`, `TileMap`/`TileLayer` (including `Swap` and `TryWorldToCell`), `GridMath`, `EventManager` (string + typed + `AnyEvent` hook), `SaveSystem`, `Camera2D`, `GameStateManager` lifecycle + `StackDepth`, `UIManager` + `ElementCount`, `SpriteSheet.Tint`, `TitleScreenState` registration/lifecycle, `HpBar` fill-width math, `LogBox` queue/trim, `DebugOverlay` state machine + watches + event tail + FPS rolling average, `SmokeHarness` arg parsing + frame counter, `SpritefontLinter` range parsing + character-coverage detection. Rendering-dependent code (SpriteBatch/SpriteFont/GraphicsDevice) is smoke-tested via the nine sample games, automated by `scripts/smoke-all.sh`.

## History & design rationale

- [`FINDINGS.md`](./FINDINGS.md) — 9-game library review. Tracks every observation that shaped the current API surface: what was kept (validated by real consumers), what was deleted (zero consumers across all 9 games), and what was extracted (duplicated across 3+ games). Includes the §8 "Suggested Execution Order" — a 6-commit cleanup that has been executed end-to-end.
- [`CLAUDE.md`](./CLAUDE.md) — guidance for AI agents working in this repo. Usage notes for every library helper, CLAUDE convention for text rendering, lifecycle, pooling, etc.
