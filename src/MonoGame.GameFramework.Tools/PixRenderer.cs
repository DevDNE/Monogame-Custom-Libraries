using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MonoGame.GameFramework.Tools;

/// <summary>
/// Turns a <see cref="PixDocument"/> into the PNG the content pipeline eats.
///
/// The PNG is committed alongside its .pix rather than generated at build time,
/// for the same reason the .xnb cache is checked rather than trusted: MGCB is
/// the consumer and it wants a file on disk. That makes the two able to drift,
/// so <see cref="Diff"/> exists and CI runs it — the .pix is the source, the
/// PNG is a build artefact that happens to be committed, and check-pix is what
/// keeps that claim true.
/// </summary>
public static class PixRenderer
{
  public sealed record RenderResult(
    string PaletteFile,
    int Width,
    int Height,
    int OpaquePixels,
    IReadOnlyList<SpriteConformer.PaletteUsage> Usage);

  /// <summary>Where the PNG for a given .pix belongs: beside it, same stem.</summary>
  public static string OutputPathFor(string pixPath)
    => Path.ChangeExtension(pixPath, ".png");

  /// <summary>
  /// Which palette governs a .pix: --palette, then the file's own `palette`
  /// directive, then the nearest palette.gpl walking up from it.
  /// </summary>
  public static string ResolvePalettePath(string pixPath, PixDocument doc, string explicitPalette = null)
  {
    string dir = Path.GetDirectoryName(Path.GetFullPath(pixPath));
    string path = explicitPalette
                  ?? (doc.PaletteOverride == null ? null : Path.GetFullPath(Path.Combine(dir, doc.PaletteOverride)))
                  ?? Palette.FindPaletteFile(dir)
                  ?? throw new PixDocument.ParseException($"{pixPath}: no palette.gpl found above it.");
    if (!File.Exists(path))
      throw new PixDocument.ParseException($"{pixPath}: palette '{path}' does not exist.");
    return path;
  }

  public static Image<Rgba32> Render(PixDocument doc, Palette palette, string sourceName = null)
  {
    Dictionary<char, Rgba32> resolved = new();
    foreach ((char c, string colorName) in doc.Key)
    {
      Palette.Entry entry = palette.Entries.FirstOrDefault(e =>
        string.Equals(e.Name, colorName, StringComparison.OrdinalIgnoreCase));
      if (entry == null)
      {
        // Naming the near misses turns a typo from a hunt into a glance.
        IEnumerable<string> near = palette.Entries
          .Where(e => e.Name.StartsWith(colorName.Split('-')[0], StringComparison.OrdinalIgnoreCase))
          .Select(e => e.Name).Take(6);
        string hint = near.Any() ? $" Did you mean: {string.Join(", ", near)}?" : "";
        throw new PixDocument.ParseException(
          $"{sourceName}: key '{c}' names '{colorName}', which is not in the palette.{hint}");
      }
      resolved[c] = new Rgba32(entry.Color.R, entry.Color.G, entry.Color.B, 255);
    }

    Image<Rgba32> image = new(doc.Width, doc.Height);
    for (int y = 0; y < doc.Height; y++)
    {
      string row = doc.Rows[y];
      for (int x = 0; x < doc.Width; x++)
      {
        char c = row[x];
        image[x, y] = c == PixDocument.TransparentChar ? new Rgba32(0, 0, 0, 0) : resolved[c];
      }
    }
    return image;
  }

  public static RenderResult RenderToFile(string pixPath, string outputPath, string explicitPalette = null)
  {
    PixDocument doc = PixDocument.Load(pixPath);
    string palettePath = ResolvePalettePath(pixPath, doc, explicitPalette);
    Palette palette = Palette.Load(palettePath);

    using Image<Rgba32> image = Render(doc, palette, pixPath);

    string dir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    image.SaveAsPng(outputPath);

    return Describe(palettePath, doc, palette);
  }

  static RenderResult Describe(string palettePath, PixDocument doc, Palette palette)
  {
    Dictionary<string, int> counts = new(StringComparer.OrdinalIgnoreCase);
    int opaque = 0;
    foreach (string row in doc.Rows)
      foreach (char c in row)
      {
        if (c == PixDocument.TransparentChar) continue;
        opaque++;
        string colorName = doc.Key[c];
        counts[colorName] = counts.GetValueOrDefault(colorName) + 1;
      }

    List<SpriteConformer.PaletteUsage> usage = palette.Entries
      .Where(e => counts.ContainsKey(e.Name))
      .Select(e => new SpriteConformer.PaletteUsage(e, counts[e.Name]))
      .OrderByDescending(u => u.PixelCount)
      .ToList();

    return new RenderResult(palettePath, doc.Width, doc.Height, opaque, usage);
  }

  /// <summary>
  /// Compares a freshly-rendered .pix against the PNG committed beside it.
  /// Returns null when they agree, or a one-line description of the first
  /// disagreement — dimensions if those differ, otherwise the first differing
  /// pixel in reading order plus a total count.
  /// </summary>
  public static string Diff(string pixPath, string pngPath, string explicitPalette = null)
  {
    if (!File.Exists(pngPath))
      return $"{Path.GetFileName(pngPath)} is missing. Run render-pix.";

    PixDocument doc = PixDocument.Load(pixPath);
    Palette palette = Palette.Load(ResolvePalettePath(pixPath, doc, explicitPalette));
    using Image<Rgba32> expected = Render(doc, palette, pixPath);
    using Image<Rgba32> actual = Image.Load<Rgba32>(pngPath);

    if (expected.Width != actual.Width || expected.Height != actual.Height)
      return $"{Path.GetFileName(pngPath)} is {actual.Width}x{actual.Height}, .pix says {expected.Width}x{expected.Height}. Run render-pix.";

    int differing = 0;
    int firstX = 0, firstY = 0;
    Rgba32 firstExpected = default, firstActual = default;
    for (int y = 0; y < expected.Height; y++)
      for (int x = 0; x < expected.Width; x++)
      {
        Rgba32 e = expected[x, y], a = actual[x, y];
        // A transparent pixel's RGB is not observable, and encoders are free to
        // store anything under a zero alpha.
        if (e.A == 0 && a.A == 0) continue;
        if (e.R == a.R && e.G == a.G && e.B == a.B && e.A == a.A) continue;
        if (differing++ == 0) (firstX, firstY, firstExpected, firstActual) = (x, y, e, a);
      }

    if (differing == 0) return null;
    return $"{Path.GetFileName(pngPath)} differs from its .pix in {differing} pixel(s); " +
           $"first at {firstX},{firstY} (png has #{firstActual.R:X2}{firstActual.G:X2}{firstActual.B:X2}/a{firstActual.A}, " +
           $".pix says #{firstExpected.R:X2}{firstExpected.G:X2}{firstExpected.B:X2}/a{firstExpected.A}). Run render-pix.";
  }

  /// <summary>
  /// Every .pix under a directory, skipping build output. Same shape as
  /// <see cref="PaletteChecker.FindSprites"/> so the two commands agree on what
  /// counts as art.
  /// </summary>
  public static IEnumerable<string> FindSources(string root)
  {
    if (!Directory.Exists(root)) yield break;
    foreach (string pix in Directory.EnumerateFiles(root, "*.pix", SearchOption.AllDirectories).OrderBy(p => p))
    {
      string rel = Path.GetRelativePath(root, pix);
      if (rel.Split(Path.DirectorySeparatorChar).Any(s => s is "obj" or "bin")) continue;
      yield return pix;
    }
  }
}
