using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.TowerDefense.Entities;

namespace MonoGame.GameFramework.TowerDefense.GameStates;

public class TitleState : TitleScreenState
{
  private readonly Action _onPlay;
  private readonly Action _onQuit;

  private readonly TowerDefenseArt _art;
  private float _elapsed;

  public TitleState(ServiceProvider sp, SpriteFont font, int vw, int vh, TowerDefenseArt art, Action onPlay, Action onQuit)
    : base(sp, font, vw, vh)
  {
    _art = art;
    _onPlay = onPlay;
    _onQuit = onQuit;
  }

  protected override Color BackgroundColor => new(18, 22, 34);
  protected override string TitleText => "Tower Defense";
  protected override string SubtitleText => "Place towers, survive 3 waves";
  protected override string HintText => "Click an open cell to place a tower. Each tower costs 20 gold.";

  protected override Texture2D BackgroundTile => _art.Turf;
  protected override NineSlice ButtonFrame => _art.Frame;
  protected override int PixelScale => TowerDefenseArt.CellScale;
  protected override Color TitleColor => new(255, 255, 255);
  protected override Color SubtitleColor => new(206, 214, 232);
  protected override Color HintColor => new(196, 170, 128);
  protected override Color HoverButtonFrameTint => new(248, 208, 96);
  protected override int TitleY => 70;
  protected override int SubtitleY => 106;
  protected override int ButtonBlockStartY => 390;

  public override void Update(GameTime gameTime)
    => _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

  /// <summary>
  /// A stretch of the road with towers either side and a wave walking down it.
  /// The one screen that has to explain the game explains it by showing the
  /// loop, not by naming it.
  /// </summary>
  protected override void DrawBackdrop(SpriteBatch spriteBatch, GameTime gameTime)
  {
    const int cell = MapPath.CellSize;
    int roadY = 200;

    PixelDraw.Tile(spriteBatch, _art.Path,
      new Rectangle(0, roadY, ViewportWidth, cell), TowerDefenseArt.CellScale);

    for (int i = 1; i < 6; i += 2)
    {
      PixelDraw.Sprite(spriteBatch, _art.Tower,
        60 + i * cell * 2, roadY - cell + 6, TowerDefenseArt.EntityScale);
      PixelDraw.Sprite(spriteBatch, _art.Tower,
        60 + i * cell * 2 + cell, roadY + cell + 6, TowerDefenseArt.EntityScale);
    }

    // The wave marches left to right and wraps. Whole pixels, so nothing lands
    // on a fraction of one.
    int march = (int)(_elapsed * 30f);
    for (int i = 0; i < 5; i++)
    {
      int x = (march + i * 90) % (ViewportWidth + 60) - 30;
      PixelDraw.Sprite(spriteBatch, _art.Enemy, x, roadY + 8, TowerDefenseArt.EntityScale);
    }
  }

  protected override IReadOnlyList<ButtonSpec> GetButtons() => new[]
  {
    new ButtonSpec("play", "Play", _onPlay),
    new ButtonSpec("quit", "Quit", _onQuit),
  };
}
