using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using GraphicsManager;
using PhysicsManager;

namespace TestingSprites
{
  public class SpriteTesting
  {
    private SpriteSheet blackTile;
    private SpriteSheet walkBasicAnim;

    private readonly DrawManager drawManager;
    public SpriteTesting(DrawManager _drawManager)
    {
      drawManager = _drawManager;
    }

    public void LoadContent(ContentManager content)
    {
      blackTile = new SpriteSheet("black_tile",
        content.Load<Texture2D>("black_tile"),
        new Vector2(100, 100), 100, 100,
        new Rectangle[] { new(0, 0, 100, 100) },
        0.1f, 0);

      walkBasicAnim = new SpriteSheet("walk_basic_anim",
        content.Load<Texture2D>("walk-basic"),
        new Vector2(100, 100), 100, 100,
        new Rectangle[] {
          new(0, 0, 100, 500),   // Frame 1
          new(100, 0, 100, 500), // Frame 2
          new(200, 0, 100, 500),
          new(300, 0, 100, 500),
          new(400, 0, 100, 500),
          new(500, 0, 100, 500),
          new(600, 0, 100, 500),
        },
        0.5f, 0);
      drawManager.AddSprite(blackTile);
      drawManager.AddSprite(walkBasicAnim);
    }


    public void Update(GameTime gameTime)
    {
      walkBasicAnim.Update(gameTime);

      if (CollisionManager.CheckCollisions(blackTile.Frames[blackTile.CurrentFrame], walkBasicAnim.Frames[walkBasicAnim.CurrentFrame]))
      {
        drawManager.RemoveSprite(blackTile);
        drawManager.RemoveSprite(walkBasicAnim);
      }
    }

  }
}