using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Puzzle.GameStates;

public class PlayState : GameState
{
  private readonly KeyboardManager _keyboard;
  private readonly MouseManager _mouse;
  private readonly SpriteFont _font;
  private readonly int _viewportWidth;
  private readonly int _viewportHeight;

  private Board _board;
  private (int c, int r)? _selected;
  private string _lastEvent = "";

  private readonly PuzzleArt _art;

  public PlayState(ServiceProvider sp, SpriteFont font, PuzzleArt art, int vw, int vh)
  {
    _art = art;
    _keyboard = sp.GetService<KeyboardManager>();
    _mouse = sp.GetService<MouseManager>();
    _font = font;
    _viewportWidth = vw;
    _viewportHeight = vh;
  }

  public override void Entered()
  {
    Vector2 origin = new(
      (_viewportWidth - Board.Columns * Board.CellSize) * 0.5f,
      (_viewportHeight - Board.Rows * Board.CellSize) * 0.5f + 16);
    _board = new Board(origin);
    _selected = null;
    _lastEvent = "";
    IsActive = true;
  }

  public override void Leaving() { }
  public override void Obscuring() => IsActive = false;
  public override void Revealed() => IsActive = true;

  public override void Update(GameTime gameTime)
  {
    if (_keyboard.WasKeyPressed(Keys.R)) { _board.FillRandomNoMatches(); _selected = null; _lastEvent = "New board"; return; }

    if (_mouse.WasLeftMouseButtonPressed())
    {
      Vector2 mouse = _mouse.GetMousePosition();
      if (!_board.Map.TryWorldToCell(mouse, out int col, out int row)) { _selected = null; return; }

      if (_selected is null)
      {
        _selected = (col, row);
        _lastEvent = $"Selected ({col},{row})";
      }
      else
      {
        (int c, int r) first = _selected.Value;
        if (first == (col, row))
        {
          _selected = null;
          _lastEvent = "Deselected";
        }
        else if (_board.AreAdjacent(first, (col, row)))
        {
          bool matched = _board.TrySwap(first, (col, row));
          _lastEvent = matched ? $"Match! Score {_board.Score}" : "No match - reverted";
          _selected = null;
        }
        else
        {
          // Not adjacent: treat as new selection.
          _selected = (col, row);
          _lastEvent = $"Selected ({col},{row})";
        }
      }
    }
  }

  public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    PixelDraw.Tile(spriteBatch, _art.Background,
      new Rectangle(0, 0, _viewportWidth, _viewportHeight), PuzzleArt.Scale);
    DrawBoard(spriteBatch);
    DrawHud(spriteBatch);
    spriteBatch.End();
  }

  private void DrawBoard(SpriteBatch spriteBatch)
  {
    // A frame around the whole board, so the play area is an object rather
    // than a region of background that happens to have gems on it.
    Rectangle bg = _board.Map.GetCellRect(0, 0);
    Rectangle last = _board.Map.GetCellRect(Board.Columns - 1, Board.Rows - 1);
    Rectangle fullBoard = new(bg.X - 16, bg.Y - 16, last.Right - bg.X + 32, last.Bottom - bg.Y + 32);
    _art.Frame.Draw(spriteBatch, fullBoard, PuzzleArt.Scale);

    for (int r = 0; r < Board.Rows; r++)
    {
      for (int c = 0; c < Board.Columns; c++)
      {
        Rectangle cell = _board.Map.GetCellRect(c, r);
        PixelDraw.Sprite(spriteBatch, _art.Cell, cell.X, cell.Y, PuzzleArt.Scale);

        Board.Gem g = _board.Gems[c, r];
        if (g == Board.Gem.Empty) continue;
        // No inset: the gem art already sits inside its own 32x32 with margin,
        // which is what lets each gem have a different silhouette without the
        // board having to know how big any of them are.
        PixelDraw.Frame(spriteBatch, _art.Gems, _art.FrameFor(g), cell.X, cell.Y, PuzzleArt.Scale);
      }
    }

    if (_selected is { } sel)
    {
      // The selection ring is still drawn from primitives, deliberately: it is
      // UI over the board rather than part of it, and a 2px rectangle is
      // exactly as legible as a sprite would be at a fraction of the cost.
      Rectangle cell = _board.Map.GetCellRect(sel.c, sel.r);
      Color ring = new(255, 255, 255);
      Primitives.DrawRectangle(spriteBatch, new Rectangle(cell.X, cell.Y, cell.Width, 2), ring);
      Primitives.DrawRectangle(spriteBatch, new Rectangle(cell.X, cell.Bottom - 2, cell.Width, 2), ring);
      Primitives.DrawRectangle(spriteBatch, new Rectangle(cell.X, cell.Y, 2, cell.Height), ring);
      Primitives.DrawRectangle(spriteBatch, new Rectangle(cell.Right - 2, cell.Y, 2, cell.Height), ring);
    }
  }

  private void DrawHud(SpriteBatch spriteBatch)
  {
    spriteBatch.DrawString(_font, $"Score {_board.Score}", new Vector2(20, 20), Color.White);
    if (!string.IsNullOrEmpty(_lastEvent))
    {
      Vector2 sz = _font.MeasureString(_lastEvent);
      spriteBatch.DrawString(_font, _lastEvent, new Vector2(_viewportWidth - sz.X - 20, 20), new Color(200, 210, 230));
    }
    const string hint = "Click two adjacent gems to swap   R reshuffle   Esc quit";
    Vector2 hs = _font.MeasureString(hint);
    spriteBatch.DrawString(_font, hint, new Vector2(_viewportWidth * 0.5f - hs.X * 0.5f, _viewportHeight - 30), new Color(180, 180, 200));
  }
}
