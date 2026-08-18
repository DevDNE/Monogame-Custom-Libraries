using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Debugging;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Pooling;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.Timing;
using MonoGame.GameFramework.TowerDefense.Entities;

namespace MonoGame.GameFramework.TowerDefense.GameStates;

public class PlayState : GameState
{
  private const int StartGold = 40;
  private const int StartLives = 5;
  private const int GoldPerKill = 5;
  private const int GoldPerWaveClear = 15;
  private const float SpawnInterval = 0.8f;
  private const float BetweenWavesCooldown = 5f;
  private static readonly int[] WaveSizes = { 5, 8, 12 };

  private enum Status { Playing, Victory, Defeat }

  private readonly KeyboardManager _keyboard;
  private readonly MouseManager _mouse;
  private readonly TimerManager _timers;
  private readonly SpriteFont _font;
  private readonly int _viewportWidth;
  private readonly int _viewportHeight;

  private MapPath.TileMapOrigin _origin;
  private HashSet<(int col, int row)> _pathCells = new();
  private readonly Dictionary<(int col, int row), Tower> _towers = new();
  private readonly PooledEntitySet<Enemy> _enemies = new(
    new ObjectPool<Enemy>(() => new Enemy(), prewarm: 16),
    isAlive: e => e.Alive && !e.Leaked);
  private readonly PooledEntitySet<Projectile> _projectiles = new(
    new ObjectPool<Projectile>(() => new Projectile(), prewarm: 32),
    isAlive: p => p.Alive);

  private int _gold = StartGold;
  private int _lives = StartLives;
  private int _waveIndex;              // 0 = pre-wave 1
  private int _enemiesToSpawnInWave;
  private float _intermissionRemaining;
  private Status _status = Status.Playing;

  private readonly TowerDefenseArt _art;

  // Gold pickups: a world position and a countdown, drawn where an enemy died.
  private readonly List<(Vector2 Position, float Remaining)> _coins = new();
  private const float BountyDuration = 0.6f;

  public PlayState(ServiceProvider sp, SpriteFont font, TowerDefenseArt art, int vw, int vh)
  {
    _art = art;
    _keyboard = sp.GetService<KeyboardManager>();
    _mouse = sp.GetService<MouseManager>();
    _timers = sp.GetService<TimerManager>();
    _font = font;
    _viewportWidth = vw;
    _viewportHeight = vh;

    DebugOverlay overlay = sp.GetService<DebugOverlay>();
    overlay.AddPooledSetWatch("enemies", _enemies);
    overlay.AddPooledSetWatch("projectiles", _projectiles);
    overlay.AddWatch("gold", () => _gold.ToString());
    overlay.AddWatch("lives", () => _lives.ToString());
    overlay.AddWatch("wave", () => $"{_waveIndex}/{WaveSizes.Length}");
    overlay.AddWatch("mouse cell", () =>
    {
      if (GridMath.TryMouseToCell(
            _mouse.GetMousePosition(),
            new Vector2(_origin.X, _origin.Y),
            MapPath.CellSize, MapPath.Columns, MapPath.Rows,
            out int c, out int r)) return $"({c},{r})";
      return "(off-map)";
    });
  }

  public override void Entered()
  {
    StartFresh();
    IsActive = true;
  }

  public override void Leaving()
  {
    ClearLiveObjects();
    _towers.Clear();
    _timers.Clear();
  }

  public override void Obscuring() => IsActive = false;
  public override void Revealed() => IsActive = true;

  private void StartFresh()
  {
    ClearLiveObjects();
    _towers.Clear();
    _timers.Clear();
    _gold = StartGold;
    _lives = StartLives;
    _status = Status.Playing;
    _waveIndex = 0;
    _intermissionRemaining = 3f;

    float mapW = MapPath.Columns * MapPath.CellSize;
    float mapH = MapPath.Rows * MapPath.CellSize;
    _origin = new MapPath.TileMapOrigin(
      (_viewportWidth - mapW) * 0.5f,
      (_viewportHeight - mapH) * 0.5f + 12f);
    _pathCells = MapPath.GetPathCells();
  }

  private void ClearLiveObjects()
  {
    _enemies.ReturnAll();
    _projectiles.ReturnAll();
  }

  public override void Update(GameTime gameTime)
  {
    if (_keyboard.WasKeyPressed(Keys.R)) { StartFresh(); return; }
    if (_status != Status.Playing) return;

    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
    UpdateCoins(dt);
    _timers.Update(gameTime);

    HandlePlacement();
    TickWaveLifecycle(dt);
    UpdateEnemies(dt);
    UpdateTowers(dt);
    UpdateProjectiles(dt);
    CheckOutcome();
  }

  private void HandlePlacement()
  {
    if (!_mouse.WasLeftMouseButtonPressed()) return;
    Vector2 m = _mouse.GetMousePosition();
    if (!GridMath.TryMouseToCell(m, new Vector2(_origin.X, _origin.Y), MapPath.CellSize, MapPath.Columns, MapPath.Rows, out int col, out int row)) return;
    if (_pathCells.Contains((col, row))) return;
    if (_towers.ContainsKey((col, row))) return;
    if (_gold < Tower.Cost) return;

    Vector2 pos = MapPath.WorldPosition(_origin, col, row);
    _towers[(col, row)] = new Tower(col, row, pos);
    _gold -= Tower.Cost;
  }

  private void TickWaveLifecycle(float dt)
  {
    if (_intermissionRemaining > 0f)
    {
      _intermissionRemaining -= dt;
      if (_intermissionRemaining <= 0f) StartNextWave();
      return;
    }
    if (_enemiesToSpawnInWave == 0 && _enemies.Count == 0)
    {
      // Wave complete
      _gold += GoldPerWaveClear;
      _intermissionRemaining = BetweenWavesCooldown;
    }
  }

  private void StartNextWave()
  {
    if (_waveIndex >= WaveSizes.Length)
    {
      // All waves survived = victory
      if (_enemies.Count == 0) _status = Status.Victory;
      return;
    }
    _enemiesToSpawnInWave = WaveSizes[_waveIndex];
    _waveIndex++;
    _timers.Every(SpawnInterval, TrySpawnEnemy);
  }

  private void TrySpawnEnemy()
  {
    if (_enemiesToSpawnInWave <= 0) return;
    (int c, int r) = MapPath.Waypoints[0];
    Vector2 spawn = MapPath.WorldPosition(_origin, c, r);
    Enemy e = _enemies.Rent();
    e.Spawn(spawn);
    _enemiesToSpawnInWave--;
  }

  private void UpdateEnemies(float dt)
    => _enemies.UpdateAndCull(
         e => e.Update(dt, _origin),
         onCull: e =>
         {
           if (e.Leaked) { _lives--; return; }
           _gold += GoldPerKill;
           _coins.Add((e.Position, BountyDuration));
         });

  private void UpdateTowers(float dt)
  {
    foreach (Tower t in _towers.Values)
    {
      Enemy target = t.TryFire(dt, _enemies.Live);
      if (target != null)
      {
        Projectile p = _projectiles.Rent();
        p.Launch(t.Position, target);
      }
    }
  }

  private void UpdateProjectiles(float dt)
    => _projectiles.UpdateAndCull(p => p.Update(dt));

  private void CheckOutcome()
  {
    if (_lives <= 0) { _status = Status.Defeat; return; }
    if (_waveIndex >= WaveSizes.Length && _enemiesToSpawnInWave == 0 && _enemies.Count == 0 && _intermissionRemaining <= 0f)
    {
      _status = Status.Victory;
    }
  }

  public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    DrawMap(spriteBatch);
    foreach (Tower t in _towers.Values) t.Draw(spriteBatch, _art);
    foreach (Enemy e in _enemies.Live) e.Draw(spriteBatch, _art);
    foreach (Projectile p in _projectiles.Live) p.Draw(spriteBatch, _art);
    DrawCoins(spriteBatch);
    DrawHud(spriteBatch);
    if (_status != Status.Playing) DrawOutcome(spriteBatch);
    spriteBatch.End();
  }

  private void DrawMap(SpriteBatch spriteBatch)
  {
    // Turf beyond the board as well, so the hillside runs off the edges of the
    // screen instead of the map floating on a flat backdrop.
    PixelDraw.Tile(spriteBatch, _art.Turf,
      new Rectangle(0, 0, _viewportWidth, _viewportHeight), TowerDefenseArt.CellScale);

    for (int r = 0; r < MapPath.Rows; r++)
    {
      for (int c = 0; c < MapPath.Columns; c++)
      {
        // Cells butt up with no 1px gap. The old inset drew a grid by letting
        // background show through; the tiles now differ by hue, which reads at
        // a glance and does not put a dark line under every tower.
        Rectangle rect = new(
          (int)_origin.X + c * MapPath.CellSize,
          (int)_origin.Y + r * MapPath.CellSize,
          MapPath.CellSize, MapPath.CellSize);
        Texture2D tile = _pathCells.Contains((c, r)) ? _art.Path : _art.Turf;
        PixelDraw.Tile(spriteBatch, tile, rect, TowerDefenseArt.CellScale);
      }
    }
  }

  private void DrawCoins(SpriteBatch spriteBatch)
  {
    foreach ((Vector2 position, float remaining) in _coins)
    {
      // Rises as it fades — the one motion everyone reads as "you gained
      // something" without a number attached.
      int lift = (int)((BountyDuration - remaining) * 40f);
      PixelDraw.Sprite(spriteBatch, _art.Coin,
        (int)position.X - _art.Coin.Width, (int)position.Y - lift, 2);
    }
  }

  private void UpdateCoins(float dt)
  {
    for (int i = _coins.Count - 1; i >= 0; i--)
    {
      float remaining = _coins[i].Remaining - dt;
      if (remaining <= 0f) _coins.RemoveAt(i);
      else _coins[i] = (_coins[i].Position, remaining);
    }
  }

  private void DrawHud(SpriteBatch spriteBatch)
  {
    spriteBatch.DrawString(_font, $"Gold {_gold}",  new Vector2(20, 12), new Color(255, 230, 120));
    spriteBatch.DrawString(_font, $"Lives {_lives}", new Vector2(160, 12), new Color(230, 120, 120));
    string wave = _waveIndex == 0
      ? $"Next wave in {_intermissionRemaining:0.0}s"
      : _intermissionRemaining > 0f
        ? $"Wave {_waveIndex}/{WaveSizes.Length} cleared. Next in {_intermissionRemaining:0.0}s"
        : $"Wave {_waveIndex}/{WaveSizes.Length}";
    spriteBatch.DrawString(_font, wave, new Vector2(320, 12), Color.White);

    const string hint = "Click grid cell to place tower (20g)   R restart   Esc quit";
    Vector2 hs = _font.MeasureString(hint);
    spriteBatch.DrawString(_font, hint, new Vector2(_viewportWidth * 0.5f - hs.X * 0.5f, _viewportHeight - 28), new Color(180, 180, 200));
  }

  private void DrawOutcome(SpriteBatch spriteBatch)
  {
    Primitives.DrawRectangle(spriteBatch, new Rectangle(0, 0, _viewportWidth, _viewportHeight), new Color(0, 0, 0, 180));
    string heading = _status == Status.Victory ? "Victory!" : "Defeat";
    Color color = _status == Status.Victory ? new Color(120, 220, 140) : new Color(230, 90, 100);
    const string hint = "Press R to play again";
    Vector2 hs = _font.MeasureString(heading);
    Vector2 ih = _font.MeasureString(hint);
    Vector2 c = new(_viewportWidth * 0.5f, _viewportHeight * 0.5f);
    spriteBatch.DrawString(_font, heading, new Vector2(c.X - hs.X * 0.5f, c.Y - 30), color);
    spriteBatch.DrawString(_font, hint, new Vector2(c.X - ih.X * 0.5f, c.Y + 10), new Color(210, 210, 220));
  }
}
