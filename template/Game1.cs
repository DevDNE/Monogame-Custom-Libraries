using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Debugging;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.Testing;
using MonoGame.GameFramework.UI;
using MonoGame.GameFramework.__SAMPLE__.GameStates;

namespace MonoGame.GameFramework.__SAMPLE__;

public class Game1 : Game
{
  private const int ViewportWidth = 960;
  private const int ViewportHeight = 640;

  private readonly ServiceProvider _serviceProvider;
  private readonly GraphicsDeviceManager _graphics;
  private SpriteBatch _spriteBatch;
  private SpriteFont _font;
  private KeyboardManager _keyboardManager;
  private MouseManager _mouseManager;
  private UIManager _uiManager;
  private GameStateManager _gameStateManager;
  private DebugOverlay _debugOverlay;
  private SmokeHarness _smoke;

  public Game1(ServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
    _graphics = new GraphicsDeviceManager(this)
    {
      PreferredBackBufferWidth = ViewportWidth,
      PreferredBackBufferHeight = ViewportHeight,
    };
    _graphics.ApplyChanges();
    Content.RootDirectory = "Content";
    IsMouseVisible = true;
    Window.Title = "__SAMPLE__ Sample";
  }

  protected override void Initialize()
  {
    _keyboardManager = _serviceProvider.GetService<KeyboardManager>();
    _mouseManager = _serviceProvider.GetService<MouseManager>();
    _uiManager = _serviceProvider.GetService<UIManager>();
    _gameStateManager = _serviceProvider.GetService<GameStateManager>();
    _debugOverlay = _serviceProvider.GetService<DebugOverlay>();
    _smoke = _serviceProvider.GetService<SmokeHarness>();
    base.Initialize();
  }

  protected override void LoadContent()
  {
    _spriteBatch = new SpriteBatch(GraphicsDevice);
    Primitives.Initialize(GraphicsDevice);
    _font = Content.Load<SpriteFont>("fonts/Arial");
    _debugOverlay.SetFont(_font);
    // Textures load exactly like fonts — the path is relative to Content/ and
    // drops the extension. See Content/Content.mgcb for the processor settings
    // that matter for pixel art.
    Texture2D placeholderSprite = Content.Load<Texture2D>("sprites/placeholder");

    PlayState playState = new(_serviceProvider, _font, placeholderSprite, ViewportWidth, ViewportHeight);
    TitleState titleState = new(
      _serviceProvider, _font, ViewportWidth, ViewportHeight,
      onPlay: () => _gameStateManager.ChangeState(playState),
      onQuit: Exit);
    _gameStateManager.PushState(titleState);
  }

  protected override void Update(GameTime gameTime)
  {
    _keyboardManager.Update();
    _mouseManager.Update();
    if (_keyboardManager.IsKeyDown(Keys.Escape)) Exit();
    _uiManager.Update(gameTime);
    _debugOverlay.Update(gameTime);
    if (!_debugOverlay.ShouldSkipUpdate) _gameStateManager.Update(gameTime);
    base.Update(gameTime);
    if (_smoke.Tick()) Exit();
  }

  protected override void Draw(GameTime gameTime)
  {
    GraphicsDevice.Clear(new Color(18, 22, 34));
    _gameStateManager.Draw(_spriteBatch, gameTime);
    _debugOverlay.Draw(_spriteBatch, gameTime);
    base.Draw(gameTime);
  }
}
