using System;
using System.IO;
using FluentAssertions;
using MonoGame.GameFramework.Tools;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tools;

public class ContentCacheCheckerTests
{
  static readonly DateTime Older = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
  static readonly DateTime Newer = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

  /// <summary>
  /// Builds the layout the checker expects:
  ///   &lt;proj&gt;/Content/fonts/Arial.spritefont
  ///   &lt;proj&gt;/Content/bin/DesktopGL/Content/fonts/Arial.xnb
  /// Passing null for xnbTime omits the compiled artifact entirely.
  /// </summary>
  static string WriteProject(DateTime sourceTime, DateTime? xnbTime)
  {
    string proj = Directory.CreateTempSubdirectory("mgf-cache-test-").FullName;
    string fonts = Path.Combine(proj, "Content", "fonts");
    Directory.CreateDirectory(fonts);
    string src = Path.Combine(fonts, "Arial.spritefont");
    File.WriteAllText(src, "<XnaContent />");
    File.SetLastWriteTimeUtc(src, sourceTime);

    string compiled = Path.Combine(proj, "Content", "bin", "DesktopGL", "Content", "fonts");
    Directory.CreateDirectory(compiled);
    if (xnbTime is { } stamp)
    {
      string xnb = Path.Combine(compiled, "Arial.xnb");
      File.WriteAllText(xnb, "compiled");
      File.SetLastWriteTimeUtc(xnb, stamp);
    }
    return proj;
  }

  [Fact]
  public void SourceOlderThanArtifact_IsNotStale()
  {
    var result = ContentCacheChecker.Check(WriteProject(Older, Newer));
    result.HadCompiledArtifacts.Should().BeTrue();
    result.StaleAssets.Should().BeEmpty();
  }

  [Fact]
  public void SourceNewerThanArtifact_IsStale()
  {
    var result = ContentCacheChecker.Check(WriteProject(Newer, Older));
    result.StaleAssets.Should().ContainSingle();
    var stale = result.StaleAssets[0];
    stale.SourcePath.Should().EndWith("Arial.spritefont");
    stale.ArtifactPath.Should().EndWith("Arial.xnb");
    stale.SourceTime.Should().BeAfter(stale.ArtifactTime);
  }

  [Fact]
  public void MissingArtifact_IsSkippedNotStale()
  {
    // Pre-build state: the .xnb hasn't been produced yet. Not a failure.
    var result = ContentCacheChecker.Check(WriteProject(Newer, xnbTime: null));
    result.StaleAssets.Should().BeEmpty();
  }

  [Fact]
  public void NoContentDirectory_ReportsNoCompiledArtifacts()
  {
    string bare = Directory.CreateTempSubdirectory("mgf-cache-test-").FullName;
    var result = ContentCacheChecker.Check(bare);
    result.HadCompiledArtifacts.Should().BeFalse();
    result.StaleAssets.Should().BeEmpty();
  }

  [Fact]
  public void UnbuiltProjectWithNoBinDirectory_ReportsNoCompiledArtifacts()
  {
    string proj = Directory.CreateTempSubdirectory("mgf-cache-test-").FullName;
    Directory.CreateDirectory(Path.Combine(proj, "Content", "fonts"));
    var result = ContentCacheChecker.Check(proj);
    result.HadCompiledArtifacts.Should().BeFalse();
  }

  [Fact]
  public void SpritefontUnderContentBin_IsNotTreatedAsSource()
  {
    // An intermediate copy under Content/bin must not be compared against
    // itself — only authored sources under Content/ count.
    string proj = WriteProject(Older, Newer);
    string strayDir = Path.Combine(proj, "Content", "bin", "DesktopGL", "Content", "fonts");
    string stray = Path.Combine(strayDir, "Stray.spritefont");
    File.WriteAllText(stray, "<XnaContent />");
    File.SetLastWriteTimeUtc(stray, Newer);
    File.WriteAllText(Path.Combine(strayDir, "Stray.xnb"), "compiled");
    File.SetLastWriteTimeUtc(Path.Combine(strayDir, "Stray.xnb"), Older);

    var result = ContentCacheChecker.Check(proj);
    result.StaleAssets.Should().BeEmpty();
  }
}
