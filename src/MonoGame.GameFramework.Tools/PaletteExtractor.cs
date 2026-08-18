using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MonoGame.GameFramework.Tools;

/// <summary>
/// Image -> .gpl. Lets a reference define the palette instead of someone
/// typing hex codes they read off a screenshot.
///
/// This exists because inventing a palette by eye is the single easiest way to
/// start a sprite wrong, and nothing upstream catches it: check-palette only
/// asks whether the art matches the palette, so art and palette invented
/// together are always in perfect agreement and always both wrong.
///
/// Two things it does beyond counting colours, both to keep the output a legal
/// member of the repo's palette family (assets/STYLE.md):
///
///   **Snaps near-black to the spine.** A reference's outline is almost never
///   exactly #1A1A1A — pure #000000 is the usual value. Emitting that verbatim
///   produces a palette check-palettes rejects, so anything close enough to be
///   the outline is written as the spine and conform-sprite then moves the art
///   onto it.
///
///   **Names by ramp.** Entries are grouped into hue families and ordered dark
///   to light inside each, because "shade within one ramp" is unfollowable when
///   every colour is called #B02860.
/// </summary>
public static class PaletteExtractor
{
  /// <summary>
  /// The outline is identified as the darkest low-chroma colour, not as
  /// whatever falls inside a radius of the spine.
  ///
  /// Radius was the obvious first try and it is wrong in both directions.
  /// Pure #000000 — far and away the most common outline colour in real
  /// references — sits 0.2175 from #1A1A1A in Oklab, outside any radius tight
  /// enough to be safe, while a dark red like #500000 sits 0.1239 inside it.
  /// Tested against a reference whose outline was #000000 and whose darkest
  /// ramp step was #500000, a radius rule picked the red.
  ///
  /// Darkest-wins is the rule that matches what an outline actually is, and
  /// the chroma guard is what stops it swallowing a saturated shadow step: a
  /// near-black has almost no chroma by definition, a dark red has plenty.
  /// </summary>
  public const double SpineMaxLightness = 0.32;

  /// <inheritdoc cref="SpineMaxLightness"/>
  public const double SpineMaxChroma = 0.08;

  public sealed record Extracted(
    Palette Palette,
    string Text,
    int SourceColors,
    int Clustered,
    Palette.Rgb? SnappedToSpine,
    IReadOnlyList<Palette.RampCollision> Collisions);

  public static Extracted Extract(
    string imagePath,
    string paletteName,
    int? maxColors = null,
    bool forceSpine = true)
  {
    ImageDescriber.Description d = ImageDescriber.Describe(imagePath);

    List<(Palette.Rgb Color, int Count)> colors =
      d.Colors.Select(c => (c.Color, c.Count)).ToList();
    int sourceCount = colors.Count;

    int clustered = 0;
    if (maxColors is int max && colors.Count > max)
    {
      colors = Cluster(colors, max);
      clustered = colors.Count;
    }

    // Spine handling: the darkest low-chroma entry is the outline, and it is
    // rewritten to the shared spine so the palette is a legal family member.
    // If nothing qualifies the spine is added unused, which check-palettes
    // wants and the art simply will not reference.
    Palette.Rgb spine = PaletteRegistry.SpineOutline;
    Palette.Rgb? outline = FindOutline(colors.Select(c => c.Color));
    Palette.Rgb? snapped = outline is Palette.Rgb o && !o.Equals(spine) ? o : null;

    List<(Palette.Rgb Color, int Count)> final = new();
    foreach ((Palette.Rgb c, int n) in colors)
      final.Add((outline is Palette.Rgb ol && c.Equals(ol) ? spine : c, n));
    if (outline == null && forceSpine) final.Add((spine, 0));

    List<Palette.Entry> entries = Name(final);
    Palette palette = Palette.Parse(entries.Select(e => $"{e.Color.R} {e.Color.G} {e.Color.B}\t{e.Name}"));

    return new Extracted(
      palette,
      Render(palette, paletteName, imagePath, d, sourceCount, snapped),
      sourceCount,
      clustered,
      snapped,
      palette.FindRampCollisions());
  }

  /// <summary>
  /// The darkest colour with little enough chroma to be an outline rather than
  /// a shadow ramp step. Null when the image has no such colour at all.
  /// </summary>
  public static Palette.Rgb? FindOutline(IEnumerable<Palette.Rgb> colors)
  {
    Palette.Rgb? best = null;
    double bestL = double.MaxValue;
    foreach (Palette.Rgb c in colors)
    {
      Palette.Oklab lab = Palette.Oklab.FromRgb(c);
      double chroma = Math.Sqrt(lab.A * lab.A + lab.B * lab.B);
      if (lab.L > SpineMaxLightness || chroma > SpineMaxChroma) continue;
      if (lab.L >= bestL) continue;
      bestL = lab.L;
      best = c;
    }
    return best;
  }

  /// <summary>
  /// Weighted k-means in Oklab. Only runs when a source has more colours than
  /// asked for — true pixel art usually does not, which is why the default is
  /// no limit at all.
  /// </summary>
  static List<(Palette.Rgb, int)> Cluster(List<(Palette.Rgb Color, int Count)> colors, int k)
  {
    // Seed from the most-used colours: deterministic, and for pixel art the
    // frequent colours are the ones that carry the design.
    List<Palette.Oklab> centers = colors
      .OrderByDescending(c => c.Count)
      .Take(k)
      .Select(c => Palette.Oklab.FromRgb(c.Color))
      .ToList();

    int[] assign = new int[colors.Count];
    for (int iter = 0; iter < 32; iter++)
    {
      bool moved = false;
      for (int i = 0; i < colors.Count; i++)
      {
        Palette.Oklab lab = Palette.Oklab.FromRgb(colors[i].Color);
        int best = 0;
        double bestD = double.MaxValue;
        for (int c = 0; c < centers.Count; c++)
        {
          double dist = centers[c].DistanceTo(lab);
          if (dist >= bestD) continue;
          bestD = dist; best = c;
        }
        if (assign[i] != best) { assign[i] = best; moved = true; }
      }
      if (iter > 0 && !moved) break;

      for (int c = 0; c < centers.Count; c++)
      {
        double l = 0, a = 0, b = 0; long w = 0;
        for (int i = 0; i < colors.Count; i++)
        {
          if (assign[i] != c) continue;
          Palette.Oklab lab = Palette.Oklab.FromRgb(colors[i].Color);
          int n = colors[i].Count;
          l += lab.L * n; a += lab.A * n; b += lab.B * n; w += n;
        }
        if (w > 0) centers[c] = new Palette.Oklab(l / w, a / w, b / w);
      }
    }

    // Represent each cluster by a real source colour, never by the centroid:
    // a centroid is a colour nothing in the image actually used, and the whole
    // point is to name the art's own colours.
    List<(Palette.Rgb, int)> result = new();
    for (int c = 0; c < centers.Count; c++)
    {
      var members = Enumerable.Range(0, colors.Count).Where(i => assign[i] == c).ToList();
      if (members.Count == 0) continue;
      int rep = members.OrderBy(i => centers[c].DistanceTo(Palette.Oklab.FromRgb(colors[i].Color))).First();
      result.Add((colors[rep].Color, members.Sum(i => colors[i].Count)));
    }
    return result;
  }

  static readonly (double Max, string Name)[] HueFamilies =
  {
    (15, "red"), (45, "amber"), (70, "gold"), (160, "green"), (200, "teal"),
    (250, "blue"), (290, "violet"), (345, "magenta"), (360, "red"),
  };

  static List<Palette.Entry> Name(List<(Palette.Rgb Color, int Count)> colors)
  {
    Dictionary<string, List<Palette.Rgb>> families = new();
    foreach ((Palette.Rgb c, _) in colors)
    {
      if (c.Equals(PaletteRegistry.SpineOutline)) continue;
      Palette.Hsv hsv = Palette.Hsv.FromRgb(c);
      // Greys have no hue worth naming, and the collision rule already exempts
      // them, so they get their own family rather than being forced into one.
      string family = hsv.S < 5 ? "grey" : HueFamilies.First(f => hsv.H < f.Max).Name;
      families.TryAdd(family, new List<Palette.Rgb>());
      families[family].Add(c);
    }

    List<Palette.Entry> entries = new() { new Palette.Entry(PaletteRegistry.SpineOutline, PaletteRegistry.SpineOutlineName) };
    foreach ((string family, List<Palette.Rgb> members) in families.OrderBy(kv => kv.Key, StringComparer.Ordinal))
    {
      List<Palette.Rgb> ordered = members.OrderBy(c => Palette.Oklab.FromRgb(c).L).ToList();
      for (int i = 0; i < ordered.Count; i++)
      {
        string name = ordered.Count == 1 ? family
          : i == 0 && ordered.Count >= 3 ? $"{family}-0-shadow"
          : i == ordered.Count - 1 && ordered.Count >= 3 ? $"{family}-{i}-hilite"
          : $"{family}-{i}";
        entries.Add(new Palette.Entry(ordered[i], name));
      }
    }
    return entries;
  }

  static string Render(
    Palette palette, string name, string source,
    ImageDescriber.Description d, int sourceCount, Palette.Rgb? snapped)
  {
    StringBuilder sb = new();
    sb.AppendLine("GIMP Palette");
    sb.Append("Name: ").AppendLine(name);
    sb.AppendLine("Columns: 5");
    sb.AppendLine("#");
    sb.Append("# Extracted by mgf-tools extract-palette from ").AppendLine(Path.GetFileName(source));
    sb.Append("# Source was ").Append(d.FileWidth).Append('x').Append(d.FileHeight);
    if (d.IsUpscaled) sb.Append(" (native ").Append(d.Width).Append('x').Append(d.Height).Append(", ").Append(d.BlockSize).Append("x blocks)");
    sb.Append(", ").Append(sourceCount).AppendLine(" distinct opaque colour(s).");
    if (snapped is Palette.Rgb s)
      sb.Append("# ").Append(s.Hex).Append(" was snapped to the shared outline spine ")
        .Append(PaletteRegistry.SpineOutline.Hex).AppendLine(" — run conform-sprite to move the art onto it.");
    sb.AppendLine("#");
    sb.AppendLine("# Names are generated: hue family, then dark -> light within it. Rename freely,");
    sb.AppendLine("# but keep 'outline' — check-palettes requires it and every palette shares it.");
    sb.AppendLine("#");
    foreach (Palette.Entry e in palette.Entries)
      sb.Append($"{e.Color.R,3} {e.Color.G,3} {e.Color.B,3}\t{e.Name}").AppendLine();
    return sb.ToString();
  }
}
