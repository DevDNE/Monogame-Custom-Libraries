using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.AutoBattler.GameStates;

public class TitleState : TitleScreenState
{
  private readonly Action _onPlay;
  private readonly Action _onQuit;

  private readonly AutoBattlerArt _art;

  public TitleState(ServiceProvider sp, SpriteFont font, int vw, int vh, AutoBattlerArt art, Action onPlay, Action onQuit)
    : base(sp, font, vw, vh)
  {
    _art = art;
    _onPlay = onPlay;
    _onQuit = onQuit;
  }

  protected override Color BackgroundColor => new(18, 22, 34);
  protected override string TitleText => "Auto Battler";
  protected override string SubtitleText => "Buy units, let them fight, survive the enemy";
  protected override string HintText => "Drag cards onto your side of the board. Space to start combat.";

  protected override Texture2D BackgroundTile => _art.Felt;
  protected override NineSlice ButtonFrame => _art.Frame;
  protected override int PixelScale => AutoBattlerArt.FeltScale;
  protected override Color TitleColor => new(238, 232, 220);
  protected override Color SubtitleColor => new(198, 152, 104);
  protected override Color HintColor => new(148, 104, 66);
  protected override Color HoverButtonFrameTint => new(230, 196, 108);
  protected override int TitleY => 70;
  protected override int SubtitleY => 106;
  protected override int ButtonBlockStartY => 400;

  /// <summary>
  /// Both armies laid out facing each other across the divider, exactly as the
  /// board shows them. The rock-paper-scissors between the three classes is the
  /// whole game, and this is the one screen with room to show all six pieces.
  /// </summary>
  protected override void DrawBackdrop(SpriteBatch spriteBatch, GameTime gameTime)
  {
    const int size = AutoBattlerArt.UnitSize * AutoBattlerArt.UnitScale;
    int y = 190;
    int mid = ViewportWidth / 2;

    for (int i = 0; i < 3; i++)
    {
      UnitType type = (UnitType)i;
      PixelDraw.Frame(spriteBatch, _art.Units, AutoBattlerArt.RectFor(type, Side.Player),
        mid - 40 - (i + 1) * (size + 8), y, AutoBattlerArt.UnitScale);
      PixelDraw.Frame(spriteBatch, _art.Units, AutoBattlerArt.RectFor(type, Side.Enemy),
        mid + 40 + i * (size + 8), y, AutoBattlerArt.UnitScale);
    }

    Primitives.DrawRectangle(spriteBatch,
      new Rectangle(mid - 1, y - 20, 2, size + 40), new Color(198, 152, 104));
  }

  protected override IReadOnlyList<ButtonSpec> GetButtons() => new[]
  {
    new ButtonSpec("play", "Play", _onPlay),
    new ButtonSpec("quit", "Quit", _onQuit),
  };
}
