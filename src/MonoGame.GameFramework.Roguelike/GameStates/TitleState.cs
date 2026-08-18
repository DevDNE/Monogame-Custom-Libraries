using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Roguelike.GameStates;

public class TitleState : TitleScreenState
{
  private readonly Action _onPlay;
  private readonly Action _onQuit;

  private readonly RoguelikeArt _art;
  private float _elapsed;

  public TitleState(ServiceProvider sp, SpriteFont font, int vw, int vh, RoguelikeArt art, Action onPlay, Action onQuit)
    : base(sp, font, vw, vh)
  {
    _art = art;
    _onPlay = onPlay;
    _onQuit = onQuit;
  }

  protected override Color BackgroundColor => new(14, 16, 24);
  protected override string TitleText => "Dungeon Run";
  protected override string SubtitleText => "Turn-based dungeon crawler";
  protected override string HintText => "WASD / Arrows move   Bump monsters to attack   > on stairs to descend";

  protected override NineSlice ButtonFrame => _art.Frame;
  protected override int PixelScale => 2;
  protected override Color TitleColor => new(255, 226, 158);
  protected override Color SubtitleColor => new(160, 170, 190);
  protected override Color HintColor => new(112, 122, 142);
  protected override Color HoverButtonFrameTint => new(248, 178, 74);
  protected override int TitleY => 100;
  protected override int SubtitleY => 136;
  protected override int ButtonBlockStartY => 330;

  public override void Update(GameTime gameTime)
    => _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

  /// <summary>
  /// A stone wall with two torches and the cast standing on the floor in front
  /// of it, lit by exactly the falloff the game uses. It is the whole pitch:
  /// this is dark, and you can only see a little of it at a time.
  /// </summary>
  protected override void DrawBackdrop(SpriteBatch spriteBatch, GameTime gameTime)
  {
    const int t = RoguelikeArt.TileSize;
    int floorTop = ViewportHeight / 2 + 40;

    for (int y = 0; y < ViewportHeight; y += t)
    {
      for (int x = 0; x < ViewportWidth; x += t)
      {
        bool floor = y >= floorTop;
        Rectangle src = RoguelikeArt.TileFrame(floor ? (int)TileKind.Floor : (int)TileKind.Wall);
        // Vignette from the centre, using the same idea as the play state.
        int dx = System.Math.Abs(x - ViewportWidth / 2) / t;
        int dy = System.Math.Abs(y - floorTop) / t;
        int d = System.Math.Max(dx, dy);
        byte v = (byte)System.Math.Clamp(255 - d * 12, 60, 255);
        spriteBatch.Draw(_art.Tiles, new Rectangle(x, y, t, t), src, new Color(v, v, v));
      }
    }

    // Torches flicker on a two-frame cycle — whole pixels, no sub-pixel drift.
    int flicker = (int)(_elapsed * 8f) % 2;
    PixelDraw.Sprite(spriteBatch, _art.Torch, 150, floorTop - 90 - flicker, 3);
    PixelDraw.Sprite(spriteBatch, _art.Torch, ViewportWidth - 198, floorTop - 90 - (1 - flicker), 3);

    int y0 = floorTop - t * 3;
    PixelDraw.Frame(spriteBatch, _art.Actors,
      RoguelikeArt.ActorRect(RoguelikeArt.ActorFrame.Hero), 300, y0, 3);
    PixelDraw.Frame(spriteBatch, _art.Actors,
      RoguelikeArt.ActorRect(RoguelikeArt.ActorFrame.Goblin), 400, y0, 3);
    PixelDraw.Frame(spriteBatch, _art.Actors,
      RoguelikeArt.ActorRect(RoguelikeArt.ActorFrame.Rat), 470, y0, 3);
  }

  protected override IReadOnlyList<ButtonSpec> GetButtons() => new[]
  {
    new ButtonSpec("play", "Play", _onPlay),
    new ButtonSpec("quit", "Quit", _onQuit),
  };
}
