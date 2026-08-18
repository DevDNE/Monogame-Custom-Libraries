using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Puzzle.GameStates;

public class TitleState : TitleScreenState
{
  private readonly Action _onPlay;
  private readonly Action _onQuit;

  private readonly PuzzleArt _art;
  private float _elapsed;

  public TitleState(ServiceProvider sp, SpriteFont font, int vw, int vh, PuzzleArt art, Action onPlay, Action onQuit)
    : base(sp, font, vw, vh)
  {
    _art = art;
    _onPlay = onPlay;
    _onQuit = onQuit;
  }

  protected override Color BackgroundColor => new(26, 38, 58);
  protected override string TitleText => "Gem Match";
  protected override string SubtitleText => "Swap adjacent gems to match 3+";
  protected override string HintText => "Click a gem, then click an adjacent one";

  protected override Texture2D BackgroundTile => _art.Background;
  protected override NineSlice ButtonFrame => _art.Frame;
  protected override int PixelScale => PuzzleArt.Scale;
  protected override Color TitleColor => new(255, 255, 255);
  protected override Color SubtitleColor => new(150, 226, 255);
  protected override Color HintColor => new(116, 156, 196);
  protected override Color HoverButtonFrameTint => new(255, 248, 164);
  protected override int TitleY => 80;
  protected override int SubtitleY => 116;
  protected override int ButtonBlockStartY => 400;

  public override void Update(GameTime gameTime)
    => _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

  /// <summary>
  /// A short row of the real board with the real gems, cycling. It shows the
  /// player the whole vocabulary of the game — six shapes, six colours — before
  /// they have clicked anything.
  /// </summary>
  protected override void DrawBackdrop(SpriteBatch spriteBatch, GameTime gameTime)
  {
    const int cell = PuzzleArt.GemSize * PuzzleArt.Scale;
    int count = 5;
    int x0 = ViewportWidth / 2 - count * cell / 2;
    int y = 190;
    int step = (int)(_elapsed * 1.5f);

    for (int i = 0; i < count; i++)
    {
      int x = x0 + i * cell;
      PixelDraw.Sprite(spriteBatch, _art.Cell, x, y, PuzzleArt.Scale);
      // 1..5 — never Gem.Empty, which is the blank frame.
      Board.Gem gem = (Board.Gem)((i + step) % 5 + 1);
      PixelDraw.Frame(spriteBatch, _art.Gems, _art.FrameFor(gem), x, y, PuzzleArt.Scale);
    }
  }

  protected override IReadOnlyList<ButtonSpec> GetButtons() => new[]
  {
    new ButtonSpec("play", "Play", _onPlay),
    new ButtonSpec("quit", "Quit", _onQuit),
  };
}
