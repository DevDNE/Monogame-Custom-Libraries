using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.BattleGrid.Components.Entities;

public class Projectile
{
  private SpriteSheet sprite;
  private Rectangle hurtbox;
  // Float position, kept by the projectile rather than by its sprite. The
  // sprite's destination rect is integer pixels, so it cannot accumulate a
  // fractional velocity without truncating it away every frame.
  private Vector2 position;
  private Vector2 velocity;
  private readonly DrawManager drawManager;
  private readonly int _damage;

  /// <summary>
  /// Takes a sheet and a source frame rather than a tint: the three shot types
  /// are three drawings now, not one rectangle in three colours. The hurtbox
  /// stays square and matches the drawn size, so what the player dodges is what
  /// they can see.
  /// </summary>
  public Projectile(
    DrawManager drawManager, Texture2D sheet, Rectangle sourceFrame,
    Vector2 position, Vector2 velocity, int damage = BattleConfig.ProjectileDamage)
  {
    this.drawManager = drawManager;
    this.position = position;
    this.velocity = velocity;
    _damage = damage;
    sprite = SpriteSheet.Static(
      sheet,
      new Rectangle((int)position.X, (int)position.Y, BattleConfig.ProjectileDisplaySize, BattleConfig.ProjectileDisplaySize),
      sourceFrame,
      name: "Projectile");
    hurtbox = sprite.DestinationFrame;
  }

  public void UnloadContent()
  {
    if (sprite != null)
    {
      drawManager.RemoveSprite(sprite);
      sprite = null;
    }
  }

  public void Update(GameTime gameTime)
  {
    position += velocity;
    sprite.DestinationFrame = new Rectangle(
      (int)position.X, (int)position.Y,
      BattleConfig.ProjectileDisplaySize, BattleConfig.ProjectileDisplaySize);
    hurtbox = sprite.DestinationFrame;
  }

  public SpriteSheet GetSprite() => sprite;
  public Rectangle GetHurtbox() => hurtbox;
  public int GetDamageNumber() => _damage;
}
