using System;
using System.Collections.Generic;
using GameEvents;

namespace Managers;
public class EventManager
{
  private readonly Dictionary<string, EventHandler<GameEventArgs>> eventHandlers = new();

  public void Subscribe(string eventName, EventHandler<GameEventArgs> handler)
  {
    if (!eventHandlers.ContainsKey(eventName))
    {
      eventHandlers[eventName] = handler;
    }
    else
    {
      eventHandlers[eventName] += handler;
    }
  }

  public void Unsubscribe(string eventName, EventHandler<GameEventArgs> handler)
  {
    if (eventHandlers.ContainsKey(eventName))
    {
      eventHandlers[eventName] -= handler;
    }
  }

  public void TriggerEvent(string eventName, object sender, GameEventArgs args)
  {
    if (eventHandlers.ContainsKey(eventName))
    {
      eventHandlers[eventName]?.Invoke(sender, args);
    }
  }
}