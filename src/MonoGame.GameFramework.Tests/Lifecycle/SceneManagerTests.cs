using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.GameFramework.Lifecycle;
using Xunit;

namespace MonoGame.GameFramework.Tests.Lifecycle;

public class SceneManagerTests
{
  private class FakeScene : GameScene
  {
    public int LoadCount, UnloadCount, UpdateCount;
    public override void LoadContent(ContentManager content) => LoadCount++;
    public override void UnloadContent() => UnloadCount++;
    public override void Update(GameTime gameTime) => UpdateCount++;
  }

  /// <summary>ContentManager only needs a service provider to construct.</summary>
  private class StubServices : IServiceProvider
  {
    public object GetService(Type serviceType) => null;
  }

  static ContentManager Content() => new(new StubServices());

  static SceneManager Ready()
  {
    SceneManager sm = new();
    sm.LoadContent(Content());
    return sm;
  }

  [Fact]
  public void LoadScene_LoadsContentAndBecomesCurrent()
  {
    SceneManager sm = Ready();
    FakeScene scene = new();
    sm.AddScene("battle", scene);

    sm.LoadScene("battle");

    scene.LoadCount.Should().Be(1);
    sm.Update(new GameTime());
    scene.UpdateCount.Should().Be(1);
  }

  [Fact]
  public void LoadScene_UnloadsThePreviousScene()
  {
    SceneManager sm = Ready();
    FakeScene a = new(), b = new();
    sm.AddScene("a", a);
    sm.AddScene("b", b);

    sm.LoadScene("a");
    sm.LoadScene("b");

    a.UnloadCount.Should().Be(1);
    b.LoadCount.Should().Be(1);
  }

  [Fact]
  public void RemoveScene_OfTheCurrentScene_StopsUpdatingIt()
  {
    // The regression: RemoveScene unloaded the scene and dropped it from the
    // dictionary without checking whether it was the current one, so Update and
    // Draw kept calling into content that had already been unloaded.
    SceneManager sm = Ready();
    FakeScene scene = new();
    sm.AddScene("battle", scene);
    sm.LoadScene("battle");

    sm.RemoveScene("battle");
    sm.Update(new GameTime());

    scene.UnloadCount.Should().Be(1);
    scene.UpdateCount.Should().Be(0);
  }

  [Fact]
  public void RemoveScene_OfAnInactiveScene_LeavesTheCurrentOneAlone()
  {
    SceneManager sm = Ready();
    FakeScene current = new(), other = new();
    sm.AddScene("current", current);
    sm.AddScene("other", other);
    sm.LoadScene("current");

    sm.RemoveScene("other");
    sm.Update(new GameTime());

    current.UpdateCount.Should().Be(1);
  }

  [Fact]
  public void LoadScene_BeforeLoadContent_ThrowsSomethingThatNamesTheCause()
  {
    // Previously this handed the scene a null ContentManager and failed deep
    // inside the scene's own LoadContent.
    SceneManager sm = new();
    sm.AddScene("battle", new FakeScene());

    sm.Invoking(x => x.LoadScene("battle"))
      .Should().Throw<InvalidOperationException>()
      .WithMessage("*LoadContent*");
  }

  [Fact]
  public void UnknownScene_Throws()
  {
    SceneManager sm = Ready();
    sm.Invoking(x => x.LoadScene("nope")).Should().Throw<KeyNotFoundException>();
    sm.Invoking(x => x.RemoveScene("nope")).Should().Throw<KeyNotFoundException>();
  }

  [Fact]
  public void UpdateAndDraw_WithNoCurrentScene_AreNoOps()
  {
    SceneManager sm = Ready();
    sm.Invoking(x => x.Update(new GameTime())).Should().NotThrow();
    sm.Invoking(x => x.Draw(null, new GameTime())).Should().NotThrow();
  }
}
