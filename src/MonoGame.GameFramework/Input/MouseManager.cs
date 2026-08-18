using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MonoGame.GameFramework.Input;
public class MouseManager
{
  private MouseState previousMouseState;
  private MouseState currentMouseState;

  /// <summary>
  /// Applied to every position <see cref="GetMousePosition"/> reports. Null
  /// reports raw window pixels.
  ///
  /// A game rendering through a <c>ScreenScaler</c> thinks in design pixels
  /// while the mouse arrives in window pixels, and the two stop agreeing the
  /// moment the window is resized or goes full screen. Set this once
  /// (<c>mouse.PositionTransform = scaler.WindowToVirtual</c>) rather than
  /// converting at every hit-test, because the call sites that would have to
  /// remember are UIManager, every grid pick, and every mouse-aimed shot.
  ///
  /// A delegate rather than a ScreenScaler reference so Input keeps no
  /// dependency on Rendering, and so a game with some other mapping -- a split
  /// screen, a game rendered into a panel -- can supply its own.
  /// </summary>
  public Func<Vector2, Vector2> PositionTransform { get; set; }

  public void Update()
  {
    previousMouseState = currentMouseState;
    currentMouseState = Mouse.GetState();
  }

  /// <summary>Mouse position in game coordinates, after <see cref="PositionTransform"/>.</summary>
  public Vector2 GetMousePosition()
  {
    Vector2 window = GetWindowMousePosition();
    return PositionTransform == null ? window : PositionTransform(window);
  }

  /// <summary>Raw position in window pixels, before any transform.</summary>
  public Vector2 GetWindowMousePosition()
  {
    return new Vector2(currentMouseState.X, currentMouseState.Y);
  }
  public bool IsLeftMouseButtonDown() => currentMouseState.LeftButton == ButtonState.Pressed;
  public bool IsRightMouseButtonDown() => currentMouseState.RightButton == ButtonState.Pressed;
  public bool IsMiddleMouseButtonDown() => currentMouseState.MiddleButton == ButtonState.Pressed;
  public bool WasLeftMouseButtonPressed()
    => previousMouseState.LeftButton == ButtonState.Released && currentMouseState.LeftButton == ButtonState.Pressed;
  public bool WasRightMouseButtonPressed()
    => previousMouseState.RightButton == ButtonState.Released && currentMouseState.RightButton == ButtonState.Pressed;
  public bool WasMiddleMouseButtonPressed()
    => previousMouseState.MiddleButton == ButtonState.Released && currentMouseState.MiddleButton == ButtonState.Pressed;
  public bool WasLeftMouseButtonReleased()
    => previousMouseState.LeftButton == ButtonState.Pressed && currentMouseState.LeftButton == ButtonState.Released;
  public bool WasRightMouseButtonReleased()
    => previousMouseState.RightButton == ButtonState.Pressed && currentMouseState.RightButton == ButtonState.Released;
  public bool WasMiddleMouseButtonReleased()
    => previousMouseState.MiddleButton == ButtonState.Pressed && currentMouseState.MiddleButton == ButtonState.Released;
  public bool IsMouseScrollWheelUp()
    => previousMouseState.ScrollWheelValue < currentMouseState.ScrollWheelValue;
  public bool IsMouseScrollWheelDown()
    => previousMouseState.ScrollWheelValue > currentMouseState.ScrollWheelValue;
}
