using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Platformer.Entities;

public class Goal
{
  public const int Width = 28;
  public const int Height = 48;

  public Rectangle Bounds { get; }

  public Goal(Vector2 position)
  {
    Bounds = new Rectangle((int)position.X, (int)position.Y, Width, Height);
  }

  public void Draw(SpriteBatch spriteBatch, PlatformerArt art)
    => PixelDraw.Sprite(spriteBatch, art.Goal, Bounds.X, Bounds.Y, PlatformerArt.Scale);
}
