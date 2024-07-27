using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameStates;

namespace Managers;
public class GameStateManager
{
  private Stack<GameState> stateStack = new Stack<GameState>();
  public void ChangeState(GameState newState)
  {
    stateStack.Pop();
    stateStack.Push(newState);
  }
  public void PopState()
  {
    stateStack.Pop();
  }
  public void PushState(GameState newState)
  {
    stateStack.Push(newState);
  }

  public GameState PeekState()
  {
    return stateStack.Peek();
  }

  public void Update(GameTime gameTime)
  {
    foreach (GameState state in stateStack)
    {
      if (state.IsActive)
      {
        state.Update(gameTime);
      }
    }
  }
}