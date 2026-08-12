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
/// Deliberately self-limiting: a project with no TextureImporter blocks in its
/// .mgcb is skipped entirely. That keeps the seven rectangle-only samples
/// silent — there is nothing to get wrong until a game actually has sprites —
/// while making it impossible for a new game to ship the mistake.
/// </summary>
public static class SpriteConventionChecker
{
  public readonly record struct Violation(string File, string Description);

  public sealed record CheckResult(
    IReadOnlyList<string> TextureAssets,
    IReadOnlyList<Violation> Violations)
  {
    public bool HasTextures => TextureAssets.Count > 0;
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
    if (!File.Exists(mgcb)) return new CheckResult(textures, violations);

    foreach (ContentBlock block in ParseBlocks(File.ReadAllLines(mgcb)))
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

    if (textures.Count == 0) return new CheckResult(textures, violations);

    foreach (string cs in Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories))
    {
      string rel = Path.GetRelativePath(projectDir, cs);
      if (rel.StartsWith("obj") || rel.StartsWith("bin")) continue;
      string[] lines = File.ReadAllLines(cs);
      for (int i = 0; i < lines.Length; i++)
      {
        if (!BareBeginCall.IsMatch(lines[i])) continue;
        violations.Add(new Violation(cs,
          $"{rel}:{i + 1}: SpriteBatch.Begin() with no samplerState in a project that ships textures. " +
          "Pass SamplerState.PointClamp — the default LinearClamp blurs pixel art."));
      }
    }

    return new CheckResult(textures, violations);
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
