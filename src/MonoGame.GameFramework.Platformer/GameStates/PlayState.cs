using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Platformer.Entities;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Platformer.GameStates;

public class PlayState : GameState
{
  private const float DeathPlaneY = 800f;

  private readonly KeyboardManager _keyboardManager;
  private readonly SpriteFont _font;
  private readonly HeroSprites _heroSprites;
  private readonly int _viewportWidth;
  private readonly int _viewportHeight;

  private Player _player;
  private List<Platform> _platforms;
  private List<Enemy> _enemies;
  private Goal _goal;
  private Camera2D _camera;
  private bool _won;

  public PlayState(ServiceProvider serviceProvider, SpriteFont font, HeroSprites heroSprites, int viewportWidth, int viewportHeight)
  {
    _keyboardManager = serviceProvider.GetService<KeyboardManager>();
    _font = font;
    _heroSprites = heroSprites;
    _viewportWidth = viewportWidth;
    _viewportHeight = viewportHeight;
  }

  public override void Entered()
  {
    _player = new Player(new Vector2(200, 60));
    _platforms = new List<Platform>
    {
      new(new Rectangle(0, 540, 1050, 36)),
      new(new Rectangle(1200, 540, 1200, 36)),
      new(new Rectangle(130, 420, 180, 24)),
      new(new Rectangle(430, 340, 180, 24)),
      new(new Rectangle(740, 420, 180, 24)),
      new(new Rectangle(430, 180, 180, 24)),
      new(new Rectangle(1050, 440, 180, 24)),
      new(new Rectangle(1400, 360, 180, 24)),
      new(new Rectangle(1700, 280, 180, 24)),
      new(new Rectangle(2000, 380, 260, 24)),
    };
    _enemies = new List<Enemy>
    {
      new(x: 1400, y: 360 - Enemy.Height, patrolMinX: 1400, patrolMaxX: 1400 + 180),
    };
    _goal = new Goal(new Vector2(2300, 540 - Goal.Height));
    _camera = new Camera2D(new Vector2(_viewportWidth, _viewportHeight))
    {
      Position = PlayerCenter(),
      Target = PlayerCenter(),
      FollowLerp = 0.12f,
    };
    _won = false;
    IsActive = true;
  }

  public override void Leaving() { }
  public override void Obscuring() => IsActive = false;
  public override void Revealed() => IsActive = true;

  private Vector2 PlayerCenter() => _player.Position + new Vector2(Player.Width * 0.5f, Player.Height * 0.5f);

  private void Respawn()
  {
    _player.Respawn();
    _won = false;
    _camera.Position = PlayerCenter();
  }

  public override void Update(GameTime gameTime)
  {
    if (_keyboardManager.WasKeyPressed(Keys.R)) Respawn();

    if (!_won)
    {
      float inputX = 0f;
      if (_keyboardManager.IsKeyDown(Keys.A) || _keyboardManager.IsKeyDown(Keys.Left)) inputX -= 1f;
      if (_keyboardManager.IsKeyDown(Keys.D) || _keyboardManager.IsKeyDown(Keys.Right)) inputX += 1f;
      bool jumpPressed = _keyboardManager.WasKeyPressed(Keys.Space);
      bool jumpHeld = _keyboardManager.IsKeyDown(Keys.Space);

      _player.Update(gameTime, _platforms, inputX, jumpPressed, jumpHeld);
      foreach (Enemy e in _enemies) e.Update(gameTime);

      if (_player.Position.Y > DeathPlaneY)
      {
        Respawn();
      }
      else if (_player.Bounds.Intersects(_goal.Bounds))
      {
        _won = true;
      }
      else
      {
        foreach (Enemy e in _enemies)
        {
          if (_player.Bounds.Intersects(e.Bounds))
          {
            Respawn();
            break;
          }
        }
      }
    }

    _camera.Target = PlayerCenter();
    _camera.Update(gameTime);
  }

  public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix(), samplerState: SamplerState.PointClamp);
    foreach (Platform p in _platforms) p.Draw(spriteBatch, Primitives.Pixel);
    foreach (Enemy e in _enemies) e.Draw(spriteBatch, Primitives.Pixel);
    _goal.Draw(spriteBatch, Primitives.Pixel);
    // Drawn last so the sprite sits in front of the rectangle-based world.
    // Sprites and Primitives share one batch fine — they're both textures to
    // SpriteBatch — but with SpriteSortMode.Deferred (the default) the call
    // order IS the draw order, so this line's position matters.
    _player.Draw(spriteBatch, _heroSprites);
    spriteBatch.End();

    if (_won)
    {
      spriteBatch.Begin();
      const string line1 = "You Win!";
      const string line2 = "Press R to play again";
      Vector2 size1 = _font.MeasureString(line1);
      Vector2 size2 = _font.MeasureString(line2);
      Vector2 center = new(_viewportWidth * 0.5f, _viewportHeight * 0.5f);
      Primitives.DrawRectangle(spriteBatch, new Rectangle(0, 0, _viewportWidth, _viewportHeight), new Color(0, 0, 0, 140));
      spriteBatch.DrawString(_font, line1, new Vector2(center.X - size1.X * 0.5f, center.Y - size1.Y - 4), Color.White);
      spriteBatch.DrawString(_font, line2, new Vector2(center.X - size2.X * 0.5f, center.Y + 4), new Color(200, 200, 200));
      spriteBatch.End();
    }
  }
}
