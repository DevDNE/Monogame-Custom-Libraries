using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.KnockItOff.GameStates;

public class TitleState : TitleScreenState
{
  private readonly Action _onPlay;
  private readonly Action _onQuit;

  private readonly NineSlice _frame;

  public TitleState(
    ServiceProvider sp, SpriteFont font, NineSlice frame, int vw, int vh,
    Action onPlay, Action onQuit)
    : base(sp, font, vw, vh)
  {
    _frame = frame;
    _onPlay = onPlay;
    _onQuit = onQuit;
  }

  protected override Color BackgroundColor => new(18, 22, 34);
  protected override string TitleText => "KnockItOff";
  protected override string SubtitleText => "(replace me)";
  protected override string HintText => "Click Play to begin, Esc to quit.";

  // Sprite-skinned buttons out of Content/sprites/ui-frame.png. Drop this
  // override and the base class falls back to flat rectangles — everything in
  // the skin is opt-in, so nothing breaks if you delete the art.
  protected override NineSlice ButtonFrame => _frame;
  protected override int PixelScale => 2;
  protected override Color TitleColor => new(250, 200, 134);
  protected override Color SubtitleColor => new(164, 174, 194);
  protected override Color HoverButtonFrameTint => new(250, 200, 134);

  protected override IReadOnlyList<ButtonSpec> GetButtons() => new[]
  {
    new ButtonSpec("play", "Play", _onPlay),
    new ButtonSpec("quit", "Quit", _onQuit),
  };
}
