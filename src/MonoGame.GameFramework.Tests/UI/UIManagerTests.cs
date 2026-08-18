using FluentAssertions;
using Microsoft.Xna.Framework;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.UI;
using Xunit;

namespace MonoGame.GameFramework.Tests.UI;

public class UIManagerTests
{
  private static SpriteSheet MakeSprite(Rectangle bounds) => new() { DestinationFrame = bounds };

  [Fact]
  public void GetElementAt_InsideBounds_ReturnsAddedSprite()
  {
    UIManager ui = new(mouseManager: null);
    SpriteSheet s = MakeSprite(new Rectangle(100, 100, 50, 50));
    ui.AddUIElement("menu", s);
    ui.GetElementAt(new Vector2(120, 120)).Should().BeSameAs(s);
  }

  [Fact]
  public void GetElementAt_OutsideBounds_ReturnsNull()
  {
    UIManager ui = new(mouseManager: null);
    SpriteSheet s = MakeSprite(new Rectangle(100, 100, 50, 50));
    ui.AddUIElement("menu", s);
    ui.GetElementAt(new Vector2(500, 500)).Should().BeNull();
  }

  [Fact]
  public void RemoveUIElement_RemovesFromHitTesting()
  {
    UIManager ui = new(mouseManager: null);
    SpriteSheet s = MakeSprite(new Rectangle(0, 0, 100, 100));
    ui.AddUIElement("menu", s);
    ui.RemoveUIElement("menu", s);
    ui.GetElementAt(new Vector2(50, 50)).Should().BeNull();
  }

  [Fact]
  public void RemoveUIElement_ClearsFocusIfElementWasFocused()
  {
    UIManager ui = new(mouseManager: null);
    SpriteSheet s = MakeSprite(new Rectangle(0, 0, 100, 100));
    ui.AddUIElement("menu", s);
    ui.SetFocus(s);
    ui.RemoveUIElement("menu", s);
    ui.FocusedElement.Should().BeNull();
  }

  [Fact]
  public void AddUIElement_DoesNotRequireDrawManager()
  {
    // UIManager constructor only takes MouseManager — no DrawManager coupling.
    UIManager ui = new(mouseManager: null);
    SpriteSheet s = MakeSprite(new Rectangle(0, 0, 10, 10));
    ui.Invoking(x => x.AddUIElement("g", s)).Should().NotThrow();
  }

  [Fact]
  public void ElementCount_SumsAcrossGroups()
  {
    UIManager ui = new(mouseManager: null);
    ui.ElementCount.Should().Be(0);
    ui.AddUIElement("a", MakeSprite(new Rectangle(0, 0, 10, 10)));
    ui.AddUIElement("a", MakeSprite(new Rectangle(0, 0, 10, 10)));
    ui.AddUIElement("b", MakeSprite(new Rectangle(0, 0, 10, 10)));
    ui.ElementCount.Should().Be(3);
  }

  [Fact]
  public void GetElementAt_OverlappingGroups_ReturnsTheNewestGroup()
  {
    // Hit-testing used to walk Dictionary.Values, whose order is not defined,
    // so two overlapping elements in different groups resolved arbitrarily.
    UIManager ui = new(mouseManager: null);
    SpriteSheet under = MakeSprite(new Rectangle(0, 0, 100, 100));
    SpriteSheet over = MakeSprite(new Rectangle(0, 0, 100, 100));
    ui.AddUIElement("board", under);
    ui.AddUIElement("modal", over);

    ui.GetElementAt(new Vector2(50, 50)).Should().BeSameAs(over);
  }

  [Fact]
  public void GetElementAt_WithinAGroup_ReturnsTheMostRecentlyAdded()
  {
    UIManager ui = new(mouseManager: null);
    SpriteSheet first = MakeSprite(new Rectangle(0, 0, 100, 100));
    SpriteSheet second = MakeSprite(new Rectangle(0, 0, 100, 100));
    ui.AddUIElement("menu", first);
    ui.AddUIElement("menu", second);

    ui.GetElementAt(new Vector2(50, 50)).Should().BeSameAs(second);
  }

  [Fact]
  public void GroupOrder_IsCreationOrderAndIsStableAcrossEmptying()
  {
    UIManager ui = new(mouseManager: null);
    SpriteSheet a = MakeSprite(new Rectangle(0, 0, 10, 10));
    ui.AddUIElement("first", a);
    ui.AddUIElement("second", MakeSprite(new Rectangle(0, 0, 10, 10)));
    ui.GroupOrder.Should().Equal("first", "second");

    // Emptying a group must not restack it behind the other one.
    ui.RemoveUIElement("first", a);
    ui.AddUIElement("first", MakeSprite(new Rectangle(0, 0, 10, 10)));
    ui.GroupOrder.Should().Equal("first", "second");
  }

}
