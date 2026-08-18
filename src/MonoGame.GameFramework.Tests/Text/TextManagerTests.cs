using System.Linq;
using FluentAssertions;
using Microsoft.Xna.Framework;
using MonoGame.GameFramework.Text;
using Xunit;

namespace MonoGame.GameFramework.Tests.Text;

/// <summary>
/// TextManager is pure handle bookkeeping and had no tests. Draw needs a real
/// SpriteBatch, so the group/handle logic is what is covered here.
/// </summary>
public class TextManagerTests
{
  [Fact]
  public void AddText_ReturnsDistinctHandles()
  {
    TextManager tm = new();
    TextHandle a = tm.AddText("hud", "score", Vector2.Zero, Color.White);
    TextHandle b = tm.AddText("hud", "lives", Vector2.Zero, Color.White);
    a.Should().NotBe(b);
  }

  [Fact]
  public void SetText_UpdatesTheRightElement()
  {
    TextManager tm = new();
    TextHandle a = tm.AddText("hud", "one", Vector2.Zero, Color.White);
    TextHandle b = tm.AddText("hud", "two", Vector2.Zero, Color.White);

    tm.SetText(a, "changed");

    tm.Find(a).Text.Should().Be("changed");
    tm.Find(b).Text.Should().Be("two");
  }

  [Fact]
  public void UpdateText_ReplacesTextPositionAndColour()
  {
    TextManager tm = new();
    TextHandle h = tm.AddText("hud", "one", Vector2.Zero, Color.White);

    tm.UpdateText(h, "two", new Vector2(5, 7), Color.Red);

    TextElement el = tm.Find(h);
    el.Text.Should().Be("two");
    el.Position.Should().Be(new Vector2(5, 7));
    el.Color.Should().Be(Color.Red);
  }

  [Fact]
  public void RemoveText_DropsOnlyThatHandle()
  {
    TextManager tm = new();
    TextHandle a = tm.AddText("hud", "one", Vector2.Zero, Color.White);
    TextHandle b = tm.AddText("hud", "two", Vector2.Zero, Color.White);

    tm.RemoveText(a);

    tm.Find(a).Should().BeNull();
    tm.Find(b).Should().NotBeNull();
  }

  [Fact]
  public void ClearGroup_RemovesOnlyThatGroup()
  {
    TextManager tm = new();
    tm.AddText("hud", "score", Vector2.Zero, Color.White);
    tm.AddText("hud", "lives", Vector2.Zero, Color.White);
    TextHandle console = tm.AddText("console", "boot", Vector2.Zero, Color.White);

    tm.ClearGroup("hud");

    tm.Elements.Should().ContainSingle();
    tm.Find(console).Should().NotBeNull();
  }

  [Fact]
  public void StaleHandle_IsIgnoredRatherThanThrowing()
  {
    TextManager tm = new();
    TextHandle h = tm.AddText("hud", "one", Vector2.Zero, Color.White);
    tm.RemoveText(h);

    tm.Invoking(x => x.SetText(h, "gone")).Should().NotThrow();
    tm.Invoking(x => x.UpdateText(h, "gone", Vector2.Zero, Color.White)).Should().NotThrow();
  }

  [Fact]
  public void TextAddedBeforeAnyFontIsLoaded_IsSkippedRatherThanThrowing()
  {
    // The regression: AddText captured the manager's font at call time and
    // LoadContent never backfilled, so anything registered before the font
    // loaded kept a null font forever and threw inside DrawString — one frame
    // away from the call that actually caused it. Draw now resolves the font at
    // draw time and skips what it cannot render.
    //
    // Draw never touches the SpriteBatch for a fontless element, which is what
    // makes a null batch safe here and is exactly the behaviour under test.
    TextManager tm = new();
    TextHandle h = tm.AddText("hud", "early", Vector2.Zero, Color.White);

    tm.Find(h).Font.Should().BeNull();
    tm.ResolveFont(tm.Find(h)).Should().BeNull("no font has been loaded yet");
    tm.Invoking(x => x.Draw(null)).Should().NotThrow();
  }

  [Fact]
  public void NullTextIsSkippedRatherThanThrowing()
  {
    TextManager tm = new();
    tm.AddText("hud", null, Vector2.Zero, Color.White);
    tm.Invoking(x => x.Draw(null)).Should().NotThrow();
  }

  [Fact]
  public void ScrollText_TrimsGroupToMaxLines()
  {
    TextManager tm = new();
    for (int i = 0; i < 5; i++) tm.AddText("log", $"line{i}", Vector2.Zero, Color.White);
    tm.AddText("hud", "kept", Vector2.Zero, Color.White);

    tm.ScrollText("log", pixelsBetweenLines: 10, maxLines: 3);

    tm.Elements.Count(e => e.Group == "log").Should().Be(3);
    tm.Elements.Count(e => e.Group == "hud").Should().Be(1);
  }

  [Fact]
  public void ScrollText_ShiftsOnlyItsOwnGroup()
  {
    TextManager tm = new();
    TextHandle log = tm.AddText("log", "a", new Vector2(0, 100), Color.White);
    TextHandle hud = tm.AddText("hud", "b", new Vector2(0, 100), Color.White);

    tm.ScrollText("log", pixelsBetweenLines: 10, maxLines: 10);

    tm.Find(log).Position.Y.Should().Be(110);
    tm.Find(hud).Position.Y.Should().Be(100);
  }
}
