namespace MonoGame.GameFramework.Tools;

/// <summary>
/// A sprite written as text: a key mapping single characters to palette entry
/// names, then a grid of those characters.
///
/// This is a third front-end for step 1 of the pipeline in assets/STYLE.md,
/// alongside Aseprite and the image generators — the step that was always meant
/// to stay swappable. It earns its place on three properties the binary formats
/// cannot offer:
///
///   **It diffs.** `git diff` on a .aseprite or a .png tells you a sprite
///   changed. On a .pix it tells you which pixels, which is the difference
///   between reviewing art and taking its word for it.
///
///   **It cannot be off-palette.** Every pixel names a palette entry, so
///   `check-palette` has nothing left to catch. The gate stops being a net and
///   becomes a formality — for this front-end only, which is why the gate stays.
///
///   **Alpha is binary by construction.** '.' is transparent, everything else
///   is opaque. There is no way to express the feathered edge that partial
///   alpha would smuggle in.
///
/// What it is not: a replacement for Aseprite. Hand-polish, onion-skinning and
/// anything above roughly 64x64 still want a real editor. The .pix files here
/// are the small, structural, high-repetition art — tiles, icons, UI frames,
/// projectiles — where a text grid is genuinely the better tool.
/// </summary>
public sealed class PixDocument
{
  /// <summary>Transparent. Reserved, so it never needs a key entry.</summary>
  public const char TransparentChar = '.';

  /// <summary>Repeats the row that follows on the same line: "*8 ....oooo".</summary>
  const char RepeatChar = '*';

  public sealed record Cell(char Char, string ColorName);

  public string Name { get; init; }
  public string PaletteOverride { get; init; }
  public IReadOnlyDictionary<char, string> Key { get; init; }
  public IReadOnlyList<string> Rows { get; init; }

  public int Width => Rows.Count == 0 ? 0 : Rows[0].Length;
  public int Height => Rows.Count;

  public sealed class ParseException(string message) : Exception(message);

  public static PixDocument Load(string path)
  {
    try
    {
      return Parse(File.ReadAllLines(path));
    }
    catch (ParseException e)
    {
      throw new ParseException($"{path}: {e.Message}");
    }
  }

  /// <summary>
  /// Parses the format. Directives first, then a `pixels` line, then the grid.
  ///
  /// Deliberately strict: a ragged row or an unkeyed character is an error, not
  /// a padded row or a transparent pixel. A format whose failure mode is "draws
  /// something slightly wrong" would put the reviewer back to eyeballing PNGs,
  /// which is the job this exists to remove.
  /// </summary>
  public static PixDocument Parse(IEnumerable<string> lines)
  {
    string name = null, paletteOverride = null;
    int? declaredWidth = null, declaredHeight = null;
    Dictionary<char, string> key = new();
    List<string> rows = new();
    bool inGrid = false;
    int lineNumber = 0;

    foreach (string raw in lines)
    {
      lineNumber++;
      string line = raw.TrimEnd();
      string trimmed = line.TrimStart();
      if (trimmed.Length == 0) continue;
      if (trimmed[0] == '#') continue;

      if (!inGrid)
      {
        string[] parts = trimmed.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        switch (parts[0])
        {
          case "pixels":
            inGrid = true;
            continue;

          case "name" when parts.Length >= 2:
            name = parts[1];
            continue;

          case "palette" when parts.Length >= 2:
            paletteOverride = parts[1];
            continue;

          case "size" when parts.Length >= 3
                           && int.TryParse(parts[1], out int w)
                           && int.TryParse(parts[2], out int h):
            declaredWidth = w;
            declaredHeight = h;
            continue;

          // "key <char> <palette-entry-name>". The name may contain spaces,
          // because palette entry names in a .gpl may.
          case "key" when parts.Length >= 3 && parts[1].Length == 1:
            char c = parts[1][0];
            if (c == TransparentChar)
              throw new ParseException($"line {lineNumber}: '{TransparentChar}' is reserved for transparent and cannot be keyed.");
            if (c == RepeatChar)
              throw new ParseException($"line {lineNumber}: '{RepeatChar}' is reserved for the row-repeat prefix and cannot be keyed.");
            if (key.ContainsKey(c))
              throw new ParseException($"line {lineNumber}: '{c}' is keyed twice (to {key[c]} and {string.Join(' ', parts[2..])}).");
            key[c] = string.Join(' ', parts[2..]);
            continue;

          default:
            throw new ParseException($"line {lineNumber}: unrecognised directive '{trimmed}'. " +
                                     "Expected name/palette/size/key, then 'pixels'.");
        }
      }

      // Grid. "*N row" repeats a row N times — skies and floors are mostly
      // repetition, and 40 identical lines hide the 3 that differ.
      string rowText = trimmed;
      int repeat = 1;
      if (rowText[0] == RepeatChar)
      {
        int space = rowText.IndexOf(' ');
        if (space < 0 || !int.TryParse(rowText[1..space], out repeat) || repeat < 1)
          throw new ParseException($"line {lineNumber}: bad repeat prefix '{rowText}'. Expected '*N ' with N >= 1.");
        rowText = rowText[(space + 1)..];
      }

      for (int i = 0; i < repeat; i++) rows.Add(rowText);
    }

    if (!inGrid) throw new ParseException("no 'pixels' section.");
    if (rows.Count == 0) throw new ParseException("'pixels' section is empty.");

    // The most common authoring mistake by far is a row off by a character or
    // two, and it is rarely alone. Reporting only the first turns fixing a
    // hand-drawn sprite into one round trip per row, so every offender is
    // listed at once — with the delta, since "40, expected 42" is the whole
    // answer and "row 17 is wrong" is not.
    int width = rows[0].Length;
    List<string> ragged = new();
    for (int y = 0; y < rows.Count; y++)
    {
      if (rows[y].Length == width) continue;
      int delta = rows[y].Length - width;
      ragged.Add($"  row {y}: {rows[y].Length} wide ({delta:+#;-#} vs row 0)");
    }
    if (ragged.Count > 0)
      throw new ParseException(
        $"row 0 is {width} wide; {ragged.Count} row(s) disagree. Rows must be equal length.\n" +
        string.Join('\n', ragged));

    List<string> unkeyed = new();
    for (int y = 0; y < rows.Count; y++)
    {
      for (int x = 0; x < rows[y].Length; x++)
      {
        char c = rows[y][x];
        if (c == TransparentChar || key.ContainsKey(c)) continue;
        unkeyed.Add($"  row {y} col {x}: '{c}'");
      }
    }
    if (unkeyed.Count > 0)
      throw new ParseException(
        $"{unkeyed.Count} character(s) have no key entry.\n" +
        string.Join('\n', unkeyed.Take(12)) +
        (unkeyed.Count > 12 ? $"\n  …and {unkeyed.Count - 12} more" : ""));

    if (declaredWidth is int dw && dw != width)
      throw new ParseException($"declared size is {dw} wide, grid is {width}.");
    if (declaredHeight is int dh && dh != rows.Count)
      throw new ParseException($"declared size is {dh} tall, grid is {rows.Count}.");

    List<char> unused = key.Keys.Where(c => !rows.Any(r => r.Contains(c))).ToList();
    if (unused.Count > 0)
      throw new ParseException($"key entries never used in the grid: {string.Join(", ", unused.Select(c => $"'{c}'"))}. " +
                               "A stale key is usually a rename that missed the grid.");

    return new PixDocument
    {
      Name = name,
      PaletteOverride = paletteOverride,
      Key = key,
      Rows = rows,
    };
  }
}
