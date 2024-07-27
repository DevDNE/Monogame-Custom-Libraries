// Game State Class: Responsible for handling game logic, including collision detection.
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework.Input;
using GameEvents;
using Managers;
using UI;

namespace GameStates;
public class DebugState : GameState
{
  private ConsoleUI _consoleUI;
  private KeyboardManager _keyboardManager;
  private EventManager _eventManager;
  public DebugState(GraphicsDevice graphicsDevice, ServiceProvider serviceProvider)
  {
    _consoleUI = new ConsoleUI(graphicsDevice, serviceProvider);
    _keyboardManager = serviceProvider.GetService<KeyboardManager>();
    _eventManager = serviceProvider.GetService<EventManager>();
    _eventManager.Subscribe("EnemyHit", Log);
    _eventManager.Subscribe("EnemyMiss", Log);
    _eventManager.Subscribe("PlayerFiredProjectile", Log);
    _eventManager.Subscribe("PlayerMoved", Log);

    IsActive = false;
  }
  
  public override void Entered()
  {
    IsActive = true;
  }
    // Document Leaving method
  
  public override void Leaving()
  {
    _eventManager.Unsubscribe("EnemyHit", Log);
    _eventManager.Unsubscribe("EnemyMiss", Log);
    _eventManager.Unsubscribe("PlayerFiredProjectile", Log);
    _eventManager.Unsubscribe("PlayerMoved", Log);
    _consoleUI.UnloadContent();
  }

  public override void Obscuring()
  {
    IsActive = false;
  }

  public override void Revealed()
  {
    IsActive = true;
  }

  public void Log(object sender, GameEventArgs e)
  {
    _consoleUI.Log(e.Message);
  }

  public override void Update(GameTime gameTime)
  {
    if (_keyboardManager.WasKeyPressed(Keys.OemTilde))
    {
      _eventManager.TriggerEvent("toggleConsole", this, new GameEventArgs("Toggling console"));
    }
  }
}

/**
public static void LogMessage(string message, LogLevel level)
{
  string formattedMessage = $"[{DateTime.Now}] [{level}] {message}";
  Console.WriteLine(formattedMessage);
}

public static void LogPerformanceMetrics(float fps, float memoryUsage)
{
  string metrics = $"FPS: {fps}, Memory Usage: {memoryUsage} MB";
  Console.WriteLine(metrics);
}

public enum LogLevel
{
  Debug,
  Info,
  Warning,
  Error
}
**/