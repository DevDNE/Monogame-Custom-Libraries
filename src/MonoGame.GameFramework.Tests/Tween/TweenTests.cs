using FluentAssertions;
using Microsoft.Xna.Framework;
using MonoGame.GameFramework.Tweening;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tweening;

public class TweenTests
{
  [Fact]
  public void Float_AdvancesLinearlyByDefault()
  {
    var t = Tween.Float(0f, 10f, 2f);
    t.Update(1f).Should().BeApproximately(5f, 1e-5f);
    t.IsComplete.Should().BeFalse();
  }

  [Fact]
  public void Float_CompletesAtDuration()
  {
    var t = Tween.Float(0f, 10f, 2f);
    t.Update(2f);
    t.Current.Should().Be(10f);
    t.IsComplete.Should().BeTrue();
  }

  [Fact]
  public void Float_DoesNotOvershoot()
  {
    var t = Tween.Float(0f, 10f, 1f);
    t.Update(5f);
    t.Current.Should().Be(10f);
    t.Elapsed.Should().Be(1f);
  }

  [Fact]
  public void Reset_RestoresInitialState()
  {
    var t = Tween.Float(0f, 10f, 1f);
    t.Update(0.5f);
    t.Reset();
    t.Elapsed.Should().Be(0f);
    t.IsComplete.Should().BeFalse();
  }

  [Fact]
  public void Vec2_InterpolatesEachAxis()
  {
    var t = Tween.Vec2(new Vector2(0, 0), new Vector2(10, 20), 1f);
    Vector2 mid = t.Update(0.5f);
    mid.X.Should().BeApproximately(5f, 1e-5f);
    mid.Y.Should().BeApproximately(10f, 1e-5f);
  }

  [Fact]
  public void Color_InterpolatesEndpoints()
  {
    var t = Tween.Color(Microsoft.Xna.Framework.Color.Black, Microsoft.Xna.Framework.Color.White, 1f);
    t.Current.Should().Be(Microsoft.Xna.Framework.Color.Black);
    t.Update(1f).Should().Be(Microsoft.Xna.Framework.Color.White);
  }

  [Fact]
  public void ZeroDuration_CompletesImmediately()
  {
    var t = Tween.Float(0f, 5f, 0f);
    t.Update(0.001f).Should().Be(5f);
    t.IsComplete.Should().BeTrue();
  }

  [Fact]
  public void Reset_AlsoRewindsCurrent()
  {
    // Reset used to rewind Elapsed but leave Current holding the value it had
    // reached, so a tween that was reset and drawn before its next Update
    // showed its end state for one frame.
    var t = Tween.Float(0f, 10f, 1f);
    t.Update(1f);
    t.Current.Should().Be(10f);

    t.Reset();

    t.Current.Should().Be(0f);
    t.Elapsed.Should().Be(0f);
    t.IsComplete.Should().BeFalse();
  }

  [Fact]
  public void Reset_OnAZeroDurationTween_StaysAtTheEndValue()
  {
    var t = Tween.Float(0f, 5f, 0f);
    t.Reset();
    t.Current.Should().Be(5f);
  }

}
