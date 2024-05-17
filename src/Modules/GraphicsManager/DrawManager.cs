using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace GraphicsManager
{
  public class DrawManager
  {
    private readonly List<SpriteSheet> sprites = new();

    public void Draw(SpriteBatch spriteBatch)
    {
      foreach (SpriteSheet sprite in sprites)
      {
        Rectangle sourceRectangle = sprite.Frames[sprite.CurrentFrame];
        spriteBatch.Draw(sprite.Texture, sprite.DestinationFrame, sourceRectangle, Color.White);
      }
    }

    public void AddSprite(SpriteSheet sprite)
    {
      sprites.Add(sprite);
    }

    public void RemoveSprite(SpriteSheet sprite)
    {
      sprites.Remove(sprite);
    }
  }
}