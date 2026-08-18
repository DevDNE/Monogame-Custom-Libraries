using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Shooter.Entities;

public class Enemy
{
  public const int Size = 28;
  public const float Speed = 90f;
  public const int ContactDamage = 10;

  public Vector2 Position;
  public bool Alive;

  public Rectangle Bounds => new((int)(Position.X - Size * 0.5f), (int)(Position.Y - Size * 0.5f), Size, Size);

  public void Spawn(Vector2 position)
  {
    Position = position;
    Alive = true;
  }

  public void Update(float dt, Vector2 playerPosition)
  {
    if (!Alive) return;
    Vector2 toPlayer = playerPosition - Position;
    if (toPlayer.LengthSquared() > 0.01f)
    {
      toPlayer.Normalize();
      Position += toPlayer * Speed * dt;
    }
  }

  public void Draw(SpriteBatch spriteBatch, ShooterArt art)
  {
    if (!Alive) return;
    PixelDraw.Sprite(spriteBatch, art.Enemy, Bounds.X, Bounds.Y, ShooterArt.EntityScale);
  }
}
