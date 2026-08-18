using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Input;
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

  // All three may be null: the base takes IServiceProvider so a test can feed a
  // container holding only a UIManager, and every read below is guarded.
  private readonly KeyboardManager _keyboard;
  private readonly GamePadManager _gamePad;
  private readonly MouseManager _mouse;

  private Vector2 _lastMousePosition;
  private bool _stickNeutral = true;

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
    _keyboard = sp.GetService<KeyboardManager>();
    _gamePad = sp.GetService<GamePadManager>();
    _mouse = sp.GetService<MouseManager>();
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

  /// <summary>What the player asked the menu to do this frame.</summary>
  protected enum MenuInput { None, Previous, Next, Activate }

  /// <summary>Set false for a mouse-only screen.</summary>
  protected virtual bool MenuNavigation => true;

  /// <summary>Fraction of full stick deflection that counts as a press.</summary>
  protected virtual float StickDeadzone => 0.5f;

  public override void Update(GameTime gameTime)
  {
    if (!MenuNavigation || _buttons.Count == 0) return;

    FollowMouse();

    switch (ReadMenuInput())
    {
      case MenuInput.Previous: MoveSelection(-1); break;
      case MenuInput.Next: MoveSelection(+1); break;
      case MenuInput.Activate: ActivateSelection(); break;
    }
  }

  /// <summary>
  /// Read one menu action from the keyboard and pad.
  ///
  /// Virtual so a game can rebind without reimplementing traversal, and so a
  /// test can drive the menu without a window: KeyboardManager and
  /// GamePadManager read the real devices, which is exactly the reason nine
  /// games shipped with a pad manager no test and no player could exercise.
  /// </summary>
  protected virtual MenuInput ReadMenuInput()
  {
    if (_keyboard != null)
    {
      if (_keyboard.WasKeyPressed(Keys.Down) || _keyboard.WasKeyPressed(Keys.S)) return MenuInput.Next;
      if (_keyboard.WasKeyPressed(Keys.Up) || _keyboard.WasKeyPressed(Keys.W)) return MenuInput.Previous;
      if (_keyboard.WasKeyPressed(Keys.Enter) || _keyboard.WasKeyPressed(Keys.Space)) return MenuInput.Activate;
    }

    if (_gamePad == null || !_gamePad.IsGamePadConnected()) return MenuInput.None;

    if (_gamePad.WasGamePadButtonPressed(Buttons.DPadDown)) return MenuInput.Next;
    if (_gamePad.WasGamePadButtonPressed(Buttons.DPadUp)) return MenuInput.Previous;
    if (_gamePad.WasGamePadButtonPressed(Buttons.A) || _gamePad.WasGamePadButtonPressed(Buttons.Start))
      return MenuInput.Activate;

    // The stick has no press event of its own, so held deflection has to be
    // edged by hand or one nudge scrolls the whole menu at 60 steps a second.
    float y = _gamePad.GetGamePadLeftThumbstickY();
    bool neutral = MathF.Abs(y) < StickDeadzone;
    bool crossed = _stickNeutral && !neutral;
    _stickNeutral = neutral;
    if (crossed) return y < 0f ? MenuInput.Next : MenuInput.Previous;

    return MenuInput.None;
  }

  /// <summary>
  /// Move the highlight to the next enabled button, wrapping.
  ///
  /// Selection lives in <see cref="UIManager.FocusedElement"/> rather than in a
  /// private index, so the mouse and the keyboard drive one thing instead of
  /// two that can disagree -- and so the focus API, which had no consumer
  /// outside its own tests, has one.
  /// </summary>
  protected void MoveSelection(int step)
  {
    int index = NextEnabledIndex(EnabledFlags(), IndexOfSelection(), step);
    if (index >= 0) UI?.SetFocus(_buttons[index].Sprite);
  }

  /// <summary>
  /// Index of the next enabled entry from <paramref name="current"/>, wrapping,
  /// or -1 when nothing is enabled. A <paramref name="current"/> of -1 means
  /// nothing is selected yet and picks the first enabled entry from the end the
  /// player is moving away from.
  ///
  /// Pure, because wrap-plus-skip is where an off-by-one hides: a menu whose
  /// last entry is disabled stops dead at the bottom, and one with a single
  /// enabled entry loops forever if the guard counts wrong.
  /// </summary>
  public static int NextEnabledIndex(IReadOnlyList<bool> enabled, int current, int step)
  {
    if (enabled == null || enabled.Count == 0 || step == 0) return -1;
    int count = enabled.Count;

    for (int i = 1; i <= count; i++)
    {
      int index = current < 0
        ? (step > 0 ? i - 1 : count - i)
        : ((current + step * i) % count + count) % count;
      if (enabled[index]) return index;
    }
    return -1;
  }

  private void ActivateSelection()
  {
    int index = IndexOfSelection();
    if (index < 0) return;

    ButtonSpec spec = _buttons[index].Spec;
    if (!spec.Enabled || spec.OnClick == null) return;

    // Nothing may touch _buttons after this: the handler is what changes state,
    // which calls Leaving() and empties the list underneath us.
    spec.OnClick();
  }

  /// <summary>
  /// Let the pointer take the highlight, but only when it actually moves.
  ///
  /// Without the movement check a resting mouse re-asserts its button every
  /// frame and the arrow keys cannot move the highlight at all.
  /// </summary>
  private void FollowMouse()
  {
    if (_mouse == null || UI == null) return;

    Vector2 position = _mouse.GetMousePosition();
    if (position == _lastMousePosition) return;
    _lastMousePosition = position;

    for (int i = 0; i < _buttons.Count; i++)
    {
      if (UI.HoveredElement != _buttons[i].Sprite) continue;
      if (_buttons[i].Spec.Enabled) UI.SetFocus(_buttons[i].Sprite);
      else UI.ClearFocus();
      return;
    }
    UI.ClearFocus();
  }

  private int IndexOfSelection()
  {
    if (UI?.FocusedElement == null) return -1;
    for (int i = 0; i < _buttons.Count; i++)
      if (_buttons[i].Sprite == UI.FocusedElement) return i;
    return -1;
  }

  private bool[] EnabledFlags()
  {
    bool[] flags = new bool[_buttons.Count];
    for (int i = 0; i < _buttons.Count; i++) flags[i] = _buttons[i].Spec.Enabled;
    return flags;
  }

  protected void RefreshButtons()
  {
    UnregisterButtons();
    RegisterButtons();
  }

  private void RegisterButtons()
  {
    IReadOnlyList<ButtonSpec> specs = GetButtons();
    _stickNeutral = true;
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

    // Start on the first enabled entry so a pad or keyboard player has
    // something to press without hunting for it with a mouse first. Mouse
    // players see it move to whatever they point at on the first motion.
    if (MenuNavigation) MoveSelection(+1);
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
    // One highlight, whichever device moved it. Reading HoveredElement here as
    // well would light two buttons at once as soon as the pointer rested
    // somewhere other than the keyboard's selection.
    bool hovered = spec.Enabled && UI?.FocusedElement == sprite;
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
