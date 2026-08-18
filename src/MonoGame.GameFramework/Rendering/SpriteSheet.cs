using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.GameFramework.Rendering;
public class SpriteSheet
{
  public string Name { get; init; }
  public Texture2D Texture { get; init; }
  /// <summary>
  /// Top-left of <see cref="DestinationFrame"/>. Derived, not stored: this used
  /// to be a settable field initialised once from the destination rect and read
  /// by nothing, so assigning it looked like it moved the sprite and did not.
  /// DrawManager draws from DestinationFrame and UIManager hit-tests against it;
  /// that rect is the single source of truth.
  /// </summary>
  public Vector2 Position => new(DestinationFrame.X, DestinationFrame.Y);
  public int Width { get; init; }
  public int Height { get; init; }
  public Rectangle SourceFrame { get; init; }
  public Rectangle DestinationFrame { get; set; }
  public Color Tint { get; set; } = Color.White;

  public static SpriteSheet Static(Texture2D texture, Rectangle destinationFrame, Rectangle? sourceFrame = null, string name = null)
  {
    Rectangle src = sourceFrame ?? new Rectangle(0, 0, texture.Width, texture.Height);
    return new SpriteSheet
    {
      Name = name,
      Texture = texture,
      Width = src.Width,
      Height = src.Height,
      SourceFrame = src,
      DestinationFrame = destinationFrame,
    };
  }
}
