using System.IO;
using System.Linq;
using FluentAssertions;
using MonoGame.GameFramework.Tools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tools;

public class PaletteCheckerTests
{
  static readonly Rgba32 Outline = new(0x1A, 0x1A, 0x1A);
  static readonly Rgba32 Blue0 = new(0x1F, 0x3F, 0x73);
  static readonly Rgba32 Blue3 = new(0x3D, 0x7E, 0xC8);
  static readonly Rgba32 Clear = new(0, 0, 0, 0);

  const string Gpl = """
    GIMP Palette
     26  26  26	outline
     31  63 115	blue-0-shadow
     61 126 200	blue-3
    """;

  static string WritePalette()
  {
    string path = Path.Combine(Directory.CreateTempSubdirectory("mgf-pal-").FullName, "palette.gpl");
    File.WriteAllText(path, Gpl);
    return path;
  }

  static void SavePng(string path, Rgba32[,] pixels)
  {
    int h = pixels.GetLength(0), w = pixels.GetLength(1);
    using Image<Rgba32> img = new(w, h);
    for (int y = 0; y < h; y++)
      for (int x = 0; x < w; x++)
        img[x, y] = pixels[y, x];
    Directory.CreateDirectory(Path.GetDirectoryName(path));
    img.SaveAsPng(path);
  }

  static string WriteProject(string spriteRelDir, params (string Name, Rgba32[,] Pixels)[] sprites)
  {
    string proj = Directory.CreateTempSubdirectory("mgf-palcheck-").FullName;
    foreach ((string name, Rgba32[,] pixels) in sprites)
      SavePng(Path.Combine(proj, spriteRelDir, name), pixels);
    return proj;
  }

  [Fact]
  public void NoPaletteFile_IsSkippedEntirely()
  {
    string proj = WriteProject(Path.Combine("Content", "sprites"),
      ("a.png", new[,] { { Blue0 } }));
    var result = PaletteChecker.Check(proj, Path.Combine(proj, "nope.gpl"));
    result.HasPalette.Should().BeFalse();
    result.Violations.Should().BeEmpty();
  }

  [Fact]
  public void ProjectWithNoSprites_IsSkippedButKeepsThePalette()
  {
    string proj = Directory.CreateTempSubdirectory("mgf-palcheck-").FullName;
    var result = PaletteChecker.Check(proj, WritePalette());
    result.HasPalette.Should().BeTrue();
    result.HasSprites.Should().BeFalse();
    result.Violations.Should().BeEmpty();
  }

  [Fact]
  public void ConformingSprite_HasNoViolations()
  {
    string proj = WriteProject(Path.Combine("Content", "sprites"),
      ("hero.png", new[,] { { Outline, Blue0 }, { Blue3, Clear } }));
    var result = PaletteChecker.Check(proj, WritePalette());
    result.ScannedSprites.Should().ContainSingle();
    result.Violations.Should().BeEmpty();
  }

  [Fact]
  public void OffPaletteColour_IsFlaggedWithCountAndNearestEntry()
  {
    var stray = new Rgba32(0x3E, 0x7F, 0xC9);
    string proj = WriteProject(Path.Combine("Content", "sprites"),
      ("hero.png", new[,] { { stray, stray }, { Blue0, Clear } }));
    var result = PaletteChecker.Check(proj, WritePalette());
    result.Violations.Should().ContainSingle()
      .Which.Description.Should()
        .Contain("2 pixel(s) of #3E7FC9")
        .And.Contain("Nearest is blue-3 #3D7EC8");
  }

  [Fact]
  public void PartialAlpha_IsFlagged()
  {
    var feathered = new Rgba32(0x1F, 0x3F, 0x73, 128);
    string proj = WriteProject(Path.Combine("Content", "sprites"),
      ("hero.png", new[,] { { feathered, Blue0 } }));
    var result = PaletteChecker.Check(proj, WritePalette());
    result.Violations.Should().ContainSingle()
      .Which.Description.Should().Contain("partial alpha").And.Contain("a=128");
  }

  [Fact]
  public void FullyTransparentPixels_AreIgnoredWhateverTheirRgb()
  {
    // PNG encoders are free to store any RGB under a zero alpha; judging those
    // bytes would fail sprites that are visually perfect.
    var junkUnderTransparent = new Rgba32(0xFF, 0x00, 0xFF, 0x00);
    string proj = WriteProject(Path.Combine("Content", "sprites"),
      ("hero.png", new[,] { { junkUnderTransparent, Blue0 } }));
    var result = PaletteChecker.Check(proj, WritePalette());
    result.Violations.Should().BeEmpty();
  }

  [Fact]
  public void SpritesUnderPlainSpritesDir_AreFound()
  {
    // assets/sprites/ has no Content/ layer, and it is where the authoring
    // sources live — it must be covered by the same command.
    string proj = WriteProject("sprites", ("hero.png", new[,] { { Blue0 } }));
    var result = PaletteChecker.Check(proj, WritePalette());
    result.ScannedSprites.Should().ContainSingle();
  }

  [Fact]
  public void SpritesUnderBinAndObj_AreSkipped()
  {
    string proj = WriteProject(Path.Combine("Content", "sprites"),
      ("good.png", new[,] { { Blue0 } }));
    SavePng(Path.Combine(proj, "obj", "sprites", "bad.png"),
      new[,] { { new Rgba32(0xFF, 0x00, 0xFF) } });
    SavePng(Path.Combine(proj, "bin", "sprites", "bad.png"),
      new[,] { { new Rgba32(0xFF, 0x00, 0xFF) } });
    var result = PaletteChecker.Check(proj, WritePalette());
    result.ScannedSprites.Should().ContainSingle();
    result.Violations.Should().BeEmpty();
  }

  [Fact]
  public void ManyOffPaletteColours_AreCappedWithASummaryLine()
  {
    // A wholly off-palette image would otherwise print one line per colour and
    // bury the signal.
    var pixels = new Rgba32[1, 12];
    for (int x = 0; x < 12; x++) pixels[0, x] = new Rgba32((byte)(200 + x), 10, 10);
    string proj = WriteProject(Path.Combine("Content", "sprites"), ("noise.png", pixels));
    var result = PaletteChecker.Check(proj, WritePalette());
    result.Violations.Should().HaveCount(9);
    result.Violations.Last().Description.Should()
      .Contain("and 4 more off-palette colour(s)").And.Contain("conform-sprite");
  }

  [Fact]
  public void RampCollisions_AreReportedWithoutBeingViolations()
  {
    string palette = Path.Combine(Directory.CreateTempSubdirectory("mgf-pal-").FullName, "palette.gpl");
    File.WriteAllText(palette, """
      GIMP Palette
       42  93 160	blue-2-mid
       58  95 160	blue-2-mid-alt
      """);
    string proj = WriteProject(Path.Combine("Content", "sprites"),
      ("hero.png", new[,] { { new Rgba32(0x2A, 0x5D, 0xA0) } }));
    var result = PaletteChecker.Check(proj, palette);
    result.Violations.Should().BeEmpty("a collision is INFO, not a failure");
    result.Collisions.Should().ContainSingle();
  }
}
