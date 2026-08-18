using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Platformer.Entities;

/// <summary>
/// The world's textures — everything the hero is not.
///
/// Separate from <see cref="HeroSprites"/> deliberately: that record exists to
/// answer "which frame for this state", which is a question about animation.
/// This one is a bag of scenery, and merging them would put a state->frame
/// lookup next to a parallax strip for no reason beyond both being PNGs.
/// </summary>
public sealed record PlatformerArt(
  Texture2D Sky,
  Texture2D Clouds,
  Texture2D Ruins,
  Texture2D Ground,
  Texture2D GroundTop,
  Texture2D Enemy,
  Texture2D Goal,
  NineSlice Frame)
{
  /// <summary>
  /// This game draws 1:1 — the hero is authored at the size he appears, and so
  /// is everything around him. Scaling would have meant re-authoring four
  /// committed hero frames to match.
  /// </summary>
  public const int Scale = 1;

  public const int TileSize = 16;
  public const int FrameBorder = 4;

  /// <summary>
  /// How much slower than the camera each background layer moves. Sky does not
  /// move at all; the further a layer is meant to be, the closer to zero.
  /// </summary>
  public const float CloudParallax = 0.15f;
  public const float RuinsParallax = 0.4f;

  public static PlatformerArt Load(ContentManager content) => new(
    content.Load<Texture2D>("sprites/sky"),
    content.Load<Texture2D>("sprites/clouds"),
    content.Load<Texture2D>("sprites/ruins"),
    content.Load<Texture2D>("sprites/ground"),
    content.Load<Texture2D>("sprites/ground-top"),
    content.Load<Texture2D>("sprites/enemy"),
    content.Load<Texture2D>("sprites/goal"),
    new NineSlice(content.Load<Texture2D>("sprites/ui-frame"), FrameBorder));
}
