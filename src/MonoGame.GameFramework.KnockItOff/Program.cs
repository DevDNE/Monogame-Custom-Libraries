using Microsoft.Extensions.DependencyInjection;
using MonoGame.GameFramework.Core;
using MonoGame.GameFramework.Testing;

namespace MonoGame.GameFramework.KnockItOff;

public class Program
{
  public static void Main(string[] args)
  {
    ServiceProvider serviceProvider = new ServiceCollection()
      .AddGameFrameworkManagers()
      .BuildServiceProvider();

    serviceProvider.GetService<SmokeHarness>().ExitAfterFrames = SmokeHarness.ParseExitAfter(args);

    using Game1 game = new(serviceProvider);
    game.Run();
  }
}
