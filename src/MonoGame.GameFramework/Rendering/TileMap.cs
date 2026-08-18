using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonoGame.GameFramework.Rendering;

public class TileMap
{
  public int Columns { get; }
  public int Rows { get; }
  public int TileWidth { get; }
  public int TileHeight { get; }
  public Vector2 Origin { get; set; } = Vector2.Zero;

  private readonly Dictionary<string, object> _layers = new();

  public TileMap(int columns, int rows, int tileWidth, int tileHeight)
  {
    Columns = columns;
    Rows = rows;
    TileWidth = tileWidth;
    TileHeight = tileHeight;
  }

  public TileLayer<T> AddLayer<T>(string name)
  {
    TileLayer<T> layer = new(name, Columns, Rows);
    _layers[name] = layer;
    return layer;
  }

  public TileLayer<T> GetLayer<T>(string name)
    => _layers.TryGetValue(name, out object layer) ? (TileLayer<T>)layer : null;

  public bool RemoveLayer(string name) => _layers.Remove(name);

  public Vector2 GetWorldPosition(int column, int row)
    => Origin + new Vector2(column * TileWidth, row * TileHeight);

  public Rectangle GetCellRect(int column, int row)
  {
    Vector2 pos = GetWorldPosition(column, row);
    return new Rectangle((int)pos.X, (int)pos.Y, TileWidth, TileHeight);
  }

  /// <summary>
  /// Unbounded point-to-cell. Floors rather than truncating, so a point left
  /// of or above the origin lands in a negative cell instead of folding onto
  /// cell 0 — an int cast rounds toward zero, which made (-1, -1) world units
  /// report as cell (0, 0), silently inside the map.
  /// Prefer <see cref="TryWorldToCell"/> when the point can miss the map.
  /// </summary>
  public (int column, int row) WorldToCell(Vector2 world)
  {
    Vector2 local = world - Origin;
    return (FloorDiv(local.X, TileWidth), FloorDiv(local.Y, TileHeight));
  }

  /// <summary>
  /// Bounds-checked point-to-cell. Both out-params are -1 on any failure.
  /// </summary>
  public bool TryWorldToCell(Vector2 world, out int column, out int row)
  {
    column = -1;
    row = -1;

    if (TileWidth <= 0 || TileHeight <= 0) return false;

    Vector2 local = world - Origin;
    if (local.X < 0 || local.Y < 0) return false;

    int c = (int)(local.X / TileWidth);
    int r = (int)(local.Y / TileHeight);
    if (c >= Columns || r >= Rows) return false;

    column = c;
    row = r;
    return true;
  }

  static int FloorDiv(float value, int size)
    => size <= 0 ? 0 : (int)MathF.Floor(value / size);
}
