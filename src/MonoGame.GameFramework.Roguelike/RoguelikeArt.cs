using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Roguelike;

/// <summary>
/// The dungeon's textures.
///
/// Both sheets are indexed rather than named: tiles by <see cref="TileKind"/>,
/// actors by a small enum below. A 60x34 map redraws every tile every frame, so
/// the lookup is on the hot path and a dictionary of strings would be the wrong
/// shape as well as the wrong cost.
/// </summary>
public sealed record RoguelikeArt(
  Texture2D Tiles,
  Texture2D Actors,
  Texture2D Torch,
  NineSlice Frame)
{
  /// <summary>
  /// Tiles are authored at DungeonGenerator.CellSize and drawn 1:1. A dungeon
  /// this wide has no room to magnify.
  /// </summary>
  public const int Scale = 1;
  public const int TileSize = DungeonGenerator.CellSize;
  public const int FrameBorder = 4;

  /// <summary>
  /// Frame 3 of the tile sheet: floor outside the lit radius. It has no
  /// TileKind because it is not a different tile, it is the same floor seen
  /// differently — which is exactly why it lives here and not in the enum.
  /// </summary>
  public const int UnlitFrame = 3;

  public enum ActorFrame { Hero = 0, Rat = 1, Goblin = 2 }

  public static Rectangle TileFrame(TileKind kind)
    => new((int)kind * TileSize, 0, TileSize, TileSize);

  public static Rectangle TileFrame(int index)
    => new(index * TileSize, 0, TileSize, TileSize);

  public static Rectangle ActorRect(ActorFrame frame)
    => new((int)frame * TileSize, 0, TileSize, TileSize);

  /// <summary>Monsters name themselves; this is the one place that mapping lives.</summary>
  public static ActorFrame FrameForMonster(string name) => name switch
  {
    "rat" => ActorFrame.Rat,
    "goblin" => ActorFrame.Goblin,
    _ => ActorFrame.Goblin,
  };

  public static RoguelikeArt Load(ContentManager content) => new(
    content.Load<Texture2D>("sprites/tiles"),
    content.Load<Texture2D>("sprites/actors"),
    content.Load<Texture2D>("sprites/torch"),
    new NineSlice(content.Load<Texture2D>("sprites/ui-frame"), FrameBorder));
}
