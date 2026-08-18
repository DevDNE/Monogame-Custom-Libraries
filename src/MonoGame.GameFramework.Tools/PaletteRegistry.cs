namespace MonoGame.GameFramework.Tools;

/// <summary>
/// The repo's palettes as a set, rather than one at a time.
///
/// Nine games with nine palettes trade a problem for a different problem. The
/// old one was that thirteen colours could not carry nine art directions. The
/// new one is that nine independent palettes drift into nine unrelated-looking
/// games — which is worse, because the samples exist to look like one library's
/// output.
///
/// What holds them together is deliberately thin: one shared colour, the
/// silhouette outline. Everything else — hue, UI chrome, how many ramps, how
/// long they are — is the game's own decision, because that is exactly the
/// axis the samples are meant to differ on. A thicker spine would have to be
/// argued for against each game's direction in turn, and the one that lost
/// would just fork its own colour anyway.
/// </summary>
public static class PaletteRegistry
{
  /// <summary>
  /// The one colour every palette must carry. STYLE.md's outline rule is the
  /// most visible thing the cast has in common; a game that picks its own
  /// near-black breaks the family for no gain a reviewer would notice.
  /// </summary>
  public static readonly Palette.Rgb SpineOutline = new(0x1A, 0x1A, 0x1A);
  public const string SpineOutlineName = "outline";

  /// <summary>
  /// Null when the palette carries the spine, otherwise a line explaining what
  /// is wrong and how to fix it.
  /// </summary>
  public static string MissingSpine(Palette palette)
  {
    Palette.Entry named = palette.Entries.FirstOrDefault(e =>
      string.Equals(e.Name, SpineOutlineName, StringComparison.OrdinalIgnoreCase));

    if (named == null)
      return $"no entry named '{SpineOutlineName}'. Every palette carries the shared silhouette " +
             $"colour: add \" 26  26  26\\t{SpineOutlineName}\".";

    if (!named.Color.Equals(SpineOutline))
      return $"'{SpineOutlineName}' is {named.Color.Hex}, but the shared silhouette colour is " +
             $"{SpineOutline.Hex}. Games differ on everything except this one.";

    return null;
  }

  /// <summary>Every palette.gpl in the repo, build output excluded.</summary>
  public static IEnumerable<string> FindAll(string repoRoot)
  {
    if (!Directory.Exists(repoRoot)) yield break;
    foreach (string file in Directory.EnumerateFiles(repoRoot, "palette.gpl", SearchOption.AllDirectories)
               .OrderBy(p => p, StringComparer.Ordinal))
    {
      string rel = Path.GetRelativePath(repoRoot, file);
      if (rel.Split(Path.DirectorySeparatorChar).Any(s => s is "obj" or "bin" or ".git")) continue;
      yield return file;
    }
  }
}
