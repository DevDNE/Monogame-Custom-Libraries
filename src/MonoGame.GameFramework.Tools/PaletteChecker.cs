using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MonoGame.GameFramework.Tools;

/// <summary>
/// Enforces the two colour rules from assets/STYLE.md that are fully
/// mechanical, and therefore have no business being a human's job:
///
///   1. Every opaque pixel is a colour from assets/palette.gpl. Palette
///      discipline is the single largest contributor to a set of sprites
///      reading as one game, and it is the thing an image generator is least
///      likely to respect on its own.
///   2. Alpha is binary — 0 or 255. Feathered edges are how a smooth-image
///      generator's output announces itself, and they fight the hard 1px
///      boundaries the whole pipeline is built around.
///
/// It also reports ramp collisions inside the palette itself, as INFO rather
/// than a failure: two colours competing for one ramp slot is real drift, but
/// fixing it means repainting committed art, so a human picks the winner.
///
/// Self-limiting in the same spirit as <see cref="SpriteConventionChecker"/>:
/// no palette file, or no sprite directory, means there is nothing to get
/// wrong yet and the check stays silent. New samples inherit the rule the
/// moment they gain their first PNG, without anyone remembering to opt in.
/// </summary>
public static class PaletteChecker
{
  public readonly record struct Violation(string File, string Description);

  /// <summary>
  /// A ramp collision, tagged with the palette it lives in. Once palettes are
  /// per-game a bare collision is unactionable — "two blues share a slot" is
  /// only useful if you know which of ten files to open.
  /// </summary>
  public readonly record struct PaletteCollision(string PaletteFile, Palette.RampCollision Collision);

  public sealed record CheckResult(
    string PaletteFile,
    IReadOnlyList<string> ScannedSprites,
    IReadOnlyList<Violation> Violations,
    IReadOnlyList<PaletteCollision> Collisions,
    IReadOnlyList<string> PalettesUsed = null)
  {
    /// <summary>Every distinct palette file the scan actually resolved against.</summary>
    public IReadOnlyList<string> PalettesUsed { get; init; } =
      PalettesUsed ?? (PaletteFile == null ? Array.Empty<string>() : new[] { PaletteFile });

    public bool HasPalette => PaletteFile != null || PalettesUsed.Count > 0;
    public bool HasSprites => ScannedSprites.Count > 0;
  }

  // A file with a wholly wrong palette would otherwise print one line per
  // distinct colour, burying the signal. Eight is enough to see the pattern.
  const int MaxReportedColorsPerFile = 8;

  /// <summary>
  /// Checks every sprite under <paramref name="projectDir"/>.
  ///
  /// With no explicit <paramref name="paletteFile"/>, each sprite is judged
  /// against its own nearest palette (see <see cref="Palette.FindPaletteFile"/>),
  /// so a game carrying Content/sprites/palette.gpl is checked against that and
  /// everything else falls through to the repo-wide assets/palette.gpl. An
  /// explicit --palette overrides all of it, because that is what asking for a
  /// specific palette means.
  /// </summary>
  public static CheckResult Check(string projectDir, string paletteFile = null)
  {
    bool overridden = paletteFile != null;
    string projectPalette = paletteFile ?? Palette.FindPaletteFile(projectDir);
    List<string> scanned = new();
    List<Violation> violations = new();

    if (overridden && !File.Exists(paletteFile))
      return new CheckResult(null, scanned, violations, Array.Empty<PaletteCollision>(), Array.Empty<string>());

    // Loading is cached by path: a nine-game sweep resolves the same handful of
    // palettes hundreds of times, and re-parsing each is pure waste.
    Dictionary<string, Palette> loaded = new(StringComparer.Ordinal);
    List<string> used = new();

    // Only palettes a sprite actually resolved to land in `used` — reporting
    // the collisions of a palette nothing in this project draws from is noise.
    Palette Resolve(string path)
    {
      if (loaded.TryGetValue(path, out Palette cached)) return cached;
      Palette p = Palette.Load(path);
      loaded[path] = p;
      used.Add(path);
      return p;
    }

    foreach (string png in FindSprites(projectDir))
    {
      string forThisSprite = overridden
        ? paletteFile
        : Palette.FindPaletteFile(Path.GetDirectoryName(png)) ?? projectPalette;
      if (forThisSprite == null || !File.Exists(forThisSprite)) continue;

      scanned.Add(png);
      violations.AddRange(CheckFile(png, Resolve(forThisSprite), projectDir));
    }

    // Collisions are a property of a palette, not of a sprite, so they are
    // reported once per palette the scan touched rather than once per file.
    List<PaletteCollision> collisions = new();
    foreach (string path in used)
      foreach (Palette.RampCollision c in loaded[path].FindRampCollisions())
        collisions.Add(new PaletteCollision(path, c));

    return new CheckResult(projectPalette, scanned, violations, collisions, used);
  }

  /// <summary>
  /// Sprites live at Content/sprites/ in a game project and at sprites/ in the
  /// repo-root assets/ directory. Accepting both lets one command cover the
  /// authoring sources and the pipeline-facing exports without a second shape.
  /// </summary>
  public static IEnumerable<string> FindSprites(string projectDir)
  {
    foreach (string relative in new[] { Path.Combine("Content", "sprites"), "sprites" })
    {
      string dir = Path.Combine(projectDir, relative);
      if (!Directory.Exists(dir)) continue;
      foreach (string png in Directory.EnumerateFiles(dir, "*.png", SearchOption.AllDirectories).OrderBy(p => p))
      {
        string rel = Path.GetRelativePath(projectDir, png);
        if (rel.StartsWith("obj") || rel.StartsWith("bin")) continue;
        yield return png;
      }
    }
  }

  static IEnumerable<Violation> CheckFile(string png, Palette palette, string projectDir)
  {
    string rel = Path.GetRelativePath(projectDir, png);
    Dictionary<Palette.Rgb, (int Count, int X, int Y)> offPalette = new();
    int partialAlpha = 0;
    int firstPartialX = 0, firstPartialY = 0, firstPartialValue = 0;

    using (Image<Rgba32> image = Image.Load<Rgba32>(png))
    {
      image.ProcessPixelRows(accessor =>
      {
        for (int y = 0; y < accessor.Height; y++)
        {
          Span<Rgba32> row = accessor.GetRowSpan(y);
          for (int x = 0; x < row.Length; x++)
          {
            Rgba32 p = row[x];
            if (p.A == 0) continue;
            if (p.A != 255)
            {
              if (partialAlpha == 0)
              {
                firstPartialX = x;
                firstPartialY = y;
                firstPartialValue = p.A;
              }
              partialAlpha++;
              continue;
            }

            Palette.Rgb rgb = new(p.R, p.G, p.B);
            if (palette.Contains(rgb)) continue;
            if (offPalette.TryGetValue(rgb, out (int Count, int X, int Y) seen))
              offPalette[rgb] = (seen.Count + 1, seen.X, seen.Y);
            else
              offPalette[rgb] = (1, x, y);
          }
        }
      });
    }

    if (partialAlpha > 0)
    {
      yield return new Violation(png,
        $"{rel}: {partialAlpha} pixel(s) with partial alpha (first at {firstPartialX},{firstPartialY} a={firstPartialValue}). " +
        "Pixel art uses binary alpha — 0 or 255.");
    }

    int reported = 0;
    foreach ((Palette.Rgb rgb, (int count, int x, int y)) in offPalette.OrderByDescending(kv => kv.Value.Count))
    {
      if (reported++ == MaxReportedColorsPerFile)
      {
        yield return new Violation(png,
          $"{rel}: …and {offPalette.Count - MaxReportedColorsPerFile} more off-palette colour(s). " +
          "Run conform-sprite over this file.");
        yield break;
      }
      (Palette.Entry entry, double distance) = palette.Nearest(rgb);
      yield return new Violation(png,
        $"{rel}: {count} pixel(s) of {rgb.Hex} are off-palette (first at {x},{y}). " +
        $"Nearest is {entry.Name} {entry.Color.Hex}, {distance:F3} away in Oklab.");
    }
  }
}
