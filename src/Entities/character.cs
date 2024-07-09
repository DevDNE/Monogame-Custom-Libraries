using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Managers;
using Graphics;

namespace Entities
{
  public class Player
  {
    private SpriteSheet character;
    private readonly DrawManager drawManager;
    private KeyboardInput keyboardInput;
    public Player(DrawManager _drawManager, KeyboardInput _keyboardInput)
    {
      drawManager = _drawManager;
      keyboardInput = _keyboardInput;
    }

    public void LoadContent(ContentManager content)
    {
      character = new SpriteSheet("PlayerCharacter",
        content.Load<Texture2D>("gfx/character"),
        new Vector2(100, 100), 30, 60,
        new Rectangle[] { // Define the frames of the sprite sheet
          new(0, 0, 15, 30),
          new(16, 0, 15, 30)
        },
        new Rectangle(100, 100, 30, 60), 
        0.5f, 0);

      drawManager.AddSprite(character);
    }
    public void UnloadContent()
    {
      if (character != null)
      {
        character.Texture.Dispose();
        character.Texture = null;
      }
      drawManager.RemoveSprite(character);
      character = null;
    }
    public void Update(GameTime gameTime)
    {
      
      if (keyboardInput.IsKeyDown(Keys.W))
      {
        character.Position = new Vector2(character.Position.X, character.Position.Y - 2);
        character.DestinationFrame = new Rectangle((int)character.Position.X, (int)character.Position.Y, character.Width, character.Height);
        character.Update(gameTime);
      }

      if (keyboardInput.IsKeyDown(Keys.A))
      {
        character.Position = new Vector2(character.Position.X - 2, character.Position.Y);
        character.DestinationFrame = new Rectangle((int)character.Position.X, (int)character.Position.Y, character.Width, character.Height);
        character.Update(gameTime);
      }

      if (keyboardInput.IsKeyDown(Keys.S))
      {
        character.Position = new Vector2(character.Position.X, character.Position.Y + 2);
        character.DestinationFrame = new Rectangle((int)character.Position.X, (int)character.Position.Y, character.Width, character.Height);
        character.Update(gameTime);
      }

      if (keyboardInput.IsKeyDown(Keys.D))
      {
        character.Position = new Vector2(character.Position.X + 2, character.Position.Y);
        character.DestinationFrame = new Rectangle((int)character.Position.X, (int)character.Position.Y, character.Width, character.Height);
        character.Update(gameTime);
      }
    }
  }
}