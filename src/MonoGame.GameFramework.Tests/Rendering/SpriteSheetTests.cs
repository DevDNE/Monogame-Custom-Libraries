using FluentAssertions;
using Microsoft.Xna.Framework;
using MonoGame.GameFramework.Rendering;
using Xunit;

namespace MonoGame.GameFramework.Tests.Rendering;

public class SpriteSheetTests
{
  [Fact]
  public void Tint_DefaultIsWhite()
  {
    SpriteSheet s = new();
    s.Tint.Should().Be(Color.White);
  }

  [Fact]
  public void Tint_SetterRoundTrips()
  {
    SpriteSheet s = new() { Tint = Color.Red };
    s.Tint.Should().Be(Color.Red);
    s.Tint = Color.Blue;
    s.Tint.Should().Be(Color.Blue);
  }

  [Fact]
  public void Position_TracksDestinationFrame()
  {
    // Position used to be a settable field initialised once from the
    // destination rect and read by nothing, so assigning it looked like it
    // moved the sprite and did not. It is now derived from the one rect that
    // DrawManager draws and UIManager hit-tests against.
    SpriteSheet s = new() { DestinationFrame = new Rectangle(10, 20, 30, 40) };
    s.Position.Should().Be(new Vector2(10, 20));

    s.DestinationFrame = new Rectangle(70, 80, 30, 40);
    s.Position.Should().Be(new Vector2(70, 80));
  }

}
