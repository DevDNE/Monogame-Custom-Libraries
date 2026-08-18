using System;
using Microsoft.Xna.Framework;

namespace MonoGame.GameFramework.Rendering;

public class Camera2D
{
  private Vector2 _position = Vector2.Zero;
  private float _zoom = 1f;
  private float _rotation = 0f;
  private Vector2 _viewportSize;
  private Vector2 _shakeOffset;

  public Vector2 Position
  {
    get => _position;
    set { if (_position != value) { _position = value; _viewDirty = true; } }
  }

  /// <summary>
  /// Magnification. While <see cref="PixelSnap"/> is on this must be a whole
  /// number: a 1.5x zoom makes some source pixels one screen-pixel wide and
  /// others two, which is the same artefact <see cref="PixelDraw"/> exists to
  /// make unrepresentable at the call site. A game that means a smooth zoom
  /// turns PixelSnap off first and owns that decision explicitly.
  /// </summary>
  public float Zoom
  {
    get => _zoom;
    set
    {
      if (_pixelSnap) RequireWholeZoom(value);
      if (_zoom != value) { _zoom = value; _viewDirty = true; }
    }
  }

  public float Rotation
  {
    get => _rotation;
    set { if (_rotation != value) { _rotation = value; _viewDirty = true; } }
  }

  public Vector2 ViewportSize
  {
    get => _viewportSize;
    set { if (_viewportSize != value) { _viewportSize = value; _viewDirty = true; } }
  }

  public float FollowLerp { get; set; } = 0.1f;
  public Vector2? Target { get; set; }

  /// <summary>
  /// Round the view translation to whole screen pixels. On by default.
  ///
  /// PixelDraw makes a fractional scale unrepresentable inside the world, and
  /// then the whole batch goes through this matrix. A camera following a target
  /// through a Lerp holds a fractional position on almost every frame, so with
  /// PointClamp the sprite edges wobble between one and two screen pixels as it
  /// moves -- exactly the artefact PixelDraw was written to prevent, one
  /// transform later. Platformer's DrawParallax already casts its scroll
  /// offsets to int and says why in a comment; the world layer thirty lines
  /// below it did not, so the background snapped and the foreground did not.
  ///
  /// The snap is applied to the composed translation rather than to Position,
  /// so the camera still moves smoothly and sub-pixel motion accumulates
  /// instead of being quantised away. It holds under rotation and zoom too,
  /// because it is the final screen offset that is rounded.
  ///
  /// Default is on because every sample in this repo is pixel art and both live
  /// consumers were wrong without it. A game drawing smooth-scaled art turns it
  /// off.
  /// </summary>
  public bool PixelSnap
  {
    get => _pixelSnap;
    set
    {
      if (value) RequireWholeZoom(_zoom);
      if (_pixelSnap != value) { _pixelSnap = value; _viewDirty = true; }
    }
  }

  private float _shakeTimeRemaining;
  private float _shakeIntensity;
  private bool _pixelSnap = true;

  // Injectable so shake is assertable. An unseeded Random makes the one part of
  // this class with observable randomness the one part no test can pin down.
  private readonly Random _random;

  private Matrix _viewMatrix;
  private bool _viewDirty = true;

  /// <param name="random">Source for <see cref="Shake"/>. Pass a seeded Random
  /// to make shake reproducible in a test; omit it in a game.</param>
  public Camera2D(Vector2 viewportSize, Random random = null)
  {
    ViewportSize = viewportSize;
    _random = random ?? new Random();
  }

  private static void RequireWholeZoom(float zoom)
  {
    if (zoom != MathF.Truncate(zoom) || zoom < 1f)
      throw new ArgumentOutOfRangeException(
        nameof(Zoom), zoom,
        "PixelSnap requires a whole-number zoom of at least 1. Set PixelSnap = false first for a smooth zoom.");
  }

  public void Shake(float duration, float intensity)
  {
    _shakeTimeRemaining = duration;
    _shakeIntensity = intensity;
  }

  public void Update(GameTime gameTime)
  {
    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

    if (Target.HasValue)
    {
      // Frame-rate independent follow. A fixed Lerp fraction per *frame* closes
      // on the target at a different speed at 30fps than at 144fps; FollowLerp
      // is therefore read as "this fraction per 1/60s" and converted to the
      // equivalent fraction for the frame actually elapsed. At the default
      // fixed 60Hz timestep dt*60 == 1 and this is arithmetically identical to
      // what it replaced, so no sample's camera feel changes.
      float per60 = MathHelper.Clamp(FollowLerp, 0f, 1f);
      float alpha = dt > 0f ? 1f - MathF.Pow(1f - per60, dt * 60f) : 0f;
      Position = Vector2.Lerp(Position, Target.Value, alpha);
    }

    if (_shakeTimeRemaining > 0f)
    {
      _shakeTimeRemaining -= dt;
      float x = ((float)_random.NextDouble() * 2f - 1f) * _shakeIntensity;
      float y = ((float)_random.NextDouble() * 2f - 1f) * _shakeIntensity;
      Vector2 newShake = _shakeTimeRemaining <= 0f ? Vector2.Zero : new Vector2(x, y);
      if (newShake != _shakeOffset) { _shakeOffset = newShake; _viewDirty = true; }
    }
    else if (_shakeOffset != Vector2.Zero)
    {
      _shakeOffset = Vector2.Zero;
      _viewDirty = true;
    }
  }

  public Matrix GetViewMatrix()
  {
    if (_viewDirty)
    {
      Vector2 center = _viewportSize * 0.5f;
      _viewMatrix = Matrix.CreateTranslation(new Vector3(-(_position + _shakeOffset), 0f))
                    * Matrix.CreateRotationZ(_rotation)
                    * Matrix.CreateScale(_zoom, _zoom, 1f)
                    * Matrix.CreateTranslation(new Vector3(center, 0f));

      // Round the composed offset, not the inputs. Everything above it -- the
      // camera position, the shake, an odd viewport whose half is a half pixel
      // -- lands in these two cells, so one rounding here covers all of them,
      // and ScreenToWorld inherits it for free by inverting the same matrix.
      if (_pixelSnap)
      {
        _viewMatrix.M41 = MathF.Round(_viewMatrix.M41);
        _viewMatrix.M42 = MathF.Round(_viewMatrix.M42);
      }

      _viewDirty = false;
    }
    return _viewMatrix;
  }

  public Vector2 ScreenToWorld(Vector2 screen)
  {
    Matrix invert = Matrix.Invert(GetViewMatrix());
    return Vector2.Transform(screen, invert);
  }

  public Vector2 WorldToScreen(Vector2 world)
  {
    return Vector2.Transform(world, GetViewMatrix());
  }
}
