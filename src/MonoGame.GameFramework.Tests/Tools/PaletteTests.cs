using System.Linq;
using FluentAssertions;
using MonoGame.GameFramework.Tools;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tools;

public class PaletteTests
{
  const string Gpl = """
    GIMP Palette
    Name: Test
    Columns: 4
    #
    # a comment
     26  26  26	outline
     31  63 115	blue-0-shadow
     42  93 160	blue-2-mid
     61 126 200	blue-3
    """;

  [Fact]
  public void Parse_ReadsColoursAndNames_SkippingHeadersAndComments()
  {
    var palette = Palette.Parse(Gpl.Split('\n'));
    palette.Count.Should().Be(4);
    palette.Entries[0].Name.Should().Be("outline");
    palette.Entries[0].Color.Hex.Should().Be("#1A1A1A");
    palette.Entries[3].Color.Hex.Should().Be("#3D7EC8");
  }

  [Fact]
  public void Parse_FallsBackToHexWhenRowHasNoName()
  {
    var palette = Palette.Parse(new[] { "GIMP Palette", " 10  20  30" });
    palette.Entries.Should().ContainSingle().Which.Name.Should().Be("#0A141E");
  }

  [Fact]
  public void Parse_IgnoresMalformedRows()
  {
    // A row that is not three parseable bytes must not silently widen the
    // palette — that would let genuinely off-palette art pass the gate.
    var palette = Palette.Parse(new[] { "GIMP Palette", "10 20", "not a colour", "999 0 0", " 1  2  3	ok" });
    palette.Entries.Should().ContainSingle().Which.Name.Should().Be("ok");
  }

  [Fact]
  public void Contains_IsExact()
  {
    var palette = Palette.Parse(Gpl.Split('\n'));
    palette.Contains(new Palette.Rgb(0x1A, 0x1A, 0x1A)).Should().BeTrue();
    palette.Contains(new Palette.Rgb(0x1B, 0x1A, 0x1A)).Should().BeFalse();
  }

  [Fact]
  public void Nearest_PicksThePerceptuallyClosestEntry()
  {
    var palette = Palette.Parse(Gpl.Split('\n'));
    var (entry, distance) = palette.Nearest(new Palette.Rgb(0x3E, 0x7F, 0xC9));
    entry.Name.Should().Be("blue-3");
    distance.Should().BeLessThan(0.01);
  }

  [Fact]
  public void Nearest_OnAnExactMatch_IsZeroDistance()
  {
    var palette = Palette.Parse(Gpl.Split('\n'));
    var (entry, distance) = palette.Nearest(new Palette.Rgb(0x1F, 0x3F, 0x73));
    entry.Name.Should().Be("blue-0-shadow");
    distance.Should().BeApproximately(0, 1e-9);
  }

  [Fact]
  public void FindRampCollisions_FlagsTwoColoursSharingOneSlot()
  {
    // The real pair from the shipped palette: 4 degrees of hue apart, and 1.2
    // points of Oklab lightness. That is one ramp step drawn twice.
    var palette = Palette.Parse(new[]
    {
      "GIMP Palette",
      " 42  93 160	blue-2-mid",
      " 58  95 160	blue-2-mid-alt",
    });
    var collisions = palette.FindRampCollisions();
    collisions.Should().ContainSingle();
    collisions[0].LightnessDelta.Should().BeApproximately(1.17, 0.05);
    collisions[0].HueDelta.Should().BeLessThan(15);
  }

  [Fact]
  public void FindRampCollisions_SeparatesBrightStepsThatHsvValueCouldNot()
  {
    // Both of these max out the red channel, so HSV value scores both at
    // exactly 100 and the pair reads as one slot — which is why the check
    // measures Oklab lightness instead. They are a hot pink and a powder pink,
    // 11 points apart, and flagging them trains people to ignore the check.
    var palette = Palette.Parse(new[]
    {
      "GIMP Palette",
      "255 108 186	neon-pink-1",
      "255 176 222	neon-pink-2-glow",
    });
    palette.FindRampCollisions().Should().BeEmpty();
  }

  [Fact]
  public void FindRampCollisions_LeavesAdjacentRampStepsAlone()
  {
    // blue-4 and blue-5-hilite are 2.6 degrees apart in hue and 5.8 in Oklab
    // lightness — the tightest legitimate neighbours in the repo. A loose
    // threshold flags them, and a check that cries wolf gets muted. This pins
    // the threshold choice from the other side.
    var palette = Palette.Parse(new[]
    {
      "GIMP Palette",
      " 90 156 224	blue-4",
      "111 176 232	blue-5-hilite",
    });
    palette.FindRampCollisions().Should().BeEmpty();
  }

  [Fact]
  public void FindRampCollisions_IgnoresGreys()
  {
    // Greys have no hue, so they cannot compete for a slot in a colour ramp
    // no matter how close their values sit.
    var palette = Palette.Parse(new[]
    {
      "GIMP Palette",
      " 26  26  26	outline",
      " 28  28  28	outline-alt",
    });
    palette.FindRampCollisions().Should().BeEmpty();
  }

  [Fact]
  public void FindRampCollisions_IgnoresDifferentHueFamilies()
  {
    var palette = Palette.Parse(new[]
    {
      "GIMP Palette",
      "107  68  35	warm-1",
      " 31  63 115	blue-0-shadow",
    });
    palette.FindRampCollisions().Should().BeEmpty();
  }

  [Fact]
  public void ShippedPalette_IsCollisionFree()
  {
    // The palette carried a collision when it was first extracted (#3A5FA0
    // duplicating #2A5DA0's ramp slot); it was resolved by dropping the stray
    // and re-running conform-sprite over every affected file. This keeps it
    // resolved — adding a colour that lands on an existing ramp slot fails
    // here rather than quietly reintroducing invisible drift.
    string path = Palette.FindPaletteFile(System.IO.Directory.GetCurrentDirectory());
    path.Should().NotBeNull("the repo ships assets/palette.gpl");
    var palette = Palette.Load(path);
    palette.Count.Should().Be(13);
    palette.FindRampCollisions().Should().BeEmpty();
  }

  [Fact]
  public void ShippedPalette_HasTheExpectedRamps()
  {
    string path = Palette.FindPaletteFile(System.IO.Directory.GetCurrentDirectory());
    var names = Palette.Load(path).Entries.Select(e => e.Name).ToList();
    names.Should().Contain("outline").And.Contain("accent-rust");
    names.Count(n => n.StartsWith("warm-")).Should().Be(6);
    names.Count(n => n.StartsWith("blue-")).Should().Be(5);
  }

  [Fact]
  public void FindPaletteFile_PrefersAPaletteSittingBesideTheArt()
  {
    // This is what makes per-game palettes work without a name-to-palette
    // mapping table: the palette is found by being next to the thing it
    // constrains, so moving art moves its rules with it.
    var root = System.IO.Directory.CreateTempSubdirectory("mgf-palfind-").FullName;
    var shared = System.IO.Path.Combine(root, "assets");
    var game = System.IO.Path.Combine(root, "src", "Game", "Content", "sprites");
    System.IO.Directory.CreateDirectory(shared);
    System.IO.Directory.CreateDirectory(game);
    System.IO.File.WriteAllText(System.IO.Path.Combine(shared, "palette.gpl"), Gpl);
    System.IO.File.WriteAllText(System.IO.Path.Combine(game, "palette.gpl"), Gpl);

    Palette.FindPaletteFile(game).Should().Be(System.IO.Path.Combine(game, "palette.gpl"));
  }

  [Fact]
  public void FindPaletteFile_FallsThroughToTheRepoWideDefault()
  {
    // Art with no palette of its own keeps resolving exactly as it did before
    // per-game palettes existed. This is what let the change land without
    // touching a single committed PNG.
    var root = System.IO.Directory.CreateTempSubdirectory("mgf-palfind-").FullName;
    var shared = System.IO.Path.Combine(root, "assets");
    var game = System.IO.Path.Combine(root, "src", "Game", "Content", "sprites");
    System.IO.Directory.CreateDirectory(shared);
    System.IO.Directory.CreateDirectory(game);
    System.IO.File.WriteAllText(System.IO.Path.Combine(shared, "palette.gpl"), Gpl);

    Palette.FindPaletteFile(game).Should().Be(System.IO.Path.Combine(shared, "palette.gpl"));
  }
}
