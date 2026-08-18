namespace MonoGame.GameFramework.Tools;

/// <summary>
/// The canonical colour palette, loaded from a GIMP .gpl file.
///
/// .gpl was chosen over a bespoke format for one reason: Aseprite reads and
/// writes it natively. The artist loads the same bytes CI enforces, so there
/// is no second copy to drift from — which is the entire point of the exercise.
///
/// Used by both <see cref="PaletteChecker"/> (does this art conform?) and
/// <see cref="SpriteConformer"/> (make this art conform).
/// </summary>
public sealed class Palette
{
  public readonly record struct Rgb(byte R, byte G, byte B)
  {
    public string Hex => $"#{R:X2}{G:X2}{B:X2}";
  }

  public sealed record Entry(Rgb Color, string Name);

  /// <summary>
  /// Two palette entries competing for the same slot in the same ramp. Not an
  /// error — resolving one means repainting every sprite that uses the loser,
  /// which is a human decision — so this is reported as INFO, never a failure.
  /// </summary>
  public readonly record struct RampCollision(Entry A, Entry B, double HueDelta, double LightnessDelta);

  // A ramp step is a hue family at a given brightness. Two entries that agree
  // on both are not two steps, they are one step drawn twice.
  //
  // Brightness is Oklab L, not HSV V. HSV V is max(r,g,b), so every colour with
  // a 255 channel scores exactly 100 no matter how pale it is: #FF6CBA and
  // #FFB0DE are a hot pink and a powder pink, and V called them the same
  // brightness. That was harmless while the repo had one palette of mid-tones
  // and became five false positives the moment the games got bright ramps — at
  // which point the check is training people to skip its output, which is worse
  // than not having it.
  //
  // The threshold is deliberately tight. At 2 points of Oklab L it isolates
  // exactly the pairs trying to be the same colour: the historical #2A5DA0 /
  // #3A5FA0 collision sits 1.2 apart, while the tightest legitimate neighbours
  // in the repo (blue-4 and blue-5-hilite) sit 5.8 apart.
  //
  // Hue stays HSV — it is well defined there, and degrees are what an artist
  // reads off a colour wheel.
  const double HueFamilyDegrees = 15.0;
  const double SameSlotLightnessDelta = 2.0;

  readonly HashSet<Rgb> _lookup;
  readonly List<(Entry Entry, Oklab Lab)> _lab;

  public IReadOnlyList<Entry> Entries { get; }

  Palette(List<Entry> entries)
  {
    Entries = entries;
    _lookup = entries.Select(e => e.Color).ToHashSet();
    _lab = entries.Select(e => (e, Oklab.FromRgb(e.Color))).ToList();
  }

  public int Count => Entries.Count;

  public bool Contains(Rgb color) => _lookup.Contains(color);

  /// <summary>
  /// Nearest palette entry in Oklab, plus the perceptual distance travelled.
  /// Oklab rather than raw RGB because RGB distance badly misjudges dark
  /// colours — it will happily snap a shadow to the wrong ramp.
  /// </summary>
  public (Entry Entry, double Distance) Nearest(Rgb color)
  {
    Oklab target = Oklab.FromRgb(color);
    Entry best = _lab[0].Entry;
    double bestDist = double.MaxValue;
    foreach ((Entry entry, Oklab lab) in _lab)
    {
      double d = lab.DistanceTo(target);
      if (d >= bestDist) continue;
      bestDist = d;
      best = entry;
    }
    return (best, bestDist);
  }

  public IReadOnlyList<RampCollision> FindRampCollisions()
  {
    List<RampCollision> collisions = new();
    for (int i = 0; i < Entries.Count; i++)
    {
      for (int j = i + 1; j < Entries.Count; j++)
      {
        Hsv a = Hsv.FromRgb(Entries[i].Color);
        Hsv b = Hsv.FromRgb(Entries[j].Color);

        // Greys have no meaningful hue, so they never belong to a colour ramp
        // and can sit at any lightness without competing with anything.
        if (a.S < 5 || b.S < 5) continue;

        double hueDelta = HueDistance(a.H, b.H);
        double lightnessDelta = Math.Abs(
          Oklab.FromRgb(Entries[i].Color).L - Oklab.FromRgb(Entries[j].Color).L) * 100.0;
        if (hueDelta < HueFamilyDegrees && lightnessDelta < SameSlotLightnessDelta)
          collisions.Add(new RampCollision(Entries[i], Entries[j], hueDelta, lightnessDelta));
      }
    }
    return collisions;
  }

  static double HueDistance(double a, double b)
  {
    double d = Math.Abs(a - b) % 360.0;
    return d > 180.0 ? 360.0 - d : d;
  }

  public static Palette Load(string path) => Parse(File.ReadAllLines(path));

  /// <summary>
  /// Parses the GIMP palette format: a "GIMP Palette" magic line, optional
  /// Name:/Columns: headers, '#' comments, then "R G B\tName" rows.
  /// Tolerant of whitespace variation because hand-edited .gpl files are
  /// common; strict about the RGB triple because a misparse there would
  /// silently widen the palette.
  /// </summary>
  public static Palette Parse(IEnumerable<string> lines)
  {
    List<Entry> entries = new();
    foreach (string raw in lines)
    {
      string line = raw.Trim();
      if (line.Length == 0 || line.StartsWith('#')) continue;
      if (line.StartsWith("GIMP Palette", StringComparison.OrdinalIgnoreCase)) continue;
      if (line.StartsWith("Name:", StringComparison.OrdinalIgnoreCase)) continue;
      if (line.StartsWith("Columns:", StringComparison.OrdinalIgnoreCase)) continue;

      string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length < 3) continue;
      if (!byte.TryParse(parts[0], out byte r)) continue;
      if (!byte.TryParse(parts[1], out byte g)) continue;
      if (!byte.TryParse(parts[2], out byte b)) continue;

      string name = parts.Length > 3 ? string.Join(' ', parts[3..]) : $"#{r:X2}{g:X2}{b:X2}";
      entries.Add(new Entry(new Rgb(r, g, b), name));
    }
    return new Palette(entries);
  }

  /// <summary>
  /// Walks up from <paramref name="startDir"/> for the palette that governs it:
  /// a palette.gpl sitting in the directory itself first, then assets/palette.gpl,
  /// then the same pair one level up, and so on to the filesystem root.
  ///
  /// Nearest-wins is what makes per-game palettes work. A game drops a
  /// palette.gpl into its own Content/sprites/ and that file governs its art —
  /// no name-to-palette mapping table to keep in sync, because the palette is
  /// found by sitting next to the thing it constrains. Anything without a local
  /// palette keeps falling through to the repo-wide assets/palette.gpl, so the
  /// art that predates this still resolves exactly as it did.
  ///
  /// Nine games cannot share thirteen colours and still look like nine games.
  /// One palette per game is the smallest change that buys that, and it keeps
  /// the property that mattered: the artist loads the same bytes CI enforces.
  /// </summary>
  public static string FindPaletteFile(string startDir)
  {
    DirectoryInfo dir = new(Path.GetFullPath(startDir));
    while (dir != null)
    {
      // A palette beside the art governs that art.
      string beside = Path.Combine(dir.FullName, "palette.gpl");
      if (File.Exists(beside)) return beside;

      string shared = Path.Combine(dir.FullName, "assets", "palette.gpl");
      if (File.Exists(shared)) return shared;

      dir = dir.Parent;
    }
    return null;
  }

  readonly record struct Oklab(double L, double A, double B)
  {
    public double DistanceTo(Oklab o)
      => Math.Sqrt((L - o.L) * (L - o.L) + (A - o.A) * (A - o.A) + (B - o.B) * (B - o.B));

    public static Oklab FromRgb(Rgb c)
    {
      double r = ToLinear(c.R / 255.0);
      double g = ToLinear(c.G / 255.0);
      double b = ToLinear(c.B / 255.0);

      double l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
      double m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
      double s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);

      return new Oklab(
        0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
        1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
        0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
    }

    static double ToLinear(double c)
      => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
  }

  readonly record struct Hsv(double H, double S, double V)
  {
    public static Hsv FromRgb(Rgb c)
    {
      double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
      double max = Math.Max(r, Math.Max(g, b));
      double min = Math.Min(r, Math.Min(g, b));
      double delta = max - min;

      double h = 0;
      if (delta > 0)
      {
        if (max == r) h = 60 * (((g - b) / delta) % 6);
        else if (max == g) h = 60 * (((b - r) / delta) + 2);
        else h = 60 * (((r - g) / delta) + 4);
      }
      if (h < 0) h += 360;

      return new Hsv(h, max == 0 ? 0 : delta / max * 100, max * 100);
    }
  }
}
