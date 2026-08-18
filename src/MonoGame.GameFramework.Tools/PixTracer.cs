using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MonoGame.GameFramework.Tools;

/// <summary>
/// PNG -> .pix. The direction the format was missing.
///
/// <see cref="PixRenderer"/> made .pix a front-end you could only author into
/// by hand, which quietly forced every sprite that started life as an image —
/// a reference, a generator's output, an Aseprite export — to be re-typed from
/// a blank grid. That is exactly the step where proportion and placement get
/// lost, because a blank grid gives you nothing to be wrong about until you
/// render it.
///
/// With this the pipeline closes:
///
///     any image -> conform-sprite (palette + size) -> trace-pix -> hand-polish
///                                                       as text -> render-pix
///
/// The tracing itself is not clever and must not be: every pixel is already a
/// palette entry by the time it arrives, so this is a lookup and a character
/// assignment. Anything smarter would be a second opinion about colour, and
/// <see cref="SpriteConformer"/> already owns that decision.
///
/// Round-trip is the contract: render-pix of a traced .pix reproduces the
/// input PNG byte-for-byte. That is what makes it safe to treat the .pix as
/// the source afterwards.
/// </summary>
public static class PixTracer
{
  public sealed class TraceException(string message) : Exception(message);

  public sealed record TraceResult(
    string Text,
    int Width,
    int Height,
    int BlockSize,
    IReadOnlyDictionary<char, string> Key);

  /// <summary>
  /// Characters never assigned: '.' is transparent and '*' prefixes a row
  /// repeat. <see cref="PixDocument"/> rejects both as keys, so the generator
  /// must agree with the parser about them.
  /// </summary>
  const string Reserved = ".*";

  /// <summary>
  /// Last-resort characters, used only once every letter in every palette
  /// name is spoken for. Deliberately visually distinct from each other —
  /// the whole value of the format is that a human can read the grid.
  /// </summary>
  const string Fallback = "0123456789+=~!@$%^&<>?/|:;#";

  public static TraceResult Trace(
    string imagePath,
    Palette palette,
    string name,
    string paletteDirective = null,
    bool reduce = true)
  {
    using Image<Rgba32> loaded = Image.Load<Rgba32>(imagePath);

    int block = reduce ? ImageDescriber.DetectBlockSize(ImageDescriber.ToArray(loaded), loaded.Width, loaded.Height) : 1;
    using Image<Rgba32> image = ImageDescriber.Reduce(loaded, block);

    int w = image.Width, h = image.Height;
    Rgba32[] px = ImageDescriber.ToArray(image);

    // Every colour must already be a palette entry. Guessing the nearest one
    // here would silently repaint the art at the moment it becomes the source
    // of truth, which is the one place a silent repaint is unrecoverable.
    Dictionary<Palette.Rgb, Palette.Entry> byColor = new();
    foreach (Palette.Entry e in palette.Entries) byColor.TryAdd(e.Color, e);

    HashSet<Palette.Rgb> offPalette = new();
    HashSet<byte> partialAlpha = new();
    foreach (Rgba32 p in px)
    {
      if (p.A is not (0 or 255)) { partialAlpha.Add(p.A); continue; }
      if (p.A == 0) continue;
      Palette.Rgb rgb = new(p.R, p.G, p.B);
      if (!byColor.ContainsKey(rgb)) offPalette.Add(rgb);
    }

    if (partialAlpha.Count > 0)
      throw new TraceException(
        $"{imagePath}: {partialAlpha.Count} partial alpha value(s) present ({string.Join(", ", partialAlpha.OrderBy(a => a).Take(8))}). " +
        ".pix has no syntax for a feathered edge. Run conform-sprite first — it flattens alpha at a threshold.");

    if (offPalette.Count > 0)
      throw new TraceException(
        $"{imagePath}: {offPalette.Count} colour(s) are not in the palette.\n" +
        string.Join('\n', offPalette.OrderBy(c => c.Hex, StringComparer.Ordinal).Take(12).Select(c => $"  {c.Hex}")) +
        (offPalette.Count > 12 ? $"\n  …and {offPalette.Count - 12} more" : "") +
        "\nRun conform-sprite against this palette first; trace-pix will not pick colours for you.");

    // Assign in first-appearance order so the key list reads top-left to
    // bottom-right, which is the order someone reading the grid meets them.
    List<Palette.Entry> order = new();
    HashSet<Palette.Rgb> seen = new();
    foreach (Rgba32 p in px)
    {
      if (p.A == 0) continue;
      Palette.Rgb rgb = new(p.R, p.G, p.B);
      if (seen.Add(rgb)) order.Add(byColor[rgb]);
    }

    Dictionary<Palette.Rgb, char> chars = AssignChars(order);

    StringBuilder sb = new();
    sb.Append("# Traced from ").Append(Path.GetFileName(imagePath));
    if (block > 1) sb.Append(" (reduced ").Append(block).Append("x from ").Append(loaded.Width).Append('x').Append(loaded.Height).Append(')');
    sb.AppendLine();
    sb.AppendLine("# Edit this file, not the PNG: render-pix regenerates the PNG from here.");
    if (name != null) sb.Append("name ").AppendLine(name);
    if (paletteDirective != null) sb.Append("palette ").AppendLine(paletteDirective);
    sb.Append("size ").Append(w).Append(' ').Append(h).AppendLine();
    foreach (Palette.Entry e in order)
      sb.Append("key ").Append(chars[e.Color]).Append(' ').AppendLine(e.Name);
    sb.AppendLine("pixels");

    List<string> rows = new(h);
    for (int y = 0; y < h; y++)
    {
      StringBuilder row = new(w);
      for (int x = 0; x < w; x++)
      {
        Rgba32 p = px[y * w + x];
        row.Append(p.A == 0 ? PixDocument.TransparentChar : chars[new Palette.Rgb(p.R, p.G, p.B)]);
      }
      rows.Add(row.ToString());
    }

    // Collapse runs of identical rows. Skies and floors are mostly repetition
    // and 40 identical lines hide the 3 that differ.
    for (int y = 0; y < rows.Count;)
    {
      int run = 1;
      while (y + run < rows.Count && rows[y + run] == rows[y]) run++;
      if (run > 1) sb.Append('*').Append(run).Append(' ');
      sb.AppendLine(rows[y]);
      y += run;
    }

    return new TraceResult(
      sb.ToString(), w, h, block,
      chars.ToDictionary(kv => kv.Value, kv => byColor[kv.Key].Name));
  }

  /// <summary>
  /// One character per entry, drawn from the entry's own name so the grid is
  /// readable without constant reference to the key: "crimson-1" wants to be
  /// 'c'. Case is the first fallback because 'c'/'C' still reads as the same
  /// material, which keeps a ramp legible as a ramp.
  /// </summary>
  static Dictionary<Palette.Rgb, char> AssignChars(IReadOnlyList<Palette.Entry> entries)
  {
    Dictionary<Palette.Rgb, char> assigned = new();
    HashSet<char> taken = new();

    foreach (Palette.Entry e in entries)
    {
      char? pick = Candidates(e.Name).FirstOrDefault(c => !taken.Contains(c) && !Reserved.Contains(c));
      if (pick is not char c2 || c2 == '\0')
        throw new TraceException(
          $"ran out of characters assigning a key for '{e.Name}'. " +
          $"A .pix cannot carry more than {26 * 2 + 10 + Fallback.Length} colours; this one needs fewer.");
      assigned[e.Color] = c2;
      taken.Add(c2);
    }
    return assigned;
  }

  static IEnumerable<char> Candidates(string name)
  {
    string[] tokens = name.Split('-', StringSplitOptions.RemoveEmptyEntries);

    foreach (string t in tokens)
    {
      if (t.Length == 0 || !char.IsLetter(t[0])) continue;
      yield return char.ToLowerInvariant(t[0]);
      yield return char.ToUpperInvariant(t[0]);
    }
    foreach (char ch in name)
    {
      if (!char.IsLetter(ch)) continue;
      yield return char.ToLowerInvariant(ch);
      yield return char.ToUpperInvariant(ch);
    }
    foreach (char ch in "abcdefghijklmnopqrstuvwxyz") { yield return ch; yield return char.ToUpperInvariant(ch); }
    foreach (char ch in Fallback) yield return ch;
  }
}
