using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Debugging;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Platformer.Entities;
using MonoGame.GameFramework.Platformer.GameStates;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.Testing;
using MonoGame.GameFramework.UI;

namespace MonoGame.GameFramework.Platformer;

public class Game1 : Game
{
  private const int ViewportWidth = 1024;
  private const int ViewportHeight = 576;

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
    Window.Title = "Platformer Sample";
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
    HeroSprites heroSprites = HeroSprites.Load(Content);
    PlatformerArt art = PlatformerArt.Load(Content);

    PlayState playState = new(_serviceProvider, _font, heroSprites, art, ViewportWidth, ViewportHeight);
    TitleState titleState = new(
      _serviceProvider, _font, ViewportWidth, ViewportHeight, art, heroSprites,
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
    GraphicsDevice.Clear(new Color(20, 24, 40));
    _gameStateManager.Draw(_spriteBatch, gameTime);
    _debugOverlay.Draw(_spriteBatch, gameTime);
    base.Draw(gameTime);
  }
}
