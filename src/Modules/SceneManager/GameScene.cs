using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace SceneManager
{
  public abstract class GameScene
  {
    public abstract void LoadContent(ContentManager content);
    public abstract void UnloadContent();
    public abstract void Update(GameTime gameTime);
  }
}