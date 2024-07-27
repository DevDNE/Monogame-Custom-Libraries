using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Managers;
using Graphics;
using GameEvents;

namespace Entities;

public class EnemyPlayer : Entity
{
  public Tile CurrentTile { get; set; }
  private SpriteSheet character;
  private DrawManager _drawManager;
  private TextManager _textManager;
  private EventManager _eventManager;

  private int hp = 100;
  private Vector2 initialPosition = new Vector2(485, 250);
  private Rectangle hitbox;
  private int hitboxWidth = 38;
  private int hitboxHeight = 22;
  private int originalWidth = 35;
  private int originalHeight = 49;
  private int displayedWidth = 70;
  private int displayedHeight = 98;
  private float frameInterval = 0.1f;
  private int currentFrame = 0;
  public EnemyPlayer(ServiceProvider serviceProvider)
  {
    _drawManager = serviceProvider.GetService<DrawManager>();
    _textManager = serviceProvider.GetService<TextManager>();
    _eventManager = serviceProvider.GetService<EventManager>();
    _eventManager.Subscribe("EnemyHit", OnProjectileHit);
    hitbox = new Rectangle((int)initialPosition.X, (int)initialPosition.Y, hitboxWidth, hitboxHeight);
  }

  public override void LoadContent(ContentManager content)
  {
    character = new SpriteSheet("PlayerCharacter",
      content.Load<Texture2D>("gfx/aquaStyle"),
      initialPosition, originalWidth, originalHeight,
      new Rectangle[] { // Define the frames of the sprite sheet
        new(0, 0, originalWidth, originalHeight),
        new(48, 0, originalWidth, originalHeight),
        new(96, 0, originalWidth, originalHeight),
        new(144, 0, originalWidth, originalHeight)
      },
      new Rectangle((int)initialPosition.X, (int)initialPosition.Y, displayedWidth, displayedHeight), 
      frameInterval, currentFrame);
     _textManager.AddText("enemy", hp.ToString() ,new Vector2(500, 150), Color.Red);

    _drawManager.AddSprite(character);
  }

  public override void UnloadContent()
  {
    if (character != null)
    {
      character.Texture.Dispose();
      character.Texture = null;
    }
    _drawManager.RemoveSprite(character);
    character = null;
    
    _eventManager.Unsubscribe("EnemyHit", OnProjectileHit);
  }

  public override void Update(GameTime gameTime){}

  public Rectangle GetHitbox()
  {
    return hitbox;
  }
  
  public void OnProjectileHit(object sender, GameEventArgs e)
  {
    int hpBeforeHit = hp;
    hp -= 10;
    if (hp > 0) {
      _textManager.UpdateText("enemy", hpBeforeHit.ToString(), hp.ToString(), new Vector2(500, 150), Color.Red);
    } else {
      _textManager.UpdateText("enemy", hpBeforeHit.ToString(), "Defeated", new Vector2(500, 150), Color.Red);
    }
  }
}
