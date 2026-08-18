using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Platformer.Entities;

public class Platform
{
  public Rectangle Bounds { get; }

  public Platform(Rectangle bounds)
  {
    Bounds = bounds;
  }

  /// <summary>
  /// Masonry tiled across the whole box, then the mossy top course laid over
  /// the first 16 rows.
  ///
  /// The offset passed to the body is the platform's own origin, so the courses
  /// line up with the platform rather than with the world grid — otherwise a
  /// platform starting at x=130 would slice its first brick in half, and every
  /// platform would slice it differently.
  /// </summary>
  public void Draw(SpriteBatch spriteBatch, PlatformerArt art)
  {
    PixelDraw.Tile(spriteBatch, art.Ground, Bounds, PlatformerArt.Scale);

    Rectangle cap = new(Bounds.X, Bounds.Y,
      Bounds.Width, System.Math.Min(PlatformerArt.TileSize, Bounds.Height));
    PixelDraw.Tile(spriteBatch, art.GroundTop, cap, PlatformerArt.Scale);
  }
}
