using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using InputManager;
using PhysicsManager;
using GraphicsManager;
using Tiles;

namespace MyGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteManager _spriteManager;
    private CollisionManager _collisionManager;
    private KeyboardInput _keyboardInput;
    private MouseInput _mouseInput;
    private GamePadInput _gamePadInput;
    private Tile _tile;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            // Set Windowed Resolution
            // PreferredBackBufferWidth = 960,
            // PreferredBackBufferHeight = 540,
            // Set Fullscreen Window (Max Resolution)
            // PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width,
            // PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height,
            // IsFullScreen = true
        };
        _graphics.ApplyChanges();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
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
