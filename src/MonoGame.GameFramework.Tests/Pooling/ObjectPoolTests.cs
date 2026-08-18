using FluentAssertions;
using MonoGame.GameFramework.Pooling;
using Xunit;

namespace MonoGame.GameFramework.Tests.Pooling;

public class ObjectPoolTests
{
  private class Bullet { public int Hits; }

  [Fact]
  public void Rent_EmptyPool_CreatesNewInstance()
  {
    ObjectPool<Bullet> pool = new(() => new Bullet());
    Bullet b = pool.Rent();
    b.Should().NotBeNull();
    pool.AvailableCount.Should().Be(0);
  }

  [Fact]
  public void Return_StoresForReuse()
  {
    ObjectPool<Bullet> pool = new(() => new Bullet());
    Bullet b = pool.Rent();
    pool.Return(b);
    pool.AvailableCount.Should().Be(1);
    pool.Rent().Should().BeSameAs(b);
  }

  [Fact]
  public void Prewarm_PopulatesAvailable()
  {
    ObjectPool<Bullet> pool = new(() => new Bullet(), prewarm: 5);
    pool.AvailableCount.Should().Be(5);
  }

  [Fact]
  public void Rent_InvokesOnRentCallback()
  {
    int rented = 0;
    ObjectPool<Bullet> pool = new(() => new Bullet(), onRent: _ => rented++);
    pool.Rent();
    pool.Rent();
    rented.Should().Be(2);
  }

  [Fact]
  public void Return_InvokesOnReturnCallback()
  {
    int returned = 0;
    ObjectPool<Bullet> pool = new(() => new Bullet(), onReturn: b => { b.Hits = 0; returned++; });
    Bullet b = pool.Rent();
    b.Hits = 7;
    pool.Return(b);
    returned.Should().Be(1);
    b.Hits.Should().Be(0);
  }

  [Fact]
  public void Return_NullItem_IsIgnored()
  {
    ObjectPool<Bullet> pool = new(() => new Bullet());
    pool.Return(null);
    pool.AvailableCount.Should().Be(0);
  }

  [Fact]
  public void Clear_EmptiesPool()
  {
    ObjectPool<Bullet> pool = new(() => new Bullet(), prewarm: 3);
    pool.Clear();
    pool.AvailableCount.Should().Be(0);
  }

#if DEBUG
  [Fact]
  public void Return_Twice_IsRejectedInDebugBuilds()
  {
    // Two Returns push the same instance twice, and the pool then hands one
    // object to two callers who both believe they own it — which shows up as an
    // entity teleporting, not as an exception, unless something checks.
    ObjectPool<object> pool = new(() => new object());
    object item = pool.Rent();
    pool.Return(item);

    pool.Invoking(p => p.Return(item))
      .Should().Throw<System.InvalidOperationException>()
      .WithMessage("*twice*");
  }

  [Fact]
  public void RentReturnRentReturn_IsFine()
  {
    ObjectPool<object> pool = new(() => new object());
    for (int i = 0; i < 5; i++) pool.Return(pool.Rent());
    pool.AvailableCount.Should().Be(1);
  }

  [Fact]
  public void ReturningAPrewarmedInstanceThatWasNeverRented_IsRejected()
  {
    ObjectPool<object> pool = new(() => new object(), prewarm: 1);
    object rented = pool.Rent();
    pool.Return(rented);
    pool.Invoking(p => p.Return(rented)).Should().Throw<System.InvalidOperationException>();
  }

  [Fact]
  public void Clear_ForgetsWhatItHadPooled()
  {
    ObjectPool<object> pool = new(() => new object());
    object item = pool.Rent();
    pool.Return(item);
    pool.Clear();
    pool.Invoking(p => p.Return(item)).Should().NotThrow();
  }
#endif

}
