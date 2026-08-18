using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.BattleGrid.Components;

/// <summary>
/// Every texture this game draws, plus the numbers that describe them.
///
/// Source rectangles live here rather than at each call site because a source
/// rectangle is a fact about the art file, not about the entity using it. Six
/// shot frames on one sheet means six places would otherwise carry a
/// hard-coded 16 that all have to change together.
///
/// States take an instance; entities that already receive a ContentManager in
/// their own LoadContent just ask it for the texture directly. Both are fine —
/// the ContentManager caches by asset name, so there is exactly one Texture2D
/// per file either way.
/// </summary>
public sealed class BattleArt
{
  /// <summary>
  /// Every sprite in this game is authored at half its on-screen size. Whole
  /// numbers only (STYLE.md), and 2 is what makes a 40x40 board tile land
  /// exactly on the existing 80px grid.
  /// </summary>
  public const int Scale = 2;

  public const string BackdropAsset = "sprites/bg-tile";
  public const string PlayerPanel = "sprites/panel-player";
  public const string EnemyPanel = "sprites/panel-enemy";
  public const string Navi = "sprites/navi";
  public const string Virus = "sprites/virus";
  public const string Shots = "sprites/shots";
  public const string HitSparkAsset = "sprites/hit-spark";
  public const string UiFrameAsset = "sprites/ui-frame";

  /// <summary>
  /// Frames on the shot sheet. Top row is the player's, bottom row is the
  /// virus's — the same three drawings with the hue swapped, so "whose shot is
  /// that" is answered by colour alone and never by shape.
  /// </summary>
  public static readonly Rectangle BusterShot = new(0, 0, 16, 16);
  public static readonly Rectangle CannonShot = new(16, 0, 16, 16);
  public static readonly Rectangle WideShot = new(32, 0, 16, 16);
  public static readonly Rectangle EnemyShot = new(0, 16, 16, 16);
  public static readonly Rectangle EnemyWideShot = new(32, 16, 16, 16);

  /// <summary>Corner size of the UI frame — the part a nine-slice never scales.</summary>
  public const int FrameBorder = 4;

  public Texture2D Backdrop { get; private init; }
  public Texture2D ShotSheet { get; private init; }
  public Texture2D HitSpark { get; private init; }
  public Texture2D NaviTexture { get; private init; }
  public Texture2D VirusTexture { get; private init; }
  public Texture2D PlayerPanelTexture { get; private init; }
  public Texture2D EnemyPanelTexture { get; private init; }
  public NineSlice Frame { get; private init; }

  public static BattleArt Load(ContentManager content) => new()
  {
    Backdrop = content.Load<Texture2D>(BackdropAsset),
    ShotSheet = content.Load<Texture2D>(Shots),
    HitSpark = content.Load<Texture2D>(HitSparkAsset),
    NaviTexture = content.Load<Texture2D>(Navi),
    VirusTexture = content.Load<Texture2D>(Virus),
    PlayerPanelTexture = content.Load<Texture2D>(PlayerPanel),
    EnemyPanelTexture = content.Load<Texture2D>(EnemyPanel),
    Frame = new NineSlice(content.Load<Texture2D>(UiFrameAsset), FrameBorder),
  };
}
