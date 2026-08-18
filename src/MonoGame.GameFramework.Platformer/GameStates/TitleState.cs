using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Platformer.Entities;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Platformer.GameStates;

public class TitleState : TitleScreenState
{
  private readonly Action _onPlay;
  private readonly Action _onQuit;
  private readonly PlatformerArt _art;
  private readonly HeroSprites _hero;

  private float _elapsed;

  public TitleState(
    ServiceProvider sp, SpriteFont font, int vw, int vh,
    PlatformerArt art, HeroSprites hero, Action onPlay, Action onQuit)
    : base(sp, font, vw, vh)
  {
    _art = art;
    _hero = hero;
    _onPlay = onPlay;
    _onQuit = onQuit;
  }

  protected override Color BackgroundColor => new(62, 40, 66);
  protected override string TitleText => "Sunset Ruins";
  protected override string SubtitleText => "A side-scrolling platformer";
  protected override string HintText => "Click Play to begin, Esc to quit.";
  protected override int TitleY => 96;
  protected override int SubtitleY => 132;
  protected override int ButtonBlockStartY => 250;

  protected override NineSlice ButtonFrame => _art.Frame;
  protected override int PixelScale => 2;
  protected override Color TitleColor => new(255, 224, 184);
  protected override Color SubtitleColor => new(245, 195, 150);
  protected override Color HintColor => new(224, 167, 123);
  protected override Color HoverButtonFrameTint => new(255, 224, 184);

  public override void Update(GameTime gameTime)
    => _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

  /// <summary>
  /// The level's own back-to-front stack — sky, clouds, ruins, floor, hero —
  /// standing still. The clouds drift, so the screen is alive without the menu
  /// having to animate anything itself.
  /// </summary>
  protected override void DrawBackdrop(SpriteBatch spriteBatch, GameTime gameTime)
  {
    Rectangle viewport = new(0, 0, ViewportWidth, ViewportHeight);
    PixelDraw.Tile(spriteBatch, _art.Sky, viewport, PlatformerArt.Scale);

    PixelDraw.Tile(spriteBatch, _art.Clouds,
      new Rectangle(0, 150, ViewportWidth, _art.Clouds.Height), PlatformerArt.Scale,
      offset: new Point((int)(_elapsed * 8f), 0));

    int floorY = ViewportHeight - 96;
    PixelDraw.Tile(spriteBatch, _art.Ruins,
      new Rectangle(0, floorY - _art.Ruins.Height + 20, ViewportWidth, _art.Ruins.Height),
      PlatformerArt.Scale);

    PixelDraw.Tile(spriteBatch, _art.Ground,
      new Rectangle(0, floorY, ViewportWidth, 96), PlatformerArt.Scale);
    PixelDraw.Tile(spriteBatch, _art.GroundTop,
      new Rectangle(0, floorY, ViewportWidth, PlatformerArt.TileSize), PlatformerArt.Scale);

    // The hero blinks on the title screen, using the same two idle frames the
    // game does. Roughly every three seconds, for a fifth of one.
    bool blinking = _elapsed % 3f > 2.8f;
    Texture2D frame = _hero[blinking ? HeroFrame.IdleBlink : HeroFrame.IdleOpen];
    PixelDraw.Sprite(spriteBatch, frame, 120, floorY - frame.Height, PlatformerArt.Scale);

    PixelDraw.Sprite(spriteBatch, _art.Enemy,
      ViewportWidth - 200, floorY - _art.Enemy.Height, PlatformerArt.Scale);
    PixelDraw.Sprite(spriteBatch, _art.Goal,
      ViewportWidth - 120, floorY - _art.Goal.Height, PlatformerArt.Scale);
  }

  protected override IReadOnlyList<ButtonSpec> GetButtons() => new[]
  {
    new ButtonSpec("play", "Play", _onPlay),
    new ButtonSpec("quit", "Quit", _onQuit),
  };
}
