using System.IO;
using System.Linq;
using FluentAssertions;
using MonoGame.GameFramework.Tools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tools;

public class SpriteConformerTests
{
  const string Gpl = """
    GIMP Palette
     26  26  26	outline
     31  63 115	blue-0-shadow
     61 126 200	blue-3
    255 224 184	warm-5-hilite
    """;

  static Palette TestPalette() => Palette.Parse(Gpl.Split('\n'));

  static string Dir() => Directory.CreateTempSubdirectory("mgf-conform-").FullName;

  static string SavePng(string path, Rgba32[,] pixels)
  {
    int h = pixels.GetLength(0), w = pixels.GetLength(1);
    using Image<Rgba32> img = new(w, h);
    for (int y = 0; y < h; y++)
      for (int x = 0; x < w; x++)
        img[x, y] = pixels[y, x];
    Directory.CreateDirectory(Path.GetDirectoryName(path));
    img.SaveAsPng(path);
    return path;
  }

  static Rgba32 PixelAt(string path, int x, int y)
  {
    using Image<Rgba32> img = Image.Load<Rgba32>(path);
    return img[x, y];
  }

  static Rgba32[,] Fill(int w, int h, Rgba32 color)
  {
    var px = new Rgba32[h, w];
    for (int y = 0; y < h; y++)
      for (int x = 0; x < w; x++)
        px[y, x] = color;
    return px;
  }

  [Fact]
  public void OffPaletteColour_SnapsToTheNearestEntry()
  {
    string dir = Dir();
    string input = SavePng(Path.Combine(dir, "in.png"),
      new[,] { { new Rgba32(0x3E, 0x7F, 0xC9) } });
    string output = Path.Combine(dir, "out.png");

    var result = SpriteConformer.Conform(input, output, TestPalette());

    PixelAt(output, 0, 0).Should().Be(new Rgba32(0x3D, 0x7E, 0xC8, 255));
    result.PixelsChanged.Should().Be(1);
    result.MeanDelta.Should().BeLessThan(0.01);
  }

  [Fact]
  public void AlreadyConformingArt_ReportsZeroDelta()
  {
    // The signal that separates "this art respects the palette" from "this art
    // is being forced onto it".
    string dir = Dir();
    string input = SavePng(Path.Combine(dir, "in.png"),
      new[,] { { new Rgba32(0x1F, 0x3F, 0x73) } });

    var result = SpriteConformer.Conform(input, Path.Combine(dir, "out.png"), TestPalette());

    result.MeanDelta.Should().BeApproximately(0, 1e-9);
    result.PixelsChanged.Should().Be(0);
  }

  [Fact]
  public void AlphaIsForcedBinary_AroundTheThreshold()
  {
    string dir = Dir();
    string input = SavePng(Path.Combine(dir, "in.png"), new[,]
    {
      {
        new Rgba32(0x1F, 0x3F, 0x73, 127),
        new Rgba32(0x1F, 0x3F, 0x73, 128),
      },
    });

    var result = SpriteConformer.Conform(input, Path.Combine(dir, "out.png"), TestPalette());

    PixelAt(Path.Combine(dir, "out.png"), 0, 0).A.Should().Be(0);
    PixelAt(Path.Combine(dir, "out.png"), 1, 0).A.Should().Be(255);
    result.AlphaFlattened.Should().Be(2);
  }

  [Fact]
  public void AlphaThreshold_IsConfigurable()
  {
    string dir = Dir();
    string input = SavePng(Path.Combine(dir, "in.png"),
      new[,] { { new Rgba32(0x1F, 0x3F, 0x73, 60) } });

    SpriteConformer.Conform(input, Path.Combine(dir, "out.png"), TestPalette(), alphaThreshold: 50);

    PixelAt(Path.Combine(dir, "out.png"), 0, 0).A.Should().Be(255);
  }

  [Fact]
  public void TargetSize_BoxResamplesDown()
  {
    string dir = Dir();
    string input = SavePng(Path.Combine(dir, "in.png"), Fill(64, 64, new Rgba32(0x3D, 0x7E, 0xC8)));

    var result = SpriteConformer.Conform(input, Path.Combine(dir, "out.png"), TestPalette(), 32, 32);

    result.SourceWidth.Should().Be(64);
    result.Width.Should().Be(32);
    result.Height.Should().Be(32);
    result.OpaquePixels.Should().Be(32 * 32);
    result.AspectDistorted.Should().BeFalse();
  }

  [Fact]
  public void FlatColourSurvivesAnIntegerDownscaleExactly()
  {
    // Box averaging over a uniform block returns the block's own colour, so
    // true pixel art at an integer reduction is unchanged — the property that
    // makes Box safe to use for both smooth and already-pixelated input.
    string dir = Dir();
    string input = SavePng(Path.Combine(dir, "in.png"), Fill(96, 96, new Rgba32(0x3D, 0x7E, 0xC8)));

    var result = SpriteConformer.Conform(input, Path.Combine(dir, "out.png"), TestPalette(), 32, 32);

    result.MeanDelta.Should().BeApproximately(0, 1e-9);
    PixelAt(Path.Combine(dir, "out.png"), 16, 16).Should().Be(new Rgba32(0x3D, 0x7E, 0xC8, 255));
  }

  [Fact]
  public void NonUniformScale_IsReportedNotSilentlyApplied()
  {
    string dir = Dir();
    string input = SavePng(Path.Combine(dir, "in.png"), Fill(64, 32, new Rgba32(0x3D, 0x7E, 0xC8)));

    var result = SpriteConformer.Conform(input, Path.Combine(dir, "out.png"), TestPalette(), 32, 32);

    result.AspectDistorted.Should().BeTrue();
  }

  [Fact]
  public void UsageHistogram_CountsEachEntry()
  {
    string dir = Dir();
    string input = SavePng(Path.Combine(dir, "in.png"), new[,]
    {
      { new Rgba32(0x1F, 0x3F, 0x73), new Rgba32(0x1F, 0x3F, 0x73) },
      { new Rgba32(0x3D, 0x7E, 0xC8), new Rgba32(0, 0, 0, 0) },
    });

    var result = SpriteConformer.Conform(input, Path.Combine(dir, "out.png"), TestPalette());

    result.Usage.Should().HaveCount(2);
    result.Usage[0].Entry.Name.Should().Be("blue-0-shadow");
    result.Usage[0].PixelCount.Should().Be(2);
    result.OpaquePixels.Should().Be(3);
  }

  [Fact]
  public void MissingOutputDirectory_IsCreated()
  {
    string dir = Dir();
    string input = SavePng(Path.Combine(dir, "in.png"), new[,] { { new Rgba32(0x1F, 0x3F, 0x73) } });
    string output = Path.Combine(dir, "deep", "nested", "out.png");

    SpriteConformer.Conform(input, output, TestPalette());

    File.Exists(output).Should().BeTrue();
  }

  [Fact]
  public void ConformOutput_AlwaysPassesTheChecker()
  {
    // The contract that makes the whole pipeline work: whatever goes in,
    // what comes out is gate-legal. Deliberately hostile input — full-spectrum
    // noise with feathered alpha, nothing near the palette.
    string dir = Dir();
    var noise = new Rgba32[16, 16];
    for (int y = 0; y < 16; y++)
      for (int x = 0; x < 16; x++)
        noise[y, x] = new Rgba32((byte)(x * 16), (byte)(y * 16), (byte)((x + y) * 8), (byte)(x * 17));
    string input = SavePng(Path.Combine(dir, "in.png"), noise);

    string palettePath = Path.Combine(dir, "palette.gpl");
    File.WriteAllText(palettePath, Gpl);

    string project = Directory.CreateTempSubdirectory("mgf-conform-proj-").FullName;
    SpriteConformer.Conform(input, Path.Combine(project, "sprites", "out.png"), TestPalette());

    var check = PaletteChecker.Check(project, palettePath);
    check.ScannedSprites.Should().ContainSingle();
    check.Violations.Should().BeEmpty();
  }
}
