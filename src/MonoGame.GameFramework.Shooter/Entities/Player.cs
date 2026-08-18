using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Shooter.Entities;

public class Player
{
  public const int Size = 28;
  public const int MaxHp = 100;
  public const float MoveSpeed = 260f;
  public const float FireCooldownSeconds = 0.15f;
  public const float InvulnSeconds = 0.4f;

  public Vector2 Position;
  public int Hp = MaxHp;
  public bool IsAlive => Hp > 0;

  public Rectangle Bounds => new((int)(Position.X - Size * 0.5f), (int)(Position.Y - Size * 0.5f), Size, Size);

  private float _fireCooldown;
  private float _invuln;

  public void Spawn(Vector2 position)
  {
    Position = position;
    Hp = MaxHp;
    _fireCooldown = 0f;
    _invuln = 0f;
  }

  public void Update(float dt, KeyboardManager keyboard)
  {
    if (!IsAlive) return;
    Vector2 input = Vector2.Zero;
    if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up)) input.Y -= 1f;
    if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down)) input.Y += 1f;
    if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) input.X -= 1f;
    if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) input.X += 1f;
    if (input.LengthSquared() > 0.01f)
    {
      input.Normalize();
      Position += input * MoveSpeed * dt;
    }
    if (_fireCooldown > 0f) _fireCooldown = MathF.Max(0f, _fireCooldown - dt);
    if (_invuln > 0f) _invuln = MathF.Max(0f, _invuln - dt);
  }

  public bool TryConsumeFireCooldown()
  {
    if (_fireCooldown > 0f) return false;
    _fireCooldown = FireCooldownSeconds;
    return true;
  }

  public bool TryDamage(int amount)
  {
    if (!IsAlive || _invuln > 0f) return false;
    Hp -= amount;
    if (Hp < 0) Hp = 0;
    _invuln = InvulnSeconds;
    return true;
  }

  /// <summary>
  /// Flashes while invulnerable by tinting the sprite rather than by hiding it.
  /// Blinking a walker in and out at 10Hz makes it hard to steer at exactly the
  /// moment the player most needs to; a tint says the same thing and keeps the
  /// silhouette on screen the whole time.
  /// </summary>
  public void Draw(SpriteBatch spriteBatch, ShooterArt art)
  {
    Color tint = _invuln > 0f && ((int)(_invuln * 20) % 2) == 0
      ? new Color(255, 128, 108)
      : Color.White;
    PixelDraw.Sprite(spriteBatch, art.Player, Bounds.X, Bounds.Y, ShooterArt.EntityScale, tint);
  }
}
