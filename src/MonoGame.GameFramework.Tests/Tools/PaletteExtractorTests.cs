using System.IO;
using System.Linq;
using FluentAssertions;
using MonoGame.GameFramework.Tools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tools;

/// <summary>
/// A palette invented alongside the art it describes is always in perfect
/// agreement with it and can still be completely wrong — check-palette only
/// asks whether the two match. Measuring the source is the only thing that
/// breaks that loop, which is what this exists for.
/// </summary>
public class PaletteExtractorTests
{
  static string Dir() => Directory.CreateTempSubdirectory("mgf-extract-").FullName;

  static string Save(string path, params Rgba32[] colors)
  {
    using Image<Rgba32> img = new(colors.Length, 1);
    for (int x = 0; x < colors.Length; x++) img[x, 0] = colors[x];
    Directory.CreateDirectory(Path.GetDirectoryName(path));
    img.SaveAsPng(path);
    return path;
  }

  static Palette.Rgb Rgb(byte r, byte g, byte b) => new(r, g, b);

  [Fact]
  public void FindOutline_PrefersPureBlackOverADarkSaturatedRed()
  {
    // The bug this replaced: the first rule snapped whatever fell inside an
    // Oklab radius of #1A1A1A. Pure #000000 — the commonest outline colour
    // there is — sits 0.2175 away, outside any safe radius, while #500000
    // sits 0.1239 inside it. On a real reference the radius rule picked the
    // dark red and left the black as a grey ramp step.
    PaletteExtractor.FindOutline(new[] { Rgb(0x50, 0, 0), Rgb(0, 0, 0), Rgb(255, 255, 255) })
      .Should().Be(Rgb(0, 0, 0));
  }

  [Fact]
  public void FindOutline_IgnoresDarkButSaturatedColours()
  {
    // A dark red is a ramp step, not a silhouette. Chroma is what separates
    // them; lightness alone does not.
    PaletteExtractor.FindOutline(new[] { Rgb(0x50, 0, 0), Rgb(0, 0x25, 0x5B) })
      .Should().BeNull();
  }

  [Fact]
  public void FindOutline_AcceptsTheSpineItself()
  {
    PaletteExtractor.FindOutline(new[] { Rgb(0x1A, 0x1A, 0x1A), Rgb(255, 255, 255) })
      .Should().Be(Rgb(0x1A, 0x1A, 0x1A));
  }

  [Fact]
  public void FindOutline_WhenNothingIsDarkEnough_IsNull()
  {
    PaletteExtractor.FindOutline(new[] { Rgb(200, 200, 200), Rgb(255, 255, 255) })
      .Should().BeNull();
  }

  [Fact]
  public void Extract_RewritesTheOutlineToTheSharedSpine()
  {
    string p = Save(Path.Combine(Dir(), "a.png"),
      new Rgba32(0, 0, 0, 255), new Rgba32(248, 64, 112, 255));

    PaletteExtractor.Extracted e = PaletteExtractor.Extract(p, "test");
    e.SnappedToSpine.Should().Be(Rgb(0, 0, 0));
    e.Palette.Contains(PaletteRegistry.SpineOutline).Should().BeTrue();
    e.Palette.Contains(Rgb(0, 0, 0)).Should().BeFalse("the source black is replaced, not kept alongside");
  }

  [Fact]
  public void Extract_AlwaysProducesAPaletteThatCarriesTheSpine()
  {
    // Every palette in the repo shares `outline`, and check-palettes fails
    // without it. An extractor that could emit an illegal palette would just
    // move the work downstream.
    string p = Save(Path.Combine(Dir(), "b.png"),
      new Rgba32(200, 200, 200, 255), new Rgba32(255, 255, 255, 255));

    PaletteExtractor.Extracted e = PaletteExtractor.Extract(p, "test");
    PaletteRegistry.MissingSpine(e.Palette).Should().BeNull();
  }

  [Fact]
  public void Extract_IgnoresTransparentPixels()
  {
    string p = Save(Path.Combine(Dir(), "c.png"),
      new Rgba32(0, 0, 0, 0), new Rgba32(248, 64, 112, 255));

    PaletteExtractor.Extract(p, "test").SourceColors.Should().Be(1);
  }

  [Fact]
  public void Extract_OrdersARampDarkToLightWithinItsHueFamily()
  {
    // All three must sit in one hue family for this to be a ramp at all —
    // #500000 and #B02860 look adjacent and are 25 degrees apart, which puts
    // them in different families and produces two ramps of one.
    string p = Save(Path.Combine(Dir(), "d.png"),
      new Rgba32(54, 111, 164, 255), new Rgba32(0, 37, 91, 255), new Rgba32(35, 73, 120, 255));

    var ramp = PaletteExtractor.Extract(p, "test").Palette.Entries
      .Where(e => e.Name != PaletteRegistry.SpineOutlineName)
      .ToList();

    ramp.Should().HaveCount(3);
    ramp[0].Color.Should().Be(Rgb(0, 37, 91));
    ramp[0].Name.Should().EndWith("-0-shadow");
    ramp[2].Color.Should().Be(Rgb(54, 111, 164));
    ramp[2].Name.Should().EndWith("-hilite");
  }

  [Fact]
  public void Extract_WritesAPaletteThatParsesBackToTheSameColours()
  {
    string p = Save(Path.Combine(Dir(), "e.png"),
      new Rgba32(0, 0, 0, 255), new Rgba32(248, 64, 112, 255), new Rgba32(54, 111, 164, 255));

    PaletteExtractor.Extracted e = PaletteExtractor.Extract(p, "test");
    Palette reparsed = Palette.Parse(e.Text.Split('\n'));

    reparsed.Entries.Select(x => x.Color)
      .Should().BeEquivalentTo(e.Palette.Entries.Select(x => x.Color));
  }

  [Fact]
  public void Extract_ClustersDownWhenAskedForFewerColours()
  {
    string p = Save(Path.Combine(Dir(), "f.png"),
      new Rgba32(0, 0, 0, 255),
      new Rgba32(250, 60, 110, 255), new Rgba32(248, 64, 112, 255), new Rgba32(246, 66, 114, 255),
      new Rgba32(50, 110, 160, 255), new Rgba32(54, 111, 164, 255));

    PaletteExtractor.Extracted e = PaletteExtractor.Extract(p, "test", maxColors: 3);
    e.SourceColors.Should().Be(6);
    e.Palette.Count.Should().BeLessThanOrEqualTo(4, "the spine may be added on top of the cluster count");
  }

  [Fact]
  public void Extract_RepresentsAClusterWithARealSourceColour()
  {
    // A centroid is a colour nothing in the image used. Naming the art's own
    // colours is the entire point.
    string p = Save(Path.Combine(Dir(), "g.png"),
      new Rgba32(0, 0, 0, 255), new Rgba32(250, 60, 110, 255), new Rgba32(246, 66, 114, 255));

    var sources = new[] { Rgb(0, 0, 0), Rgb(250, 60, 110), Rgb(246, 66, 114), PaletteRegistry.SpineOutline };
    PaletteExtractor.Extract(p, "test", maxColors: 2).Palette.Entries
      .Select(e => e.Color).Should().BeSubsetOf(sources);
  }
}
