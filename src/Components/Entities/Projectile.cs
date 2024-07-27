using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Managers;
using Graphics;

namespace Entities;
public class Projectile : Entity
{
  private SpriteSheet sprite;
  private Rectangle hurtbox;
  private int hurtboxWidth;
  private int hurtboxHeight;
  private int damageValue = 10;
  private Vector2 velocity;
  private readonly DrawManager drawManager;
  public Projectile(DrawManager _drawManager, Vector2 _position, Vector2 _velocity, SpriteSheet _sprite)
  {
    velocity = _velocity;
    drawManager = _drawManager;
    sprite = _sprite;
    sprite.Position = _position;
    hurtboxWidth = 38;
    hurtboxHeight = 22;
    hurtbox = new Rectangle((int)sprite.Position.X, (int)sprite.Position.Y, hurtboxWidth, hurtboxHeight);
  }

  public override void LoadContent(ContentManager content)
  {
    drawManager.AddSprite(sprite);
  }

  public override void UnloadContent()
  {
    if (sprite != null)
    {
      sprite.Texture.Dispose();
      sprite.Texture = null;
    }
    drawManager.RemoveSprite(sprite);
    sprite = null;
  }

  public override void Update(GameTime gameTime)
  {
    sprite.Position += velocity;
    sprite.DestinationFrame = new Rectangle((int)sprite.Position.X, (int)sprite.Position.Y, 30, 60);
    hurtbox = new Rectangle((int)sprite.Position.X, (int)sprite.Position.Y, hurtboxWidth, hurtboxHeight);
  }

  public SpriteSheet GetSprite() {
    return sprite;
  }
  
  public void SetDestinationFrame(Rectangle desitnationFrame) {
    sprite.DestinationFrame = desitnationFrame;
  }

  public Rectangle GetHurtbox() {
    return hurtbox;
  }

  public int GetDamageNumber()
  {
    return damageValue;
  }
}
