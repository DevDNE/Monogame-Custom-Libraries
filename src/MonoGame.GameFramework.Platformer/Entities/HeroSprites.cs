using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.GameFramework.Platformer.Entities;

/// <summary>Which authored frame the hero should show this instant.</summary>
public enum HeroFrame
{
  IdleOpen,
  IdleBlink,
  WalkLeft,
  WalkRight,
}

/// <summary>
/// The hero's four authored frames, held as individual textures.
///
/// Deliberately game-side, not a library type: this is the first sample to
/// render a real sprite instead of a coloured rectangle, so there is exactly
/// one consumer. Per FINDINGS §8 Tier D, extraction waits for a second
/// consumer in a different genre — that's what tells us whether the library
/// wants an atlas, a frame-cycling animator, or nothing at all.
///
/// Note what the art does and doesn't demand: walking is one frame per
/// direction, so nothing here cycles frames over time. The blink is the only
/// time-driven change. A real walk cycle would be the thing that justifies a
/// library animator; this art does not.
/// </summary>
public sealed record HeroSprites(
  Texture2D IdleOpen,
  Texture2D IdleBlink,
  Texture2D WalkLeft,
  Texture2D WalkRight)
{
  /// <summary>Authored size of every frame. See Player.SpriteSize for why this matters.</summary>
  public const int FrameSize = 32;

  public Texture2D this[HeroFrame frame] => frame switch
  {
    HeroFrame.IdleBlink => IdleBlink,
    HeroFrame.WalkLeft => WalkLeft,
    HeroFrame.WalkRight => WalkRight,
    _ => IdleOpen,
  };

  public static HeroSprites Load(ContentManager content) => new(
    content.Load<Texture2D>("sprites/hero-f1-idle-open"),
    content.Load<Texture2D>("sprites/hero-f2-idle-blink"),
    content.Load<Texture2D>("sprites/hero-f3-walk-left"),
    content.Load<Texture2D>("sprites/hero-f4-walk-right"));
}
