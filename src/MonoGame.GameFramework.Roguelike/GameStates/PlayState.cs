using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.UI;

namespace MonoGame.GameFramework.Roguelike.GameStates;

public class PlayState : GameState
{
  private readonly KeyboardManager _keyboard;
  private readonly SpriteFont _font;
  private readonly int _viewportWidth;
  private readonly int _viewportHeight;
  private readonly Random _seedRng = new();

  private TileMap _map;
  private TileLayer<TileKind> _tiles;
  private List<Rectangle> _rooms;
  private PlayerActor _player;
  private readonly List<MonsterActor> _monsters = new();
  private readonly LogBox _log = new(maxLines: 5, fadeStart: 0.5f, fadeStep: 0.1f);
  private int _dungeonLevel = 1;
  private int _currentSeed;
  private bool _gameOver;

  private readonly RoguelikeArt _art;

  // Torchlight falloff, in tiles. Tuned so a 60x34 map still shows the player
  // roughly a room at a time — small enough that exploring means something,
  // large enough that a monster is visible before it is adjacent.
  private const int LitRadius = 5;
  private const int DarkRadius = 11;
  private static readonly Color UnlitTint = new(52, 58, 72);

  public PlayState(ServiceProvider sp, SpriteFont font, RoguelikeArt art, int vw, int vh)
  {
    _art = art;
    _keyboard = sp.GetService<KeyboardManager>();
    _font = font;
    _viewportWidth = vw;
    _viewportHeight = vh;
  }

  public override void Entered()
  {
    _dungeonLevel = 1;
    _currentSeed = _seedRng.Next();
    GenerateLevel();
    IsActive = true;
  }

  public override void Leaving() { }
  public override void Obscuring() => IsActive = false;
  public override void Revealed() => IsActive = true;

  private void GenerateLevel()
  {
    DungeonGenerator.Result result = DungeonGenerator.Generate(_currentSeed);
    _map = result.Map;
    _tiles = result.Tiles;
    _rooms = result.Rooms;
    // Preserve player HP between levels, but create once or retain.
    if (_player == null) _player = new PlayerActor(result.Spawn.X, result.Spawn.Y);
    else { _player.Col = result.Spawn.X; _player.Row = result.Spawn.Y; }
    CenterMapOrigin();
    SpawnMonsters();
    _log.Clear();
    AppendLog($"Dungeon level {_dungeonLevel}.");
    _gameOver = false;
  }

  private void CenterMapOrigin()
  {
    float mapW = DungeonGenerator.Columns * DungeonGenerator.CellSize;
    float mapH = DungeonGenerator.Rows * DungeonGenerator.CellSize;
    _map.Origin = new Vector2((_viewportWidth - mapW) * 0.5f, (_viewportHeight - mapH) * 0.5f - 10f);
  }

  private void SpawnMonsters()
  {
    _monsters.Clear();
    Random rng = new(_currentSeed * 7919 + _dungeonLevel);
    int perLevelCount = Math.Min(_rooms.Count - 1, 3 + _dungeonLevel);
    int spawned = 0;
    for (int i = 1; i < _rooms.Count && spawned < perLevelCount; i++)
    {
      Rectangle r = _rooms[i];
      int col = rng.Next(r.Left, r.Right);
      int row = rng.Next(r.Top, r.Bottom);
      int hp = 6 + rng.Next(_dungeonLevel * 2);
      int atk = 2 + rng.Next(_dungeonLevel);
      string name = (rng.Next(3)) switch { 0 => "rat", 1 => "goblin", _ => "skeleton" };
      Color tint = name switch
      {
        "rat" => new Color(170, 120, 100),
        "goblin" => new Color(120, 180, 100),
        _ => new Color(200, 200, 210),
      };
      _monsters.Add(new MonsterActor(col, row, name, hp, atk, tint));
      spawned++;
    }
  }

  public override void Update(GameTime gameTime)
  {
    if (_keyboard.WasKeyPressed(Keys.R))
    {
      _dungeonLevel = 1;
      _currentSeed = _seedRng.Next();
      _player = null;
      GenerateLevel();
      return;
    }

    if (_gameOver) return;

    int dCol = 0, dRow = 0;
    if (_keyboard.WasKeyPressed(Keys.W) || _keyboard.WasKeyPressed(Keys.Up)) dRow = -1;
    else if (_keyboard.WasKeyPressed(Keys.S) || _keyboard.WasKeyPressed(Keys.Down)) dRow = 1;
    else if (_keyboard.WasKeyPressed(Keys.A) || _keyboard.WasKeyPressed(Keys.Left)) dCol = -1;
    else if (_keyboard.WasKeyPressed(Keys.D) || _keyboard.WasKeyPressed(Keys.Right)) dCol = 1;
    else if (_keyboard.WasKeyPressed(Keys.OemPeriod) && _keyboard.IsKeyDown(Keys.LeftShift))
    {
      // '>' key: descend stairs if standing on them.
      if (_tiles[_player.Col, _player.Row] == TileKind.StairsDown)
      {
        _dungeonLevel++;
        _currentSeed = _seedRng.Next();
        GenerateLevel();
      }
      return;
    }

    if (dCol == 0 && dRow == 0) return;
    TryPlayerAction(dCol, dRow);
    if (!_gameOver) MonstersTurn();
    if (!_player.Alive) { _gameOver = true; AppendLog("You died. Press R for a new dungeon."); }
  }

  private void TryPlayerAction(int dCol, int dRow)
  {
    int nc = _player.Col + dCol;
    int nr = _player.Row + dRow;
    if (!_tiles.InBounds(nc, nr) || _tiles[nc, nr] == TileKind.Wall) return;

    foreach (MonsterActor m in _monsters)
    {
      if (!m.Alive) continue;
      if (m.Col == nc && m.Row == nr)
      {
        m.Damage(_player.AttackPower);
        AppendLog(m.Alive ? $"You hit the {m.Name} ({_player.AttackPower})." : $"You kill the {m.Name}.");
        return;
      }
    }

    _player.Col = nc;
    _player.Row = nr;
  }

  private void MonstersTurn()
  {
    foreach (MonsterActor m in _monsters)
    {
      if (!m.Alive) continue;
      int dCol = Math.Sign(_player.Col - m.Col);
      int dRow = Math.Sign(_player.Row - m.Row);
      // Adjacent → attack.
      if (Math.Abs(_player.Col - m.Col) + Math.Abs(_player.Row - m.Row) == 1)
      {
        _player.Damage(m.AttackPower);
        AppendLog($"The {m.Name} hits you ({m.AttackPower}).");
        continue;
      }
      // Simple move toward player; prefer the longer axis.
      if (Math.Abs(_player.Col - m.Col) >= Math.Abs(_player.Row - m.Row))
      {
        if (TryMonsterStep(m, dCol, 0)) continue;
        TryMonsterStep(m, 0, dRow);
      }
      else
      {
        if (TryMonsterStep(m, 0, dRow)) continue;
        TryMonsterStep(m, dCol, 0);
      }
    }
  }

  private bool TryMonsterStep(MonsterActor m, int dCol, int dRow)
  {
    if (dCol == 0 && dRow == 0) return false;
    int nc = m.Col + dCol;
    int nr = m.Row + dRow;
    if (!_tiles.InBounds(nc, nr) || _tiles[nc, nr] == TileKind.Wall) return false;
    if (_player.Col == nc && _player.Row == nr) return false;
    foreach (MonsterActor other in _monsters)
      if (other != m && other.Alive && other.Col == nc && other.Row == nr) return false;
    m.Col = nc;
    m.Row = nr;
    return true;
  }

  private void AppendLog(string msg) => _log.Add(msg);

  public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    Primitives.DrawRectangle(spriteBatch, new Rectangle(0, 0, _viewportWidth, _viewportHeight), new Color(18, 20, 26));
    DrawDungeon(spriteBatch);
    DrawActors(spriteBatch);
    DrawHud(spriteBatch);
    if (_gameOver) DrawGameOver(spriteBatch);
    spriteBatch.End();
  }

  /// <summary>
  /// Every tile, every frame, plus a torch-coloured falloff around the player.
  ///
  /// The falloff is a tint on the tile sprite rather than a second texture or a
  /// darkening rectangle over the top. One draw call either way, and tinting
  /// keeps the masonry visible in the dark instead of replacing it with a flat
  /// shape — a dungeon whose walls vanish at range reads as unfinished rather
  /// than as unlit.
  /// </summary>
  private void DrawDungeon(SpriteBatch spriteBatch)
  {
    for (int r = 0; r < DungeonGenerator.Rows; r++)
    {
      for (int c = 0; c < DungeonGenerator.Columns; c++)
      {
        Rectangle cell = _map.GetCellRect(c, r);
        TileKind kind = _tiles[c, r];
        Rectangle source = RoguelikeArt.TileFrame(kind);
        spriteBatch.Draw(_art.Tiles, cell, source, LightAt(c, r));
      }
    }
  }

  /// <summary>
  /// Torchlight: full brightness within a few tiles of the player, falling off
  /// to a cold near-black beyond it. Chebyshev distance rather than Euclidean,
  /// because the movement is grid-based and a circular light over square moves
  /// makes diagonal steps feel like they cost more.
  /// </summary>
  private Color LightAt(int col, int row)
  {
    int distance = Math.Max(Math.Abs(col - _player.Col), Math.Abs(row - _player.Row));
    if (distance <= LitRadius) return Color.White;
    if (distance >= DarkRadius) return UnlitTint;

    float t = (distance - LitRadius) / (float)(DarkRadius - LitRadius);
    return new Color(
      (int)MathHelper.Lerp(255, UnlitTint.R, t),
      (int)MathHelper.Lerp(255, UnlitTint.G, t),
      (int)MathHelper.Lerp(255, UnlitTint.B, t));
  }

  private void DrawActors(SpriteBatch spriteBatch)
  {
    foreach (MonsterActor m in _monsters)
    {
      if (!m.Alive) continue;
      // Monsters obey the same light as the floor they stand on, so one
      // wandering out of the torch radius dims rather than disappearing.
      Rectangle cell = _map.GetCellRect(m.Col, m.Row);
      spriteBatch.Draw(_art.Actors, cell,
        RoguelikeArt.ActorRect(RoguelikeArt.FrameForMonster(m.Name)), LightAt(m.Col, m.Row));
    }

    Rectangle pc = _map.GetCellRect(_player.Col, _player.Row);
    spriteBatch.Draw(_art.Actors, pc,
      RoguelikeArt.ActorRect(RoguelikeArt.ActorFrame.Hero), Color.White);
  }

  private void DrawHud(SpriteBatch spriteBatch)
  {
    // Top-left: HP + depth
    spriteBatch.DrawString(_font, $"HP {_player.Hp}/{_player.MaxHp}", new Vector2(20, 14), Color.White);
    spriteBatch.DrawString(_font, $"Depth {_dungeonLevel}", new Vector2(180, 14), new Color(230, 210, 120));

    // Top-right: controls
    const string hint = "WASD move   Shift+. descend   R new   Esc quit";
    Vector2 hs = _font.MeasureString(hint);
    spriteBatch.DrawString(_font, hint, new Vector2(_viewportWidth - hs.X - 20, 14), new Color(180, 180, 200));

    int logY = _viewportHeight - 24;
    Vector2 logOrigin = new(20, logY - (_log.Count - 1) * 22);
    _log.Draw(spriteBatch, _font, logOrigin);
  }

  private void DrawGameOver(SpriteBatch spriteBatch)
  {
    Primitives.DrawRectangle(spriteBatch, new Rectangle(0, 0, _viewportWidth, _viewportHeight), new Color(0, 0, 0, 180));
    const string heading = "You Died";
    const string hint = "Press R for a new dungeon";
    Vector2 hs = _font.MeasureString(heading);
    Vector2 ih = _font.MeasureString(hint);
    Vector2 c = new(_viewportWidth * 0.5f, _viewportHeight * 0.5f);
    spriteBatch.DrawString(_font, heading, new Vector2(c.X - hs.X * 0.5f, c.Y - 30), new Color(230, 90, 100));
    spriteBatch.DrawString(_font, hint, new Vector2(c.X - ih.X * 0.5f, c.Y + 10), new Color(210, 210, 220));
  }
}
