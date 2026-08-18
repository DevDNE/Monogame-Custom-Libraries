using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.TowerDefense.Entities;

public class Projectile
{
  public const int Size = 8;
  public const float Speed = 420f;
  public const int Damage = 8;

  public Vector2 Position;
  public Enemy Target;
  public bool Alive;

  public Rectangle Bounds => new((int)(Position.X - Size * 0.5f), (int)(Position.Y - Size * 0.5f), Size, Size);

  public void Launch(Vector2 origin, Enemy target)
  {
    Position = origin;
    Target = target;
    Alive = true;
  }

  public void Update(float dt)
  {
    if (!Alive) return;
    if (Target == null || !Target.Alive) { Alive = false; return; }
    Vector2 toTarget = Target.Position - Position;
    float dist = toTarget.Length();
    float step = Speed * dt;
    if (dist <= step)
    {
      Target.Damage(Damage);
      Alive = false;
    }
    else
    {
      toTarget /= dist;
      Position += toTarget * step;
    }
  }

  public void Draw(SpriteBatch spriteBatch, TowerDefenseArt art)
  {
    if (!Alive) return;
    PixelDraw.Sprite(spriteBatch, art.Shot, Bounds.X, Bounds.Y, TowerDefenseArt.ShotScale);
  }
}
