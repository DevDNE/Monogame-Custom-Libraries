using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.GameFramework.UI;

/// <summary>
/// Fixed-size scrolling text panel. Oldest messages fade; newest are fully
/// opaque. Draw() lays lines out top-down from <paramref name="origin"/>:
/// oldest at origin.Y, newest at origin.Y + (Count-1)*lineHeight. Consumers
/// that want bottom-anchored placement compute origin.Y accordingly.
/// </summary>
public sealed class LogBox
{
  private readonly Queue<string> _lines = new();

  public int MaxLines { get; }
  public float FadeStart { get; }
  public float FadeStep { get; }
  public Color BaseColor { get; }

  public int Count => _lines.Count;
  public IReadOnlyCollection<string> Lines => _lines;

  public LogBox(int maxLines = 6, float fadeStart = 0.55f, float fadeStep = 0.08f, Color? baseColor = null)
  {
    MaxLines = maxLines;
    FadeStart = fadeStart;
    FadeStep = fadeStep;
    BaseColor = baseColor ?? new Color(220, 220, 230);
  }

  public void Add(string message)
  {
    _lines.Enqueue(message);
    while (_lines.Count > MaxLines) _lines.Dequeue();
  }

  public void Clear() => _lines.Clear();

  /// <summary>
  /// Opacity multiplier for the line at <paramref name="index"/>, counting from
  /// the oldest. The newest line is always 1.0 and older lines step down by
  /// <see cref="FadeStep"/> to a <see cref="FadeStart"/> floor — keyed to
  /// distance from the newest, so the gradient does not shift as the box fills.
  /// Exposed because it is the whole visual contract and Draw needs a SpriteFont.
  /// </summary>
  public float FadeFor(int index)
  {
    float fade = 1f - FadeStep * (_lines.Count - 1 - index);
    if (fade < FadeStart) fade = FadeStart;
    return fade > 1f ? 1f : fade;
  }

  public void Draw(SpriteBatch spriteBatch, SpriteFont font, Vector2 origin, float lineHeight = 22f)
  {
    int i = 0;
    foreach (string msg in _lines)
    {
      float fade = FadeFor(i);
      Color c = new((byte)(BaseColor.R * fade), (byte)(BaseColor.G * fade), (byte)(BaseColor.B * fade));
      spriteBatch.DrawString(font, msg, new Vector2(origin.X, origin.Y + i * lineHeight), c);
      i++;
    }
  }
}
