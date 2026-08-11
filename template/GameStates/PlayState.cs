using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.__SAMPLE__.GameStates;

public class PlayState : GameState
{
  private readonly KeyboardManager _keyboard;
  private readonly MouseManager _mouse;
  private readonly SpriteFont _font;
  private readonly Texture2D _placeholderSprite;
  private readonly int _viewportWidth;
  private readonly int _viewportHeight;

  public PlayState(ServiceProvider sp, SpriteFont font, Texture2D placeholderSprite, int vw, int vh)
  {
    _keyboard = sp.GetService<KeyboardManager>();
    _mouse = sp.GetService<MouseManager>();
    _font = font;
    _placeholderSprite = placeholderSprite;
    _viewportWidth = vw;
    _viewportHeight = vh;
  }

  public override void Entered() => IsActive = true;
  public override void Leaving() { }
  public override void Obscuring() => IsActive = false;
  public override void Revealed() => IsActive = true;

  public override void Update(GameTime gameTime)
  {
    // Your gameplay Update logic.
  }

  public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    // SamplerState.PointClamp keeps pixel art crisp. The default (LinearClamp)
    // blurs any texture drawn at a scale other than 1:1 — including this
    // placeholder at 4x below. Keep it on every Begin that draws sprites.
    spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    Primitives.DrawRectangle(spriteBatch, new Rectangle(0, 0, _viewportWidth, _viewportHeight), new Color(18, 22, 34));

    const string placeholder = "__SAMPLE__ PlayState — replace this with your game.";
    Vector2 sz = _font.MeasureString(placeholder);
    spriteBatch.DrawString(_font, placeholder,
      new Vector2(_viewportWidth / 2f - sz.X / 2f, _viewportHeight / 2f - sz.Y / 2f),
      Color.White);

    // Proof the sprite path works end-to-end: source PNG -> MGCB -> .xnb ->
    // Content.Load -> Draw. Scaled 4x with an integer factor; scale pixel art
    // by whole numbers only, or pixels come out uneven sizes.
    const int scale = 4;
    int spriteSize = _placeholderSprite.Width * scale;
    spriteBatch.Draw(
      _placeholderSprite,
      new Rectangle(
        (int)(_viewportWidth / 2f - spriteSize / 2f),
        (int)(_viewportHeight / 2f + sz.Y),
        spriteSize,
        _placeholderSprite.Height * scale),
      Color.White);
    spriteBatch.End();
  }
}
