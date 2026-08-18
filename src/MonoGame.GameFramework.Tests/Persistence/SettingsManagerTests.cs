using System.IO;
using FluentAssertions;
using MonoGame.GameFramework.Persistence;
using Xunit;

namespace MonoGame.GameFramework.Tests.Persistence;

/// <summary>
/// SettingsManager loads during boot, so anything it throws is a crash before a
/// window exists. It used to throw on malformed JSON and to dereference null on
/// a file containing the literal <c>null</c>.
/// </summary>
public class SettingsManagerTests
{
  static string TempFile(string contents)
  {
    string path = Path.Combine(Path.GetTempPath(), $"mgf-settings-{Path.GetRandomFileName()}.json");
    File.WriteAllText(path, contents);
    return path;
  }

  [Fact]
  public void Defaults_AreAppliedWithoutAFile()
  {
    SettingsManager s = new();
    s.WindowWidth.Should().Be(800);
    s.WindowHeight.Should().Be(600);
    s.IsFullScreen.Should().BeFalse();
    s.IsBorderless.Should().BeFalse();
  }

  [Fact]
  public void SaveThenLoad_RoundTrips()
  {
    string path = Path.Combine(Path.GetTempPath(), $"mgf-settings-{Path.GetRandomFileName()}.json");
    SettingsManager saved = new(path)
    {
      WindowTitle = "Roguelike",
      WindowWidth = 1280,
      WindowHeight = 720,
      IsFullScreen = true,
      IsBorderless = true,
    };
    saved.SaveSettings();

    SettingsManager loaded = new(path);
    loaded.LoadSettings().Should().BeTrue();
    loaded.WindowTitle.Should().Be("Roguelike");
    loaded.WindowWidth.Should().Be(1280);
    loaded.WindowHeight.Should().Be(720);
    loaded.IsFullScreen.Should().BeTrue();
    loaded.IsBorderless.Should().BeTrue();

    File.Delete(path);
  }

  [Fact]
  public void CorruptJson_KeepsDefaultsInsteadOfThrowing()
  {
    string path = TempFile("{ this is not json");
    SettingsManager s = new(path);

    s.Invoking(x => x.LoadSettings()).Should().NotThrow();
    s.LoadSettings().Should().BeFalse();
    s.WindowWidth.Should().Be(800);
    s.WindowHeight.Should().Be(600);

    File.Delete(path);
  }

  [Fact]
  public void TruncatedJson_KeepsDefaultsInsteadOfThrowing()
  {
    string path = TempFile("{\"WindowWidth\":128");
    SettingsManager s = new(path);

    s.LoadSettings().Should().BeFalse();
    s.WindowWidth.Should().Be(800);

    File.Delete(path);
  }

  [Fact]
  public void LiteralNullFile_KeepsDefaultsInsteadOfThrowing()
  {
    string path = TempFile("null");
    SettingsManager s = new(path);

    s.Invoking(x => x.LoadSettings()).Should().NotThrow();
    s.LoadSettings().Should().BeFalse();
    s.WindowWidth.Should().Be(800);

    File.Delete(path);
  }

  [Fact]
  public void MissingFile_ReturnsFalse()
  {
    SettingsManager s = new(Path.Combine(Path.GetTempPath(), "mgf-settings-does-not-exist.json"));
    s.LoadSettings().Should().BeFalse();
  }

  [Fact]
  public void NullPath_IsANoOpForBothDirections()
  {
    SettingsManager s = new();
    s.Invoking(x => x.SaveSettings()).Should().NotThrow();
    s.LoadSettings().Should().BeFalse();
  }

  [Fact]
  public void Defaults_LeaveEveryVolumeAtFull()
  {
    SettingsManager s = new();
    s.MasterVolume.Should().Be(1f);
    s.SoundVolume.Should().Be(1f);
    s.MusicVolume.Should().Be(1f);
  }

  [Fact]
  public void Volumes_RoundTripThroughTheFile()
  {
    string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
    try
    {
      SettingsManager saved = new(path) { MasterVolume = 0.6f, SoundVolume = 0.25f, MusicVolume = 0f };
      saved.SaveSettings();

      SettingsManager loaded = new(path);
      loaded.LoadSettings().Should().BeTrue();
      loaded.MasterVolume.Should().BeApproximately(0.6f, 1e-6f);
      loaded.SoundVolume.Should().BeApproximately(0.25f, 1e-6f);
      loaded.MusicVolume.Should().Be(0f);
    }
    finally
    {
      if (File.Exists(path)) File.Delete(path);
    }
  }

  [Theory]
  [InlineData("2.5", 1f)]
  [InlineData("-1", 0f)]
  [InlineData("NaN", 0f)]
  public void AHandEditedVolume_IsClampedOnLoad(string written, float expected)
  {
    // The file is plain JSON a player can open. An out-of-range number reaching
    // XNA is a throw; NaN is worse, because it multiplies through and silences
    // the game with no error at all.
    string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
    try
    {
      File.WriteAllText(path, "{\"MasterVolume\":" + written + "}");
      SettingsManager loaded = new(path);
      loaded.LoadSettings().Should().BeTrue();
      loaded.MasterVolume.Should().Be(expected);
    }
    finally
    {
      if (File.Exists(path)) File.Delete(path);
    }
  }

  [Fact]
  public void ApplyTo_CopiesEveryLevelOntoTheSoundManager()
  {
    SettingsManager settings = new() { MasterVolume = 0.5f, SoundVolume = 0.25f, MusicVolume = 0.75f };
    MonoGame.GameFramework.Audio.SoundManager sound = new();

    settings.ApplyTo(sound);

    sound.MasterVolume.Should().BeApproximately(0.5f, 1e-6f);
    sound.SoundVolume.Should().BeApproximately(0.25f, 1e-6f);
    sound.MusicVolume.Should().BeApproximately(0.75f, 1e-6f);
  }

  [Fact]
  public void ApplyTo_WithNoSoundManager_DoesNothing()
  {
    SettingsManager settings = new();
    System.Action apply = () => settings.ApplyTo(null);
    apply.Should().NotThrow();
  }
}
