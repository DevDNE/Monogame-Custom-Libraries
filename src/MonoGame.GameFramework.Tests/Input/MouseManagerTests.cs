using FluentAssertions;
using Microsoft.Xna.Framework;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Rendering;
using Xunit;

namespace MonoGame.GameFramework.Tests.Input;

/// <summary>
/// Update() reads the real mouse and needs a window, so these exercise the
/// transform against the default state instead. That is enough: the transform
/// is the new behaviour, and it is the piece that silently breaks every
/// hit-test in the repo if it is wired up wrong.
/// </summary>
public class MouseManagerTests
{
  [Fact]
  public void WithNoTransform_ReportsWindowPixels()
  {
    MouseManager mouse = new();
    mouse.GetMousePosition().Should().Be(mouse.GetWindowMousePosition());
  }

  [Fact]
  public void Transform_IsAppliedToTheReportedPosition()
  {
    MouseManager mouse = new() { PositionTransform = p => p + new Vector2(40, 25) };
    mouse.GetMousePosition().Should().Be(mouse.GetWindowMousePosition() + new Vector2(40, 25));
  }

  [Fact]
  public void Transform_DoesNotTouchTheRawWindowPosition()
  {
    // The raw reading has to stay available: anything positioning against the
    // real window -- a native cursor, an OS-level drag -- needs the untouched
    // value, and it is also how a scaler bug is diagnosed.
    MouseManager mouse = new();
    Vector2 raw = mouse.GetWindowMousePosition();
    mouse.PositionTransform = _ => new Vector2(999, 999);
    mouse.GetWindowMousePosition().Should().Be(raw);
  }

  [Fact]
  public void AScalerTransform_MapsWindowPixelsIntoDesignPixels()
  {
    // The wiring a game actually does, minus the device: everything downstream
    // of GetMousePosition -- UIManager hit-testing, GridMath cell picks,
    // Camera2D.ScreenToWorld -- then works in design pixels without knowing
    // the window was resized.
    MouseManager mouse = new()
    {
      PositionTransform = p => ScreenScaler.WindowToVirtual(p, 1024, 576, 2560, 1440),
    };

    // Default state is (0,0): the window's top-left corner, which at 2x with
    // 256x144 bars sits well outside the picture, up and to the left.
    Vector2 mapped = mouse.GetMousePosition();
    mapped.X.Should().Be(-128f);
    mapped.Y.Should().Be(-72f);
  }
}
