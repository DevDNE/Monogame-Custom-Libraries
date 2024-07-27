using Microsoft.Xna.Framework;
namespace GameStates;
public abstract class GameState
{
    public bool IsActive { get; set; }
    public abstract void Entered();
    public abstract void Leaving();
    public abstract void Obscuring();
    public abstract void Revealed();
    public abstract void Update(GameTime gameTime);
}