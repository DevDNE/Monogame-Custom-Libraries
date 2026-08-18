using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.UI;

namespace MonoGame.GameFramework.Lifecycle;

public abstract class TitleScreenState : GameState
{
  public sealed record ButtonSpec(string Id, string Label, Action OnClick, bool Enabled = true);

  protected readonly UIManager UI;
  protected readonly SpriteFont Font;
  protected readonly int ViewportWidth;
  protected readonly int ViewportHeight;

  private readonly List<(ButtonSpec Spec, SpriteSheet Sprite)> _buttons = new();

  public IReadOnlyList<SpriteSheet> RegisteredButtons
  {
    get
    {
      SpriteSheet[] copy = new SpriteSheet[_buttons.Count];
      for (int i = 0; i < _buttons.Count; i++) copy[i] = _buttons[i].Sprite;
      return copy;
    }
  }

  protected virtual int ButtonWidth => 240;
  protected virtual int ButtonHeight => 56;
  protected virtual int ButtonGap => 16;
  protected virtual int ButtonBlockStartY => ViewportHeight / 2 - 10;
  protected virtual string GroupName => "title";
  protected virtual int TitleY => 120;
  protected virtual int SubtitleY => 156;
  protected virtual Color TitleColor => Color.White;
  protected virtual Color SubtitleColor => new(170, 180, 210);
  protected virtual Color HintColor => new(180, 180, 200);
  protected virtual Color NormalButtonColor => new(55, 70, 110);
  protected virtual Color HoverButtonColor => new(90, 120, 170);
  protected virtual Color DisabledButtonColor => new(40, 40, 55);
  protected virtual Color ButtonLabelColor => Color.White;
  protected virtual Color DisabledLabelColor => new(150, 150, 165);

  protected abstract Color BackgroundColor { get; }
  protected abstract string TitleText { get; }
  protected virtual string SubtitleText => "";
  protected virtual string HintText => "";

  // ---- Pixel-art skin (all opt-in) ------------------------------------------
  //
  // Nine games needed nine title screens that look like nine games, and the
  // flat-rectangle path below was the reason they all looked like one. Every
  // hook here defaults to null or to the old behaviour, so a screen that
  // overrides nothing renders exactly as it did before any of this existed —
  // which is what made it safe to change a base class with nine live consumers.

  /// <summary>Frame drawn behind each button. Null keeps the flat rectangle.</summary>
  protected virtual NineSlice ButtonFrame => null;

  /// <summary>Frame for the hovered state. Defaults to the normal frame, tinted.</summary>
  protected virtual NineSlice HoverButtonFrame => ButtonFrame;

  /// <summary>Frame for a disabled button. Defaults to the normal frame, tinted.</summary>
  protected virtual NineSlice DisabledButtonFrame => ButtonFrame;

  /// <summary>Tiled behind everything. Null paints <see cref="BackgroundColor"/> flat.</summary>
  protected virtual Texture2D BackgroundTile => null;

  /// <summary>Whole-number magnification for every sprite on this screen.</summary>
  protected virtual int PixelScale => 3;

  /// <summary>
  /// Tint applied to the button frame per state. Tinting one sprite is how a
  /// game gets three button states out of one 16x16 file; a game wanting truly
  /// different art per state overrides the frames instead.
  /// </summary>
  protected virtual Color ButtonFrameTint => Color.White;
  protected virtual Color HoverButtonFrameTint => Color.White;
  protected virtual Color DisabledButtonFrameTint => new(120, 120, 130);

  /// <summary>
  /// Painted after the background tile and before the buttons. Override for
  /// parallax layers, a logo, a character standing beside the menu — the parts
  /// of a title screen that are art rather than layout.
  /// </summary>
  protected virtual void DrawBackdrop(SpriteBatch spriteBatch, GameTime gameTime) { }

  /// <summary>Painted after the buttons and text, for foreground trim.</summary>
  protected virtual void DrawOverlay(SpriteBatch spriteBatch, GameTime gameTime) { }

  protected abstract IReadOnlyList<ButtonSpec> GetButtons();

  protected TitleScreenState(IServiceProvider sp, SpriteFont font, int viewportWidth, int viewportHeight)
  {
    UI = sp.GetService<UIManager>();
    Font = font;
    ViewportWidth = viewportWidth;
    ViewportHeight = viewportHeight;
  }

  public override void Entered()
  {
    RegisterButtons();
    IsActive = true;
  }

  public override void Leaving()
  {
    UnregisterButtons();
  }

  public override void Obscuring() => IsActive = false;

  public override void Revealed()
  {
    RefreshButtons();
    IsActive = true;
  }

  public override void Update(GameTime gameTime) { }

  protected void RefreshButtons()
  {
    UnregisterButtons();
    RegisterButtons();
  }

  private void RegisterButtons()
  {
    IReadOnlyList<ButtonSpec> specs = GetButtons();
    int cx = ViewportWidth / 2;
    int y = ButtonBlockStartY;
    foreach (ButtonSpec spec in specs)
    {
      Rectangle bounds = new(cx - ButtonWidth / 2, y, ButtonWidth, ButtonHeight);
      SpriteSheet sprite = CreateButtonSprite(spec.Id, bounds);
      UI.AddUIElement(GroupName, sprite);
      if (spec.Enabled && spec.OnClick != null)
      {
        UI.OnClick(sprite, spec.OnClick);
      }
      _buttons.Add((spec, sprite));
      y += ButtonHeight + ButtonGap;
    }
  }

  protected virtual SpriteSheet CreateButtonSprite(string id, Rectangle bounds)
    => SpriteSheet.Static(Primitives.Pixel, bounds, name: id);

  private void UnregisterButtons()
  {
    foreach ((_, SpriteSheet sprite) in _buttons)
    {
      UI.RemoveUIElement(GroupName, sprite);
    }
    _buttons.Clear();
  }

  public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    // PointClamp unconditionally: this base class now draws textures, and the
    // default LinearClamp blurs every one of them. A screen with no sprites
    // cannot tell the difference.
    spriteBatch.Begin(samplerState: SamplerState.PointClamp);

    Rectangle viewport = new(0, 0, ViewportWidth, ViewportHeight);
    Primitives.DrawRectangle(spriteBatch, viewport, BackgroundColor);
    if (BackgroundTile != null)
      PixelDraw.Tile(spriteBatch, BackgroundTile, viewport, PixelScale);

    DrawBackdrop(spriteBatch, gameTime);

    foreach ((ButtonSpec spec, SpriteSheet sprite) in _buttons)
    {
      DrawButton(spriteBatch, sprite, spec);
    }

    if (!string.IsNullOrEmpty(TitleText))
    {
      Vector2 ts = Font.MeasureString(TitleText);
      spriteBatch.DrawString(Font, TitleText, new Vector2(ViewportWidth / 2f - ts.X / 2f, TitleY), TitleColor);
    }
    if (!string.IsNullOrEmpty(SubtitleText))
    {
      Vector2 ss = Font.MeasureString(SubtitleText);
      spriteBatch.DrawString(Font, SubtitleText, new Vector2(ViewportWidth / 2f - ss.X / 2f, SubtitleY), SubtitleColor);
    }
    if (!string.IsNullOrEmpty(HintText))
    {
      Vector2 hs = Font.MeasureString(HintText);
      spriteBatch.DrawString(Font, HintText, new Vector2(ViewportWidth / 2f - hs.X / 2f, ViewportHeight - 60), HintColor);
    }

    DrawOverlay(spriteBatch, gameTime);
    spriteBatch.End();
  }

  private void DrawButton(SpriteBatch spriteBatch, SpriteSheet sprite, ButtonSpec spec)
  {
    bool hovered = spec.Enabled && UI.HoveredElement == sprite;
    Rectangle bounds = sprite.DestinationFrame;

    NineSlice frame = !spec.Enabled ? DisabledButtonFrame : hovered ? HoverButtonFrame : ButtonFrame;
    if (frame != null)
    {
      Color tint = !spec.Enabled ? DisabledButtonFrameTint : hovered ? HoverButtonFrameTint : ButtonFrameTint;
      frame.Draw(spriteBatch, bounds, PixelScale, tint);
    }
    else
    {
      Color bg = !spec.Enabled
        ? DisabledButtonColor
        : hovered ? HoverButtonColor : NormalButtonColor;
      Primitives.DrawRectangle(spriteBatch, bounds, bg);
    }

    Vector2 size = Font.MeasureString(spec.Label);
    spriteBatch.DrawString(Font, spec.Label,
      new Vector2(bounds.Center.X - size.X / 2f, bounds.Center.Y - size.Y / 2f),
      spec.Enabled ? ButtonLabelColor : DisabledLabelColor);
  }
}
