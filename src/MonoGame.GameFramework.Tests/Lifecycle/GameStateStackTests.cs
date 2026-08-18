using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Lifecycle;
using Xunit;

namespace MonoGame.GameFramework.Tests.Lifecycle;

/// <summary>
/// The stacking path: push onto a non-empty stack, paint order, the two hooks
/// that only fire when the stack is deeper than one, and pop.
///
/// None of this was covered. Every sample booted with one PushState and then
/// called ChangeState forever, so the stack was never deeper than one, Obscuring
/// and Revealed never fired anywhere, and Draw painted the stack top-first —
/// meaning a pushed overlay rendered *behind* the state it was covering. Nothing
/// caught it because nothing drew two states at once.
///
/// GameStateManager.Draw only forwards its SpriteBatch, so these pass null and
/// record the calls instead of rendering.
/// </summary>
public class GameStateStackTests
{
  private class RecordingState : GameState
  {
    public string Name = "";
    public List<string> Log = new();
    public int EnteredCount, LeavingCount, ObscuringCount, RevealedCount;

    public override void Entered() { EnteredCount++; IsActive = true; }
    public override void Leaving() { LeavingCount++; }
    public override void Obscuring() { ObscuringCount++; IsActive = false; }
    public override void Revealed() { RevealedCount++; IsActive = true; ClearVisibilityOverride(); }
    public override void Update(GameTime gameTime) => Log.Add($"update:{Name}");
    public override void Draw(SpriteBatch spriteBatch, GameTime gameTime) => Log.Add($"draw:{Name}");
  }

  /// <summary>A state that stays on screen while something is pushed over it.</summary>
  private class BackdropState : RecordingState
  {
    public override void Obscuring()
    {
      ObscuringCount++;
      IsActive = false;
      IsVisible = true;
    }
  }

  [Fact]
  public void Draw_PaintsBottomOfStackFirstAndTopLast()
  {
    List<string> log = new();
    GameStateManager m = new();
    BackdropState play = new() { Name = "play", Log = log };
    RecordingState pause = new() { Name = "pause", Log = log };

    m.PushState(play);
    m.PushState(pause);
    m.Draw(null, new GameTime());

    // The overlay must land last, or it is painted over by the scene it covers.
    log.Should().Equal("draw:play", "draw:pause");
  }

  [Fact]
  public void Update_RunsTopOfStackFirst()
  {
    List<string> log = new();
    GameStateManager m = new();
    RecordingState play = new() { Name = "play", Log = log };
    RecordingState pause = new() { Name = "pause", Log = log };
    // Keep both updating so the order itself is what is under test.
    m.PushState(play);
    m.PushState(pause);
    play.IsActive = true;

    m.Update(new GameTime());

    log.Should().Equal("update:pause", "update:play");
  }

  [Fact]
  public void PushThenPop_FiresObscuringThenRevealedOnTheStateBeneath()
  {
    GameStateManager m = new();
    BackdropState play = new() { Name = "play" };
    RecordingState pause = new() { Name = "pause" };

    m.PushState(play);
    play.ObscuringCount.Should().Be(0);

    m.PushState(pause);
    play.ObscuringCount.Should().Be(1);
    play.RevealedCount.Should().Be(0);

    m.PopState();
    pause.LeavingCount.Should().Be(1);
    play.RevealedCount.Should().Be(1);
    m.PeekState().Should().BeSameAs(play);
    m.StackDepth.Should().Be(1);
  }

  [Fact]
  public void ObscuredBackdrop_StopsUpdatingButKeepsDrawing()
  {
    List<string> log = new();
    GameStateManager m = new();
    BackdropState play = new() { Name = "play", Log = log };
    RecordingState pause = new() { Name = "pause", Log = log };

    m.PushState(play);
    m.PushState(pause);
    log.Clear();

    m.Update(new GameTime());
    m.Draw(null, new GameTime());

    // The scene beneath is frozen but still on screen — the whole point of a
    // pause overlay, and inexpressible while one flag gated both.
    log.Should().Equal("update:pause", "draw:play", "draw:pause");
  }

  [Fact]
  public void PoppingBack_RestoresTheBackdropToNormalUpdateAndDraw()
  {
    List<string> log = new();
    GameStateManager m = new();
    BackdropState play = new() { Name = "play", Log = log };
    RecordingState pause = new() { Name = "pause", Log = log };

    m.PushState(play);
    m.PushState(pause);
    m.PopState();
    log.Clear();

    m.Update(new GameTime());
    m.Draw(null, new GameTime());

    log.Should().Equal("update:play", "draw:play");
  }

  [Fact]
  public void HiddenState_IsNotDrawn()
  {
    List<string> log = new();
    GameStateManager m = new();
    RecordingState play = new() { Name = "play", Log = log };
    RecordingState pause = new() { Name = "pause", Log = log };

    m.PushState(play);   // Obscuring sets IsActive=false, and IsVisible follows it
    m.PushState(pause);

    m.Draw(null, new GameTime());

    // Default behaviour is unchanged: a state that stopped updating stops drawing.
    log.Should().Equal("draw:pause");
  }

  [Fact]
  public void IsVisible_FollowsIsActiveUntilSetExplicitly()
  {
    RecordingState s = new();
    s.IsActive = false;
    s.IsVisible.Should().BeFalse();
    s.IsActive = true;
    s.IsVisible.Should().BeTrue();

    s.IsVisible = false;
    s.IsActive.Should().BeTrue();
    s.IsVisible.Should().BeFalse();

    s.ClearVisibilityOverride();
    s.IsVisible.Should().BeTrue();
  }

  [Fact]
  public void DeepStack_DrawsBottomToTop()
  {
    List<string> log = new();
    GameStateManager m = new();
    for (int i = 0; i < 4; i++)
    {
      BackdropState s = new() { Name = $"s{i}", Log = log };
      m.PushState(s);
    }

    m.Draw(null, new GameTime());

    log.Should().Equal("draw:s0", "draw:s1", "draw:s2", "draw:s3");
  }

  private class SelfPoppingState : GameState
  {
    public GameStateManager Manager;
    public List<string> Log;
    public override void Entered() { IsActive = true; }
    public override void Leaving() { }
    public override void Obscuring() { IsActive = false; }
    public override void Revealed() { IsActive = true; }
    public override void Update(GameTime gameTime)
    {
      Log.Add("update:overlay");
      Manager.PopState();
    }
  }

  [Fact]
  public void StateRevealedByAPopMidUpdate_DoesNotAlsoRunThisFrame()
  {
    // Update runs top-down, so an overlay that pops itself hands control back to
    // a state further down the buffer that has not been visited yet — which then
    // sees the same input that dismissed the overlay. One press of the pause key
    // closed the menu and instantly reopened it.
    List<string> log = new();
    GameStateManager m = new();
    BackdropState play = new() { Name = "play", Log = log };
    m.PushState(play);
    m.PushState(new SelfPoppingState { Manager = m, Log = log });

    m.Update(new GameTime());

    log.Should().Equal("update:overlay");
    m.PeekState().Should().BeSameAs(play);
    play.RevealedCount.Should().Be(1);
  }

  [Fact]
  public void TheFrameAfterAPop_TheRevealedStateRunsNormally()
  {
    List<string> log = new();
    GameStateManager m = new();
    BackdropState play = new() { Name = "play", Log = log };
    m.PushState(play);
    m.PushState(new SelfPoppingState { Manager = m, Log = log });

    m.Update(new GameTime());
    log.Clear();
    m.Update(new GameTime());

    log.Should().Equal("update:play");
  }

  [Fact]
  public void PopOutsideUpdate_LeavesTheRevealedStateFreeToRunImmediately()
  {
    List<string> log = new();
    GameStateManager m = new();
    BackdropState play = new() { Name = "play", Log = log };
    RecordingState pause = new() { Name = "pause", Log = log };
    m.PushState(play);
    m.PushState(pause);

    m.PopState();          // popped from game code, not from inside Update
    m.Update(new GameTime());

    log.Should().Equal("update:play");
  }

}
