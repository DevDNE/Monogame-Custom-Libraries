using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Managers;
public class GamePadManager
{
  private GamePadState previousGamePadState;
  private GamePadState currentGamePadState;

  public void Update()
  {
    previousGamePadState = currentGamePadState;
    currentGamePadState = GamePad.GetState(PlayerIndex.One);
  }
  public bool IsGamePadButtonDown(Buttons button)
  {
    return currentGamePadState.IsButtonDown(button);
  }
  public bool IsGamePadButtonUp(Buttons button)
  {
    return currentGamePadState.IsButtonUp(button);
  }
  public bool WasGamePadButtonPressed(Buttons button)
  {
    return previousGamePadState.IsButtonUp(button) && currentGamePadState.IsButtonDown(button);
  }
  public bool WasGamePadButtonReleased(Buttons button)
  {
    return previousGamePadState.IsButtonDown(button) && currentGamePadState.IsButtonUp(button);
  }
  public float GetGamePadLeftThumbstickX()
  {
    return currentGamePadState.ThumbSticks.Left.X;
  }
  public float GetGamePadLeftThumbstickY()
  {
    return currentGamePadState.ThumbSticks.Left.Y;
  }
  public float GetGamePadRightThumbstickX()
  {
    return currentGamePadState.ThumbSticks.Right.X;
  }
  public float GetGamePadRightThumbstickY()
  {
    return currentGamePadState.ThumbSticks.Right.Y;
  }
  public float GetGamePadLeftTrigger()
  {
    return currentGamePadState.Triggers.Left;
  }
  public float GetGamePadRightTrigger()
  {
    return currentGamePadState.Triggers.Right;
  }
  public bool IsGamePadConnected()
  {
    return currentGamePadState.IsConnected;
  }
  public bool WasGamePadDisconnected()
  {
    return previousGamePadState.IsConnected && !currentGamePadState.IsConnected;
  }
}