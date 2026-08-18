using FluentAssertions;
using System;
using Microsoft.Xna.Framework;
using MonoGame.GameFramework.Rendering;
using Xunit;

namespace MonoGame.GameFramework.Tests.Rendering;

public class Camera2DTests
{
  private static Camera2D MakeCamera(Vector2? position = null, float zoom = 1f)
  {
    // PixelSnap first: it rejects a fractional zoom, and object initialisers
    // assign in written order. Tests exercising a non-whole zoom are testing
    // the smooth-scale path, which is exactly what turning it off declares.
    Camera2D cam = new(new Vector2(800, 600))
    {
      PixelSnap = zoom == MathF.Truncate(zoom) && zoom >= 1f,
      Position = position ?? Vector2.Zero,
      Zoom = zoom,
    };
    return cam;
  }

  [Fact]
  public void WorldToScreen_AtCameraPosition_IsViewportCenter()
  {
    Camera2D cam = MakeCamera(position: new Vector2(50, 75));
    Vector2 screen = cam.WorldToScreen(cam.Position);
    screen.X.Should().BeApproximately(400f, 1e-3f);
    screen.Y.Should().BeApproximately(300f, 1e-3f);
  }

  [Fact]
  public void ScreenToWorld_ViewportCenter_IsCameraPosition()
  {
    Camera2D cam = MakeCamera(position: new Vector2(250, -125));
    Vector2 world = cam.ScreenToWorld(new Vector2(400, 300));
    world.X.Should().BeApproximately(250f, 1e-3f);
    world.Y.Should().BeApproximately(-125f, 1e-3f);
  }

  [Theory]
  [InlineData(1f)]
  [InlineData(2f)]
  [InlineData(0.5f)]
  public void ScreenToWorld_RoundtripsThroughWorldToScreen(float zoom)
  {
    Camera2D cam = MakeCamera(position: new Vector2(17, 29), zoom: zoom);
    Vector2 original = new(123, 456);
    Vector2 screen = cam.WorldToScreen(original);
    Vector2 back = cam.ScreenToWorld(screen);
    back.X.Should().BeApproximately(original.X, 1e-3f);
    back.Y.Should().BeApproximately(original.Y, 1e-3f);
  }

  [Fact]
  public void ChangingPosition_UpdatesViewMatrix()
  {
    Camera2D cam = MakeCamera();
    Matrix before = cam.GetViewMatrix();
    cam.Position = new Vector2(100, 0);
    Matrix after = cam.GetViewMatrix();
    after.Should().NotBe(before);
  }

  [Fact]
  public void GetViewMatrix_StableWhenNothingChanges()
  {
    Camera2D cam = MakeCamera(position: new Vector2(10, 20), zoom: 1.5f);
    Matrix a = cam.GetViewMatrix();
    Matrix b = cam.GetViewMatrix();
    b.Should().Be(a);
  }

  [Fact]
  public void Zoom_ScalesWorldToScreenDeltaFromCenter()
  {
    Camera2D cam = MakeCamera(position: Vector2.Zero, zoom: 2f);
    // A point 10 world-units right of camera should land 20 screen-pixels right of center.
    Vector2 screen = cam.WorldToScreen(new Vector2(10, 0));
    (screen.X - 400f).Should().BeApproximately(20f, 1e-3f);
  }

  [Fact]
  public void Follow_CoversTheSameGroundRegardlessOfFrameRate()
  {
    // FollowLerp was applied as a fixed fraction per *frame*, so the camera
    // closed on the player at a different speed at 30fps than at 144fps. It is
    // now read as a fraction per 1/60s and converted to the elapsed frame.
    static float RunAt(float dt, int steps)
    {
      Camera2D cam = new(new Vector2(800, 600)) { Position = Vector2.Zero, FollowLerp = 0.5f };
      cam.Target = new Vector2(100, 0);
      GameTime gt = new(System.TimeSpan.Zero, System.TimeSpan.FromSeconds(dt));
      for (int i = 0; i < steps; i++) cam.Update(gt);
      return cam.Position.X;
    }

    float atSixty = RunAt(1f / 60f, 60);        // one second at 60fps
    float atThirty = RunAt(1f / 30f, 30);       // one second at 30fps
    float atOneForty = RunAt(1f / 144f, 144);   // one second at 144fps

    atThirty.Should().BeApproximately(atSixty, 0.5f);
    atOneForty.Should().BeApproximately(atSixty, 0.5f);
  }

  [Fact]
  public void Follow_AtSixtyFps_IsUnchangedFromAPlainLerp()
  {
    // The conversion must be a no-op at the default fixed timestep, or every
    // sample's camera feel would have shifted underneath it.
    Camera2D cam = new(new Vector2(800, 600)) { Position = Vector2.Zero, FollowLerp = 0.25f };
    cam.Target = new Vector2(100, 0);
    cam.Update(new GameTime(System.TimeSpan.Zero, System.TimeSpan.FromSeconds(1.0 / 60.0)));
    cam.Position.X.Should().BeApproximately(25f, 1e-3f);
  }

  [Fact]
  public void Follow_WithZeroElapsedTime_DoesNotMove()
  {
    Camera2D cam = new(new Vector2(800, 600)) { Position = Vector2.Zero, FollowLerp = 0.5f };
    cam.Target = new Vector2(100, 0);
    cam.Update(new GameTime(System.TimeSpan.Zero, System.TimeSpan.Zero));
    cam.Position.X.Should().Be(0f);
  }

  // ---- Pixel snapping -------------------------------------------------------

  [Fact]
  public void GetViewMatrix_SnapsTranslationToWholePixels()
  {
    Camera2D cam = MakeCamera(position: new Vector2(10.4f, 20.6f));
    Matrix view = cam.GetViewMatrix();
    view.M41.Should().Be(MathF.Round(view.M41));
    view.M42.Should().Be(MathF.Round(view.M42));
  }

  [Fact]
  public void WorldToScreen_FromAFractionalCamera_LandsAWholePixel()
  {
    // The artefact this prevents: a world-integer sprite corner landing on a
    // fraction of a screen pixel, so with PointClamp its edge is one pixel wide
    // on some frames and two on others while the camera drifts past it.
    Camera2D cam = MakeCamera(position: new Vector2(10.4f, 20.6f));
    Vector2 screen = cam.WorldToScreen(new Vector2(64, 128));
    screen.X.Should().Be(MathF.Round(screen.X));
    screen.Y.Should().Be(MathF.Round(screen.Y));
  }

  [Fact]
  public void PixelSnap_Off_LeavesTheFractionalTranslationAlone()
  {
    Camera2D cam = new(new Vector2(800, 600)) { PixelSnap = false, Position = new Vector2(10.4f, 20.6f) };
    Matrix view = cam.GetViewMatrix();
    view.M41.Should().BeApproximately(389.6f, 1e-3f);
    view.M42.Should().BeApproximately(279.4f, 1e-3f);
  }

  [Fact]
  public void EveryFrameOfAFollowLerp_LandsOnAWholePixel()
  {
    // The live case. FollowLerp holds a fractional position on nearly every
    // frame, which is why both camera-using samples were wrong before this.
    Camera2D cam = new(new Vector2(800, 600)) { Position = Vector2.Zero, FollowLerp = 0.1f };
    cam.Target = new Vector2(317, 199);
    GameTime gt = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

    for (int i = 0; i < 120; i++)
    {
      cam.Update(gt);
      Matrix view = cam.GetViewMatrix();
      view.M41.Should().Be(MathF.Round(view.M41), "frame {0} translation must be whole", i);
      view.M42.Should().Be(MathF.Round(view.M42), "frame {0} translation must be whole", i);
    }
  }

  [Fact]
  public void Snapping_DoesNotQuantiseTheCameraItself()
  {
    // Rounding the composed matrix rather than Position is what lets sub-pixel
    // motion accumulate. Snapping Position instead would make a camera moving
    // slower than one pixel per frame never move at all.
    Camera2D cam = new(new Vector2(800, 600)) { Position = Vector2.Zero, FollowLerp = 0.01f };
    cam.Target = new Vector2(10, 0);
    GameTime gt = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

    cam.Update(gt);
    cam.Position.X.Should().BeGreaterThan(0f).And.BeLessThan(1f);
  }

  [Fact]
  public void OddViewport_SnapsTheHalfPixelCentre()
  {
    Camera2D cam = new(new Vector2(801, 601)) { Position = Vector2.Zero };
    Matrix view = cam.GetViewMatrix();
    view.M41.Should().Be(MathF.Round(view.M41));
    view.M42.Should().Be(MathF.Round(view.M42));
  }

  [Fact]
  public void ShakeOffset_IsSnappedToo()
  {
    Camera2D cam = new(new Vector2(800, 600), new Random(7)) { Position = Vector2.Zero };
    cam.Shake(1f, 4f);
    cam.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0)));
    Matrix view = cam.GetViewMatrix();
    view.M41.Should().Be(MathF.Round(view.M41));
    view.M42.Should().Be(MathF.Round(view.M42));
  }

  [Fact]
  public void Shake_WithASeededRandom_IsReproducible()
  {
    static Vector2 Run()
    {
      Camera2D cam = new(new Vector2(800, 600), new Random(1234)) { Position = Vector2.Zero, PixelSnap = false };
      cam.Shake(1f, 8f);
      GameTime gt = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));
      for (int i = 0; i < 5; i++) cam.Update(gt);
      Matrix view = cam.GetViewMatrix();
      return new Vector2(view.M41, view.M42);
    }

    Run().Should().Be(Run());
  }

  [Theory]
  [InlineData(1.5f)]
  [InlineData(0.5f)]
  [InlineData(0f)]
  public void FractionalZoom_IsRejectedWhilePixelSnapIsOn(float zoom)
  {
    Camera2D cam = new(new Vector2(800, 600));
    Action set = () => cam.Zoom = zoom;
    set.Should().Throw<ArgumentOutOfRangeException>();
  }

  [Theory]
  [InlineData(1f)]
  [InlineData(2f)]
  [InlineData(4f)]
  public void WholeZoom_IsAcceptedWhilePixelSnapIsOn(float zoom)
  {
    Camera2D cam = new(new Vector2(800, 600));
    Action set = () => cam.Zoom = zoom;
    set.Should().NotThrow();
  }

  [Fact]
  public void TurningPixelSnapOn_OverAFractionalZoom_Throws()
  {
    Camera2D cam = new(new Vector2(800, 600)) { PixelSnap = false, Zoom = 1.5f };
    Action enable = () => cam.PixelSnap = true;
    enable.Should().Throw<ArgumentOutOfRangeException>();
  }

  [Fact]
  public void ScreenToWorld_StillRoundtrips_WhenSnapped()
  {
    // Snapping changes the matrix, so the inverse must be taken from the same
    // snapped matrix or a mouse-aimed shot would not go where the crosshair is.
    Camera2D cam = MakeCamera(position: new Vector2(17.3f, 29.8f));
    Vector2 screen = new(613, 84);
    Vector2 world = cam.ScreenToWorld(screen);
    Vector2 back = cam.WorldToScreen(world);
    back.X.Should().BeApproximately(screen.X, 1e-3f);
    back.Y.Should().BeApproximately(screen.Y, 1e-3f);
  }
}
