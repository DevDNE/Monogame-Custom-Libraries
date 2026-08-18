using System.IO;
using System.Linq;
using FluentAssertions;
using MonoGame.GameFramework.Tools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tools;

/// <summary>
/// Measuring an image is the step the pipeline never had, and every number it
/// reports is one that was previously guessed. The guesses were wrong: a
/// reference recreated by eye came out on a 32x40 canvas at 81% width when the
/// source was 32x32 at 47%, and all of it was one command away.
/// </summary>
public class ImageDescriberTests
{
  static string Dir() => Directory.CreateTempSubdirectory("mgf-describe-").FullName;

  static string Save(string path, Rgba32[,] px)
  {
    int h = px.GetLength(0), w = px.GetLength(1);
    using Image<Rgba32> img = new(w, h);
    for (int y = 0; y < h; y++)
      for (int x = 0; x < w; x++)
        img[x, y] = px[y, x];
    Directory.CreateDirectory(Path.GetDirectoryName(path));
    img.SaveAsPng(path);
    return path;
  }

  static readonly Rgba32 Clear = new(0, 0, 0, 0);
  static readonly Rgba32 Black = new(0, 0, 0, 255);
  static readonly Rgba32 Red = new(255, 0, 0, 255);

  static Rgba32[,] Blank(int w, int h)
  {
    var px = new Rgba32[h, w];
    for (int y = 0; y < h; y++)
      for (int x = 0; x < w; x++)
        px[y, x] = Clear;
    return px;
  }

  [Fact]
  public void DetectBlockSize_OnArtAlreadyAtNativeResolution_Is1()
  {
    var px = Blank(4, 4);
    px[0, 0] = Black;
    px[1, 2] = Red;
    string p = Save(Path.Combine(Dir(), "a.png"), px);

    ImageDescriber.Describe(p).BlockSize.Should().Be(1);
  }

  [Theory]
  [InlineData(2)]
  [InlineData(5)]
  [InlineData(20)]
  public void DetectBlockSize_OnAnIntegerUpscale_RecoversTheFactor(int scale)
  {
    // The failure this exists to prevent: a 640x640 PNG of 32x32 pixel art and
    // a 640x640 PNG of 40x40 pixel art are indistinguishable to the eye.
    var small = Blank(4, 4);
    small[0, 0] = Black;
    small[1, 2] = Red;
    small[3, 3] = Black;

    var big = Blank(4 * scale, 4 * scale);
    for (int y = 0; y < 4 * scale; y++)
      for (int x = 0; x < 4 * scale; x++)
        big[y, x] = small[y / scale, x / scale];

    ImageDescriber.Description d = ImageDescriber.Describe(Save(Path.Combine(Dir(), "b.png"), big));
    d.BlockSize.Should().Be(scale);
    d.Width.Should().Be(4);
    d.Height.Should().Be(4);
    d.IsUpscaled.Should().BeTrue();
  }

  [Fact]
  public void DetectBlockSize_WhenOneBlockIsNotUniform_DoesNotClaimTheFactor()
  {
    // A single dissenting pixel has to defeat the whole factor, or reducing by
    // it would silently discard real detail.
    var px = Blank(4, 4);
    for (int y = 0; y < 4; y++)
      for (int x = 0; x < 4; x++)
        px[y, x] = Black;
    px[1, 1] = Red;

    ImageDescriber.Describe(Save(Path.Combine(Dir(), "c.png"), px)).BlockSize.Should().Be(1);
  }

  [Fact]
  public void Describe_CountsColoursAtNativeResolutionNotFileResolution()
  {
    var small = Blank(2, 2);
    small[0, 0] = Black;
    small[0, 1] = Red;
    var big = Blank(20, 20);
    for (int y = 0; y < 20; y++)
      for (int x = 0; x < 20; x++)
        big[y, x] = small[y / 10, x / 10];

    ImageDescriber.Description d = ImageDescriber.Describe(Save(Path.Combine(Dir(), "d.png"), big));
    d.Colors.Should().HaveCount(2);
    d.OpaquePixels.Should().Be(2);
    d.TransparentPixels.Should().Be(2);
  }

  [Fact]
  public void Describe_ReportsWhereTheContentSitsOnTheCanvas()
  {
    // The number that made the first recreation wrong: content bounds are what
    // "feet-anchored" and "50% wide" are actually about.
    var px = Blank(32, 32);
    for (int y = 4; y < 32; y++)
      for (int x = 11; x < 26; x++)
        px[y, x] = Red;

    ImageDescriber.Bounds b = ImageDescriber.Describe(Save(Path.Combine(Dir(), "e.png"), px)).Content!.Value;
    b.X.Should().Be(11);
    b.Y.Should().Be(4);
    b.Width.Should().Be(15);
    b.Height.Should().Be(28);
    b.Bottom.Should().Be(32, "soles on the last row is what feet-anchored means");
  }

  [Fact]
  public void Describe_OnAFullyTransparentImage_HasNoContentBounds()
  {
    ImageDescriber.Describe(Save(Path.Combine(Dir(), "f.png"), Blank(8, 8)))
      .Content.Should().BeNull();
  }

  [Fact]
  public void Describe_ReportsPartialAlphaRatherThanHidingIt()
  {
    var px = Blank(2, 2);
    px[0, 0] = new Rgba32(255, 0, 0, 128);
    ImageDescriber.Description d = ImageDescriber.Describe(Save(Path.Combine(Dir(), "g.png"), px));
    d.BinaryAlpha.Should().BeFalse();
    d.AlphaValues.Should().Contain((byte)128);
  }

  [Fact]
  public void Reduce_ThrowsAwayNothing()
  {
    // Reduction is only offered at a factor where every block was uniform, so
    // it must be information-preserving: re-expanding reproduces the source.
    var small = Blank(3, 3);
    small[0, 0] = Black;
    small[2, 1] = Red;
    var big = Blank(9, 9);
    for (int y = 0; y < 9; y++)
      for (int x = 0; x < 9; x++)
        big[y, x] = small[y / 3, x / 3];

    using Image<Rgba32> loaded = Image.Load<Rgba32>(Save(Path.Combine(Dir(), "h.png"), big));
    using Image<Rgba32> reduced = ImageDescriber.Reduce(loaded, 3);

    reduced.Width.Should().Be(3);
    for (int y = 0; y < 3; y++)
      for (int x = 0; x < 3; x++)
        reduced[x, y].Should().Be(small[y, x]);
  }
}
