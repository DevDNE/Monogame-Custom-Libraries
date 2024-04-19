using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using GraphicsManager;
using PhysicsManager;

namespace Tiles
{
  public class Tile
  {
    private Sprite blackTile;
    private Sprite whiteTile;
    private SpriteManager spriteManager;
    private CollisionManager collisionManager;
    public Tile(SpriteManager _spriteManager, CollisionManager _collisionManager)
    {
      spriteManager = _spriteManager;
      collisionManager = _collisionManager;
    }

    public void LoadContent(ContentManager content) {
      blackTile = new Sprite("black_tile", content.Load<Texture2D>("black_tile"), new Vector2(100, 100), 100, 100);
      whiteTile = new Sprite("white_tile", content.Load<Texture2D>("white_tile"), new Vector2(300, 300), 100, 100);
      spriteManager.AddSprite(blackTile);
      spriteManager.AddSprite(whiteTile);
    }
    public void Update(GameTime gameTime)
    {
      if (CollisionManager.CheckCollisions(blackTile.BoundingBox, whiteTile.BoundingBox))
      {
        spriteManager.RemoveSprite(blackTile);
        spriteManager.RemoveSprite(whiteTile);
      }
    }

  }
}