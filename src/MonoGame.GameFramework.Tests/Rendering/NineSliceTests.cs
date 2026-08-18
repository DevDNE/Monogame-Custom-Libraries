using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Xna.Framework;
using MonoGame.GameFramework.Rendering;
using Xunit;

namespace MonoGame.GameFramework.Tests.Rendering;

/// <summary>
/// NineSlice geometry. The type is drawn by every title screen in the repo and
/// had no tests at all, while PixelDraw.TileRects next door had ten — so the
/// blit arithmetic was extracted into the same shape and is asserted here.
/// Source rect is a 16x16 frame with a 4px border, matching the repo's frames.
/// </summary>
public class NineSliceTests
{
  static readonly Rectangle Frame = new(0, 0, 16, 16);
  const int Border = 4;

  static List<(Rectangle Source, Rectangle Destination)> Slice(Rectangle destination, int scale = 1, int border = Border)
    => NineSlice.SliceRects(Frame, border, destination, scale).ToList();

  [Fact]
  public void CornersAreNeverScaledByTheDestination()
  {
    var rects = Slice(new Rectangle(0, 0, 100, 60), scale: 2);
    var corners = rects.Take(4).ToList();

    // Every corner is border x border in source and border*scale on screen,
    // whatever size the destination is.
    corners.Should().OnlyContain(r => r.Source.Width == Border && r.Source.Height == Border);
    corners.Should().OnlyContain(r => r.Destination.Width == Border * 2 && r.Destination.Height == Border * 2);
  }

  [Fact]
  public void CornersSitInTheFourCornersOfTheDestination()
  {
    Rectangle dest = new(10, 20, 100, 60);
    var corners = Slice(dest, scale: 1).Take(4).Select(r => r.Destination).ToList();

    corners.Should().Contain(new Rectangle(10, 20, 4, 4));                 // top-left
    corners.Should().Contain(new Rectangle(10 + 100 - 4, 20, 4, 4));       // top-right
    corners.Should().Contain(new Rectangle(10, 20 + 60 - 4, 4, 4));        // bottom-left
    corners.Should().Contain(new Rectangle(10 + 100 - 4, 20 + 60 - 4, 4, 4)); // bottom-right
  }

  [Fact]
  public void CornersReadFromTheFourCornersOfTheSource()
  {
    var corners = Slice(new Rectangle(0, 0, 64, 64)).Take(4).Select(r => r.Source).ToList();

    corners.Should().Contain(new Rectangle(0, 0, 4, 4));
    corners.Should().Contain(new Rectangle(12, 0, 4, 4));
    corners.Should().Contain(new Rectangle(0, 12, 4, 4));
    corners.Should().Contain(new Rectangle(12, 12, 4, 4));
  }

  [Fact]
  public void EveryPieceStaysInsideTheDestination()
  {
    Rectangle dest = new(7, 13, 103, 57);
    foreach (var (_, d) in Slice(dest, scale: 3))
    {
      dest.Contains(d).Should().BeTrue($"piece {d} escaped the frame {dest}");
    }
  }

  [Fact]
  public void EveryPieceReadsFromInsideTheSourceFrame()
  {
    foreach (var (s, _) in Slice(new Rectangle(0, 0, 120, 90), scale: 2))
    {
      Frame.Contains(s).Should().BeTrue($"source {s} escaped the frame {Frame}");
    }
  }

  [Fact]
  public void NoTwoPiecesOverlap()
  {
    var rects = Slice(new Rectangle(0, 0, 96, 64), scale: 2).Select(r => r.Destination).ToList();
    for (int i = 0; i < rects.Count; i++)
      for (int j = i + 1; j < rects.Count; j++)
        rects[i].Intersects(rects[j]).Should().BeFalse($"{rects[i]} overlaps {rects[j]}");
  }

  [Fact]
  public void EveryPieceKeepsPixelsSquareAtTheGivenScale()
  {
    const int scale = 3;
    foreach (var (s, d) in Slice(new Rectangle(0, 0, 120, 90), scale))
    {
      d.Width.Should().Be(s.Width * scale);
      d.Height.Should().Be(s.Height * scale);
    }
  }

  [Fact]
  public void DestinationExactlyTwoBordersWide_DrawsCornersOnlyAndTheyMeet()
  {
    var rects = Slice(new Rectangle(0, 0, 8, 8), scale: 1);
    rects.Should().HaveCount(4);
    rects.Select(r => r.Destination).Should().BeEquivalentTo(new[]
    {
      new Rectangle(0, 0, 4, 4), new Rectangle(4, 0, 4, 4),
      new Rectangle(0, 4, 4, 4), new Rectangle(4, 4, 4, 4),
    });
  }

  [Fact]
  public void ZeroBorder_YieldsOnlyTheTiledMiddle()
  {
    var rects = NineSlice.SliceRects(Frame, border: 0, new Rectangle(0, 0, 32, 32), scale: 1).ToList();
    rects.Should().NotBeEmpty();
    rects.Should().OnlyContain(r => r.Source == Frame);
    rects.Should().HaveCount(4); // 32x32 destination filled by a 16x16 tile
  }

  [Theory]
  [InlineData(0, 50)]
  [InlineData(50, 0)]
  [InlineData(-10, 20)]
  public void EmptyOrNegativeDestination_YieldsNothing(int width, int height)
    => Slice(new Rectangle(0, 0, width, height)).Should().BeEmpty();

  [Fact]
  public void FractionalScale_IsUnrepresentable_AndZeroIsRejected()
  {
    FluentActions.Invoking(() => Slice(new Rectangle(0, 0, 40, 40), scale: 0))
      .Should().Throw<System.ArgumentOutOfRangeException>()
      .WithMessage("*whole numbers only*");
  }

  [Fact]
  public void NegativeBorder_IsRejected()
  {
    FluentActions.Invoking(() => Slice(new Rectangle(0, 0, 40, 40), scale: 1, border: -1))
      .Should().Throw<System.ArgumentOutOfRangeException>();
  }

  [Fact]
  public void EdgesTileRatherThanStretch()
  {
    // A 16x16 frame with a 4px border has an 8px middle span. A destination
    // whose inner width is 24 must therefore repeat the top edge three times,
    // not stretch one copy across.
    var rects = Slice(new Rectangle(0, 0, 4 + 24 + 4, 40), scale: 1);
    var topEdge = rects.Where(r => r.Destination.Y == 0 && r.Source.X == 4).ToList();
    topEdge.Should().HaveCount(3);
    topEdge.Should().OnlyContain(r => r.Source.Width == 8);
  }

  [Fact]
  public void PartialTrailingTileIsClipped_NotSquashed()
  {
    // Inner width of 20 against an 8px middle span: two whole tiles and a 4px
    // remainder, which must be a narrower *source*, never a squashed full one.
    var rects = Slice(new Rectangle(0, 0, 4 + 20 + 4, 40), scale: 1);
    var topEdge = rects.Where(r => r.Destination.Y == 0 && r.Source.X >= 4 && r.Source.X < 12).ToList();
    topEdge.Should().HaveCount(3);
    topEdge[^1].Source.Width.Should().Be(4);
    topEdge[^1].Destination.Width.Should().Be(4);
  }
}
