using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using MonoGame.GameFramework.Tools;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tools;

/// <summary>
/// The palettes as a set. These run against the real repo rather than fixtures
/// on purpose: the thing worth protecting is not that the checker works, it is
/// that the ten palettes actually shipped stay coherent as games get added.
/// </summary>
public class PaletteRegistryTests
{
  static string RepoRoot()
  {
    string palette = Palette.FindPaletteFile(Directory.GetCurrentDirectory());
    palette.Should().NotBeNull("the repo ships assets/palette.gpl");
    // <repo>/assets/palette.gpl -> <repo>
    return Directory.GetParent(Path.GetDirectoryName(palette)!)!.FullName;
  }

  public static IEnumerable<object[]> AllPalettes()
    => PaletteRegistry.FindAll(RepoRoot()).Select(p => new object[] { p });

  [Theory]
  [MemberData(nameof(AllPalettes))]
  public void EveryPalette_CarriesTheSharedSpine(string path)
  {
    // One shared colour is the whole of what holds nine art directions
    // together. A game that picks its own near-black breaks the family for a
    // difference no reviewer would consciously notice.
    PaletteRegistry.MissingSpine(Palette.Load(path)).Should().BeNull();
  }

  [Theory]
  [MemberData(nameof(AllPalettes))]
  public void EveryPalette_IsCollisionFree(string path)
  {
    // Two colours in one ramp slot are invisible to the eye and recorded in
    // every file, which turns "the mid blue" into a coin flip. Catching it at
    // the palette is cheap; catching it after the art ships means repainting.
    Palette.Load(path).FindRampCollisions().Should().BeEmpty();
  }

  [Theory]
  [MemberData(nameof(AllPalettes))]
  public void EveryPalette_HasNoDuplicateColoursOrNames(string path)
  {
    var entries = Palette.Load(path).Entries;
    entries.Select(e => e.Color).Should().OnlyHaveUniqueItems();
    entries.Select(e => e.Name.ToLowerInvariant()).Should().OnlyHaveUniqueItems();
  }

  [Fact]
  public void EveryGame_CarriesItsOwnPalette()
  {
    // A game silently falling through to assets/palette.gpl would still build
    // and still pass check-palette — it would just quietly look like the
    // Platformer, which is the failure this whole change exists to prevent.
    string src = Path.Combine(RepoRoot(), "src");
    // "MonoGame.GameFramework.*" also matches the library itself — .NET keeps
    // the 8.3-era rule that a trailing '*' matches an empty extension.
    var games = Directory.EnumerateDirectories(src, "MonoGame.GameFramework.*")
      .Select(Path.GetFileName)
      .Where(n => n != "MonoGame.GameFramework" && !n.EndsWith(".Tests") && !n.EndsWith(".Tools"))
      .ToList();

    games.Should().HaveCount(9, "the repo ships nine sample games");
    foreach (string game in games)
    {
      File.Exists(Path.Combine(src, game, "Content", "sprites", "palette.gpl"))
        .Should().BeTrue($"{game} needs its own Content/sprites/palette.gpl");
    }
  }

  [Fact]
  public void MissingSpine_ExplainsHowToFixIt()
  {
    string message = PaletteRegistry.MissingSpine(Palette.Parse(new[]
    {
      "GIMP Palette",
      " 61 126 200	blue-3",
    }));
    message.Should().Contain("outline").And.Contain("26  26  26");
  }

  [Fact]
  public void WrongSpineColour_IsCaughtEvenWhenTheNameIsRight()
  {
    string message = PaletteRegistry.MissingSpine(Palette.Parse(new[]
    {
      "GIMP Palette",
      " 20  20  24	outline",
    }));
    message.Should().Contain("#1A1A1A");
  }
}
