using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.GameFramework.Rendering;

/// <summary>
/// A panel or button frame that survives being resized.
///
/// A pixel-art button cannot be one sprite: the label decides the width, the
/// artist decides the corner radius, and scaling one bitmap to fit destroys the
/// second to satisfy the first. So the source is cut into nine — four corners
/// that never scale, four edges that repeat along one axis, and a middle that
/// repeats along both.
///
/// This is what makes the menus in nine different games affordable. Each game
/// draws one small frame sprite, and every button, panel and dialogue box in
/// that game comes out of it at whatever size the layout wants.
///
/// The border inset is uniform because every frame in this repo is symmetric,
/// and a per-edge inset is four numbers to get wrong for a case nothing here
/// has. Add it when a sprite needs it.
/// </summary>
public sealed class NineSlice
{
  public Texture2D Texture { get; }
  public Rectangle Source { get; }

  /// <summary>Corner size in source pixels — the part that never stretches.</summary>
  public int Border { get; }

  public NineSlice(Texture2D texture, int border, Rectangle? source = null)
  {
    ArgumentNullException.ThrowIfNull(texture);
    Texture = texture;
    Source = source ?? new Rectangle(0, 0, texture.Width, texture.Height);
    Border = border;

    if (border < 0) throw new ArgumentOutOfRangeException(nameof(border), border, "Border cannot be negative.");
    if (border * 2 >= Source.Width || border * 2 >= Source.Height)
      throw new ArgumentOutOfRangeException(nameof(border), border,
        $"Border {border} leaves no middle in a {Source.Width}x{Source.Height} source.");
  }

  /// <summary>Smallest destination that shows the frame without the corners overlapping.</summary>
  public int MinimumSize(int scale) => Border * 2 * scale;

  /// <summary>
  /// The area inside the frame, where a caller should put its content. Callers
  /// that centre a label in the full rect instead will look right until a game
  /// gives its frame a thick bottom lip.
  /// </summary>
  public Rectangle ContentBounds(Rectangle destination, int scale)
    => new(destination.X + Border * scale, destination.Y + Border * scale,
      Math.Max(0, destination.Width - Border * 2 * scale),
      Math.Max(0, destination.Height - Border * 2 * scale));

  public void Draw(SpriteBatch spriteBatch, Rectangle destination, int scale, Color? tint = null)
  {
    if (scale < 1) throw new ArgumentOutOfRangeException(nameof(scale), scale, "Pixel art scales by whole numbers only.");
    if (destination.Width <= 0 || destination.Height <= 0) return;

    Color color = tint ?? Color.White;
    int b = Border;
    int bs = b * scale;

    // Middle spans in source pixels — what is left after the two corners.
    int midSrcW = Source.Width - b * 2;
    int midSrcH = Source.Height - b * 2;

    // Destination spans. A rect narrower than both corners would draw them
    // overlapping; clamping at zero drops the middle and lets the corners meet,
    // which degrades more gracefully than a negative-width rectangle.
    int midDstW = Math.Max(0, destination.Width - bs * 2);
    int midDstH = Math.Max(0, destination.Height - bs * 2);

    int left = destination.X, right = destination.Right - bs;
    int top = destination.Y, bottom = destination.Bottom - bs;

    if (b > 0)
    {
      Blit(spriteBatch, new Rectangle(Source.X, Source.Y, b, b), new Rectangle(left, top, bs, bs), color);
      Blit(spriteBatch, new Rectangle(Source.Right - b, Source.Y, b, b), new Rectangle(right, top, bs, bs), color);
      Blit(spriteBatch, new Rectangle(Source.X, Source.Bottom - b, b, b), new Rectangle(left, bottom, bs, bs), color);
      Blit(spriteBatch, new Rectangle(Source.Right - b, Source.Bottom - b, b, b), new Rectangle(right, bottom, bs, bs), color);
    }

    if (midDstW > 0 && b > 0)
    {
      Rectangle topEdge = new(Source.X + b, Source.Y, midSrcW, b);
      Rectangle bottomEdge = new(Source.X + b, Source.Bottom - b, midSrcW, b);
      PixelDraw.Tile(spriteBatch, Texture, topEdge, new Rectangle(left + bs, top, midDstW, bs), scale, color);
      PixelDraw.Tile(spriteBatch, Texture, bottomEdge, new Rectangle(left + bs, bottom, midDstW, bs), scale, color);
    }

    if (midDstH > 0 && b > 0)
    {
      Rectangle leftEdge = new(Source.X, Source.Y + b, b, midSrcH);
      Rectangle rightEdge = new(Source.Right - b, Source.Y + b, b, midSrcH);
      PixelDraw.Tile(spriteBatch, Texture, leftEdge, new Rectangle(left, top + bs, bs, midDstH), scale, color);
      PixelDraw.Tile(spriteBatch, Texture, rightEdge, new Rectangle(right, top + bs, bs, midDstH), scale, color);
    }

    if (midDstW > 0 && midDstH > 0)
    {
      Rectangle middle = new(Source.X + b, Source.Y + b, midSrcW, midSrcH);
      PixelDraw.Tile(spriteBatch, Texture, middle, new Rectangle(left + bs, top + bs, midDstW, midDstH), scale, color);
    }
  }

  void Blit(SpriteBatch spriteBatch, Rectangle source, Rectangle destination, Color color)
    => spriteBatch.Draw(Texture, destination, source, color, 0f, Vector2.Zero, SpriteEffects.None, 0f);
}
