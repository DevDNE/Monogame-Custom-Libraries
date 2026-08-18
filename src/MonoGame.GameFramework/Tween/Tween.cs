using System;
using Microsoft.Xna.Framework;

namespace MonoGame.GameFramework.Tweening;

public class Tween<T>
{
  public T From { get; }
  public T To { get; }
  public float Duration { get; }
  public float Elapsed { get; private set; }
  public bool IsComplete => Elapsed >= Duration;
  public T Current { get; private set; }

  private readonly Func<T, T, float, T> _interpolator;
  private readonly Func<float, float> _easing;

  public Tween(T from, T to, float duration, Func<T, T, float, T> interpolator, Func<float, float> easing = null)
  {
    From = from;
    To = to;
    Duration = duration;
    _interpolator = interpolator ?? throw new ArgumentNullException(nameof(interpolator));
    _easing = easing ?? Easing.Linear;
    Current = duration <= 0f ? to : from;
  }

  public T Update(float deltaSeconds)
  {
    if (IsComplete) return Current;
    Elapsed = Math.Min(Elapsed + deltaSeconds, Duration);
    float t = Duration <= 0f ? 1f : Elapsed / Duration;
    Current = _interpolator(From, To, _easing(t));
    return Current;
  }

  /// <summary>
  /// Rewind to the start. Current is restored the same way the constructor
  /// sets it, so a tween that is reset and drawn before its next Update shows
  /// the *start* value rather than the end value it happened to be holding.
  /// </summary>
  public void Reset()
  {
    Elapsed = 0f;
    Current = Duration <= 0f ? To : From;
  }
}

public static class Tween
{
  public static Tween<float> Float(float from, float to, float duration, Func<float, float> easing = null)
    => new(from, to, duration, MathHelper.Lerp, easing);

  public static Tween<Vector2> Vec2(Vector2 from, Vector2 to, float duration, Func<float, float> easing = null)
    => new(from, to, duration, Vector2.Lerp, easing);

  public static Tween<Color> Color(Color from, Color to, float duration, Func<float, float> easing = null)
    => new(from, to, duration, Microsoft.Xna.Framework.Color.Lerp, easing);
}
