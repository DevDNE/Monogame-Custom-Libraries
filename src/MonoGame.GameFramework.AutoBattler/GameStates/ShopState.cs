using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Debugging;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.UI;

namespace MonoGame.GameFramework.AutoBattler.GameStates;

/// <summary>
/// Player picks one random unit from an offered set of three and drags it
/// onto an empty player-side cell. Press Space to start combat when ready.
/// The enemy side is auto-populated at the start of each shop so the
/// player always has someone to fight.
///
/// Drag-and-drop is hand-rolled here with mouse press / release / held
/// state. UIManager.OnClick doesn't fit this pattern. Flagged as a
/// finding for the §8 revision.
/// </summary>
public class ShopState : GameState
{
  private const int CardWidth = 180;
  private const int CardHeight = 220;
  private const int CardGap = 16;
  private const int BoardCellSize = 80;

  private readonly KeyboardManager _keyboard;
  private readonly MouseManager _mouse;
  private readonly SpriteFont _font;
  private readonly GameModel _model;
  private readonly int _viewportWidth;
  private readonly int _viewportHeight;
  private readonly Action _onStartCombat;

  // 3 cards offered this turn.
  private readonly List<UnitType> _offers = new();
  private readonly Random _rng = new();

  // Drag state
  private int _draggingCardIndex = -1;
  private Vector2 _dragPosition;

  // Board origin cached once per Entered.
  private Vector2 _boardOrigin;

  private readonly AutoBattlerArt _art;

  public ShopState(ServiceProvider sp, SpriteFont font, GameModel model, AutoBattlerArt art, int vw, int vh, Action onStartCombat)
  {
    _art = art;
    _keyboard = sp.GetService<KeyboardManager>();
    _mouse = sp.GetService<MouseManager>();
    _font = font;
    _model = model;
    _viewportWidth = vw;
    _viewportHeight = vh;
    _onStartCombat = onStartCombat;

    DebugOverlay overlay = sp.GetService<DebugOverlay>();
    overlay.AddWatch("shop cell", () =>
    {
      if (GridMath.TryMouseToCell(
            _mouse.GetMousePosition(), _boardOrigin,
            BoardCellSize, Board.Columns, Board.Rows,
            out int c, out int r)) return $"({c},{r}) {(_model?.Board?.IsPlayerCell(c, r) == true ? "player" : "off-side")}";
      return "(off-board)";
    });
    overlay.AddWatch("dragging", () => _draggingCardIndex < 0 ? "-" : $"card {_draggingCardIndex} @ {_dragPosition:F0}");
  }

  public override void Entered()
  {
    _boardOrigin = new Vector2(
      (_viewportWidth - Board.Columns * BoardCellSize) * 0.5f,
      80f);
    RollOffers();
    PopulateEnemies();
    IsActive = true;
  }

  public override void Leaving() { }
  public override void Obscuring() => IsActive = false;
  public override void Revealed() => IsActive = true;

  private void RollOffers()
  {
    _offers.Clear();
    UnitType[] pool = { UnitType.Warrior, UnitType.Archer, UnitType.Tank };
    for (int i = 0; i < 3; i++) _offers.Add(pool[_rng.Next(pool.Length)]);
    _draggingCardIndex = -1;
  }

  /// <summary>
  /// Generate enemy units for this round. Count + composition scale with
  /// the round number, capped at the enemy half's capacity.
  /// </summary>
  private void PopulateEnemies()
  {
    // Remove any enemies left over from a previous shop re-entry (shouldn't
    // happen in MVP but cheap to be defensive).
    _model.Board.Units.RemoveAll(u => u.Side == Side.Enemy);

    int count = System.Math.Min(1 + _model.Round, 8);
    UnitType[] pool = { UnitType.Warrior, UnitType.Archer, UnitType.Tank };
    int placed = 0;
    for (int attempt = 0; attempt < 50 && placed < count; attempt++)
    {
      int col = _rng.Next(Board.PlayerSideEndColExclusive, Board.Columns);
      int row = _rng.Next(Board.Rows);
      if (_model.Board.UnitAt(col, row) != null) continue;
      _model.Board.Units.Add(new Unit(pool[_rng.Next(pool.Length)], Side.Enemy, col, row));
      placed++;
    }
  }

  public override void Update(GameTime gameTime)
  {
    if (_keyboard.WasKeyPressed(Keys.Space))
    {
      _onStartCombat();
      return;
    }

    Vector2 mouse = _mouse.GetMousePosition();

    // Begin drag
    if (_mouse.WasLeftMouseButtonPressed() && _draggingCardIndex < 0)
    {
      for (int i = 0; i < _offers.Count; i++)
      {
        Rectangle card = CardRect(i);
        if (card.Contains(mouse) && _model.Gold >= UnitStats.Of(_offers[i]).Cost)
        {
          _draggingCardIndex = i;
          _dragPosition = mouse;
          break;
        }
      }
    }
    // Update drag
    else if (_draggingCardIndex >= 0)
    {
      _dragPosition = mouse;
      if (_mouse.WasLeftMouseButtonReleased())
      {
        TryDropCard();
        _draggingCardIndex = -1;
      }
    }
  }

  private void TryDropCard()
  {
    if (!GridMath.TryMouseToCell(_dragPosition, _boardOrigin, BoardCellSize, Board.Columns, Board.Rows, out int col, out int row)) return;
    if (!_model.Board.IsPlayerCell(col, row)) return;
    if (_model.Board.UnitAt(col, row) != null) return;

    UnitType type = _offers[_draggingCardIndex];
    UnitStats.Stats stats = UnitStats.Of(type);
    if (_model.Gold < stats.Cost) return;

    _model.Board.Units.Add(new Unit(type, Side.Player, col, row));
    _model.Gold -= stats.Cost;
    _offers.RemoveAt(_draggingCardIndex);
    if (_offers.Count == 0) RollOffers(); // refill
  }

  public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    PixelDraw.Tile(spriteBatch, _art.Felt,
      new Rectangle(0, 0, _viewportWidth, _viewportHeight), AutoBattlerArt.FeltScale,
      tint: new Color(150, 150, 150));
    DrawBoard(spriteBatch);
    DrawUnits(spriteBatch);
    DrawCards(spriteBatch);
    DrawHud(spriteBatch);
    if (_draggingCardIndex >= 0) DrawDragGhost(spriteBatch);
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
      DrawUnit(spriteBatch, u, CellRect(u.Col, u.Row));
    }
  }

  private void DrawUnit(SpriteBatch spriteBatch, Unit u, Rectangle cell)
  {
    UnitStats.Stats s = u.Stats;
    int size = AutoBattlerArt.UnitSize * AutoBattlerArt.UnitScale;
    int x = cell.Center.X - size / 2;
    int y = cell.Bottom - size - 2;
    PixelDraw.Frame(spriteBatch, _art.Units, AutoBattlerArt.RectFor(u.Type, u.Side),
      x, y, AutoBattlerArt.UnitScale);
    HpBar.Draw(spriteBatch, new Rectangle(cell.X + 8, cell.Y + 4, cell.Width - 16, 5),
      u.Hp, s.MaxHp, new Color(98, 148, 114));
  }

  private void DrawCards(SpriteBatch spriteBatch)
  {
    for (int i = 0; i < _offers.Count; i++)
    {
      DrawCard(spriteBatch, i, CardRect(i), _offers[i], i == _draggingCardIndex);
    }
  }

  private void DrawCard(SpriteBatch spriteBatch, int i, Rectangle rect, UnitType t, bool beingDragged)
  {
    UnitStats.Stats s = UnitStats.Of(t);
    bool affordable = _model.Gold >= s.Cost;
    // Unaffordable cards are dimmed rather than recoloured, so the frame stays
    // the same object and only its reachability changes.
    Color tint = beingDragged ? new Color(120, 120, 120) : (affordable ? Color.White : new Color(96, 96, 104));
    _art.Frame.Draw(spriteBatch, rect, AutoBattlerArt.FeltScale, tint);

    // The card shows the actual piece, at the size it will be on the board.
    int size = AutoBattlerArt.UnitSize * AutoBattlerArt.UnitScale;
    PixelDraw.Frame(spriteBatch, _art.Units, AutoBattlerArt.RectFor(t, Side.Player),
      rect.Center.X - size / 2, rect.Y + 16, AutoBattlerArt.UnitScale, tint);
    Vector2 nameSz = _font.MeasureString(s.Name);
    spriteBatch.DrawString(_font, s.Name, new Vector2(rect.Center.X - nameSz.X / 2f, rect.Y + 90), Color.White);
    string stats = $"HP {s.MaxHp}  ATK {s.Attack}  RNG {s.Range}";
    Vector2 statSz = _font.MeasureString(stats);
    spriteBatch.DrawString(_font, stats, new Vector2(rect.Center.X - statSz.X / 2f, rect.Y + 120), new Color(200, 210, 230));
    string cost = $"Cost {s.Cost}";
    Vector2 costSz = _font.MeasureString(cost);
    spriteBatch.DrawString(_font, cost, new Vector2(rect.Center.X - costSz.X / 2f, rect.Bottom - 40), affordable ? new Color(255, 220, 120) : new Color(180, 100, 100));
  }

  private void DrawDragGhost(SpriteBatch spriteBatch)
  {
    if (_draggingCardIndex < 0 || _draggingCardIndex >= _offers.Count) return;
    UnitStats.Stats s = UnitStats.Of(_offers[_draggingCardIndex]);
    Rectangle ghost = new(
      (int)_dragPosition.X - BoardCellSize / 2,
      (int)_dragPosition.Y - BoardCellSize / 2,
      BoardCellSize, BoardCellSize);
    Primitives.DrawRectangle(spriteBatch, ghost, new Color(s.PlayerTint.R, s.PlayerTint.G, s.PlayerTint.B, (byte)180));
  }

  private void DrawHud(SpriteBatch spriteBatch)
  {
    spriteBatch.DrawString(_font, $"Round {_model.Round}", new Vector2(20, 20), Color.White);
    spriteBatch.DrawString(_font, $"Hero HP {_model.PlayerHeroHp}", new Vector2(20, 46), new Color(120, 220, 160));
    spriteBatch.DrawString(_font, $"Enemy HP {_model.EnemyHeroHp}", new Vector2(200, 46), new Color(230, 120, 120));
    spriteBatch.DrawString(_font, $"Gold {_model.Gold}", new Vector2(_viewportWidth - 140, 20), new Color(255, 220, 120));

    const string hint = "Drag a card to your (blue) side. Space: start combat. Esc: quit.";
    Vector2 hs = _font.MeasureString(hint);
    spriteBatch.DrawString(_font, hint, new Vector2(_viewportWidth * 0.5f - hs.X * 0.5f, _viewportHeight - 28), new Color(200, 200, 215));
  }

  private Rectangle CellRect(int col, int row)
    => new((int)_boardOrigin.X + col * BoardCellSize,
           (int)_boardOrigin.Y + row * BoardCellSize,
           BoardCellSize - 2, BoardCellSize - 2);

  private Rectangle CardRect(int index)
  {
    int total = _offers.Count * CardWidth + (_offers.Count - 1) * CardGap;
    int startX = (_viewportWidth - total) / 2;
    int y = _viewportHeight - CardHeight - 60;
    return new Rectangle(startX + index * (CardWidth + CardGap), y, CardWidth, CardHeight);
  }
}
