using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Debugging;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Puzzle.GameStates;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.Testing;
using MonoGame.GameFramework.UI;

namespace MonoGame.GameFramework.Puzzle;

public class Game1 : Game
{
  private const int ViewportWidth = 720;
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
  private ScreenScaler _screen;

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
    Window.Title = "Puzzle Sample";
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
    // The game paints at a fixed design size and the scaler puts that on the
    // window at a whole-number scale. Routing the mouse through it is not
    // optional: without the transform every hit-test in the game reads window
    // pixels while the game draws in design pixels, and they stop agreeing the
    // moment the window is resized.
    _screen = new ScreenScaler(GraphicsDevice, ViewportWidth, ViewportHeight);
    _screen.AttachTo(Window, _graphics);
    _mouseManager.PositionTransform = _screen.WindowToVirtual;
    Primitives.Initialize(GraphicsDevice);
    _font = Content.Load<SpriteFont>("fonts/Arial");
    _debugOverlay.SetFont(_font);

    PuzzleArt art = PuzzleArt.Load(Content);

    PlayState playState = new(_serviceProvider, _font, art, ViewportWidth, ViewportHeight);
    TitleState titleState = new(
      _serviceProvider, _font, ViewportWidth, ViewportHeight, art,
      onPlay: () => _gameStateManager.ChangeState(playState),
      onQuit: Exit);
    _gameStateManager.PushState(titleState);
  }

  protected override void Update(GameTime gameTime)
  {
    _keyboardManager.Update();
    _mouseManager.Update();
    if (_keyboardManager.IsKeyDown(Keys.Escape)) Exit();
    if (_keyboardManager.WasKeyPressed(Keys.F11)) _screen.ToggleFullScreen(_graphics);
    _uiManager.Update(gameTime);
    _debugOverlay.Update(gameTime);
    if (!_debugOverlay.ShouldSkipUpdate) _gameStateManager.Update(gameTime);
    base.Update(gameTime);
    if (_smoke.Tick()) Exit();
  }

  protected override void Draw(GameTime gameTime)
  {
    _screen.BeginDraw();
    GraphicsDevice.Clear(new Color(22, 24, 36));
    _gameStateManager.Draw(_spriteBatch, gameTime);
    _debugOverlay.Draw(_spriteBatch, gameTime);
    _screen.Present(_spriteBatch);
    base.Draw(gameTime);
  }
}
