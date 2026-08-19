using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.GameFramework.Platformer.Entities;

/// <summary>Which authored pose the hero should show this instant.</summary>
public enum HeroFrame
{
  IdleOpen,
  IdleBlink,
  Walk,
}

/// <summary>
/// The hero's poses: two single-frame idles, and a walk held as a horizontal
/// strip of <see cref="WalkFrameCount"/> frames.
///
/// Deliberately game-side, not a library type. Platformer is now the first
/// sample to cycle frames over time, but it is still the *only* one — the
/// nine games otherwise index sheets by an enum and step whole pixels. Per
/// FINDINGS §8 Tier D and the standing rule in CLAUDE.md, SpriteSheet.Animated
/// stays deleted until a *second* consumer shows what the shared shape should
/// be. Cycling a strip is eight lines here; guessing the abstraction from one
/// consumer is how the old Core.Entity got built and then deleted.
///
/// There is one facing, not two. mirror(hero-f3-walk-left) was byte-identical
/// to hero-f4-walk-right across all 1024 pixels, so the second facing was a
/// redundant texture rather than authored art; Player flips at draw time.
/// </summary>
public sealed record HeroSprites(
  Texture2D IdleOpen,
  Texture2D IdleBlink,
  Texture2D WalkStrip)
{
  /// <summary>Authored size of every frame. See Player.SpriteSize for why this matters.</summary>
  public const int FrameSize = 32;

  /// <summary>Frames on the walk strip: contact, passing, contact, passing.</summary>
  public const int WalkFrameCount = 4;

  public Texture2D this[HeroFrame frame] => frame switch
  {
    HeroFrame.IdleBlink => IdleBlink,
    HeroFrame.Walk => WalkStrip,
    _ => IdleOpen,
  };

  /// <summary>
  /// Source rectangle for one frame of a pose. Single-frame poses ignore the
  /// phase; the walk wraps it, so a caller can hand over a monotonically
  /// increasing step count without doing the modulo itself — which is where
  /// an off-by-one turns the loop seam into a stutter.
  /// </summary>
  public static Rectangle Source(HeroFrame frame, int phase) => frame == HeroFrame.Walk
    ? new Rectangle(WrapPhase(phase) * FrameSize, 0, FrameSize, FrameSize)
    : new Rectangle(0, 0, FrameSize, FrameSize);

  /// <summary>Wraps a step count onto the strip, negatives included.</summary>
  public static int WrapPhase(int phase) => ((phase % WalkFrameCount) + WalkFrameCount) % WalkFrameCount;

  public static HeroSprites Load(ContentManager content) => new(
    content.Load<Texture2D>("sprites/hero-f1-idle-open"),
    content.Load<Texture2D>("sprites/hero-f2-idle-blink"),
    content.Load<Texture2D>("sprites/hero-walk"));
}
