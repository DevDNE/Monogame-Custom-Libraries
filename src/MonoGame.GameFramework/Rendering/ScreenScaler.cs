using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.GameFramework.Rendering;

/// <summary>
/// Renders the game at a fixed design resolution and presents it at the largest
/// whole-number scale the window can hold, centred, with letterbox bars.
///
/// This closes the last gap in the whole-pixel rule. PixelDraw makes a
/// fractional scale unrepresentable at the call site, Camera2D snaps the view
/// matrix, check-sprites gates the sampler and the texture format -- and then
/// the finished frame was handed to the window at whatever scale the window
/// happened to be, which is the one scale in the pipeline nothing controlled.
/// A game whose backbuffer is 1024x576 shown on a 2560x1440 display is either
/// stretched by 2.5 or not scaled at all; neither is a thing this repo's style
/// bible permits anywhere else.
///
/// The trade is explicit: whole-number scaling means unused screen space at
/// most window sizes. Bars are the honest way to spend it.
/// </summary>
public sealed class ScreenScaler : IDisposable
{
  private readonly GraphicsDevice _device;

  public int DesignWidth { get; }
  public int DesignHeight { get; }

  /// <summary>Everything draws here; <see cref="Present"/> puts it on screen.</summary>
  public RenderTarget2D Target { get; }

  /// <summary>The design surface, as a rect. Handy for full-screen fills.</summary>
  public Rectangle Bounds => new(0, 0, DesignWidth, DesignHeight);

  /// <summary>Colour behind the bars.</summary>
  public Color LetterboxColor { get; set; } = Color.Black;

  public ScreenScaler(GraphicsDevice device, int designWidth, int designHeight)
  {
    if (designWidth <= 0) throw new ArgumentOutOfRangeException(nameof(designWidth), designWidth, "Design width must be positive.");
    if (designHeight <= 0) throw new ArgumentOutOfRangeException(nameof(designHeight), designHeight, "Design height must be positive.");

    _device = device ?? throw new ArgumentNullException(nameof(device));
    DesignWidth = designWidth;
    DesignHeight = designHeight;
    Target = new RenderTarget2D(device, designWidth, designHeight);
  }

  /// <summary>
  /// Largest whole-number scale that fits, and where the scaled surface lands.
  ///
  /// Pure, because it is the part that is easy to get wrong and impossible to
  /// eyeball: an off-by-one puts a one-pixel bar down one edge only, and a
  /// rounding error here reintroduces the fractional scale the whole type
  /// exists to prevent. Same treatment as PixelDraw.TileRects and
  /// NineSlice.SliceRects.
  ///
  /// A window smaller than the design size clamps to 1x and centres, so the
  /// surface is cropped evenly rather than shrunk to a fraction. Cropping loses
  /// the edges of the screen; a 0.75x scale loses every fourth pixel of all of
  /// it, everywhere, permanently.
  /// </summary>
  public static (int Scale, Rectangle Destination) Fit(int designWidth, int designHeight, int windowWidth, int windowHeight)
  {
    if (designWidth <= 0) throw new ArgumentOutOfRangeException(nameof(designWidth), designWidth, "Design width must be positive.");
    if (designHeight <= 0) throw new ArgumentOutOfRangeException(nameof(designHeight), designHeight, "Design height must be positive.");

    int scale = Math.Max(1, Math.Min(windowWidth / designWidth, windowHeight / designHeight));
    int width = designWidth * scale;
    int height = designHeight * scale;

    // Integer halving, so an odd remainder puts the extra pixel on the right
    // and bottom bars rather than splitting one across both.
    return (scale, new Rectangle((windowWidth - width) / 2, (windowHeight - height) / 2, width, height));
  }

  /// <summary>Current scale factor. Recomputed on read, so a resize needs no notification.</summary>
  public int Scale => Fit(DesignWidth, DesignHeight, BackBufferWidth, BackBufferHeight).Scale;

  /// <summary>Where the scaled surface sits inside the window, in window pixels.</summary>
  public Rectangle Destination => Fit(DesignWidth, DesignHeight, BackBufferWidth, BackBufferHeight).Destination;

  private int BackBufferWidth => _device.PresentationParameters.BackBufferWidth;
  private int BackBufferHeight => _device.PresentationParameters.BackBufferHeight;

  /// <summary>
  /// Let the player resize the window, and keep the backbuffer following it.
  ///
  /// AllowUserResizing on its own resizes the window and leaves the backbuffer
  /// where it was, so the driver stretches the old surface to the new window --
  /// by a fraction, which is the exact outcome this type exists to prevent. The
  /// re-entrancy guard is not optional: ApplyChanges resizes the window, which
  /// raises ClientSizeChanged again.
  /// </summary>
  public void AttachTo(GameWindow window, GraphicsDeviceManager graphics)
  {
    if (window == null) throw new ArgumentNullException(nameof(window));
    if (graphics == null) throw new ArgumentNullException(nameof(graphics));

    window.AllowUserResizing = true;
    window.ClientSizeChanged += (_, _) =>
    {
      if (_resizing) return;
      _resizing = true;
      try
      {
        graphics.PreferredBackBufferWidth = Math.Max(1, window.ClientBounds.Width);
        graphics.PreferredBackBufferHeight = Math.Max(1, window.ClientBounds.Height);
        graphics.ApplyChanges();
      }
      finally
      {
        _resizing = false;
      }
    };
  }

  private bool _resizing;

  /// <summary>Point everything at the design surface. Call before the first Clear of the frame.</summary>
  public void BeginDraw() => _device.SetRenderTarget(Target);

  /// <summary>
  /// Put the design surface on screen: bars, then the frame at a whole-number
  /// scale, point-sampled.
  /// </summary>
  public void Present(SpriteBatch spriteBatch)
  {
    _device.SetRenderTarget(null);
    _device.Clear(LetterboxColor);
    spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    spriteBatch.Draw(Target, Destination, Color.White);
    spriteBatch.End();
  }

  /// <summary>
  /// Window pixel to design pixel. Every mouse coordinate has to come through
  /// here once the game renders at a different size than the window, or clicks
  /// land somewhere the player did not aim -- which is why MouseManager takes
  /// this as a transform rather than leaving it to nine call sites.
  /// </summary>
  public Vector2 WindowToVirtual(Vector2 window)
    => WindowToVirtual(window, DesignWidth, DesignHeight, BackBufferWidth, BackBufferHeight);

  /// <summary>
  /// The mapping, without a device. Split out for the same reason
  /// <see cref="Fit"/> is: this is the arithmetic that decides whether a click
  /// lands where the player aimed, and it must be assertable without a
  /// GraphicsDevice a test cannot create.
  /// </summary>
  public static Vector2 WindowToVirtual(Vector2 window, int designWidth, int designHeight, int windowWidth, int windowHeight)
  {
    (int scale, Rectangle destination) = Fit(designWidth, designHeight, windowWidth, windowHeight);
    return new Vector2((window.X - destination.X) / scale, (window.Y - destination.Y) / scale);
  }

  /// <summary>
  /// As <see cref="WindowToVirtual"/>, but false when the point is in a bar
  /// rather than on the game surface. Use it to reject clicks that landed
  /// outside the picture.
  /// </summary>
  public bool TryWindowToVirtual(Vector2 window, out Vector2 virtualPosition)
    => TryWindowToVirtual(window, DesignWidth, DesignHeight, BackBufferWidth, BackBufferHeight, out virtualPosition);

  public static bool TryWindowToVirtual(
    Vector2 window, int designWidth, int designHeight, int windowWidth, int windowHeight, out Vector2 virtualPosition)
  {
    virtualPosition = WindowToVirtual(window, designWidth, designHeight, windowWidth, windowHeight);
    return virtualPosition.X >= 0 && virtualPosition.Y >= 0
        && virtualPosition.X < designWidth && virtualPosition.Y < designHeight;
  }

  /// <summary>
  /// Swap between a windowed design-sized surface and borderless full screen.
  ///
  /// Borderless at the desktop resolution rather than an exclusive mode change:
  /// the scaler is already picking a whole-number scale for whatever it is
  /// given, so there is nothing to gain from making the display switch modes,
  /// and a mode switch is the part that strands a player in a black screen.
  /// </summary>
  public void ToggleFullScreen(GraphicsDeviceManager graphics)
  {
    if (graphics == null) throw new ArgumentNullException(nameof(graphics));

    if (graphics.IsFullScreen)
    {
      graphics.IsFullScreen = false;
      graphics.PreferredBackBufferWidth = DesignWidth;
      graphics.PreferredBackBufferHeight = DesignHeight;
    }
    else
    {
      graphics.HardwareModeSwitch = false;
      graphics.IsFullScreen = true;
      graphics.PreferredBackBufferWidth = _device.Adapter.CurrentDisplayMode.Width;
      graphics.PreferredBackBufferHeight = _device.Adapter.CurrentDisplayMode.Height;
    }

    graphics.ApplyChanges();
  }

  public void Dispose() => Target?.Dispose();
}
