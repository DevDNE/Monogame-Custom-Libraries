using System.IO;
using System.Linq;
using FluentAssertions;
using MonoGame.GameFramework.Tools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tools;

/// <summary>
/// trace-pix is only safe if the .pix it writes is a faithful stand-in for the
/// PNG it read, because from that moment the .pix is the source and the PNG is
/// regenerated from it. Round-trip is therefore the contract, and most of what
/// is below is that one property approached from different angles.
/// </summary>
public class PixTracerTests
{
  const string Gpl = """
    GIMP Palette
     26  26  26	outline
    176  40  96	crimson-1
    248  64 112	crimson-2-hilite
    255 255 255	bone-2-hilite
    """;

  static Palette TestPalette() => Palette.Parse(Gpl.Split('\n'));

  static readonly Rgba32 Clear = new(0, 0, 0, 0);
  static readonly Rgba32 Outline = new(26, 26, 26, 255);
  static readonly Rgba32 Crimson = new(176, 40, 96, 255);
  static readonly Rgba32 Hilite = new(248, 64, 112, 255);
  static readonly Rgba32 Bone = new(255, 255, 255, 255);

  static string Dir() => Directory.CreateTempSubdirectory("mgf-trace-").FullName;

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

  static Rgba32[,] Sample() => new Rgba32[,]
  {
    { Clear,   Outline, Outline, Clear   },
    { Outline, Crimson, Crimson, Outline },
    { Outline, Crimson, Bone,    Outline },
    { Clear,   Hilite,  Hilite,  Clear   },
  };

  [Fact]
  public void Trace_ThenRender_ReproducesTheInputExactly()
  {
    // The whole contract. If this can drift, the .pix is not a source, it is a
    // lossy note about one.
    var px = Sample();
    string png = Save(Path.Combine(Dir(), "in.png"), px);

    PixTracer.TraceResult r = PixTracer.Trace(png, TestPalette(), "sample");
    using Image<Rgba32> rendered = PixRenderer.Render(
      PixDocument.Parse(r.Text.Split('\n')), TestPalette(), "sample");

    rendered.Width.Should().Be(4);
    rendered.Height.Should().Be(4);
    for (int y = 0; y < 4; y++)
      for (int x = 0; x < 4; x++)
        rendered[x, y].Should().Be(px[y, x], $"pixel {x},{y} must survive the round trip");
  }

  [Fact]
  public void Trace_OnAnUpscaledSource_ReducesToNativeFirst()
  {
    var small = Sample();
    var big = new Rgba32[16, 16];
    for (int y = 0; y < 16; y++)
      for (int x = 0; x < 16; x++)
        big[y, x] = small[y / 4, x / 4];

    PixTracer.TraceResult r = PixTracer.Trace(Save(Path.Combine(Dir(), "big.png"), big), TestPalette(), "sample");
    r.BlockSize.Should().Be(4);
    r.Width.Should().Be(4);
    r.Height.Should().Be(4);
  }

  [Fact]
  public void Trace_WithReduceDisabled_KeepsTheSourceResolution()
  {
    var small = Sample();
    var big = new Rgba32[8, 8];
    for (int y = 0; y < 8; y++)
      for (int x = 0; x < 8; x++)
        big[y, x] = small[y / 2, x / 2];

    PixTracer.Trace(Save(Path.Combine(Dir(), "big2.png"), big), TestPalette(), "s", reduce: false)
      .Width.Should().Be(8);
  }

  [Fact]
  public void Trace_RefusesAnOffPaletteColour()
  {
    // Snapping to the nearest entry here would repaint the art at exactly the
    // moment it becomes the source of truth, where the change is unrecoverable.
    var px = Sample();
    px[1, 1] = new Rgba32(1, 2, 3, 255);

    System.Action trace = () => PixTracer.Trace(Save(Path.Combine(Dir(), "off.png"), px), TestPalette(), "s");
    trace.Should().Throw<PixTracer.TraceException>()
      .WithMessage("*#010203*")
      .WithMessage("*conform-sprite*");
  }

  [Fact]
  public void Trace_RefusesPartialAlpha()
  {
    var px = Sample();
    px[1, 1] = new Rgba32(176, 40, 96, 128);

    System.Action trace = () => PixTracer.Trace(Save(Path.Combine(Dir(), "alpha.png"), px), TestPalette(), "s");
    trace.Should().Throw<PixTracer.TraceException>().WithMessage("*conform-sprite*");
  }

  [Fact]
  public void Trace_NeverAssignsTheReservedCharacters()
  {
    // '.' is transparent and '*' prefixes a row repeat; PixDocument rejects
    // both as keys, so the writer has to agree with the parser about them.
    PixTracer.TraceResult r = PixTracer.Trace(
      Save(Path.Combine(Dir(), "res.png"), Sample()), TestPalette(), "s");
    r.Key.Keys.Should().NotContain('.').And.NotContain('*');
  }

  [Fact]
  public void Trace_DrawsKeyCharactersFromThePaletteNames()
  {
    // The format's value is that a human can read the grid; 'c' for crimson
    // does that and an arbitrary symbol does not.
    PixTracer.TraceResult r = PixTracer.Trace(
      Save(Path.Combine(Dir(), "key.png"), Sample()), TestPalette(), "s");

    r.Key['o'].Should().Be("outline");
    r.Key.Should().Contain(kv => kv.Value == "crimson-1" && char.ToLowerInvariant(kv.Key) == 'c');
  }

  [Fact]
  public void Trace_CollapsesRunsOfIdenticalRows()
  {
    // The lone crimson pixel is load-bearing: without it the image is uniform,
    // reduces to a single row, and there is no run left to collapse.
    var px = new Rgba32[6, 2];
    for (int y = 0; y < 6; y++)
      for (int x = 0; x < 2; x++)
        px[y, x] = Outline;
    px[0, 0] = Crimson;

    PixTracer.TraceResult r = PixTracer.Trace(Save(Path.Combine(Dir(), "rep.png"), px), TestPalette(), "s");
    r.Text.Should().Contain("*5 ", "rows 1..5 are identical");

    // …and the collapsed form still parses back to six rows.
    PixDocument.Parse(r.Text.Split('\n')).Rows.Should().HaveCount(6);
  }

  [Fact]
  public void Trace_EmitsAPaletteDirectiveWhenAsked()
  {
    PixTracer.TraceResult r = PixTracer.Trace(
      Save(Path.Combine(Dir(), "dir.png"), Sample()), TestPalette(), "s", paletteDirective: "palette.gpl");
    r.Text.Should().Contain("palette palette.gpl");
  }
}
