using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.TowerDefense.Entities;

/// <summary>
/// The board's textures.
///
/// Turf and path are the load-bearing pair: the entire "can I build here"
/// question is answered by which of those two a cell is drawn with, which is
/// why the grid needs no gridlines and the placement rule needs no legend.
/// </summary>
public sealed record TowerDefenseArt(
  Texture2D Turf,
  Texture2D Path,
  Texture2D Tower,
  Texture2D Enemy,
  Texture2D Shot,
  Texture2D Coin,
  NineSlice Frame)
{
  /// <summary>Cells are authored at 22 and drawn at 2x into MapPath.CellSize (44).</summary>
  public const int CellScale = 2;

  /// <summary>Tower is 16x16 authored, enemy 14x14 — both doubled to match their hitboxes.</summary>
  public const int EntityScale = 2;

  /// <summary>The shot is 8x8 authored and drawn 1:1, exactly its own hitbox.</summary>
  public const int ShotScale = 1;

  public const int FrameBorder = 4;

  public static TowerDefenseArt Load(ContentManager content) => new(
    content.Load<Texture2D>("sprites/turf"),
    content.Load<Texture2D>("sprites/path"),
    content.Load<Texture2D>("sprites/tower"),
    content.Load<Texture2D>("sprites/enemy"),
    content.Load<Texture2D>("sprites/shot"),
    content.Load<Texture2D>("sprites/coin"),
    new NineSlice(content.Load<Texture2D>("sprites/ui-frame"), FrameBorder));
}
