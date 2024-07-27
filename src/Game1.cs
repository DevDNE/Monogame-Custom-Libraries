using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Managers;
using GameStates;

namespace MyGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private readonly ServiceProvider _serviceProvider;
    private DrawManager _drawManager;
    private TextManager _textManager;
    private SettingsManager _settingsManager;
    private UIManager _uiManager;
    private SoundManager _soundManager;
    private KeyboardManager _keyboardManager;
    private MouseManager _mouseManager;
    private GamePadManager _gamePadManager;
    private GameStateManager _gameStateManager;
    private SceneManager _sceneManager;
    // private SteamworksManager _steamworksManager;
    private SpriteBatch _spriteBatch;
    private GameState initialState;

    public Game1(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _settingsManager = serviceProvider.GetService<SettingsManager>();
        _settingsManager.LoadSettings();
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = _settingsManager.WindowWidth,
            PreferredBackBufferHeight = _settingsManager.WindowHeight,
            IsFullScreen = _settingsManager.IsFullScreen
        };
        _graphics.ApplyChanges();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        //_steamworksManager = _serviceProvider.GetService<SteamworksManager>();
        Window.Title = _settingsManager.WindowTitle;
        _drawManager = _serviceProvider.GetService<DrawManager>();
        _textManager = _serviceProvider.GetService<TextManager>();
        _uiManager = _serviceProvider.GetService<UIManager>();
        _soundManager = _serviceProvider.GetService<SoundManager>();
        _keyboardManager = _serviceProvider.GetService<KeyboardManager>();
        _mouseManager = _serviceProvider.GetService<MouseManager>();
        _gamePadManager = _serviceProvider.GetService<GamePadManager>();
        _gameStateManager = _serviceProvider.GetService<GameStateManager>();
        _sceneManager = _serviceProvider.GetService<SceneManager>();
        initialState = new BattleState(GraphicsDevice, _serviceProvider);
        _gameStateManager.PushState(initialState);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _textManager.LoadContent(Content.Load<SpriteFont>("fonts/Arial"));
        _uiManager.LoadContent(_drawManager);
        _soundManager.LoadContent(Content);
        _sceneManager.LoadContent(Content);
        _gameStateManager.PeekState().Entered();
        _gameStateManager.PeekState().Revealed();
    }

    protected override void Update(GameTime gameTime)
    {
        _keyboardManager.Update();
        _mouseManager.Update();
        _gamePadManager.Update();
        _gameStateManager.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin();
        _drawManager.Draw(_spriteBatch);
        _textManager.Draw(_spriteBatch);
        _spriteBatch.End();
        base.Draw(gameTime);
    }

    protected override void OnExiting(object sender, EventArgs args)
    {
        //_steamworksManager.Shutdown();
        base.OnExiting(sender, args);
    }
}
