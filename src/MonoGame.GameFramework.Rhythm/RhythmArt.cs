using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Rhythm;

/// <summary>
/// The board's textures.
///
/// The lane tile is mostly transparent by design — see its .pix header. That
/// makes the draw order load-bearing here in a way it is not in the other
/// samples: horizon, then lanes, then receptors, then notes. Swap any two and
/// the backdrop this palette exists for disappears behind the board.
/// </summary>
public sealed record RhythmArt(
  Texture2D Horizon,
  Texture2D Lane,
  Texture2D Notes,
  Texture2D Receptor,
  NineSlice Frame)
{
  /// <summary>Everything is authored at half size; 2x lands the horizon on 640x768 exactly.</summary>
  public const int Scale = 2;

  public const int FrameBorder = 4;

  /// <summary>Approaching, then struck. Near-complements — see the palette header.</summary>
  public static readonly Rectangle NoteApproach = new(0, 0, 50, 12);
  public static readonly Rectangle NoteStruck = new(50, 0, 50, 12);

  public static RhythmArt Load(ContentManager content) => new(
    content.Load<Texture2D>("sprites/horizon"),
    content.Load<Texture2D>("sprites/lane"),
    content.Load<Texture2D>("sprites/notes"),
    content.Load<Texture2D>("sprites/receptor"),
    new NineSlice(content.Load<Texture2D>("sprites/ui-frame"), FrameBorder));
}
