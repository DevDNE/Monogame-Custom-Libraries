using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using InputManager;
using PhysicsManager;
using GraphicsManager;
using Tiles;
using SettingsManager;

namespace MyGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private WindowManager _windowManager;

    private SpriteBatch _spriteBatch;
    private SpriteManager _spriteManager;
    private CollisionManager _collisionManager;
    private KeyboardInput _keyboardInput;
    private MouseInput _mouseInput;
    private GamePadInput _gamePadInput;
    private Tile _tile;

    public Game1()
    {
        _windowManager = new WindowManager();
        _windowManager.LoadSettings();
        System.Console.WriteLine("Window Title: " + _windowManager.WindowTitle);
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
        _spriteManager = new SpriteManager();
        _collisionManager = new CollisionManager();
        _tile = new Tile(_spriteManager, _collisionManager);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _tile.LoadContent(Content);
    }

    protected override void Update(GameTime gameTime)
    {
        _keyboardInput.Update();
        _mouseInput.Update();
        _gamePadInput.Update();
        _tile.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin();
        _spriteManager.Draw(_spriteBatch);
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}
