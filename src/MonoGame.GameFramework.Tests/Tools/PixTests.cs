using System.IO;
using System.Linq;
using FluentAssertions;
using MonoGame.GameFramework.Tools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tools;

public class PixTests
{
  const string Gpl = """
    GIMP Palette
     26  26  26	outline
     31  63 115	blue-0-shadow
     61 126 200	blue-3
    """;

  static string WriteDir(params (string Name, string Contents)[] files)
  {
    string dir = Directory.CreateTempSubdirectory("mgf-pix-").FullName;
    File.WriteAllText(Path.Combine(dir, "palette.gpl"), Gpl);
    foreach ((string name, string contents) in files)
      File.WriteAllText(Path.Combine(dir, name), contents);
    return dir;
  }

  const string Chevron = """
    # a 4x3 chevron
    name chevron
    size 4 3
    key o outline
    key b blue-3
    pixels
    o..o
    .ob.
    ..o.
    """;

  [Fact]
  public void Parse_ReadsTheGridAndKey()
  {
    var doc = PixDocument.Parse(Chevron.Split('\n'));
    doc.Name.Should().Be("chevron");
    doc.Width.Should().Be(4);
    doc.Height.Should().Be(3);
    doc.Key['b'].Should().Be("blue-3");
  }

  [Fact]
  public void Render_MapsCharactersToPaletteColoursAndDotToTransparent()
  {
    string dir = WriteDir(("chevron.pix", Chevron));
    PixRenderer.RenderToFile(Path.Combine(dir, "chevron.pix"), Path.Combine(dir, "chevron.png"));

    using Image<Rgba32> png = Image.Load<Rgba32>(Path.Combine(dir, "chevron.png"));
    png.Width.Should().Be(4);
    png.Height.Should().Be(3);
    png[0, 0].Should().Be(new Rgba32(0x1A, 0x1A, 0x1A, 255));
    png[2, 1].Should().Be(new Rgba32(0x3D, 0x7E, 0xC8, 255));
    png[1, 0].A.Should().Be(0, "'.' is transparent");
  }

  [Fact]
  public void Render_ProducesOnlyBinaryAlpha()
  {
    // The property that makes check-palette's alpha rule unfalsifiable for
    // this front-end: there is no syntax for a half-transparent pixel.
    string dir = WriteDir(("chevron.pix", Chevron));
    PixRenderer.RenderToFile(Path.Combine(dir, "chevron.pix"), Path.Combine(dir, "chevron.png"));

    using Image<Rgba32> png = Image.Load<Rgba32>(Path.Combine(dir, "chevron.png"));
    for (int y = 0; y < png.Height; y++)
      for (int x = 0; x < png.Width; x++)
        png[x, y].A.Should().Match(a => a == 0 || a == 255);
  }

  [Fact]
  public void RowRepeatPrefix_ExpandsToThatManyRows()
  {
    var doc = PixDocument.Parse("""
      key o outline
      pixels
      *3 oo
      ..
      """.Split('\n'));
    doc.Height.Should().Be(4);
    doc.Rows.Take(3).Should().AllBe("oo");
  }

  [Fact]
  public void RaggedRows_AreARejectedFileNotAPaddedOne()
  {
    // Silently padding would make a typo render as art that is subtly wrong,
    // which puts the reviewer back to eyeballing PNGs.
    var act = () => PixDocument.Parse("""
      key o outline
      pixels
      ooo
      oo
      """.Split('\n'));
    act.Should().Throw<PixDocument.ParseException>().WithMessage("*row 1: 2 wide*");
  }

  [Fact]
  public void UnkeyedCharacter_IsRejected()
  {
    var act = () => PixDocument.Parse("""
      key o outline
      pixels
      oxo
      """.Split('\n'));
    act.Should().Throw<PixDocument.ParseException>().WithMessage("*row 0 col 1: 'x'*");
  }

  [Fact]
  public void DeclaredSizeDisagreeingWithTheGrid_IsRejected()
  {
    var act = () => PixDocument.Parse("""
      size 8 1
      key o outline
      pixels
      ooo
      """.Split('\n'));
    act.Should().Throw<PixDocument.ParseException>().WithMessage("*declared size is 8 wide, grid is 3*");
  }

  [Fact]
  public void StaleKeyEntry_IsRejected()
  {
    // A key naming a colour the grid no longer uses is almost always a rename
    // that stopped halfway.
    var act = () => PixDocument.Parse("""
      key o outline
      key b blue-3
      pixels
      ooo
      """.Split('\n'));
    act.Should().Throw<PixDocument.ParseException>().WithMessage("*never used in the grid*");
  }

  [Fact]
  public void KeyNamingAColourOutsideThePalette_IsRejectedWithASuggestion()
  {
    string dir = WriteDir(("bad.pix", """
      key b blue-9-nope
      pixels
      bb
      """));
    var act = () => PixRenderer.RenderToFile(Path.Combine(dir, "bad.pix"), Path.Combine(dir, "bad.png"));
    act.Should().Throw<PixDocument.ParseException>()
      .WithMessage("*not in the palette*").And.Message.Should().Contain("blue-0-shadow");
  }

  [Fact]
  public void Diff_IsNullWhenThePngMatchesItsSource()
  {
    string dir = WriteDir(("chevron.pix", Chevron));
    PixRenderer.RenderToFile(Path.Combine(dir, "chevron.pix"), Path.Combine(dir, "chevron.png"));
    PixRenderer.Diff(Path.Combine(dir, "chevron.pix"), Path.Combine(dir, "chevron.png")).Should().BeNull();
  }

  [Fact]
  public void Diff_ReportsTheFirstPixelThatDrifted()
  {
    // The PNG is committed but the .pix is the source. This is the check that
    // keeps that claim honest — hand-edit the export and CI says so.
    string dir = WriteDir(("chevron.pix", Chevron));
    string png = Path.Combine(dir, "chevron.png");
    PixRenderer.RenderToFile(Path.Combine(dir, "chevron.pix"), png);

    using (Image<Rgba32> tampered = Image.Load<Rgba32>(png))
    {
      tampered[2, 1] = new Rgba32(0x1F, 0x3F, 0x73, 255);
      tampered.SaveAsPng(png);
    }

    PixRenderer.Diff(Path.Combine(dir, "chevron.pix"), png).Should()
      .Contain("1 pixel(s)").And.Contain("first at 2,1").And.Contain("render-pix");
  }

  [Fact]
  public void Diff_ReportsAMissingExport()
  {
    string dir = WriteDir(("chevron.pix", Chevron));
    PixRenderer.Diff(Path.Combine(dir, "chevron.pix"), Path.Combine(dir, "chevron.png"))
      .Should().Contain("missing");
  }

  [Fact]
  public void Diff_IgnoresTheRgbUnderFullyTransparentPixels()
  {
    // PNG encoders may store anything beneath a zero alpha; judging those bytes
    // would fail exports that are pixel-identical on screen.
    string dir = WriteDir(("chevron.pix", Chevron));
    string png = Path.Combine(dir, "chevron.png");
    PixRenderer.RenderToFile(Path.Combine(dir, "chevron.pix"), png);

    using (Image<Rgba32> tampered = Image.Load<Rgba32>(png))
    {
      tampered[1, 0] = new Rgba32(0xFF, 0x00, 0xFF, 0x00);
      tampered.SaveAsPng(png);
    }

    PixRenderer.Diff(Path.Combine(dir, "chevron.pix"), png).Should().BeNull();
  }

  // ------------------------------------------------------------------
  // The `frames` directive. It exists so the animation gate is
  // self-limiting on a property of the file rather than on a list: a PNG
  // cannot say whether it is four poses or one wide tile, so the source does.
  // ------------------------------------------------------------------

  [Fact]
  public void Frames_IsNullOnAStill()
  {
    PixDocument doc = PixDocument.Parse("""
      size 4 2
      key o outline
      pixels
      oooo
      oooo
      """.Split('\n'));

    doc.Frames.Should().BeNull();
    doc.FrameWidth.Should().BeNull("a still has no frame width to report");
  }

  [Fact]
  public void Frames_DeclaredOnAStrip_GivesTheFrameWidth()
  {
    PixDocument doc = PixDocument.Parse("""
      size 8 2
      frames 4
      key o outline
      pixels
      oooooooo
      oooooooo
      """.Split('\n'));

    doc.Frames.Should().Be(4);
    doc.FrameWidth.Should().Be(2);
  }

  [Fact]
  public void Frames_ThatDoNotDivideTheWidth_AreRejected()
  {
    // Slicing anyway offsets every frame after the first, and the result still
    // renders, so the parser is the last place this can be caught cheaply.
    System.Action parse = () => PixDocument.Parse("""
      size 7 2
      frames 4
      key o outline
      pixels
      ooooooo
      ooooooo
      """.Split('\n'));

    parse.Should().Throw<PixDocument.ParseException>()
      .WithMessage("*does not divide*")
      .WithMessage("*1.75*");
  }

  [Fact]
  public void Frames_BelowOne_IsRejected()
  {
    System.Action parse = () => PixDocument.Parse("""
      size 4 2
      frames 0
      key o outline
      pixels
      oooo
      oooo
      """.Split('\n'));

    parse.Should().Throw<PixDocument.ParseException>().WithMessage("*at least 1*");
  }

  [Fact]
  public void Frames_IsListedAmongTheKnownDirectives()
  {
    // The error text is the only documentation a typo gets.
    System.Action parse = () => PixDocument.Parse("""
      size 4 2
      frmaes 2
      key o outline
      pixels
      oooo
      oooo
      """.Split('\n'));

    parse.Should().Throw<PixDocument.ParseException>().WithMessage("*frames*");
  }
}
