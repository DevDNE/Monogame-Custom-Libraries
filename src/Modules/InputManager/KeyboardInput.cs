using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace InputManager
{
  public class KeyboardInput
  {
    private KeyboardState previousKeyboardState;
    private KeyboardState currentKeyboardState;

    public void Update()
    {
      previousKeyboardState = currentKeyboardState;
      currentKeyboardState = Keyboard.GetState();
    }
    public bool IsKeyDown(Keys key)
    {
      return currentKeyboardState.IsKeyDown(key);
    }
    public bool IsKeyUp(Keys key)
    {
      return currentKeyboardState.IsKeyUp(key);
    }
    public bool WasKeyPressed(Keys key)
    {
      return previousKeyboardState.IsKeyUp(key) && currentKeyboardState.IsKeyDown(key);

    }
    public bool WasKeyReleased(Keys key)
    {
      return previousKeyboardState.IsKeyDown(key) && currentKeyboardState.IsKeyUp(key);
    }
  }
}