using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Managers;
using Graphics;
using GameEvents;

namespace UI;
public class ConsoleUI
{
  private UIManager _uiManager;
  private TextManager _textManager;
  private SettingsManager _settingsManager;
  private Texture2D backgroundTexture;
  private Rectangle backgroundRectangle;
  private SpriteSheet consoleSpriteSheet;
  private int consoleHeight = 125;
  private EventManager _eventManager;

  public ConsoleUI(GraphicsDevice graphicsDevice, ServiceProvider serviceProvider)
  {
    _uiManager = serviceProvider.GetService<UIManager>();
    _textManager = serviceProvider.GetService<TextManager>();
    _settingsManager = serviceProvider.GetService<SettingsManager>();
    _eventManager = serviceProvider.GetService<EventManager>();
    
    // Create a black transparent texture for the background
    backgroundTexture = new Texture2D(graphicsDevice, 1, 1);
    backgroundTexture.SetData(new[] { Color.Black * 0.5f });

    // Set the position and size of the background rectangle
    backgroundRectangle = new Rectangle(0, 0, _settingsManager.GetWindowWidth(), consoleHeight);

    consoleSpriteSheet = new SpriteSheet("console", backgroundTexture, new Vector2(0, 0), _settingsManager.GetWindowWidth(), consoleHeight, new Rectangle[] { backgroundRectangle }, backgroundRectangle, 0, 0);
    _uiManager.AddUIElement("console", consoleSpriteSheet);

    _eventManager.Subscribe("toggleConsole", ToggleConsole);
  }

  public void UnloadContent()
  {
    _eventManager.Unsubscribe("toggleConsole", ToggleConsole);
    if (backgroundTexture != null)
    {
      backgroundTexture.Dispose();
      backgroundTexture = null;
    }
    _uiManager.RemoveUIElement("console", consoleSpriteSheet);
    backgroundTexture = null;
  }
  
  public void ToggleConsole(object sender, GameEventArgs e)
  {
    // Toggle the visibility of the console
    consoleSpriteSheet.DestinationFrame = new Rectangle(0, 0, consoleSpriteSheet.DestinationFrame.Width, consoleSpriteSheet.DestinationFrame.Height == 0 ? consoleHeight : 0);
    _uiManager.UpdateUIElement("console", consoleSpriteSheet);
    // TODO: Text Remains on screen when console is closed if not cleared.
    _textManager.ClearGroup("console");
  }

  public void Log(string message)
  {
    if (IsConsoleOpen())
    {
      _textManager.AddText("console", message, new Vector2(0, 0), Color.White);
      _textManager.ScrollText("console", 20, 5);
    }
  }

  private bool IsConsoleOpen(){
    return consoleSpriteSheet.DestinationFrame.Height > 0;
  }
}