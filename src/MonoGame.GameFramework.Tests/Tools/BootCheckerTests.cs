using System.IO;
using FluentAssertions;
using MonoGame.GameFramework.Tools;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tools;

public class BootCheckerTests
{
  /// <summary>
  /// Minimal Game1.cs satisfying all four boot conventions. Individual tests
  /// strip one line at a time to prove each check fires independently.
  /// </summary>
  const string CompliantGame1 = """
    public class Game1 : Game
    {
      DebugOverlay _overlay;
      SmokeHarness _smoke;

      protected override void LoadContent()
      {
        Primitives.Initialize(GraphicsDevice);
        _overlay = _services.GetService<DebugOverlay>();
        _overlay.SetFont(_font);
        _smoke = _services.GetService<SmokeHarness>();
      }

      protected override void Update(GameTime gt)
      {
        _overlay.Update(gt);
        if (!_overlay.ShouldSkipUpdate) _gsm.Update(gt);
        if (_smoke.Tick()) Exit();
      }
    }
    """;

  static string WriteGame1(string contents)
  {
    string tmp = Directory.CreateTempSubdirectory("mgf-boot-test-").FullName;
    File.WriteAllText(Path.Combine(tmp, "Game1.cs"), contents);
    return tmp;
  }

  [Fact]
  public void CompliantGame1_HasNoMissingConventions()
  {
    var result = BootChecker.Check(WriteGame1(CompliantGame1));
    result.Missing.Should().BeEmpty();
  }

  [Fact]
  public void MissingGame1File_IsReported()
  {
    string empty = Directory.CreateTempSubdirectory("mgf-boot-test-").FullName;
    var result = BootChecker.Check(empty);
    result.Missing.Should().ContainSingle()
      .Which.Description.Should().Contain("Game1.cs not found");
  }

  [Fact]
  public void MissingPrimitivesInitialize_IsFlagged()
  {
    string src = CompliantGame1.Replace("Primitives.Initialize(GraphicsDevice);", "");
    var result = BootChecker.Check(WriteGame1(src));
    result.Missing.Should().ContainSingle()
      .Which.Description.Should().Contain("Primitives.Initialize");
  }

  [Fact]
  public void MissingSetFont_IsFlagged()
  {
    string src = CompliantGame1.Replace("_overlay.SetFont(_font);", "");
    var result = BootChecker.Check(WriteGame1(src));
    result.Missing.Should().ContainSingle()
      .Which.Description.Should().Contain("SetFont");
  }

  [Fact]
  public void MissingShouldSkipUpdateGuard_IsFlagged()
  {
    string src = CompliantGame1.Replace(
      "if (!_overlay.ShouldSkipUpdate) _gsm.Update(gt);", "_gsm.Update(gt);");
    var result = BootChecker.Check(WriteGame1(src));
    result.Missing.Should().ContainSingle()
      .Which.Description.Should().Contain("ShouldSkipUpdate");
  }

  [Fact]
  public void DebugOverlayReferencedButNotResolvedFromDi_IsFlagged()
  {
    string src = CompliantGame1.Replace(
      "_overlay = _services.GetService<DebugOverlay>();", "_overlay = new DebugOverlay();");
    var result = BootChecker.Check(WriteGame1(src));
    result.Missing.Should().ContainSingle()
      .Which.Description.Should().Contain("not resolved from DI");
  }

  [Fact]
  public void GetRequiredServiceResolution_IsAccepted()
  {
    string src = CompliantGame1.Replace(
      "_services.GetService<DebugOverlay>()", "_services.GetRequiredService<DebugOverlay>()");
    var result = BootChecker.Check(WriteGame1(src));
    result.Missing.Should().BeEmpty();
  }

  [Fact]
  public void NoDebugOverlayAtAll_ReportsSingleUmbrellaFinding()
  {
    // When the type is absent entirely, the sub-checks (DI/SetFont/guard) are
    // suppressed in favour of one actionable "wire it up" message.
    string src = CompliantGame1
      .Replace("DebugOverlay _overlay;", "")
      .Replace("_overlay = _services.GetService<DebugOverlay>();", "")
      .Replace("_overlay.SetFont(_font);", "")
      .Replace("_overlay.Update(gt);", "")
      .Replace("if (!_overlay.ShouldSkipUpdate) _gsm.Update(gt);", "_gsm.Update(gt);");
    var result = BootChecker.Check(WriteGame1(src));
    result.Missing.Should().ContainSingle()
      .Which.Description.Should().Contain("DebugOverlay not referenced");
  }

  [Fact]
  public void SmokeHarnessWithoutTick_IsFlagged()
  {
    string src = CompliantGame1.Replace("if (_smoke.Tick()) Exit();", "");
    var result = BootChecker.Check(WriteGame1(src));
    result.Missing.Should().HaveCount(2);
    result.Missing.Should().Contain(m => m.Description.Contains(".Tick() never called"));
    result.Missing.Should().Contain(m => m.Description.Contains("Exit() call missing"));
  }

  [Fact]
  public void NoSmokeHarnessAtAll_ReportsSingleUmbrellaFinding()
  {
    string src = CompliantGame1
      .Replace("SmokeHarness _smoke;", "")
      .Replace("_smoke = _services.GetService<SmokeHarness>();", "")
      .Replace("if (_smoke.Tick()) Exit();", "");
    var result = BootChecker.Check(WriteGame1(src));
    result.Missing.Should().ContainSingle()
      .Which.Description.Should().Contain("SmokeHarness not referenced");
  }
}
