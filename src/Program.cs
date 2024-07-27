using Microsoft.Extensions.DependencyInjection;
using Managers;

public class Program
{
    public static void Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<DrawManager>()
            .AddSingleton<EventManager>()
            .AddSingleton<GamePadManager>()
            .AddSingleton<GameStateManager>()
            .AddSingleton<KeyboardManager>()
            .AddSingleton<MouseManager>()
            .AddSingleton<SceneManager>()
            .AddSingleton<SettingsManager>()
            .AddSingleton<SoundManager>()
            .AddSingleton<SteamworksManager>()
            .AddSingleton<TextManager>()
            .AddSingleton<UIManager>()
            .BuildServiceProvider();

        using var game = new MyGame.Game1(serviceProvider);
        game.Run();
    }
}