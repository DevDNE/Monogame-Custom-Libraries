using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Pooling;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.Shooter.Entities;
using MonoGame.GameFramework.Debugging;
using MonoGame.GameFramework.Timing;
using MonoGame.GameFramework.UI;

namespace MonoGame.GameFramework.Shooter.GameStates;

public class PlayState : GameState
{
  private const int ArenaWidth = 2400;
  private const int ArenaHeight = 1600;
  private const float EnemySpawnInterval = 1.5f;
  private const int EnemiesPerSpawn = 2;

  private readonly KeyboardManager _keyboard;
  private readonly MouseManager _mouse;
  private readonly TimerManager _timers;
  private readonly SpriteFont _font;
  private readonly ShooterArt _art;

  // Kill flashes: a world position and a countdown. Same reasoning as
  // BattleGrid's sparks — a particle system for this would be all machinery
  // and no feature.
  private readonly List<(Vector2 Position, float Remaining)> _bursts = new();
  private const float BurstDuration = 0.22f;

  private readonly int _viewportWidth;
  private readonly int _viewportHeight;
  private readonly Random _random = new();

  private readonly Player _player = new();
  private readonly PooledEntitySet<Projectile> _projectiles;
  private readonly PooledEntitySet<Enemy> _enemies;

  private Camera2D _camera;
  private int _score;
  private bool _gameOver;

  public PlayState(ServiceProvider serviceProvider, SpriteFont font, ShooterArt art, int viewportWidth, int viewportHeight)
  {
    _art = art;
    _keyboard = serviceProvider.GetService<KeyboardManager>();
    _mouse = serviceProvider.GetService<MouseManager>();
    _timers = serviceProvider.GetService<TimerManager>();
    _font = font;
    _viewportWidth = viewportWidth;
    _viewportHeight = viewportHeight;

    _projectiles = new PooledEntitySet<Projectile>(
      new ObjectPool<Projectile>(() => new Projectile(), prewarm: 32, onReturn: p => p.Alive = false),
      isAlive: p => p.Alive);
    _enemies = new PooledEntitySet<Enemy>(
      new ObjectPool<Enemy>(() => new Enemy(), prewarm: 16, onReturn: e => e.Alive = false),
      isAlive: e => e.Alive);

    DebugOverlay overlay = serviceProvider.GetService<DebugOverlay>();
    overlay.AddPooledSetWatch("projectiles", _projectiles);
    overlay.AddPooledSetWatch("enemies", _enemies);
    overlay.AddWatch("player hp", () => $"{_player.Hp}/{Player.MaxHp}");
    overlay.AddWatch("score", () => _score.ToString());
    overlay.AddWatch("mouse world", () => _camera == null
      ? "(pre-spawn)"
      : _camera.ScreenToWorld(_mouse.GetMousePosition()).ToString());
  }

  public override void Entered()
  {
    StartFresh();
    IsActive = true;
  }

  public override void Leaving()
  {
    _projectiles.ReturnAll();
    _enemies.ReturnAll();
    _timers.Clear();
  }

  public override void Obscuring() => IsActive = false;
  public override void Revealed() => IsActive = true;

  private void StartFresh()
  {
    _projectiles.ReturnAll();
    _enemies.ReturnAll();
    _timers.Clear();
    _score = 0;
    _gameOver = false;
    _player.Spawn(new Vector2(ArenaWidth * 0.5f, ArenaHeight * 0.5f));
    _camera = new Camera2D(new Vector2(_viewportWidth, _viewportHeight))
    {
      Position = _player.Position,
      Target = _player.Position,
      FollowLerp = 0.15f,
    };
    _timers.Every(EnemySpawnInterval, SpawnWave);
  }

  public override void Update(GameTime gameTime)
  {
    if (_keyboard.WasKeyPressed(Keys.R)) { Leaving(); StartFresh(); return; }
    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
    UpdateBursts(dt);
    _timers.Update(gameTime);

    if (!_gameOver)
    {
      _player.Update(dt, _keyboard);
      HandleFiring();
      UpdateProjectiles(dt);
      UpdateEnemies(dt);
      CheckCollisions();
      if (!_player.IsAlive) _gameOver = true;
    }

    _camera.Target = _player.Position;
    _camera.Update(gameTime);
  }

  private void HandleFiring()
  {
    bool firePressed = _mouse.IsLeftMouseButtonDown() || _keyboard.IsKeyDown(Keys.Space);
    if (!firePressed) return;
    if (!_player.TryConsumeFireCooldown()) return;

    Vector2 mouseScreen = _mouse.GetMousePosition();
    Vector2 mouseWorld = _camera.ScreenToWorld(mouseScreen);
    Vector2 dir = mouseWorld - _player.Position;
    if (dir.LengthSquared() < 0.01f) dir = new Vector2(1, 0);
    else dir.Normalize();

    Projectile p = _projectiles.Rent();
    p.Launch(_player.Position, dir);
  }

  private void UpdateProjectiles(float dt)
    => _projectiles.UpdateAndCull(p => p.Update(dt));

  private void UpdateEnemies(float dt)
    => _enemies.UpdateAndCull(e => e.Update(dt, _player.Position));

  private void CheckCollisions()
  {
    // projectile vs enemy
    IReadOnlyList<Projectile> projectiles = _projectiles.Live;
    IReadOnlyList<Enemy> enemies = _enemies.Live;
    for (int pi = projectiles.Count - 1; pi >= 0; pi--)
    {
      Projectile p = projectiles[pi];
      if (!p.Alive) continue;
      for (int ei = enemies.Count - 1; ei >= 0; ei--)
      {
        Enemy e = enemies[ei];
        if (!e.Alive) continue;
        if (p.Bounds.Intersects(e.Bounds))
        {
          p.Alive = false;
          e.Alive = false;
          _bursts.Add((e.Position, BurstDuration));
          _score += 10;
          break;
        }
      }
    }

    // enemy vs player
    foreach (Enemy e in enemies)
    {
      if (!e.Alive) continue;
      if (e.Bounds.Intersects(_player.Bounds))
      {
        _player.TryDamage(Enemy.ContactDamage);
      }
    }
  }

  private void SpawnWave()
  {
    if (_gameOver) return;
    for (int i = 0; i < EnemiesPerSpawn; i++)
    {
      // Spawn just outside the current viewport, on a random edge.
      Vector2 camPos = _camera.Position;
      float half = _viewportWidth * 0.5f + 40;
      float halfY = _viewportHeight * 0.5f + 40;
      Vector2 pos = _random.Next(4) switch
      {
        0 => new Vector2(camPos.X - half, camPos.Y + (float)(_random.NextDouble() - 0.5) * _viewportHeight),
        1 => new Vector2(camPos.X + half, camPos.Y + (float)(_random.NextDouble() - 0.5) * _viewportHeight),
        2 => new Vector2(camPos.X + (float)(_random.NextDouble() - 0.5) * _viewportWidth, camPos.Y - halfY),
        _ => new Vector2(camPos.X + (float)(_random.NextDouble() - 0.5) * _viewportWidth, camPos.Y + halfY),
      };
      pos.X = Math.Clamp(pos.X, Enemy.Size, ArenaWidth - Enemy.Size);
      pos.Y = Math.Clamp(pos.Y, Enemy.Size, ArenaHeight - Enemy.Size);
      Enemy e = _enemies.Rent();
      e.Spawn(pos);
    }
  }

  public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    // World pass
    spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix(), samplerState: SamplerState.PointClamp);
    DrawArena(spriteBatch);
    foreach (Enemy e in _enemies.Live) e.Draw(spriteBatch, _art);
    foreach (Projectile p in _projectiles.Live) p.Draw(spriteBatch, _art);
    _player.Draw(spriteBatch, _art);
    DrawBursts(spriteBatch);
    spriteBatch.End();

    // Screen-space HUD. PointClamp because the frame behind the game-over
    // panel is a sprite, and LinearClamp would soften its corners.
    spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    DrawHud(spriteBatch);
    if (_gameOver) DrawGameOver(spriteBatch);
    spriteBatch.End();
  }

  private void DrawArena(SpriteBatch spriteBatch)
  {
    // The whole 2400x1600 floor is one Tile call. It looks wasteful and is not:
    // Tile only emits the rows and columns that intersect the destination, and
    // the destination here is the arena, not the viewport — MonoGame culls the
    // off-screen quads, and the alternative is a visibility calculation that
    // would have to know about the camera.
    PixelDraw.Tile(spriteBatch, _art.Floor,
      new Rectangle(0, 0, ArenaWidth, ArenaHeight), ShooterArt.FloorScale);

    // Hazard stripes on all four walls, inside the play area so the boundary is
    // something the player reads rather than something they discover.
    const int band = 16 * ShooterArt.HazardScale;
    foreach (Rectangle edge in new[]
    {
      new Rectangle(0, 0, ArenaWidth, band),
      new Rectangle(0, ArenaHeight - band, ArenaWidth, band),
      new Rectangle(0, 0, band, ArenaHeight),
      new Rectangle(ArenaWidth - band, 0, band, ArenaHeight),
    })
    {
      PixelDraw.Tile(spriteBatch, _art.Hazard, edge, ShooterArt.HazardScale);
    }
  }

  private void DrawBursts(SpriteBatch spriteBatch)
  {
    foreach ((Vector2 position, float remaining) in _bursts)
    {
      // Shrinks over its life, so the first frame is the biggest. A flash that
      // holds its size and then disappears reads as a dropped frame.
      int scale = remaining > BurstDuration * 0.6f ? 2 : 1;
      int half = _art.Burst.Width * scale / 2;
      PixelDraw.Sprite(spriteBatch, _art.Burst,
        (int)position.X - half, (int)position.Y - half, scale);
    }
  }

  private void UpdateBursts(float dt)
  {
    for (int i = _bursts.Count - 1; i >= 0; i--)
    {
      float remaining = _bursts[i].Remaining - dt;
      if (remaining <= 0f) _bursts.RemoveAt(i);
      else _bursts[i] = (_bursts[i].Position, remaining);
    }
  }

  private void DrawHud(SpriteBatch spriteBatch)
  {
    HpBar.Draw(spriteBatch, new Rectangle(20, 20, 240, 22), _player.Hp, Player.MaxHp, new Color(80, 200, 140));
    spriteBatch.DrawString(_font, $"HP {_player.Hp}", new Vector2(20, 46), Color.White);

    // Score top-right
    string score = $"Score {_score}";
    Vector2 sz = _font.MeasureString(score);
    spriteBatch.DrawString(_font, score, new Vector2(_viewportWidth - sz.X - 20, 20), Color.White);

    // Hint bottom
    const string hint = "WASD move   Mouse aim   Click / Space fire   R restart   Esc quit";
    Vector2 hSz = _font.MeasureString(hint);
    spriteBatch.DrawString(_font, hint, new Vector2(_viewportWidth * 0.5f - hSz.X * 0.5f, _viewportHeight - 30), new Color(200, 200, 215));
  }

  private void DrawGameOver(SpriteBatch spriteBatch)
  {
    Primitives.DrawRectangle(spriteBatch, new Rectangle(0, 0, _viewportWidth, _viewportHeight), new Color(0, 0, 0, 180));
    const string heading = "Game Over";
    string final = $"Final score: {_score}";
    const string hint = "Press R to restart";
    Vector2 hSz = _font.MeasureString(heading);
    Vector2 fSz = _font.MeasureString(final);
    Vector2 iSz = _font.MeasureString(hint);
    Vector2 c = new(_viewportWidth * 0.5f, _viewportHeight * 0.5f);
    spriteBatch.DrawString(_font, heading, new Vector2(c.X - hSz.X * 0.5f, c.Y - 60), new Color(230, 90, 100));
    spriteBatch.DrawString(_font, final,   new Vector2(c.X - fSz.X * 0.5f, c.Y - 10), Color.White);
    spriteBatch.DrawString(_font, hint,    new Vector2(c.X - iSz.X * 0.5f, c.Y + 30), new Color(210, 210, 220));
  }
}
