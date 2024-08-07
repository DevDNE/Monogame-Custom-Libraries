using System;
using System.IO;
using Newtonsoft.Json;
using dotenv.net;

// TODO: Settings are not being loaded from the json file

namespace Managers;
public class SettingsManager
{
  private readonly string settingsFilePath;
  public string WindowTitle { get; set; }
  public int WindowWidth { get; set; }
  public int WindowHeight { get; set; }
  public bool IsFullScreen { get; set; }
  public bool IsBorderless { get; set; }
  public bool DebugMode { get; set; }
  public SettingsManager()
  {
    WindowTitle = "My Game - Custom Library";
    WindowWidth = 800;
    WindowHeight = 600;
    IsFullScreen = false;
    IsBorderless = false;
    DebugMode = true;
    DotEnv.Load(new DotEnvOptions(envFilePaths: new[] { "..\\..\\..\\.env" }));
    settingsFilePath = Environment.GetEnvironmentVariable("SETTINGS_FILE_PATH");
  }

  public void SaveSettings()
  {
    string json = JsonConvert.SerializeObject(this);
    File.WriteAllText(settingsFilePath, json);
  }

  public void LoadSettings()
  {
    if (File.Exists(settingsFilePath))
    {
      string json = File.ReadAllText(settingsFilePath);
      SettingsManager settings = JsonConvert.DeserializeObject<SettingsManager>(json);

      // Copy loaded settings to this instance
      WindowTitle = settings.WindowTitle;
      WindowWidth = settings.WindowWidth;
      WindowHeight = settings.WindowHeight;
      IsFullScreen = settings.IsFullScreen;
      IsBorderless = settings.IsBorderless;
      DebugMode = settings.DebugMode;
    }
  }
  
  public int GetWindowWidth()
  {
    return WindowWidth;
  }
}