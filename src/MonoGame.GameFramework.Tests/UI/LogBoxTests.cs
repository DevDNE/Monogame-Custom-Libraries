using System.Linq;
using FluentAssertions;
using MonoGame.GameFramework.UI;
using Xunit;

namespace MonoGame.GameFramework.Tests.UI;

public class LogBoxTests
{
  [Fact]
  public void Add_TrimsToMaxLines()
  {
    LogBox box = new(maxLines: 3);
    box.Add("a");
    box.Add("b");
    box.Add("c");
    box.Add("d");
    box.Count.Should().Be(3);
    box.Lines.ToArray().Should().Equal("b", "c", "d");
  }

  [Fact]
  public void Clear_EmptiesQueue()
  {
    LogBox box = new();
    box.Add("a");
    box.Add("b");
    box.Clear();
    box.Count.Should().Be(0);
  }

  [Fact]
  public void Add_Preserves_Order_OldestFirst()
  {
    LogBox box = new(maxLines: 6);
    box.Add("first");
    box.Add("second");
    box.Add("third");
    box.Lines.ToArray().Should().Equal("first", "second", "third");
  }

  [Fact]
  public void DefaultParameters_AreSensible()
  {
    LogBox box = new();
    box.MaxLines.Should().Be(6);
    box.FadeStart.Should().BeApproximately(0.55f, 1e-5f);
    box.FadeStep.Should().BeApproximately(0.08f, 1e-5f);
  }

  [Fact]
  public void Fade_IsKeyedToDistanceFromNewest_NotQueuePosition()
  {
    // The regression: fade counted up from the oldest *present* line, so every
    // line changed brightness as the box filled toward MaxLines and the newest
    // never reached full strength. Distance from the newest is stable.
    LogBox box = new(maxLines: 4, fadeStart: 0.5f, fadeStep: 0.1f);
    box.Add("a");
    float oneLine = box.FadeFor(0);

    box.Add("b");
    box.Add("c");
    float newestOfThree = box.FadeFor(2);

    oneLine.Should().Be(1f, "the newest line is always fully opaque");
    newestOfThree.Should().Be(1f);
  }

  [Fact]
  public void Fade_StepsDownFromNewestAndFloorsAtFadeStart()
  {
    LogBox box = new(maxLines: 6, fadeStart: 0.5f, fadeStep: 0.1f);
    for (int i = 0; i < 6; i++) box.Add($"line{i}");

    box.FadeFor(5).Should().BeApproximately(1.0f, 1e-5f);  // newest
    box.FadeFor(4).Should().BeApproximately(0.9f, 1e-5f);
    box.FadeFor(3).Should().BeApproximately(0.8f, 1e-5f);
    box.FadeFor(0).Should().BeApproximately(0.5f, 1e-5f);  // oldest, at the floor
  }

}
