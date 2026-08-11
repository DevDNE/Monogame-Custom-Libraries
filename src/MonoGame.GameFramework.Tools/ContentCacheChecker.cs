namespace MonoGame.GameFramework.Tools;

/// <summary>
/// Detects stale MonoGame content-pipeline artifacts. Documented failure
/// mode (FINDINGS.md §1.10): MGCB's incremental cache silently skips
/// rebuilding a .xnb when the source .spritefont's XML shape is preserved
/// but semantics changed (e.g. CharacterRegion added). Build passes green;
/// the game still crashes at runtime with an uncovered-glyph
/// ArgumentException from SpriteFont.MeasureString.
///
/// Check is timestamp-based: for each source asset under Content/, find
/// the matching compiled .xnb under Content/bin/<platform>/Content/, and
/// flag source-newer-than-artifact. If the artifact doesn't exist yet,
/// skip (pre-build state, not a failure).
///
/// Today we only cover *.spritefont since that's where the documented
/// incident lives. Other source formats (png/wav/ogg) are content-hashed
/// by MGCB and don't hit this class of bug.
/// </summary>
public static class ContentCacheChecker
{
  public readonly record struct StaleAsset(
    string SourcePath,
    string ArtifactPath,
    DateTime SourceTime,
    DateTime ArtifactTime);

  public sealed record CheckResult(
    IReadOnlyList<StaleAsset> StaleAssets,
    bool HadCompiledArtifacts);

  public static CheckResult Check(string projectDir)
  {
    string contentDir = Path.Combine(projectDir, "Content");
    string binDir = Path.Combine(contentDir, "bin");
    if (!Directory.Exists(contentDir) || !Directory.Exists(binDir))
      return new CheckResult(Array.Empty<StaleAsset>(), false);

    // Pick the first platform subdir (today samples ship one: DesktopGL).
    string platformDir = Directory.EnumerateDirectories(binDir).FirstOrDefault();
    if (platformDir == null) return new CheckResult(Array.Empty<StaleAsset>(), false);

    // Compiled layout is Content/bin/<platform>/Content/<relPath>.xnb.
    string compiledRoot = Path.Combine(platformDir, "Content");
    if (!Directory.Exists(compiledRoot)) compiledRoot = platformDir;

    List<StaleAsset> stale = new();
    foreach (string source in Directory.EnumerateFiles(contentDir, "*.spritefont", SearchOption.AllDirectories))
    {
      string rel = Path.GetRelativePath(contentDir, source);
      if (rel.StartsWith("bin") || rel.StartsWith("obj")) continue;
      string xnbRel = Path.ChangeExtension(rel, ".xnb");
      string xnbPath = Path.Combine(compiledRoot, xnbRel);
      if (!File.Exists(xnbPath)) continue;
      DateTime srcTime = File.GetLastWriteTimeUtc(source);
      DateTime xnbTime = File.GetLastWriteTimeUtc(xnbPath);
      if (srcTime > xnbTime)
      {
        stale.Add(new StaleAsset(source, xnbPath, srcTime, xnbTime));
      }
    }
    return new CheckResult(stale, true);
  }
}
