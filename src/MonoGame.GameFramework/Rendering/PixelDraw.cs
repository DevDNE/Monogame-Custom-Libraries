using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.GameFramework.Rendering;

/// <summary>
/// Draw helpers that cannot produce a blurry or half-pixel sprite.
///
/// The rules in assets/STYLE.md that break pixel art are all rules about the
/// call site — integer scale, point sampling, no non-uniform stretch — and
/// nothing enforced them at the point where the mistake is actually made.
/// check-sprites catches a bare Begin(), but it cannot see that a destination
/// rectangle was computed from a float division.
///
/// These take a scale factor rather than a destination rectangle, which makes
/// the failure unrepresentable: there is no argument you can pass that scales
/// 1.5x or squashes the aspect. Anything wanting a genuine stretch should call
/// SpriteBatch directly and own that decision explicitly.
/// </summary>
public static class PixelDraw
{
  /// <summary>Whole texture, top-left anchored, scaled by a whole number.</summary>
  public static void Sprite(
    SpriteBatch spriteBatch, Texture2D texture, int x, int y, int scale,
    Color? tint = null, SpriteEffects effects = SpriteEffects.None)
    => Frame(spriteBatch, texture, new Rectangle(0, 0, texture.Width, texture.Height), x, y, scale, tint, effects);

  /// <summary>One frame out of a sheet, top-left anchored, scaled by a whole number.</summary>
  public static void Frame(
    SpriteBatch spriteBatch, Texture2D texture, Rectangle source, int x, int y, int scale,
    Color? tint = null, SpriteEffects effects = SpriteEffects.None)
  {
    if (scale < 1) throw new ArgumentOutOfRangeException(nameof(scale), scale, "Pixel art scales by whole numbers only.");
    spriteBatch.Draw(
      texture,
      new Rectangle(x, y, source.Width * scale, source.Height * scale),
      source, tint ?? Color.White, 0f, Vector2.Zero, effects, 0f);
  }

  /// <summary>
  /// One frame out of a sheet, centred horizontally on <paramref name="centerX"/>
  /// and bottom-anchored to <paramref name="bottomY"/>.
  ///
  /// Feet-anchoring is the common case for a character standing on something,
  /// and doing the arithmetic at each call site is where the off-by-one that
  /// makes a sprite hover comes from (STYLE.md: "put the soles on row 31").
  /// </summary>
  public static void FrameFootAnchored(
    SpriteBatch spriteBatch, Texture2D texture, Rectangle source, int centerX, int bottomY, int scale,
    Color? tint = null, SpriteEffects effects = SpriteEffects.None)
    => Frame(spriteBatch, texture, source,
      centerX - source.Width * scale / 2, bottomY - source.Height * scale,
      scale, tint, effects);

  /// <summary>
  /// Fills <paramref name="destination"/> by repeating the texture, clipping the
  /// last row and column rather than squashing them.
  ///
  /// Tiling rather than stretching is the whole point: a 16x16 tile stretched
  /// to fill 800x600 has pixels of four different sizes, which is the single
  /// most obvious way to make pixel art look like a mistake.
  /// </summary>
  public static void Tile(
    SpriteBatch spriteBatch, Texture2D texture, Rectangle destination, int scale,
    Color? tint = null, Point? offset = null)
    => Tile(spriteBatch, texture, new Rectangle(0, 0, texture.Width, texture.Height), destination, scale, tint, offset);

  public static void Tile(
    SpriteBatch spriteBatch, Texture2D texture, Rectangle source, Rectangle destination, int scale,
    Color? tint = null, Point? offset = null)
  {
    Color color = tint ?? Color.White;
    foreach ((Rectangle src, Rectangle dst) in TileRects(source, destination, scale, offset))
      spriteBatch.Draw(texture, dst, src, color, 0f, Vector2.Zero, SpriteEffects.None, 0f);
  }

  /// <summary>
  /// The source/destination pairs <see cref="Tile"/> would draw.
  ///
  /// Split out from the draw call because the clipping is the part that is easy
  /// to get wrong and impossible to eyeball: an off-by-one here is a one-pixel
  /// seam that only appears at certain scroll offsets, or a partial tile that
  /// stretches instead of clipping. Neither survives being asserted on, and
  /// neither can be asserted on through a SpriteBatch.
  /// </summary>
  public static IEnumerable<(Rectangle Source, Rectangle Destination)> TileRects(
    Rectangle source, Rectangle destination, int scale, Point? offset = null)
  {
    if (scale < 1) throw new ArgumentOutOfRangeException(nameof(scale), scale, "Pixel art scales by whole numbers only.");
    if (destination.Width <= 0 || destination.Height <= 0) yield break;

    int tileW = source.Width * scale;
    int tileH = source.Height * scale;
    if (tileW <= 0 || tileH <= 0) yield break;

    // A non-zero offset scrolls the pattern; the modulo keeps the start point
    // within one tile of the destination so a parallax layer can be handed any
    // world coordinate, however large, without looping over empty space.
    //
    // The offset arrives in screen pixels but is snapped down to a whole source
    // pixel, so the start always sits a multiple of `scale` from the
    // destination edge. That is both the correct behaviour and the thing that
    // makes the clipping below exact: a parallax layer at 3x that scrolled by
    // one *screen* pixel would be showing a third of a pixel, which is the
    // artefact this whole file exists to prevent.
    Point shift = offset ?? Point.Zero;
    int startX = destination.X - Mod(shift.X, tileW) / scale * scale;
    int startY = destination.Y - Mod(shift.Y, tileH) / scale * scale;

    for (int y = startY; y < destination.Bottom; y += tileH)
    {
      for (int x = startX; x < destination.Right; x += tileW)
      {
        // Clip against the destination in *source* pixels, so a partial tile
        // draws a smaller source rather than a squashed full one. That is what
        // keeps every pixel exactly `scale` screen-pixels wide.
        //
        // Leading edges divide exactly, by the snap above. Trailing edges round
        // *up*: a destination whose size is not a multiple of the scale has no
        // whole pixel to put in the last sliver, and leaving up to scale-1
        // pixels unpainted beats drawing over whatever is outside.
        int clipLeft = Math.Max(0, destination.Left - x) / scale;
        int clipTop = Math.Max(0, destination.Top - y) / scale;
        int clipRight = CeilDiv(Math.Max(0, x + tileW - destination.Right), scale);
        int clipBottom = CeilDiv(Math.Max(0, y + tileH - destination.Bottom), scale);

        Rectangle src = new(
          source.X + clipLeft,
          source.Y + clipTop,
          source.Width - clipLeft - clipRight,
          source.Height - clipTop - clipBottom);
        if (src.Width <= 0 || src.Height <= 0) continue;

        yield return (src, new Rectangle(
          x + clipLeft * scale, y + clipTop * scale,
          src.Width * scale, src.Height * scale));
      }
    }
  }

  static int Mod(int value, int m) => ((value % m) + m) % m;

  static int CeilDiv(int value, int divisor) => (value + divisor - 1) / divisor;
}
