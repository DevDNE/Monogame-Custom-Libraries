using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.Shooter.Entities;

namespace MonoGame.GameFramework.Shooter.GameStates;

public class TitleState : TitleScreenState
{
  private readonly Action _onPlay;
  private readonly Action _onQuit;
  private readonly ShooterArt _art;

  private float _elapsed;

  public TitleState(
    ServiceProvider sp, SpriteFont font, int vw, int vh, ShooterArt art,
    Action onPlay, Action onQuit)
    : base(sp, font, vw, vh)
  {
    _art = art;
    _onPlay = onPlay;
    _onQuit = onQuit;
  }

  protected override Color BackgroundColor => new(24, 20, 22);
  protected override string TitleText => "Rust Belt";
  protected override string SubtitleText => "Twin-stick arena survival";
  protected override string HintText => "WASD move   Mouse aim   Click / Space fire";
  protected override int TitleY => 90;
  protected override int SubtitleY => 126;
  protected override int ButtonBlockStartY => 380;

  protected override Texture2D BackgroundTile => _art.Floor;
  protected override NineSlice ButtonFrame => _art.Frame;
  protected override int PixelScale => 2;

  protected override Color TitleColor => new(255, 214, 120);
  protected override Color SubtitleColor => new(236, 158, 56);
  protected override Color HintColor => new(132, 156, 180);
  protected override Color HoverButtonFrameTint => new(255, 214, 120);

  public override void Update(GameTime gameTime)
    => _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

  /// <summary>
  /// The fight in miniature: the walker in the middle, four drones closing on
  /// it, and a hazard band top and bottom. It restates the one rule the game
  /// runs on — cool is you, warm is coming for you — before the player has
  /// pressed anything.
  /// </summary>
  protected override void DrawBackdrop(SpriteBatch spriteBatch, GameTime gameTime)
  {
    const int band = 16 * 2;
    PixelDraw.Tile(spriteBatch, _art.Hazard, new Rectangle(0, 0, ViewportWidth, band), 2);
    PixelDraw.Tile(spriteBatch, _art.Hazard,
      new Rectangle(0, ViewportHeight - band, ViewportWidth, band), 2);

    int cx = ViewportWidth / 2;
    int cy = 250;
    PixelDraw.Sprite(spriteBatch, _art.Player, cx - 28, cy - 28, 2);

    // The drones creep in and reset — whole pixels only, so the approach never
    // lands a sprite on a half-pixel.
    int closing = (int)(_elapsed * 10f) % 70;
    (int dx, int dy)[] approach = { (-1, -1), (1, -1), (-1, 1), (1, 1) };
    foreach ((int dx, int dy) in approach)
    {
      int gap = 200 - closing;
      PixelDraw.Sprite(spriteBatch, _art.Enemy,
        cx + dx * gap - 28, cy + dy * (gap * 2 / 3) - 28, 2);
    }
  }

  protected override IReadOnlyList<ButtonSpec> GetButtons() => new[]
  {
    new ButtonSpec("play", "Play", _onPlay),
    new ButtonSpec("quit", "Quit", _onQuit),
  };
}
