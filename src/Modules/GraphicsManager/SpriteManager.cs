using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace GraphicsManager
{
  public class SpriteManager
  {
    private List<Sprite> sprites = new();
    public void Draw(SpriteBatch spriteBatch)
    {
      foreach (Sprite sprite in sprites)
      {
        spriteBatch.Draw(sprite.Texture, sprite.Position, sprite.BoundingBox, Color.White);
      }
    }

    public void AddSprite(Sprite sprite)
    {
      sprites.Add(sprite);
    }
    public void RemoveSprite(Sprite sprite)
    {
      sprites.Remove(sprite);
    }
  }
}

//_whiteTile = Content.Load<Texture2D>("white_tile");

//_blackTile = new Sprite(Content.Load<Texture2D>("black_tile"), new Vector2(100, 100), 32, 32);
//_whiteTile = new Sprite(Content.Load<Texture2D>("white_tile"), new Vector2(130, 130), 32, 32);