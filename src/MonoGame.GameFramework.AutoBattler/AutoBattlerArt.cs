using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.AutoBattler;

/// <summary>
/// The table and the pieces on it.
///
/// One unit sheet holds both armies: three classes in blue, then the same three
/// in red. Indexing it takes a <see cref="UnitType"/> and a <see cref="Side"/>,
/// which is the whole reason the two rows are laid out in the same order —
/// side picks the row, type picks the column, and no lookup table is needed.
/// </summary>
public sealed record AutoBattlerArt(
  Texture2D Felt,
  Texture2D Units,
  Texture2D Coin,
  NineSlice Frame)
{
  /// <summary>The felt square is 40x40, drawn at 2x into the 80px board cell.</summary>
  public const int FeltScale = 2;

  /// <summary>
  /// Units are 24x24 authored and drawn at 3x — 72px inside an 80px cell. At 2x
  /// they read as counters sitting in a large empty square; at 3x they read as
  /// pieces that occupy it, which is what a board game looks like.
  /// </summary>
  public const int UnitScale = 3;
  public const int UnitSize = 24;
  public const int FrameBorder = 4;

  public static Rectangle RectFor(UnitType type, Side side)
  {
    int column = type switch
    {
      UnitType.Warrior => 0,
      UnitType.Archer => 1,
      _ => 2,
    };
    if (side == Side.Enemy) column += 3;
    return new Rectangle(column * UnitSize, 0, UnitSize, UnitSize);
  }

  public static AutoBattlerArt Load(ContentManager content) => new(
    content.Load<Texture2D>("sprites/felt"),
    content.Load<Texture2D>("sprites/units"),
    content.Load<Texture2D>("sprites/coin"),
    new NineSlice(content.Load<Texture2D>("sprites/ui-frame"), FrameBorder));
}
