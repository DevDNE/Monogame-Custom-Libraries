using FluentAssertions;
using MonoGame.GameFramework.Audio;
using Xunit;

namespace MonoGame.GameFramework.Tests.Audio;

/// <summary>
/// Playing anything needs an audio device, so what is asserted here is the
/// mixing and the clamping -- the arithmetic that decides whether a volume
/// slider does what it says, and the guards that stop a bad value reaching the
/// platform layer.
/// </summary>
public class SoundManagerTests
{
  [Fact]
  public void EffectiveVolume_MultipliesMasterCategoryAndCall()
  {
    SoundManager.EffectiveVolume(0.5f, 0.5f, 0.5f).Should().BeApproximately(0.125f, 1e-6f);
  }

  [Fact]
  public void EffectiveVolume_AtFullEverything_IsUnattenuated()
  {
    SoundManager.EffectiveVolume(1f, 1f, 1f).Should().Be(1f);
  }

  [Theory]
  [InlineData(0f, 1f, 1f)]
  [InlineData(1f, 0f, 1f)]
  [InlineData(1f, 1f, 0f)]
  public void EffectiveVolume_AnyZeroLevel_IsSilence(float master, float category, float call)
  {
    SoundManager.EffectiveVolume(master, category, call).Should().Be(0f);
  }

  [Theory]
  [InlineData(2f, 1f, 1f, 1f)]
  [InlineData(-1f, 1f, 1f, 0f)]
  [InlineData(1f, 5f, 1f, 1f)]
  [InlineData(1f, 1f, 100f, 1f)]
  public void EffectiveVolume_ClampsEveryInput(float master, float category, float call, float expected)
  {
    // XNA throws on a volume outside [0,1]; a game passing 1.5 to make
    // something louder should get the loudest available sound, not a crash.
    SoundManager.EffectiveVolume(master, category, call).Should().Be(expected);
  }

  [Theory]
  [InlineData(1.5f, 1f)]
  [InlineData(-0.2f, 0f)]
  [InlineData(0.3f, 0.3f)]
  public void VolumeProperties_ClampOnAssignment(float assigned, float expected)
  {
    SoundManager sound = new()
    {
      MasterVolume = assigned,
      SoundVolume = assigned,
      MusicVolume = assigned,
    };

    sound.MasterVolume.Should().BeApproximately(expected, 1e-6f);
    sound.SoundVolume.Should().BeApproximately(expected, 1e-6f);
    sound.MusicVolume.Should().BeApproximately(expected, 1e-6f);
  }

  [Fact]
  public void SettingVolume_BeforeAnyAudioExists_DoesNotThrow()
  {
    // Volume is set from persisted settings during boot, which happens before
    // LoadContent and therefore before there is a device to talk to. This is
    // the same class of failure SettingsManager's own try/catch exists for.
    SoundManager sound = new();
    System.Action set = () => { sound.MasterVolume = 0.4f; sound.MusicVolume = 0.2f; sound.SoundVolume = 0.9f; };
    set.Should().NotThrow();
  }

  [Fact]
  public void PlayingAnUnloadedEffect_IsStillANoOpWithTheNewArguments()
  {
    SoundManager sound = new();
    System.Action play = () => sound.PlaySoundEffect("audio/nope", volume: 0.5f, pitch: 0.2f, pan: -1f);
    play.Should().NotThrow();
  }

  [Fact]
  public void LoopingAnUnloadedEffect_ReturnsNullRatherThanThrowing()
  {
    SoundManager sound = new();
    sound.PlayLooping("audio/nope").Should().BeNull();
  }

  [Fact]
  public void StopLoops_WithNothingPlaying_IsSafe()
  {
    SoundManager sound = new();
    System.Action stop = () => sound.StopLoops();
    stop.Should().NotThrow();
  }
}
