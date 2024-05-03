using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using InputManager;
using GraphicsManager;
using SettingsManager;
using TestingSprites;


namespace MyGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly WindowManager _windowManager;

    private SpriteBatch _spriteBatch;
    private DrawManager _drawManager;
    private KeyboardInput _keyboardInput;
    private MouseInput _mouseInput;
    private GamePadInput _gamePadInput;
    private SpriteTesting _spriteTesting;

    public Game1()
    {
        _windowManager = new WindowManager();
        _windowManager.LoadSettings();
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = _windowManager.WindowWidth,
            PreferredBackBufferHeight = _windowManager.WindowHeight,
            IsFullScreen = _windowManager.IsFullScreen
        };
        _graphics.ApplyChanges();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        Window.Title = _windowManager.WindowTitle;
        _keyboardInput = new KeyboardInput();
        _mouseInput = new MouseInput();
        _gamePadInput = new GamePadInput();
        _drawManager = new DrawManager();
        _spriteTesting = new SpriteTesting(_drawManager);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _spriteTesting.LoadContent(Content);
    }

    protected override void Update(GameTime gameTime)
    {
        _keyboardInput.Update();
        _mouseInput.Update();
        _gamePadInput.Update();
        _spriteTesting.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin();
        _drawManager.Draw(_spriteBatch);
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}
