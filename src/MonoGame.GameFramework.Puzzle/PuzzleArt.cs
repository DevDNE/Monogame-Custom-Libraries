using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Puzzle;

/// <summary>
/// Every texture the board draws.
///
/// The gem sheet is indexed by <see cref="Board.Gem"/> directly: frame 0 is a
/// deliberately blank 32x32, because Gem.Empty is 0 and a sheet that needed an
/// offset is a sheet somebody eventually reads one frame out.
/// </summary>
public sealed record PuzzleArt(
  Texture2D Background,
  Texture2D Cell,
  Texture2D Gems,
  Texture2D Burst,
  NineSlice Frame)
{
  /// <summary>Everything is authored at 32x32 and drawn at 2x into the 64px cells.</summary>
  public const int Scale = 2;
  public const int GemSize = 32;
  public const int FrameBorder = 4;

  public Rectangle FrameFor(Board.Gem gem) => new((int)gem * GemSize, 0, GemSize, GemSize);

  public static PuzzleArt Load(ContentManager content) => new(
    content.Load<Texture2D>("sprites/bg-tile"),
    content.Load<Texture2D>("sprites/cell"),
    content.Load<Texture2D>("sprites/gems"),
    content.Load<Texture2D>("sprites/burst"),
    new NineSlice(content.Load<Texture2D>("sprites/ui-frame"), FrameBorder));
}
