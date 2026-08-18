using Microsoft.Xna.Framework;

namespace MonoGame.GameFramework.Rendering;

public static class GridMath
{
  /// <summary>
  /// Bounds-checked conversion from a screen/world point to a grid cell.
  /// On any failure both out-params are set to -1 — every failing branch, not
  /// just the ones left of or above the grid, so a caller that reads the
  /// out-params without checking the bool gets the same answer whichever edge
  /// it missed.
  /// </summary>
  public static bool TryMouseToCell(Vector2 mouse, Vector2 origin, int cellSize, int columns, int rows, out int column, out int row)
  {
    column = -1;
    row = -1;

    if (cellSize <= 0) return false;

    float localX = mouse.X - origin.X;
    float localY = mouse.Y - origin.Y;
    if (localX < 0 || localY < 0) return false;

    int c = (int)(localX / cellSize);
    int r = (int)(localY / cellSize);
    if (c >= columns || r >= rows) return false;

    column = c;
    row = r;
    return true;
  }
}
