using System.Text.RegularExpressions;

namespace MonoGame.GameFramework.Tools;

/// <summary>
/// Enforces the pixel-art conventions from FINDINGS §1.10 / §1.17 — the ones
/// that are silently wrong by default and produce no build error:
///
///   1. A texture built with TextureFormat=Compressed. DXT block compression
///      mangles the hard 1px colour boundaries pixel art is made of. The build
///      is green, the .xnb is valid, the art is just quietly ruined.
///   2. ResizeToPowerOfTwo / MakeSquare left True, which pads a 32x32 source
///      into something larger and shifts every source rectangle you compute.
///   3. SpriteBatch.Begin() with no arguments in a project that ships
///      textures. The default sampler is LinearClamp, which blurs pixel art at
///      any scale other than 1:1.
///
/// Deliberately self-limiting, on a rule that reads off project structure:
///
///   * .mgcb with TextureImporter blocks  — a game that ships sprites. Full
///     check: content settings and source.
///   * .mgcb with no TextureImporter blocks — a game that has opted out of
///     sprites. Skipped entirely, so a rectangle-only sample stays silent.
///   * no .mgcb at all — shared library code. It has no content of its own, so
///     no content rule can apply, but its Draw calls run inside every game that
///     *does* ship sprites. Source is scanned; content checks are skipped.
///
/// That last case was previously a bail-out on the very first line, which meant
/// MonoGame.GameFramework — the one project whose code runs in all nine games —
/// was the only place a bare Begin() could never be reported. DebugOverlay had
/// one.
/// </summary>
public static class SpriteConventionChecker
{
  public readonly record struct Violation(string File, string Description);

  public sealed record CheckResult(
    IReadOnlyList<string> TextureAssets,
    IReadOnlyList<Violation> Violations,
    bool SourceScanned = false)
  {
    public bool HasTextures => TextureAssets.Count > 0;

    /// <summary>Shared code: no content of its own, but its source was checked.</summary>
    public bool IsSharedCode => !HasTextures && SourceScanned;
  }

  // A bare Begin() — no sampler, no anything. Begin(...) with arguments is
  // left alone: judging whether some other overload passes a sampler is the
  // compiler's job, not a regex's, and a false failure in CI is worse than a
  // missed one.
  static readonly Regex BareBeginCall = new(@"\.Begin\s*\(\s*\)", RegexOptions.Compiled);

  public static CheckResult Check(string projectDir)
  {
    string mgcb = Path.Combine(projectDir, "Content", "Content.mgcb");
    List<string> textures = new();
    List<Violation> violations = new();
    bool hasContentPipeline = File.Exists(mgcb);

    foreach (ContentBlock block in hasContentPipeline
      ? ParseBlocks(File.ReadAllLines(mgcb))
      : Enumerable.Empty<ContentBlock>())
    {
      if (!block.IsTexture) continue;
      textures.Add(block.Asset);

      string format = block.Param("TextureFormat");
      if (format != null && !format.Equals("Color", StringComparison.OrdinalIgnoreCase))
        violations.Add(new Violation(mgcb,
          $"{block.Asset}: TextureFormat={format}. Use Color — Compressed (DXT) destroys pixel-art colour boundaries."));

      foreach (string padding in new[] { "ResizeToPowerOfTwo", "MakeSquare" })
      {
        if (string.Equals(block.Param(padding), "True", StringComparison.OrdinalIgnoreCase))
          violations.Add(new Violation(mgcb,
            $"{block.Asset}: {padding}=True pads the source and shifts every source rectangle. Use False."));
      }
    }

    // Scan source when this project ships textures, or when it is shared code
    // with no content pipeline of its own. A project that has an .mgcb and
    // chose not to put textures in it is the one case that stays silent.
    bool scanSource = textures.Count > 0 || !hasContentPipeline;
    if (!scanSource) return new CheckResult(textures, violations);

    foreach (string cs in Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories))
    {
      string rel = Path.GetRelativePath(projectDir, cs);
      if (rel.StartsWith("obj") || rel.StartsWith("bin")) continue;
      string[] lines = File.ReadAllLines(cs);
      for (int i = 0; i < lines.Length; i++)
      {
        if (!BareBeginCall.IsMatch(lines[i])) continue;
        string why = textures.Count > 0
          ? "in a project that ships textures"
          : "in shared code drawn by projects that ship textures";
        violations.Add(new Violation(cs,
          $"{rel}:{i + 1}: SpriteBatch.Begin() with no samplerState {why}. " +
          "Pass SamplerState.PointClamp — the default LinearClamp blurs pixel art."));
      }
    }

    return new CheckResult(textures, violations, SourceScanned: true);
  }

  sealed class ContentBlock
  {
    public string Asset = "";
    public bool IsTexture;
    readonly Dictionary<string, string> _params = new(StringComparer.OrdinalIgnoreCase);
    public void AddParam(string kv)
    {
      int eq = kv.IndexOf('=');
      if (eq <= 0) return;
      _params[kv[..eq].Trim()] = kv[(eq + 1)..].Trim();
    }
    public string Param(string name) => _params.GetValueOrDefault(name);
  }

  static IEnumerable<ContentBlock> ParseBlocks(string[] lines)
  {
    ContentBlock current = null;
    foreach (string raw in lines)
    {
      string line = raw.Trim();
      if (line.StartsWith("#begin "))
      {
        if (current != null) yield return current;
        current = new ContentBlock { Asset = line["#begin ".Length..].Trim() };
      }
      else if (current == null || line.StartsWith("#"))
      {
        continue;
      }
      else if (line.StartsWith("/importer:"))
      {
        current.IsTexture = line["/importer:".Length..].Trim()
          .Equals("TextureImporter", StringComparison.OrdinalIgnoreCase);
      }
      else if (line.StartsWith("/processorParam:"))
      {
        current.AddParam(line["/processorParam:".Length..]);
      }
    }
    if (current != null) yield return current;
  }
}
