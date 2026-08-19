using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Platformer.Entities;

public class Player
{
  public const float MoveSpeed = 280f;
  public const float GroundAcceleration = 1800f;
  public const float AirAcceleration = 1200f;
  public const float JumpVelocity = -700f;
  public const float JumpCutVelocity = -200f;
  public const float Gravity = 1800f;
  public const float MaxFallSpeed = 900f;
  public const float CoyoteTime = 0.15f;
  public const float JumpBufferTime = 0.15f;
  public const int Width = 32;
  public const int Height = 48;

  /// <summary>
  /// Authored frame size. Note this is NOT Height: the collision box is
  /// 32x48, the art is 32x32. Pixel art can't be stretched 32->48 to bridge
  /// that (non-uniform scaling produces uneven pixel sizes), so the frame is
  /// drawn at native size anchored to the box's feet. See SpriteDestination.
  /// </summary>
  public const int SpriteSize = HeroSprites.FrameSize;

  private const float MovingThreshold = 8f;
  private const float BlinkInterval = 3.2f;
  private const float BlinkDuration = 0.14f;

  /// <summary>
  /// World pixels of ground covered per walk frame.
  ///
  /// The cycle advances on distance travelled, not on a timer, so it stays
  /// locked to the hero rather than skating: pushing into a wall stops the
  /// legs, and the acceleration ramp winds them up instead of snapping to full
  /// speed. At MoveSpeed that works out near 13 frames a second, and it slows
  /// on its own through the deceleration slide.
  /// </summary>
  private const float WalkPixelsPerFrame = 22f;

  /// <summary>Ground covered by one full contact-passing-contact-passing loop.</summary>
  private const float WalkCyclePixels = WalkPixelsPerFrame * HeroSprites.WalkFrameCount;

  public Vector2 Position { get; set; }
  public Vector2 Velocity { get; set; }
  public bool IsGrounded { get; private set; }

  public Rectangle Bounds => new((int)Position.X, (int)Position.Y, Width, Height);

  /// <summary>
  /// Where the sprite is drawn, as distinct from where the player collides.
  /// Feet-anchored and horizontally centred on the collision box, at native
  /// resolution. The 16px of collision box above the sprite's head is real —
  /// the player clips ceilings slightly before the art touches them.
  /// </summary>
  public Rectangle SpriteDestination => new(
    (int)Position.X + (Width - SpriteSize) / 2,
    Bounds.Bottom - SpriteSize,
    SpriteSize,
    SpriteSize);

  private readonly Vector2 _spawnPosition;
  private float _coyoteTimer;
  private float _jumpBufferTimer;
  private float _blinkTimer;
  private float _walkDistance;
  private int _facing = 1;

  public Player(Vector2 spawnPosition)
  {
    _spawnPosition = spawnPosition;
    Respawn();
  }

  public void Respawn()
  {
    Position = _spawnPosition;
    Velocity = Vector2.Zero;
    IsGrounded = false;
    _coyoteTimer = 0f;
    _jumpBufferTimer = 0f;
    _blinkTimer = 0f;
    _walkDistance = 0f;
    _facing = 1;
  }

  public void Update(GameTime gameTime, IReadOnlyList<Platform> platforms, float inputX, bool jumpPressed, bool jumpHeld)
  {
    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

    // Facing latches on the last directional input so the hero keeps facing
    // the way they were moving after the key is released.
    if (inputX > 0f) _facing = 1;
    else if (inputX < 0f) _facing = -1;

    _blinkTimer += dt;
    if (_blinkTimer >= BlinkInterval + BlinkDuration) _blinkTimer = 0f;

    if (IsGrounded) _coyoteTimer = CoyoteTime;
    else _coyoteTimer = MathF.Max(0f, _coyoteTimer - dt);

    if (jumpPressed) _jumpBufferTimer = JumpBufferTime;
    else _jumpBufferTimer = MathF.Max(0f, _jumpBufferTimer - dt);

    float targetVx = inputX * MoveSpeed;
    float accel = IsGrounded ? GroundAcceleration : AirAcceleration;
    float newVx = ApproachTarget(Velocity.X, targetVx, accel * dt);

    float newVy = Velocity.Y + Gravity * dt;

    if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
    {
      newVy = JumpVelocity;
      _jumpBufferTimer = 0f;
      _coyoteTimer = 0f;
    }

    if (!jumpHeld && newVy < JumpCutVelocity)
    {
      newVy = JumpCutVelocity;
    }

    newVy = MathHelper.Clamp(newVy, -MaxFallSpeed, MaxFallSpeed);

    Velocity = new Vector2(newVx, newVy);

    float startX = Position.X;
    MoveX(Velocity.X * dt, platforms);
    MoveY(Velocity.Y * dt, platforms);

    // Measured after the move, so a hero pinned against a wall stops stepping
    // even though Velocity.X is still non-zero going into MoveX. Kept modulo a
    // whole cycle so a long walk can't drift the float into a range where the
    // division loses the frame.
    if (MathF.Abs(Velocity.X) > MovingThreshold)
      _walkDistance = (_walkDistance + MathF.Abs(Position.X - startX)) % WalkCyclePixels;
    else
      _walkDistance = 0f;

    if (_jumpBufferTimer > 0f && IsGrounded)
    {
      Velocity = new Vector2(Velocity.X, JumpVelocity);
      _jumpBufferTimer = 0f;
      _coyoteTimer = 0f;
      IsGrounded = false;
    }
  }

  private void MoveX(float delta, IReadOnlyList<Platform> platforms)
  {
    Position = new Vector2(Position.X + delta, Position.Y);
    foreach (Platform p in platforms)
    {
      if (!Bounds.Intersects(p.Bounds)) continue;
      if (delta > 0)
        Position = new Vector2(p.Bounds.Left - Width, Position.Y);
      else if (delta < 0)
        Position = new Vector2(p.Bounds.Right, Position.Y);
      Velocity = new Vector2(0, Velocity.Y);
    }
  }

  private void MoveY(float delta, IReadOnlyList<Platform> platforms)
  {
    Position = new Vector2(Position.X, Position.Y + delta);
    IsGrounded = false;
    foreach (Platform p in platforms)
    {
      if (!Bounds.Intersects(p.Bounds)) continue;
      if (delta > 0)
      {
        Position = new Vector2(Position.X, p.Bounds.Top - Height);
        IsGrounded = true;
      }
      else if (delta < 0)
      {
        Position = new Vector2(Position.X, p.Bounds.Bottom);
      }
      Velocity = new Vector2(Velocity.X, 0);
    }
  }

  private static float ApproachTarget(float current, float target, float step)
  {
    float diff = target - current;
    if (MathF.Abs(diff) <= step) return target;
    return current + MathF.Sign(diff) * step;
  }

  /// <summary>
  /// Maps player state to an authored frame. Pure and graphics-free, so it
  /// stays unit-testable without a GraphicsDevice — and it's the part worth
  /// watching for extraction: "entity state selects a frame" is the pattern a
  /// second sprite-using game would repeat, not the SpriteBatch.Draw call.
  ///
  /// Movement reads Velocity rather than raw input so the hero holds the walk
  /// frame through the deceleration slide and while airborne.
  /// </summary>
  public HeroFrame CurrentFrame
  {
    get
    {
      if (MathF.Abs(Velocity.X) > MovingThreshold) return HeroFrame.Walk;
      return _blinkTimer >= BlinkInterval ? HeroFrame.IdleBlink : HeroFrame.IdleOpen;
    }
  }

  /// <summary>
  /// Which frame of the walk strip is showing. Zero whenever the hero is still,
  /// so a step always begins on a contact pose rather than wherever the last
  /// one happened to stop.
  /// </summary>
  public int WalkPhase => (int)(_walkDistance / WalkPixelsPerFrame);

  /// <summary>Horizontal facing: 1 right, -1 left. The art is authored facing right.</summary>
  public int Facing => _facing;

  public void Draw(SpriteBatch spriteBatch, HeroSprites sprites)
  {
    HeroFrame frame = CurrentFrame;
    PixelDraw.Frame(
      spriteBatch,
      sprites[frame],
      HeroSprites.Source(frame, WalkPhase),
      SpriteDestination.X,
      SpriteDestination.Y,
      scale: 1,
      effects: _facing < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
  }
}
