using Microsoft.Xna.Framework;

namespace ManageScenes
{
  public abstract class GameScene
  {
    public abstract void LoadContent();
    public abstract void UnloadContent();
    public abstract void Update(GameTime gameTime);
  }
}