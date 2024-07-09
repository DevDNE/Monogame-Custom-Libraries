using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Managers;
using Debugging;


namespace MyGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly SettingsManager _settingsManager;
    private SpriteBatch _spriteBatch;
    private DrawManager _drawManager;
    private KeyboardInput _keyboardInput;
    private MouseInput _mouseInput;
    private GamePadInput _gamePadInput;
    private PerformanceMonitor _performanceMonitor;
    private SoundManager _soundManager;
    private SceneManager _sceneManager;
    private TextManager _textManager;
    //private SteamworksManager _steamworksManager;

    public Game1()
    {
        _settingsManager = new SettingsManager();
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
        //_steamworksManager = new SteamworksManager();
        Window.Title = _settingsManager.WindowTitle;
        _keyboardInput = new KeyboardInput();
        _mouseInput = new MouseInput();
        _gamePadInput = new GamePadInput();
        _drawManager = new DrawManager();
        _performanceMonitor = new PerformanceMonitor(this);
        _soundManager = new SoundManager(Content);
        _textManager = new TextManager(Content.Load<SpriteFont>("fonts/Arial"));
        _sceneManager = new SceneManager(_drawManager, _textManager, _keyboardInput, _mouseInput, _gamePadInput, _soundManager, Content);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _sceneManager.LoadScene("FirstScene");
        //_media.LoadSoundEffect("beep");
    }

    protected override void Update(GameTime gameTime)
    {
        _keyboardInput.Update();
        _mouseInput.Update();
        _gamePadInput.Update();
        //_performanceMonitor.Update(gameTime);
        _sceneManager.Update(gameTime);
        //_steamworksManager.Update();

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
        //_steamworksManager.Shutdown();
        base.OnExiting(sender, args);
    }
}
