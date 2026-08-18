using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.AutoBattler.GameStates;
using MonoGame.GameFramework.Debugging;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.Testing;
using MonoGame.GameFramework.UI;

namespace MonoGame.GameFramework.AutoBattler;

public class Game1 : Game
{
  private const int ViewportWidth = 760;
  private const int ViewportHeight = 720;

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

  private readonly GameModel _model = new();

  private TitleState _titleState;
  private ShopState _shopState;
  private CombatState _combatState;
  private PostCombatState _postCombatState;

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
    Window.Title = "Auto Battler";
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
    AutoBattlerArt art = AutoBattlerArt.Load(Content);
    _debugOverlay.SetFont(_font);
    _debugOverlay.AddWatch("round", () => _model.Round.ToString());
    _debugOverlay.AddWatch("hero hp", () => $"{_model.PlayerHeroHp} vs {_model.EnemyHeroHp}");
    _debugOverlay.AddWatch("gold", () => _model.Gold.ToString());

    _shopState = new ShopState(_serviceProvider, _font, _model, art, ViewportWidth, ViewportHeight,
      onStartCombat: () => _gameStateManager.ChangeState(_combatState));

    _combatState = new CombatState(_serviceProvider, _font, _model, art, ViewportWidth, ViewportHeight,
      onCombatEnded: winner =>
      {
        _postCombatState.Configure(winner);
        _gameStateManager.ChangeState(_postCombatState);
      });

    _postCombatState = new PostCombatState(_serviceProvider, _font, _model, ViewportWidth, ViewportHeight,
      onNextRound: () => _gameStateManager.ChangeState(_shopState),
      onRestart: () =>
      {
        _model.Reset();
        _gameStateManager.ChangeState(_titleState);
      });

    _titleState = new TitleState(_serviceProvider, _font, ViewportWidth, ViewportHeight, art,
      onPlay: () => { _model.Reset(); _gameStateManager.ChangeState(_shopState); },
      onQuit: Exit);

    _gameStateManager.PushState(_titleState);
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
    GraphicsDevice.Clear(new Color(18, 22, 34));
    _gameStateManager.Draw(_spriteBatch, gameTime);
    _debugOverlay.Draw(_spriteBatch, gameTime);
    _screen.Present(_spriteBatch);
    base.Draw(gameTime);
  }
}
