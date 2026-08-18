using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Persistence;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.VisualNovel.GameStates;

public class TitleState : TitleScreenState
{
  private readonly SaveSystem _saves;
  private readonly string _savePath;
  private readonly Action<DialogueState> _onPlay;
  private readonly Action _onQuit;

  private readonly VisualNovelArt _art;

  public TitleState(
    ServiceProvider sp, SpriteFont font, int vw, int vh,
    string savePath, VisualNovelArt art,
    Action<DialogueState> onPlay, Action onQuit)
    : base(sp, font, vw, vh)
  {
    _art = art;
    _saves = sp.GetService<SaveSystem>();
    _savePath = savePath;
    _onPlay = onPlay;
    _onQuit = onQuit;
  }

  protected override Color BackgroundColor => new(26, 22, 36);
  protected override string TitleText => "A Meeting at the Diner";
  protected override string SubtitleText => "A 3-choice visual novel";
  protected override int ButtonWidth => 280;
  protected override int ButtonGap => 14;
  protected override NineSlice ButtonFrame => _art.Frame;
  protected override int PixelScale => VisualNovelArt.PortraitScale;
  protected override Color TitleColor => new(255, 228, 204);
  protected override Color SubtitleColor => new(226, 140, 86);
  protected override Color HintColor => new(124, 110, 148);
  protected override Color HoverButtonFrameTint => new(248, 184, 104);
  protected override Color DisabledButtonFrameTint => new(86, 74, 106);
  protected override int TitleY => 70;
  protected override int SubtitleY => 106;
  protected override int ButtonBlockStartY => 300;

  /// <summary>
  /// The room the story happens in, with both characters already standing in
  /// it. A visual novel's title screen is the only place it can show its cast
  /// before the reader has committed to any of them.
  /// </summary>
  protected override void DrawBackdrop(SpriteBatch spriteBatch, GameTime gameTime)
  {
    PixelDraw.Sprite(spriteBatch, _art.Room, 0, 0, VisualNovelArt.RoomScale);

    const int scale = VisualNovelArt.PortraitScale;
    int height = VisualNovelArt.PortraitHeight * scale;
    int y = ViewportHeight - height - 20;

    foreach ((Portrait who, int x) in new[] { (Portrait.Alex, 90), (Portrait.Morgan, ViewportWidth - 234) })
    {
      Rectangle? source = VisualNovelArt.RectFor(who);
      if (source != null) PixelDraw.Frame(spriteBatch, _art.Portraits, source.Value, x, y, scale);
    }
  }

  protected override IReadOnlyList<ButtonSpec> GetButtons()
  {
    bool hasSave = _saves.Exists(_savePath);
    return new[]
    {
      new ButtonSpec("new", "New game", StartNew),
      new ButtonSpec("continue", hasSave ? "Continue" : "(no save)", ContinueSave, Enabled: hasSave),
      new ButtonSpec("quit", "Quit", _onQuit),
    };
  }

  private void StartNew()
  {
    if (_saves.Exists(_savePath)) _saves.Delete(_savePath);
    _onPlay(new DialogueState());
  }

  private void ContinueSave()
  {
    if (_saves.TryLoad(_savePath, out SaveFile<DialogueState> file)) _onPlay(file.Data);
  }
}
