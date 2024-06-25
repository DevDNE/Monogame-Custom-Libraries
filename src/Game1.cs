using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using InputManager;
using NetworkManager;
using GraphicsManager;
using SettingsManager;
using DebugManager;
using SoundManager;
using SceneManager;
using UIManager;

namespace MyGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly WindowSettings _windowSettings;
    private SpriteBatch _spriteBatch;
    private DrawManager _drawManager;
    private KeyboardInput _keyboardInput;
    private MouseInput _mouseInput;
    private GamePadInput _gamePadInput;
    private PerformanceMonitor _performanceMonitor;
    private Media _media;
    private ManageScenes _manageScenes;
    private TextManager _textManager;
    private SteamworksManager _steamworksManager;

    public Game1()
    {
        _windowSettings = new WindowSettings();
        _windowSettings.LoadSettings();
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = _windowSettings.WindowWidth,
            PreferredBackBufferHeight = _windowSettings.WindowHeight,
            IsFullScreen = _windowSettings.IsFullScreen
        };
        _graphics.ApplyChanges();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _steamworksManager = new SteamworksManager();
        Window.Title = _windowSettings.WindowTitle;
        _keyboardInput = new KeyboardInput();
        _mouseInput = new MouseInput();
        _gamePadInput = new GamePadInput();
        _drawManager = new DrawManager();
        _performanceMonitor = new PerformanceMonitor(this);
        _media = new Media(Content);
        _textManager = new TextManager(Content.Load<SpriteFont>("fonts/Arial"));

        _manageScenes = new ManageScenes(_drawManager, _textManager, _keyboardInput, _mouseInput, _gamePadInput, _media, Content);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _manageScenes.LoadScene("FirstScene");
        //_media.LoadSoundEffect("beep");
    }

    protected override void Update(GameTime gameTime)
    {
        _keyboardInput.Update();
        _mouseInput.Update();
        _gamePadInput.Update();
        //_performanceMonitor.Update(gameTime);
        _manageScenes.Update(gameTime);
        _steamworksManager.Update();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin();
        _textManager.Draw(_spriteBatch);
        _drawManager.Draw(_spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
    
    protected override void OnExiting(object sender, EventArgs args)
    {
        _steamworksManager.Shutdown();
        base.OnExiting(sender, args);
    }
}
