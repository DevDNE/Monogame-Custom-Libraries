using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.BattleGrid.Components;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.BattleGrid.GameStates;

/// <summary>
/// The first state in this repo that is actually pushed onto the stack rather
/// than swapped in.
///
/// Every sample used to boot with one PushState and then call ChangeState
/// forever, which meant the stack was never deeper than one and
/// GameState.Obscuring/Revealed could not fire anywhere. That left the whole
/// stacking path — push, pop, both hooks, and the order the manager paints in —
/// carried by the library's tests and by nothing that runs. It was also wrong:
/// Draw walked the stack top-first, so this panel would have rendered *behind*
/// the battle it is covering.
///
/// So this state exists to be a real consumer, not a demo. It requires:
///   - PushState onto a non-empty stack, firing PlayState.Obscuring
///   - the battle beneath to keep drawing while its simulation stops
///     (GameState.IsVisible held true, IsActive dropped)
///   - the manager to paint bottom-up, or this panel is invisible
///   - PopState, firing PlayState.Revealed to hand control back
/// </summary>
public class PauseState : GameState
{
  private readonly GameStateManager _states;
  private readonly KeyboardManager _keyboard;
  private readonly SpriteFont _font;
  private readonly BattleArt _art;
  private readonly int _viewportWidth;
  private readonly int _viewportHeight;

  public PauseState(
    GameStateManager states, KeyboardManager keyboard, SpriteFont font,
    BattleArt art, int viewportWidth, int viewportHeight)
  {
    _states = states;
    _keyboard = keyboard;
    _font = font;
    _art = art;
    _viewportWidth = viewportWidth;
    _viewportHeight = viewportHeight;
  }

  public override void Entered() => IsActive = true;
  public override void Leaving() { }
  public override void Obscuring() => IsActive = false;
  public override void Revealed() => IsActive = true;

  public override void Update(GameTime gameTime)
  {
    if (_keyboard.WasKeyPressed(Keys.P)) _states.PopState();
  }

  public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    spriteBatch.Begin(samplerState: SamplerState.PointClamp);

    // Scrim over the battle, which is still being drawn underneath. This is the
    // part that only works because Draw paints bottom-up.
    Primitives.DrawRectangle(
      spriteBatch, new Rectangle(0, 0, _viewportWidth, _viewportHeight), new Color(10, 8, 24, 190));

    int panelW = 420;
    int panelH = 180;
    Rectangle panel = new(
      _viewportWidth / 2 - panelW / 2, _viewportHeight / 2 - panelH / 2, panelW, panelH);
    _art.Frame.Draw(spriteBatch, panel, BattleArt.Scale, new Color(150, 240, 255));

    Rectangle content = _art.Frame.ContentBounds(panel, BattleArt.Scale);
    DrawCentered(spriteBatch, "PAUSED", content.Center.X, content.Y + 12, new Color(150, 240, 255));
    DrawCentered(spriteBatch, "P to resume", content.Center.X, content.Y + 56, new Color(228, 232, 244));
    DrawCentered(spriteBatch, "the duel is still on screen,", content.Center.X, content.Y + 90, new Color(164, 168, 188));
    DrawCentered(spriteBatch, "and still frozen", content.Center.X, content.Y + 114, new Color(164, 168, 188));

    spriteBatch.End();
  }

  private void DrawCentered(SpriteBatch spriteBatch, string text, int centerX, int y, Color color)
  {
    Vector2 size = _font.MeasureString(text);
    spriteBatch.DrawString(_font, text, new Vector2(centerX - size.X / 2f, y), color);
  }
}
