using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Events;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.UI;

namespace MonoGame.GameFramework.AutoBattler.GameStates;

/// <summary>
/// Auto-combat tick. Every 0.5s each living unit takes a turn: attack the
/// nearest enemy in range, or step one tile toward the nearest enemy via
/// hand-rolled BFS. Combat ends when one side has no living units.
/// </summary>
public class CombatState : GameState
{
  private const float TickInterval = 0.5f;
  private const int BoardCellSize = 80;

  private readonly EventManager _events;
  private readonly SpriteFont _font;
  private readonly GameModel _model;
  private readonly int _viewportWidth;
  private readonly int _viewportHeight;
  private readonly Action<Side?> _onCombatEnded;

  private readonly LogBox _log = new(maxLines: 6, baseColor: new Color(220, 220, 235));
  private float _tickAccum;
  private Vector2 _boardOrigin;
  private bool _handlersSubscribed;

  private readonly AutoBattlerArt _art;

  public CombatState(ServiceProvider sp, SpriteFont font, GameModel model, AutoBattlerArt art, int vw, int vh, Action<Side?> onCombatEnded)
  {
    _art = art;
    _events = sp.GetService<EventManager>();
    _font = font;
    _model = model;
    _viewportWidth = vw;
    _viewportHeight = vh;
    _onCombatEnded = onCombatEnded;
  }

  public override void Entered()
  {
    _boardOrigin = new Vector2(
      (_viewportWidth - Board.Columns * BoardCellSize) * 0.5f,
      80f);
    _log.Clear();
    _tickAccum = 0f;
    if (!_handlersSubscribed)
    {
      _events.Subscribe<UnitDamaged>(OnDamaged);
      _events.Subscribe<UnitKilled>(OnKilled);
      _handlersSubscribed = true;
    }
    AppendLog($"-- Round {_model.Round}: combat begins --");
    IsActive = true;
  }

  public override void Leaving()
  {
    _events.Unsubscribe<UnitDamaged>(OnDamaged);
    _events.Unsubscribe<UnitKilled>(OnKilled);
    _handlersSubscribed = false;
  }

  public override void Obscuring() => IsActive = false;
  public override void Revealed() => IsActive = true;

  public override void Update(GameTime gameTime)
  {
    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
    _tickAccum += dt;
    while (_tickAccum >= TickInterval)
    {
      _tickAccum -= TickInterval;
      RunTick();
      if (CheckEnded()) return;
    }
  }

  private void RunTick()
  {
    // Snapshot to avoid mutating while iterating. Losers removed at end.
    List<Unit> actors = new(_model.Board.Units);
    foreach (Unit actor in actors)
    {
      if (!actor.Alive) continue;
      Unit nearest = FindNearestEnemy(actor);
      if (nearest == null) continue;

      int dist = Pathing.ChebyshevDistance(actor, nearest);
      if (dist <= actor.Stats.Range)
      {
        // Attack
        nearest.Damage(actor.Stats.Attack);
        _events.Publish(new UnitDamaged { Attacker = actor, Victim = nearest, Amount = actor.Stats.Attack });
        if (!nearest.Alive)
          _events.Publish(new UnitKilled { Killer = actor, Victim = nearest });
      }
      else
      {
        // Step toward
        (int, int)? step = Pathing.NextStepToward(_model.Board, actor, nearest);
        if (step is (int c, int r) && _model.Board.UnitAt(c, r) == null)
        {
          actor.Col = c;
          actor.Row = r;
        }
      }
    }
    _model.Board.Units.RemoveAll(u => !u.Alive);
  }

  private Unit FindNearestEnemy(Unit actor)
  {
    Unit best = null;
    int bestD = int.MaxValue;
    foreach (Unit u in _model.Board.Units)
    {
      if (!u.Alive || u.Side == actor.Side) continue;
      int d = Pathing.ChebyshevDistance(actor, u);
      if (d < bestD) { bestD = d; best = u; }
    }
    return best;
  }

  private bool CheckEnded()
  {
    int p = _model.Board.AliveCount(Side.Player);
    int e = _model.Board.AliveCount(Side.Enemy);
    if (p > 0 && e > 0) return false;
    Side? winner = (p, e) switch { (> 0, 0) => Side.Player, (0, > 0) => Side.Enemy, _ => null };
    _events.Publish(new CombatEnded { Winner = winner ?? Side.Player });
    _onCombatEnded(winner);
    return true;
  }

  private void OnDamaged(UnitDamaged e)
  {
    AppendLog($"{e.Attacker.Side} {e.Attacker.Stats.Name} hits {e.Victim.Side} {e.Victim.Stats.Name} for {e.Amount}.");
  }

  private void OnKilled(UnitKilled e)
  {
    AppendLog($"{e.Victim.Side} {e.Victim.Stats.Name} dies.");
  }

  private void AppendLog(string msg) => _log.Add(msg);

  public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    PixelDraw.Tile(spriteBatch, _art.Felt,
      new Rectangle(0, 0, _viewportWidth, _viewportHeight), AutoBattlerArt.FeltScale,
      tint: new Color(150, 150, 150));
    DrawBoard(spriteBatch);
    DrawUnits(spriteBatch);
    DrawHud(spriteBatch);
    DrawLog(spriteBatch);
    spriteBatch.End();
  }

  private void DrawBoard(SpriteBatch spriteBatch)
  {
    for (int r = 0; r < Board.Rows; r++)
      for (int c = 0; c < Board.Columns; c++)
      {
        Rectangle cell = CellRect(c, r);
        PixelDraw.Tile(spriteBatch, _art.Felt, cell, AutoBattlerArt.FeltScale);
      }

    // The divider stays a primitive: it is a rule between two halves of the
    // board, not an object on it, and one 2px line does not want a texture.
    int midX = (int)_boardOrigin.X + Board.PlayerSideEndColExclusive * BoardCellSize;
    Primitives.DrawRectangle(spriteBatch,
      new Rectangle(midX - 1, (int)_boardOrigin.Y, 2, Board.Rows * BoardCellSize),
      new Color(198, 152, 104));
  }

  private void DrawUnits(SpriteBatch spriteBatch)
  {
    foreach (Unit u in _model.Board.Units)
    {
      if (!u.Alive) continue;
      Rectangle cell = CellRect(u.Col, u.Row);
      UnitStats.Stats s = u.Stats;
      Color tint = u.Side == Side.Player ? s.PlayerTint : s.EnemyTint;
      Rectangle inner = new(cell.X + 6, cell.Y + 6, cell.Width - 12, cell.Height - 12);
      Primitives.DrawRectangle(spriteBatch, inner, tint);
      HpBar.Draw(spriteBatch, new Rectangle(inner.X, inner.Bottom - 8, inner.Width, 5), u.Hp, s.MaxHp, new Color(120, 220, 140));
      string letter = s.Name[..1];
      Vector2 sz = _font.MeasureString(letter);
      spriteBatch.DrawString(_font, letter, new Vector2(inner.Center.X - sz.X / 2f, inner.Y + 6), Color.White);
    }
  }

  private void DrawHud(SpriteBatch spriteBatch)
  {
    spriteBatch.DrawString(_font, $"Round {_model.Round} — COMBAT", new Vector2(20, 20), Color.White);
    spriteBatch.DrawString(_font, $"Hero HP {_model.PlayerHeroHp}", new Vector2(20, 46), new Color(120, 220, 160));
    spriteBatch.DrawString(_font, $"Enemy HP {_model.EnemyHeroHp}", new Vector2(200, 46), new Color(230, 120, 120));
    int playerCount = _model.Board.AliveCount(Side.Player);
    int enemyCount = _model.Board.AliveCount(Side.Enemy);
    spriteBatch.DrawString(_font, $"Units {playerCount} vs {enemyCount}", new Vector2(_viewportWidth - 200, 20), new Color(200, 210, 230));
  }

  private void DrawLog(SpriteBatch spriteBatch)
    => _log.Draw(spriteBatch, _font, new Vector2(20, _viewportHeight - 180));

  private Rectangle CellRect(int col, int row)
    => new((int)_boardOrigin.X + col * BoardCellSize,
           (int)_boardOrigin.Y + row * BoardCellSize,
           BoardCellSize - 2, BoardCellSize - 2);
}
