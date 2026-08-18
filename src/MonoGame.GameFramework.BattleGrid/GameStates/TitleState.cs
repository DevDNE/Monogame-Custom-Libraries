using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.BattleGrid.Components;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.BattleGrid.GameStates;

public class TitleState : TitleScreenState
{
  private readonly Action _onPlay;
  private readonly Action _onQuit;
  private readonly BattleArt _art;

  private float _elapsed;

  public TitleState(
    ServiceProvider sp, SpriteFont font, int vw, int vh, BattleArt art,
    Action onPlay, Action onQuit)
    : base(sp, font, vw, vh)
  {
    _art = art;
    _onPlay = onPlay;
    _onQuit = onQuit;
  }

  protected override Color BackgroundColor => new(20, 16, 40);
  protected override string TitleText => "BattleGrid";
  protected override string SubtitleText => "Grid-based dueling";
  protected override string HintText => "Click Play to begin, Esc to quit.";

  protected override NineSlice ButtonFrame => _art.Frame;
  protected override Texture2D BackgroundTile => _art.Backdrop;
  protected override int PixelScale => BattleArt.Scale;

  protected override Color TitleColor => new(150, 240, 255);
  protected override Color SubtitleColor => new(228, 72, 160);
  protected override Color HintColor => new(164, 168, 188);
  protected override Color HoverButtonFrameTint => new(255, 196, 64);

  // Buttons sit low so the duel above them has room.
  protected override int ButtonBlockStartY => ViewportHeight - 160;

  public override void Update(GameTime gameTime)
    => _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

  /// <summary>
  /// The two combatants facing each other on a strip of their own board.
  ///
  /// Drawn from the same textures the battle uses, deliberately: a title screen
  /// built from its own assets is one that stops matching the game the first
  /// time either changes, and nobody notices until someone plays it.
  /// </summary>
  protected override void DrawBackdrop(SpriteBatch spriteBatch, GameTime gameTime)
  {
    const int panel = 40 * BattleArt.Scale;
    int y = ViewportHeight / 2 - 40;
    int leftX = ViewportWidth / 2 - panel * 2 - 20;
    int rightX = ViewportWidth / 2 + panel + 20;

    for (int i = 0; i < 2; i++)
    {
      PixelDraw.Sprite(spriteBatch, _art.PlayerPanelTexture, leftX + i * panel, y, BattleArt.Scale);
      PixelDraw.Sprite(spriteBatch, _art.EnemyPanelTexture, rightX + i * panel, y, BattleArt.Scale);
    }

    // A slow two-frame bob. Whole pixels only — a sine in floats would put the
    // sprite on a half-pixel every other frame, which is the one thing point
    // sampling cannot hide.
    int bob = (int)(_elapsed * 2f) % 2 == 0 ? 0 : BattleArt.Scale;

    PixelDraw.Sprite(spriteBatch, _art.NaviTexture,
      leftX + panel / 2, y + panel - 48 * BattleArt.Scale, BattleArt.Scale);
    PixelDraw.Sprite(spriteBatch, _art.VirusTexture,
      rightX + panel / 2, y + panel - 48 * BattleArt.Scale - bob, BattleArt.Scale);
  }

  protected override IReadOnlyList<ButtonSpec> GetButtons() => new[]
  {
    new ButtonSpec("play", "Play", _onPlay),
    new ButtonSpec("quit", "Quit", _onQuit),
  };
}
