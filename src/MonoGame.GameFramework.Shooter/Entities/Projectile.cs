using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Shooter.Entities;

public class Projectile
{
  public const int Size = 8;
  public const float Speed = 600f;
  public const float LifetimeSeconds = 1.2f;

  public Vector2 Position;
  public Vector2 Velocity;
  public float LifeRemaining;
  public bool Alive;

  public Rectangle Bounds => new((int)(Position.X - Size * 0.5f), (int)(Position.Y - Size * 0.5f), Size, Size);

  public void Launch(Vector2 origin, Vector2 direction)
  {
    Position = origin;
    Velocity = direction * Speed;
    LifeRemaining = LifetimeSeconds;
    Alive = true;
  }

  public void Update(float dt)
  {
    if (!Alive) return;
    Position += Velocity * dt;
    LifeRemaining -= dt;
    if (LifeRemaining <= 0f) Alive = false;
  }

  public void Draw(SpriteBatch spriteBatch, ShooterArt art)
  {
    if (!Alive) return;
    PixelDraw.Sprite(spriteBatch, art.Shot, Bounds.X, Bounds.Y, ShooterArt.EntityScale);
  }
}
