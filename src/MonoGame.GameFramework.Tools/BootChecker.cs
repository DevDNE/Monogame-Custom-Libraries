using System.Text.RegularExpressions;

namespace MonoGame.GameFramework.Tools;

/// <summary>
/// Scans a sample's Game1.cs for four boot conventions documented in
/// CLAUDE.md (see "Debug overlay" and "Smoke harness" sections):
///   1. LoadContent calls Primitives.Initialize(GraphicsDevice) — required
///      for DrawRectangle / Pixel to work anywhere downstream.
///   2. DebugOverlay resolved from DI (GetService&lt;DebugOverlay&gt; or
///      GetRequiredService&lt;DebugOverlay&gt;) and given a font via SetFont.
///   3. GameStateManager.Update guarded by !&lt;overlay&gt;.ShouldSkipUpdate
///      so pause/step works.
///   4. SmokeHarness.Tick() called and Exit() invoked when it returns true
///      so --exit-after N headless smoke-tests actually exit.
///
/// These aren't safety-critical checks — they enforce the thinnest
/// convention in the project. Easy to forget in a hand-written 10th
/// sample; easy for Primitives.Initialize to get deleted during a refactor
/// and land a "renders fine in normal code paths, blows up the first time
/// a HpBar draws" regression.
///
/// Approach mirrors SpritefontLinter: regex on text, no compiler.
/// </summary>
public static class BootChecker
{
  public readonly record struct MissingConvention(string Description);
  public sealed record CheckResult(string Game1Path, IReadOnlyList<MissingConvention> Missing);

  static readonly Regex PrimitivesInitialize = new(@"\bPrimitives\.Initialize\s*\(", RegexOptions.Compiled);
  static readonly Regex DebugOverlayRef = new(@"\bDebugOverlay\b", RegexOptions.Compiled);
  static readonly Regex DebugOverlayResolve = new(@"GetService\s*<\s*DebugOverlay\s*>|GetRequiredService\s*<\s*DebugOverlay\s*>", RegexOptions.Compiled);
  static readonly Regex SetFontCall = new(@"\.SetFont\s*\(", RegexOptions.Compiled);
  static readonly Regex ShouldSkipUpdateGuard = new(@"!\s*\w+\.ShouldSkipUpdate", RegexOptions.Compiled);
  static readonly Regex SmokeHarnessRef = new(@"\bSmokeHarness\b", RegexOptions.Compiled);
  static readonly Regex TickCall = new(@"\b\w+\.Tick\s*\(\s*\)", RegexOptions.Compiled);
  static readonly Regex ExitCall = new(@"\bExit\s*\(\s*\)", RegexOptions.Compiled);

  public static CheckResult Check(string projectDir)
  {
    string game1 = Path.Combine(projectDir, "Game1.cs");
    List<MissingConvention> missing = new();
    if (!File.Exists(game1))
    {
      missing.Add(new MissingConvention("Game1.cs not found at project root."));
      return new CheckResult(game1, missing);
    }
    string src = File.ReadAllText(game1);

    if (!PrimitivesInitialize.IsMatch(src))
      missing.Add(new MissingConvention("LoadContent must call Primitives.Initialize(GraphicsDevice) — required for DrawRectangle / Pixel."));

    if (!DebugOverlayRef.IsMatch(src))
    {
      missing.Add(new MissingConvention("DebugOverlay not referenced. Resolve via GetService<DebugOverlay>() and wire Update/Draw + SetFont."));
    }
    else
    {
      if (!DebugOverlayResolve.IsMatch(src))
        missing.Add(new MissingConvention("DebugOverlay referenced but not resolved from DI (expected GetService<DebugOverlay>() or GetRequiredService<DebugOverlay>())."));
      if (!SetFontCall.IsMatch(src))
        missing.Add(new MissingConvention("DebugOverlay.SetFont(_font) must be called after the font loads so the overlay has a font to render with."));
      if (!ShouldSkipUpdateGuard.IsMatch(src))
        missing.Add(new MissingConvention("GameStateManager.Update must be guarded with 'if (!<overlay>.ShouldSkipUpdate) ...' so pause/step works."));
    }

    if (!SmokeHarnessRef.IsMatch(src))
    {
      missing.Add(new MissingConvention("SmokeHarness not referenced — required for headless --exit-after smoke tests."));
    }
    else
    {
      if (!TickCall.IsMatch(src))
        missing.Add(new MissingConvention("SmokeHarness referenced but .Tick() never called — --exit-after won't work."));
      if (!ExitCall.IsMatch(src))
        missing.Add(new MissingConvention("Exit() call missing — SmokeHarness.Tick() returning true must trigger Game.Exit()."));
    }

    return new CheckResult(game1, missing);
  }
}
