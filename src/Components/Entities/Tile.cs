using Graphics;

namespace Entities;
public class Tile
{
  public int Row { get; }
  public int Column { get; }
  public bool IsOccupied { get; set; }
  public SpriteSheet TileSprite { get; set; }

  public Tile(int row, int column, SpriteSheet spriteSheet)
  {
    Row = row;
    Column = column;
    TileSprite = spriteSheet;
  }
}