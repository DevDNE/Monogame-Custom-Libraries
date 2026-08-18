using System;
using System.Collections.Generic;

namespace MonoGame.GameFramework.Pooling;

public class ObjectPool<T> where T : class
{
  private readonly Stack<T> _available = new();
  private readonly Func<T> _factory;
  private readonly Action<T> _onRent;
  private readonly Action<T> _onReturn;

  public int AvailableCount => _available.Count;

  public ObjectPool(Func<T> factory, int prewarm = 0, Action<T> onRent = null, Action<T> onReturn = null)
  {
    _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    _onRent = onRent;
    _onReturn = onReturn;
    for (int i = 0; i < prewarm; i++)
    {
      T item = _factory();
#if DEBUG
      _pooled.Add(item);
#endif
      _available.Push(item);
    }
  }

#if DEBUG
  // Debug-only double-return detector. Returning the same instance twice
  // pushes it twice, and the pool then hands one object to two callers who
  // both believe they own it — a bug that shows up as an entity teleporting
  // rather than as an exception. Reference identity, so entities need no
  // Equals/GetHashCode. Compiled out of Release: the check costs a hash lookup
  // on a path that runs per projectile per frame.
  private readonly HashSet<T> _pooled = new(ReferenceEqualityComparer.Instance as IEqualityComparer<T>);
#endif

  public T Rent()
  {
    T item = _available.Count > 0 ? _available.Pop() : _factory();
#if DEBUG
    _pooled.Remove(item);
#endif
    _onRent?.Invoke(item);
    return item;
  }

  public void Return(T item)
  {
    if (item == null) return;
#if DEBUG
    if (!_pooled.Add(item))
      throw new InvalidOperationException(
        $"{typeof(T).Name} returned to the pool twice. The second Return would hand one instance to two callers.");
#endif
    _onReturn?.Invoke(item);
    _available.Push(item);
  }

  public void Clear()
  {
    _available.Clear();
#if DEBUG
    _pooled.Clear();
#endif
  }
}
