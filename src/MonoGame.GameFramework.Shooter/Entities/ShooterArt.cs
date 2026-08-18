using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Shooter.Entities;

/// <summary>
/// Every texture the arena draws.
///
/// The two combatants are the same size and the same shape; only hue tells them
/// apart. That is a deliberate constraint of the palette (see its header) and
/// it is why nothing here needs a facing, a rotation, or a frame index — the
/// whole game is legible from the tint alone, which is all a player has time
/// for when six drones arrive at once.
/// </summary>
public sealed record ShooterArt(
  Texture2D Floor,
  Texture2D Hazard,
  Texture2D Player,
  Texture2D Enemy,
  Texture2D Shot,
  Texture2D Burst,
  NineSlice Frame)
{
  /// <summary>The floor is authored 40x40 and drawn at 2x, landing on the arena's 80px grid.</summary>
  public const int FloorScale = 2;

  /// <summary>Entities are authored at the size of their own hitboxes and drawn 1:1.</summary>
  public const int EntityScale = 1;

  public const int HazardScale = 2;
  public const int FrameBorder = 4;

  public static ShooterArt Load(ContentManager content) => new(
    content.Load<Texture2D>("sprites/floor"),
    content.Load<Texture2D>("sprites/hazard"),
    content.Load<Texture2D>("sprites/player"),
    content.Load<Texture2D>("sprites/enemy"),
    content.Load<Texture2D>("sprites/shot"),
    content.Load<Texture2D>("sprites/burst"),
    new NineSlice(content.Load<Texture2D>("sprites/ui-frame"), FrameBorder));
}
