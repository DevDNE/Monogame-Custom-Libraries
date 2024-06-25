using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Entities
{
  public abstract class Entity
  {
    public abstract void LoadContent(ContentManager content);
    public abstract void UnloadContent();
    public abstract void Update(GameTime gameTime);
  }
}