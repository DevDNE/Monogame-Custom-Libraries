using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Xna.Framework;
using MonoGame.GameFramework.Rendering;
using Xunit;

namespace MonoGame.GameFramework.Tests.Rendering;

public class PixelDrawTests
{
  static readonly Rectangle Tile16 = new(0, 0, 16, 16);

  [Fact]
  public void TileRects_CoversTheDestinationExactlyOnAnEvenFit()
  {
    var rects = PixelDraw.TileRects(Tile16, new Rectangle(0, 0, 64, 32), scale: 1).ToList();
    rects.Should().HaveCount(8, "64x32 holds 4x2 tiles of 16x16");
    rects.Should().OnlyContain(r => r.Source == Tile16, "no tile needed clipping");
    rects.Sum(r => r.Destination.Width * r.Destination.Height).Should().Be(64 * 32);
  }

  [Fact]
  public void TileRects_ClipsThePartialTileInsteadOfSquashingIt()
  {
    // The whole reason this is tiling and not a stretched Draw: the last column
    // must show 4 source pixels at full size, not 16 squashed into 4.
    var rects = PixelDraw.TileRects(Tile16, new Rectangle(0, 0, 20, 16), scale: 1).ToList();
    rects.Should().HaveCount(2);
    rects[1].Source.Width.Should().Be(4);
    rects[1].Destination.Width.Should().Be(4);
  }

  [Fact]
  public void TileRects_KeepsEveryPixelExactlyScaleWide()
  {
    // The invariant that makes the art look right. If any rect's destination is
    // not its source times the scale, something is being stretched.
    const int scale = 3;
    foreach ((Rectangle src, Rectangle dst) in
             PixelDraw.TileRects(Tile16, new Rectangle(7, 5, 101, 73), scale, new Point(11, 4)))
    {
      dst.Width.Should().Be(src.Width * scale);
      dst.Height.Should().Be(src.Height * scale);
    }
  }

  [Fact]
  public void TileRects_NeverDrawsOutsideTheDestination()
  {
    Rectangle destination = new(10, 20, 100, 70);
    foreach ((_, Rectangle dst) in PixelDraw.TileRects(Tile16, destination, scale: 2, new Point(5, 9)))
      destination.Contains(dst).Should().BeTrue($"{dst} escaped {destination}");
  }

  [Fact]
  public void TileRects_SnapsAScrollOffsetToAWholeSourcePixel()
  {
    // At 3x, scrolling by one screen pixel would show a third of a pixel. The
    // offset is quantised down to a whole source pixel instead, so 3, 4 and 5
    // screen pixels of scroll are all the same one-pixel step.
    var at3 = PixelDraw.TileRects(Tile16, new Rectangle(0, 0, 96, 48), scale: 3, new Point(3, 0)).First();
    var at5 = PixelDraw.TileRects(Tile16, new Rectangle(0, 0, 96, 48), scale: 3, new Point(5, 0)).First();
    at3.Should().Be(at5);

    var at6 = PixelDraw.TileRects(Tile16, new Rectangle(0, 0, 96, 48), scale: 3, new Point(6, 0)).First();
    at6.Should().NotBe(at3, "6 screen pixels is a further whole source pixel");
  }

  [Fact]
  public void TileRects_ScrollsThePatternWithoutLeavingAGap()
  {
    // A parallax layer is handed a world coordinate, so the offset shifts the
    // pattern while the destination stays put — and the destination must still
    // end up fully covered.
    Rectangle destination = new(0, 0, 64, 16);
    var rects = PixelDraw.TileRects(Tile16, destination, scale: 1, new Point(5, 0)).ToList();
    rects.Sum(r => r.Destination.Width * r.Destination.Height).Should().Be(64 * 16);
    rects.Select(r => r.Destination.Left).Min().Should().Be(0);
    rects.Select(r => r.Destination.Right).Max().Should().Be(64);
  }

  [Fact]
  public void TileRects_HandlesAHugeOffsetWithoutIteratingToIt()
  {
    // Modulo-first is what stops a camera at x=2,000,000 from looping two
    // million times before reaching the visible area.
    var rects = PixelDraw.TileRects(Tile16, new Rectangle(0, 0, 32, 16), scale: 1, new Point(2_000_000, 0)).ToList();
    rects.Should().HaveCountLessThan(6);
    rects.Sum(r => r.Destination.Width * r.Destination.Height).Should().Be(32 * 16);
  }

  [Fact]
  public void TileRects_HandlesANegativeOffset()
  {
    var rects = PixelDraw.TileRects(Tile16, new Rectangle(0, 0, 64, 16), scale: 1, new Point(-5, 0)).ToList();
    rects.Sum(r => r.Destination.Width * r.Destination.Height).Should().Be(64 * 16);
  }

  [Fact]
  public void TileRects_OnAnEmptyDestination_DrawsNothing()
  {
    PixelDraw.TileRects(Tile16, new Rectangle(0, 0, 0, 40), scale: 1).Should().BeEmpty();
    PixelDraw.TileRects(Tile16, new Rectangle(0, 0, 40, -3), scale: 1).Should().BeEmpty();
  }

  [Fact]
  public void TileRects_RejectsAFractionalScale()
  {
    // There is no fractional scale to reject — the type system did that. What
    // is left is zero and negative, which would silently draw nothing.
    Action act = () => PixelDraw.TileRects(Tile16, new Rectangle(0, 0, 16, 16), scale: 0).ToList();
    act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*whole numbers*");
  }
}
