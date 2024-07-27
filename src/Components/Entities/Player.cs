using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Managers;
using Graphics;
using UI;
using GameEvents;

namespace Entities;

public class Player : Entity
{
  public Tile CurrentTile { get; set; }
  private SpriteSheet character;
  private readonly DrawManager _drawManager;
  private PlayerHealthUI playerHealthUI;
  private KeyboardManager _keyboardManager;
  private Vector2 initialPosition = new Vector2(200, 250);
  private List<Projectile> projectiles = new List<Projectile>();
  private Rectangle hitbox;
  private int hitboxWidth = 38;
  private int hitboxHeight = 22;
  private SpriteSheet tempProjectile;
  private int originalWidth = 35;
  private int originalHeight = 49;
  private int displayedWidth = 70;
  private int displayedHeight = 98;
  private float frameInterval = 0.1f;
  private int currentFrame = 0;
  private bool isMoving = false;
  private int isMovingCounter = 0;
  private EventManager _eventManager;
  public Player(ServiceProvider serviceProvider)
  {
    _drawManager = serviceProvider.GetService<DrawManager>();
    _keyboardManager = serviceProvider.GetService<KeyboardManager>();
    _eventManager = serviceProvider.GetService<EventManager>();
    hitbox = new Rectangle((int)initialPosition.X, (int)initialPosition.Y, hitboxWidth, hitboxHeight);
    playerHealthUI = new PlayerHealthUI(serviceProvider); 
  }

  public override void LoadContent(ContentManager content)
  {
    playerHealthUI.LoadContent(content);
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
      
    // TODO: Change this to the actual projectile sprite
    tempProjectile = new SpriteSheet("projectileSprite", content.Load<Texture2D>("gfx/Player_Idle"), 
      new Vector2(200, 270), 35, 40, 
      new Rectangle[] { new(0, 0, 35, 40), }, 
      new Rectangle(100, 100, 35, 40), 0.5f, 0);
  
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

    playerHealthUI.UnloadContent();
  }

  public override void Update(GameTime gameTime)
  {     
    CheckMoving(gameTime);
    CheckMovingUp(gameTime);
    CheckMovingLeft(gameTime);
    CheckMovingDown(gameTime);
    CheckMovingRight(gameTime);
    CheckFiredProjectile(gameTime);
  }

  private void CheckMoving(GameTime gameTime)
  {
    if (isMoving)
    {
      isMovingCounter++;
      character.Update(gameTime);
      if (isMovingCounter == 24)
      {
        isMoving = false;
        isMovingCounter = 0;
        character.CurrentFrame = 0;
      }
    }
  }

  private void CheckMovingUp(GameTime gameTime)
  {
    if (!_keyboardManager.IsKeyDown(Keys.W) && _keyboardManager.WasKeyReleased(Keys.W))
    {
      if (character.Position.Y > 200)
      {
        isMoving = true;
        character.Position = new Vector2(character.Position.X, character.Position.Y - 80);
        character.DestinationFrame = new Rectangle((int)character.Position.X, (int)character.Position.Y, displayedWidth, displayedHeight);
        character.Update(gameTime);
        _eventManager.TriggerEvent("PlayerMoved", this, new GameEventArgs("Player moved up"));
      }
    }
  }

  private void CheckMovingLeft(GameTime gameTime)
  {
    if (!_keyboardManager.IsKeyDown(Keys.A) && _keyboardManager.WasKeyReleased(Keys.A))
    {
      if (character.Position.X > 150)
      {
        isMoving = true;
        character.Position = new Vector2(character.Position.X - 80, character.Position.Y);
        character.DestinationFrame = new Rectangle((int)character.Position.X, (int)character.Position.Y, displayedWidth, displayedHeight);
        character.Update(gameTime);
        _eventManager.TriggerEvent("PlayerMoved", this, new GameEventArgs("Player moved left"));
      }
    }
  }

  private void CheckMovingDown(GameTime gameTime)
  {
    if (!_keyboardManager.IsKeyDown(Keys.S) && _keyboardManager.WasKeyReleased(Keys.S))
    {
      if (character.Position.Y < 300)
      {
        isMoving = true;
        character.Position = new Vector2(character.Position.X, character.Position.Y + 80);
        character.DestinationFrame = new Rectangle((int)character.Position.X, (int)character.Position.Y, displayedWidth, displayedHeight);
        character.Update(gameTime);
        _eventManager.TriggerEvent("PlayerMoved", this, new GameEventArgs("Player moved down"));
      }
    }
  }

  private void CheckMovingRight(GameTime gameTime)
  {
    if (!_keyboardManager.IsKeyDown(Keys.D) && _keyboardManager.WasKeyReleased(Keys.D))
    {
      if (character.Position.X < 250)
      {
        isMoving = true;
        character.Position = new Vector2(character.Position.X + 80, character.Position.Y);
        character.DestinationFrame = new Rectangle((int)character.Position.X, (int)character.Position.Y, displayedWidth, displayedHeight);
        character.Update(gameTime);
        _eventManager.TriggerEvent("PlayerMoved", this, new GameEventArgs("Player moved right"));
      }
    }
  }

  private void CheckFiredProjectile(GameTime gameTime)
  {
    if (!_keyboardManager.IsKeyDown(Keys.Space) && _keyboardManager.WasKeyReleased(Keys.Space))
    {
      FireProjectile(new Projectile(_drawManager, character.Position, 
        new Vector2(10, 0), (SpriteSheet) tempProjectile.Clone()));
    }
    RemoveProjectileOffscreen(gameTime);
  }

  private void RemoveProjectileOffscreen(GameTime gameTime) {
    // This loop prevents Collection was modified; enumeration operation may not execute. error
    for (int i = projectiles.Count - 1; i >= 0; i--)
      {
        projectiles[i].Update(gameTime);
        // Currently using 1000 as the offscreen value to remove the projectile
        if (projectiles[i].GetHurtbox().X > 1000 || projectiles[i].GetHurtbox().X < 0)
        {
          _drawManager.RemoveSprite(projectiles[i].GetSprite());
          projectiles.RemoveAt(i);
        }
      }
  }
  public void RemoveProjectileOnCollision(Projectile projectile)
  {
    _drawManager.RemoveSprite(projectile.GetSprite());
    projectiles.Remove(projectile);
  }

  public void FireProjectile(Projectile projectile)
  {
    _drawManager.AddSprite(projectile.GetSprite());
    projectiles.Add(projectile);
    _eventManager.TriggerEvent("PlayerFiredProjectile", this, new GameEventArgs("Player fired a projectile"));
  }

  public List<Projectile> GetProjectiles()
  {
    return projectiles;
  }
  
  public Rectangle GetHitbox()
  {
    return hitbox;
  }
}
