using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MonoGame.GameFramework.Audio;
using MonoGame.GameFramework.Content;
using MonoGame.GameFramework.Core;
using MonoGame.GameFramework.Debugging;
using MonoGame.GameFramework.Events;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Persistence;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.Testing;
using MonoGame.GameFramework.Text;
using MonoGame.GameFramework.Timing;
using MonoGame.GameFramework.UI;
using Xunit;

namespace MonoGame.GameFramework.Tests.Core;

/// <summary>
/// The container every sample boots from had no test. A dropped registration or
/// a constructor-injection cycle would surface as a null service at boot in one
/// unlucky game, which is the most expensive place to find it.
/// </summary>
public class ServiceCollectionExtensionsTests
{
  static ServiceProvider Build()
    => new ServiceCollection().AddGameFrameworkManagers().BuildServiceProvider();

  // Every type AddGameFrameworkManagers registers. A service added there without
  // being added here is the one gap this file cannot close on its own.
  static readonly Type[] Registered =
  {
    typeof(SettingsManager), typeof(AssetCatalog), typeof(ILogger), typeof(SaveSystem),
    typeof(TimerManager), typeof(DrawManager), typeof(EventManager), typeof(GamePadManager),
    typeof(GameStateManager), typeof(KeyboardManager), typeof(MouseManager), typeof(SceneManager),
    typeof(SoundManager), typeof(TextManager), typeof(UIManager), typeof(DebugOverlay),
    typeof(SmokeHarness),
  };

  public static TheoryData<Type> RegisteredServices
  {
    get
    {
      TheoryData<Type> data = new();
      foreach (Type t in Registered) data.Add(t);
      return data;
    }
  }

  [Theory]
  [MemberData(nameof(RegisteredServices))]
  public void EveryRegisteredService_Resolves(Type service)
  {
    using ServiceProvider sp = Build();
    sp.GetService(service).Should().NotBeNull($"{service.Name} is registered by AddGameFrameworkManagers");
  }

  [Fact]
  public void EveryServiceRegistered_IsCoveredByThisFile()
  {
    // Guards the list above against drift: if AddGameFrameworkManagers grows a
    // registration, the count moves and this test says so.
    ServiceCollection services = new();
    services.AddGameFrameworkManagers();
    services.Count.Should().Be(Registered.Length);
  }

  [Fact]
  public void EveryRegisteredService_IsASingleton()
  {
    using ServiceProvider sp = Build();
    foreach (Type service in Registered)
    {
      sp.GetService(service).Should().BeSameAs(sp.GetService(service), $"{service.Name} should be a singleton");
    }
  }

  [Fact]
  public void DebugOverlay_GetsTheSameManagersEveryoneElseDoes()
  {
    // The overlay reports state depth, UI count and timer count. If it were
    // handed its own instances those numbers would always read zero.
    using ServiceProvider sp = Build();
    DebugOverlay overlay = sp.GetRequiredService<DebugOverlay>();
    GameStateManager states = sp.GetRequiredService<GameStateManager>();

    overlay.Should().NotBeNull();
    states.PushState(new NoopState());
    sp.GetRequiredService<GameStateManager>().StackDepth.Should().Be(1);
  }

  [Fact]
  public void SettingsFilePath_FlowsThroughToTheRegisteredInstance()
  {
    using ServiceProvider sp = new ServiceCollection()
      .AddGameFrameworkManagers("some/path/settings.json")
      .BuildServiceProvider();

    sp.GetRequiredService<SettingsManager>().Should().NotBeNull();
  }

  private class NoopState : GameState
  {
    public override void Entered() { }
    public override void Leaving() { }
    public override void Obscuring() { }
    public override void Revealed() { }
    public override void Update(Microsoft.Xna.Framework.GameTime gameTime) { }
  }
}
