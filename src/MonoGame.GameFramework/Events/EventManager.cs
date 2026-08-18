using System;
using System.Collections.Generic;
using MonoGame.GameFramework.Events;

namespace MonoGame.GameFramework.Events;
public class EventManager
{
  private readonly Dictionary<string, EventHandler<GameEventArgs>> eventHandlers = new();
  private readonly Dictionary<Type, Delegate> typedHandlers = new();

  /// <summary>
  /// Fires after any event is dispatched — string or typed. Intended for
  /// diagnostics (debug overlay event tail, telemetry) that want to observe
  /// the bus without pre-subscribing to every event name.
  /// </summary>
  public event Action<string, object, GameEventArgs> AnyEvent;

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
    if (!eventHandlers.TryGetValue(eventName, out EventHandler<GameEventArgs> existing)) return;
    EventHandler<GameEventArgs> updated = existing - handler;
    // Drop the key once the last handler goes, rather than leaving a null
    // value behind. TriggerEvent null-guards either way, but a long session
    // that subscribes and unsubscribes per state would otherwise grow this
    // dictionary forever. Matches what Unsubscribe<T> below already does.
    if (updated == null) eventHandlers.Remove(eventName);
    else eventHandlers[eventName] = updated;
  }

  /// <summary>
  /// Whether anything is currently subscribed to <paramref name="eventName"/>.
  /// Also the observable form of "unsubscribing the last handler drops the key
  /// rather than leaving a null behind".
  /// </summary>
  public bool HasSubscribers(string eventName)
    => eventHandlers.TryGetValue(eventName, out EventHandler<GameEventArgs> h) && h != null;

  /// <summary>Whether anything is currently subscribed to <typeparamref name="T"/>.</summary>
  public bool HasSubscribers<T>() where T : class
    => typedHandlers.TryGetValue(typeof(T), out Delegate d) && d != null;

  public void TriggerEvent(string eventName, object sender, GameEventArgs args)
  {
    if (eventHandlers.ContainsKey(eventName))
    {
      eventHandlers[eventName]?.Invoke(sender, args);
    }
    AnyEvent?.Invoke(eventName, sender, args);
  }

  public void Subscribe<T>(Action<T> handler) where T : class
  {
    if (typedHandlers.TryGetValue(typeof(T), out Delegate existing))
      typedHandlers[typeof(T)] = Delegate.Combine(existing, handler);
    else
      typedHandlers[typeof(T)] = handler;
  }

  public void Unsubscribe<T>(Action<T> handler) where T : class
  {
    if (!typedHandlers.TryGetValue(typeof(T), out Delegate existing)) return;
    Delegate updated = Delegate.Remove(existing, handler);
    if (updated == null) typedHandlers.Remove(typeof(T));
    else typedHandlers[typeof(T)] = updated;
  }

  public void Publish<T>(T payload) where T : class
  {
    if (typedHandlers.TryGetValue(typeof(T), out Delegate existing))
      ((Action<T>)existing)?.Invoke(payload);

    // Only build the diagnostic args when something is actually listening.
    // AnyEvent is normally unsubscribed outside the debug overlay, and a
    // combat loop publishing a damage event per tick should not allocate a
    // name string and an args object for nobody.
    Action<string, object, GameEventArgs> any = AnyEvent;
    if (any == null) return;
    string name = typeof(T).Name;
    any(name, payload, new GameEventArgs(name));
  }
}
