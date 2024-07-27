using System;

namespace GameEvents;

public class GameEventArgs : EventArgs
{
  public string Message { get; }

  public GameEventArgs(string message)
  {
    Message = message;
  }
}