using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Entities;
using GraphicsManager;
using InputManager;
// using SoundManager;


namespace ManageScenes
{
  public class FirstScene : GameScene
  {
    private ContentManager content;
    private Player _player;
    public FirstScene(DrawManager drawManager, KeyboardInput keyboardInput, ContentManager content)
    {
      this.content = content;
      _player = new Player(drawManager, keyboardInput);
    }

    public override void LoadContent()
    {
      // Add your content loading logic here
      _player.LoadContent(content);
    }

    public override void UnloadContent()
    {
      // Add your content unloading logic here
      _player.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
      // Add your update logic here
      _player.Update(gameTime);
    }
  }
}