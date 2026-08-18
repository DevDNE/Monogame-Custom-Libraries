using System.IO;
using System.Linq;
using FluentAssertions;
using MonoGame.GameFramework.Tools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tools;

/// <summary>
/// A cycle can be legal and still be broken, and most of the ways it breaks
/// are mechanical. These are the ones worth failing a build over: a frame that
/// does nothing, a hole in the cycle, a strip that cannot be sliced evenly.
/// Whether the walk *looks* right is not in here and cannot be —
/// scripts/anim-preview.sh exists for that half.
/// </summary>
public class AnimationCheckerTests
{
  static string Dir() => Directory.CreateTempSubdirectory("mgf-anim-").FullName;

  static readonly Rgba32 Clear = new(0, 0, 0, 0);
  static readonly Rgba32 Red = new(200, 40, 40, 255);
  static readonly Rgba32 Blue = new(40, 40, 200, 255);

  /// <summary>A strip of `marks`, one per frame: a 4x4 frame with a single
  /// pixel at the given position, or null for an entirely empty frame.</summary>
  static string Strip(string path, params (int X, int Y)?[] marks)
  {
    const int F = 4;
    using Image<Rgba32> img = new(F * marks.Length, F);
    for (int y = 0; y < F; y++)
      for (int x = 0; x < F * marks.Length; x++)
        img[x, y] = Clear;

    for (int i = 0; i < marks.Length; i++)
    {
      if (marks[i] is not (int mx, int my)) continue;
      // A floor pixel on the last row keeps every frame footed unless a test
      // deliberately lifts it.
      img[i * F + 0, F - 1] = Red;
      img[i * F + mx, my] = Red;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(path));
    img.SaveAsPng(path);
    return path;
  }

  [Fact]
  public void Check_OnAHealthyCycle_ReportsNoProblems()
  {
    string p = Strip(Path.Combine(Dir(), "ok.png"), (1, 0), (2, 0), (3, 0), (2, 1));
    AnimationChecker.Result r = AnimationChecker.Check(p, 4);

    r.Ok.Should().BeTrue();
    r.FrameCount.Should().Be(4);
    r.FrameWidth.Should().Be(4);
    r.BinaryAlpha.Should().BeTrue();
  }

  [Fact]
  public void Check_CatchesTwoIdenticalNeighbours()
  {
    // The defect this exists for: a frame that renders and advances the clock
    // and changes nothing on screen.
    string p = Strip(Path.Combine(Dir(), "dead.png"), (1, 0), (1, 0), (3, 0), (2, 1));
    AnimationChecker.Result r = AnimationChecker.Check(p, 4);

    r.Ok.Should().BeFalse();
    r.Problems.Should().ContainSingle().Which.Should().Contain("frames 0 and 1 are identical");
  }

  [Fact]
  public void Check_CatchesAStutterAtTheLoopSeam()
  {
    // The one boundary reading frames left-to-right never looks at.
    string p = Strip(Path.Combine(Dir(), "seam.png"), (1, 0), (2, 0), (3, 0), (1, 0));
    AnimationChecker.Result r = AnimationChecker.Check(p, 4);

    r.Ok.Should().BeFalse();
    r.Problems.Should().ContainSingle().Which.Should().Contain("frames 3 and 0 are identical");
  }

  [Fact]
  public void Check_WrapsTransitionsSoTheSeamIsAlwaysMeasured()
  {
    string p = Strip(Path.Combine(Dir(), "wrap.png"), (1, 0), (2, 0), (3, 0), (2, 1));
    AnimationChecker.Result r = AnimationChecker.Check(p, 4);

    r.Transitions.Should().HaveCount(4);
    r.Transitions.Last().From.Should().Be(3);
    r.Transitions.Last().To.Should().Be(0);
  }

  [Fact]
  public void Check_CatchesAnEmptyFrame()
  {
    string p = Strip(Path.Combine(Dir(), "hole.png"), (1, 0), null, (3, 0), (2, 1));
    AnimationChecker.Result r = AnimationChecker.Check(p, 4);

    r.Ok.Should().BeFalse();
    r.Problems.Should().Contain(x => x.Contains("frame 1 is entirely transparent"));
    r.Frames[1].Content.Should().BeNull();
  }

  [Fact]
  public void Check_CatchesAWidthThatDoesNotDivide()
  {
    // Slicing anyway would shift every frame after the first, and the result
    // still renders — which is exactly why it needs catching here.
    string p = Strip(Path.Combine(Dir(), "ragged.png"), (1, 0), (2, 0), (3, 0));
    AnimationChecker.Result r = AnimationChecker.Check(p, 5);

    r.Ok.Should().BeFalse();
    r.Problems.Should().Contain(x => x.Contains("does not divide"));
  }

  [Fact]
  public void Check_CatchesPartialAlpha()
  {
    string dir = Dir();
    string p = Strip(Path.Combine(dir, "alpha.png"), (1, 0), (2, 0));
    using (Image<Rgba32> img = Image.Load<Rgba32>(p))
    {
      img[2, 2] = new Rgba32(200, 40, 40, 128);
      img.SaveAsPng(p);
    }

    AnimationChecker.Result r = AnimationChecker.Check(p, 4);
    r.Ok.Should().BeFalse();
    r.BinaryAlpha.Should().BeFalse();
    r.Problems.Should().Contain(x => x.Contains("partial alpha"));
  }

  [Fact]
  public void Check_ReportsFootingPerFrameWithoutFailingOnIt()
  {
    // A walk keeps a sole on the last row. A jump does not, and neither does a
    // spinning coin, so this is reported and never failed.
    string dir = Dir();
    string p = Strip(Path.Combine(dir, "foot.png"), (1, 0), (2, 0));
    using (Image<Rgba32> img = Image.Load<Rgba32>(p))
    {
      img[4, 3] = Clear;   // lift frame 1 off the floor
      img.SaveAsPng(p);
    }

    AnimationChecker.Result r = AnimationChecker.Check(p, 4);
    r.Frames[0].SolesOnLastRow.Should().BeTrue();
    r.Frames[1].SolesOnLastRow.Should().BeFalse();
    r.Ok.Should().BeTrue("footing is a report, not a gate");
  }

  [Fact]
  public void Check_ReportsWhereEachTransitionChanged()
  {
    // The bounding box is the useful half: "the two contact frames differ only
    // in the boot region" is a claim a reviewer can check without an eye.
    string p = Strip(Path.Combine(Dir(), "region.png"), (1, 0), (1, 1));
    AnimationChecker.Result r = AnimationChecker.Check(p, 4);

    AnimationChecker.Transition t = r.Transitions[0];
    t.ChangedPixels.Should().Be(2);
    t.ChangedBounds!.Value.X.Should().Be(1);
    t.ChangedBounds!.Value.Y.Should().Be(0);
    t.ChangedBounds!.Value.Height.Should().Be(2);
  }

  [Fact]
  public void Check_OnASingleFrame_HasNoTransitionsAndNoProblems()
  {
    string p = Strip(Path.Combine(Dir(), "one.png"), (1, 0));
    AnimationChecker.Result r = AnimationChecker.Check(p, 4);

    r.FrameCount.Should().Be(1);
    r.Transitions.Should().BeEmpty("a single frame has nothing to differ from, including itself");
    r.Ok.Should().BeTrue();
  }
}
