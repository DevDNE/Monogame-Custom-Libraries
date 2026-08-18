using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.VisualNovel;

/// <summary>
/// The room and its cast.
///
/// Portraits are the entire visual budget of a visual novel, which is why this
/// game's palette is the only one carrying a skin ramp. Everything else here —
/// one room, one frame — exists to give those busts somewhere to stand.
/// </summary>
public sealed record VisualNovelArt(
  Texture2D Room,
  Texture2D Portraits,
  NineSlice Frame)
{
  /// <summary>The room is authored 240x175 and drawn at 4x to fill 960x700 exactly.</summary>
  public const int RoomScale = 4;

  /// <summary>Busts are 48x64, drawn at 3x — big enough to read, small enough to leave the text room.</summary>
  public const int PortraitScale = 3;
  public const int PortraitWidth = 48;
  public const int PortraitHeight = 64;
  public const int FrameBorder = 4;

  /// <summary>
  /// Frame index per speaker. Portrait.None has no bust — the narrator is not a
  /// person in the room, and drawing a placeholder for them would imply one.
  /// </summary>
  public static Rectangle? RectFor(Portrait speaker)
  {
    int index = speaker switch
    {
      Portrait.Alex => 0,
      Portrait.Morgan => 1,
      _ => -1,
    };
    return index < 0 ? null : new Rectangle(index * PortraitWidth, 0, PortraitWidth, PortraitHeight);
  }

  public static VisualNovelArt Load(ContentManager content) => new(
    content.Load<Texture2D>("sprites/room"),
    content.Load<Texture2D>("sprites/portraits"),
    new NineSlice(content.Load<Texture2D>("sprites/ui-frame"), FrameBorder));
}
