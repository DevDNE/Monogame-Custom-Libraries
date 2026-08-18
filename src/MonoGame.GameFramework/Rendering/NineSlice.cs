using System;
using System.Collections.Generic;
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
    Color color = tint ?? Color.White;
    foreach ((Rectangle src, Rectangle dst) in SliceRects(Source, Border, destination, scale))
      Blit(spriteBatch, src, dst, color);
  }

  /// <summary>
  /// The source/destination pairs <see cref="Draw"/> would blit — corners, then
  /// the four edges, then the middle, with edges and middle already expanded
  /// into their repeated tiles.
  ///
  /// Split out for the same reason <see cref="PixelDraw.TileRects"/> is: this is
  /// nine rectangles of arithmetic where an off-by-one is a one-pixel seam or a
  /// corner drawn twice, none of it eyeballable and none of it assertable
  /// through a SpriteBatch. Static and texture-free, so it can be tested without
  /// a GraphicsDevice.
  /// </summary>
  public static IEnumerable<(Rectangle Source, Rectangle Destination)> SliceRects(
    Rectangle source, int border, Rectangle destination, int scale)
  {
    if (scale < 1) throw new ArgumentOutOfRangeException(nameof(scale), scale, "Pixel art scales by whole numbers only.");
    if (border < 0) throw new ArgumentOutOfRangeException(nameof(border), border, "Border cannot be negative.");
    if (destination.Width <= 0 || destination.Height <= 0) yield break;

    int b = border;
    int bs = b * scale;

    // Middle spans in source pixels — what is left after the two corners.
    int midSrcW = source.Width - b * 2;
    int midSrcH = source.Height - b * 2;

    // Destination spans. A rect narrower than both corners would draw them
    // overlapping; clamping at zero drops the middle and lets the corners meet,
    // which degrades more gracefully than a negative-width rectangle.
    int midDstW = Math.Max(0, destination.Width - bs * 2);
    int midDstH = Math.Max(0, destination.Height - bs * 2);

    int left = destination.X, right = destination.Right - bs;
    int top = destination.Y, bottom = destination.Bottom - bs;

    if (b > 0)
    {
      yield return (new Rectangle(source.X, source.Y, b, b), new Rectangle(left, top, bs, bs));
      yield return (new Rectangle(source.Right - b, source.Y, b, b), new Rectangle(right, top, bs, bs));
      yield return (new Rectangle(source.X, source.Bottom - b, b, b), new Rectangle(left, bottom, bs, bs));
      yield return (new Rectangle(source.Right - b, source.Bottom - b, b, b), new Rectangle(right, bottom, bs, bs));
    }

    if (midDstW > 0 && b > 0 && midSrcW > 0)
    {
      Rectangle topEdge = new(source.X + b, source.Y, midSrcW, b);
      Rectangle bottomEdge = new(source.X + b, source.Bottom - b, midSrcW, b);
      foreach (var pair in PixelDraw.TileRects(topEdge, new Rectangle(left + bs, top, midDstW, bs), scale))
        yield return pair;
      foreach (var pair in PixelDraw.TileRects(bottomEdge, new Rectangle(left + bs, bottom, midDstW, bs), scale))
        yield return pair;
    }

    if (midDstH > 0 && b > 0 && midSrcH > 0)
    {
      Rectangle leftEdge = new(source.X, source.Y + b, b, midSrcH);
      Rectangle rightEdge = new(source.Right - b, source.Y + b, b, midSrcH);
      foreach (var pair in PixelDraw.TileRects(leftEdge, new Rectangle(left, top + bs, bs, midDstH), scale))
        yield return pair;
      foreach (var pair in PixelDraw.TileRects(rightEdge, new Rectangle(right, top + bs, bs, midDstH), scale))
        yield return pair;
    }

    if (midDstW > 0 && midDstH > 0 && midSrcW > 0 && midSrcH > 0)
    {
      Rectangle middle = new(source.X + b, source.Y + b, midSrcW, midSrcH);
      foreach (var pair in PixelDraw.TileRects(middle, new Rectangle(left + bs, top + bs, midDstW, midDstH), scale))
        yield return pair;
    }
  }

  void Blit(SpriteBatch spriteBatch, Rectangle source, Rectangle destination, Color color)
    => spriteBatch.Draw(Texture, destination, source, color, 0f, Vector2.Zero, SpriteEffects.None, 0f);
}
