using System;
using System.IO;
using Newtonsoft.Json;

namespace MonoGame.GameFramework.Persistence;
public class SettingsManager
{
  private readonly string settingsFilePath;
  public string WindowTitle { get; set; }
  public int WindowWidth { get; set; }
  public int WindowHeight { get; set; }
  public bool IsFullScreen { get; set; }
  public bool IsBorderless { get; set; }

  // Volume lives here rather than only on SoundManager because it is the one
  // audio setting a player expects to survive a restart, and this file is
  // already the thing that survives a restart. Stored raw and clamped on load:
  // a hand-edited settings file is the reason SettingsManager has a failure
  // path at all.
  public float MasterVolume { get; set; }
  public float SoundVolume { get; set; }
  public float MusicVolume { get; set; }

  public SettingsManager(string settingsFilePath = null)
  {
    WindowTitle = AppDomain.CurrentDomain.FriendlyName;
    WindowWidth = 800;
    WindowHeight = 600;
    IsFullScreen = false;
    IsBorderless = false;
    MasterVolume = 1f;
    SoundVolume = 1f;
    MusicVolume = 1f;
    this.settingsFilePath = settingsFilePath;
  }

  private static float Clamp01(float value)
  {
    // NaN fails both comparisons, so it lands on the default rather than
    // propagating into a volume multiply and silencing the game.
    if (!(value > 0f)) return 0f;
    return value > 1f ? 1f : value;
  }

  /// <summary>
  /// Copy the persisted levels onto a <c>SoundManager</c>. One call, so a game
  /// does not have to remember three assignments in the right order.
  /// </summary>
  public void ApplyTo(Audio.SoundManager sound)
  {
    if (sound == null) return;
    sound.MasterVolume = MasterVolume;
    sound.SoundVolume = SoundVolume;
    sound.MusicVolume = MusicVolume;
  }

  public void SaveSettings()
  {
    if (settingsFilePath == null) return;
    string json = JsonConvert.SerializeObject(this);
    File.WriteAllText(settingsFilePath, json);
  }

  /// <summary>
  /// Load settings over the constructor defaults. Anything unreadable — corrupt
  /// JSON, a file containing the literal <c>null</c>, an IO failure — leaves the
  /// defaults in place and returns false. This runs during boot, so a throw here
  /// is a crash before a window exists, which is the hardest kind to diagnose
  /// from a user report.
  /// </summary>
  public bool LoadSettings()
  {
    if (settingsFilePath == null || !File.Exists(settingsFilePath)) return false;

    SettingsManager settings;
    try
    {
      string json = File.ReadAllText(settingsFilePath);
      settings = JsonConvert.DeserializeObject<SettingsManager>(json);
    }
    catch (JsonException)
    {
      return false;
    }
    catch (IOException)
    {
      return false;
    }

    if (settings == null) return false;

    WindowTitle = settings.WindowTitle;
    WindowWidth = settings.WindowWidth;
    WindowHeight = settings.WindowHeight;
    IsFullScreen = settings.IsFullScreen;
    IsBorderless = settings.IsBorderless;
    MasterVolume = Clamp01(settings.MasterVolume);
    SoundVolume = Clamp01(settings.SoundVolume);
    MusicVolume = Clamp01(settings.MusicVolume);
    return true;
  }
}
