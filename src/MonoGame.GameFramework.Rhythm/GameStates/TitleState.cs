using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Rhythm.GameStates;

public class TitleState : TitleScreenState
{
  private readonly Action _onPlay;
  private readonly Action _onQuit;

  private readonly RhythmArt _art;
  private float _elapsed;

  public TitleState(ServiceProvider sp, SpriteFont font, int vw, int vh, RhythmArt art, Action onPlay, Action onQuit)
    : base(sp, font, vw, vh)
  {
    _art = art;
    _onPlay = onPlay;
    _onQuit = onQuit;
  }

  protected override Color BackgroundColor => new(18, 20, 32);
  protected override string TitleText => "Beat Lanes";
  protected override string SubtitleText => "4-lane rhythm — hit notes as they cross the line";
  protected override string HintText => "Keys: D  F  J  K    Esc to quit";
  protected override int SubtitleY => 160;

  protected override NineSlice ButtonFrame => _art.Frame;
  protected override int PixelScale => RhythmArt.Scale;
  protected override Color TitleColor => new(255, 176, 222);
  protected override Color SubtitleColor => new(196, 252, 254);
  protected override Color HintColor => new(148, 152, 184);
  protected override Color HoverButtonFrameTint => new(196, 252, 254);
  protected override int TitleY => 120;
  protected override int ButtonBlockStartY => 420;

  public override void Update(GameTime gameTime)
    => _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

  /// <summary>
  /// The playfield itself, with notes falling on a loop. Nothing is playable —
  /// it is the game's own draw order (horizon, lanes, receptors, notes) run as
  /// an attract mode, so the title screen cannot drift from what the game
  /// looks like.
  /// </summary>
  protected override void DrawBackdrop(SpriteBatch spriteBatch, GameTime gameTime)
  {
    PixelDraw.Sprite(spriteBatch, _art.Horizon, 0, 0, RhythmArt.Scale);

    const int laneWidth = 100;
    int boardX = (ViewportWidth - laneWidth * 4) / 2;
    int targetY = ViewportHeight - 120;

    for (int lane = 0; lane < 4; lane++)
    {
      PixelDraw.Tile(spriteBatch, _art.Lane,
        new Rectangle(boardX + lane * laneWidth, 0, laneWidth, ViewportHeight), RhythmArt.Scale);
      PixelDraw.Sprite(spriteBatch, _art.Receptor,
        boardX + lane * laneWidth, targetY - _art.Receptor.Height * RhythmArt.Scale / 2,
        RhythmArt.Scale);
    }

    // Four notes on staggered loops. Whole pixels only.
    int[] phase = { 0, 190, 90, 300 };
    for (int lane = 0; lane < 4; lane++)
    {
      int y = ((int)(_elapsed * 240f) + phase[lane]) % (targetY + 120) - 40;
      Rectangle source = System.Math.Abs(y - targetY) < 24
        ? RhythmArt.NoteStruck
        : RhythmArt.NoteApproach;
      PixelDraw.Frame(spriteBatch, _art.Notes, source, boardX + lane * laneWidth, y, RhythmArt.Scale);
    }
  }

  protected override IReadOnlyList<ButtonSpec> GetButtons() => new[]
  {
    new ButtonSpec("play", "Play", _onPlay),
    new ButtonSpec("quit", "Quit", _onQuit),
  };
}
