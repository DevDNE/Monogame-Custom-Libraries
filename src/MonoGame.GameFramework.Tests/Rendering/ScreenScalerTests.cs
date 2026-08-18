using System;
using FluentAssertions;
using Microsoft.Xna.Framework;
using MonoGame.GameFramework.Rendering;
using Xunit;

namespace MonoGame.GameFramework.Tests.Rendering;

/// <summary>
/// Everything here is the pure half of ScreenScaler. The render target and the
/// Present call need a GraphicsDevice, which a test cannot create; the fit and
/// the coordinate mapping are the parts that decide whether the picture is
/// scaled by a whole number and whether a click lands where the player aimed,
/// so those are the parts that were kept device-free.
/// </summary>
public class ScreenScalerTests
{
  [Fact]
  public void Fit_ExactMultiple_FillsTheWindowWithNoBars()
  {
    (int scale, Rectangle dest) = ScreenScaler.Fit(1024, 576, 2048, 1152);
    scale.Should().Be(2);
    dest.Should().Be(new Rectangle(0, 0, 2048, 1152));
  }

  [Fact]
  public void Fit_NonMultiple_CentresAndLetterboxes()
  {
    // 1024x576 on a 1440p display: 2x fits, 3x does not.
    (int scale, Rectangle dest) = ScreenScaler.Fit(1024, 576, 2560, 1440);
    scale.Should().Be(2);
    dest.Should().Be(new Rectangle(256, 144, 2048, 1152));
  }

  [Fact]
  public void Fit_TakesTheSmallerOfTheTwoAxes()
  {
    // Wide enough for 3x, tall enough only for 1x.
    (int scale, Rectangle dest) = ScreenScaler.Fit(1024, 576, 4000, 600);
    scale.Should().Be(1);
    dest.Width.Should().Be(1024);
    dest.Height.Should().Be(576);
  }

  [Fact]
  public void Fit_WindowSmallerThanDesign_ClampsToOneAndCropsEvenly()
  {
    // The alternative is a fractional downscale, which costs every fourth pixel
    // of the whole picture rather than the edges of it.
    (int scale, Rectangle dest) = ScreenScaler.Fit(1024, 576, 800, 400);
    scale.Should().Be(1);
    dest.Width.Should().Be(1024);
    dest.Height.Should().Be(576);
    dest.X.Should().Be((800 - 1024) / 2);
    dest.Y.Should().Be((400 - 576) / 2);
    (dest.X + dest.Width / 2).Should().Be(400, "the crop is centred horizontally");
  }

  [Theory]
  [InlineData(320, 180, 1920, 1080)]
  [InlineData(1024, 576, 2560, 1440)]
  [InlineData(800, 600, 1367, 769)]
  [InlineData(640, 360, 1000, 1000)]
  [InlineData(1024, 576, 1025, 577)]
  [InlineData(100, 100, 201, 100)]
  public void Fit_NeverProducesAFractionalScale(int dw, int dh, int ww, int wh)
  {
    (int scale, Rectangle dest) = ScreenScaler.Fit(dw, dh, ww, wh);
    dest.Width.Should().Be(dw * scale);
    dest.Height.Should().Be(dh * scale);
    (dest.Width % dw).Should().Be(0);
    (dest.Height % dh).Should().Be(0);
  }

  [Theory]
  [InlineData(320, 180, 1920, 1080)]
  [InlineData(1024, 576, 2560, 1440)]
  [InlineData(800, 600, 1367, 769)]
  public void Fit_PicksTheLargestScaleThatFits(int dw, int dh, int ww, int wh)
  {
    // Maximality, stated directly: one more whole step must overflow an axis.
    // Without this the fit could quietly return 1 everywhere and every other
    // assertion here would still pass.
    (int scale, _) = ScreenScaler.Fit(dw, dh, ww, wh);
    bool nextStepFits = dw * (scale + 1) <= ww && dh * (scale + 1) <= wh;
    nextStepFits.Should().BeFalse();
  }

  [Fact]
  public void Fit_OddRemainder_PutsTheExtraPixelOnTheFarBar()
  {
    (_, Rectangle dest) = ScreenScaler.Fit(100, 100, 201, 100);
    dest.X.Should().Be(50);
    int rightBar = 201 - dest.Right;
    rightBar.Should().Be(51);
  }

  [Theory]
  [InlineData(0, 100)]
  [InlineData(100, 0)]
  [InlineData(-1, 100)]
  public void Fit_RejectsANonPositiveDesignSize(int dw, int dh)
  {
    Action fit = () => ScreenScaler.Fit(dw, dh, 800, 600);
    fit.Should().Throw<ArgumentOutOfRangeException>();
  }

  // ---- Window pixel to design pixel -----------------------------------------

  [Fact]
  public void WindowToVirtual_TopLeftOfThePicture_IsTheOrigin()
  {
    Vector2 v = ScreenScaler.WindowToVirtual(new Vector2(256, 144), 1024, 576, 2560, 1440);
    v.Should().Be(Vector2.Zero);
  }

  [Fact]
  public void WindowToVirtual_CentreOfThePicture_IsCentreOfTheDesign()
  {
    Vector2 v = ScreenScaler.WindowToVirtual(new Vector2(1280, 720), 1024, 576, 2560, 1440);
    v.X.Should().BeApproximately(512f, 1e-3f);
    v.Y.Should().BeApproximately(288f, 1e-3f);
  }

  [Fact]
  public void WindowToVirtual_UndoesTheScale()
  {
    // 2x: one design pixel is two window pixels.
    Vector2 a = ScreenScaler.WindowToVirtual(new Vector2(256, 144), 1024, 576, 2560, 1440);
    Vector2 b = ScreenScaler.WindowToVirtual(new Vector2(258, 144), 1024, 576, 2560, 1440);
    (b.X - a.X).Should().BeApproximately(1f, 1e-3f);
  }

  [Fact]
  public void TryWindowToVirtual_InsideThePicture_IsTrue()
  {
    bool ok = ScreenScaler.TryWindowToVirtual(new Vector2(1280, 720), 1024, 576, 2560, 1440, out Vector2 v);
    ok.Should().BeTrue();
    v.X.Should().BeApproximately(512f, 1e-3f);
  }

  [Theory]
  [InlineData(10, 720)]     // left bar
  [InlineData(2550, 720)]   // right bar
  [InlineData(1280, 10)]    // top bar
  [InlineData(1280, 1430)]  // bottom bar
  public void TryWindowToVirtual_InABar_IsFalse(float x, float y)
  {
    ScreenScaler.TryWindowToVirtual(new Vector2(x, y), 1024, 576, 2560, 1440, out _).Should().BeFalse();
  }

  [Fact]
  public void TryWindowToVirtual_TheLastPixelOfThePicture_IsStillInside()
  {
    (_, Rectangle dest) = ScreenScaler.Fit(1024, 576, 2560, 1440);
    ScreenScaler.TryWindowToVirtual(new Vector2(dest.Right - 1, dest.Bottom - 1), 1024, 576, 2560, 1440, out _)
      .Should().BeTrue();
  }

  [Theory]
  [InlineData(0f, 0f)]
  [InlineData(511f, 287f)]
  [InlineData(1023f, 575f)]
  public void DesignPixel_RoundtripsThroughTheWindow(float dx, float dy)
  {
    (int scale, Rectangle dest) = ScreenScaler.Fit(1024, 576, 2560, 1440);
    Vector2 window = new(dest.X + dx * scale, dest.Y + dy * scale);
    Vector2 back = ScreenScaler.WindowToVirtual(window, 1024, 576, 2560, 1440);
    back.X.Should().BeApproximately(dx, 1e-3f);
    back.Y.Should().BeApproximately(dy, 1e-3f);
  }

  [Fact]
  public void UnscaledWindow_IsTheIdentityMapping()
  {
    // The nine samples run windowed at their design size today, so this is the
    // path that must not change anything under them.
    Vector2 v = ScreenScaler.WindowToVirtual(new Vector2(613, 84), 1024, 576, 1024, 576);
    v.Should().Be(new Vector2(613, 84));
  }
}
