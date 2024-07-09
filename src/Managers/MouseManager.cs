using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Managers
{
  public class MouseInput
  {
    private MouseState previousMouseState;
    private MouseState currentMouseState;

    public void Update()
    {
      previousMouseState = currentMouseState;
      currentMouseState = Mouse.GetState();
    }
    public Vector2 GetMousePosition()
    {
      return new Vector2(currentMouseState.X, currentMouseState.Y);
    }
    public bool IsLeftMouseButtonDown()
    {
      return currentMouseState.LeftButton.HasFlag(ButtonState.Pressed);
    }
    public bool IsRightMouseButtonDown()
    {
      return currentMouseState.RightButton.HasFlag(ButtonState.Pressed);
    }
    public bool IsMiddleMouseButtonDown()
    {
      return currentMouseState.MiddleButton.HasFlag(ButtonState.Pressed);
    }
    public bool WasLeftMouseButtonPressed()
    {
      return !previousMouseState.LeftButton.HasFlag(ButtonState.Pressed) && currentMouseState.LeftButton.HasFlag(ButtonState.Pressed);
    }
    public bool WasRightMouseButtonPressed()
    {
      return !previousMouseState.RightButton.HasFlag(ButtonState.Pressed) && currentMouseState.RightButton.HasFlag(ButtonState.Pressed);;
    }
    public bool WasMiddleMouseButtonPressed()
    {
      return !previousMouseState.MiddleButton.HasFlag(ButtonState.Pressed) && currentMouseState.MiddleButton.HasFlag(ButtonState.Pressed);;
    }
    public bool WasLeftMouseButtonReleased()
    {
      return previousMouseState.LeftButton.HasFlag(ButtonState.Pressed) && !currentMouseState.LeftButton.HasFlag(ButtonState.Pressed);
    }
    public bool WasRightMouseButtonReleased()
    {
      return previousMouseState.RightButton.HasFlag(ButtonState.Pressed) && !currentMouseState.RightButton.HasFlag(ButtonState.Pressed);
    }
    public bool WasMiddleMouseButtonReleased()
    {
      return previousMouseState.MiddleButton.HasFlag(ButtonState.Pressed) && !currentMouseState.MiddleButton.HasFlag(ButtonState.Pressed);
    }
    public bool IsMouseScrollWheelUp()
    {
      return previousMouseState.ScrollWheelValue < currentMouseState.ScrollWheelValue;
    }
    public bool IsMouseScrollWheelDown()
    {
      return previousMouseState.ScrollWheelValue > currentMouseState.ScrollWheelValue;
    }
  }
}