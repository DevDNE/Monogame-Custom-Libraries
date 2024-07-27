using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Managers;
using Entities;

// using SoundManager;


namespace Scenes
{
  public class FirstScene : GameScene
  {
    private ContentManager content;
    private Player _player;
    public FirstScene()
    {
      //_player = new Player(drawManager, keyboardInput);
    }

    public override void LoadContent(ContentManager content)
    {
      // Add your content loading logic here
      //_player.LoadContent(content);
    }

    public override void UnloadContent()
    {
      // Add your content unloading logic here
      //_player.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
      // Add your update logic here
      //_player.Update(gameTime);
    }
  }
}