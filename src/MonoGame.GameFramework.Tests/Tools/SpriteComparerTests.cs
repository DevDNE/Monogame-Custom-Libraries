using System.IO;
using FluentAssertions;
using MonoGame.GameFramework.Tools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tools;

/// <summary>
/// Every other gate here answers "is this legal". None of them answers "is this
/// close", and for a recreation the two are unrelated — art can be perfectly
/// legal and completely wrong. Without a number, "it looks off" cannot drive an
/// iteration.
/// </summary>
public class SpriteComparerTests
{
  static string Dir() => Directory.CreateTempSubdirectory("mgf-compare-").FullName;

  static readonly Rgba32 Clear = new(0, 0, 0, 0);
  static readonly Rgba32 Red = new(200, 40, 40, 255);
  static readonly Rgba32 Blue = new(40, 40, 200, 255);

  static string Rect(string path, int w, int h, int x0, int y0, int rw, int rh, Rgba32? fill = null)
  {
    using Image<Rgba32> img = new(w, h);
    for (int y = 0; y < h; y++)
      for (int x = 0; x < w; x++)
        img[x, y] = x >= x0 && x < x0 + rw && y >= y0 && y < y0 + rh ? (fill ?? Red) : Clear;
    Directory.CreateDirectory(Path.GetDirectoryName(path));
    img.SaveAsPng(path);
    return path;
  }

  /// <summary>A square plus a one-cell mark in the bottom-left corner.</summary>
  static void WithCorner(string path, int size, int x0, int y0, int side)
  {
    int cell = size / 8;
    using Image<Rgba32> img = new(size, size);
    for (int y = 0; y < size; y++)
      for (int x = 0; x < size; x++)
      {
        bool inSquare = x >= x0 && x < x0 + side && y >= y0 && y < y0 + side;
        bool inCorner = x < cell && y >= size - cell;
        img[x, y] = inSquare || inCorner ? Red : Clear;
      }
    Directory.CreateDirectory(Path.GetDirectoryName(path));
    img.SaveAsPng(path);
  }

  [Fact]
  public void Compare_AnImageWithItself_IsPerfect()
  {
    string d = Dir();
    string a = Rect(Path.Combine(d, "a.png"), 16, 16, 4, 4, 8, 8);

    SpriteComparer.Comparison c = SpriteComparer.Compare(a, a);
    c.SameSize.Should().BeTrue();
    c.CanvasIou.Should().Be(1.0);
    c.ShapeIou.Should().Be(1.0);
    c.ExactMatchRatio.Should().Be(1.0);
    c.MeanDelta.Should().Be(0);
  }

  [Fact]
  public void Compare_NonOverlappingSilhouettes_ScoreZeroOnCanvas()
  {
    string d = Dir();
    string a = Rect(Path.Combine(d, "a.png"), 16, 16, 0, 0, 4, 4);
    string b = Rect(Path.Combine(d, "b.png"), 16, 16, 12, 12, 4, 4);

    SpriteComparer.Comparison c = SpriteComparer.Compare(a, b);
    c.CanvasIou.Should().Be(0);
    c.ShapeIou.Should().Be(1.0, "they are the same shape in different places — that is the distinction");
  }

  [Fact]
  public void Compare_SameShapeShifted_KeepsShapeIouAndLosesCanvasIou()
  {
    // Exactly the diagnostic that found the real defect: a recreation whose
    // silhouette was right but which sat 3px left of where it belonged.
    string d = Dir();
    string a = Rect(Path.Combine(d, "a.png"), 32, 32, 11, 4, 8, 8);
    string b = Rect(Path.Combine(d, "b.png"), 32, 32, 6, 4, 8, 8);

    SpriteComparer.Comparison c = SpriteComparer.Compare(a, b);
    c.ShapeIou.Should().Be(1.0);
    c.CanvasIou.Should().BeLessThan(0.5);
  }

  [Fact]
  public void Compare_DifferentCanvasSizes_ReportsNoCanvasMetricsButStillComparesShape()
  {
    string d = Dir();
    string a = Rect(Path.Combine(d, "a.png"), 32, 32, 8, 8, 8, 8);
    string b = Rect(Path.Combine(d, "b.png"), 32, 40, 8, 8, 8, 8);

    SpriteComparer.Comparison c = SpriteComparer.Compare(a, b);
    c.SameSize.Should().BeFalse();
    c.CanvasUnion.Should().Be(0, "a canvas comparison across different canvases would be a guess");
    c.ShapeIou.Should().Be(1.0);
  }

  [Fact]
  public void Compare_SameSilhouetteDifferentColours_SeparatesShapeFromColour()
  {
    string d = Dir();
    string a = Rect(Path.Combine(d, "a.png"), 16, 16, 4, 4, 8, 8, Red);
    string b = Rect(Path.Combine(d, "b.png"), 16, 16, 4, 4, 8, 8, Blue);

    SpriteComparer.Comparison c = SpriteComparer.Compare(a, b);
    c.CanvasIou.Should().Be(1.0, "the silhouette is identical");
    c.ExactMatchRatio.Should().Be(0, "and every pixel is the wrong colour");
    c.MeanDelta.Should().BeGreaterThan(0);
  }

  [Fact]
  public void Compare_ReducesUpscaledInputsBeforeMeasuring()
  {
    // A reference arrives as a big PNG far more often than at native size.
    //
    // The stray corner pixel pins the detected factor at 4. Without it the
    // rect alone is uniform at 8x too, and the detector correctly reports the
    // larger factor — an 8x8 of one square really is an 8x upscale of a 4x4.
    string d = Dir();
    string a = Path.Combine(d, "a.png");
    string big = Path.Combine(d, "big.png");
    WithCorner(a, 8, 2, 2, 4);
    WithCorner(big, 32, 8, 8, 16);

    SpriteComparer.Comparison c = SpriteComparer.Compare(a, big);
    c.WidthB.Should().Be(8, "the 4x upscale is reduced before anything is measured");
    c.CanvasIou.Should().Be(1.0);
  }
}
